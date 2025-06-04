using System;
using System.Collections;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер автоматического сохранения игры
    /// </summary>
    public class AutoSaveManager : MonoBehaviour
    {
        [Header("Настройки автосохранения")]
        [Tooltip("Включить автосохранение")]
        public bool enableAutoSave = true;

        [Tooltip("Интервал автосохранения в секундах")]
        [Range(30f, 600f)]
        public float autoSaveInterval = 60f;

        [Tooltip("Сохранять при критических событиях")]
        public bool saveOnCriticalEvents = true;

        [Tooltip("Показывать индикатор сохранения")]
        public bool showSaveIndicator = true;

        [Header("Условия автосохранения")]
        [Tooltip("Сохранять при повышении уровня")]
        public bool saveOnLevelUp = true;

        [Tooltip("Сохранять при смене локации")]
        public bool saveOnLocationChange = true;

        [Tooltip("Сохранять при получении крупной суммы денег")]
        public bool saveOnBigMoneyGain = true;

        [Tooltip("Минимальная сумма для автосохранения")]
        public float bigMoneyThreshold = 1000f;

        [Tooltip("Сохранять при смерти игрока")]
        public bool saveOnPlayerDeath = true;

        [Header("UI компоненты")]
        [Tooltip("Индикатор процесса сохранения")]
        public GameObject saveIndicator;

        [Tooltip("Текст индикатора сохранения")]
        public TMPro.TextMeshProUGUI saveIndicatorText;

        [Tooltip("Анимация индикатора")]
        public Animator saveIndicatorAnimator;

        [Tooltip("Звук при сохранении")]
        public AudioClip saveSound;

        // Компоненты
        private SaveSystem saveSystem;
        private PlayerStats playerStats;
        private LocationManager locationManager;
        private AudioSource audioSource;

        // Состояние
        private Coroutine autoSaveCoroutine;
        private bool isSaving = false;
        private float lastSaveTime;
        private int consecutiveFailures = 0;
        private const int maxFailures = 3;

        // События
        public event Action OnAutoSaveStarted;
        public event Action<SaveOperationResult> OnAutoSaveCompleted;
        public event Action<string> OnSaveIndicatorUpdate;

        #region Unity Methods

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            // Получение ссылок на системы
            saveSystem = SaveSystem.Instance;
            if (saveSystem == null)
            {
                Debug.LogError("AutoSaveManager: SaveSystem не найден!");
                enabled = false;
                return;
            }

            playerStats = GameManager.Instance?.PlayerStats;
            locationManager = FindObjectOfType<LocationManager>();

            // Подписка на события
            SubscribeToEvents();

            // Загрузка настроек
            LoadAutoSaveSettings();

            // Запуск автосохранения
            if (enableAutoSave)
            {
                StartAutoSave();
            }

            // Скрыть индикатор при запуске
            if (saveIndicator != null)
                saveIndicator.SetActive(false);
        }

        private void OnDestroy()
        {
            // Отписка от событий
            UnsubscribeFromEvents();

            // Остановка автосохранения
            StopAutoSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && enableAutoSave)
            {
                // Сохранить при сворачивании приложения
                TriggerAutoSave("Приложение свернуто");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && enableAutoSave)
            {
                // Сохранить при потере фокуса
                TriggerAutoSave("Потеря фокуса");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Запустить автосохранение
        /// </summary>
        public void StartAutoSave()
        {
            if (!enableAutoSave || autoSaveCoroutine != null)
                return;

            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
            Debug.Log($"Автосохранение запущено с интервалом {autoSaveInterval} секунд");
        }

        /// <summary>
        /// Остановить автосохранение
        /// </summary>
        public void StopAutoSave()
        {
            if (autoSaveCoroutine != null)
            {
                StopCoroutine(autoSaveCoroutine);
                autoSaveCoroutine = null;
                Debug.Log("Автосохранение остановлено");
            }
        }

        /// <summary>
        /// Принудительно запустить автосохранение
        /// </summary>
        public void TriggerAutoSave(string reason = "Ручное сохранение")
        {
            if (isSaving)
            {
                Debug.Log("Сохранение уже выполняется, пропускаем...");
                return;
            }

            StartCoroutine(PerformAutoSave(reason));
        }

        /// <summary>
        /// Установить интервал автосохранения
        /// </summary>
        public void SetAutoSaveInterval(float interval)
        {
            autoSaveInterval = Mathf.Clamp(interval, 30f, 600f);
            SaveAutoSaveSettings();

            // Перезапустить автосохранение с новым интервалом
            if (autoSaveCoroutine != null)
            {
                StopAutoSave();
                StartAutoSave();
            }

            Debug.Log($"Интервал автосохранения установлен: {autoSaveInterval} секунд");
        }

        /// <summary>
        /// Включить/выключить автосохранение
        /// </summary>
        public void SetAutoSaveEnabled(bool enabled)
        {
            enableAutoSave = enabled;
            SaveAutoSaveSettings();

            if (enabled)
            {
                StartAutoSave();
            }
            else
            {
                StopAutoSave();
            }

            Debug.Log($"Автосохранение {(enabled ? "включено" : "выключено")}");
        }

        /// <summary>
        /// Получить время до следующего автосохранения
        /// </summary>
        public float GetTimeToNextAutoSave()
        {
            if (!enableAutoSave || autoSaveCoroutine == null)
                return -1f;

            float timeSinceLastSave = Time.time - lastSaveTime;
            return Mathf.Max(0f, autoSaveInterval - timeSinceLastSave);
        }

        /// <summary>
        /// Проверить, выполняется ли сохранение в данный момент
        /// </summary>
        public bool IsSaving()
        {
            return isSaving;
        }

        #endregion

        #region Event Subscriptions

        /// <summary>
        /// Подписаться на игровые события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (saveOnLevelUp)
                GameEvents.OnLevelUp += OnLevelUp;

            if (saveOnLocationChange)
                GameEvents.OnLocationChanged += OnLocationChanged;

            if (saveOnBigMoneyGain)
                GameEvents.OnMoneyEarned += OnMoneyEarned;

            if (saveOnPlayerDeath)
                GameEvents.OnPlayerDeath += OnPlayerDeath;

            // Подписка на события системы сохранений
            if (saveSystem != null)
            {
                saveSystem.OnSaveCompleted += OnSaveCompleted;
            }
        }

        /// <summary>
        /// Отписаться от игровых событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnLocationChanged -= OnLocationChanged;
            GameEvents.OnMoneyEarned -= OnMoneyEarned;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;

            if (saveSystem != null)
            {
                saveSystem.OnSaveCompleted -= OnSaveCompleted;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            if (saveOnLevelUp && enableAutoSave)
            {
                TriggerAutoSave($"Повышение до {data.newLevel} уровня");
            }
        }

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType newLocation, LocationData locationData)
        {
            if (saveOnLocationChange && enableAutoSave)
            {
                TriggerAutoSave($"Переход в {locationData.locationName}");
            }
        }

        /// <summary>
        /// Обработчик получения денег
        /// </summary>
        private void OnMoneyEarned(MoneyEventData data)
        {
            if (saveOnBigMoneyGain && enableAutoSave && data.amount >= bigMoneyThreshold)
            {
                TriggerAutoSave($"Получено {GameUtils.FormatMoney((float)data.amount)}");
            }
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            if (saveOnPlayerDeath && enableAutoSave)
            {
                TriggerAutoSave("Смерть игрока");
            }
        }

        /// <summary>
        /// Обработчик завершения сохранения
        /// </summary>
        private void OnSaveCompleted(SaveOperationResult result)
        {
            if (result.IsSuccess)
            {
                consecutiveFailures = 0;
                lastSaveTime = Time.time;
            }
            else
            {
                consecutiveFailures++;
                Debug.LogWarning($"Ошибка автосохранения: {result.message}");

                // Показать уведомление об ошибке
                GameEvents.TriggerNotification(
                    $"Ошибка сохранения: {result.message}",
                    NotificationType.Warning
                );

                // Отключить автосохранение после нескольких неудач подряд
                if (consecutiveFailures >= maxFailures)
                {
                    enableAutoSave = false;
                    GameEvents.TriggerNotification(
                        "Автосохранение отключено из-за повторяющихся ошибок",
                        NotificationType.Danger
                    );
                }
            }

            OnAutoSaveCompleted?.Invoke(result);
        }

        #endregion

        #region Auto Save Logic

        /// <summary>
        /// Корутина автосохранения
        /// </summary>
        private IEnumerator AutoSaveRoutine()
        {
            while (enableAutoSave)
            {
                yield return new WaitForSeconds(autoSaveInterval);

                // Проверить, стоит ли сохранять
                if (ShouldAutoSave())
                {
                    yield return StartCoroutine(PerformAutoSave("Автосохранение по таймеру"));
                }
            }
        }

        /// <summary>
        /// Выполнить автосохранение
        /// </summary>
        private IEnumerator PerformAutoSave(string reason)
        {
            if (isSaving)
                yield break;

            isSaving = true;
            OnAutoSaveStarted?.Invoke();

            // Показать индикатор сохранения
            if (showSaveIndicator)
            {
                ShowSaveIndicator(reason);
            }

            Debug.Log($"Начинаем автосохранение: {reason}");

            // Небольшая задержка для UI эффектов
            yield return new WaitForSeconds(0.1f);

            // Выполнить сохранение
            SaveOperationResult result = saveSystem.SaveCurrentGame();

            // Обработать результат
            if (result.IsSuccess)
            {
                Debug.Log($"Автосохранение успешно завершено: {reason}");
                
                if (showSaveIndicator)
                {
                    UpdateSaveIndicator("Игра сохранена");
                }

                // Воспроизвести звук
                PlaySaveSound();

                // Показать уведомление
                GameEvents.TriggerNotification("Игра автоматически сохранена", NotificationType.Success, NotificationPriority.Low);
            }
            else
            {
                Debug.LogWarning($"Ошибка автосохранения: {result.message}");
                
                if (showSaveIndicator)
                {
                    UpdateSaveIndicator($"Ошибка: {result.message}");
                }
            }

            // Скрыть индикатор через некоторое время
            if (showSaveIndicator)
            {
                yield return new WaitForSeconds(2f);
                HideSaveIndicator();
            }

            isSaving = false;
        }

        /// <summary>
        /// Проверить, стоит ли выполнять автосохранение
        /// </summary>
        private bool ShouldAutoSave()
        {
            // Не сохранять если игра на паузе
            if (Time.timeScale == 0f)
                return false;

            // Не сохранять если игрок мертв (кроме специального случая)
            if (playerStats != null && playerStats.Health <= 0 && !saveOnPlayerDeath)
                return false;

            // Не сохранять если уже недавно сохранялись
            if (Time.time - lastSaveTime < autoSaveInterval * 0.5f)
                return false;

            return true;
        }

        #endregion

        #region UI Indicator

        /// <summary>
        /// Показать индикатор сохранения
        /// </summary>
        private void ShowSaveIndicator(string message)
        {
            if (saveIndicator == null)
                return;

            saveIndicator.SetActive(true);

            if (saveIndicatorText != null)
            {
                saveIndicatorText.text = $"Сохранение... ({message})";
            }

            if (saveIndicatorAnimator != null)
            {
                saveIndicatorAnimator.SetTrigger("Show");
            }

            OnSaveIndicatorUpdate?.Invoke(message);
        }

        /// <summary>
        /// Обновить текст индикатора
        /// </summary>
        private void UpdateSaveIndicator(string message)
        {
            if (saveIndicatorText != null)
            {
                saveIndicatorText.text = message;
            }

            OnSaveIndicatorUpdate?.Invoke(message);
        }

        /// <summary>
        /// Скрыть индикатор сохранения
        /// </summary>
        private void HideSaveIndicator()
        {
            if (saveIndicator == null)
                return;

            if (saveIndicatorAnimator != null)
            {
                saveIndicatorAnimator.SetTrigger("Hide");
            }
            else
            {
                saveIndicator.SetActive(false);
            }
        }

        /// <summary>
        /// Воспроизвести звук сохранения
        /// </summary>
        private void PlaySaveSound()
        {
            if (saveSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(saveSound, 0.5f);
            }
        }

        #endregion

        #region Settings Persistence

        /// <summary>
        /// Загрузить настройки автосохранения
        /// </summary>
        private void LoadAutoSaveSettings()
        {
            enableAutoSave = PlayerPrefs.GetInt("AutoSaveEnabled", 1) == 1;
            autoSaveInterval = PlayerPrefs.GetFloat("AutoSaveInterval", 60f);
            saveOnCriticalEvents = PlayerPrefs.GetInt("SaveOnCriticalEvents", 1) == 1;
            showSaveIndicator = PlayerPrefs.GetInt("ShowSaveIndicator", 1) == 1;
            saveOnLevelUp = PlayerPrefs.GetInt("SaveOnLevelUp", 1) == 1;
            saveOnLocationChange = PlayerPrefs.GetInt("SaveOnLocationChange", 1) == 1;
            saveOnBigMoneyGain = PlayerPrefs.GetInt("SaveOnBigMoneyGain", 1) == 1;
            bigMoneyThreshold = PlayerPrefs.GetFloat("BigMoneyThreshold", 1000f);
            saveOnPlayerDeath = PlayerPrefs.GetInt("SaveOnPlayerDeath", 1) == 1;
        }

        /// <summary>
        /// Сохранить настройки автосохранения
        /// </summary>
        private void SaveAutoSaveSettings()
        {
            PlayerPrefs.SetInt("AutoSaveEnabled", enableAutoSave ? 1 : 0);
            PlayerPrefs.SetFloat("AutoSaveInterval", autoSaveInterval);
            PlayerPrefs.SetInt("SaveOnCriticalEvents", saveOnCriticalEvents ? 1 : 0);
            PlayerPrefs.SetInt("ShowSaveIndicator", showSaveIndicator ? 1 : 0);
            PlayerPrefs.SetInt("SaveOnLevelUp", saveOnLevelUp ? 1 : 0);
            PlayerPrefs.SetInt("SaveOnLocationChange", saveOnLocationChange ? 1 : 0);
            PlayerPrefs.SetInt("SaveOnBigMoneyGain", saveOnBigMoneyGain ? 1 : 0);
            PlayerPrefs.SetFloat("BigMoneyThreshold", bigMoneyThreshold);
            PlayerPrefs.SetInt("SaveOnPlayerDeath", saveOnPlayerDeath ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Public Configuration Methods

        /// <summary>
        /// Настроить условия автосохранения
        /// </summary>
        public void ConfigureAutoSaveConditions(
            bool levelUp = true,
            bool locationChange = true,
            bool bigMoney = true,
            float moneyThreshold = 1000f,
            bool playerDeath = true)
        {
            saveOnLevelUp = levelUp;
            saveOnLocationChange = locationChange;
            saveOnBigMoneyGain = bigMoney;
            bigMoneyThreshold = moneyThreshold;
            saveOnPlayerDeath = playerDeath;

            SaveAutoSaveSettings();
            Debug.Log("Условия автосохранения обновлены");
        }

        /// <summary>
        /// Получить статистику автосохранения
        /// </summary>
        public AutoSaveStatistics GetStatistics()
        {
            return new AutoSaveStatistics
            {
                isEnabled = enableAutoSave,
                interval = autoSaveInterval,
                timeToNext = GetTimeToNextAutoSave(),
                lastSaveTime = lastSaveTime,
                consecutiveFailures = consecutiveFailures,
                isSaving = isSaving
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Принудительное автосохранение")]
        private void DebugTriggerAutoSave()
        {
            TriggerAutoSave("Отладочное сохранение");
        }

        [ContextMenu("Переключить автосохранение")]
        private void DebugToggleAutoSave()
        {
            SetAutoSaveEnabled(!enableAutoSave);
        }

        [ContextMenu("Показать статистику")]
        private void DebugShowStatistics()
        {
            var stats = GetStatistics();
            Debug.Log($"Автосохранение: {(stats.isEnabled ? "включено" : "выключено")}, " +
                     $"Интервал: {stats.interval}с, " +
                     $"До следующего: {stats.timeToNext:F1}с, " +
                     $"Ошибки подряд: {stats.consecutiveFailures}");
        }

        #endregion
    }

    /// <summary>
    /// Статистика автосохранения
    /// </summary>
    [System.Serializable]
    public class AutoSaveStatistics
    {
        public bool isEnabled;
        public float interval;
        public float timeToNext;
        public float lastSaveTime;
        public int consecutiveFailures;
        public bool isSaving;
    }
}
