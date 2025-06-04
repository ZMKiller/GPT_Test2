using System;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Центральный менеджер системы времени дня
    /// </summary>
    public class TimeOfDayManager : MonoBehaviour
    {
        [Header("Настройки времени")]
        [SerializeField] private bool enableTimeFlow = true;
        [SerializeField] private float realSecondsPerGameHour = 60f; // 1 час = 60 секунд
        [SerializeField] private TimeSpeed currentTimeSpeed = TimeSpeed.Normal;
        [SerializeField] private bool pauseTimeAtNight = false; // Пауза времени ночью для облегчения игры
        
        [Header("Начальное время")]
        [SerializeField] private int startHour = 8;
        [SerializeField] private int startMinute = 0;
        [SerializeField] private int startDay = 1;
        [SerializeField] private Season startSeason = Season.Spring;

        [Header("Настройки периодов времени")]
        [SerializeField] private int earlyMorningStart = 5;    // 5:00
        [SerializeField] private int morningStart = 8;         // 8:00
        [SerializeField] private int afternoonStart = 12;      // 12:00
        [SerializeField] private int eveningStart = 17;        // 17:00
        [SerializeField] private int nightStart = 21;          // 21:00
        [SerializeField] private int lateNightStart = 1;       // 1:00

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool allowTimeSkip = true; // Разрешить пропуск времени

        // Текущее состояние времени
        private DateTime currentDateTime;
        private TimePeriod currentTimePeriod;
        private Season currentSeason;
        private float timeAccumulator = 0f;
        private bool timeInitialized = false;

        // Компоненты
        private VisualTimeManager visualTimeManager;
        private WeatherSystem weatherSystem;
        private TimeBasedEvents timeBasedEvents;

        // События
        public event Action<DateTime> OnTimeChanged;
        public event Action<TimePeriod, TimePeriod> OnTimePeriodChanged;
        public event Action<int> OnHourChanged;
        public event Action<int> OnDayChanged;
        public event Action<Season> OnSeasonChanged;
        public event Action<TimeSpeed> OnTimeSpeedChanged;

        // Свойства
        public DateTime CurrentDateTime => currentDateTime;
        public TimePeriod CurrentTimePeriod => currentTimePeriod;
        public Season CurrentSeason => currentSeason;
        public TimeSpeed CurrentTimeSpeed => currentTimeSpeed;
        public int CurrentHour => currentDateTime.Hour;
        public int CurrentMinute => currentDateTime.Minute;
        public int CurrentDay => currentDateTime.Day;
        public bool IsTimeFlowing => enableTimeFlow && currentTimeSpeed != TimeSpeed.Paused;
        public bool IsNight => currentTimePeriod == TimePeriod.Night || currentTimePeriod == TimePeriod.LateNight;
        public bool IsDay => currentTimePeriod == TimePeriod.Morning || currentTimePeriod == TimePeriod.Afternoon;

        #region Unity Methods

        private void Awake()
        {
            // Найти связанные компоненты
            visualTimeManager = FindObjectOfType<VisualTimeManager>();
            weatherSystem = FindObjectOfType<WeatherSystem>();
            timeBasedEvents = FindObjectOfType<TimeBasedEvents>();
        }

        private void Start()
        {
            InitializeTime();
            SubscribeToEvents();
            
            Debug.Log("TimeOfDayManager инициализирован");
        }

        private void Update()
        {
            if (IsTimeFlowing && !Application.isEditor || Application.isPlaying)
            {
                UpdateTime();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация времени
        /// </summary>
        private void InitializeTime()
        {
            // Создать начальную дату и время
            currentDateTime = new DateTime(2024, GetMonthForSeason(startSeason), startDay, startHour, startMinute, 0);
            currentSeason = startSeason;
            currentTimePeriod = GetTimePeriodForHour(currentDateTime.Hour);
            
            timeInitialized = true;

            // Уведомить о начальном времени
            OnTimeChanged?.Invoke(currentDateTime);
            OnTimePeriodChanged?.Invoke(TimePeriod.Morning, currentTimePeriod);
            OnSeasonChanged?.Invoke(currentSeason);

            if (showDebugInfo)
            {
                Debug.Log($"Время инициализировано: {FormatTime()} ({currentTimePeriod}, {currentSeason})");
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            // Подписка на события паузы игры
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        #endregion

        #region Time Update

        /// <summary>
        /// Обновление времени
        /// </summary>
        private void UpdateTime()
        {
            if (!timeInitialized) return;

            // Проверить паузу времени ночью
            if (pauseTimeAtNight && IsNight && currentTimeSpeed == TimeSpeed.Normal)
            {
                return;
            }

            // Накопить время
            float deltaTime = Time.deltaTime * GetTimeSpeedMultiplier();
            timeAccumulator += deltaTime;

            // Если накопилось достаточно времени для одной минуты
            float secondsPerGameMinute = realSecondsPerGameHour / 60f;
            if (timeAccumulator >= secondsPerGameMinute)
            {
                // Добавить минуты
                int minutesToAdd = Mathf.FloorToInt(timeAccumulator / secondsPerGameMinute);
                timeAccumulator -= minutesToAdd * secondsPerGameMinute;

                // Обновить время
                DateTime previousDateTime = currentDateTime;
                int previousHour = currentDateTime.Hour;
                int previousDay = currentDateTime.Day;
                
                currentDateTime = currentDateTime.AddMinutes(minutesToAdd);

                // Проверить изменения
                CheckTimeChanges(previousDateTime, previousHour, previousDay);

                // Уведомить об изменении времени
                OnTimeChanged?.Invoke(currentDateTime);
            }
        }

        /// <summary>
        /// Проверить изменения времени
        /// </summary>
        private void CheckTimeChanges(DateTime previousDateTime, int previousHour, int previousDay)
        {
            // Проверить изменение часа
            if (currentDateTime.Hour != previousHour)
            {
                OnHourChanged?.Invoke(currentDateTime.Hour);
                CheckTimePeriodChange();
                
                if (showDebugInfo)
                {
                    Debug.Log($"Новый час: {currentDateTime.Hour}:00 ({currentTimePeriod})");
                }
            }

            // Проверить изменение дня
            if (currentDateTime.Day != previousDay)
            {
                OnDayChanged?.Invoke(currentDateTime.Day);
                CheckSeasonChange();
                
                if (showDebugInfo)
                {
                    Debug.Log($"Новый день: {currentDateTime.Day} ({currentSeason})");
                }
            }
        }

        /// <summary>
        /// Проверить смену периода времени
        /// </summary>
        private void CheckTimePeriodChange()
        {
            TimePeriod newTimePeriod = GetTimePeriodForHour(currentDateTime.Hour);
            if (newTimePeriod != currentTimePeriod)
            {
                TimePeriod previousPeriod = currentTimePeriod;
                currentTimePeriod = newTimePeriod;
                OnTimePeriodChanged?.Invoke(previousPeriod, currentTimePeriod);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Смена периода времени: {previousPeriod} -> {currentTimePeriod}");
                }
            }
        }

        /// <summary>
        /// Проверить смену сезона
        /// </summary>
        private void CheckSeasonChange()
        {
            Season newSeason = GetSeasonForMonth(currentDateTime.Month);
            if (newSeason != currentSeason)
            {
                currentSeason = newSeason;
                OnSeasonChanged?.Invoke(currentSeason);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Смена сезона: {currentSeason}");
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Установить скорость времени
        /// </summary>
        public void SetTimeSpeed(TimeSpeed speed)
        {
            if (currentTimeSpeed != speed)
            {
                currentTimeSpeed = speed;
                OnTimeSpeedChanged?.Invoke(speed);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Скорость времени изменена: {speed} ({GetTimeSpeedMultiplier()}x)");
                }
            }
        }

        /// <summary>
        /// Переключить паузу времени
        /// </summary>
        public void TogglePause()
        {
            if (currentTimeSpeed == TimeSpeed.Paused)
            {
                SetTimeSpeed(TimeSpeed.Normal);
            }
            else
            {
                SetTimeSpeed(TimeSpeed.Paused);
            }
        }

        /// <summary>
        /// Пропустить время (сон, ожидание)
        /// </summary>
        public void SkipTime(int hours, int minutes = 0)
        {
            if (!allowTimeSkip) return;

            DateTime previousDateTime = currentDateTime;
            int previousHour = currentDateTime.Hour;
            int previousDay = currentDateTime.Day;

            currentDateTime = currentDateTime.AddHours(hours).AddMinutes(minutes);
            
            CheckTimeChanges(previousDateTime, previousHour, previousDay);
            OnTimeChanged?.Invoke(currentDateTime);

            if (showDebugInfo)
            {
                Debug.Log($"Время пропущено на {hours}ч {minutes}м. Текущее время: {FormatTime()}");
            }
        }

        /// <summary>
        /// Установить конкретное время
        /// </summary>
        public void SetTime(int hour, int minute = 0)
        {
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59) return;

            DateTime newDateTime = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, hour, minute, 0);
            
            // Если новое время меньше текущего, переходим на следующий день
            if (newDateTime <= currentDateTime)
            {
                newDateTime = newDateTime.AddDays(1);
            }

            DateTime previousDateTime = currentDateTime;
            int previousHour = currentDateTime.Hour;
            int previousDay = currentDateTime.Day;

            currentDateTime = newDateTime;
            
            CheckTimeChanges(previousDateTime, previousHour, previousDay);
            OnTimeChanged?.Invoke(currentDateTime);

            if (showDebugInfo)
            {
                Debug.Log($"Время установлено: {FormatTime()}");
            }
        }

        /// <summary>
        /// Включить/выключить течение времени
        /// </summary>
        public void SetTimeFlowEnabled(bool enabled)
        {
            enableTimeFlow = enabled;
            
            if (showDebugInfo)
            {
                Debug.Log($"Течение времени: {(enabled ? "включено" : "выключено")}");
            }
        }

        /// <summary>
        /// Получить время до следующего периода
        /// </summary>
        public TimeSpan GetTimeToNextPeriod()
        {
            int nextPeriodHour = GetNextPeriodStartHour();
            DateTime nextPeriodTime = new DateTime(currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, nextPeriodHour, 0, 0);
            
            if (nextPeriodTime <= currentDateTime)
            {
                nextPeriodTime = nextPeriodTime.AddDays(1);
            }

            return nextPeriodTime - currentDateTime;
        }

        /// <summary>
        /// Проверить является ли время рабочим
        /// </summary>
        public bool IsWorkingHours()
        {
            return currentTimePeriod == TimePeriod.Morning || currentTimePeriod == TimePeriod.Afternoon;
        }

        /// <summary>
        /// Проверить доступность по времени
        /// </summary>
        public bool IsTimeAvailable(TimePeriod requiredPeriod)
        {
            return currentTimePeriod == requiredPeriod;
        }

        /// <summary>
        /// Проверить доступность по времени (несколько периодов)
        /// </summary>
        public bool IsTimeAvailable(params TimePeriod[] availablePeriods)
        {
            foreach (var period in availablePeriods)
            {
                if (currentTimePeriod == period)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Получить форматированное время
        /// </summary>
        public string FormatTime(bool includeSeconds = false)
        {
            if (includeSeconds)
            {
                return currentDateTime.ToString("HH:mm:ss");
            }
            else
            {
                return currentDateTime.ToString("HH:mm");
            }
        }

        /// <summary>
        /// Получить форматированную дату
        /// </summary>
        public string FormatDate()
        {
            return currentDateTime.ToString("dd.MM.yyyy");
        }

        /// <summary>
        /// Получить полную информацию о времени
        /// </summary>
        public string GetTimeInfo()
        {
            return $"{FormatTime()} {FormatDate()} ({GetTimePeriodName(currentTimePeriod)}, {GetSeasonName(currentSeason)})";
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public TimeOfDayManagerSaveData GetSaveData()
        {
            return new TimeOfDayManagerSaveData
            {
                currentDateTime = currentDateTime.ToBinary(),
                currentSeason = currentSeason,
                timeSpeed = currentTimeSpeed,
                enableTimeFlow = enableTimeFlow,
                realSecondsPerGameHour = realSecondsPerGameHour
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(TimeOfDayManagerSaveData data)
        {
            currentDateTime = DateTime.FromBinary(data.currentDateTime);
            currentSeason = data.currentSeason;
            currentTimeSpeed = data.timeSpeed;
            enableTimeFlow = data.enableTimeFlow;
            realSecondsPerGameHour = data.realSecondsPerGameHour;

            currentTimePeriod = GetTimePeriodForHour(currentDateTime.Hour);
            timeInitialized = true;

            // Уведомить о загруженном времени
            OnTimeChanged?.Invoke(currentDateTime);
            OnTimePeriodChanged?.Invoke(currentTimePeriod, currentTimePeriod);
            OnSeasonChanged?.Invoke(currentSeason);
            OnTimeSpeedChanged?.Invoke(currentTimeSpeed);

            if (showDebugInfo)
            {
                Debug.Log($"Время загружено: {GetTimeInfo()}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Получить множитель скорости времени
        /// </summary>
        private float GetTimeSpeedMultiplier()
        {
            switch (currentTimeSpeed)
            {
                case TimeSpeed.Paused: return 0f;
                case TimeSpeed.Normal: return 1f;
                case TimeSpeed.Fast: return 2f;
                case TimeSpeed.VeryFast: return 4f;
                case TimeSpeed.UltraFast: return 8f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Получить период времени для часа
        /// </summary>
        private TimePeriod GetTimePeriodForHour(int hour)
        {
            if (hour >= lateNightStart && hour < earlyMorningStart)
                return TimePeriod.LateNight;
            else if (hour >= earlyMorningStart && hour < morningStart)
                return TimePeriod.EarlyMorning;
            else if (hour >= morningStart && hour < afternoonStart)
                return TimePeriod.Morning;
            else if (hour >= afternoonStart && hour < eveningStart)
                return TimePeriod.Afternoon;
            else if (hour >= eveningStart && hour < nightStart)
                return TimePeriod.Evening;
            else
                return TimePeriod.Night;
        }

        /// <summary>
        /// Получить начальный час следующего периода
        /// </summary>
        private int GetNextPeriodStartHour()
        {
            switch (currentTimePeriod)
            {
                case TimePeriod.LateNight: return earlyMorningStart;
                case TimePeriod.EarlyMorning: return morningStart;
                case TimePeriod.Morning: return afternoonStart;
                case TimePeriod.Afternoon: return eveningStart;
                case TimePeriod.Evening: return nightStart;
                case TimePeriod.Night: return lateNightStart;
                default: return morningStart;
            }
        }

        /// <summary>
        /// Получить сезон для месяца
        /// </summary>
        private Season GetSeasonForMonth(int month)
        {
            switch (month)
            {
                case 12:
                case 1:
                case 2:
                    return Season.Winter;
                case 3:
                case 4:
                case 5:
                    return Season.Spring;
                case 6:
                case 7:
                case 8:
                    return Season.Summer;
                case 9:
                case 10:
                case 11:
                    return Season.Autumn;
                default:
                    return Season.Spring;
            }
        }

        /// <summary>
        /// Получить месяц для сезона
        /// </summary>
        private int GetMonthForSeason(Season season)
        {
            switch (season)
            {
                case Season.Spring: return 3;
                case Season.Summer: return 6;
                case Season.Autumn: return 9;
                case Season.Winter: return 12;
                default: return 3;
            }
        }

        /// <summary>
        /// Получить название периода времени
        /// </summary>
        private string GetTimePeriodName(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning: return "Раннее утро";
                case TimePeriod.Morning: return "Утро";
                case TimePeriod.Afternoon: return "День";
                case TimePeriod.Evening: return "Вечер";
                case TimePeriod.Night: return "Ночь";
                case TimePeriod.LateNight: return "Поздняя ночь";
                default: return period.ToString();
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
        /// Обработчик изменения состояния игры
        /// </summary>
        private void OnGameStateChanged(GameState newState)
        {
            // Автоматически останавливать время на паузе
            if (newState == GameState.Paused)
            {
                if (currentTimeSpeed != TimeSpeed.Paused)
                {
                    SetTimeSpeed(TimeSpeed.Paused);
                }
            }
            else if (newState == GameState.Playing)
            {
                if (currentTimeSpeed == TimeSpeed.Paused)
                {
                    SetTimeSpeed(TimeSpeed.Normal);
                }
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Skip 1 Hour")]
        private void DebugSkip1Hour()
        {
            SkipTime(1);
        }

        [ContextMenu("Skip to Morning")]
        private void DebugSkipToMorning()
        {
            SetTime(morningStart);
        }

        [ContextMenu("Skip to Evening")]
        private void DebugSkipToEvening()
        {
            SetTime(eveningStart);
        }

        [ContextMenu("Set Fast Time")]
        private void DebugSetFastTime()
        {
            SetTimeSpeed(TimeSpeed.Fast);
        }

        [ContextMenu("Toggle Pause")]
        private void DebugTogglePause()
        {
            TogglePause();
        }

        [ContextMenu("Show Time Info")]
        private void DebugShowTimeInfo()
        {
            Debug.Log($"Время: {GetTimeInfo()}");
            Debug.Log($"До следующего периода: {GetTimeToNextPeriod()}");
            Debug.Log($"Рабочее время: {IsWorkingHours()}");
        }

        #endregion
    }

    /// <summary>
    /// Данные TimeOfDayManager для сохранения
    /// </summary>
}
