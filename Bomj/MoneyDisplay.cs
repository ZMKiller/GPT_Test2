using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Компонент отображения денег с анимациями
    /// </summary>
    public class MoneyDisplay : MonoBehaviour
    {
        [Header("UI элементы")]
        [SerializeField] private Text moneyText;                    // Основной текст денег
        [SerializeField] private Text moneyChangeText;              // Текст изменения (+/-)
        [SerializeField] private Image moneyIcon;                   // Иконка денег
        [SerializeField] private Animator moneyAnimator;            // Аниматор для эффектов

        [Header("Настройки отображения")]
        [SerializeField] private string currencySymbol = "₽";       // Символ валюты
        [SerializeField] private bool useShortFormat = true;        // Использовать короткий формат
        [SerializeField] private bool showPlusSign = true;          // Показывать знак "+" для положительных изменений
        [SerializeField] private int decimalPlaces = 0;             // Количество знаков после запятой

        [Header("Цвета")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color gainColor = Color.green;
        [SerializeField] private Color lossColor = Color.red;
        [SerializeField] private Color highAmountColor = Color.yellow;
        [SerializeField] private float highAmountThreshold = 1000000f; // Порог "больших денег"

        [Header("Анимация изменения")]
        [SerializeField] private float changeDisplayDuration = 2f;   // Длительность показа изменения
        [SerializeField] private float changeAnimationSpeed = 1f;    // Скорость анимации изменения
        [SerializeField] private Vector3 changeTextOffset = new Vector3(0, 50, 0); // Смещение текста изменения
        [SerializeField] private AnimationCurve changeScaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Анимация счетчика")]
        [SerializeField] private float counterAnimationDuration = 1f; // Длительность анимации счетчика
        [SerializeField] private AnimationCurve counterCurve = AnimationCurve.EaseOut(0, 0, 1, 1);
        [SerializeField] private bool useCounterAnimation = true;     // Использовать анимацию счетчика

        [Header("Эффекты")]
        [SerializeField] private ParticleSystem coinParticles;       // Частицы монет
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] coinSounds;             // Звуки получения денег
        [SerializeField] private AudioClip spendSound;               // Звук траты денег

        // Приватные переменные
        private float currentDisplayedAmount = 0f;
        private float targetAmount = 0f;
        private PlayerStats playerStats;
        
        // Корутины
        private Coroutine counterCoroutine;
        private Coroutine changeTextCoroutine;

        // Кеш для компонентов
        private RectTransform moneyChangeTextRect;
        private Vector3 originalChangeTextPosition;

        #region Unity Callbacks

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupEventHandlers();
            InitializeDisplay();
        }

        private void OnDestroy()
        {
            RemoveEventHandlers();
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать компоненты
        /// </summary>
        private void InitializeComponents()
        {
            // Найти PlayerStats
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null && GameManager.Instance != null)
            {
                playerStats = GameManager.Instance.PlayerStats;
            }

            // Получить RectTransform для текста изменения
            if (moneyChangeText != null)
            {
                moneyChangeTextRect = moneyChangeText.GetComponent<RectTransform>();
                if (moneyChangeTextRect != null)
                {
                    originalChangeTextPosition = moneyChangeTextRect.localPosition;
                }
            }

            // Создать AudioSource если не назначен
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Скрыть текст изменения изначально
            if (moneyChangeText != null)
            {
                moneyChangeText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Инициализировать отображение
        /// </summary>
        private void InitializeDisplay()
        {
            if (playerStats != null)
            {
                targetAmount = playerStats.Money;
                currentDisplayedAmount = targetAmount;
                UpdateMoneyText(currentDisplayedAmount);
            }
        }

        #endregion

        #region Обработка событий

        /// <summary>
        /// Настроить обработчики событий
        /// </summary>
        private void SetupEventHandlers()
        {
            GameEvents.OnStatChanged += OnStatChanged;
            GameEvents.OnMoneyEarned += OnMoneyEarned;
        }

        /// <summary>
        /// Удалить обработчики событий
        /// </summary>
        private void RemoveEventHandlers()
        {
            GameEvents.OnStatChanged -= OnStatChanged;
            GameEvents.OnMoneyEarned -= OnMoneyEarned;
        }

        /// <summary>
        /// Обработчик изменения характеристик
        /// </summary>
        private void OnStatChanged(StatChangedEventData data)
        {
            if (data.statType == StatType.Money)
            {
                float change = data.newValue - currentDisplayedAmount;
                UpdateMoney(data.newValue);
                
                if (Mathf.Abs(change) > 0.01f)
                {
                    ShowMoneyChange(change, change > 0);
                }
            }
        }

        /// <summary>
        /// Обработчик заработка денег
        /// </summary>
        private void OnMoneyEarned(MoneyEventData data)
        {
            PlayMoneyEffect(true);
        }

        #endregion

        #region Обновление отображения

        /// <summary>
        /// Обновить отображение денег
        /// </summary>
        /// <param name="newAmount">Новая сумма</param>
        public void UpdateMoney(float newAmount)
        {
            targetAmount = newAmount;

            if (useCounterAnimation && Mathf.Abs(newAmount - currentDisplayedAmount) > 0.01f)
            {
                StartCounterAnimation();
            }
            else
            {
                currentDisplayedAmount = newAmount;
                UpdateMoneyText(currentDisplayedAmount);
                UpdateMoneyColor(currentDisplayedAmount);
            }
        }

        /// <summary>
        /// Показать изменение денег
        /// </summary>
        /// <param name="change">Изменение суммы</param>
        /// <param name="isPositive">Положительное изменение</param>
        public void ShowMoneyChange(float change, bool isPositive)
        {
            if (moneyChangeText == null) return;

            // Остановить предыдущую анимацию если идет
            if (changeTextCoroutine != null)
            {
                StopCoroutine(changeTextCoroutine);
            }

            changeTextCoroutine = StartCoroutine(AnimateMoneyChange(change, isPositive));
            PlayMoneyEffect(isPositive);
        }

        /// <summary>
        /// Обновить текст денег
        /// </summary>
        private void UpdateMoneyText(float amount)
        {
            if (moneyText == null) return;

            string formattedAmount = FormatMoney(amount);
            moneyText.text = $"{formattedAmount} {currencySymbol}";
        }

        /// <summary>
        /// Обновить цвет текста денег
        /// </summary>
        private void UpdateMoneyColor(float amount)
        {
            if (moneyText == null) return;

            Color targetColor = normalColor;
            
            if (amount >= highAmountThreshold)
            {
                targetColor = highAmountColor;
            }

            moneyText.color = targetColor;
        }

        /// <summary>
        /// Форматировать деньги для отображения
        /// </summary>
        private string FormatMoney(float amount)
        {
            if (useShortFormat)
            {
                return GameUtils.FormatMoney(amount);
            }
            else
            {
                return amount.ToString($"F{decimalPlaces}");
            }
        }

        #endregion

        #region Анимации

        /// <summary>
        /// Запустить анимацию счетчика
        /// </summary>
        private void StartCounterAnimation()
        {
            if (counterCoroutine != null)
            {
                StopCoroutine(counterCoroutine);
            }

            counterCoroutine = StartCoroutine(AnimateCounter());
        }

        /// <summary>
        /// Анимация счетчика денег
        /// </summary>
        private IEnumerator AnimateCounter()
        {
            float startAmount = currentDisplayedAmount;
            float elapsed = 0f;

            while (elapsed < counterAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / counterAnimationDuration;
                float curveValue = counterCurve.Evaluate(t);
                
                currentDisplayedAmount = Mathf.Lerp(startAmount, targetAmount, curveValue);
                UpdateMoneyText(currentDisplayedAmount);
                
                yield return null;
            }

            currentDisplayedAmount = targetAmount;
            UpdateMoneyText(currentDisplayedAmount);
            UpdateMoneyColor(currentDisplayedAmount);
            
            counterCoroutine = null;
        }

        /// <summary>
        /// Анимация изменения денег
        /// </summary>
        private IEnumerator AnimateMoneyChange(float change, bool isPositive)
        {
            if (moneyChangeText == null || moneyChangeTextRect == null)
                yield break;

            // Подготовить текст
            string changeText = isPositive && showPlusSign ? "+" : "";
            changeText += FormatMoney(Mathf.Abs(change));
            if (!string.IsNullOrEmpty(currencySymbol))
            {
                changeText += " " + currencySymbol;
            }

            moneyChangeText.text = changeText;
            moneyChangeText.color = isPositive ? gainColor : lossColor;
            moneyChangeText.gameObject.SetActive(true);

            // Начальные параметры
            moneyChangeTextRect.localPosition = originalChangeTextPosition;
            moneyChangeTextRect.localScale = Vector3.zero;
            Color startColor = moneyChangeText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            float elapsed = 0f;
            
            while (elapsed < changeDisplayDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / changeDisplayDuration;
                
                // Анимация масштаба
                float scaleValue = changeScaleCurve.Evaluate(t);
                moneyChangeTextRect.localScale = Vector3.one * scaleValue;
                
                // Анимация позиции
                Vector3 currentOffset = Vector3.Lerp(Vector3.zero, changeTextOffset, t);
                moneyChangeTextRect.localPosition = originalChangeTextPosition + currentOffset;
                
                // Анимация прозрачности (исчезновение в конце)
                if (t > 0.7f)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    moneyChangeText.color = Color.Lerp(startColor, endColor, fadeT);
                }
                
                yield return null;
            }

            // Скрыть текст
            moneyChangeText.gameObject.SetActive(false);
            moneyChangeTextRect.localPosition = originalChangeTextPosition;
            moneyChangeTextRect.localScale = Vector3.one;
            
            changeTextCoroutine = null;
        }

        #endregion

        #region Эффекты

        /// <summary>
        /// Воспроизвести эффект денег
        /// </summary>
        /// <param name="isGain">Получение денег или трата</param>
        private void PlayMoneyEffect(bool isGain)
        {
            // Анимация через Animator
            if (moneyAnimator != null)
            {
                if (isGain)
                {
                    moneyAnimator.SetTrigger("MoneyGain");
                }
                else
                {
                    moneyAnimator.SetTrigger("MoneyLoss");
                }
            }

            // Частицы
            if (coinParticles != null && isGain)
            {
                coinParticles.Play();
            }

            // Звук
            PlayMoneySound(isGain);

            // Анимация иконки
            if (moneyIcon != null)
            {
                StartCoroutine(AnimateMoneyIcon(isGain));
            }
        }

        /// <summary>
        /// Воспроизвести звук денег
        /// </summary>
        private void PlayMoneySound(bool isGain)
        {
            if (audioSource == null) return;

            AudioClip clipToPlay = null;

            if (isGain && coinSounds != null && coinSounds.Length > 0)
            {
                // Случайный звук монеты
                clipToPlay = coinSounds[Random.Range(0, coinSounds.Length)];
            }
            else if (!isGain && spendSound != null)
            {
                clipToPlay = spendSound;
            }

            if (clipToPlay != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f); // Небольшая вариация
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        /// <summary>
        /// Анимация иконки денег
        /// </summary>
        private IEnumerator AnimateMoneyIcon(bool isGain)
        {
            if (moneyIcon == null) yield break;

            Vector3 originalScale = moneyIcon.transform.localScale;
            float animationDuration = 0.3f;
            float scaleMultiplier = isGain ? 1.2f : 0.8f;

            // Анимация масштаба
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                
                float scale = Mathf.Lerp(1f, scaleMultiplier, Mathf.PingPong(t * 2f, 1f));
                moneyIcon.transform.localScale = originalScale * scale;
                
                yield return null;
            }

            moneyIcon.transform.localScale = originalScale;
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Установить символ валюты
        /// </summary>
        public void SetCurrencySymbol(string symbol)
        {
            currencySymbol = symbol;
            UpdateMoneyText(currentDisplayedAmount);
        }

        /// <summary>
        /// Включить/выключить короткий формат
        /// </summary>
        public void SetShortFormat(bool useShort)
        {
            useShortFormat = useShort;
            UpdateMoneyText(currentDisplayedAmount);
        }

        /// <summary>
        /// Установить количество знаков после запятой
        /// </summary>
        public void SetDecimalPlaces(int places)
        {
            decimalPlaces = Mathf.Max(0, places);
            UpdateMoneyText(currentDisplayedAmount);
        }

        /// <summary>
        /// Принудительно обновить отображение
        /// </summary>
        public void ForceUpdate()
        {
            if (playerStats != null)
            {
                UpdateMoney(playerStats.Money);
            }
        }

        /// <summary>
        /// Установить цвета
        /// </summary>
        public void SetColors(Color normal, Color gain, Color loss, Color high)
        {
            normalColor = normal;
            gainColor = gain;
            lossColor = loss;
            highAmountColor = high;
            
            UpdateMoneyColor(currentDisplayedAmount);
        }

        /// <summary>
        /// Установить порог "больших денег"
        /// </summary>
        public void SetHighAmountThreshold(float threshold)
        {
            highAmountThreshold = threshold;
            UpdateMoneyColor(currentDisplayedAmount);
        }

        /// <summary>
        /// Получить текущую отображаемую сумму
        /// </summary>
        public float GetDisplayedAmount()
        {
            return currentDisplayedAmount;
        }

        /// <summary>
        /// Проверить идет ли анимация счетчика
        /// </summary>
        public bool IsAnimating()
        {
            return counterCoroutine != null || changeTextCoroutine != null;
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Тестовое добавление денег
        /// </summary>
        [ContextMenu("Test Add Money")]
        private void TestAddMoney()
        {
            if (Application.isPlaying)
            {
                ShowMoneyChange(100f, true);
                UpdateMoney(currentDisplayedAmount + 100f);
            }
        }

        /// <summary>
        /// Тестовая трата денег
        /// </summary>
        [ContextMenu("Test Spend Money")]
        private void TestSpendMoney()
        {
            if (Application.isPlaying)
            {
                ShowMoneyChange(-50f, false);
                UpdateMoney(currentDisplayedAmount - 50f);
            }
        }

        /// <summary>
        /// Тест большой суммы
        /// </summary>
        [ContextMenu("Test Big Amount")]
        private void TestBigAmount()
        {
            if (Application.isPlaying)
            {
                UpdateMoney(1500000f);
            }
        }

        #endregion
    }
}