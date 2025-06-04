using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система влияния времени на геймплей
    /// </summary>
    public class TimeBasedGameplay : MonoBehaviour
    {
        [Header("Настройки системы")]
        [SerializeField] private bool enableTimeBasedGameplay = true;
        [SerializeField] private bool enableDynamicPricing = true;
        [SerializeField] private bool enableTimeBasedNPCs = true;
        [SerializeField] private bool enableLocationSchedules = true;
        [SerializeField] private bool enableWorkEfficiency = true;

        [Header("Модификаторы по времени дня")]
        [SerializeField] private TimeBasedModifiers morningModifiers = new TimeBasedModifiers();
        [SerializeField] private TimeBasedModifiers dayModifiers = new TimeBasedModifiers();
        [SerializeField] private TimeBasedModifiers eveningModifiers = new TimeBasedModifiers();
        [SerializeField] private TimeBasedModifiers nightModifiers = new TimeBasedModifiers();

        [Header("Динамическое ценообразование")]
        [SerializeField] private float[] hourlyPriceMultipliers = new float[24]; // Множители цен по часам
        [SerializeField] private float maxPriceVariation = 0.3f; // Максимальное изменение цены ±30%

        [Header("Расписания локаций")]
        [SerializeField] private LocationSchedule[] locationSchedules;

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;

        // Компоненты
        private TimeOfDayManager timeOfDayManager;
        private PlayerStats playerStats;
        private ShopSystem shopSystem;
        private JobSystem jobSystem;
        private EducationSystem educationSystem;
        private NPCManager npcManager;
        private LocationManager locationManager;

        // Текущее состояние
        private TimePeriod currentTimePeriod;
        private TimeBasedModifiers currentModifiers;
        private Dictionary<Location, bool> locationAvailability = new Dictionary<Location, bool>();
        private bool modifiersApplied = false;

        // События
        public event Action<TimePeriod, TimeBasedModifiers> OnTimeBasedModifiersChanged;
        public event Action<Location, bool> OnLocationAvailabilityChanged;
        public event Action<float> OnPriceMultiplierChanged;

        // Свойства
        public TimeBasedModifiers CurrentModifiers => currentModifiers;
        public float CurrentPriceMultiplier => GetCurrentPriceMultiplier();
        public bool IsWorkTime => IsTimeForWork();
        public bool IsEducationTime => IsTimeForEducation();
        public bool IsBusinessTime => IsTimeForBusiness();

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            playerStats = FindObjectOfType<PlayerStats>();
            shopSystem = FindObjectOfType<ShopSystem>();
            jobSystem = FindObjectOfType<JobSystem>();
            educationSystem = FindObjectOfType<EducationSystem>();
            npcManager = FindObjectOfType<NPCManager>();
            locationManager = FindObjectOfType<LocationManager>();

            // Инициализация данных по умолчанию
            InitializeDefaultData();
        }

        private void Start()
        {
            if (enableTimeBasedGameplay)
            {
                SubscribeToEvents();
                UpdateTimeBasedEffects();
            }

            Debug.Log("TimeBasedGameplay инициализирован");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация данных по умолчанию
        /// </summary>
        private void InitializeDefaultData()
        {
            // Инициализация модификаторов времени дня
            InitializeTimeModifiers();

            // Инициализация множителей цен по часам
            InitializeHourlyPriceMultipliers();

            // Инициализация расписаний локаций
            InitializeLocationSchedules();
        }

        /// <summary>
        /// Инициализация модификаторов времени
        /// </summary>
        private void InitializeTimeModifiers()
        {
            // Утро (6-12) - хорошо для физической активности
            morningModifiers.workEfficiencyMultiplier = 1.1f;
            morningModifiers.educationEfficiencyMultiplier = 1.0f;
            morningModifiers.earningsMultiplier = 0.9f;
            morningModifiers.moodChangeMultiplier = 1.1f;
            morningModifiers.healthChangeMultiplier = 1.2f;
            morningModifiers.hungerRateMultiplier = 1.1f;

            // День (12-18) - время основной активности
            dayModifiers.workEfficiencyMultiplier = 1.2f;
            dayModifiers.educationEfficiencyMultiplier = 1.1f;
            dayModifiers.earningsMultiplier = 1.1f;
            dayModifiers.moodChangeMultiplier = 1.0f;
            dayModifiers.healthChangeMultiplier = 1.0f;
            dayModifiers.hungerRateMultiplier = 1.0f;

            // Вечер (18-24) - время развлечений и отдыха
            eveningModifiers.workEfficiencyMultiplier = 0.9f;
            eveningModifiers.educationEfficiencyMultiplier = 0.8f;
            eveningModifiers.earningsMultiplier = 1.0f;
            eveningModifiers.moodChangeMultiplier = 1.1f;
            eveningModifiers.healthChangeMultiplier = 0.9f;
            eveningModifiers.hungerRateMultiplier = 0.9f;

            // Ночь (0-6) - время отдыха
            nightModifiers.workEfficiencyMultiplier = 0.7f;
            nightModifiers.educationEfficiencyMultiplier = 0.6f;
            nightModifiers.earningsMultiplier = 0.8f;
            nightModifiers.moodChangeMultiplier = 0.8f;
            nightModifiers.healthChangeMultiplier = 0.8f;
            nightModifiers.hungerRateMultiplier = 0.8f;
        }

        /// <summary>
        /// Инициализация множителей цен по часам
        /// </summary>
        private void InitializeHourlyPriceMultipliers()
        {
            if (hourlyPriceMultipliers == null || hourlyPriceMultipliers.Length != 24)
            {
                hourlyPriceMultipliers = new float[24];
            }

            // Установить базовые множители (реалистичные для разного времени дня)
            for (int hour = 0; hour < 24; hour++)
            {
                if (hour >= 6 && hour <= 9) // Утренний пик
                {
                    hourlyPriceMultipliers[hour] = 1.1f;
                }
                else if (hour >= 12 && hour <= 14) // Обеденное время
                {
                    hourlyPriceMultipliers[hour] = 1.15f;
                }
                else if (hour >= 18 && hour <= 21) // Вечерний пик
                {
                    hourlyPriceMultipliers[hour] = 1.2f;
                }
                else if (hour >= 22 || hour <= 5) // Ночное время
                {
                    hourlyPriceMultipliers[hour] = 0.9f;
                }
                else // Обычное время
                {
                    hourlyPriceMultipliers[hour] = 1.0f;
                }
            }
        }

        /// <summary>
        /// Инициализация расписаний локаций
        /// </summary>
        private void InitializeLocationSchedules()
        {
            if (locationSchedules == null || locationSchedules.Length == 0)
            {
                locationSchedules = new LocationSchedule[]
                {
                    new LocationSchedule
                    {
                        location = Location.JobCenter,
                        openHour = 8,
                        closeHour = 18,
                        availableOnWeekends = false,
                        description = "Центр занятости"
                    },
                    new LocationSchedule
                    {
                        location = Location.Library,
                        openHour = 9,
                        closeHour = 21,
                        availableOnWeekends = true,
                        description = "Библиотека"
                    },
                    new LocationSchedule
                    {
                        location = Location.University,
                        openHour = 8,
                        closeHour = 20,
                        availableOnWeekends = false,
                        description = "Университет"
                    },
                    new LocationSchedule
                    {
                        location = Location.Shop,
                        openHour = 8,
                        closeHour = 22,
                        availableOnWeekends = true,
                        description = "Магазин"
                    },
                    new LocationSchedule
                    {
                        location = Location.Hospital,
                        openHour = 0,
                        closeHour = 24,
                        availableOnWeekends = true,
                        description = "Больница"
                    },
                    new LocationSchedule
                    {
                        location = Location.Park,
                        openHour = 6,
                        closeHour = 23,
                        availableOnWeekends = true,
                        description = "Парк"
                    },
                    new LocationSchedule
                    {
                        location = Location.BusinessDistrict,
                        openHour = 9,
                        closeHour = 18,
                        availableOnWeekends = false,
                        description = "Деловой район"
                    }
                };
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimePeriodChanged += OnTimePeriodChanged;
                timeOfDayManager.OnHourChanged += OnHourChanged;
                timeOfDayManager.OnDayChanged += OnDayChanged;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimePeriodChanged -= OnTimePeriodChanged;
                timeOfDayManager.OnHourChanged -= OnHourChanged;
                timeOfDayManager.OnDayChanged -= OnDayChanged;
            }
        }

        #endregion

        #region Time-Based Effects

        /// <summary>
        /// Обновить эффекты, зависящие от времени
        /// </summary>
        private void UpdateTimeBasedEffects()
        {
            if (!enableTimeBasedGameplay || timeOfDayManager == null) return;

            currentTimePeriod = timeOfDayManager.CurrentTimePeriod;
            
            // Обновить модификаторы
            UpdateTimeBasedModifiers();
            
            // Обновить доступность локаций
            if (enableLocationSchedules)
            {
                UpdateLocationAvailability();
            }

            // Обновить динамические цены
            if (enableDynamicPricing)
            {
                UpdateDynamicPricing();
            }

            // Обновить NPC активность
            if (enableTimeBasedNPCs)
            {
                UpdateNPCActivity();
            }
        }

        /// <summary>
        /// Обновить модификаторы времени
        /// </summary>
        private void UpdateTimeBasedModifiers()
        {
            TimeBasedModifiers newModifiers = GetModifiersForTimePeriod(currentTimePeriod);
            
            if (currentModifiers == null || !currentModifiers.Equals(newModifiers))
            {
                // Удалить старые модификаторы
                RemoveTimeBasedModifiers();
                
                // Применить новые модификаторы
                currentModifiers = newModifiers;
                ApplyTimeBasedModifiers();
                
                OnTimeBasedModifiersChanged?.Invoke(currentTimePeriod, currentModifiers);

                if (showDebugInfo)
                {
                    Debug.Log($"Обновлены временные модификаторы для {currentTimePeriod}");
                }
            }
        }

        /// <summary>
        /// Получить модификаторы для периода времени
        /// </summary>
        private TimeBasedModifiers GetModifiersForTimePeriod(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning:
                case TimePeriod.Morning:
                    return morningModifiers;
                case TimePeriod.Day:
                case TimePeriod.Afternoon:
                    return dayModifiers;
                case TimePeriod.Evening:
                    return eveningModifiers;
                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    return nightModifiers;
                default:
                    return dayModifiers;
            }
        }

        /// <summary>
        /// Применить модификаторы времени
        /// </summary>
        private void ApplyTimeBasedModifiers()
        {
            if (playerStats == null || currentModifiers == null) return;

            // Применить модификаторы к характеристикам игрока
            if (enableWorkEfficiency)
            {
                playerStats.AddStatModifier(new StatModifier(
                    StatType.EarningsMultiplier, 
                    currentModifiers.earningsMultiplier - 1f, 
                    ModifierOperation.Add, 
                    "TimeOfDay"));

                playerStats.AddStatModifier(new StatModifier(
                    StatType.ExperienceMultiplier, 
                    currentModifiers.workEfficiencyMultiplier - 1f, 
                    ModifierOperation.Add, 
                    "TimeOfDay"));

                playerStats.AddStatModifier(new StatModifier(
                    StatType.LearningMultiplier, 
                    currentModifiers.educationEfficiencyMultiplier - 1f, 
                    ModifierOperation.Add, 
                    "TimeOfDay"));
            }

            playerStats.AddStatModifier(new StatModifier(
                StatType.MoodRegen, 
                (currentModifiers.moodChangeMultiplier - 1f) * 0.5f, 
                ModifierOperation.Add, 
                "TimeOfDay"));

            playerStats.AddStatModifier(new StatModifier(
                StatType.HealthRegen, 
                (currentModifiers.healthChangeMultiplier - 1f) * 0.3f, 
                ModifierOperation.Add, 
                "TimeOfDay"));

            playerStats.AddStatModifier(new StatModifier(
                StatType.HungerRate, 
                (currentModifiers.hungerRateMultiplier - 1f) * 0.2f, 
                ModifierOperation.Add, 
                "TimeOfDay"));

            modifiersApplied = true;
        }

        /// <summary>
        /// Удалить модификаторы времени
        /// </summary>
        private void RemoveTimeBasedModifiers()
        {
            if (playerStats != null && modifiersApplied)
            {
                playerStats.RemoveModifiersBySource("TimeOfDay");
                modifiersApplied = false;
            }
        }

        /// <summary>
        /// Обновить доступность локаций
        /// </summary>
        private void UpdateLocationAvailability()
        {
            if (timeOfDayManager == null) return;

            int currentHour = timeOfDayManager.CurrentDateTime.Hour;
            DayOfWeek currentDayOfWeek = timeOfDayManager.CurrentDateTime.DayOfWeek;
            bool isWeekend = currentDayOfWeek == DayOfWeek.Saturday || currentDayOfWeek == DayOfWeek.Sunday;

            foreach (var schedule in locationSchedules)
            {
                bool wasAvailable = locationAvailability.ContainsKey(schedule.location) ? 
                                   locationAvailability[schedule.location] : true;

                bool isOpen = IsLocationOpen(schedule, currentHour, isWeekend);
                locationAvailability[schedule.location] = isOpen;

                if (wasAvailable != isOpen)
                {
                    OnLocationAvailabilityChanged?.Invoke(schedule.location, isOpen);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"{schedule.description} теперь {(isOpen ? "открыто" : "закрыто")}");
                    }
                }
            }

            // Уведомить LocationManager об изменениях
            if (locationManager != null)
            {
                foreach (var kvp in locationAvailability)
                {
                    locationManager.SetLocationAvailable(kvp.Key, kvp.Value);
                }
            }
        }

        /// <summary>
        /// Проверить открыта ли локация
        /// </summary>
        private bool IsLocationOpen(LocationSchedule schedule, int currentHour, bool isWeekend)
        {
            // Проверить выходные дни
            if (isWeekend && !schedule.availableOnWeekends)
            {
                return false;
            }

            // Проверить часы работы
            if (schedule.openHour <= schedule.closeHour)
            {
                // Обычный случай (например, 9-18)
                return currentHour >= schedule.openHour && currentHour < schedule.closeHour;
            }
            else
            {
                // Работа через полночь (например, 22-6)
                return currentHour >= schedule.openHour || currentHour < schedule.closeHour;
            }
        }

        /// <summary>
        /// Обновить динамические цены
        /// </summary>
        private void UpdateDynamicPricing()
        {
            float newMultiplier = GetCurrentPriceMultiplier();
            OnPriceMultiplierChanged?.Invoke(newMultiplier);

            // Применить к системе магазина
            if (shopSystem != null)
            {
                shopSystem.SetTimeBasedPriceMultiplier(newMultiplier);
            }
        }

        /// <summary>
        /// Обновить активность NPC
        /// </summary>
        private void UpdateNPCActivity()
        {
            if (npcManager == null) return;

            // Настроить количество и типы NPC в зависимости от времени
            NPCSpawnSettings spawnSettings = GetNPCSpawnSettingsForTime(currentTimePeriod);
            npcManager.UpdateTimeBasedSpawning(spawnSettings);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Время для работы
        /// </summary>
        public bool IsTimeForWork()
        {
            return currentTimePeriod == TimePeriod.Morning || 
                   currentTimePeriod == TimePeriod.Day || 
                   currentTimePeriod == TimePeriod.Afternoon;
        }

        /// <summary>
        /// Время для образования
        /// </summary>
        public bool IsTimeForEducation()
        {
            return currentTimePeriod == TimePeriod.Morning || 
                   currentTimePeriod == TimePeriod.Day;
        }

        /// <summary>
        /// Время для бизнеса
        /// </summary>
        public bool IsTimeForBusiness()
        {
            return currentTimePeriod == TimePeriod.Day || 
                   currentTimePeriod == TimePeriod.Afternoon;
        }

        /// <summary>
        /// Получить текущий множитель цен
        /// </summary>
        public float GetCurrentPriceMultiplier()
        {
            if (timeOfDayManager == null) return 1f;
            
            int currentHour = timeOfDayManager.CurrentDateTime.Hour;
            return hourlyPriceMultipliers[currentHour];
        }

        /// <summary>
        /// Получить модификатор эффективности для типа активности
        /// </summary>
        public float GetEfficiencyModifier(ActivityType activityType)
        {
            if (currentModifiers == null) return 1f;

            switch (activityType)
            {
                case ActivityType.Work:
                    return currentModifiers.workEfficiencyMultiplier;
                case ActivityType.Education:
                    return currentModifiers.educationEfficiencyMultiplier;
                case ActivityType.Rest:
                    return currentModifiers.moodChangeMultiplier;
                case ActivityType.Exercise:
                    return currentModifiers.healthChangeMultiplier;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Локация доступна
        /// </summary>
        public bool IsLocationAvailable(Location location)
        {
            return locationAvailability.ContainsKey(location) ? 
                   locationAvailability[location] : true;
        }

        /// <summary>
        /// Получить расписание локации
        /// </summary>
        public LocationSchedule GetLocationSchedule(Location location)
        {
            foreach (var schedule in locationSchedules)
            {
                if (schedule.location == location)
                {
                    return schedule;
                }
            }
            return null;
        }

        /// <summary>
        /// Получить настройки спавна NPC для времени
        /// </summary>
        private NPCSpawnSettings GetNPCSpawnSettingsForTime(TimePeriod period)
        {
            var settings = new NPCSpawnSettings();
            
            switch (period)
            {
                case TimePeriod.EarlyMorning:
                    settings.maxNPCs = 15;
                    settings.workerWeight = 0.6f;
                    settings.businessPersonWeight = 0.1f;
                    settings.studentWeight = 0.1f;
                    settings.touristWeight = 0.05f;
                    settings.homelessWeight = 0.15f;
                    break;

                case TimePeriod.Morning:
                    settings.maxNPCs = 35;
                    settings.workerWeight = 0.4f;
                    settings.businessPersonWeight = 0.2f;
                    settings.studentWeight = 0.2f;
                    settings.touristWeight = 0.1f;
                    settings.homelessWeight = 0.1f;
                    break;

                case TimePeriod.Day:
                case TimePeriod.Afternoon:
                    settings.maxNPCs = 50;
                    settings.workerWeight = 0.3f;
                    settings.businessPersonWeight = 0.25f;
                    settings.studentWeight = 0.15f;
                    settings.touristWeight = 0.2f;
                    settings.homelessWeight = 0.1f;
                    break;

                case TimePeriod.Evening:
                    settings.maxNPCs = 40;
                    settings.workerWeight = 0.2f;
                    settings.businessPersonWeight = 0.1f;
                    settings.studentWeight = 0.2f;
                    settings.touristWeight = 0.3f;
                    settings.homelessWeight = 0.2f;
                    break;

                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    settings.maxNPCs = 10;
                    settings.workerWeight = 0.1f;
                    settings.businessPersonWeight = 0.05f;
                    settings.studentWeight = 0.1f;
                    settings.touristWeight = 0.05f;
                    settings.homelessWeight = 0.7f;
                    break;
            }

            return settings;
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public TimeBasedGameplaySaveData GetSaveData()
        {
            return new TimeBasedGameplaySaveData
            {
                currentTimePeriod = currentTimePeriod,
                locationAvailability = new Dictionary<Location, bool>(locationAvailability)
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(TimeBasedGameplaySaveData data)
        {
            currentTimePeriod = data.currentTimePeriod;
            locationAvailability = data.locationAvailability ?? new Dictionary<Location, bool>();
            
            UpdateTimeBasedEffects();

            if (showDebugInfo)
            {
                Debug.Log($"TimeBasedGameplay загружен для периода {currentTimePeriod}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены периода времени
        /// </summary>
        private void OnTimePeriodChanged(TimePeriod oldPeriod, TimePeriod newPeriod)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Период времени изменен: {oldPeriod} -> {newPeriod}");
            }

            UpdateTimeBasedEffects();
        }

        /// <summary>
        /// Обработчик изменения часа
        /// </summary>
        private void OnHourChanged(int newHour)
        {
            // Обновить эффекты каждый час
            UpdateTimeBasedEffects();
        }

        /// <summary>
        /// Обработчик смены дня
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            // Обновить доступность локаций для нового дня
            if (enableLocationSchedules)
            {
                UpdateLocationAvailability();
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Show Current Modifiers")]
        private void DebugShowCurrentModifiers()
        {
            if (currentModifiers != null)
            {
                Debug.Log($"Текущие модификаторы ({currentTimePeriod}):\n" +
                         $"Работа: {currentModifiers.workEfficiencyMultiplier:P1}\n" +
                         $"Образование: {currentModifiers.educationEfficiencyMultiplier:P1}\n" +
                         $"Заработок: {currentModifiers.earningsMultiplier:P1}\n" +
                         $"Настроение: {currentModifiers.moodChangeMultiplier:P1}\n" +
                         $"Здоровье: {currentModifiers.healthChangeMultiplier:P1}");
            }
        }

        [ContextMenu("Show Location Availability")]
        private void DebugShowLocationAvailability()
        {
            Debug.Log("Доступность локаций:");
            foreach (var kvp in locationAvailability)
            {
                Debug.Log($"{kvp.Key}: {(kvp.Value ? "Открыто" : "Закрыто")}");
            }
        }

        [ContextMenu("Show Price Multiplier")]
        private void DebugShowPriceMultiplier()
        {
            Debug.Log($"Текущий множитель цен: {GetCurrentPriceMultiplier():P1}");
        }

        [ContextMenu("Force Update All Effects")]
        private void DebugForceUpdateAllEffects()
        {
            UpdateTimeBasedEffects();
            Debug.Log("Все временные эффекты принудительно обновлены");
        }

        #endregion
    }

    /// <summary>
    /// Модификаторы времени дня
    /// </summary>
    [System.Serializable]
    public class TimeBasedModifiers
    {
        [Range(0.5f, 2f)] public float workEfficiencyMultiplier = 1f;
        [Range(0.5f, 2f)] public float educationEfficiencyMultiplier = 1f;
        [Range(0.5f, 2f)] public float earningsMultiplier = 1f;
        [Range(0.5f, 2f)] public float moodChangeMultiplier = 1f;
        [Range(0.5f, 2f)] public float healthChangeMultiplier = 1f;
        [Range(0.5f, 2f)] public float hungerRateMultiplier = 1f;

        public bool Equals(TimeBasedModifiers other)
        {
            if (other == null) return false;
            
            return Mathf.Approximately(workEfficiencyMultiplier, other.workEfficiencyMultiplier) &&
                   Mathf.Approximately(educationEfficiencyMultiplier, other.educationEfficiencyMultiplier) &&
                   Mathf.Approximately(earningsMultiplier, other.earningsMultiplier) &&
                   Mathf.Approximately(moodChangeMultiplier, other.moodChangeMultiplier) &&
                   Mathf.Approximately(healthChangeMultiplier, other.healthChangeMultiplier) &&
                   Mathf.Approximately(hungerRateMultiplier, other.hungerRateMultiplier);
        }
    }

    /// <summary>
    /// Расписание локации
    /// </summary>
    [System.Serializable]
    public class LocationSchedule
    {
        public Location location;
        public string description;
        [Range(0, 23)] public int openHour = 9;
        [Range(0, 24)] public int closeHour = 18;
        public bool availableOnWeekends = true;
    }

    /// <summary>
    /// Настройки спавна NPC
    /// </summary>
    public class NPCSpawnSettings
    {
        public int maxNPCs = 30;
        [Range(0f, 1f)] public float workerWeight = 0.3f;
        [Range(0f, 1f)] public float businessPersonWeight = 0.2f;
        [Range(0f, 1f)] public float studentWeight = 0.2f;
        [Range(0f, 1f)] public float touristWeight = 0.2f;
        [Range(0f, 1f)] public float homelessWeight = 0.1f;
    }

    /// <summary>
    /// Данные TimeBasedGameplay для сохранения
    /// </summary>
    [System.Serializable]
    public class TimeBasedGameplaySaveData
    {
        public TimePeriod currentTimePeriod;
        public Dictionary<Location, bool> locationAvailability;
    }
}