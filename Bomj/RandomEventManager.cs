using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер случайных событий
    /// </summary>
    public class RandomEventManager : MonoBehaviour
    {
        [Header("Настройки системы событий")]
        [SerializeField] private bool enableRandomEvents = true;
        [SerializeField] private float globalEventInterval = 3600f;        // Интервал глобальных событий (секунды)
        [SerializeField] private float personalEventInterval = 1800f;      // Интервал личных событий
        [SerializeField] private float locationEventInterval = 2400f;      // Интервал локационных событий
        [SerializeField] private float temporalEventInterval = 900f;       // Интервал временных событий

        [Header("Вероятности событий")]
        [SerializeField] private float globalEventChance = 0.3f;           // Шанс глобального события
        [SerializeField] private float personalEventChance = 0.5f;         // Шанс личного события
        [SerializeField] private float locationEventChance = 0.4f;         // Шанс локационного события
        [SerializeField] private float temporalEventChance = 0.6f;         // Шанс временного события
        [SerializeField] private float chainEventChance = 0.2f;            // Шанс цепочки событий

        [Header("Модификаторы")]
        [SerializeField] private float luckEventModifier = 0.1f;           // Влияние удачи на события
        [SerializeField] private float levelEventModifier = 0.05f;         // Влияние уровня на события
        [SerializeField] private float rareEventMultiplier = 0.1f;         // Множитель редких событий

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceEvents = false;
        [SerializeField] private RandomEventType debugEventType = RandomEventType.LuckyFind;

        // Состояние системы
        private Dictionary<RandomEventType, float> lastEventTime = new Dictionary<RandomEventType, float>();
        private List<RandomEvent> activeEvents = new List<RandomEvent>();
        private List<EventChain> activeEventChains = new List<EventChain>();
        private Dictionary<Location, List<RandomEvent>> locationEvents = new Dictionary<Location, List<RandomEvent>>();

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;
        private NotificationSystem notificationSystem;
        private FriendSystem friendSystem;
        private TransportSystem transportSystem;

        // События
        public event Action<RandomEvent> OnRandomEventTriggered;
        public event Action<RandomEvent> OnEventCompleted;
        public event Action<EventChain> OnEventChainStarted;
        public event Action<EventChain> OnEventChainCompleted;
        public event Action<RandomEventType> OnGlobalEventStarted;

        // Свойства
        public List<RandomEvent> ActiveEvents => new List<RandomEvent>(activeEvents);
        public List<EventChain> ActiveEventChains => new List<EventChain>(activeEventChains);
        public int TotalEventsTriggered { get; private set; } = 0;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = FindObjectOfType<SkillSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
            friendSystem = FindObjectOfType<FriendSystem>();
            transportSystem = FindObjectOfType<TransportSystem>();
        }

        private void Start()
        {
            if (enableRandomEvents)
            {
                InitializeEventSystem();
                SubscribeToEvents();
            }

            Debug.Log("RandomEventManager инициализирован");
        }

        private void Update()
        {
            if (enableRandomEvents)
            {
                UpdateEventSystem();
                UpdateActiveEvents();
                UpdateEventChains();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация системы событий
        /// </summary>
        private void InitializeEventSystem()
        {
            // Инициализировать время последних событий
            foreach (RandomEventType eventType in Enum.GetValues(typeof(RandomEventType)))
            {
                lastEventTime[eventType] = Time.time;
            }

            // Инициализировать локационные события
            foreach (Location location in Enum.GetValues(typeof(Location)))
            {
                locationEvents[location] = new List<RandomEvent>();
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (locationManager != null)
            {
                locationManager.OnLocationChanged += OnLocationChanged;
            }

            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimePeriodChanged += OnTimePeriodChanged;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (locationManager != null)
            {
                locationManager.OnLocationChanged -= OnLocationChanged;
            }

            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimePeriodChanged -= OnTimePeriodChanged;
            }
        }

        #endregion

        #region Event System Update

        /// <summary>
        /// Обновить систему событий
        /// </summary>
        private void UpdateEventSystem()
        {
            CheckGlobalEvents();
            CheckPersonalEvents();
            CheckLocationEvents();
            CheckTemporalEvents();
        }

        /// <summary>
        /// Проверить глобальные события
        /// </summary>
        private void CheckGlobalEvents()
        {
            if (!ShouldCheckEventType(RandomEventType.EconomicCrisis, globalEventInterval))
                return;

            if (forceEvents || UnityEngine.Random.value < globalEventChance)
            {
                TriggerGlobalEvent();
            }
        }

        /// <summary>
        /// Проверить личные события
        /// </summary>
        private void CheckPersonalEvents()
        {
            if (!ShouldCheckEventType(RandomEventType.LuckyFind, personalEventInterval))
                return;

            float actualChance = CalculatePersonalEventChance();
            
            if (forceEvents || UnityEngine.Random.value < actualChance)
            {
                TriggerPersonalEvent();
            }
        }

        /// <summary>
        /// Проверить локационные события
        /// </summary>
        private void CheckLocationEvents()
        {
            if (!ShouldCheckEventType(RandomEventType.NewBusiness, locationEventInterval))
                return;

            if (forceEvents || UnityEngine.Random.value < locationEventChance)
            {
                TriggerLocationEvent();
            }
        }

        /// <summary>
        /// Проверить временные события
        /// </summary>
        private void CheckTemporalEvents()
        {
            if (!ShouldCheckEventType(RandomEventType.CommunityEvent, temporalEventInterval))
                return;

            if (forceEvents || UnityEngine.Random.value < temporalEventChance)
            {
                TriggerTemporalEvent();
            }
        }

        #endregion

        #region Global Events

        /// <summary>
        /// Запустить глобальное событие
        /// </summary>
        private void TriggerGlobalEvent()
        {
            RandomEventType[] globalEvents = {
                RandomEventType.EconomicCrisis,
                RandomEventType.EconomicBoom,
                RandomEventType.WeatherDisaster,
                RandomEventType.PoliticalChange
            };

            RandomEventType eventType = globalEvents[UnityEngine.Random.Range(0, globalEvents.Length)];
            var randomEvent = CreateGlobalEvent(eventType);
            
            ExecuteEvent(randomEvent);
            lastEventTime[eventType] = Time.time;

            OnGlobalEventStarted?.Invoke(eventType);

            if (showDebugInfo)
            {
                Debug.Log($"Глобальное событие: {eventType}");
            }
        }

        /// <summary>
        /// Создать глобальное событие
        /// </summary>
        private RandomEvent CreateGlobalEvent(RandomEventType eventType)
        {
            var randomEvent = new RandomEvent
            {
                eventType = eventType,
                title = GetEventTitle(eventType),
                description = GetEventDescription(eventType),
                isGlobal = true,
                duration = GetEventDuration(eventType),
                startTime = Time.time,
                isInstant = false
            };

            switch (eventType)
            {
                case RandomEventType.EconomicCrisis:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoneyModifier, -0.3f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.JobAvailability, -0.5f, randomEvent.duration));
                    break;

                case RandomEventType.EconomicBoom:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoneyModifier, 0.5f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.JobAvailability, 0.3f, randomEvent.duration));
                    break;

                case RandomEventType.WeatherDisaster:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.HealthModifier, -0.2f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoodModifier, -0.3f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.TransportDisruption, -0.7f, randomEvent.duration));
                    break;

                case RandomEventType.PoliticalChange:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.LawChange, 1f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.PoliceActivity, 0.3f, randomEvent.duration));
                    break;
            }

            return randomEvent;
        }

        #endregion

        #region Personal Events

        /// <summary>
        /// Запустить личное событие
        /// </summary>
        private void TriggerPersonalEvent()
        {
            RandomEventType[] personalEvents = {
                RandomEventType.LuckyFind,
                RandomEventType.UnexpectedMeeting,
                RandomEventType.JobOffer,
                RandomEventType.Investment,
                RandomEventType.Inheritance,
                RandomEventType.Accident
            };

            // Учесть удачу игрока при выборе события
            RandomEventType eventType = SelectEventByLuck(personalEvents);
            var randomEvent = CreatePersonalEvent(eventType);
            
            ExecuteEvent(randomEvent);
            lastEventTime[eventType] = Time.time;

            if (showDebugInfo)
            {
                Debug.Log($"Личное событие: {eventType}");
            }
        }

        /// <summary>
        /// Создать личное событие
        /// </summary>
        private RandomEvent CreatePersonalEvent(RandomEventType eventType)
        {
            var randomEvent = new RandomEvent
            {
                eventType = eventType,
                title = GetEventTitle(eventType),
                description = GetEventDescription(eventType),
                isGlobal = false,
                duration = 0f,
                startTime = Time.time,
                isInstant = true
            };

            switch (eventType)
            {
                case RandomEventType.LuckyFind:
                    ProcessLuckyFind(randomEvent);
                    break;

                case RandomEventType.UnexpectedMeeting:
                    ProcessUnexpectedMeeting(randomEvent);
                    break;

                case RandomEventType.JobOffer:
                    ProcessJobOffer(randomEvent);
                    break;

                case RandomEventType.Investment:
                    ProcessInvestmentOpportunity(randomEvent);
                    break;

                case RandomEventType.Inheritance:
                    ProcessInheritance(randomEvent);
                    break;

                case RandomEventType.Accident:
                    ProcessAccident(randomEvent);
                    break;
            }

            return randomEvent;
        }

        /// <summary>
        /// Обработать счастливую находку
        /// </summary>
        private void ProcessLuckyFind(RandomEvent randomEvent)
        {
            float luckLevel = GetPlayerLuck();
            double baseAmount = 50;
            double foundMoney = baseAmount * (1 + luckLevel * 0.1f) * UnityEngine.Random.Range(0.5f, 2f);

            if (moneySystem != null)
            {
                moneySystem.AddMoney(foundMoney, "Счастливая находка");
            }

            randomEvent.rewards.Add($"Найдено: ${foundMoney:F0}");
            randomEvent.description += $" Вы нашли ${foundMoney:F0}!";
        }

        /// <summary>
        /// Обработать неожиданную встречу
        /// </summary>
        private void ProcessUnexpectedMeeting(RandomEvent randomEvent)
        {
            if (friendSystem != null && friendSystem.CanMakeNewFriends)
            {
                // Добавить случайного друга
                Location currentLocation = locationManager?.CurrentLocation ?? Location.Street;
                string[] names = { "Александр", "Елена", "Михаил", "Анна", "Сергей", "Ольга" };
                string randomName = names[UnityEngine.Random.Range(0, names.Length)];
                
                FriendType[] friendTypes = { FriendType.Helper, FriendType.Mentor, FriendType.BusinessPartner };
                FriendType randomType = friendTypes[UnityEngine.Random.Range(0, friendTypes.Length)];

                if (friendSystem.AddFriend(randomName, randomType, currentLocation))
                {
                    randomEvent.rewards.Add($"Новый знакомый: {randomName}");
                    randomEvent.description += $" Вы познакомились с {randomName}!";
                }
            }
        }

        /// <summary>
        /// Обработать предложение работы
        /// </summary>
        private void ProcessJobOffer(RandomEvent randomEvent)
        {
            // Временный бонус к заработку
            if (playerStats != null)
            {
                float duration = 7200f; // 2 часа
                playerStats.AddStatModifier(new StatModifier(
                    StatType.EarningsMultiplier, 1.5f, ModifierOperation.Multiply, 
                    "Временная работа", duration));
                
                randomEvent.rewards.Add("Бонус к заработку +50% на 2 часа");
                randomEvent.description += " Вам предложили временную работу!";
            }
        }

        /// <summary>
        /// Обработать инвестиционную возможность
        /// </summary>
        private void ProcessInvestmentOpportunity(RandomEvent randomEvent)
        {
            double investmentAmount = 200;
            
            if (moneySystem != null && moneySystem.CanAfford(investmentAmount))
            {
                // Предложить инвестицию
                randomEvent.choices.Add(new EventChoice
                {
                    text = $"Инвестировать ${investmentAmount}",
                    cost = investmentAmount,
                    successChance = 0.7f,
                    successReward = investmentAmount * 1.5,
                    failurePenalty = investmentAmount
                });

                randomEvent.choices.Add(new EventChoice
                {
                    text = "Отказаться",
                    cost = 0,
                    successChance = 1f,
                    successReward = 0,
                    failurePenalty = 0
                });

                randomEvent.description += $" Возможность инвестировать ${investmentAmount} с шансом получить прибыль!";
                randomEvent.requiresChoice = true;
            }
        }

        /// <summary>
        /// Обработать наследство
        /// </summary>
        private void ProcessInheritance(RandomEvent randomEvent)
        {
            double inheritanceAmount = UnityEngine.Random.Range(500f, 2000f);
            
            if (moneySystem != null)
            {
                moneySystem.AddMoney(inheritanceAmount, "Наследство");
            }

            randomEvent.rewards.Add($"Наследство: ${inheritanceAmount:F0}");
            randomEvent.description += $" Вы получили наследство ${inheritanceAmount:F0}!";
        }

        /// <summary>
        /// Обработать несчастный случай
        /// </summary>
        private void ProcessAccident(RandomEvent randomEvent)
        {
            if (playerStats != null)
            {
                float healthLoss = UnityEngine.Random.Range(10f, 30f);
                playerStats.ChangeHealth(-healthLoss);
                
                float moodLoss = UnityEngine.Random.Range(15f, 25f);
                playerStats.ChangeMood(-moodLoss);

                randomEvent.penalties.Add($"Здоровье -{healthLoss:F0}");
                randomEvent.penalties.Add($"Настроение -{moodLoss:F0}");
                randomEvent.description += " Вы попали в неприятную ситуацию...";
            }
        }

        #endregion

        #region Location Events

        /// <summary>
        /// Запустить локационное событие
        /// </summary>
        private void TriggerLocationEvent()
        {
            Location currentLocation = locationManager?.CurrentLocation ?? Location.Street;
            RandomEventType eventType = GetLocationEventType(currentLocation);
            
            var randomEvent = CreateLocationEvent(eventType, currentLocation);
            ExecuteEvent(randomEvent);
            
            // Добавить в локационные события
            locationEvents[currentLocation].Add(randomEvent);
            
            lastEventTime[eventType] = Time.time;

            if (showDebugInfo)
            {
                Debug.Log($"Локационное событие: {eventType} в {currentLocation}");
            }
        }

        /// <summary>
        /// Получить тип события для локации
        /// </summary>
        private RandomEventType GetLocationEventType(Location location)
        {
            switch (location)
            {
                case Location.BusinessDistrict:
                    return UnityEngine.Random.value < 0.6f ? 
                        RandomEventType.NewBusiness : RandomEventType.AreaUpgrade;
                        
                case Location.Shop:
                    return RandomEventType.SpecialSale;
                    
                case Location.Industrial:
                case Location.Abandoned:
                    return UnityEngine.Random.value < 0.7f ? 
                        RandomEventType.AreaDecline : RandomEventType.NewBusiness;
                        
                default:
                    return RandomEventType.AreaUpgrade;
            }
        }

        /// <summary>
        /// Создать локационное событие
        /// </summary>
        private RandomEvent CreateLocationEvent(RandomEventType eventType, Location location)
        {
            var randomEvent = new RandomEvent
            {
                eventType = eventType,
                title = GetEventTitle(eventType),
                description = GetLocationEventDescription(eventType, location),
                isGlobal = false,
                location = location,
                duration = GetEventDuration(eventType),
                startTime = Time.time,
                isInstant = eventType == RandomEventType.SpecialSale
            };

            switch (eventType)
            {
                case RandomEventType.NewBusiness:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.JobAvailability, 0.2f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.LocationSafety, 0.1f, randomEvent.duration));
                    break;

                case RandomEventType.AreaUpgrade:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.LocationSafety, 0.3f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.LocationComfort, 0.2f, randomEvent.duration));
                    break;

                case RandomEventType.AreaDecline:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.LocationSafety, -0.2f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.CrimeRate, 0.3f, randomEvent.duration));
                    break;

                case RandomEventType.SpecialSale:
                    ProcessSpecialSale(randomEvent);
                    break;
            }

            return randomEvent;
        }

        /// <summary>
        /// Обработать специальную распродажу
        /// </summary>
        private void ProcessSpecialSale(RandomEvent randomEvent)
        {
            // Скидка на покупки в текущей локации
            randomEvent.effects.Add(new EventEffect(EventEffectType.ShopDiscount, 0.3f, 3600f)); // 1 час
            randomEvent.description += " Скидка 30% на товары в течение часа!";
        }

        #endregion

        #region Temporal Events

        /// <summary>
        /// Запустить временное событие
        /// </summary>
        private void TriggerTemporalEvent()
        {
            TimePeriod currentPeriod = timeOfDayManager?.CurrentTimePeriod ?? TimePeriod.Day;
            RandomEventType eventType = GetTemporalEventType(currentPeriod);
            
            var randomEvent = CreateTemporalEvent(eventType, currentPeriod);
            ExecuteEvent(randomEvent);
            
            lastEventTime[eventType] = Time.time;

            if (showDebugInfo)
            {
                Debug.Log($"Временное событие: {eventType} в {currentPeriod}");
            }
        }

        /// <summary>
        /// Получить тип временного события
        /// </summary>
        private RandomEventType GetTemporalEventType(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.Morning:
                case TimePeriod.Day:
                    return UnityEngine.Random.value < 0.5f ? 
                        RandomEventType.CommunityEvent : RandomEventType.Celebration;
                        
                case TimePeriod.Evening:
                    return RandomEventType.Festival;
                    
                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    return RandomEventType.Protest;
                    
                default:
                    return RandomEventType.CommunityEvent;
            }
        }

        /// <summary>
        /// Создать временное событие
        /// </summary>
        private RandomEvent CreateTemporalEvent(RandomEventType eventType, TimePeriod period)
        {
            var randomEvent = new RandomEvent
            {
                eventType = eventType,
                title = GetEventTitle(eventType),
                description = GetTemporalEventDescription(eventType, period),
                isGlobal = false,
                duration = GetEventDuration(eventType),
                startTime = Time.time,
                isInstant = false
            };

            switch (eventType)
            {
                case RandomEventType.CommunityEvent:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoodModifier, 0.2f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.SocialBonus, 0.3f, randomEvent.duration));
                    break;

                case RandomEventType.Celebration:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoodModifier, 0.3f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoneyModifier, 0.1f, randomEvent.duration));
                    break;

                case RandomEventType.Protest:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.PoliceActivity, 0.5f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.TransportDisruption, -0.3f, randomEvent.duration));
                    break;

                case RandomEventType.Festival:
                    randomEvent.effects.Add(new EventEffect(EventEffectType.MoodModifier, 0.4f, randomEvent.duration));
                    randomEvent.effects.Add(new EventEffect(EventEffectType.SocialBonus, 0.5f, randomEvent.duration));
                    break;
            }

            return randomEvent;
        }

        #endregion

        #region Event Chain System

        /// <summary>
        /// Проверить цепочки событий
        /// </summary>
        private void CheckEventChains()
        {
            if (UnityEngine.Random.value < chainEventChance)
            {
                TriggerEventChain();
            }
        }

        /// <summary>
        /// Запустить цепочку событий
        /// </summary>
        private void TriggerEventChain()
        {
            var eventChain = CreateRandomEventChain();
            if (eventChain != null)
            {
                activeEventChains.Add(eventChain);
                OnEventChainStarted?.Invoke(eventChain);
                
                // Запустить первое событие в цепочке
                if (eventChain.events.Count > 0)
                {
                    ExecuteEvent(eventChain.events[0]);
                    eventChain.currentEventIndex = 0;
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Началась цепочка событий: {eventChain.name}");
                }
            }
        }

        /// <summary>
        /// Создать случайную цепочку событий
        /// </summary>
        private EventChain CreateRandomEventChain()
        {
            string[] chainNames = { 
                "Путь к успеху", "Череда неудач", "Новые возможности", 
                "Испытания судьбы", "Счастливая полоса" 
            };

            var eventChain = new EventChain
            {
                name = chainNames[UnityEngine.Random.Range(0, chainNames.Length)],
                events = new List<RandomEvent>(),
                currentEventIndex = -1,
                isCompleted = false
            };

            // Создать 2-4 связанных события
            int eventCount = UnityEngine.Random.Range(2, 5);
            for (int i = 0; i < eventCount; i++)
            {
                var chainEvent = CreateChainEvent(i, eventCount);
                eventChain.events.Add(chainEvent);
            }

            return eventChain;
        }

        /// <summary>
        /// Создать событие цепочки
        /// </summary>
        private RandomEvent CreateChainEvent(int index, int totalEvents)
        {
            RandomEventType[] chainEventTypes = {
                RandomEventType.UnexpectedMeeting,
                RandomEventType.JobOffer,
                RandomEventType.Investment,
                RandomEventType.LuckyFind
            };

            RandomEventType eventType = chainEventTypes[UnityEngine.Random.Range(0, chainEventTypes.Length)];
            
            var chainEvent = CreatePersonalEvent(eventType);
            chainEvent.title = $"Глава {index + 1}: {chainEvent.title}";
            chainEvent.isChainEvent = true;
            chainEvent.chainIndex = index;
            chainEvent.totalChainEvents = totalEvents;

            return chainEvent;
        }

        /// <summary>
        /// Обновить цепочки событий
        /// </summary>
        private void UpdateEventChains()
        {
            for (int i = activeEventChains.Count - 1; i >= 0; i--)
            {
                var eventChain = activeEventChains[i];
                
                if (eventChain.isCompleted)
                {
                    OnEventChainCompleted?.Invoke(eventChain);
                    activeEventChains.RemoveAt(i);
                    continue;
                }

                // Проверить, можно ли запустить следующее событие
                if (CanTriggerNextChainEvent(eventChain))
                {
                    TriggerNextChainEvent(eventChain);
                }
            }
        }

        /// <summary>
        /// Можно ли запустить следующее событие в цепочке
        /// </summary>
        private bool CanTriggerNextChainEvent(EventChain eventChain)
        {
            if (eventChain.currentEventIndex >= eventChain.events.Count - 1)
                return false;

            var currentEvent = eventChain.events[eventChain.currentEventIndex];
            return currentEvent.isCompleted && Time.time - currentEvent.completionTime >= 300f; // 5 минут
        }

        /// <summary>
        /// Запустить следующее событие в цепочке
        /// </summary>
        private void TriggerNextChainEvent(EventChain eventChain)
        {
            eventChain.currentEventIndex++;
            
            if (eventChain.currentEventIndex < eventChain.events.Count)
            {
                var nextEvent = eventChain.events[eventChain.currentEventIndex];
                ExecuteEvent(nextEvent);
            }
            else
            {
                eventChain.isCompleted = true;
            }
        }

        #endregion

        #region Event Execution

        /// <summary>
        /// Выполнить событие
        /// </summary>
        private void ExecuteEvent(RandomEvent randomEvent)
        {
            activeEvents.Add(randomEvent);
            TotalEventsTriggered++;

            // Применить мгновенные эффекты
            if (randomEvent.isInstant)
            {
                ApplyEventEffects(randomEvent);
                randomEvent.isCompleted = true;
                randomEvent.completionTime = Time.time;
            }
            else
            {
                // Запустить продолжительное событие
                StartCoroutine(ProcessDurationEvent(randomEvent));
            }

            // Показать уведомление
            ShowEventNotification(randomEvent);

            OnRandomEventTriggered?.Invoke(randomEvent);

            // Проверить возможность цепочки событий
            if (UnityEngine.Random.value < chainEventChance && !randomEvent.isChainEvent)
            {
                CheckEventChains();
            }
        }

        /// <summary>
        /// Обработать продолжительное событие
        /// </summary>
        private IEnumerator ProcessDurationEvent(RandomEvent randomEvent)
        {
            ApplyEventEffects(randomEvent);
            
            yield return new WaitForSeconds(randomEvent.duration);
            
            RemoveEventEffects(randomEvent);
            randomEvent.isCompleted = true;
            randomEvent.completionTime = Time.time;
            
            OnEventCompleted?.Invoke(randomEvent);
        }

        /// <summary>
        /// Применить эффекты события
        /// </summary>
        private void ApplyEventEffects(RandomEvent randomEvent)
        {
            foreach (var effect in randomEvent.effects)
            {
                ApplyEventEffect(effect, randomEvent);
            }
        }

        /// <summary>
        /// Применить эффект события
        /// </summary>
        private void ApplyEventEffect(EventEffect effect, RandomEvent randomEvent)
        {
            switch (effect.type)
            {
                case EventEffectType.MoneyModifier:
                    if (playerStats != null)
                    {
                        string sourceName = $"Событие: {randomEvent.title}";
                        playerStats.AddStatModifier(new StatModifier(
                            StatType.EarningsMultiplier, 1f + effect.value, ModifierOperation.Multiply,
                            sourceName, effect.duration));
                    }
                    break;

                case EventEffectType.HealthModifier:
                    if (playerStats != null)
                    {
                        string sourceName = $"Событие: {randomEvent.title}";
                        float healthChange = playerStats.MaxHealth * effect.value;
                        playerStats.AddStatModifier(new StatModifier(
                            StatType.Health, healthChange, ModifierOperation.Add,
                            sourceName, effect.duration));
                    }
                    break;

                case EventEffectType.MoodModifier:
                    if (playerStats != null)
                    {
                        string sourceName = $"Событие: {randomEvent.title}";
                        playerStats.AddStatModifier(new StatModifier(
                            StatType.MoodRegen, effect.value * 10f, ModifierOperation.Add,
                            sourceName, effect.duration));
                    }
                    break;

                case EventEffectType.LuckModifier:
                    if (playerStats != null)
                    {
                        string sourceName = $"Событие: {randomEvent.title}";
                        playerStats.AddStatModifier(new StatModifier(
                            StatType.LuckModifier, effect.value * 20f, ModifierOperation.Add,
                            sourceName, effect.duration));
                    }
                    break;

                // TODO: Добавить обработку других типов эффектов
            }
        }

        /// <summary>
        /// Удалить эффекты события
        /// </summary>
        private void RemoveEventEffects(RandomEvent randomEvent)
        {
            if (playerStats != null)
            {
                string sourceName = $"Событие: {randomEvent.title}";
                playerStats.RemoveStatModifiersBySource(sourceName);
            }
        }

        #endregion

        #region Active Events Management

        /// <summary>
        /// Обновить активные события
        /// </summary>
        private void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var activeEvent = activeEvents[i];
                
                // Удалить завершенные мгновенные события
                if (activeEvent.isInstant && activeEvent.isCompleted && 
                    Time.time - activeEvent.completionTime > 60f) // Хранить 1 минуту
                {
                    activeEvents.RemoveAt(i);
                }
                // Удалить завершенные продолжительные события
                else if (!activeEvent.isInstant && activeEvent.isCompleted)
                {
                    activeEvents.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Проверить, нужно ли проверять тип события
        /// </summary>
        private bool ShouldCheckEventType(RandomEventType eventType, float interval)
        {
            if (!lastEventTime.ContainsKey(eventType))
                return true;

            return Time.time - lastEventTime[eventType] >= interval;
        }

        /// <summary>
        /// Рассчитать шанс личного события
        /// </summary>
        private float CalculatePersonalEventChance()
        {
            float chance = personalEventChance;
            
            // Бонус от удачи
            float luck = GetPlayerLuck();
            chance += luck * luckEventModifier;
            
            // Бонус от уровня
            if (playerStats != null)
            {
                chance += playerStats.CurrentLevel * levelEventModifier;
            }

            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Выбрать событие с учетом удачи
        /// </summary>
        private RandomEventType SelectEventByLuck(RandomEventType[] events)
        {
            float luck = GetPlayerLuck();
            
            // Положительные события с хорошей удачей
            if (luck > 7f)
            {
                var positiveEvents = new List<RandomEventType>();
                foreach (var eventType in events)
                {
                    if (IsPositiveEvent(eventType))
                        positiveEvents.Add(eventType);
                }
                
                if (positiveEvents.Count > 0)
                    return positiveEvents[UnityEngine.Random.Range(0, positiveEvents.Count)];
            }
            // Негативные события с плохой удачей
            else if (luck < 3f)
            {
                var negativeEvents = new List<RandomEventType>();
                foreach (var eventType in events)
                {
                    if (!IsPositiveEvent(eventType))
                        negativeEvents.Add(eventType);
                }
                
                if (negativeEvents.Count > 0)
                    return negativeEvents[UnityEngine.Random.Range(0, negativeEvents.Count)];
            }

            // Обычный случайный выбор
            return events[UnityEngine.Random.Range(0, events.Length)];
        }

        /// <summary>
        /// Является ли событие положительным
        /// </summary>
        private bool IsPositiveEvent(RandomEventType eventType)
        {
            switch (eventType)
            {
                case RandomEventType.LuckyFind:
                case RandomEventType.UnexpectedMeeting:
                case RandomEventType.JobOffer:
                case RandomEventType.Investment:
                case RandomEventType.Inheritance:
                case RandomEventType.EconomicBoom:
                case RandomEventType.Celebration:
                case RandomEventType.Festival:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Получить удачу игрока
        /// </summary>
        private float GetPlayerLuck()
        {
            if (skillSystem != null)
            {
                return skillSystem.GetSkillLevel(SkillType.Luck);
            }
            return 5f; // Базовое значение
        }

        /// <summary>
        /// Получить заголовок события
        /// </summary>
        private string GetEventTitle(RandomEventType eventType)
        {
            switch (eventType)
            {
                case RandomEventType.EconomicCrisis: return "Экономический кризис";
                case RandomEventType.EconomicBoom: return "Экономический подъем";
                case RandomEventType.WeatherDisaster: return "Непогода";
                case RandomEventType.PoliticalChange: return "Политические изменения";
                case RandomEventType.LuckyFind: return "Счастливая находка";
                case RandomEventType.UnexpectedMeeting: return "Неожиданная встреча";
                case RandomEventType.JobOffer: return "Предложение работы";
                case RandomEventType.Investment: return "Инвестиционная возможность";
                case RandomEventType.Inheritance: return "Наследство";
                case RandomEventType.Accident: return "Несчастный случай";
                case RandomEventType.NewBusiness: return "Новый бизнес";
                case RandomEventType.AreaUpgrade: return "Улучшение района";
                case RandomEventType.AreaDecline: return "Упадок района";
                case RandomEventType.SpecialSale: return "Распродажа";
                case RandomEventType.CommunityEvent: return "Общественное мероприятие";
                case RandomEventType.Celebration: return "Празднование";
                case RandomEventType.Protest: return "Протест";
                case RandomEventType.Festival: return "Фестиваль";
                default: return eventType.ToString();
            }
        }

        /// <summary>
        /// Получить описание события
        /// </summary>
        private string GetEventDescription(RandomEventType eventType)
        {
            switch (eventType)
            {
                case RandomEventType.EconomicCrisis: 
                    return "В стране начался экономический кризис. Работу найти стало сложнее, а доходы снизились.";
                case RandomEventType.EconomicBoom: 
                    return "Экономика на подъеме! Появилось больше рабочих мест и возможностей заработка.";
                case RandomEventType.WeatherDisaster: 
                    return "Плохая погода затрудняет передвижение и влияет на настроение.";
                case RandomEventType.LuckyFind: 
                    return "Вы нашли что-то ценное на улице.";
                case RandomEventType.UnexpectedMeeting: 
                    return "Случайно встретили интересного человека.";
                case RandomEventType.Accident: 
                    return "С вами произошел неприятный инцидент.";
                default: 
                    return $"Произошло событие: {eventType}";
            }
        }

        /// <summary>
        /// Получить описание локационного события
        /// </summary>
        private string GetLocationEventDescription(RandomEventType eventType, Location location)
        {
            string locationName = GetLocationName(location);
            
            switch (eventType)
            {
                case RandomEventType.NewBusiness:
                    return $"В районе {locationName} открылся новый бизнес, что улучшило обстановку.";
                case RandomEventType.AreaUpgrade:
                    return $"Район {locationName} благоустраивается - становится безопаснее и комфортнее.";
                case RandomEventType.AreaDecline:
                    return $"Обстановка в районе {locationName} ухудшается.";
                case RandomEventType.SpecialSale:
                    return $"В {locationName} проходит специальная распродажа!";
                default:
                    return GetEventDescription(eventType);
            }
        }

        /// <summary>
        /// Получить описание временного события
        /// </summary>
        private string GetTemporalEventDescription(RandomEventType eventType, TimePeriod period)
        {
            string timeText = GetTimePeriodName(period);
            
            switch (eventType)
            {
                case RandomEventType.CommunityEvent:
                    return $"В {timeText} проходит местное мероприятие.";
                case RandomEventType.Celebration:
                    return $"В {timeText} горожане что-то празднуют!";
                case RandomEventType.Protest:
                    return $"В {timeText} на улицах протестующие.";
                case RandomEventType.Festival:
                    return $"В {timeText} проходит фестиваль!";
                default:
                    return GetEventDescription(eventType);
            }
        }

        /// <summary>
        /// Получить название локации
        /// </summary>
        private string GetLocationName(Location location)
        {
            switch (location)
            {
                case Location.Street: return "на улице";
                case Location.Park: return "в парке";
                case Location.Shop: return "у магазина";
                case Location.BusinessDistrict: return "в деловом районе";
                case Location.Industrial: return "в промзоне";
                case Location.Abandoned: return "в заброшенном районе";
                case Location.Hospital: return "у больницы";
                case Location.University: return "у университета";
                case Location.Library: return "у библиотеки";
                case Location.JobCenter: return "у центра занятости";
                default: return location.ToString();
            }
        }

        /// <summary>
        /// Получить название периода времени
        /// </summary>
        private string GetTimePeriodName(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning: return "раннее утро";
                case TimePeriod.Morning: return "утром";
                case TimePeriod.Day: return "днем";
                case TimePeriod.Evening: return "вечером";
                case TimePeriod.Night: return "ночью";
                case TimePeriod.LateNight: return "поздней ночью";
                default: return period.ToString();
            }
        }

        /// <summary>
        /// Получить длительность события
        /// </summary>
        private float GetEventDuration(RandomEventType eventType)
        {
            switch (eventType)
            {
                case RandomEventType.EconomicCrisis:
                case RandomEventType.EconomicBoom:
                case RandomEventType.PoliticalChange:
                    return 86400f; // 1 день
                    
                case RandomEventType.WeatherDisaster:
                    return 21600f; // 6 часов
                    
                case RandomEventType.NewBusiness:
                case RandomEventType.AreaUpgrade:
                case RandomEventType.AreaDecline:
                    return 172800f; // 2 дня
                    
                case RandomEventType.CommunityEvent:
                case RandomEventType.Celebration:
                case RandomEventType.Festival:
                case RandomEventType.Protest:
                    return 7200f; // 2 часа
                    
                default:
                    return 0f; // Мгновенное событие
            }
        }

        /// <summary>
        /// Показать уведомление о событии
        /// </summary>
        private void ShowEventNotification(RandomEvent randomEvent)
        {
            if (notificationSystem == null)
                return;

            NotificationType notificationType = IsPositiveEvent(randomEvent.eventType) ? 
                NotificationType.Info : NotificationType.Warning;

            if (randomEvent.isGlobal)
                notificationType = NotificationType.Important;

            notificationSystem.ShowNotification(randomEvent.title, randomEvent.description, notificationType);
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public RandomEventManagerSaveData GetSaveData()
        {
            return new RandomEventManagerSaveData
            {
                lastEventTime = new Dictionary<RandomEventType, float>(lastEventTime),
                activeEvents = new List<RandomEvent>(activeEvents),
                activeEventChains = new List<EventChain>(activeEventChains),
                totalEventsTriggered = TotalEventsTriggered
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(RandomEventManagerSaveData data)
        {
            lastEventTime = data.lastEventTime ?? new Dictionary<RandomEventType, float>();
            activeEvents = data.activeEvents ?? new List<RandomEvent>();
            activeEventChains = data.activeEventChains ?? new List<EventChain>();
            TotalEventsTriggered = data.totalEventsTriggered;

            // Восстановить активные эффекты
            foreach (var activeEvent in activeEvents)
            {
                if (!activeEvent.isCompleted && !activeEvent.isInstant)
                {
                    float remainingTime = activeEvent.duration - (Time.time - activeEvent.startTime);
                    if (remainingTime > 0)
                    {
                        StartCoroutine(ProcessDurationEvent(activeEvent));
                    }
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"RandomEventManager загружен. Активных событий: {activeEvents.Count}, " +
                         $"Цепочек: {activeEventChains.Count}, Всего событий: {TotalEventsTriggered}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType oldLocation, LocationType newLocation)
        {
            // Проверить локационные события для новой локации
            if (locationEvents.ContainsKey((Location)newLocation))
            {
                var events = locationEvents[(Location)newLocation];
                foreach (var locationEvent in events)
                {
                    if (!locationEvent.isCompleted && 
                        Time.time - locationEvent.startTime < locationEvent.duration)
                    {
                        // Событие все еще активно в этой локации
                        if (notificationSystem != null)
                        {
                            notificationSystem.ShowNotification("Локальное событие", 
                                $"В этом районе: {locationEvent.title}", NotificationType.Info);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик смены времени суток
        /// </summary>
        private void OnTimePeriodChanged(TimePeriod newPeriod)
        {
            // Некоторые события могут завершаться со сменой времени
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Force Global Event")]
        private void DebugForceGlobalEvent()
        {
            TriggerGlobalEvent();
        }

        [ContextMenu("Force Personal Event")]
        private void DebugForcePersonalEvent()
        {
            TriggerPersonalEvent();
        }

        [ContextMenu("Force Event Chain")]
        private void DebugForceEventChain()
        {
            TriggerEventChain();
        }

        [ContextMenu("Force Specific Event")]
        private void DebugForceSpecificEvent()
        {
            var testEvent = CreatePersonalEvent(debugEventType);
            ExecuteEvent(testEvent);
        }

        [ContextMenu("Show Active Events")]
        private void DebugShowActiveEvents()
        {
            Debug.Log($"Активных событий: {activeEvents.Count}");
            Debug.Log($"Активных цепочек: {activeEventChains.Count}");
            Debug.Log($"Всего событий: {TotalEventsTriggered}");
            
            foreach (var activeEvent in activeEvents)
            {
                Debug.Log($"- {activeEvent.title}: {(activeEvent.isCompleted ? "Завершено" : "Активно")}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Класс случайного события
    /// </summary>
    [System.Serializable]
    public class RandomEvent
    {
        public RandomEventType eventType;       // Тип события
        public string title;                    // Заголовок
        public string description;              // Описание
        public bool isGlobal;                   // Глобальное событие
        public Location location;               // Локация (для локальных событий)
        public float duration;                  // Длительность
        public float startTime;                 // Время начала
        public float completionTime;            // Время завершения
        public bool isInstant;                  // Мгновенное событие
        public bool isCompleted;                // Завершено
        public bool requiresChoice;             // Требует выбора игрока
        
        // Цепочки событий
        public bool isChainEvent;               // Часть цепочки
        public int chainIndex;                  // Индекс в цепочке
        public int totalChainEvents;            // Общее количество в цепочке
        
        // Эффекты и результаты
        public List<EventEffect> effects = new List<EventEffect>();
        public List<EventChoice> choices = new List<EventChoice>();
        public List<string> rewards = new List<string>();
        public List<string> penalties = new List<string>();
    }

    /// <summary>
    /// Эффект события
    /// </summary>
    [System.Serializable]
    public class EventEffect
    {
        public EventEffectType type;            // Тип эффекта
        public float value;                     // Значение
        public float duration;                  // Длительность

        public EventEffect(EventEffectType effectType, float effectValue, float effectDuration)
        {
            type = effectType;
            value = effectValue;
            duration = effectDuration;
        }
    }

    /// <summary>
    /// Выбор в событии
    /// </summary>
    [System.Serializable]
    public class EventChoice
    {
        public string text;                     // Текст выбора
        public double cost;                     // Стоимость
        public float successChance;             // Шанс успеха
        public double successReward;            // Награда за успех
        public double failurePenalty;           // Штраф за неудачу
    }

    /// <summary>
    /// Цепочка событий
    /// </summary>
    [System.Serializable]
    public class EventChain
    {
        public string name;                     // Название цепочки
        public List<RandomEvent> events;        // События в цепочке
        public int currentEventIndex;           // Текущий индекс события
        public bool isCompleted;                // Завершена ли цепочка
    }

    /// <summary>
    /// Типы эффектов событий
    /// </summary>
    public enum EventEffectType
    {
        MoneyModifier,          // Модификатор дохода
        HealthModifier,         // Модификатор здоровья
        MoodModifier,           // Модификатор настроения
        LuckModifier,           // Модификатор удачи
        JobAvailability,        // Доступность работы
        LocationSafety,         // Безопасность локации
        LocationComfort,        // Комфорт локации
        TransportDisruption,    // Нарушение транспорта
        PoliceActivity,         // Активность полиции
        CrimeRate,              // Уровень преступности
        ShopDiscount,           // Скидка в магазинах
        SocialBonus,            // Социальный бонус
        LawChange               // Изменение законов
    }

    /// <summary>
    /// Данные RandomEventManager для сохранения
    /// </summary>
    [System.Serializable]
    public class RandomEventManagerSaveData
    {
        public Dictionary<RandomEventType, float> lastEventTime;
        public List<RandomEvent> activeEvents;
        public List<EventChain> activeEventChains;
        public int totalEventsTriggered;
    }
}
