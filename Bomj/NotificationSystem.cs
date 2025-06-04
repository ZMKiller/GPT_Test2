using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система уведомлений игры
    /// </summary>
    public class NotificationSystem : MonoBehaviour
    {
        [Header("Настройки уведомлений")]
        [SerializeField] private GameObject notificationPrefab;      // Префаб уведомления
        [SerializeField] private Transform notificationParent;      // Родительский объект для уведомлений
        [SerializeField] private int maxNotifications = 5;          // Максимум одновременных уведомлений
        [SerializeField] private float notificationSpacing = 10f;   // Расстояние между уведомлениями

        [Header("Время отображения")]
        [SerializeField] private float defaultDuration = 3f;        // Время по умолчанию
        [SerializeField] private float infoDuration = 2f;           // Время для информационных
        [SerializeField] private float warningDuration = 4f;        // Время для предупреждений
        [SerializeField] private float dangerDuration = 5f;         // Время для опасных
        [SerializeField] private float achievementDuration = 6f;    // Время для достижений

        [Header("Анимация")]
        [SerializeField] private float slideInDuration = 0.5f;      // Время появления
        [SerializeField] private float slideOutDuration = 0.3f;     // Время исчезновения
        [SerializeField] private AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Стили уведомлений")]
        [SerializeField] private NotificationStyle[] notificationStyles;

        [Header("Звуки")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip infoSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip warningSound;
        [SerializeField] private AudioClip dangerSound;
        [SerializeField] private AudioClip achievementSound;

        // Приватные переменные
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private List<NotificationItem> activeNotifications = new List<NotificationItem>();
        private bool isProcessingQueue = false;

        // События
        public Action<NotificationData> OnNotificationShown;
        public Action<NotificationData> OnNotificationHidden;

        #region Unity Callbacks

        private void Awake()
        {
            InitializeSystem();
        }

        private void Start()
        {
            SetupEventHandlers();
        }

        private void Update()
        {
            ProcessNotificationQueue();
        }

        private void OnDestroy()
        {
            RemoveEventHandlers();
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать систему уведомлений
        /// </summary>
        private void InitializeSystem()
        {
            // Создать AudioSource если не назначен
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Создать родительский объект для уведомлений если не назначен
            if (notificationParent == null)
            {
                GameObject parent = new GameObject("NotificationContainer");
                parent.transform.SetParent(transform, false);
                notificationParent = parent.transform;

                // Настроить как вертикальную группу
                VerticalLayoutGroup layoutGroup = parent.AddComponent<VerticalLayoutGroup>();
                layoutGroup.spacing = notificationSpacing;
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = false;

                // Настроить Content Size Fitter
                ContentSizeFitter sizeFitter = parent.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Инициализировать стили по умолчанию если не назначены
            if (notificationStyles == null || notificationStyles.Length == 0)
            {
                InitializeDefaultStyles();
            }

            Debug.Log("NotificationSystem инициализирована");
        }

        /// <summary>
        /// Инициализировать стили по умолчанию
        /// </summary>
        private void InitializeDefaultStyles()
        {
            notificationStyles = new NotificationStyle[5];

            // Info
            notificationStyles[0] = new NotificationStyle
            {
                type = NotificationType.Info,
                backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.9f),
                textColor = Color.white,
                icon = null
            };

            // Success
            notificationStyles[1] = new NotificationStyle
            {
                type = NotificationType.Success,
                backgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.9f),
                textColor = Color.white,
                icon = null
            };

            // Warning
            notificationStyles[2] = new NotificationStyle
            {
                type = NotificationType.Warning,
                backgroundColor = new Color(0.9f, 0.7f, 0.1f, 0.9f),
                textColor = Color.black,
                icon = null
            };

            // Danger
            notificationStyles[3] = new NotificationStyle
            {
                type = NotificationType.Danger,
                backgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.9f),
                textColor = Color.white,
                icon = null
            };

            // Achievement
            notificationStyles[4] = new NotificationStyle
            {
                type = NotificationType.Achievement,
                backgroundColor = new Color(0.8f, 0.6f, 0.2f, 0.9f),
                textColor = Color.white,
                icon = null
            };
        }

        #endregion

        #region Обработка событий

        /// <summary>
        /// Настроить обработчики событий
        /// </summary>
        private void SetupEventHandlers()
        {
            GameEvents.OnStatChanged += OnStatChanged;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnLowHealth += OnLowHealth;
            GameEvents.OnHighHunger += OnHighHunger;
            GameEvents.OnPlayerDeath += OnPlayerDeath;
            GameEvents.OnNPCInteraction += OnNPCInteraction;
            GameEvents.OnTimeOfDayChanged += OnTimeOfDayChanged;
        }

        /// <summary>
        /// Удалить обработчики событий
        /// </summary>
        private void RemoveEventHandlers()
        {
            GameEvents.OnStatChanged -= OnStatChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnLowHealth -= OnLowHealth;
            GameEvents.OnHighHunger -= OnHighHunger;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnNPCInteraction -= OnNPCInteraction;
            GameEvents.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        }

        /// <summary>
        /// Обработчик изменения характеристик
        /// </summary>
        private void OnStatChanged(StatChangedEventData data)
        {
            // Уведомления о критических изменениях
            if (data.statType == StatType.Health && data.newValue <= 10f)
            {
                ShowNotification("Критически низкое здоровье!", NotificationType.Danger, NotificationPriority.Critical);
            }
            else if (data.statType == StatType.Hunger && data.newValue >= 90f)
            {
                ShowNotification("Вы очень голодны!", NotificationType.Warning, NotificationPriority.High);
            }
            else if (data.statType == StatType.Mood && data.newValue <= 20f)
            {
                ShowNotification("Настроение на нуле...", NotificationType.Warning, NotificationPriority.Normal);
            }
        }

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            ShowNotification($"Поздравляем! Достигнут {data.newLevel} уровень!", 
                           NotificationType.Achievement, NotificationPriority.High);
        }

        /// <summary>
        /// Обработчик низкого здоровья
        /// </summary>
        private void OnLowHealth()
        {
            ShowNotification("Низкое здоровье! Нужно отдохнуть.", NotificationType.Warning, NotificationPriority.High);
        }

        /// <summary>
        /// Обработчик высокого голода
        /// </summary>
        private void OnHighHunger()
        {
            ShowNotification("Сильный голод! Нужно поесть.", NotificationType.Warning, NotificationPriority.High);
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            ShowNotification("Вы умерли! Игра окончена.", NotificationType.Danger, NotificationPriority.Critical);
        }

        /// <summary>
        /// Обработчик взаимодействия с NPC
        /// </summary>
        private void OnNPCInteraction(NPCInteractionEventData data)
        {
            string message = $"Получено {GameUtils.FormatMoney(data.moneyAmount)} от {GetNPCTypeName(data.npcType)}";
            ShowNotification(message, NotificationType.Success, NotificationPriority.Low);
        }

        /// <summary>
        /// Обработчик смены времени дня
        /// </summary>
        private void OnTimeOfDayChanged(TimeOfDay newTime)
        {
            string timeMessage = GetTimeOfDayMessage(newTime);
            ShowNotification(timeMessage, NotificationType.Info, NotificationPriority.Low);
        }

        #endregion

        #region Управление уведомлениями

        /// <summary>
        /// Показать уведомление
        /// </summary>
        /// <param name="message">Текст уведомления</param>
        /// <param name="type">Тип уведомления</param>
        /// <param name="priority">Приоритет</param>
        /// <param name="duration">Время отображения (опционально)</param>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info,
                                   NotificationPriority priority = NotificationPriority.Normal,
                                   float duration = -1f)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // Использовать время по умолчанию если не указано
            if (duration < 0)
            {
                duration = GetDefaultDuration(type);
            }

            NotificationData notificationData = new NotificationData
            {
                message = message,
                type = type,
                priority = priority,
                duration = duration,
                timestamp = Time.time
            };

            // Добавить в очередь
            notificationQueue.Enqueue(notificationData);
        }

        public void ShowNotification(string title, string message, NotificationType type,
                                     NotificationPriority priority = NotificationPriority.Normal,
                                     float duration = -1f)
        {
            ShowNotification($"{title}: {message}", type, priority, duration);
        }

        /// <summary>
        /// Обработать очередь уведомлений
        /// </summary>
        private void ProcessNotificationQueue()
        {
            if (isProcessingQueue || notificationQueue.Count == 0)
                return;

            // Проверить можно ли показать новое уведомление
            if (activeNotifications.Count >= maxNotifications)
            {
                // Удалить самое старое уведомление если оно не критическое
                RemoveOldestNonCriticalNotification();
                return;
            }

            // Получить следующее уведомление из очереди
            NotificationData nextNotification = notificationQueue.Dequeue();
            
            // Проверить приоритет - критические уведомления могут вытеснить другие
            if (nextNotification.priority == NotificationPriority.Critical && 
                activeNotifications.Count >= maxNotifications)
            {
                RemoveLowestPriorityNotification();
            }

            if (activeNotifications.Count < maxNotifications)
            {
                StartCoroutine(ShowNotificationCoroutine(nextNotification));
            }
        }

        /// <summary>
        /// Корутина показа уведомления
        /// </summary>
        private IEnumerator ShowNotificationCoroutine(NotificationData data)
        {
            isProcessingQueue = true;

            // Создать уведомление
            NotificationItem notificationItem = CreateNotificationItem(data);
            if (notificationItem == null)
            {
                isProcessingQueue = false;
                yield break;
            }

            activeNotifications.Add(notificationItem);
            OnNotificationShown?.Invoke(data);

            // Воспроизвести звук
            PlayNotificationSound(data.type);

            // Анимация появления
            yield return StartCoroutine(AnimateNotificationIn(notificationItem));

            // Ожидание времени отображения
            yield return new WaitForSeconds(data.duration);

            // Анимация исчезновения
            yield return StartCoroutine(AnimateNotificationOut(notificationItem));

            // Удаление
            activeNotifications.Remove(notificationItem);
            if (notificationItem.gameObject != null)
            {
                Destroy(notificationItem.gameObject);
            }

            OnNotificationHidden?.Invoke(data);
            isProcessingQueue = false;
        }

        /// <summary>
        /// Создать элемент уведомления
        /// </summary>
        private NotificationItem CreateNotificationItem(NotificationData data)
        {
            if (notificationPrefab == null)
            {
                Debug.LogError("NotificationSystem: Префаб уведомления не назначен!");
                return null;
            }

            GameObject notificationObject = Instantiate(notificationPrefab, notificationParent);
            NotificationItem item = notificationObject.GetComponent<NotificationItem>();
            
            if (item == null)
            {
                item = notificationObject.AddComponent<NotificationItem>();
            }

            // Настроить уведомление
            item.Setup(data, GetNotificationStyle(data.type));
            
            return item;
        }

        /// <summary>
        /// Анимация появления уведомления
        /// </summary>
        private IEnumerator AnimateNotificationIn(NotificationItem item)
        {
            if (item == null) yield break;

            RectTransform rectTransform = item.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = item.gameObject.AddComponent<CanvasGroup>();
            }

            // Начальные параметры
            Vector3 startPosition = rectTransform.localPosition + Vector3.right * 300f;
            Vector3 endPosition = rectTransform.localPosition;
            
            rectTransform.localPosition = startPosition;
            canvasGroup.alpha = 0f;

            float elapsed = 0f;
            
            while (elapsed < slideInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideInDuration;
                float curveValue = slideInCurve.Evaluate(t);

                rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
                canvasGroup.alpha = curveValue;

                yield return null;
            }

            rectTransform.localPosition = endPosition;
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Анимация исчезновения уведомления
        /// </summary>
        private IEnumerator AnimateNotificationOut(NotificationItem item)
        {
            if (item == null) yield break;

            RectTransform rectTransform = item.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();

            Vector3 startPosition = rectTransform.localPosition;
            Vector3 endPosition = startPosition + Vector3.right * 300f;

            float elapsed = 0f;
            
            while (elapsed < slideOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slideOutDuration;
                float curveValue = slideOutCurve.Evaluate(t);

                rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, curveValue);
                canvasGroup.alpha = 1f - curveValue;

                yield return null;
            }
        }

        #endregion

        #region Управление очередью

        /// <summary>
        /// Удалить самое старое некритическое уведомление
        /// </summary>
        private void RemoveOldestNonCriticalNotification()
        {
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                if (activeNotifications[i].Data.priority != NotificationPriority.Critical)
                {
                    StartCoroutine(ForceRemoveNotification(activeNotifications[i]));
                    break;
                }
            }
        }

        /// <summary>
        /// Удалить уведомление с наименьшим приоритетом
        /// </summary>
        private void RemoveLowestPriorityNotification()
        {
            NotificationItem lowestPriority = null;
            NotificationPriority minPriority = NotificationPriority.Critical;

            foreach (var notification in activeNotifications)
            {
                if (notification.Data.priority < minPriority)
                {
                    minPriority = notification.Data.priority;
                    lowestPriority = notification;
                }
            }

            if (lowestPriority != null)
            {
                StartCoroutine(ForceRemoveNotification(lowestPriority));
            }
        }

        /// <summary>
        /// Принудительно удалить уведомление
        /// </summary>
        private IEnumerator ForceRemoveNotification(NotificationItem item)
        {
            if (item == null) yield break;

            yield return StartCoroutine(AnimateNotificationOut(item));

            activeNotifications.Remove(item);
            if (item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }

        #endregion

        #region Утилиты

        /// <summary>
        /// Получить время отображения по умолчанию для типа
        /// </summary>
        private float GetDefaultDuration(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Info:
                    return infoDuration;
                case NotificationType.Success:
                    return defaultDuration;
                case NotificationType.Warning:
                    return warningDuration;
                case NotificationType.Danger:
                    return dangerDuration;
                case NotificationType.Achievement:
                    return achievementDuration;
                default:
                    return defaultDuration;
            }
        }

        /// <summary>
        /// Получить стиль уведомления по типу
        /// </summary>
        private NotificationStyle GetNotificationStyle(NotificationType type)
        {
            if (notificationStyles != null)
            {
                foreach (var style in notificationStyles)
                {
                    if (style.type == type)
                    {
                        return style;
                    }
                }
            }

            // Стиль по умолчанию
            return new NotificationStyle
            {
                type = type,
                backgroundColor = Color.gray,
                textColor = Color.white,
                icon = null
            };
        }

        /// <summary>
        /// Воспроизвести звук уведомления
        /// </summary>
        private void PlayNotificationSound(NotificationType type)
        {
            if (audioSource == null) return;

            AudioClip clipToPlay = null;

            switch (type)
            {
                case NotificationType.Info:
                    clipToPlay = infoSound;
                    break;
                case NotificationType.Success:
                    clipToPlay = successSound;
                    break;
                case NotificationType.Warning:
                    clipToPlay = warningSound;
                    break;
                case NotificationType.Danger:
                    clipToPlay = dangerSound;
                    break;
                case NotificationType.Achievement:
                    clipToPlay = achievementSound;
                    break;
            }

            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        /// <summary>
        /// Получить название типа NPC
        /// </summary>
        private string GetNPCTypeName(NPCType type)
        {
            switch (type)
            {
                case NPCType.Poor:
                    return "бедного";
                case NPCType.Normal:
                    return "прохожего";
                case NPCType.Rich:
                    return "богатого";
                case NPCType.Tourist:
                    return "туриста";
                case NPCType.Businessman:
                    return "бизнесмена";
                default:
                    return "прохожего";
            }
        }

        /// <summary>
        /// Получить сообщение о времени дня
        /// </summary>
        private string GetTimeOfDayMessage(TimeOfDay time)
        {
            switch (time)
            {
                case TimeOfDay.Morning:
                    return "Наступило утро";
                case TimeOfDay.Day:
                    return "Наступил день";
                case TimeOfDay.Evening:
                    return "Наступил вечер";
                case TimeOfDay.Night:
                    return "Наступила ночь";
                default:
                    return "Время изменилось";
            }
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Очистить все уведомления
        /// </summary>
        public void ClearAllNotifications()
        {
            notificationQueue.Clear();
            
            foreach (var notification in activeNotifications)
            {
                if (notification != null && notification.gameObject != null)
                {
                    Destroy(notification.gameObject);
                }
            }
            
            activeNotifications.Clear();
        }

        /// <summary>
        /// Получить количество активных уведомлений
        /// </summary>
        public int GetActiveNotificationCount()
        {
            return activeNotifications.Count;
        }

        /// <summary>
        /// Получить количество уведомлений в очереди
        /// </summary>
        public int GetQueuedNotificationCount()
        {
            return notificationQueue.Count;
        }

        /// <summary>
        /// Установить максимальное количество уведомлений
        /// </summary>
        public void SetMaxNotifications(int max)
        {
            maxNotifications = Mathf.Max(1, max);
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Показать тестовое уведомление
        /// </summary>
        [ContextMenu("Test Info Notification")]
        private void TestInfoNotification()
        {
            if (Application.isPlaying)
            {
                ShowNotification("Тестовое информационное уведомление", NotificationType.Info);
            }
        }

        /// <summary>
        /// Показать тестовое предупреждение
        /// </summary>
        [ContextMenu("Test Warning Notification")]
        private void TestWarningNotification()
        {
            if (Application.isPlaying)
            {
                ShowNotification("Тестовое предупреждение!", NotificationType.Warning);
            }
        }

        #endregion
    }

    /// <summary>
    /// Данные уведомления
    /// </summary>
    [System.Serializable]
    public class NotificationData
    {
        public string message;
        public NotificationType type;
        public NotificationPriority priority;
        public float duration;
        public float timestamp;
    }

    /// <summary>
    /// Стиль уведомления
    /// </summary>
    [System.Serializable]
    public class NotificationStyle
    {
        public NotificationType type;
        public Color backgroundColor;
        public Color textColor;
        public Sprite icon;
    }

    /// <summary>
    /// Элемент уведомления
    /// </summary>
    public class NotificationItem : MonoBehaviour
    {
        [SerializeField] private Text messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;

        private NotificationData data;

        public NotificationData Data => data;

        /// <summary>
        /// Настроить уведомление
        /// </summary>
        public void Setup(NotificationData notificationData, NotificationStyle style)
        {
            data = notificationData;

            // Настроить текст
            if (messageText != null)
            {
                messageText.text = data.message;
                messageText.color = style.textColor;
            }

            // Настроить фон
            if (backgroundImage != null)
            {
                backgroundImage.color = style.backgroundColor;
            }

            // Настроить иконку
            if (iconImage != null && style.icon != null)
            {
                iconImage.sprite = style.icon;
                iconImage.gameObject.SetActive(true);
            }
            else if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
        }
    }
}