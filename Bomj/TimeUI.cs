using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HomelessToMillionaire
{
    /// <summary>
    /// UI интерфейс системы времени
    /// </summary>
    public class TimeUI : MonoBehaviour
    {
        [Header("Основные элементы UI")]
        [SerializeField] private GameObject timePanel;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI dateText;
        [SerializeField] private TextMeshProUGUI dayOfWeekText;
        [SerializeField] private TextMeshProUGUI seasonText;

        [Header("Индикатор периода дня")]
        [SerializeField] private GameObject timePeriodPanel;
        [SerializeField] private TextMeshProUGUI timePeriodText;
        [SerializeField] private Image timePeriodIcon;
        [SerializeField] private Slider timeProgressSlider;
        [SerializeField] private TextMeshProUGUI timeProgressText;

        [Header("Управление временем")]
        [SerializeField] private GameObject timeControlPanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button normalSpeedButton;
        [SerializeField] private Button fastSpeedButton;
        [SerializeField] private Button veryFastSpeedButton;
        [SerializeField] private TextMeshProUGUI currentSpeedText;

        [Header("Информация о погоде")]
        [SerializeField] private GameObject weatherPanel;
        [SerializeField] private TextMeshProUGUI weatherText;
        [SerializeField] private Image weatherIcon;
        [SerializeField] private TextMeshProUGUI temperatureText;

        [Header("Система сна")]
        [SerializeField] private GameObject sleepPanel;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Slider fatigueSlider;
        [SerializeField] private TextMeshProUGUI fatigueText;
        [SerializeField] private Image fatigueIcon;

        [Header("Уведомления времени")]
        [SerializeField] private GameObject timeNotificationPanel;
        [SerializeField] private TextMeshProUGUI timeNotificationText;
        [SerializeField] private float notificationDuration = 3f;

        [Header("Иконки периодов времени")]
        [SerializeField] private Sprite morningIcon;
        [SerializeField] private Sprite dayIcon;
        [SerializeField] private Sprite eveningIcon;
        [SerializeField] private Sprite nightIcon;

        [Header("Иконки погоды")]
        [SerializeField] private Sprite clearWeatherIcon;
        [SerializeField] private Sprite cloudyWeatherIcon;
        [SerializeField] private Sprite rainyWeatherIcon;
        [SerializeField] private Sprite snowWeatherIcon;
        [SerializeField] private Sprite fogWeatherIcon;
        [SerializeField] private Sprite stormWeatherIcon;

        [Header("Иконки усталости")]
        [SerializeField] private Sprite restedIcon;
        [SerializeField] private Sprite tiredIcon;
        [SerializeField] private Sprite exhaustedIcon;

        [Header("Цвета времени дня")]
        [SerializeField] private Color morningColor = new Color(1f, 0.8f, 0.6f);
        [SerializeField] private Color dayColor = new Color(0.9f, 0.9f, 1f);
        [SerializeField] private Color eveningColor = new Color(1f, 0.7f, 0.5f);
        [SerializeField] private Color nightColor = new Color(0.6f, 0.6f, 0.9f);

        [Header("Настройки")]
        [SerializeField] private bool showTimeControls = true;
        [SerializeField] private bool showWeatherInfo = true;
        [SerializeField] private bool showSleepInfo = true;
        [SerializeField] private bool autoHideNotifications = true;
        [SerializeField] private bool use24HourFormat = true;

        // Компоненты
        private TimeOfDayManager timeOfDayManager;
        private WeatherSystem weatherSystem;
        private SleepSystem sleepSystem;
        private UIManager uiManager;

        // Состояние UI
        private TimePeriod currentTimePeriod;
        private WeatherType currentWeather;
        private FatigueLevel currentFatigueLevel;
        private bool isTimeControlsVisible = true;
        private Coroutine notificationCoroutine;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            weatherSystem = FindObjectOfType<WeatherSystem>();
            sleepSystem = FindObjectOfType<SleepSystem>();
            uiManager = FindObjectOfType<UIManager>();
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            UpdateAllTimeUI();

            Debug.Log("TimeUI инициализирован");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация UI
        /// </summary>
        private void InitializeUI()
        {
            // Настроить кнопки управления временем
            SetupTimeControlButtons();

            // Настроить кнопку сна
            SetupSleepButton();

            // Настроить видимость панелей
            SetupPanelVisibility();

            // Настроить начальные значения
            SetupInitialValues();
        }

        /// <summary>
        /// Настроить кнопки управления временем
        /// </summary>
        private void SetupTimeControlButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(() => SetTimeSpeed(TimeSpeed.Paused));
            }

            if (normalSpeedButton != null)
            {
                normalSpeedButton.onClick.AddListener(() => SetTimeSpeed(TimeSpeed.Normal));
            }

            if (fastSpeedButton != null)
            {
                fastSpeedButton.onClick.AddListener(() => SetTimeSpeed(TimeSpeed.Fast));
            }

            if (veryFastSpeedButton != null)
            {
                veryFastSpeedButton.onClick.AddListener(() => SetTimeSpeed(TimeSpeed.VeryFast));
            }
        }

        /// <summary>
        /// Настроить кнопку сна
        /// </summary>
        private void SetupSleepButton()
        {
            if (sleepButton != null)
            {
                sleepButton.onClick.AddListener(OnSleepButtonClicked);
            }
        }

        /// <summary>
        /// Настроить видимость панелей
        /// </summary>
        private void SetupPanelVisibility()
        {
            if (timeControlPanel != null)
            {
                timeControlPanel.SetActive(showTimeControls);
            }

            if (weatherPanel != null)
            {
                weatherPanel.SetActive(showWeatherInfo);
            }

            if (sleepPanel != null)
            {
                sleepPanel.SetActive(showSleepInfo);
            }

            if (timeNotificationPanel != null)
            {
                timeNotificationPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Настроить начальные значения
        /// </summary>
        private void SetupInitialValues()
        {
            // Установить начальные значения слайдеров
            if (timeProgressSlider != null)
            {
                timeProgressSlider.minValue = 0f;
                timeProgressSlider.maxValue = 1f;
                timeProgressSlider.interactable = false;
            }

            if (fatigueSlider != null)
            {
                fatigueSlider.minValue = 0f;
                fatigueSlider.maxValue = 100f;
                fatigueSlider.interactable = false;
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimeChanged += OnTimeChanged;
                timeOfDayManager.OnTimePeriodChanged += OnTimePeriodChanged;
                timeOfDayManager.OnDayChanged += OnDayChanged;
                timeOfDayManager.OnSeasonChanged += OnSeasonChanged;
                timeOfDayManager.OnTimeSpeedChanged += OnTimeSpeedChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged += OnWeatherChanged;
            }

            if (sleepSystem != null)
            {
                sleepSystem.OnFatigueChanged += OnFatigueChanged;
                sleepSystem.OnFatigueLevelChanged += OnFatigueLevelChanged;
                sleepSystem.OnSleepStarted += OnSleepStarted;
                sleepSystem.OnSleepCompleted += OnSleepCompleted;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimeChanged -= OnTimeChanged;
                timeOfDayManager.OnTimePeriodChanged -= OnTimePeriodChanged;
                timeOfDayManager.OnDayChanged -= OnDayChanged;
                timeOfDayManager.OnSeasonChanged -= OnSeasonChanged;
                timeOfDayManager.OnTimeSpeedChanged -= OnTimeSpeedChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged -= OnWeatherChanged;
            }

            if (sleepSystem != null)
            {
                sleepSystem.OnFatigueChanged -= OnFatigueChanged;
                sleepSystem.OnFatigueLevelChanged -= OnFatigueLevelChanged;
                sleepSystem.OnSleepStarted -= OnSleepStarted;
                sleepSystem.OnSleepCompleted -= OnSleepCompleted;
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// Обновить весь UI времени
        /// </summary>
        private void UpdateAllTimeUI()
        {
            UpdateTimeDisplay();
            UpdateTimePeriodDisplay();
            UpdateWeatherDisplay();
            UpdateSleepDisplay();
            UpdateTimeControlsDisplay();
        }

        /// <summary>
        /// Обновить отображение времени
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (timeOfDayManager == null) return;

            DateTime currentTime = timeOfDayManager.CurrentDateTime;

            // Время
            if (timeText != null)
            {
                string timeFormat = use24HourFormat ? "HH:mm" : "h:mm tt";
                timeText.text = currentTime.ToString(timeFormat);
            }

            // Дата
            if (dateText != null)
            {
                dateText.text = currentTime.ToString("dd.MM.yyyy");
            }

            // День недели
            if (dayOfWeekText != null)
            {
                dayOfWeekText.text = GetDayOfWeekName(currentTime.DayOfWeek);
            }

            // Сезон
            if (seasonText != null)
            {
                seasonText.text = GetSeasonName(timeOfDayManager.CurrentSeason);
            }
        }

        /// <summary>
        /// Обновить отображение периода дня
        /// </summary>
        private void UpdateTimePeriodDisplay()
        {
            if (timeOfDayManager == null) return;

            TimePeriod period = timeOfDayManager.CurrentTimePeriod;
            currentTimePeriod = period;

            // Название периода
            if (timePeriodText != null)
            {
                timePeriodText.text = GetTimePeriodName(period);
                timePeriodText.color = GetTimePeriodColor(period);
            }

            // Иконка периода
            if (timePeriodIcon != null)
            {
                timePeriodIcon.sprite = GetTimePeriodIcon(period);
                timePeriodIcon.color = GetTimePeriodColor(period);
            }

            // Прогресс времени
            UpdateTimeProgress();
        }

        /// <summary>
        /// Обновить прогресс времени
        /// </summary>
        private void UpdateTimeProgress()
        {
            if (timeOfDayManager == null || timeProgressSlider == null) return;

            DateTime currentTime = timeOfDayManager.CurrentDateTime;
            
            // Вычислить прогресс дня (0-1)
            float dayProgress = (currentTime.Hour + currentTime.Minute / 60f) / 24f;
            
            timeProgressSlider.value = dayProgress;

            if (timeProgressText != null)
            {
                timeProgressText.text = $"{dayProgress:P0}";
            }
        }

        /// <summary>
        /// Обновить отображение погоды
        /// </summary>
        private void UpdateWeatherDisplay()
        {
            if (!showWeatherInfo || weatherSystem == null) return;

            currentWeather = weatherSystem.CurrentWeather;

            // Название погоды
            if (weatherText != null)
            {
                weatherText.text = weatherSystem.GetWeatherDescription();
            }

            // Иконка погоды
            if (weatherIcon != null)
            {
                weatherIcon.sprite = GetWeatherIcon(currentWeather);
            }

            // Температура (условная)
            if (temperatureText != null)
            {
                int temperature = GetEstimatedTemperature();
                temperatureText.text = $"{temperature}°C";
            }
        }

        /// <summary>
        /// Обновить отображение сна
        /// </summary>
        private void UpdateSleepDisplay()
        {
            if (!showSleepInfo || sleepSystem == null) return;

            // Уровень усталости
            if (fatigueSlider != null)
            {
                fatigueSlider.value = sleepSystem.CurrentFatigue;
            }

            if (fatigueText != null)
            {
                fatigueText.text = $"{sleepSystem.FatiguePercentage:P0} - {sleepSystem.GetFatigueLevelName()}";
                fatigueText.color = GetFatigueColor(sleepSystem.CurrentFatigueLevel);
            }

            // Иконка усталости
            if (fatigueIcon != null)
            {
                fatigueIcon.sprite = GetFatigueIcon(sleepSystem.CurrentFatigueLevel);
                fatigueIcon.color = GetFatigueColor(sleepSystem.CurrentFatigueLevel);
            }

            // Кнопка сна
            if (sleepButton != null)
            {
                sleepButton.interactable = !sleepSystem.IsSleeping;
                var buttonText = sleepButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = sleepSystem.IsSleeping ? "Спит..." : "Спать";
                }
            }
        }

        /// <summary>
        /// Обновить отображение управления временем
        /// </summary>
        private void UpdateTimeControlsDisplay()
        {
            if (!showTimeControls || timeOfDayManager == null) return;

            TimeSpeed currentSpeed = timeOfDayManager.CurrentTimeSpeed;

            // Подсветить активную кнопку
            SetTimeControlButtonHighlight(pauseButton, currentSpeed == TimeSpeed.Paused);
            SetTimeControlButtonHighlight(normalSpeedButton, currentSpeed == TimeSpeed.Normal);
            SetTimeControlButtonHighlight(fastSpeedButton, currentSpeed == TimeSpeed.Fast);
            SetTimeControlButtonHighlight(veryFastSpeedButton, currentSpeed == TimeSpeed.VeryFast);

            // Текст текущей скорости
            if (currentSpeedText != null)
            {
                currentSpeedText.text = GetTimeSpeedName(currentSpeed);
            }
        }

        #endregion

        #region UI Actions

        /// <summary>
        /// Установить скорость времени
        /// </summary>
        private void SetTimeSpeed(TimeSpeed speed)
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.SetTimeSpeed(speed);
            }
        }

        /// <summary>
        /// Обработчик нажатия кнопки сна
        /// </summary>
        private void OnSleepButtonClicked()
        {
            if (sleepSystem == null) return;

            if (sleepSystem.IsSleeping)
            {
                sleepSystem.WakeUp();
            }
            else
            {
                // Показать меню выбора места для сна
                ShowSleepLocationMenu();
            }
        }

        /// <summary>
        /// Показать меню выбора места для сна
        /// </summary>
        private void ShowSleepLocationMenu()
        {
            if (uiManager != null)
            {
                // Здесь можно открыть специальное меню выбора места для сна
                // Пока используем лучшее доступное место
                SleepLocation bestLocation = sleepSystem.GetBestAvailableSleepLocation();
                sleepSystem.StartSleep(bestLocation, 8f);
            }
        }

        /// <summary>
        /// Показать уведомление времени
        /// </summary>
        public void ShowTimeNotification(string message, float duration = 0f)
        {
            if (timeNotificationPanel == null || timeNotificationText == null) return;

            timeNotificationText.text = message;
            timeNotificationPanel.SetActive(true);

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }

            float displayDuration = duration > 0f ? duration : notificationDuration;
            if (autoHideNotifications)
            {
                notificationCoroutine = StartCoroutine(HideNotificationCoroutine(displayDuration));
            }
        }

        /// <summary>
        /// Скрыть уведомление времени
        /// </summary>
        public void HideTimeNotification()
        {
            if (timeNotificationPanel != null)
            {
                timeNotificationPanel.SetActive(false);
            }

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
                notificationCoroutine = null;
            }
        }

        /// <summary>
        /// Корутина скрытия уведомления
        /// </summary>
        private IEnumerator HideNotificationCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideTimeNotification();
        }

        /// <summary>
        /// Переключить видимость управления временем
        /// </summary>
        public void ToggleTimeControls()
        {
            isTimeControlsVisible = !isTimeControlsVisible;
            if (timeControlPanel != null)
            {
                timeControlPanel.SetActive(isTimeControlsVisible && showTimeControls);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Получить название дня недели
        /// </summary>
        private string GetDayOfWeekName(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday: return "Понедельник";
                case DayOfWeek.Tuesday: return "Вторник";
                case DayOfWeek.Wednesday: return "Среда";
                case DayOfWeek.Thursday: return "Четверг";
                case DayOfWeek.Friday: return "Пятница";
                case DayOfWeek.Saturday: return "Суббота";
                case DayOfWeek.Sunday: return "Воскресенье";
                default: return dayOfWeek.ToString();
            }
        }

        /// <summary>
        /// Получить название сезона
        /// </summary>
        private string GetSeasonName(Season season)
        {
            switch (season)
            {
                case Season.Spring: return "Весна";
                case Season.Summer: return "Лето";
                case Season.Autumn: return "Осень";
                case Season.Winter: return "Зима";
                default: return season.ToString();
            }
        }

        /// <summary>
        /// Получить название периода дня
        /// </summary>
        private string GetTimePeriodName(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning: return "Раннее утро";
                case TimePeriod.Morning: return "Утро";
                case TimePeriod.Day: return "День";
                case TimePeriod.Afternoon: return "После обеда";
                case TimePeriod.Evening: return "Вечер";
                case TimePeriod.Night: return "Ночь";
                case TimePeriod.LateNight: return "Поздняя ночь";
                default: return period.ToString();
            }
        }

        /// <summary>
        /// Получить название скорости времени
        /// </summary>
        private string GetTimeSpeedName(TimeSpeed speed)
        {
            switch (speed)
            {
                case TimeSpeed.Paused: return "Пауза";
                case TimeSpeed.Normal: return "1x";
                case TimeSpeed.Fast: return "2x";
                case TimeSpeed.VeryFast: return "4x";
                default: return speed.ToString();
            }
        }

        /// <summary>
        /// Получить цвет периода дня
        /// </summary>
        private Color GetTimePeriodColor(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning:
                case TimePeriod.Morning:
                    return morningColor;
                case TimePeriod.Day:
                case TimePeriod.Afternoon:
                    return dayColor;
                case TimePeriod.Evening:
                    return eveningColor;
                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    return nightColor;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Получить иконку периода дня
        /// </summary>
        private Sprite GetTimePeriodIcon(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning:
                case TimePeriod.Morning:
                    return morningIcon;
                case TimePeriod.Day:
                case TimePeriod.Afternoon:
                    return dayIcon;
                case TimePeriod.Evening:
                    return eveningIcon;
                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    return nightIcon;
                default:
                    return dayIcon;
            }
        }

        /// <summary>
        /// Получить иконку погоды
        /// </summary>
        private Sprite GetWeatherIcon(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear: return clearWeatherIcon;
                case WeatherType.Cloudy: return cloudyWeatherIcon;
                case WeatherType.Rainy:
                case WeatherType.Heavy_Rain: return rainyWeatherIcon;
                case WeatherType.Snow: return snowWeatherIcon;
                case WeatherType.Fog: return fogWeatherIcon;
                case WeatherType.Storm: return stormWeatherIcon;
                default: return clearWeatherIcon;
            }
        }

        /// <summary>
        /// Получить иконку усталости
        /// </summary>
        private Sprite GetFatigueIcon(FatigueLevel fatigueLevel)
        {
            switch (fatigueLevel)
            {
                case FatigueLevel.Rested:
                case FatigueLevel.Slightly_Tired:
                    return restedIcon;
                case FatigueLevel.Tired:
                case FatigueLevel.Very_Tired:
                    return tiredIcon;
                case FatigueLevel.Exhausted:
                    return exhaustedIcon;
                default:
                    return restedIcon;
            }
        }

        /// <summary>
        /// Получить цвет усталости
        /// </summary>
        private Color GetFatigueColor(FatigueLevel fatigueLevel)
        {
            switch (fatigueLevel)
            {
                case FatigueLevel.Rested: return Color.green;
                case FatigueLevel.Slightly_Tired: return Color.yellow;
                case FatigueLevel.Tired: return Color.yellow;
                case FatigueLevel.Very_Tired: return Color.red;
                case FatigueLevel.Exhausted: return Color.red;
                default: return Color.white;
            }
        }

        /// <summary>
        /// Получить примерную температуру
        /// </summary>
        private int GetEstimatedTemperature()
        {
            int baseTemp = 20; // Базовая температура

            // Изменения от сезона
            if (timeOfDayManager != null)
            {
                switch (timeOfDayManager.CurrentSeason)
                {
                    case Season.Spring: baseTemp += UnityEngine.Random.Range(-5, 10); break;
                    case Season.Summer: baseTemp += UnityEngine.Random.Range(5, 15); break;
                    case Season.Autumn: baseTemp += UnityEngine.Random.Range(-10, 5); break;
                    case Season.Winter: baseTemp += UnityEngine.Random.Range(-20, -5); break;
                }

                // Изменения от времени дня
                switch (timeOfDayManager.CurrentTimePeriod)
                {
                    case TimePeriod.EarlyMorning:
                    case TimePeriod.Night:
                    case TimePeriod.LateNight:
                        baseTemp -= 5;
                        break;
                    case TimePeriod.Day:
                    case TimePeriod.Afternoon:
                        baseTemp += 5;
                        break;
                }
            }

            // Изменения от погоды
            if (weatherSystem != null)
            {
                switch (weatherSystem.CurrentWeather)
                {
                    case WeatherType.Snow: baseTemp -= 10; break;
                    case WeatherType.Rainy:
                    case WeatherType.Heavy_Rain: baseTemp -= 3; break;
                    case WeatherType.Storm: baseTemp -= 5; break;
                    case WeatherType.Fog: baseTemp -= 2; break;
                }
            }

            return baseTemp;
        }

        /// <summary>
        /// Установить подсветку кнопки управления временем
        /// </summary>
        private void SetTimeControlButtonHighlight(Button button, bool highlighted)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = highlighted ? Color.yellow : Color.white;
            button.colors = colors;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик изменения времени
        /// </summary>
        private void OnTimeChanged(DateTime newTime)
        {
            UpdateTimeDisplay();
            UpdateTimeProgress();
        }

        /// <summary>
        /// Обработчик смены периода времени
        /// </summary>
        private void OnTimePeriodChanged(TimePeriod oldPeriod, TimePeriod newPeriod)
        {
            UpdateTimePeriodDisplay();
            
            string message = $"Наступил период: {GetTimePeriodName(newPeriod)}";
            ShowTimeNotification(message);
        }

        /// <summary>
        /// Обработчик смены дня
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            UpdateTimeDisplay();
            ShowTimeNotification($"Новый день: {newDay}");
        }

        /// <summary>
        /// Обработчик смены сезона
        /// </summary>
        private void OnSeasonChanged(Season newSeason)
        {
            UpdateTimeDisplay();
            ShowTimeNotification($"Наступил сезон: {GetSeasonName(newSeason)}");
        }

        /// <summary>
        /// Обработчик изменения скорости времени
        /// </summary>
        private void OnTimeSpeedChanged(TimeSpeed newSpeed)
        {
            UpdateTimeControlsDisplay();
        }

        /// <summary>
        /// Обработчик изменения погоды
        /// </summary>
        private void OnWeatherChanged(WeatherType weather, WeatherIntensity intensity)
        {
            UpdateWeatherDisplay();
            
            if (weatherSystem != null)
            {
                ShowTimeNotification($"Погода: {weatherSystem.GetWeatherDescription()}");
            }
        }

        /// <summary>
        /// Обработчик изменения усталости
        /// </summary>
        private void OnFatigueChanged(float newFatigue)
        {
            UpdateSleepDisplay();
        }

        /// <summary>
        /// Обработчик изменения уровня усталости
        /// </summary>
        private void OnFatigueLevelChanged(FatigueLevel newLevel)
        {
            currentFatigueLevel = newLevel;
            UpdateSleepDisplay();

            if (newLevel == FatigueLevel.Exhausted)
            {
                ShowTimeNotification("Вы истощены! Нужно отдохнуть.", 5f);
            }
            else if (newLevel == FatigueLevel.Very_Tired)
            {
                ShowTimeNotification("Вы очень устали. Рекомендуется отдых.");
            }
        }

        /// <summary>
        /// Обработчик начала сна
        /// </summary>
        private void OnSleepStarted(SleepLocation location, float hours)
        {
            UpdateSleepDisplay();
            ShowTimeNotification($"Начинаем спать на {hours:F0} часов");
        }

        /// <summary>
        /// Обработчик завершения сна
        /// </summary>
        private void OnSleepCompleted(SleepLocation location, float hours)
        {
            UpdateSleepDisplay();
            ShowTimeNotification($"Проснулись после {hours:F0} часов сна");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Показать/скрыть панель времени
        /// </summary>
        public void SetTimePanelVisible(bool visible)
        {
            if (timePanel != null)
            {
                timePanel.SetActive(visible);
            }
        }

        /// <summary>
        /// Показать/скрыть управление временем
        /// </summary>
        public void SetTimeControlsVisible(bool visible)
        {
            showTimeControls = visible;
            if (timeControlPanel != null)
            {
                timeControlPanel.SetActive(visible && isTimeControlsVisible);
            }
        }

        /// <summary>
        /// Показать/скрыть информацию о погоде
        /// </summary>
        public void SetWeatherInfoVisible(bool visible)
        {
            showWeatherInfo = visible;
            if (weatherPanel != null)
            {
                weatherPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// Показать/скрыть информацию о сне
        /// </summary>
        public void SetSleepInfoVisible(bool visible)
        {
            showSleepInfo = visible;
            if (sleepPanel != null)
            {
                sleepPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// Установить 24-часовой формат времени
        /// </summary>
        public void Set24HourFormat(bool use24Hour)
        {
            use24HourFormat = use24Hour;
            UpdateTimeDisplay();
        }

        #endregion
    }
}
