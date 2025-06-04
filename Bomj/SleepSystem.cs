using System;
using System.Collections;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система сна и отдыха
    /// </summary>
    public class SleepSystem : MonoBehaviour
    {
        [Header("Настройки системы сна")]
        [SerializeField] private bool enableSleepSystem = true;
        [SerializeField] private float maxFatigue = 100f;                    // Максимальная усталость
        [SerializeField] private float fatigueIncreaseRate = 2f;             // Скорость роста усталости в час
        [SerializeField] private float fatigueIncreaseMultiplier = 1f;       // Множитель роста усталости
        [SerializeField] private bool autoSleepWhenExhausted = false;        // Автоматический сон при истощении

        [Header("Влияние усталости")]
        [SerializeField] private float fatigueHealthPenalty = -0.5f;         // Штраф к здоровью от усталости
        [SerializeField] private float fatigueMoodPenalty = -0.3f;           // Штраф к настроению от усталости
        [SerializeField] private float fatigueEarningsPenalty = -0.4f;       // Штраф к заработку от усталости
        [SerializeField] private float fatigueHungerIncrease = 0.2f;         // Увеличение голода от усталости

        [Header("Эффекты сна по локациям")]
        [SerializeField] private SleepLocationData[] sleepLocations;

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool skipSleepAnimation = false;
        [SerializeField] private float debugFatigueLevel = 50f;

        // Текущее состояние
        private float currentFatigue = 0f;
        private FatigueLevel currentFatigueLevel = FatigueLevel.Rested;
        private bool isSleeping = false;
        private DateTime lastSleepTime;
        private SleepLocation lastSleepLocation = SleepLocation.Street;
        private float totalSleepTime = 0f;
        private int sleepSessionsCount = 0;

        // Компоненты
        private TimeOfDayManager timeOfDayManager;
        private PlayerStats playerStats;
        private LocationManager locationManager;
        private MoneySystem moneySystem;

        // События
        public event Action<FatigueLevel> OnFatigueLevelChanged;
        public event Action<SleepLocation, float> OnSleepStarted;
        public event Action<SleepLocation, float> OnSleepCompleted;
        public event Action<float> OnFatigueChanged;

        // Свойства
        public float CurrentFatigue => currentFatigue;
        public FatigueLevel CurrentFatigueLevel => currentFatigueLevel;
        public float FatiguePercentage => currentFatigue / maxFatigue;
        public bool IsSleeping => isSleeping;
        public bool IsExhausted => currentFatigueLevel == FatigueLevel.Exhausted;
        public bool ShouldSleep => currentFatigue >= maxFatigue * 0.8f; // Рекомендация спать при 80% усталости

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            playerStats = FindObjectOfType<PlayerStats>();
            locationManager = FindObjectOfType<LocationManager>();
            moneySystem = FindObjectOfType<MoneySystem>();

            // Инициализация данных локаций сна по умолчанию
            InitializeDefaultSleepLocations();
        }

        private void Start()
        {
            if (enableSleepSystem)
            {
                SubscribeToEvents();
                lastSleepTime = timeOfDayManager?.CurrentDateTime ?? DateTime.Now;
                
                // Применить начальные эффекты усталости
                ApplyFatigueEffects();
            }

            Debug.Log("SleepSystem инициализирован");
        }

        private void Update()
        {
            if (enableSleepSystem && !isSleeping)
            {
                UpdateFatigue();
            }

            if (Application.isEditor && showDebugInfo)
            {
                // Дебаг в редакторе
                if (Mathf.Abs(currentFatigue - debugFatigueLevel) > 1f)
                {
                    SetFatigue(debugFatigueLevel);
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация данных локаций сна по умолчанию
        /// </summary>
        private void InitializeDefaultSleepLocations()
        {
            if (sleepLocations == null || sleepLocations.Length == 0)
            {
                sleepLocations = new SleepLocationData[]
                {
                    new SleepLocationData
                    {
                        location = SleepLocation.Street,
                        cost = 0f,
                        healthRestore = 0.1f,
                        moodRestore = -0.1f,
                        fatigueReduction = 0.3f,
                        safetyRisk = 0.4f,
                        minSleepHours = 3f,
                        maxSleepHours = 8f,
                        displayName = "Улица"
                    },
                    new SleepLocationData
                    {
                        location = SleepLocation.Park,
                        cost = 0f,
                        healthRestore = 0.15f,
                        moodRestore = 0.05f,
                        fatigueReduction = 0.4f,
                        safetyRisk = 0.3f,
                        minSleepHours = 4f,
                        maxSleepHours = 8f,
                        displayName = "Парк"
                    },
                    new SleepLocationData
                    {
                        location = SleepLocation.Shelter,
                        cost = 5f,
                        healthRestore = 0.3f,
                        moodRestore = 0.1f,
                        fatigueReduction = 0.6f,
                        safetyRisk = 0.1f,
                        minSleepHours = 6f,
                        maxSleepHours = 10f,
                        displayName = "Приют"
                    },
                    new SleepLocationData
                    {
                        location = SleepLocation.Hostel,
                        cost = 25f,
                        healthRestore = 0.5f,
                        moodRestore = 0.2f,
                        fatigueReduction = 0.8f,
                        safetyRisk = 0.05f,
                        minSleepHours = 6f,
                        maxSleepHours = 12f,
                        displayName = "Хостел"
                    },
                    new SleepLocationData
                    {
                        location = SleepLocation.Hotel,
                        cost = 100f,
                        healthRestore = 0.8f,
                        moodRestore = 0.4f,
                        fatigueReduction = 1f,
                        safetyRisk = 0f,
                        minSleepHours = 8f,
                        maxSleepHours = 12f,
                        displayName = "Отель"
                    },
                    new SleepLocationData
                    {
                        location = SleepLocation.Apartment,
                        cost = 200f, // Цена покупки/аренды
                        healthRestore = 1f,
                        moodRestore = 0.6f,
                        fatigueReduction = 1f,
                        safetyRisk = 0f,
                        minSleepHours = 6f,
                        maxSleepHours = 12f,
                        displayName = "Квартира"
                    },
                    new SleepLocationData
                    {
                        location = SleepLocation.House,
                        cost = 1000f, // Цена покупки
                        healthRestore = 1.2f,
                        moodRestore = 0.8f,
                        fatigueReduction = 1f,
                        safetyRisk = 0f,
                        minSleepHours = 6f,
                        maxSleepHours = 14f,
                        displayName = "Дом"
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
                timeOfDayManager.OnHourChanged -= OnHourChanged;
                timeOfDayManager.OnDayChanged -= OnDayChanged;
            }
        }

        #endregion

        #region Fatigue Management

        /// <summary>
        /// Обновить усталость
        /// </summary>
        private void UpdateFatigue()
        {
            if (timeOfDayManager == null) return;

            // Вычислить время с последнего обновления
            DateTime currentTime = timeOfDayManager.CurrentDateTime;
            TimeSpan timeSinceLastUpdate = currentTime - lastSleepTime;
            
            // Увеличить усталость на основе прошедшего времени
            float hoursAwake = (float)timeSinceLastUpdate.TotalHours;
            float fatigueIncrease = hoursAwake * fatigueIncreaseRate * fatigueIncreaseMultiplier;
            
            AddFatigue(fatigueIncrease);
            lastSleepTime = currentTime;

            // Автоматический сон при истощении
            if (autoSleepWhenExhausted && IsExhausted && !isSleeping)
            {
                StartSleep(GetBestAvailableSleepLocation(), 8f);
            }
        }

        /// <summary>
        /// Добавить усталость
        /// </summary>
        public void AddFatigue(float amount)
        {
            float oldFatigue = currentFatigue;
            currentFatigue = Mathf.Clamp(currentFatigue + amount, 0f, maxFatigue);

            if (Mathf.Abs(oldFatigue - currentFatigue) > 0.1f)
            {
                OnFatigueChanged?.Invoke(currentFatigue);
                CheckFatigueLevelChange();
                ApplyFatigueEffects();

                if (showDebugInfo && amount > 0.1f)
                {
                    Debug.Log($"Усталость увеличена на {amount:F1}. Текущая: {currentFatigue:F1}/{maxFatigue} ({GetFatigueLevelName()})");
                }
            }
        }

        /// <summary>
        /// Уменьшить усталость
        /// </summary>
        public void ReduceFatigue(float amount)
        {
            AddFatigue(-amount);
        }

        /// <summary>
        /// Установить уровень усталости
        /// </summary>
        public void SetFatigue(float amount)
        {
            float oldFatigue = currentFatigue;
            currentFatigue = Mathf.Clamp(amount, 0f, maxFatigue);

            OnFatigueChanged?.Invoke(currentFatigue);
            CheckFatigueLevelChange();
            ApplyFatigueEffects();

            if (showDebugInfo)
            {
                Debug.Log($"Усталость установлена: {currentFatigue:F1}/{maxFatigue} ({GetFatigueLevelName()})");
            }
        }

        /// <summary>
        /// Проверить изменение уровня усталости
        /// </summary>
        private void CheckFatigueLevelChange()
        {
            FatigueLevel newLevel = CalculateFatigueLevel();
            if (newLevel != currentFatigueLevel)
            {
                FatigueLevel oldLevel = currentFatigueLevel;
                currentFatigueLevel = newLevel;
                OnFatigueLevelChanged?.Invoke(newLevel);

                if (showDebugInfo)
                {
                    Debug.Log($"Уровень усталости изменен: {oldLevel} -> {newLevel}");
                }
            }
        }

        /// <summary>
        /// Вычислить уровень усталости
        /// </summary>
        private FatigueLevel CalculateFatigueLevel()
        {
            float percentage = FatiguePercentage;
            
            if (percentage <= 0.2f) return FatigueLevel.Rested;
            if (percentage <= 0.4f) return FatigueLevel.Slightly_Tired;
            if (percentage <= 0.6f) return FatigueLevel.Tired;
            if (percentage <= 0.8f) return FatigueLevel.Very_Tired;
            return FatigueLevel.Exhausted;
        }

        #endregion

        #region Sleep Management

        /// <summary>
        /// Начать сон
        /// </summary>
        public bool StartSleep(SleepLocation location, float hours = 8f)
        {
            if (isSleeping)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("Уже спим!");
                }
                return false;
            }

            SleepLocationData locationData = GetSleepLocationData(location);
            if (locationData == null)
            {
                if (showDebugInfo)
                {
                    Debug.LogError($"Данные для локации сна {location} не найдены!");
                }
                return false;
            }

            // Проверить доступность локации
            if (!CanSleepAtLocation(location))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Нельзя спать в локации {location}");
                }
                return false;
            }

            // Ограничить время сна
            hours = Mathf.Clamp(hours, locationData.minSleepHours, locationData.maxSleepHours);

            // Списать деньги за сон (если нужно)
            if (locationData.cost > 0 && moneySystem != null)
            {
                if (!moneySystem.CanAfford(locationData.cost))
                {
                    if (showDebugInfo)
                    {
                        Debug.LogWarning($"Недостаточно денег для сна в {locationData.displayName}. Нужно: ${locationData.cost}");
                    }
                    return false;
                }

                moneySystem.SpendMoney(locationData.cost, $"Сон ({locationData.displayName})");
            }

            // Начать сон
            isSleeping = true;
            lastSleepLocation = location;
            OnSleepStarted?.Invoke(location, hours);

            if (showDebugInfo)
            {
                Debug.Log($"Начинаем спать в {locationData.displayName} на {hours:F1} часов");
            }

            // Запустить корутину сна
            StartCoroutine(SleepCoroutine(locationData, hours));

            return true;
        }

        /// <summary>
        /// Корутина сна
        /// </summary>
        private IEnumerator SleepCoroutine(SleepLocationData locationData, float hours)
        {
            float sleepDuration = skipSleepAnimation ? 0.1f : 2f; // Анимация сна

            // Показать эффекты начала сна
            if (!skipSleepAnimation)
            {
                yield return new WaitForSeconds(sleepDuration);
            }

            // Пропустить время
            if (timeOfDayManager != null)
            {
                timeOfDayManager.SkipTime(Mathf.FloorToInt(hours), Mathf.FloorToInt((hours % 1) * 60));
            }

            // Применить эффекты сна
            ApplySleepEffects(locationData, hours);

            // Проверить риск безопасности
            CheckSleepSafetyRisk(locationData);

            // Завершить сон
            CompleteSleep(locationData, hours);

            yield return null;
        }

        /// <summary>
        /// Применить эффекты сна
        /// </summary>
        private void ApplySleepEffects(SleepLocationData locationData, float hours)
        {
            if (playerStats == null) return;

            // Восстановить усталость
            float fatigueReduction = maxFatigue * locationData.fatigueReduction;
            ReduceFatigue(fatigueReduction);

            // Восстановить здоровье
            if (locationData.healthRestore > 0)
            {
                float healthRestore = playerStats.MaxHealth * locationData.healthRestore;
                playerStats.ChangeHealth(healthRestore);
            }

            // Изменить настроение
            if (Mathf.Abs(locationData.moodRestore) > 0.01f)
            {
                float moodChange = playerStats.MaxMood * locationData.moodRestore;
                playerStats.ChangeMood(moodChange);
            }

            // Увеличить голод (во время сна тоже есть хочется)
            float hungerIncrease = hours * 2f; // 2 голода за час сна
            playerStats.ChangeHunger(hungerIncrease);

            // Обновить статистику
            totalSleepTime += hours;
            sleepSessionsCount++;

            if (showDebugInfo)
            {
                Debug.Log($"Эффекты сна применены: " +
                         $"Усталость -{fatigueReduction:F1}, " +
                         $"Здоровье +{playerStats.MaxHealth * locationData.healthRestore:F1}, " +
                         $"Настроение {(locationData.moodRestore >= 0 ? "+" : "")}{playerStats.MaxMood * locationData.moodRestore:F1}");
            }
        }

        /// <summary>
        /// Проверить риск безопасности во время сна
        /// </summary>
        private void CheckSleepSafetyRisk(SleepLocationData locationData)
        {
            if (locationData.safetyRisk <= 0f) return;

            if (UnityEngine.Random.value < locationData.safetyRisk)
            {
                // Произошло что-то плохое
                ApplySafetyRiskPenalty(locationData);
            }
        }

        /// <summary>
        /// Применить штраф за риск безопасности
        /// </summary>
        private void ApplySafetyRiskPenalty(SleepLocationData locationData)
        {
            if (playerStats == null) return;

            // Возможные варианты: ограбление, избиение, болезнь
            float riskRoll = UnityEngine.Random.value;

            if (riskRoll < 0.4f && moneySystem != null)
            {
                // Ограбление - потеря денег
                double currentMoney = moneySystem.GetMoneyDouble();
                double stolenAmount = currentMoney * UnityEngine.Random.Range(0.1f, 0.3f);
                if (stolenAmount > 0)
                {
                    moneySystem.SpendMoney(stolenAmount, "Ограбление во время сна");
                    GameEvents.TriggerNotification($"Вас ограбили! Потеряно ${stolenAmount:F0}", 
                                                  NotificationType.Danger, NotificationPriority.High);
                }
            }
            else if (riskRoll < 0.7f)
            {
                // Избиение - потеря здоровья
                float healthLoss = playerStats.MaxHealth * UnityEngine.Random.Range(0.1f, 0.25f);
                playerStats.ChangeHealth(-healthLoss);
                GameEvents.TriggerNotification("Вас избили во время сна!", 
                                              NotificationType.Danger, NotificationPriority.High);
            }
            else
            {
                // Плохой сон - потеря настроения
                float moodLoss = playerStats.MaxMood * UnityEngine.Random.Range(0.15f, 0.3f);
                playerStats.ChangeMood(-moodLoss);
                GameEvents.TriggerNotification("Плохо спали из-за шума и холода", 
                                              NotificationType.Warning, NotificationPriority.Normal);
            }

            if (showDebugInfo)
            {
                Debug.LogWarning($"Риск безопасности сработал в {locationData.displayName}");
            }
        }

        /// <summary>
        /// Завершить сон
        /// </summary>
        private void CompleteSleep(SleepLocationData locationData, float hours)
        {
            isSleeping = false;
            lastSleepTime = timeOfDayManager?.CurrentDateTime ?? DateTime.Now;
            
            OnSleepCompleted?.Invoke(lastSleepLocation, hours);

            GameEvents.TriggerNotification($"Проспали {hours:F0}ч в {locationData.displayName}", 
                                          NotificationType.Info, NotificationPriority.Low);

            if (showDebugInfo)
            {
                Debug.Log($"Сон завершен в {locationData.displayName}. Текущая усталость: {currentFatigue:F1}/{maxFatigue}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Можно ли спать в указанной локации
        /// </summary>
        public bool CanSleepAtLocation(SleepLocation location)
        {
            SleepLocationData locationData = GetSleepLocationData(location);
            if (locationData == null) return false;

            // Проверить деньги
            if (locationData.cost > 0 && moneySystem != null)
            {
                if (!moneySystem.CanAfford(locationData.cost))
                    return false;
            }

            // Проверить доступность локации по времени
            if (timeOfDayManager != null)
            {
                TimePeriod currentPeriod = timeOfDayManager.CurrentTimePeriod;
                
                // Некоторые места недоступны в определенное время
                switch (location)
                {
                    case SleepLocation.Shelter:
                        // Приюты обычно работают только ночью
                        return currentPeriod == TimePeriod.Evening || 
                               currentPeriod == TimePeriod.Night || 
                               currentPeriod == TimePeriod.LateNight;
                        
                    case SleepLocation.Hotel:
                        // Отели работают всегда, но дороже ночью
                        return true;
                        
                    default:
                        return true;
                }
            }

            return true;
        }

        /// <summary>
        /// Получить лучшую доступную локацию для сна
        /// </summary>
        public SleepLocation GetBestAvailableSleepLocation()
        {
            // Проверить от лучшего к худшему
            SleepLocation[] priorityOrder = 
            {
                SleepLocation.House,
                SleepLocation.Apartment,
                SleepLocation.Hotel,
                SleepLocation.Hostel,
                SleepLocation.Shelter,
                SleepLocation.Park,
                SleepLocation.Street
            };

            foreach (var location in priorityOrder)
            {
                if (CanSleepAtLocation(location))
                {
                    return location;
                }
            }

            return SleepLocation.Street; // Fallback
        }

        /// <summary>
        /// Получить доступные локации для сна
        /// </summary>
        public SleepLocation[] GetAvailableSleepLocations()
        {
            var availableLocations = new System.Collections.Generic.List<SleepLocation>();

            foreach (var locationData in sleepLocations)
            {
                if (CanSleepAtLocation(locationData.location))
                {
                    availableLocations.Add(locationData.location);
                }
            }

            return availableLocations.ToArray();
        }

        /// <summary>
        /// Получить данные локации сна
        /// </summary>
        public SleepLocationData GetSleepLocationData(SleepLocation location)
        {
            foreach (var locationData in sleepLocations)
            {
                if (locationData.location == location)
                {
                    return locationData;
                }
            }
            return null;
        }

        /// <summary>
        /// Прервать сон
        /// </summary>
        public void WakeUp()
        {
            if (isSleeping)
            {
                StopAllCoroutines();
                isSleeping = false;
                
                GameEvents.TriggerNotification("Сон прерван", NotificationType.Warning, NotificationPriority.Normal);
                
                if (showDebugInfo)
                {
                    Debug.Log("Сон принудительно прерван");
                }
            }
        }

        /// <summary>
        /// Получить название уровня усталости
        /// </summary>
        public string GetFatigueLevelName()
        {
            switch (currentFatigueLevel)
            {
                case FatigueLevel.Rested: return "Отдохнувший";
                case FatigueLevel.Slightly_Tired: return "Слегка устал";
                case FatigueLevel.Tired: return "Устал";
                case FatigueLevel.Very_Tired: return "Очень устал";
                case FatigueLevel.Exhausted: return "Изнеможение";
                default: return currentFatigueLevel.ToString();
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public SleepSystemSaveData GetSaveData()
        {
            return new SleepSystemSaveData
            {
                currentFatigue = currentFatigue,
                lastSleepTime = lastSleepTime.ToBinary(),
                totalSleepTime = totalSleepTime,
                sleepSessionsCount = sleepSessionsCount,
                lastSleepLocation = lastSleepLocation
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(SleepSystemSaveData data)
        {
            currentFatigue = data.currentFatigue;
            lastSleepTime = DateTime.FromBinary(data.lastSleepTime);
            totalSleepTime = data.totalSleepTime;
            sleepSessionsCount = data.sleepSessionsCount;
            lastSleepLocation = data.lastSleepLocation;

            CheckFatigueLevelChange();
            ApplyFatigueEffects();

            if (showDebugInfo)
            {
                Debug.Log($"SleepSystem загружен. Усталость: {currentFatigue:F1}, последний сон: {lastSleepTime}");
            }
        }

        #endregion

        #region Fatigue Effects

        /// <summary>
        /// Применить эффекты усталости
        /// </summary>
        private void ApplyFatigueEffects()
        {
            if (playerStats == null) return;

            // Удалить старые модификаторы усталости
            playerStats.RemoveModifiersBySource("Fatigue");

            // Вычислить штрафы на основе уровня усталости
            float fatigueMultiplier = GetFatigueEffectMultiplier();

            if (fatigueMultiplier > 0.1f)
            {
                // Штраф к здоровью
                playerStats.AddStatModifier(new StatModifier(
                    StatType.HealthRegen, fatigueHealthPenalty * fatigueMultiplier, 
                    ModifierOperation.Add, "Fatigue"));

                // Штраф к настроению
                playerStats.AddStatModifier(new StatModifier(
                    StatType.MoodRegen, fatigueMoodPenalty * fatigueMultiplier, 
                    ModifierOperation.Add, "Fatigue"));

                // Штраф к заработку
                playerStats.AddStatModifier(new StatModifier(
                    StatType.EarningsMultiplier, fatigueEarningsPenalty * fatigueMultiplier, 
                    ModifierOperation.Add, "Fatigue"));

                // Увеличение голода
                playerStats.AddStatModifier(new StatModifier(
                    StatType.HungerRate, fatigueHungerIncrease * fatigueMultiplier, 
                    ModifierOperation.Add, "Fatigue"));
            }
        }

        /// <summary>
        /// Получить множитель эффекта усталости
        /// </summary>
        private float GetFatigueEffectMultiplier()
        {
            switch (currentFatigueLevel)
            {
                case FatigueLevel.Rested: return 0f;
                case FatigueLevel.Slightly_Tired: return 0.2f;
                case FatigueLevel.Tired: return 0.5f;
                case FatigueLevel.Very_Tired: return 0.8f;
                case FatigueLevel.Exhausted: return 1f;
                default: return 0f;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик изменения часа
        /// </summary>
        private void OnHourChanged(int newHour)
        {
            // Автоматическое увеличение усталости каждый час
            if (!isSleeping)
            {
                AddFatigue(fatigueIncreaseRate);
            }
        }

        /// <summary>
        /// Обработчик смены дня
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            // Сброс некоторых статистик каждый день
            if (showDebugInfo)
            {
                Debug.Log($"Новый день. Всего часов сна: {totalSleepTime:F1}, сессий сна: {sleepSessionsCount}");
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Add Fatigue (10)")]
        private void DebugAddFatigue()
        {
            AddFatigue(10f);
        }

        [ContextMenu("Remove Fatigue (20)")]
        private void DebugRemoveFatigue()
        {
            ReduceFatigue(20f);
        }

        [ContextMenu("Set Exhausted")]
        private void DebugSetExhausted()
        {
            SetFatigue(maxFatigue);
        }

        [ContextMenu("Set Rested")]
        private void DebugSetRested()
        {
            SetFatigue(0f);
        }

        [ContextMenu("Sleep in Park (6h)")]
        private void DebugSleepInPark()
        {
            StartSleep(SleepLocation.Park, 6f);
        }

        [ContextMenu("Sleep in Hotel (8h)")]
        private void DebugSleepInHotel()
        {
            StartSleep(SleepLocation.Hotel, 8f);
        }

        [ContextMenu("Show Sleep Info")]
        private void DebugShowSleepInfo()
        {
            Debug.Log($"Усталость: {currentFatigue:F1}/{maxFatigue} ({GetFatigueLevelName()})");
            Debug.Log($"Процент усталости: {FatiguePercentage:P1}");
            Debug.Log($"Рекомендуется спать: {ShouldSleep}");
            Debug.Log($"Лучшая доступная локация: {GetBestAvailableSleepLocation()}");
            Debug.Log($"Всего сна: {totalSleepTime:F1}ч, сессий: {sleepSessionsCount}");
        }

        #endregion
    }

    /// <summary>
    /// Данные локации для сна
    /// </summary>
    [System.Serializable]
    public class SleepLocationData
    {
        public SleepLocation location;
        public string displayName;
        public double cost;                 // Стоимость сна
        public float healthRestore;         // Восстановление здоровья (множитель от макс.)
        public float moodRestore;           // Восстановление настроения (множитель от макс.)
        public float fatigueReduction;      // Снижение усталости (множитель от макс.)
        public float safetyRisk;            // Риск опасности (0-1)
        public float minSleepHours;         // Минимальное время сна
        public float maxSleepHours;         // Максимальное время сна
    }

    /// <summary>
    /// Данные SleepSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class SleepSystemSaveData
    {
        public float currentFatigue;
        public long lastSleepTime;
        public float totalSleepTime;
        public int sleepSessionsCount;
        public SleepLocation lastSleepLocation;
    }
}
