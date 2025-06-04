using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система друзей и социальных отношений
    /// </summary>
    public class FriendSystem : MonoBehaviour
    {
        [Header("Настройки системы друзей")]
        [SerializeField] private bool enableFriendSystem = true;
        [SerializeField] private int maxFriends = 20;                  // Максимум друзей
        [SerializeField] private float relationshipDecayRate = 1f;      // Скорость ухудшения отношений
        [SerializeField] private float interactionCooldown = 3600f;     // Кулдаун взаимодействий (секунды)

        [Header("Экономические настройки")]
        [SerializeField] private double baseLoanAmount = 100;           // Базовая сумма займа
        [SerializeField] private double maxLoanAmount = 5000;           // Максимальная сумма займа
        [SerializeField] private float loanInterestRate = 0.1f;         // Процентная ставка
        [SerializeField] private double giftCostMultiplier = 10;        // Множитель стоимости подарков

        [Header("Настройки встреч")]
        [SerializeField] private float meetingChance = 0.05f;           // Шанс встретить нового человека
        [SerializeField] private float meetingInterval = 300f;          // Интервал проверки встреч
        [SerializeField] private bool allowBetrayals = true;            // Разрешить предательства

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool freeFriendship = false;
        [SerializeField] private bool instantMeeting = false;

        // Состояние системы
        private Dictionary<string, Friend> friends = new Dictionary<string, Friend>();
        private Dictionary<string, float> lastInteractionTime = new Dictionary<string, float>();
        private float lastMeetingCheck = 0f;
        private List<LoanData> activeLoans = new List<LoanData>();

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private NotificationSystem notificationSystem;
        private TimeOfDayManager timeOfDayManager;

        // События
        public event Action<Friend> OnFriendAdded;
        public event Action<Friend> OnFriendLost;
        public event Action<Friend, RelationshipLevel> OnRelationshipChanged;
        public event Action<Friend, double> OnLoanGiven;
        public event Action<Friend, double> OnLoanRepaid;
        public event Action<Friend> OnFriendBetrayed;
        public event Action<Friend, FriendHelp> OnFriendHelped;

        // Свойства
        public int FriendCount => friends.Count;
        public Dictionary<string, Friend> Friends => new Dictionary<string, Friend>(friends);
        public List<LoanData> ActiveLoans => new List<LoanData>(activeLoans);
        public bool CanMakeNewFriends => friends.Count < maxFriends;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = FindObjectOfType<SkillSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
        }

        private void Start()
        {
            if (enableFriendSystem)
            {
                SubscribeToEvents();
                lastMeetingCheck = Time.time;
            }

            Debug.Log("FriendSystem инициализирован");
        }

        private void Update()
        {
            if (enableFriendSystem)
            {
                UpdateRelationships();
                CheckNewMeetings();
                ProcessLoans();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (locationManager != null)
            {
                locationManager.OnLocationChanged += OnLocationChanged;
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
        }

        #endregion

        #region Friend Management

        /// <summary>
        /// Добавить друга
        /// </summary>
        public bool AddFriend(string name, FriendType type, Location metAt)
        {
            if (friends.ContainsKey(name))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Друг {name} уже существует");
                }
                return false;
            }

            if (!CanMakeNewFriends)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("Достигнут максимум друзей");
                }
                return false;
            }

            var friend = CreateFriend(name, type, metAt);
            friends[name] = friend;
            lastInteractionTime[name] = Time.time;

            OnFriendAdded?.Invoke(friend);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Новый знакомый", 
                    $"Вы познакомились с {name}", NotificationType.Info);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Добавлен друг: {name} ({type})");
            }

            return true;
        }

        /// <summary>
        /// Удалить друга
        /// </summary>
        public bool RemoveFriend(string name, string reason = "")
        {
            if (!friends.ContainsKey(name))
                return false;

            var friend = friends[name];
            friends.Remove(name);
            lastInteractionTime.Remove(name);

            // Удалить связанные займы
            activeLoans.RemoveAll(loan => loan.friendName == name);

            OnFriendLost?.Invoke(friend);

            if (notificationSystem != null)
            {
                string message = !string.IsNullOrEmpty(reason) ? 
                    $"Вы потеряли друга {name}: {reason}" : 
                    $"Вы больше не дружите с {name}";
                notificationSystem.ShowNotification("Потеря друга", message, NotificationType.Warning);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Удален друг: {name}. Причина: {reason}");
            }

            return true;
        }

        /// <summary>
        /// Создать друга
        /// </summary>
        private Friend CreateFriend(string name, FriendType type, Location metAt)
        {
            var friend = new Friend
            {
                name = name,
                type = type,
                relationship = RelationshipLevel.Neutral,
                loyalty = 50f,
                trust = 30f,
                metAt = metAt,
                meetingTime = Time.time,
                lastContact = Time.time,
                helpCount = 0,
                totalFavors = 0,
                isAvailable = true,
                currentLocation = metAt
            };

            // Установить базовые характеристики в зависимости от типа
            SetFriendBaseStats(friend);

            return friend;
        }

        /// <summary>
        /// Установить базовые характеристики друга
        /// </summary>
        private void SetFriendBaseStats(Friend friend)
        {
            switch (friend.type)
            {
                case FriendType.Helper:
                    friend.loyalty = 60f;
                    friend.trust = 50f;
                    friend.helpfulness = 80f;
                    friend.reliability = 70f;
                    break;

                case FriendType.Teacher:
                    friend.loyalty = 70f;
                    friend.trust = 80f;
                    friend.helpfulness = 90f;
                    friend.reliability = 85f;
                    friend.knowledgeLevel = 90f;
                    break;

                case FriendType.BusinessPartner:
                    friend.loyalty = 50f;
                    friend.trust = 60f;
                    friend.helpfulness = 70f;
                    friend.reliability = 80f;
                    friend.wealthLevel = 80f;
                    break;

                case FriendType.Protector:
                    friend.loyalty = 80f;
                    friend.trust = 70f;
                    friend.helpfulness = 60f;
                    friend.reliability = 90f;
                    friend.strengthLevel = 90f;
                    break;

                case FriendType.Informant:
                    friend.loyalty = 40f;
                    friend.trust = 30f;
                    friend.helpfulness = 50f;
                    friend.reliability = 60f;
                    friend.knowledgeLevel = 80f;
                    break;

                case FriendType.Criminal:
                    friend.loyalty = 30f;
                    friend.trust = 20f;
                    friend.helpfulness = 40f;
                    friend.reliability = 50f;
                    friend.dangerLevel = 80f;
                    break;

                default:
                    friend.loyalty = 50f;
                    friend.trust = 40f;
                    friend.helpfulness = 60f;
                    friend.reliability = 60f;
                    break;
            }

            // Случайные вариации ±20%
            friend.loyalty *= UnityEngine.Random.Range(0.8f, 1.2f);
            friend.trust *= UnityEngine.Random.Range(0.8f, 1.2f);
            friend.helpfulness *= UnityEngine.Random.Range(0.8f, 1.2f);
            friend.reliability *= UnityEngine.Random.Range(0.8f, 1.2f);

            // Ограничить значения
            friend.loyalty = Mathf.Clamp(friend.loyalty, 0f, 100f);
            friend.trust = Mathf.Clamp(friend.trust, 0f, 100f);
            friend.helpfulness = Mathf.Clamp(friend.helpfulness, 0f, 100f);
            friend.reliability = Mathf.Clamp(friend.reliability, 0f, 100f);
        }

        #endregion

        #region Relationship Management

        /// <summary>
        /// Обновить отношения
        /// </summary>
        private void UpdateRelationships()
        {
            var friendsToUpdate = new List<string>(friends.Keys);
            
            foreach (string name in friendsToUpdate)
            {
                if (friends.ContainsKey(name))
                {
                    UpdateFriendRelationship(name);
                }
            }
        }

        /// <summary>
        /// Обновить отношения с другом
        /// </summary>
        private void UpdateFriendRelationship(string name)
        {
            var friend = friends[name];
            
            // Естественное ухудшение отношений со временем
            float timeSinceContact = Time.time - friend.lastContact;
            float decayAmount = (timeSinceContact / 86400f) * relationshipDecayRate; // За день
            
            friend.loyalty = Mathf.Max(0, friend.loyalty - decayAmount);
            friend.trust = Mathf.Max(0, friend.trust - decayAmount * 0.5f);

            // Обновить уровень отношений
            RelationshipLevel oldLevel = friend.relationship;
            friend.relationship = CalculateRelationshipLevel(friend);
            
            if (oldLevel != friend.relationship)
            {
                OnRelationshipChanged?.Invoke(friend, friend.relationship);
                
                if (friend.relationship < oldLevel)
                {
                    // Отношения ухудшились
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Отношения", 
                            $"Отношения с {name} ухудшились: {friend.relationship}", 
                            NotificationType.Warning);
                    }
                }
            }

            // Проверить потерю друга
            if (friend.relationship == RelationshipLevel.Enemy || friend.loyalty <= 0)
            {
                RemoveFriend(name, "Отношения испортились");
            }

            // Проверить предательство
            if (allowBetrayals && CheckForBetrayal(friend))
            {
                ProcessBetrayal(friend);
            }
        }

        /// <summary>
        /// Рассчитать уровень отношений
        /// </summary>
        private RelationshipLevel CalculateRelationshipLevel(Friend friend)
        {
            float averageScore = (friend.loyalty + friend.trust) / 2f;
            
            if (averageScore >= 90f) return RelationshipLevel.Family;
            if (averageScore >= 80f) return RelationshipLevel.Trusted;
            if (averageScore >= 70f) return RelationshipLevel.Best;
            if (averageScore >= 60f) return RelationshipLevel.Good;
            if (averageScore >= 50f) return RelationshipLevel.Friendly;
            if (averageScore >= 40f) return RelationshipLevel.Neutral;
            if (averageScore >= 20f) return RelationshipLevel.Unfriendly;
            if (averageScore >= 10f) return RelationshipLevel.Hostile;
            return RelationshipLevel.Enemy;
        }

        /// <summary>
        /// Взаимодействовать с другом
        /// </summary>
        public bool InteractWithFriend(string name, FriendInteraction interaction)
        {
            if (!friends.ContainsKey(name))
                return false;

            if (!CanInteractWithFriend(name))
                return false;

            var friend = friends[name];
            bool success = false;

            switch (interaction)
            {
                case FriendInteraction.Chat:
                    success = ChatWithFriend(friend);
                    break;
                case FriendInteraction.GiveGift:
                    success = GiveGiftToFriend(friend);
                    break;
                case FriendInteraction.AskForHelp:
                    success = AskFriendForHelp(friend);
                    break;
                case FriendInteraction.RequestLoan:
                    success = RequestLoanFromFriend(friend);
                    break;
                case FriendInteraction.Betray:
                    success = BetrayFriend(friend);
                    break;
            }

            if (success)
            {
                friend.lastContact = Time.time;
                lastInteractionTime[name] = Time.time;
            }

            return success;
        }

        /// <summary>
        /// Поговорить с другом
        /// </summary>
        private bool ChatWithFriend(Friend friend)
        {
            // Улучшить отношения
            float loyaltyGain = UnityEngine.Random.Range(2f, 8f);
            float trustGain = UnityEngine.Random.Range(1f, 5f);

            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                loyaltyGain += charisma * 0.2f;
                trustGain += charisma * 0.1f;
            }

            friend.loyalty = Mathf.Min(100f, friend.loyalty + loyaltyGain);
            friend.trust = Mathf.Min(100f, friend.trust + trustGain);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Общение", 
                    $"Хорошо пообщались с {friend.name}", NotificationType.Info);
            }

            return true;
        }

        /// <summary>
        /// Подарить подарок другу
        /// </summary>
        private bool GiveGiftToFriend(Friend friend)
        {
            double giftCost = CalculateGiftCost(friend);
            
            if (freeFriendship)
                giftCost = 0;

            if (moneySystem == null || !moneySystem.CanAfford(giftCost))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Недостаточно денег", 
                        $"Нужно ${giftCost} для подарка", NotificationType.Warning);
                }
                return false;
            }

            if (giftCost > 0)
            {
                moneySystem.SpendMoney(giftCost, $"Подарок для {friend.name}");
            }

            // Значительное улучшение отношений
            float loyaltyGain = UnityEngine.Random.Range(10f, 20f);
            float trustGain = UnityEngine.Random.Range(5f, 15f);

            friend.loyalty = Mathf.Min(100f, friend.loyalty + loyaltyGain);
            friend.trust = Mathf.Min(100f, friend.trust + trustGain);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Подарок", 
                    $"Подарили подарок {friend.name} за ${giftCost}", NotificationType.Info);
            }

            return true;
        }

        #endregion

        #region Friend Help System

        /// <summary>
        /// Попросить помощи у друга
        /// </summary>
        private bool AskFriendForHelp(Friend friend)
        {
            if (!CanFriendHelp(friend))
                return false;

            FriendHelp helpType = DeterminePossibleHelp(friend);
            bool success = ProvideFriendHelp(friend, helpType);

            if (success)
            {
                friend.helpCount++;
                friend.totalFavors++;
                
                // Снизить готовность помочь
                friend.helpfulness = Mathf.Max(20f, friend.helpfulness - 10f);
                
                OnFriendHelped?.Invoke(friend, helpType);
            }

            return success;
        }

        /// <summary>
        /// Определить возможную помощь
        /// </summary>
        private FriendHelp DeterminePossibleHelp(Friend friend)
        {
            var possibleHelp = new List<FriendHelp>();

            switch (friend.type)
            {
                case FriendType.Helper:
                    possibleHelp.AddRange(new[] { FriendHelp.Money, FriendHelp.Information, FriendHelp.Comfort });
                    break;
                case FriendType.Teacher:
                    possibleHelp.AddRange(new[] { FriendHelp.Learning, FriendHelp.Information, FriendHelp.Advice });
                    break;
                case FriendType.BusinessPartner:
                    possibleHelp.AddRange(new[] { FriendHelp.Money, FriendHelp.JobOpportunity, FriendHelp.Business });
                    break;
                case FriendType.Protector:
                    possibleHelp.AddRange(new[] { FriendHelp.Protection, FriendHelp.Safety });
                    break;
                case FriendType.Informant:
                    possibleHelp.AddRange(new[] { FriendHelp.Information, FriendHelp.Secrets });
                    break;
                case FriendType.Criminal:
                    possibleHelp.AddRange(new[] { FriendHelp.IllegalServices, FriendHelp.Protection });
                    break;
                default:
                    possibleHelp.Add(FriendHelp.Comfort);
                    break;
            }

            return possibleHelp[UnityEngine.Random.Range(0, possibleHelp.Count)];
        }

        /// <summary>
        /// Предоставить помощь друга
        /// </summary>
        private bool ProvideFriendHelp(Friend friend, FriendHelp helpType)
        {
            switch (helpType)
            {
                case FriendHelp.Money:
                    return ProvideFriendMoney(friend);
                case FriendHelp.Information:
                    return ProvideInformation(friend);
                case FriendHelp.Learning:
                    return ProvideLearning(friend);
                case FriendHelp.JobOpportunity:
                    return ProvideJobOpportunity(friend);
                case FriendHelp.Protection:
                    return ProvideProtection(friend);
                case FriendHelp.Comfort:
                    return ProvideComfort(friend);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Предоставить деньги
        /// </summary>
        private bool ProvideFriendMoney(Friend friend)
        {
            double amount = CalculateFriendHelpAmount(friend) * 0.5; // Помощь меньше займа
            
            if (moneySystem != null)
            {
                moneySystem.AddMoney(amount, $"Помощь от {friend.name}");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Помощь друга", 
                        $"{friend.name} дал вам ${amount}", NotificationType.Money);
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Предоставить информацию
        /// </summary>
        private bool ProvideInformation(Friend friend)
        {
            // Дать информацию о работе, возможностях и т.д.
            if (notificationSystem != null)
            {
                string[] infoTypes = { "работе", "возможностях заработка", "полезных местах", "опасностях" };
                string info = infoTypes[UnityEngine.Random.Range(0, infoTypes.Length)];
                
                notificationSystem.ShowNotification("Полезная информация", 
                    $"{friend.name} рассказал вам о {info}", NotificationType.Info);
            }

            // Временный бонус к удаче
            if (playerStats != null)
            {
                playerStats.AddStatModifier(new StatModifier(
                    StatType.LuckModifier, 10f, ModifierOperation.Add, 
                    $"Информация от {friend.name}", 3600f)); // 1 час
            }

            return true;
        }

        /// <summary>
        /// Предоставить обучение
        /// </summary>
        private bool ProvideLearning(Friend friend)
        {
            if (skillSystem != null)
            {
                // Дать опыт в случайном навыке
                var skillTypes = Enum.GetValues(typeof(SkillType));
                var randomSkill = (SkillType)skillTypes.GetValue(UnityEngine.Random.Range(0, skillTypes.Length));
                
                float experience = friend.knowledgeLevel * 0.5f;
                skillSystem.AddSkillExperience(randomSkill, experience, $"Обучение от {friend.name}");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Обучение", 
                        $"{friend.name} научил вас полезным навыкам", NotificationType.Info);
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Предоставить возможность работы
        /// </summary>
        private bool ProvideJobOpportunity(Friend friend)
        {
            // TODO: Интеграция с JobSystem
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Возможность работы", 
                    $"{friend.name} предложил вам работу", NotificationType.Info);
            }

            // Временный бонус к заработку
            if (playerStats != null)
            {
                playerStats.AddStatModifier(new StatModifier(
                    StatType.EarningsMultiplier, 1.2f, ModifierOperation.Multiply, 
                    $"Работа от {friend.name}", 7200f)); // 2 часа
            }

            return true;
        }

        /// <summary>
        /// Предоставить защиту
        /// </summary>
        private bool ProvideProtection(Friend friend)
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Защита", 
                    $"{friend.name} обеспечил вам защиту", NotificationType.Info);
            }

            // TODO: Интеграция с CombatSystem для снижения риска нападений
            return true;
        }

        /// <summary>
        /// Предоставить утешение
        /// </summary>
        private bool ProvideComfort(Friend friend)
        {
            if (playerStats != null)
            {
                playerStats.ChangeMood(20f);
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Поддержка", 
                        $"{friend.name} поддержал вас в трудную минуту", NotificationType.Info);
                }
                
                return true;
            }
            
            return false;
        }

        #endregion

        #region Loan System

        /// <summary>
        /// Запросить займ у друга
        /// </summary>
        private bool RequestLoanFromFriend(Friend friend)
        {
            if (!CanGiveLoan(friend))
                return false;

            double amount = CalculateLoanAmount(friend);
            
            var loanData = new LoanData
            {
                friendName = friend.name,
                amount = amount,
                interestRate = loanInterestRate,
                startTime = Time.time,
                dueTime = Time.time + 604800f, // 1 неделя
                isPaid = false
            };

            activeLoans.Add(loanData);
            
            if (moneySystem != null)
            {
                moneySystem.AddMoney(amount, $"Займ от {friend.name}");
            }

            // Снизить готовность давать займы
            friend.trust -= 15f;
            friend.helpfulness -= 20f;

            OnLoanGiven?.Invoke(friend, amount);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Займ", 
                    $"{friend.name} одолжил вам ${amount}", NotificationType.Money);
            }

            return true;
        }

        /// <summary>
        /// Вернуть займ
        /// </summary>
        public bool RepayLoan(string friendName)
        {
            var loan = activeLoans.Find(l => l.friendName == friendName && !l.isPaid);
            if (loan == null)
                return false;

            double totalAmount = CalculateLoanRepaymentAmount(loan);
            
            if (moneySystem == null || !moneySystem.CanAfford(totalAmount))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Недостаточно денег", 
                        $"Нужно ${totalAmount} для возврата займа", NotificationType.Warning);
                }
                return false;
            }

            moneySystem.SpendMoney(totalAmount, $"Возврат займа {friendName}");
            loan.isPaid = true;

            // Улучшить отношения
            if (friends.ContainsKey(friendName))
            {
                var friend = friends[friendName];
                friend.trust += 10f;
                friend.loyalty += 5f;
                
                OnLoanRepaid?.Invoke(friend, totalAmount);
            }

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Займ возвращен", 
                    $"Вы вернули займ {friendName}: ${totalAmount}", NotificationType.Money);
            }

            return true;
        }

        /// <summary>
        /// Обработать займы
        /// </summary>
        private void ProcessLoans()
        {
            foreach (var loan in activeLoans)
            {
                if (loan.isPaid)
                    continue;

                // Проверить просрочку
                if (Time.time > loan.dueTime)
                {
                    ProcessOverdueLoan(loan);
                }
            }
        }

        /// <summary>
        /// Обработать просроченный займ
        /// </summary>
        private void ProcessOverdueLoan(LoanData loan)
        {
            if (friends.ContainsKey(loan.friendName))
            {
                var friend = friends[loan.friendName];
                
                // Ухудшить отношения
                friend.trust -= 20f;
                friend.loyalty -= 15f;
                
                if (friend.trust <= 10f)
                {
                    RemoveFriend(loan.friendName, "Не вернул займ вовремя");
                }
                else if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Просрочка займа", 
                        $"{friend.name} недоволен просрочкой займа", NotificationType.Warning);
                }
            }

            loan.dueTime += 86400f; // Продлить на день
        }

        #endregion

        #region Meeting System

        /// <summary>
        /// Проверить новые встречи
        /// </summary>
        private void CheckNewMeetings()
        {
            if (Time.time - lastMeetingCheck < meetingInterval && !instantMeeting)
                return;

            lastMeetingCheck = Time.time;

            if (!CanMakeNewFriends)
                return;

            float actualMeetingChance = CalculateMeetingChance();
            
            if (UnityEngine.Random.value < actualMeetingChance)
            {
                TriggerRandomMeeting();
            }
        }

        /// <summary>
        /// Рассчитать шанс встречи
        /// </summary>
        private float CalculateMeetingChance()
        {
            float chance = meetingChance;

            // Увеличить шанс в социальных местах
            if (locationManager != null)
            {
                chance *= GetLocationSocialMultiplier(locationManager.CurrentLocation);
            }

            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                chance += charisma * 0.01f;
            }

            // Время суток влияет на встречи
            if (timeOfDayManager != null)
            {
                var period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Day || period == TimePeriod.Evening)
                {
                    chance *= 1.5f;
                }
                else if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                {
                    chance *= 0.5f;
                }
            }

            return chance;
        }

        /// <summary>
        /// Запустить случайную встречу
        /// </summary>
        private void TriggerRandomMeeting()
        {
            Location currentLocation = locationManager?.CurrentLocation ?? Location.Street;
            FriendType friendType = DetermineFriendTypeForLocation(currentLocation);
            string name = GenerateRandomName();

            if (AddFriend(name, friendType, currentLocation))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Случайная встреча: {name} ({friendType}) в {currentLocation}");
                }
            }
        }

        /// <summary>
        /// Определить тип друга для локации
        /// </summary>
        private FriendType DetermineFriendTypeForLocation(Location location)
        {
            switch (location)
            {
                case Location.University:
                    return UnityEngine.Random.value < 0.7f ? FriendType.Teacher : FriendType.Helper;
                case Location.BusinessDistrict:
                    return UnityEngine.Random.value < 0.6f ? FriendType.BusinessPartner : FriendType.Official;
                case Location.Library:
                    return FriendType.Teacher;
                case Location.JobCenter:
                    return FriendType.Helper;
                case Location.Hospital:
                    return FriendType.Helper;
                case Location.Abandoned:
                    return UnityEngine.Random.value < 0.4f ? FriendType.Criminal : FriendType.Informant;
                default:
                    var types = new[] { FriendType.Helper, FriendType.Mentor, FriendType.Rival };
                    return types[UnityEngine.Random.Range(0, types.Length)];
            }
        }

        /// <summary>
        /// Сгенерировать случайное имя
        /// </summary>
        private string GenerateRandomName()
        {
            string[] names = { 
                "Алексей", "Мария", "Дмитрий", "Анна", "Сергей", "Елена", "Андрей", "Ольга",
                "Владимир", "Татьяна", "Николай", "Ирина", "Игорь", "Наталья", "Павел", "Светлана",
                "Максим", "Екатерина", "Виктор", "Людмила", "Артем", "Галина", "Роман", "Вера"
            };
            
            return names[UnityEngine.Random.Range(0, names.Length)];
        }

        #endregion

        #region Betrayal System

        /// <summary>
        /// Проверить предательство
        /// </summary>
        private bool CheckForBetrayal(Friend friend)
        {
            if (friend.relationship >= RelationshipLevel.Good)
                return false;

            float betrayalChance = CalculateBetrayalChance(friend);
            return UnityEngine.Random.value < betrayalChance;
        }

        /// <summary>
        /// Рассчитать шанс предательства
        /// </summary>
        private float CalculateBetrayalChance(Friend friend)
        {
            float chance = 0.001f; // 0.1% базовый шанс

            // Увеличить для неблагонадежных типов
            if (friend.type == FriendType.Criminal || friend.type == FriendType.Informant)
            {
                chance *= 5f;
            }

            // Увеличить при низкой лояльности
            if (friend.loyalty < 30f)
            {
                chance *= 3f;
            }

            // Увеличить при низком доверии
            if (friend.trust < 20f)
            {
                chance *= 2f;
            }

            return chance;
        }

        /// <summary>
        /// Обработать предательство
        /// </summary>
        private void ProcessBetrayal(Friend friend)
        {
            BetrayalType betrayalType = DetermineBetrayalType(friend);
            
            switch (betrayalType)
            {
                case BetrayalType.MoneyTheft:
                    ProcessMoneyTheft(friend);
                    break;
                case BetrayalType.PoliceInform:
                    ProcessPoliceInform(friend);
                    break;
                case BetrayalType.Sabotage:
                    ProcessSabotage(friend);
                    break;
                case BetrayalType.Abandonment:
                    ProcessAbandonment(friend);
                    break;
            }

            OnFriendBetrayed?.Invoke(friend);
            RemoveFriend(friend.name, "Предательство");
        }

        /// <summary>
        /// Определить тип предательства
        /// </summary>
        private BetrayalType DetermineBetrayalType(Friend friend)
        {
            switch (friend.type)
            {
                case FriendType.Criminal:
                    return BetrayalType.MoneyTheft;
                case FriendType.Informant:
                    return BetrayalType.PoliceInform;
                case FriendType.Rival:
                    return BetrayalType.Sabotage;
                default:
                    return BetrayalType.Abandonment;
            }
        }

        /// <summary>
        /// Обработать кражу денег
        /// </summary>
        private void ProcessMoneyTheft(Friend friend)
        {
            if (moneySystem != null)
            {
                double currentMoney = moneySystem.GetMoneyDouble();
                double stolenAmount = currentMoney * UnityEngine.Random.Range(0.1f, 0.3f);
                
                if (stolenAmount > 0)
                {
                    moneySystem.SpendMoney(stolenAmount, $"Кража {friend.name}");
                    
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Предательство!", 
                            $"{friend.name} украл у вас ${stolenAmount}", NotificationType.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// Обработать донос в полицию
        /// </summary>
        private void ProcessPoliceInform(Friend friend)
        {
            // TODO: Интеграция с PoliceSystem
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Предательство!", 
                    $"{friend.name} сдал вас полиции", NotificationType.Warning);
            }
        }

        /// <summary>
        /// Обработать саботаж
        /// </summary>
        private void ProcessSabotage(Friend friend)
        {
            if (playerStats != null)
            {
                playerStats.ChangeMood(-30f);
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Предательство!", 
                        $"{friend.name} испортил ваши дела", NotificationType.Warning);
                }
            }
        }

        /// <summary>
        /// Обработать оставление в беде
        /// </summary>
        private void ProcessAbandonment(Friend friend)
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Предательство!", 
                    $"{friend.name} бросил вас в трудную минуту", NotificationType.Warning);
            }
        }

        /// <summary>
        /// Предать друга (действие игрока)
        /// </summary>
        private bool BetrayFriend(Friend friend)
        {
            // Получить выгоду от предательства
            double benefit = CalculateBetrayalBenefit(friend);
            
            if (moneySystem != null && benefit > 0)
            {
                moneySystem.AddMoney(benefit, $"Предательство {friend.name}");
            }

            // Ухудшить отношения с другими друзьями
            foreach (var otherFriend in friends.Values)
            {
                if (otherFriend.name != friend.name)
                {
                    otherFriend.trust -= 10f;
                    otherFriend.loyalty -= 5f;
                }
            }

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Предательство", 
                    $"Вы предали {friend.name} и получили ${benefit}", NotificationType.Money);
            }

            RemoveFriend(friend.name, "Предательство игрока");
            return true;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Можно ли взаимодействовать с другом
        /// </summary>
        private bool CanInteractWithFriend(string name)
        {
            if (!lastInteractionTime.ContainsKey(name))
                return true;

            return Time.time - lastInteractionTime[name] >= interactionCooldown;
        }

        /// <summary>
        /// Может ли друг помочь
        /// </summary>
        private bool CanFriendHelp(Friend friend)
        {
            return friend.relationship >= RelationshipLevel.Friendly && 
                   friend.helpfulness >= 40f && 
                   friend.isAvailable;
        }

        /// <summary>
        /// Может ли дать займ
        /// </summary>
        private bool CanGiveLoan(Friend friend)
        {
            return friend.relationship >= RelationshipLevel.Good && 
                   friend.trust >= 60f && 
                   !HasActiveLoanWith(friend.name);
        }

        /// <summary>
        /// Есть ли активный займ с другом
        /// </summary>
        private bool HasActiveLoanWith(string friendName)
        {
            return activeLoans.Exists(loan => loan.friendName == friendName && !loan.isPaid);
        }

        /// <summary>
        /// Рассчитать стоимость подарка
        /// </summary>
        private double CalculateGiftCost(Friend friend)
        {
            double baseCost = giftCostMultiplier;
            
            // Дорогие подарки для состоятельных друзей
            if (friend.type == FriendType.BusinessPartner || friend.type == FriendType.Official)
            {
                baseCost *= 3;
            }

            return baseCost * UnityEngine.Random.Range(1f, 3f);
        }

        /// <summary>
        /// Рассчитать сумму займа
        /// </summary>
        private double CalculateLoanAmount(Friend friend)
        {
            double amount = baseLoanAmount;
            
            // Увеличить в зависимости от уровня отношений
            amount *= (float)friend.relationship / 4f;
            
            // Увеличить в зависимости от доверия
            amount *= friend.trust / 50f;
            
            // Увеличить для богатых друзей
            amount *= friend.wealthLevel / 50f;

            return Mathf.Min((float)amount, (float)maxLoanAmount);
        }

        /// <summary>
        /// Рассчитать сумму помощи
        /// </summary>
        private double CalculateFriendHelpAmount(Friend friend)
        {
            return CalculateLoanAmount(friend) * 0.5; // Помощь меньше займа
        }

        /// <summary>
        /// Рассчитать сумму возврата займа
        /// </summary>
        private double CalculateLoanRepaymentAmount(LoanData loan)
        {
            float timeMultiplier = (Time.time - loan.startTime) / 86400f; // В днях
            double interest = loan.amount * loan.interestRate * timeMultiplier;
            return loan.amount + interest;
        }

        /// <summary>
        /// Рассчитать выгоду от предательства
        /// </summary>
        private double CalculateBetrayalBenefit(Friend friend)
        {
            double benefit = friend.wealthLevel * 2;
            
            if (friend.type == FriendType.BusinessPartner)
            {
                benefit *= 3;
            }
            else if (friend.type == FriendType.Criminal)
            {
                benefit *= 1.5;
            }

            return benefit;
        }

        /// <summary>
        /// Получить социальный множитель локации
        /// </summary>
        private float GetLocationSocialMultiplier(Location location)
        {
            switch (location)
            {
                case Location.University: return 3f;
                case Location.BusinessDistrict: return 2f;
                case Location.Library: return 1.5f;
                case Location.Shop: return 1.2f;
                case Location.Park: return 1.5f;
                case Location.Hospital: return 1.3f;
                case Location.JobCenter: return 2f;
                case Location.Abandoned: return 0.3f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public FriendSystemSaveData GetSaveData()
        {
            return new FriendSystemSaveData
            {
                friends = new Dictionary<string, Friend>(friends),
                lastInteractionTime = new Dictionary<string, float>(lastInteractionTime),
                lastMeetingCheck = lastMeetingCheck,
                activeLoans = new List<LoanData>(activeLoans)
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(FriendSystemSaveData data)
        {
            friends = data.friends ?? new Dictionary<string, Friend>();
            lastInteractionTime = data.lastInteractionTime ?? new Dictionary<string, float>();
            lastMeetingCheck = data.lastMeetingCheck;
            activeLoans = data.activeLoans ?? new List<LoanData>();

            if (showDebugInfo)
            {
                Debug.Log($"FriendSystem загружен. Друзей: {friends.Count}, " +
                         $"Активных займов: {activeLoans.Count}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType oldLocation, LocationType newLocation)
        {
            // Обновить доступность друзей в зависимости от локации
            foreach (var friend in friends.Values)
            {
                friend.isAvailable = IsFriendAvailableInLocation(friend, (Location)newLocation);
            }
        }

        /// <summary>
        /// Доступен ли друг в локации
        /// </summary>
        private bool IsFriendAvailableInLocation(Friend friend, Location location)
        {
            // Некоторые друзья могут быть недоступны в определенных местах
            if (friend.type == FriendType.Criminal && 
                (location == Location.Hospital || location == Location.University))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Add Test Friend")]
        private void DebugAddTestFriend()
        {
            AddFriend("Тестовый друг", FriendType.Helper, Location.Street);
        }

        [ContextMenu("Trigger Random Meeting")]
        private void DebugTriggerRandomMeeting()
        {
            TriggerRandomMeeting();
        }

        [ContextMenu("Show Friends Info")]
        private void DebugShowFriendsInfo()
        {
            Debug.Log($"Друзей: {friends.Count}/{maxFriends}");
            Debug.Log($"Активных займов: {activeLoans.Count}");
            
            foreach (var friend in friends.Values)
            {
                Debug.Log($"- {friend.name} ({friend.type}): {friend.relationship}, " +
                         $"Лояльность: {friend.loyalty:F1}, Доверие: {friend.trust:F1}");
            }
        }

        [ContextMenu("Force Betrayal")]
        private void DebugForceBetrayalCheck()
        {
            foreach (var friend in friends.Values)
            {
                if (CheckForBetrayal(friend))
                {
                    ProcessBetrayal(friend);
                    break;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Класс друга
    /// </summary>
    [System.Serializable]
    public class Friend
    {
        public string name;                         // Имя
        public FriendType type;                     // Тип друга
        public RelationshipLevel relationship;      // Уровень отношений
        public float loyalty;                       // Лояльность (0-100)
        public float trust;                         // Доверие (0-100)
        public float helpfulness = 60f;             // Готовность помочь (0-100)
        public float reliability = 60f;             // Надежность (0-100)
        public Location metAt;                      // Где познакомились
        public float meetingTime;                   // Время знакомства
        public float lastContact;                   // Последний контакт
        public int helpCount;                       // Количество помощи
        public int totalFavors;                     // Общее количество одолжений
        public bool isAvailable;                    // Доступен ли сейчас
        public Location currentLocation;            // Текущая локация

        // Дополнительные характеристики
        public float knowledgeLevel = 50f;          // Уровень знаний
        public float wealthLevel = 50f;             // Уровень богатства
        public float strengthLevel = 50f;           // Физическая сила
        public float dangerLevel = 30f;             // Уровень опасности

        /// <summary>
        /// Получить описание друга
        /// </summary>
        public string GetDescription()
        {
            return $"{name} ({type}) - {relationship}\n" +
                   $"Лояльность: {loyalty:F0}%, Доверие: {trust:F0}%\n" +
                   $"Помог раз: {helpCount}";
        }

        /// <summary>
        /// Получить общий рейтинг
        /// </summary>
        public float GetOverallRating()
        {
            return (loyalty + trust + helpfulness + reliability) / 4f;
        }
    }

    /// <summary>
    /// Данные займа
    /// </summary>
    [System.Serializable]
    public class LoanData
    {
        public string friendName;       // Имя друга
        public double amount;           // Сумма займа
        public float interestRate;      // Процентная ставка
        public float startTime;         // Время выдачи
        public float dueTime;           // Срок возврата
        public bool isPaid;             // Возвращен ли займ
    }

    /// <summary>
    /// Типы взаимодействия с друзьями
    /// </summary>
    public enum FriendInteraction
    {
        Chat,           // Поговорить
        GiveGift,       // Подарить подарок
        AskForHelp,     // Попросить помощи
        RequestLoan,    // Попросить займ
        Betray          // Предать
    }

    /// <summary>
    /// Типы помощи от друзей
    /// </summary>
    public enum FriendHelp
    {
        Money,              // Деньги
        Information,        // Информация
        Learning,           // Обучение
        JobOpportunity,     // Возможность работы
        Protection,         // Защита
        Comfort,            // Утешение
        Business,           // Деловая помощь
        Safety,             // Безопасность
        Secrets,            // Секреты
        Advice,             // Совет
        IllegalServices     // Нелегальные услуги
    }

    /// <summary>
    /// Типы предательства
    /// </summary>
    public enum BetrayalType
    {
        MoneyTheft,     // Кража денег
        PoliceInform,   // Донос в полицию
        Sabotage,       // Саботаж
        Abandonment     // Оставление в беде
    }

    /// <summary>
    /// Данные FriendSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class FriendSystemSaveData
    {
        public Dictionary<string, Friend> friends;
        public Dictionary<string, float> lastInteractionTime;
        public float lastMeetingCheck;
        public List<LoanData> activeLoans;
    }
}
