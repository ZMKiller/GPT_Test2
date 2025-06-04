using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Панель отображения характеристик игрока
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [Header("Элементы здоровья")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private Gradient healthColorGradient;

        [Header("Элементы голода")]
        [SerializeField] private Slider hungerSlider;
        [SerializeField] private Text hungerText;
        [SerializeField] private Image hungerFill;
        [SerializeField] private Color hungerColorLow = Color.red;
        [SerializeField] private Color hungerColorHigh = Color.yellow;

        [Header("Элементы настроения")]
        [SerializeField] private Slider moodSlider;
        [SerializeField] private Text moodText;
        [SerializeField] private Image moodIcon;
        [SerializeField] private Sprite[] moodSprites; // 0-плохое, 1-среднее, 2-хорошее
        [SerializeField] private Gradient moodColorGradient;

        [Header("Элементы уровня и опыта")]
        [SerializeField] private Text levelText;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private Text experienceText;
        [SerializeField] private Image experienceFill;

        [Header("Анимация")]
        [SerializeField] private Animator panelAnimator;
        [SerializeField] private float updateSmoothness = 5f;
        [SerializeField] private bool useWarningAnimations = true;

        [Header("Предупреждающие эффекты")]
        [SerializeField] private Image warningOverlay;
        [SerializeField] private Color warningColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private float warningFlashSpeed = 2f;

        // Компоненты и ссылки
        private PlayerStats playerStats;
        private CanvasGroup canvasGroup;
        
        // Кешированные значения для плавной анимации
        private float targetHealthValue;
        private float targetHungerValue;
        private float targetMoodValue;
        private float targetExperienceValue;
        
        // Состояние предупреждений
        private bool isShowingHealthWarning = false;
        private bool isShowingHungerWarning = false;
        
        // Корутины анимаций
        private Coroutine healthWarningCoroutine;
        private Coroutine hungerWarningCoroutine;

        #region Unity Callbacks

        private void Awake()
        {
            // Получить компоненты
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            InitializeComponents();
            SetupEventHandlers();
            InitializeDisplay();
        }

        private void Update()
        {
            UpdateSmoothValues();
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

            if (playerStats == null)
            {
                Debug.LogError("StatsPanel: PlayerStats не найден!");
                return;
            }

            // Создать градиенты по умолчанию если не назначены
            if (healthColorGradient == null)
            {
                healthColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0].color = Color.red;
                colorKeys[0].time = 0f;
                colorKeys[1].color = Color.yellow;
                colorKeys[1].time = 0.5f;
                colorKeys[2].color = Color.green;
                colorKeys[2].time = 1f;
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0].alpha = 1f;
                alphaKeys[0].time = 0f;
                alphaKeys[1].alpha = 1f;
                alphaKeys[1].time = 1f;
                
                healthColorGradient.SetKeys(colorKeys, alphaKeys);
            }

            if (moodColorGradient == null)
            {
                moodColorGradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0].color = new Color(0.5f, 0.5f, 1f); // Грустный синий
                colorKeys[0].time = 0f;
                colorKeys[1].color = Color.white; // Нейтральный
                colorKeys[1].time = 0.5f;
                colorKeys[2].color = Color.yellow; // Счастливый желтый
                colorKeys[2].time = 1f;
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0].alpha = 1f;
                alphaKeys[0].time = 0f;
                alphaKeys[1].alpha = 1f;
                alphaKeys[1].time = 1f;
                
                moodColorGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        /// <summary>
        /// Инициализировать отображение
        /// </summary>
        private void InitializeDisplay()
        {
            if (playerStats == null) return;

            // Установить начальные значения
            targetHealthValue = playerStats.HealthPercentage;
            targetHungerValue = playerStats.HungerPercentage;
            targetMoodValue = playerStats.MoodPercentage;
            targetExperienceValue = playerStats.ExperiencePercentage;

            // Обновить сразу без анимации
            UpdateHealthDisplay(true);
            UpdateHungerDisplay(true);
            UpdateMoodDisplay(true);
            UpdateLevelDisplay();
            UpdateExperienceDisplay(true);
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
        }

        /// <summary>
        /// Обработчик изменения характеристик
        /// </summary>
        private void OnStatChanged(StatChangedEventData data)
        {
            switch (data.statType)
            {
                case StatType.Health:
                    targetHealthValue = data.newValue / data.maxValue;
                    break;
                case StatType.Hunger:
                    targetHungerValue = data.newValue / data.maxValue;
                    break;
                case StatType.Mood:
                    targetMoodValue = data.newValue / data.maxValue;
                    break;
                case StatType.Level:
                    UpdateLevelDisplay();
                    break;
                case StatType.Experience:
                    targetExperienceValue = playerStats.ExperiencePercentage;
                    break;
            }
        }

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            // Анимация повышения уровня
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("LevelUp");
            }

            // Показать уведомление через UIManager
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification(
                    $"Поздравляем! Достигнут {data.newLevel} уровень!",
                    NotificationType.Achievement,
                    NotificationPriority.High
                );
            }
        }

        /// <summary>
        /// Обработчик низкого здоровья
        /// </summary>
        private void OnLowHealth()
        {
            if (useWarningAnimations && !isShowingHealthWarning)
            {
                StartHealthWarning();
            }
        }

        /// <summary>
        /// Обработчик высокого голода
        /// </summary>
        private void OnHighHunger()
        {
            if (useWarningAnimations && !isShowingHungerWarning)
            {
                StartHungerWarning();
            }
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            // Остановить все предупреждающие анимации
            StopAllWarnings();
            
            // Приглушить панель
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.5f;
            }
        }

        #endregion

        #region Обновление отображения

        /// <summary>
        /// Обновить плавные значения
        /// </summary>
        private void UpdateSmoothValues()
        {
            if (playerStats == null) return;

            // Плавное обновление слайдеров
            bool healthChanged = UpdateHealthDisplay(false);
            bool hungerChanged = UpdateHungerDisplay(false);
            bool moodChanged = UpdateMoodDisplay(false);
            bool expChanged = UpdateExperienceDisplay(false);

            // Проверить нужно ли остановить предупреждения
            if (healthChanged && playerStats.HealthPercentage > GameUtils.LOW_HEALTH_THRESHOLD / 100f)
            {
                StopHealthWarning();
            }

            if (hungerChanged && playerStats.HungerPercentage < GameUtils.HIGH_HUNGER_THRESHOLD / 100f)
            {
                StopHungerWarning();
            }
        }

        /// <summary>
        /// Обновить отображение здоровья
        /// </summary>
        /// <param name="immediate">Немедленное обновление</param>
        /// <returns>True если значение изменилось</returns>
        private bool UpdateHealthDisplay(bool immediate)
        {
            if (healthSlider == null) return false;

            float currentValue = healthSlider.value;
            float newValue = immediate ? targetHealthValue : 
                           Mathf.Lerp(currentValue, targetHealthValue, Time.deltaTime * updateSmoothness);

            if (Mathf.Abs(currentValue - newValue) > 0.001f)
            {
                healthSlider.value = newValue;

                // Обновить текст
                if (healthText != null && playerStats != null)
                {
                    healthText.text = $"{playerStats.Health:F0}/{playerStats.MaxHealth:F0}";
                }

                // Обновить цвет
                if (healthFill != null)
                {
                    healthFill.color = healthColorGradient.Evaluate(newValue);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Обновить отображение голода
        /// </summary>
        /// <param name="immediate">Немедленное обновление</param>
        /// <returns>True если значение изменилось</returns>
        private bool UpdateHungerDisplay(bool immediate)
        {
            if (hungerSlider == null) return false;

            float currentValue = hungerSlider.value;
            float newValue = immediate ? targetHungerValue : 
                           Mathf.Lerp(currentValue, targetHungerValue, Time.deltaTime * updateSmoothness);

            if (Mathf.Abs(currentValue - newValue) > 0.001f)
            {
                hungerSlider.value = newValue;

                // Обновить текст
                if (hungerText != null && playerStats != null)
                {
                    hungerText.text = $"{playerStats.Hunger:F0}/{playerStats.MaxHunger:F0}";
                }

                // Обновить цвет (инвертированный - чем больше голод, тем хуже)
                if (hungerFill != null)
                {
                    hungerFill.color = Color.Lerp(hungerColorHigh, hungerColorLow, newValue);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Обновить отображение настроения
        /// </summary>
        /// <param name="immediate">Немедленное обновление</param>
        /// <returns>True если значение изменилось</returns>
        private bool UpdateMoodDisplay(bool immediate)
        {
            if (moodSlider == null) return false;

            float currentValue = moodSlider.value;
            float newValue = immediate ? targetMoodValue : 
                           Mathf.Lerp(currentValue, targetMoodValue, Time.deltaTime * updateSmoothness);

            if (Mathf.Abs(currentValue - newValue) > 0.001f)
            {
                moodSlider.value = newValue;

                // Обновить текст
                if (moodText != null && playerStats != null)
                {
                    moodText.text = $"{playerStats.Mood:F0}/{playerStats.MaxMood:F0}";
                }

                // Обновить цвет
                if (moodSlider.fillRect != null)
                {
                    Image fill = moodSlider.fillRect.GetComponent<Image>();
                    if (fill != null)
                    {
                        fill.color = moodColorGradient.Evaluate(newValue);
                    }
                }

                // Обновить иконку настроения
                UpdateMoodIcon(newValue);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Обновить отображение уровня
        /// </summary>
        private void UpdateLevelDisplay()
        {
            if (levelText != null && playerStats != null)
            {
                levelText.text = $"Уровень {playerStats.Level}";
            }
        }

        /// <summary>
        /// Обновить отображение опыта
        /// </summary>
        /// <param name="immediate">Немедленное обновление</param>
        /// <returns>True если значение изменилось</returns>
        private bool UpdateExperienceDisplay(bool immediate)
        {
            if (experienceSlider == null) return false;

            float currentValue = experienceSlider.value;
            float newValue = immediate ? targetExperienceValue : 
                           Mathf.Lerp(currentValue, targetExperienceValue, Time.deltaTime * updateSmoothness);

            if (Mathf.Abs(currentValue - newValue) > 0.001f)
            {
                experienceSlider.value = newValue;

                // Обновить текст
                if (experienceText != null && playerStats != null)
                {
                    experienceText.text = $"{playerStats.Experience:F0}/{playerStats.ExperienceToNext:F0}";
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Обновить иконку настроения
        /// </summary>
        private void UpdateMoodIcon(float moodPercentage)
        {
            if (moodIcon == null || moodSprites == null || moodSprites.Length == 0)
                return;

            int spriteIndex;
            if (moodPercentage < 0.33f)
                spriteIndex = 0; // Плохое настроение
            else if (moodPercentage < 0.66f)
                spriteIndex = 1; // Среднее настроение
            else
                spriteIndex = 2; // Хорошее настроение

            if (spriteIndex < moodSprites.Length && moodSprites[spriteIndex] != null)
            {
                moodIcon.sprite = moodSprites[spriteIndex];
            }
        }

        #endregion

        #region Предупреждающие анимации

        /// <summary>
        /// Запустить предупреждение о низком здоровье
        /// </summary>
        private void StartHealthWarning()
        {
            if (healthWarningCoroutine != null)
            {
                StopCoroutine(healthWarningCoroutine);
            }

            isShowingHealthWarning = true;
            healthWarningCoroutine = StartCoroutine(HealthWarningAnimation());
        }

        /// <summary>
        /// Остановить предупреждение о низком здоровье
        /// </summary>
        private void StopHealthWarning()
        {
            if (healthWarningCoroutine != null)
            {
                StopCoroutine(healthWarningCoroutine);
                healthWarningCoroutine = null;
            }

            isShowingHealthWarning = false;

            // Восстановить нормальный цвет
            if (healthFill != null && playerStats != null)
            {
                healthFill.color = healthColorGradient.Evaluate(playerStats.HealthPercentage);
            }
        }

        /// <summary>
        /// Запустить предупреждение о высоком голоде
        /// </summary>
        private void StartHungerWarning()
        {
            if (hungerWarningCoroutine != null)
            {
                StopCoroutine(hungerWarningCoroutine);
            }

            isShowingHungerWarning = true;
            hungerWarningCoroutine = StartCoroutine(HungerWarningAnimation());
        }

        /// <summary>
        /// Остановить предупреждение о высоком голоде
        /// </summary>
        private void StopHungerWarning()
        {
            if (hungerWarningCoroutine != null)
            {
                StopCoroutine(hungerWarningCoroutine);
                hungerWarningCoroutine = null;
            }

            isShowingHungerWarning = false;

            // Восстановить нормальный цвет
            if (hungerFill != null && playerStats != null)
            {
                hungerFill.color = Color.Lerp(hungerColorHigh, hungerColorLow, playerStats.HungerPercentage);
            }
        }

        /// <summary>
        /// Остановить все предупреждения
        /// </summary>
        private void StopAllWarnings()
        {
            StopHealthWarning();
            StopHungerWarning();

            if (warningOverlay != null)
            {
                warningOverlay.color = Color.clear;
            }
        }

        /// <summary>
        /// Анимация предупреждения здоровья
        /// </summary>
        private IEnumerator HealthWarningAnimation()
        {
            Color normalColor = healthColorGradient.Evaluate(playerStats.HealthPercentage);
            Color warningColorHealth = Color.red;

            while (isShowingHealthWarning)
            {
                // Мигание красным цветом
                float flash = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
                Color currentColor = Color.Lerp(normalColor, warningColorHealth, flash);
                
                if (healthFill != null)
                {
                    healthFill.color = currentColor;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Анимация предупреждения голода
        /// </summary>
        private IEnumerator HungerWarningAnimation()
        {
            Color normalColor = Color.Lerp(hungerColorHigh, hungerColorLow, playerStats.HungerPercentage);
            Color warningColorHunger = Color.red;

            while (isShowingHungerWarning)
            {
                // Мигание красным цветом
                float flash = Mathf.PingPong(Time.time * warningFlashSpeed, 1f);
                Color currentColor = Color.Lerp(normalColor, warningColorHunger, flash);
                
                if (hungerFill != null)
                {
                    hungerFill.color = currentColor;
                }

                yield return null;
            }
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Принудительно обновить отображение
        /// </summary>
        public void UpdateDisplay()
        {
            if (playerStats == null) return;

            targetHealthValue = playerStats.HealthPercentage;
            targetHungerValue = playerStats.HungerPercentage;
            targetMoodValue = playerStats.MoodPercentage;
            targetExperienceValue = playerStats.ExperiencePercentage;

            UpdateHealthDisplay(true);
            UpdateHungerDisplay(true);
            UpdateMoodDisplay(true);
            UpdateLevelDisplay();
            UpdateExperienceDisplay(true);
        }

        /// <summary>
        /// Установить видимость панели
        /// </summary>
        /// <param name="visible">Видимость</param>
        /// <param name="animated">Анимированный переход</param>
        public void SetVisible(bool visible, bool animated = true)
        {
            if (canvasGroup == null) return;

            if (animated)
            {
                StartCoroutine(FadePanel(visible));
            }
            else
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        /// <summary>
        /// Анимация затухания панели
        /// </summary>
        private IEnumerator FadePanel(bool fadeIn)
        {
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = fadeIn ? 1f : 0f;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = fadeIn;
            canvasGroup.blocksRaycasts = fadeIn;
        }

        /// <summary>
        /// Включить/выключить предупреждающие анимации
        /// </summary>
        public void SetWarningAnimations(bool enabled)
        {
            useWarningAnimations = enabled;
            
            if (!enabled)
            {
                StopAllWarnings();
            }
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Показать тестовое предупреждение
        /// </summary>
        [ContextMenu("Test Health Warning")]
        private void TestHealthWarning()
        {
            if (Application.isPlaying)
            {
                StartHealthWarning();
            }
        }

        /// <summary>
        /// Показать тестовое предупреждение голода
        /// </summary>
        [ContextMenu("Test Hunger Warning")]
        private void TestHungerWarning()
        {
            if (Application.isPlaying)
            {
                StartHungerWarning();
            }
        }

        #endregion
    }
}