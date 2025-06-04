using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система событий, зависящих от времени
    /// </summary>
    public class TimeBasedEvents : MonoBehaviour
    {
        [Header("Настройки системы")]
        [SerializeField] private bool enableTimeBasedEvents = true;
        [SerializeField] private float eventCheckInterval = 60f; // Проверка событий каждые 60 секунд
        [SerializeField] private int maxActiveEvents = 3; // Максимум активных событий одновременно
        [SerializeField] private bool allowEventOverlap = false; // Разрешить перекрытие событий

        [Header("Базовые вероятности событий")]
        [SerializeField] private float baseEventChance = 0.1f; // 10% шанс события каждую проверку
        [SerializeField] private float specialEventChance = 0.05f; // 5% шанс специального события
        [SerializeField] private float criticalEventChance = 0.02f; // 2% шанс критического события

        [Header("События по времени дня")]
        [SerializeField] private TimeBasedEventData[] morningEvents;
        [SerializeField] private TimeBasedEventData[] dayEvents;
        [SerializeField] private TimeBasedEventData[] eveningEvents;
        [SerializeField] private TimeBasedEventData[] nightEvents;

        [Header("Специальные события")]
        [SerializeField] private TimeBasedEventData[] weekendEvents;
        [SerializeField] private TimeBasedEventData[] weatherEvents;
        [SerializeField] private TimeBasedEventData[] levelBasedEvents;

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceNextEvent = false;
        [SerializeField] private string debugEventId = "";

        // Компоненты
        private TimeOfDayManager timeOfDayManager;
        private WeatherSystem weatherSystem;
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private LevelSystem levelSystem;
        private SkillSystem skillSystem;

        // Состояние системы
        private List<ActiveTimeEvent> activeEvents = new List<ActiveTimeEvent>();
        private List<string> completedEvents = new List<string>(); // События, завершенные сегодня
        private Dictionary<string, DateTime> lastEventTimes = new Dictionary<string, DateTime>();
        private float nextEventCheckTime = 0f;
        private bool systemInitialized = false;

        // События
        public event Action<TimeBasedEventData> OnEventStarted;
        public event Action<TimeBasedEventData, bool> OnEventCompleted; // bool = успешно завершено
        public event Action<TimeBasedEventData> OnEventExpired;
        public event Action<TimeBasedEventData> OnEventAvailable;

        // Свойства
        public int ActiveEventsCount => activeEvents.Count;
        public bool HasActiveEvents => activeEvents.Count > 0;
        public bool CanStartNewEvent => activeEvents.Count < maxActiveEvents;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            weatherSystem = FindObjectOfType<WeatherSystem>();
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            levelSystem = FindObjectOfType<LevelSystem>();
            skillSystem = FindObjectOfType<SkillSystem>();

            // Инициализация событий по умолчанию
            InitializeDefaultEvents();
        }

        private void Start()
        {
            if (enableTimeBasedEvents)
            {
                SubscribeToEvents();
                nextEventCheckTime = Time.time + eventCheckInterval;
                StartCoroutine(EventCheckCoroutine());
            }

            systemInitialized = true;
            Debug.Log("TimeBasedEvents инициализирован");
        }

        private void Update()
        {
            if (enableTimeBasedEvents && systemInitialized)
            {
                UpdateActiveEvents();
                
                if (forceNextEvent && !string.IsNullOrEmpty(debugEventId))
                {
                    ForceStartEvent(debugEventId);
                    forceNextEvent = false;
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
        /// Инициализация событий по умолчанию
        /// </summary>
        private void InitializeDefaultEvents()
        {
            InitializeMorningEvents();
            InitializeDayEvents();
            InitializeEveningEvents();
            InitializeNightEvents();
            InitializeSpecialEvents();
        }

        /// <summary>
        /// Инициализация утренних событий
        /// </summary>
        private void InitializeMorningEvents()
        {
            morningEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "morning_jog_offer",
                    title = "Утренняя пробежка",
                    description = "Незнакомец предлагает присоединиться к утренней пробежке в парке",
                    eventType = TimeBasedEventType.Positive,
                    category = EventCategory.Health,
                    duration = 30f, // 30 минут
                    requirements = new EventRequirements { minLevel = 1, maxFatigue = 60f },
                    rewards = new EventRewards 
                    { 
                        healthBonus = 15f, 
                        moodBonus = 10f, 
                        skillExperience = new Dictionary<SkillType, float> 
                        { 
                            { SkillType.Physical_Fitness, 5f } 
                        } 
                    },
                    chance = 0.3f,
                    isRepeatable = true,
                    cooldownHours = 24
                },
                new TimeBasedEventData
                {
                    id = "morning_newspaper_job",
                    title = "Раздача газет",
                    description = "Киоскер предлагает подработку - раздавать утренние газеты",
                    eventType = TimeBasedEventType.Opportunity,
                    category = EventCategory.Work,
                    duration = 60f,
                    requirements = new EventRequirements { minLevel = 1 },
                    rewards = new EventRewards { moneyBonus = 25.0, experience = 10f },
                    chance = 0.2f,
                    isRepeatable = true,
                    cooldownHours = 24
                },
                new TimeBasedEventData
                {
                    id = "morning_coffee_charity",
                    title = "Благотворительный кофе",
                    description = "Добрая женщина предлагает бесплатный кофе и бутерброд",
                    eventType = TimeBasedEventType.Positive,
                    category = EventCategory.Social,
                    duration = 15f,
                    requirements = new EventRequirements { maxMoney = 50.0 },
                    rewards = new EventRewards { hungerRestore = 20f, moodBonus = 15f },
                    chance = 0.25f,
                    isRepeatable = true,
                    cooldownHours = 48
                }
            };
        }

        /// <summary>
        /// Инициализация дневных событий
        /// </summary>
        private void InitializeDayEvents()
        {
            dayEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "day_volunteer_work",
                    title = "Волонтерская работа",
                    description = "НКО ищет волонтеров для помощи в организации мероприятия",
                    eventType = TimeBasedEventType.Opportunity,
                    category = EventCategory.Work,
                    duration = 120f,
                    requirements = new EventRequirements { minLevel = 2, minCharisma = 2 },
                    rewards = new EventRewards 
                    { 
                        experience = 25f, 
                        moodBonus = 20f,
                        skillExperience = new Dictionary<SkillType, float> 
                        { 
                            { SkillType.Charisma, 8f },
                            { SkillType.Business_Skills, 3f }
                        }
                    },
                    chance = 0.15f,
                    isRepeatable = true,
                    cooldownHours = 72
                },
                new TimeBasedEventData
                {
                    id = "day_lost_wallet",
                    title = "Потерянный кошелек",
                    description = "Вы нашли кошелек с деньгами. Что будете делать?",
                    eventType = TimeBasedEventType.Choice,
                    category = EventCategory.Moral,
                    duration = 10f,
                    requirements = new EventRequirements { minLevel = 1 },
                    chance = 0.1f,
                    isRepeatable = true,
                    cooldownHours = 168 // Неделя
                },
                new TimeBasedEventData
                {
                    id = "day_street_performance",
                    title = "Уличное выступление",
                    description = "Возможность выступить на улице и заработать чаевые",
                    eventType = TimeBasedEventType.Skill_Challenge,
                    category = EventCategory.Performance,
                    duration = 45f,
                    requirements = new EventRequirements { minCharisma = 3 },
                    rewards = new EventRewards 
                    { 
                        moneyBonus = 15.0, 
                        experience = 15f,
                        skillExperience = new Dictionary<SkillType, float> 
                        { 
                            { SkillType.Charisma, 5f } 
                        }
                    },
                    chance = 0.2f,
                    isRepeatable = true,
                    cooldownHours = 24
                }
            };
        }

        /// <summary>
        /// Инициализация вечерних событий
        /// </summary>
        private void InitializeEveningEvents()
        {
            eveningEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "evening_networking",
                    title = "Нетворкинг встреча",
                    description = "Неформальная встреча предпринимателей в кафе",
                    eventType = TimeBasedEventType.Opportunity,
                    category = EventCategory.Business,
                    duration = 90f,
                    requirements = new EventRequirements { minLevel = 5, minCharisma = 4, minMoney = 20.0 },
                    rewards = new EventRewards 
                    { 
                        experience = 30f,
                        skillExperience = new Dictionary<SkillType, float> 
                        { 
                            { SkillType.Business_Skills, 10f },
                            { SkillType.Charisma, 5f }
                        }
                    },
                    chance = 0.1f,
                    isRepeatable = true,
                    cooldownHours = 168
                },
                new TimeBasedEventData
                {
                    id = "evening_restaurant_help",
                    title = "Помощь в ресторане",
                    description = "Ресторан ищет временного помощника на вечер",
                    eventType = TimeBasedEventType.Work,
                    category = EventCategory.Work,
                    duration = 180f,
                    requirements = new EventRequirements { minLevel = 1, maxFatigue = 70f },
                    rewards = new EventRewards { moneyBonus = 45.0, experience = 20f },
                    chance = 0.25f,
                    isRepeatable = true,
                    cooldownHours = 48
                },
                new TimeBasedEventData
                {
                    id = "evening_community_dinner",
                    title = "Общественный ужин",
                    description = "Бесплатный ужин для нуждающихся в общественном центре",
                    eventType = TimeBasedEventType.Positive,
                    category = EventCategory.Social,
                    duration = 60f,
                    requirements = new EventRequirements { maxMoney = 30.0 },
                    rewards = new EventRewards { hungerRestore = 40f, moodBonus = 10f },
                    chance = 0.3f,
                    isRepeatable = true,
                    cooldownHours = 24
                }
            };
        }

        /// <summary>
        /// Инициализация ночных событий
        /// </summary>
        private void InitializeNightEvents()
        {
            nightEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "night_security_job",
                    title = "Ночная охрана",
                    description = "Требуется охранник на ночную смену",
                    eventType = TimeBasedEventType.Work,
                    category = EventCategory.Work,
                    duration = 480f, // 8 часов
                    requirements = new EventRequirements { minLevel = 3, minPhysicalFitness = 3 },
                    rewards = new EventRewards { moneyBonus = 80.0, experience = 25f },
                    chance = 0.15f,
                    isRepeatable = true,
                    cooldownHours = 72
                },
                new TimeBasedEventData
                {
                    id = "night_danger_encounter",
                    title = "Опасная встреча",
                    description = "Подозрительные личности приближаются к вам",
                    eventType = TimeBasedEventType.Danger,
                    category = EventCategory.Survival,
                    duration = 5f,
                    requirements = new EventRequirements { minLevel = 1 },
                    chance = 0.2f,
                    isRepeatable = true,
                    cooldownHours = 48
                },
                new TimeBasedEventData
                {
                    id = "night_illegal_opportunity",
                    title = "Сомнительное предложение",
                    description = "Незнакомец предлагает быстро заработать сомнительным способом",
                    eventType = TimeBasedEventType.Choice,
                    category = EventCategory.Moral,
                    duration = 15f,
                    requirements = new EventRequirements { minLevel = 2 },
                    chance = 0.1f,
                    isRepeatable = true,
                    cooldownHours = 168
                },
                new TimeBasedEventData
                {
                    id = "night_insomnia_walk",
                    title = "Бессонная прогулка",
                    description = "Не можете заснуть, прогулка поможет успокоиться",
                    eventType = TimeBasedEventType.Positive,
                    category = EventCategory.Health,
                    duration = 30f,
                    requirements = new EventRequirements { minFatigue = 80f },
                    rewards = new EventRewards { fatigueReduction = 15f, moodBonus = 5f },
                    chance = 0.4f,
                    isRepeatable = true,
                    cooldownHours = 12
                }
            };
        }

        /// <summary>
        /// Инициализация специальных событий
        /// </summary>
        private void InitializeSpecialEvents()
        {
            // Выходные события
            weekendEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "weekend_market_help",
                    title = "Помощь на рынке",
                    description = "Торговец на рынке ищет помощника на выходные",
                    eventType = TimeBasedEventType.Work,
                    category = EventCategory.Work,
                    duration = 240f,
                    requirements = new EventRequirements { minLevel = 2 },
                    rewards = new EventRewards { moneyBonus = 60.0, experience = 30f },
                    chance = 0.3f,
                    isRepeatable = true,
                    cooldownHours = 168
                }
            };

            // События, связанные с погодой
            weatherEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "rain_shelter_help",
                    title = "Помощь в приюте",
                    description = "Из-за дождя в приюте много людей, нужна помощь волонтеров",
                    eventType = TimeBasedEventType.Opportunity,
                    category = EventCategory.Social,
                    duration = 120f,
                    requirements = new EventRequirements { minLevel = 1 },
                    rewards = new EventRewards { experience = 20f, moodBonus = 25f },
                    chance = 0.4f,
                    weatherRequired = WeatherType.Rainy,
                    isRepeatable = true,
                    cooldownHours = 48
                }
            };

            // События, зависящие от уровня
            levelBasedEvents = new TimeBasedEventData[]
            {
                new TimeBasedEventData
                {
                    id = "level10_job_interview",
                    title = "Собеседование на работу",
                    description = "Компания заинтересовалась вашими навыками и опытом",
                    eventType = TimeBasedEventType.Opportunity,
                    category = EventCategory.Career,
                    duration = 60f,
                    requirements = new EventRequirements { minLevel = 10, minEducation = 5, minCharisma = 4 },
                    rewards = new EventRewards { experience = 100f, moneyBonus = 200.0 },
                    chance = 0.2f,
                    isRepeatable = false
                }
            };
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimePeriodChanged += OnTimePeriodChanged;
                timeOfDayManager.OnDayChanged += OnDayChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged += OnWeatherChanged;
            }

            if (levelSystem != null)
            {
                levelSystem.OnLevelUp += OnPlayerLevelUp;
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
                timeOfDayManager.OnDayChanged -= OnDayChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged -= OnWeatherChanged;
            }

            if (levelSystem != null)
            {
                levelSystem.OnLevelUp -= OnPlayerLevelUp;
            }
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Корутина проверки событий
        /// </summary>
        private IEnumerator EventCheckCoroutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(eventCheckInterval);
                
                if (enableTimeBasedEvents && CanStartNewEvent)
                {
                    CheckForNewEvents();
                }
            }
        }

        /// <summary>
        /// Проверить новые события
        /// </summary>
        private void CheckForNewEvents()
        {
            if (timeOfDayManager == null) return;

            TimePeriod currentPeriod = timeOfDayManager.CurrentTimePeriod;
            var availableEvents = GetAvailableEventsForTime(currentPeriod);

            foreach (var eventData in availableEvents)
            {
                if (ShouldTriggerEvent(eventData))
                {
                    StartEvent(eventData);
                    break; // Запускаем только одно событие за раз
                }
            }
        }

        /// <summary>
        /// Получить доступные события для времени
        /// </summary>
        private TimeBasedEventData[] GetAvailableEventsForTime(TimePeriod period)
        {
            var events = new List<TimeBasedEventData>();

            // Основные события времени дня
            switch (period)
            {
                case TimePeriod.EarlyMorning:
                case TimePeriod.Morning:
                    events.AddRange(morningEvents);
                    break;
                case TimePeriod.Day:
                case TimePeriod.Afternoon:
                    events.AddRange(dayEvents);
                    break;
                case TimePeriod.Evening:
                    events.AddRange(eveningEvents);
                    break;
                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    events.AddRange(nightEvents);
                    break;
            }

            // Добавить выходные события
            if (IsWeekend())
            {
                events.AddRange(weekendEvents);
            }

            // Добавить события погоды
            if (weatherSystem != null)
            {
                var weatherSpecificEvents = weatherEvents.Where(e => 
                    e.weatherRequired == WeatherType.Any || 
                    e.weatherRequired == weatherSystem.CurrentWeather);
                events.AddRange(weatherSpecificEvents);
            }

            // Добавить события уровня
            if (levelSystem != null)
            {
                var levelSpecificEvents = levelBasedEvents.Where(e => 
                    levelSystem.CurrentLevel >= e.requirements.minLevel);
                events.AddRange(levelSpecificEvents);
            }

            return events.ToArray();
        }

        /// <summary>
        /// Проверить, нужно ли запустить событие
        /// </summary>
        private bool ShouldTriggerEvent(TimeBasedEventData eventData)
        {
            // Проверить базовые требования
            if (!MeetsRequirements(eventData.requirements))
                return false;

            // Проверить, не завершено ли уже сегодня
            if (!eventData.isRepeatable && completedEvents.Contains(eventData.id))
                return false;

            // Проверить кулдаун
            if (IsOnCooldown(eventData))
                return false;

            // Проверить шанс срабатывания
            float eventChance = eventData.chance * baseEventChance;
            
            // Модификаторы шанса
            if (eventData.eventType == TimeBasedEventType.Danger)
                eventChance *= GetDangerChanceMultiplier();
            
            if (eventData.category == EventCategory.Moral)
                eventChance *= GetMoralEventChanceMultiplier();

            return UnityEngine.Random.value < eventChance;
        }

        /// <summary>
        /// Запустить событие
        /// </summary>
        private void StartEvent(TimeBasedEventData eventData)
        {
            var activeEvent = new ActiveTimeEvent
            {
                eventData = eventData,
                startTime = Time.time,
                endTime = Time.time + eventData.duration,
                status = EventStatus.Active
            };

            activeEvents.Add(activeEvent);
            lastEventTimes[eventData.id] = timeOfDayManager?.CurrentDateTime ?? DateTime.Now;

            OnEventStarted?.Invoke(eventData);

            // Показать уведомление
            GameEvents.TriggerNotification(
                eventData.title, 
                GetNotificationTypeForEvent(eventData), 
                NotificationPriority.High,
                eventData.description);

            if (showDebugInfo)
            {
                Debug.Log($"Запущено событие: {eventData.title} (длительность: {eventData.duration}с)");
            }

            // Для некоторых типов событий запустить автоматическое завершение
            if (eventData.eventType != TimeBasedEventType.Choice)
            {
                StartCoroutine(AutoCompleteEventCoroutine(activeEvent));
            }
        }

        /// <summary>
        /// Корутина автоматического завершения события
        /// </summary>
        private IEnumerator AutoCompleteEventCoroutine(ActiveTimeEvent activeEvent)
        {
            yield return new WaitForSeconds(activeEvent.eventData.duration);

            if (activeEvent.status == EventStatus.Active)
            {
                // Автоматически завершить с успехом для большинства событий
                bool success = activeEvent.eventData.eventType != TimeBasedEventType.Danger ||
                              UnityEngine.Random.value > 0.3f; // 70% шанс успеха для опасных событий

                CompleteEvent(activeEvent.eventData.id, success);
            }
        }

        /// <summary>
        /// Завершить событие
        /// </summary>
        public bool CompleteEvent(string eventId, bool success = true)
        {
            var activeEvent = activeEvents.FirstOrDefault(e => e.eventData.id == eventId);
            if (activeEvent == null) return false;

            activeEvent.status = success ? EventStatus.Completed : EventStatus.Failed;
            
            if (success && activeEvent.eventData.rewards != null)
            {
                ApplyEventRewards(activeEvent.eventData.rewards);
            }
            else if (!success)
            {
                ApplyEventPenalties(activeEvent.eventData);
            }

            // Добавить в завершенные события
            if (!completedEvents.Contains(eventId))
            {
                completedEvents.Add(eventId);
            }

            OnEventCompleted?.Invoke(activeEvent.eventData, success);

            // Уведомление о завершении
            string message = success ? "Событие завершено успешно!" : "Событие провалено.";
            var notificationType = success ? NotificationType.Success : NotificationType.Warning;
            GameEvents.TriggerNotification(message, notificationType, NotificationPriority.Normal);

            activeEvents.Remove(activeEvent);

            if (showDebugInfo)
            {
                Debug.Log($"Событие {activeEvent.eventData.title} {(success ? "успешно завершено" : "провалено")}");
            }

            return true;
        }

        /// <summary>
        /// Обновить активные события
        /// </summary>
        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var activeEvent = activeEvents[i];
                
                if (Time.time >= activeEvent.endTime && activeEvent.status == EventStatus.Active)
                {
                    // Событие истекло
                    activeEvent.status = EventStatus.Expired;
                    OnEventExpired?.Invoke(activeEvent.eventData);
                    
                    GameEvents.TriggerNotification(
                        "Время события истекло", 
                        NotificationType.Warning, 
                        NotificationPriority.Normal);

                    activeEvents.RemoveAt(i);

                    if (showDebugInfo)
                    {
                        Debug.Log($"Событие {activeEvent.eventData.title} истекло");
                    }
                }
            }
        }

        #endregion

        #region Requirements and Rewards

        /// <summary>
        /// Проверить требования события
        /// </summary>
        private bool MeetsRequirements(EventRequirements requirements)
        {
            if (requirements == null) return true;

            // Проверить уровень
            if (levelSystem != null && levelSystem.CurrentLevel < requirements.minLevel)
                return false;

            // Проверить деньги
            if (moneySystem != null)
            {
                double currentMoney = moneySystem.GetMoneyDouble();
                if (currentMoney < requirements.minMoney || 
                    (requirements.maxMoney > 0 && currentMoney > requirements.maxMoney))
                    return false;
            }

            // Проверить навыки
            if (skillSystem != null)
            {
                if (skillSystem.GetSkillLevel(SkillType.Charisma) < requirements.minCharisma ||
                    skillSystem.GetSkillLevel(SkillType.Education) < requirements.minEducation ||
                    skillSystem.GetSkillLevel(SkillType.Physical_Fitness) < requirements.minPhysicalFitness ||
                    skillSystem.GetSkillLevel(SkillType.Business_Skills) < requirements.minBusinessSkills ||
                    skillSystem.GetSkillLevel(SkillType.Luck) < requirements.minLuck)
                    return false;
            }

            // Проверить состояние игрока
            if (playerStats != null)
            {
                if (playerStats.CurrentHealth < requirements.minHealth ||
                    playerStats.CurrentMood < requirements.minMood ||
                    playerStats.CurrentHunger > requirements.maxHunger ||
                    playerStats.CurrentFatigue > requirements.maxFatigue)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Применить награды события
        /// </summary>
        private void ApplyEventRewards(EventRewards rewards)
        {
            if (rewards == null) return;

            // Деньги
            if (rewards.moneyBonus > 0 && moneySystem != null)
            {
                moneySystem.AddMoney(rewards.moneyBonus, "Событие: " + rewards.source);
            }

            // Опыт
            if (rewards.experience > 0 && levelSystem != null)
            {
                levelSystem.AddExperience(rewards.experience, "Событие");
            }

            // Характеристики
            if (playerStats != null)
            {
                if (rewards.healthBonus > 0) playerStats.ChangeHealth(rewards.healthBonus);
                if (rewards.moodBonus > 0) playerStats.ChangeMood(rewards.moodBonus);
                if (rewards.hungerRestore > 0) playerStats.ChangeHunger(-rewards.hungerRestore);
                if (rewards.fatigueReduction > 0) 
                {
                    // Если есть SleepSystem, используем его
                    var sleepSystem = FindObjectOfType<SleepSystem>();
                    if (sleepSystem != null)
                    {
                        sleepSystem.ReduceFatigue(rewards.fatigueReduction);
                    }
                }
            }

            // Опыт навыков
            if (rewards.skillExperience != null && skillSystem != null)
            {
                foreach (var kvp in rewards.skillExperience)
                {
                    skillSystem.AddSkillExperience(kvp.Key, kvp.Value, "Событие");
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"Применены награды события: " +
                         $"Деньги +{rewards.moneyBonus}, " +
                         $"Опыт +{rewards.experience}, " +
                         $"Здоровье +{rewards.healthBonus}");
            }
        }

        /// <summary>
        /// Применить штрафы за провал события
        /// </summary>
        private void ApplyEventPenalties(TimeBasedEventData eventData)
        {
            if (playerStats == null) return;

            // Базовые штрафы в зависимости от типа события
            switch (eventData.eventType)
            {
                case TimeBasedEventType.Danger:
                    playerStats.ChangeHealth(-UnityEngine.Random.Range(10f, 25f));
                    playerStats.ChangeMood(-UnityEngine.Random.Range(5f, 15f));
                    break;

                case TimeBasedEventType.Work:
                    playerStats.ChangeMood(-UnityEngine.Random.Range(5f, 10f));
                    break;

                case TimeBasedEventType.Choice:
                    // Штрафы зависят от выбора
                    playerStats.ChangeMood(-UnityEngine.Random.Range(3f, 8f));
                    break;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Принудительно запустить событие
        /// </summary>
        public bool ForceStartEvent(string eventId)
        {
            var allEvents = new List<TimeBasedEventData>();
            allEvents.AddRange(morningEvents);
            allEvents.AddRange(dayEvents);
            allEvents.AddRange(eveningEvents);
            allEvents.AddRange(nightEvents);
            allEvents.AddRange(weekendEvents);
            allEvents.AddRange(weatherEvents);
            allEvents.AddRange(levelBasedEvents);

            var eventData = allEvents.FirstOrDefault(e => e.id == eventId);
            if (eventData != null && CanStartNewEvent)
            {
                StartEvent(eventData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Получить активные события
        /// </summary>
        public ActiveTimeEvent[] GetActiveEvents()
        {
            return activeEvents.ToArray();
        }

        /// <summary>
        /// Событие на кулдауне
        /// </summary>
        private bool IsOnCooldown(TimeBasedEventData eventData)
        {
            if (eventData.cooldownHours <= 0) return false;

            if (lastEventTimes.ContainsKey(eventData.id))
            {
                DateTime lastTime = lastEventTimes[eventData.id];
                DateTime currentTime = timeOfDayManager?.CurrentDateTime ?? DateTime.Now;
                TimeSpan timeSince = currentTime - lastTime;
                
                return timeSince.TotalHours < eventData.cooldownHours;
            }

            return false;
        }

        /// <summary>
        /// Выходные дни
        /// </summary>
        private bool IsWeekend()
        {
            if (timeOfDayManager == null) return false;
            
            DayOfWeek day = timeOfDayManager.CurrentDateTime.DayOfWeek;
            return day == DayOfWeek.Saturday || day == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Получить множитель шанса опасности
        /// </summary>
        private float GetDangerChanceMultiplier()
        {
            float multiplier = 1f;
            
            // Ночью опаснее
            if (timeOfDayManager != null)
            {
                TimePeriod period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                    multiplier *= 2f;
            }

            // В плохую погоду опаснее
            if (weatherSystem != null && weatherSystem.IsStormy)
                multiplier *= 1.5f;

            return multiplier;
        }

        /// <summary>
        /// Получить множитель шанса моральных событий
        /// </summary>
        private float GetMoralEventChanceMultiplier()
        {
            float multiplier = 1f;
            
            // Зависит от уровня игрока
            if (levelSystem != null)
            {
                multiplier *= (1f + levelSystem.CurrentLevel * 0.1f);
            }

            return multiplier;
        }

        /// <summary>
        /// Получить тип уведомления для события
        /// </summary>
        private NotificationType GetNotificationTypeForEvent(TimeBasedEventData eventData)
        {
            switch (eventData.eventType)
            {
                case TimeBasedEventType.Positive:
                    return NotificationType.Success;
                case TimeBasedEventType.Danger:
                    return NotificationType.Danger;
                case TimeBasedEventType.Opportunity:
                case TimeBasedEventType.Work:
                    return NotificationType.Info;
                case TimeBasedEventType.Choice:
                    return NotificationType.Warning;
                default:
                    return NotificationType.Info;
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public TimeBasedEventsSaveData GetSaveData()
        {
            return new TimeBasedEventsSaveData
            {
                completedEvents = new List<string>(completedEvents),
                lastEventTimes = new Dictionary<string, long>()
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(TimeBasedEventsSaveData data)
        {
            completedEvents = data.completedEvents ?? new List<string>();
            
            // Конвертировать long обратно в DateTime
            lastEventTimes.Clear();
            if (data.lastEventTimes != null)
            {
                foreach (var kvp in data.lastEventTimes)
                {
                    lastEventTimes[kvp.Key] = DateTime.FromBinary(kvp.Value);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"TimeBasedEvents загружен. Завершенных событий: {completedEvents.Count}");
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
                Debug.Log($"Период времени изменен: {oldPeriod} -> {newPeriod}, проверяем новые события");
            }

            // Немедленная проверка событий при смене периода
            if (enableTimeBasedEvents && CanStartNewEvent)
            {
                CheckForNewEvents();
            }
        }

        /// <summary>
        /// Обработчик смены дня
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            // Сброс завершенных событий на новый день
            completedEvents.Clear();

            if (showDebugInfo)
            {
                Debug.Log("Новый день - сброс завершенных событий");
            }
        }

        /// <summary>
        /// Обработчик изменения погоды
        /// </summary>
        private void OnWeatherChanged(WeatherType weather, WeatherIntensity intensity)
        {
            // Проверить события, связанные с погодой
            if (enableTimeBasedEvents && CanStartNewEvent)
            {
                var weatherSpecificEvents = weatherEvents.Where(e => e.weatherRequired == weather);
                foreach (var eventData in weatherSpecificEvents)
                {
                    if (ShouldTriggerEvent(eventData))
                    {
                        StartEvent(eventData);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик повышения уровня игрока
        /// </summary>
        private void OnPlayerLevelUp(LevelUpData data)
        {
            // Проверить события, связанные с уровнем
            var levelSpecificEvents = levelBasedEvents.Where(e =>
                e.requirements.minLevel == data.newLevel &&
                !completedEvents.Contains(e.id));

            foreach (var eventData in levelSpecificEvents)
            {
                if (CanStartNewEvent)
                {
                    StartEvent(eventData);
                    break;
                }
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Trigger Random Event")]
        private void DebugTriggerRandomEvent()
        {
            if (CanStartNewEvent)
            {
                CheckForNewEvents();
            }
        }

        [ContextMenu("Show Active Events")]
        private void DebugShowActiveEvents()
        {
            Debug.Log($"Активных событий: {activeEvents.Count}");
            foreach (var evt in activeEvents)
            {
                float timeLeft = evt.endTime - Time.time;
                Debug.Log($"- {evt.eventData.title} (осталось: {timeLeft:F0}с, статус: {evt.status})");
            }
        }

        [ContextMenu("Clear All Events")]
        private void DebugClearAllEvents()
        {
            activeEvents.Clear();
            completedEvents.Clear();
            Debug.Log("Все события очищены");
        }

        [ContextMenu("Show Event Stats")]
        private void DebugShowEventStats()
        {
            Debug.Log($"Завершенных событий сегодня: {completedEvents.Count}");
            Debug.Log($"Событий на кулдауне: {lastEventTimes.Count}");
            Debug.Log($"Можно запустить новое событие: {CanStartNewEvent}");
        }

        #endregion
    }

    /// <summary>
    /// Данные события, зависящего от времени
    /// </summary>
    [System.Serializable]
    public class TimeBasedEventData
    {
        public string id;
        public string title;
        public string description;
        public TimeBasedEventType eventType;
        public EventCategory category;
        public float duration; // в секундах
        public EventRequirements requirements;
        public EventRewards rewards;
        public float chance; // 0-1
        public bool isRepeatable = true;
        public float cooldownHours = 24f;
        public WeatherType weatherRequired = WeatherType.Any;
    }

    /// <summary>
    /// Требования для события
    /// </summary>
    [System.Serializable]
    public class EventRequirements
    {
        public int minLevel = 1;
        public double minMoney = 0.0;
        public double maxMoney = 0.0; // 0 = без ограничений
        public float minHealth = 0f;
        public float minMood = 0f;
        public float maxHunger = 100f;
        public float maxFatigue = 100f;
        public int minCharisma = 0;
        public int minEducation = 0;
        public int minPhysicalFitness = 0;
        public int minBusinessSkills = 0;
        public int minLuck = 0;
    }

    /// <summary>
    /// Награды за событие
    /// </summary>
    [System.Serializable]
    public class EventRewards
    {
        public double moneyBonus = 0.0;
        public float experience = 0f;
        public float healthBonus = 0f;
        public float moodBonus = 0f;
        public float hungerRestore = 0f;
        public float fatigueReduction = 0f;
        public Dictionary<SkillType, float> skillExperience;
        public string source = "";
    }

    /// <summary>
    /// Активное событие
    /// </summary>
    public class ActiveTimeEvent
    {
        public TimeBasedEventData eventData;
        public float startTime;
        public float endTime;
        public EventStatus status;
    }

    /// <summary>
    /// Данные TimeBasedEvents для сохранения
    /// </summary>
    [System.Serializable]
    public class TimeBasedEventsSaveData
    {
        public List<string> completedEvents;
        public Dictionary<string, long> lastEventTimes; // DateTime.ToBinary()
    }
}
