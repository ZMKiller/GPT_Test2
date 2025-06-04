using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система социальных взаимодействий и репутации
    /// </summary>
    public class SocialSystem : MonoBehaviour
    {
        [Header("Настройки социальной системы")]
        [SerializeField] private bool enableSocialSystem = true;
        [SerializeField] private float reputationDecayRate = 0.5f;         // Скорость ухудшения репутации
        [SerializeField] private float influenceDecayRate = 0.3f;          // Скорость ухудшения влияния
        [SerializeField] private int maxNetworkConnections = 50;           // Максимум сетевых связей

        [Header("Модификаторы репутации")]
        [SerializeField] private float appearanceModifier = 0.2f;          // Влияние внешности
        [SerializeField] private float behaviorModifier = 0.3f;            // Влияние поведения
        [SerializeField] private float wealthModifier = 0.25f;             // Влияние богатства
        [SerializeField] private float achievementModifier = 0.15f;        // Влияние достижений

        [Header("Настройки взаимодействий")]
        [SerializeField] private float interactionCooldown = 1800f;        // Кулдаун взаимодействий (секунды)
        [SerializeField] private float networkBonusMultiplier = 1.5f;      // Множитель бонуса от сети
        [SerializeField] private float groupLoyaltyDecay = 1f;             // Скорость ухудшения лояльности группы

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool freeReputation = false;
        [SerializeField] private bool instantNetworking = false;

        // Состояние системы
        private Dictionary<SocialGroup, float> groupReputation = new Dictionary<SocialGroup, float>();
        private Dictionary<SocialGroup, float> groupInfluence = new Dictionary<SocialGroup, float>();
        private Dictionary<SocialGroup, float> groupLoyalty = new Dictionary<SocialGroup, float>();
        private List<NetworkConnection> networkConnections = new List<NetworkConnection>();
        private Dictionary<string, float> lastInteractionTime = new Dictionary<string, float>();
        private SocialStatus currentSocialStatus = SocialStatus.Homeless;
        private Dictionary<SocialGroup, bool> groupMembership = new Dictionary<SocialGroup, bool>();

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private FriendSystem friendSystem;
        private NotificationSystem notificationSystem;
        private AchievementSystem achievementSystem;

        // События
        public event Action<SocialGroup, float> OnReputationChanged;
        public event Action<SocialGroup, float> OnInfluenceChanged;
        public event Action<SocialStatus> OnSocialStatusChanged;
        public event Action<NetworkConnection> OnNetworkConnectionAdded;
        public event Action<NetworkConnection> OnNetworkConnectionLost;
        public event Action<SocialGroup> OnGroupJoined;
        public event Action<SocialGroup> OnGroupLeft;

        // Свойства
        public SocialStatus CurrentSocialStatus => currentSocialStatus;
        public Dictionary<SocialGroup, float> GroupReputation => new Dictionary<SocialGroup, float>(groupReputation);
        public Dictionary<SocialGroup, float> GroupInfluence => new Dictionary<SocialGroup, float>(groupInfluence);
        public List<NetworkConnection> NetworkConnections => new List<NetworkConnection>(networkConnections);
        public float OverallReputation => CalculateOverallReputation();
        public float SocialInfluence => CalculateSocialInfluence();

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = FindObjectOfType<SkillSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            friendSystem = FindObjectOfType<FriendSystem>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
            achievementSystem = FindObjectOfType<AchievementSystem>();
        }

        private void Start()
        {
            if (enableSocialSystem)
            {
                InitializeSocialSystem();
                SubscribeToEvents();
            }

            Debug.Log("SocialSystem инициализирован");
        }

        private void Update()
        {
            if (enableSocialSystem)
            {
                UpdateSocialSystem();
                UpdateReputation();
                UpdateSocialStatus();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация социальной системы
        /// </summary>
        private void InitializeSocialSystem()
        {
            // Инициализировать репутацию во всех группах
            foreach (SocialGroup group in Enum.GetValues(typeof(SocialGroup)))
            {
                groupReputation[group] = 50f; // Нейтральная репутация
                groupInfluence[group] = 0f;
                groupLoyalty[group] = 0f;
                groupMembership[group] = false;
            }

            // Начальный статус
            currentSocialStatus = SocialStatus.Homeless;
            
            // Слегка снизить репутацию в приличных кругах для бомжа
            groupReputation[SocialGroup.HighSociety] = 20f;
            groupReputation[SocialGroup.Business] = 30f;
            groupReputation[SocialGroup.Government] = 25f;
            
            // Немного выше в маргинальных группах
            groupReputation[SocialGroup.Homeless] = 70f;
            groupReputation[SocialGroup.Criminal] = 60f;
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (friendSystem != null)
            {
                friendSystem.OnFriendAdded += OnFriendAdded;
                friendSystem.OnFriendLost += OnFriendLost;
            }

            if (playerStats != null)
            {
                playerStats.OnLevelUp += OnPlayerLevelUp;
            }

            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged += OnMoneyChanged;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (friendSystem != null)
            {
                friendSystem.OnFriendAdded -= OnFriendAdded;
                friendSystem.OnFriendLost -= OnFriendLost;
            }

            if (playerStats != null)
            {
                playerStats.OnLevelUp -= OnPlayerLevelUp;
            }

            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged -= OnMoneyChanged;
            }
        }

        #endregion

        #region Social System Update

        /// <summary>
        /// Обновить социальную систему
        /// </summary>
        private void UpdateSocialSystem()
        {
            UpdateNetworkConnections();
            UpdateGroupLoyalty();
        }

        /// <summary>
        /// Обновить репутацию
        /// </summary>
        private void UpdateReputation()
        {
            foreach (SocialGroup group in Enum.GetValues(typeof(SocialGroup)))
            {
                // Естественная деградация репутации
                if (groupReputation[group] > 50f)
                {
                    float decay = reputationDecayRate * Time.deltaTime;
                    groupReputation[group] = Mathf.Max(50f, groupReputation[group] - decay);
                }
                
                // Деградация влияния
                if (groupInfluence[group] > 0f)
                {
                    float decay = influenceDecayRate * Time.deltaTime;
                    groupInfluence[group] = Mathf.Max(0f, groupInfluence[group] - decay);
                }
            }
        }

        /// <summary>
        /// Обновить социальный статус
        /// </summary>
        private void UpdateSocialStatus()
        {
            SocialStatus newStatus = CalculateSocialStatus();
            
            if (newStatus != currentSocialStatus)
            {
                SocialStatus oldStatus = currentSocialStatus;
                currentSocialStatus = newStatus;
                
                OnSocialStatusChanged?.Invoke(newStatus);
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Изменение статуса", 
                        $"Ваш социальный статус изменился: {GetSocialStatusName(newStatus)}", 
                        NotificationType.Info);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Социальный статус изменился: {oldStatus} → {newStatus}");
                }
            }
        }

        /// <summary>
        /// Обновить сетевые связи
        /// </summary>
        private void UpdateNetworkConnections()
        {
            for (int i = networkConnections.Count - 1; i >= 0; i--)
            {
                var connection = networkConnections[i];
                
                // Снижение силы связи со временем
                connection.strength -= Time.deltaTime * 0.1f; // 0.1 в секунду
                
                if (connection.strength <= 0f)
                {
                    OnNetworkConnectionLost?.Invoke(connection);
                    networkConnections.RemoveAt(i);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Потеряна связь: {connection.contactName}");
                    }
                }
            }
        }

        /// <summary>
        /// Обновить лояльность групп
        /// </summary>
        private void UpdateGroupLoyalty()
        {
            foreach (SocialGroup group in Enum.GetValues(typeof(SocialGroup)))
            {
                if (groupLoyalty[group] > 0f && !groupMembership[group])
                {
                    float decay = groupLoyaltyDecay * Time.deltaTime;
                    groupLoyalty[group] = Mathf.Max(0f, groupLoyalty[group] - decay);
                }
            }
        }

        #endregion

        #region Reputation Management

        /// <summary>
        /// Изменить репутацию в группе
        /// </summary>
        public void ChangeReputation(SocialGroup group, float amount, string reason = "")
        {
            if (freeReputation && amount < 0)
                return;

            float oldReputation = groupReputation[group];
            groupReputation[group] = Mathf.Clamp(groupReputation[group] + amount, 0f, 100f);
            
            OnReputationChanged?.Invoke(group, groupReputation[group]);

            if (notificationSystem != null && Mathf.Abs(amount) >= 5f)
            {
                string change = amount > 0 ? "повысилась" : "понизилась";
                string message = !string.IsNullOrEmpty(reason) ? 
                    $"Репутация в {GetGroupName(group)} {change}: {reason}" :
                    $"Репутация в {GetGroupName(group)} {change}";
                
                NotificationType type = amount > 0 ? NotificationType.Info : NotificationType.Warning;
                notificationSystem.ShowNotification("Репутация", message, type);
            }

            // Влияние на другие группы
            ApplyReputationCrossEffects(group, amount);

            if (showDebugInfo)
            {
                Debug.Log($"Репутация в {group}: {oldReputation:F1} → {groupReputation[group]:F1} ({amount:+F1})");
            }
        }

        /// <summary>
        /// Применить перекрестные эффекты репутации
        /// </summary>
        private void ApplyReputationCrossEffects(SocialGroup affectedGroup, float amount)
        {
            switch (affectedGroup)
            {
                case SocialGroup.Criminal:
                    // Криминальная репутация ухудшает отношения с законопослушными группами
                    if (amount > 0)
                    {
                        ChangeReputation(SocialGroup.Government, -amount * 0.3f);
                        ChangeReputation(SocialGroup.HighSociety, -amount * 0.2f);
                    }
                    break;

                case SocialGroup.Government:
                    // Правительственная репутация ухудшает отношения с криминалом
                    if (amount > 0)
                    {
                        ChangeReputation(SocialGroup.Criminal, -amount * 0.4f);
                    }
                    break;

                case SocialGroup.HighSociety:
                    // Высшее общество может помочь в бизнесе
                    if (amount > 0)
                    {
                        ChangeReputation(SocialGroup.Business, amount * 0.2f);
                    }
                    break;

                case SocialGroup.Religious:
                    // Религиозная репутация улучшает отношения с благотворительными организациями
                    if (amount > 0)
                    {
                        ChangeReputation(SocialGroup.Community, amount * 0.3f);
                    }
                    break;
            }
        }

        /// <summary>
        /// Изменить влияние в группе
        /// </summary>
        public void ChangeInfluence(SocialGroup group, float amount, string reason = "")
        {
            float oldInfluence = groupInfluence[group];
            groupInfluence[group] = Mathf.Clamp(groupInfluence[group] + amount, 0f, 100f);
            
            OnInfluenceChanged?.Invoke(group, groupInfluence[group]);

            if (notificationSystem != null && Mathf.Abs(amount) >= 3f)
            {
                string change = amount > 0 ? "усилилось" : "ослабло";
                string message = !string.IsNullOrEmpty(reason) ? 
                    $"Влияние в {GetGroupName(group)} {change}: {reason}" :
                    $"Влияние в {GetGroupName(group)} {change}";
                
                notificationSystem.ShowNotification("Влияние", message, NotificationType.Info);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Влияние в {group}: {oldInfluence:F1} → {groupInfluence[group]:F1} ({amount:+F1})");
            }
        }

        #endregion

        #region Social Interactions

        /// <summary>
        /// Взаимодействовать с представителем группы
        /// </summary>
        public bool InteractWithGroup(SocialGroup group, SocialInteractionType interactionType)
        {
            if (!CanInteractWithGroup(group))
                return false;

            bool success = false;
            
            switch (interactionType)
            {
                case SocialInteractionType.Networking:
                    success = PerformNetworking(group);
                    break;
                case SocialInteractionType.Favor:
                    success = RequestFavor(group);
                    break;
                case SocialInteractionType.Business:
                    success = ConductBusiness(group);
                    break;
                case SocialInteractionType.Charity:
                    success = ParticipateInCharity(group);
                    break;
                case SocialInteractionType.Ceremony:
                    success = AttendCeremony(group);
                    break;
            }

            if (success)
            {
                lastInteractionTime[group.ToString()] = Time.time;
            }

            return success;
        }

        /// <summary>
        /// Можно ли взаимодействовать с группой
        /// </summary>
        private bool CanInteractWithGroup(SocialGroup group)
        {
            string key = group.ToString();
            
            if (lastInteractionTime.ContainsKey(key))
            {
                return Time.time - lastInteractionTime[key] >= interactionCooldown;
            }
            
            return true;
        }

        /// <summary>
        /// Выполнить нетворкинг
        /// </summary>
        private bool PerformNetworking(SocialGroup group)
        {
            if (networkConnections.Count >= maxNetworkConnections)
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Нетворкинг", 
                        "Вы уже знаете максимальное количество людей", NotificationType.Warning);
                }
                return false;
            }

            float successChance = CalculateNetworkingChance(group);
            
            if (UnityEngine.Random.value < successChance)
            {
                var connection = CreateNetworkConnection(group);
                networkConnections.Add(connection);
                
                ChangeReputation(group, 3f, "Успешный нетворкинг");
                ChangeInfluence(group, 1f);
                
                OnNetworkConnectionAdded?.Invoke(connection);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Новая связь", 
                        $"Установлена связь с {connection.contactName}", NotificationType.Info);
                }

                return true;
            }
            else
            {
                ChangeReputation(group, -1f, "Неудачный нетворкинг");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Нетворкинг", 
                        "Не удалось установить полезные связи", NotificationType.Warning);
                }

                return false;
            }
        }

        /// <summary>
        /// Запросить одолжение
        /// </summary>
        private bool RequestFavor(SocialGroup group)
        {
            float favorChance = CalculateFavorChance(group);
            
            if (UnityEngine.Random.value < favorChance)
            {
                var favor = DetermineFavor(group);
                ApplyFavorBenefit(favor);
                
                ChangeReputation(group, -2f, "Использование связей");
                ChangeInfluence(group, -3f);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Одолжение", 
                        $"Получена помощь: {favor.description}", NotificationType.Info);
                }

                return true;
            }
            else
            {
                ChangeReputation(group, -3f, "Отказ в одолжении");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Отказ", 
                        "В одолжении отказано", NotificationType.Warning);
                }

                return false;
            }
        }

        /// <summary>
        /// Провести деловую встречу
        /// </summary>
        private bool ConductBusiness(SocialGroup group)
        {
            if (group != SocialGroup.Business && groupReputation[SocialGroup.Business] < 60f)
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Бизнес", 
                        "Недостаточная деловая репутация", NotificationType.Warning);
                }
                return false;
            }

            double investmentCost = CalculateBusinessInvestment();
            
            if (moneySystem == null || !moneySystem.CanAfford(investmentCost))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Бизнес", 
                        $"Нужно ${investmentCost} для участия в деле", NotificationType.Warning);
                }
                return false;
            }

            float successChance = CalculateBusinessChance(group);
            
            if (UnityEngine.Random.value < successChance)
            {
                moneySystem.SpendMoney(investmentCost, "Деловая инвестиция");
                double profit = investmentCost * UnityEngine.Random.Range(1.2f, 2.5f);
                moneySystem.AddMoney(profit, "Прибыль от дела");
                
                ChangeReputation(SocialGroup.Business, 5f, "Успешная сделка");
                ChangeInfluence(SocialGroup.Business, 3f);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Успешная сделка", 
                        $"Прибыль: ${profit - investmentCost:F0}", NotificationType.Money);
                }

                return true;
            }
            else
            {
                moneySystem.SpendMoney(investmentCost, "Неудачная инвестиция");
                
                ChangeReputation(SocialGroup.Business, -3f, "Неудачная сделка");

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Неудачная сделка", 
                        $"Потеряно: ${investmentCost}", NotificationType.Warning);
                }

                return false;
            }
        }

        /// <summary>
        /// Участвовать в благотворительности
        /// </summary>
        private bool ParticipateInCharity(SocialGroup group)
        {
            double donationAmount = CalculateCharityDonation();
            
            if (moneySystem == null || !moneySystem.CanAfford(donationAmount))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Благотворительность", 
                        $"Нужно ${donationAmount} для пожертвования", NotificationType.Warning);
                }
                return false;
            }

            moneySystem.SpendMoney(donationAmount, "Благотворительность");
            
            ChangeReputation(SocialGroup.Community, 8f, "Благотворительность");
            ChangeReputation(SocialGroup.Religious, 5f, "Помощь нуждающимся");
            ChangeReputation(SocialGroup.HighSociety, 3f, "Социальная ответственность");

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Благотворительность", 
                    $"Пожертвовано ${donationAmount} на доброе дело", NotificationType.Info);
            }

            return true;
        }

        /// <summary>
        /// Посетить церемонию
        /// </summary>
        private bool AttendCeremony(SocialGroup group)
        {
            float ceremonyFee = 100f;
            
            if (moneySystem == null || !moneySystem.CanAfford(ceremonyFee))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Церемония", 
                        $"Нужно ${ceremonyFee} для участия", NotificationType.Warning);
                }
                return false;
            }

            moneySystem.SpendMoney(ceremonyFee, "Участие в церемонии");
            
            ChangeReputation(group, 4f, "Участие в церемонии");
            ChangeInfluence(group, 2f);

            // Шанс встретить полезных людей
            if (UnityEngine.Random.value < 0.3f)
            {
                PerformNetworking(group);
            }

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Церемония", 
                    "Вы произвели хорошее впечатление", NotificationType.Info);
            }

            return true;
        }

        #endregion

        #region Group Membership

        /// <summary>
        /// Попытаться вступить в группу
        /// </summary>
        public bool TryJoinGroup(SocialGroup group)
        {
            if (groupMembership[group])
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Членство", 
                        $"Вы уже состоите в {GetGroupName(group)}", NotificationType.Info);
                }
                return false;
            }

            if (!CanJoinGroup(group))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Членство", 
                        $"Недостаточная репутация для вступления в {GetGroupName(group)}", 
                        NotificationType.Warning);
                }
                return false;
            }

            double membershipFee = GetMembershipFee(group);
            
            if (membershipFee > 0 && (moneySystem == null || !moneySystem.CanAfford(membershipFee)))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Членство", 
                        $"Нужно ${membershipFee} для вступления", NotificationType.Warning);
                }
                return false;
            }

            if (membershipFee > 0)
            {
                moneySystem.SpendMoney(membershipFee, $"Членство в {GetGroupName(group)}");
            }

            groupMembership[group] = true;
            groupLoyalty[group] = 60f; // Начальная лояльность
            
            OnGroupJoined?.Invoke(group);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Новое членство", 
                    $"Вы вступили в {GetGroupName(group)}", NotificationType.Info);
            }

            return true;
        }

        /// <summary>
        /// Покинуть группу
        /// </summary>
        public bool LeaveGroup(SocialGroup group)
        {
            if (!groupMembership[group])
                return false;

            groupMembership[group] = false;
            groupLoyalty[group] = 0f;
            
            // Штраф к репутации
            ChangeReputation(group, -20f, "Покинул группу");
            
            OnGroupLeft?.Invoke(group);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Членство", 
                    $"Вы покинули {GetGroupName(group)}", NotificationType.Warning);
            }

            return true;
        }

        /// <summary>
        /// Можно ли вступить в группу
        /// </summary>
        private bool CanJoinGroup(SocialGroup group)
        {
            float requiredReputation = GetRequiredReputationForMembership(group);
            return groupReputation[group] >= requiredReputation;
        }

        /// <summary>
        /// Получить требуемую репутацию для членства
        /// </summary>
        private float GetRequiredReputationForMembership(SocialGroup group)
        {
            switch (group)
            {
                case SocialGroup.HighSociety: return 80f;
                case SocialGroup.Business: return 70f;
                case SocialGroup.Government: return 75f;
                case SocialGroup.Academic: return 65f;
                case SocialGroup.Media: return 60f;
                case SocialGroup.Religious: return 55f;
                case SocialGroup.Community: return 50f;
                case SocialGroup.Criminal: return 40f;
                case SocialGroup.Homeless: return 30f;
                default: return 60f;
            }
        }

        /// <summary>
        /// Получить плату за членство
        /// </summary>
        private double GetMembershipFee(SocialGroup group)
        {
            switch (group)
            {
                case SocialGroup.HighSociety: return 2000;
                case SocialGroup.Business: return 1000;
                case SocialGroup.Government: return 500;
                case SocialGroup.Academic: return 300;
                case SocialGroup.Media: return 400;
                case SocialGroup.Religious: return 100;
                case SocialGroup.Community: return 50;
                case SocialGroup.Criminal: return 200;
                case SocialGroup.Homeless: return 0;
                default: return 250;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Рассчитать общую репутацию
        /// </summary>
        private float CalculateOverallReputation()
        {
            float total = 0;
            int count = 0;
            
            foreach (var reputation in groupReputation.Values)
            {
                total += reputation;
                count++;
            }
            
            return count > 0 ? total / count : 0;
        }

        /// <summary>
        /// Рассчитать социальное влияние
        /// </summary>
        private float CalculateSocialInfluence()
        {
            float total = 0;
            
            foreach (var influence in groupInfluence.Values)
            {
                total += influence;
            }
            
            // Бонус от сетевых связей
            float networkBonus = networkConnections.Count * networkBonusMultiplier;
            
            return total + networkBonus;
        }

        /// <summary>
        /// Рассчитать социальный статус
        /// </summary>
        private SocialStatus CalculateSocialStatus()
        {
            float wealth = GetPlayerWealth();
            float reputation = OverallReputation;
            float influence = SocialInfluence;
            
            // Взвешенная оценка
            float statusScore = (wealth * 0.4f + reputation * 0.35f + influence * 0.25f);
            
            if (statusScore >= 90f) return SocialStatus.Elite;
            if (statusScore >= 80f) return SocialStatus.UpperClass;
            if (statusScore >= 70f) return SocialStatus.UpperMiddleClass;
            if (statusScore >= 60f) return SocialStatus.MiddleClass;
            if (statusScore >= 50f) return SocialStatus.LowerMiddleClass;
            if (statusScore >= 40f) return SocialStatus.WorkingClass;
            if (statusScore >= 30f) return SocialStatus.Poor;
            return SocialStatus.Homeless;
        }

        /// <summary>
        /// Получить богатство игрока (0-100)
        /// </summary>
        private float GetPlayerWealth()
        {
            if (moneySystem == null)
                return 0f;

            double money = moneySystem.GetMoneyDouble();
            
            // Логарифмическая шкала для богатства
            if (money <= 0) return 0f;
            if (money >= 100000) return 100f;
            
            return Mathf.Clamp01((float)Math.Log10(money + 1) / 5f) * 100f;
        }

        /// <summary>
        /// Рассчитать шанс нетворкинга
        /// </summary>
        private float CalculateNetworkingChance(SocialGroup group)
        {
            float baseChance = 0.3f;
            
            // Бонус от репутации
            baseChance += groupReputation[group] * 0.003f;
            
            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                baseChance += charisma * 0.05f;
            }
            
            // Бонус от внешности
            if (playerStats != null)
            {
                // TODO: Добавить систему внешности
                // float appearance = playerStats.GetAppearance();
                // baseChance += appearance * 0.02f;
            }
            
            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Рассчитать шанс одолжения
        /// </summary>
        private float CalculateFavorChance(SocialGroup group)
        {
            float baseChance = 0.2f;
            
            // Бонус от репутации и влияния
            baseChance += groupReputation[group] * 0.004f;
            baseChance += groupInfluence[group] * 0.003f;
            
            // Бонус от количества связей
            int connectionsInGroup = networkConnections.FindAll(c => c.group == group).Count;
            baseChance += connectionsInGroup * 0.05f;
            
            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Рассчитать шанс успеха в бизнесе
        /// </summary>
        private float CalculateBusinessChance(SocialGroup group)
        {
            float baseChance = 0.4f;
            
            // Бонус от деловой репутации
            baseChance += groupReputation[SocialGroup.Business] * 0.003f;
            
            // Бонус от навыков
            if (skillSystem != null)
            {
                float business = skillSystem.GetSkillLevel(SkillType.Business);
                baseChance += business * 0.03f;
            }
            
            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Рассчитать инвестицию для бизнеса
        /// </summary>
        private double CalculateBusinessInvestment()
        {
            return UnityEngine.Random.Range(200f, 1000f);
        }

        /// <summary>
        /// Рассчитать пожертвование
        /// </summary>
        private double CalculateCharityDonation()
        {
            if (moneySystem == null)
                return 50;

            double money = moneySystem.GetMoneyDouble();
            return Math.Max(20, money * 0.05); // 5% от денег, минимум $20
        }

        /// <summary>
        /// Создать сетевую связь
        /// </summary>
        private NetworkConnection CreateNetworkConnection(SocialGroup group)
        {
            string[] names = { "Александр", "Мария", "Дмитрий", "Анна", "Сергей", "Елена" };
            string[] titles = GetGroupTitles(group);
            
            return new NetworkConnection
            {
                contactName = names[UnityEngine.Random.Range(0, names.Length)],
                title = titles[UnityEngine.Random.Range(0, titles.Length)],
                group = group,
                strength = UnityEngine.Random.Range(60f, 90f),
                establishedTime = Time.time,
                influence = UnityEngine.Random.Range(30f, 70f)
            };
        }

        /// <summary>
        /// Получить должности для группы
        /// </summary>
        private string[] GetGroupTitles(SocialGroup group)
        {
            switch (group)
            {
                case SocialGroup.Business:
                    return new[] { "Директор", "Менеджер", "Предприниматель", "Инвестор" };
                case SocialGroup.Government:
                    return new[] { "Чиновник", "Депутат", "Министр", "Советник" };
                case SocialGroup.Academic:
                    return new[] { "Профессор", "Доцент", "Исследователь", "Декан" };
                case SocialGroup.Media:
                    return new[] { "Журналист", "Редактор", "Продюсер", "Блогер" };
                case SocialGroup.HighSociety:
                    return new[] { "Аристократ", "Филантроп", "Меценат", "Коллекционер" };
                default:
                    return new[] { "Знакомый", "Контакт", "Партнер", "Союзник" };
            }
        }

        /// <summary>
        /// Определить одолжение
        /// </summary>
        private SocialFavor DetermineFavor(SocialGroup group)
        {
            switch (group)
            {
                case SocialGroup.Business:
                    return new SocialFavor
                    {
                        type = FavorType.Business,
                        description = "Выгодная деловая возможность",
                        value = UnityEngine.Random.Range(500f, 2000f)
                    };
                    
                case SocialGroup.Government:
                    return new SocialFavor
                    {
                        type = FavorType.Legal,
                        description = "Помощь с документами",
                        value = UnityEngine.Random.Range(200f, 800f)
                    };
                    
                case SocialGroup.Media:
                    return new SocialFavor
                    {
                        type = FavorType.Reputation,
                        description = "Положительная публикация",
                        value = UnityEngine.Random.Range(10f, 30f)
                    };
                    
                default:
                    return new SocialFavor
                    {
                        type = FavorType.Information,
                        description = "Полезная информация",
                        value = UnityEngine.Random.Range(50f, 200f)
                    };
            }
        }

        /// <summary>
        /// Применить преимущество от одолжения
        /// </summary>
        private void ApplyFavorBenefit(SocialFavor favor)
        {
            switch (favor.type)
            {
                case FavorType.Business:
                    if (moneySystem != null)
                    {
                        moneySystem.AddMoney(favor.value, "Деловое одолжение");
                    }
                    break;
                    
                case FavorType.Legal:
                    // TODO: Интеграция с системой штрафов
                    break;
                    
                case FavorType.Reputation:
                    ChangeReputation(SocialGroup.Community, favor.value);
                    break;
                    
                case FavorType.Information:
                    if (playerStats != null)
                    {
                        playerStats.AddStatModifier(new StatModifier(
                            StatType.LuckModifier, favor.value * 0.1f, ModifierOperation.Add,
                            "Полезная информация", 3600f));
                    }
                    break;
            }
        }

        /// <summary>
        /// Получить название группы
        /// </summary>
        private string GetGroupName(SocialGroup group)
        {
            switch (group)
            {
                case SocialGroup.HighSociety: return "Высшее общество";
                case SocialGroup.Business: return "Деловые круги";
                case SocialGroup.Government: return "Государственные органы";
                case SocialGroup.Academic: return "Академические круги";
                case SocialGroup.Media: return "СМИ";
                case SocialGroup.Religious: return "Религиозные общины";
                case SocialGroup.Community: return "Местные сообщества";
                case SocialGroup.Criminal: return "Криминальные группировки";
                case SocialGroup.Homeless: return "Бездомные";
                default: return group.ToString();
            }
        }

        /// <summary>
        /// Получить название социального статуса
        /// </summary>
        private string GetSocialStatusName(SocialStatus status)
        {
            switch (status)
            {
                case SocialStatus.Elite: return "Элита";
                case SocialStatus.UpperClass: return "Высший класс";
                case SocialStatus.UpperMiddleClass: return "Высший средний класс";
                case SocialStatus.MiddleClass: return "Средний класс";
                case SocialStatus.LowerMiddleClass: return "Низший средний класс";
                case SocialStatus.WorkingClass: return "Рабочий класс";
                case SocialStatus.Poor: return "Бедняк";
                case SocialStatus.Homeless: return "Бездомный";
                default: return status.ToString();
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public SocialSystemSaveData GetSaveData()
        {
            return new SocialSystemSaveData
            {
                groupReputation = new Dictionary<SocialGroup, float>(groupReputation),
                groupInfluence = new Dictionary<SocialGroup, float>(groupInfluence),
                groupLoyalty = new Dictionary<SocialGroup, float>(groupLoyalty),
                groupMembership = new Dictionary<SocialGroup, bool>(groupMembership),
                networkConnections = new List<NetworkConnection>(networkConnections),
                lastInteractionTime = new Dictionary<string, float>(lastInteractionTime),
                currentSocialStatus = currentSocialStatus
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(SocialSystemSaveData data)
        {
            groupReputation = data.groupReputation ?? new Dictionary<SocialGroup, float>();
            groupInfluence = data.groupInfluence ?? new Dictionary<SocialGroup, float>();
            groupLoyalty = data.groupLoyalty ?? new Dictionary<SocialGroup, float>();
            groupMembership = data.groupMembership ?? new Dictionary<SocialGroup, bool>();
            networkConnections = data.networkConnections ?? new List<NetworkConnection>();
            lastInteractionTime = data.lastInteractionTime ?? new Dictionary<string, float>();
            currentSocialStatus = data.currentSocialStatus;

            if (showDebugInfo)
            {
                Debug.Log($"SocialSystem загружен. Статус: {currentSocialStatus}, " +
                         $"Связей: {networkConnections.Count}, Репутация: {OverallReputation:F1}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик добавления друга
        /// </summary>
        private void OnFriendAdded(Friend friend)
        {
            // Репутация зависит от типа друга
            switch (friend.type)
            {
                case FriendType.BusinessPartner:
                    ChangeReputation(SocialGroup.Business, 2f);
                    break;
                case FriendType.Teacher:
                    ChangeReputation(SocialGroup.Academic, 2f);
                    break;
                case FriendType.Criminal:
                    ChangeReputation(SocialGroup.Criminal, 3f);
                    ChangeReputation(SocialGroup.Government, -1f);
                    break;
            }
        }

        /// <summary>
        /// Обработчик потери друга
        /// </summary>
        private void OnFriendLost(Friend friend)
        {
            // Небольшой штраф к репутации
            ChangeReputation(SocialGroup.Community, -1f);
        }

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnPlayerLevelUp(int newLevel)
        {
            // Повышение уровня улучшает репутацию
            ChangeReputation(SocialGroup.Community, 1f, "Личностный рост");
        }

        /// <summary>
        /// Обработчик изменения денег
        /// </summary>
        private void OnMoneyChanged(double currentAmount, double change, string source)
        {
            // Богатство влияет на репутацию в определенных кругах
            if (change > 0 && currentAmount > 10000)
            {
                float wealthGain = (float)(change / 1000f);
                ChangeReputation(SocialGroup.Business, wealthGain * 0.1f);
                ChangeReputation(SocialGroup.HighSociety, wealthGain * 0.05f);
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Add Network Connection")]
        private void DebugAddNetworkConnection()
        {
            PerformNetworking(SocialGroup.Business);
        }

        [ContextMenu("Join High Society")]
        private void DebugJoinHighSociety()
        {
            TryJoinGroup(SocialGroup.HighSociety);
        }

        [ContextMenu("Show Social Stats")]
        private void DebugShowSocialStats()
        {
            Debug.Log($"Социальная статистика:");
            Debug.Log($"Статус: {currentSocialStatus}");
            Debug.Log($"Общая репутация: {OverallReputation:F1}");
            Debug.Log($"Социальное влияние: {SocialInfluence:F1}");
            Debug.Log($"Сетевых связей: {networkConnections.Count}");
            
            foreach (var group in groupReputation.Keys)
            {
                Debug.Log($"- {group}: Репутация {groupReputation[group]:F1}, " +
                         $"Влияние {groupInfluence[group]:F1}, " +
                         $"Член: {groupMembership[group]}");
            }
        }

        [ContextMenu("Max Reputation")]
        private void DebugMaxReputation()
        {
            foreach (SocialGroup group in Enum.GetValues(typeof(SocialGroup)))
            {
                groupReputation[group] = 100f;
                groupInfluence[group] = 100f;
            }
        }

        #endregion
    }

    /// <summary>
    /// Сетевая связь
    /// </summary>
    [System.Serializable]
    public class NetworkConnection
    {
        public string contactName;              // Имя контакта
        public string title;                    // Должность
        public SocialGroup group;               // Группа
        public float strength;                  // Сила связи (0-100)
        public float establishedTime;           // Время установления
        public float influence;                 // Влиятельность контакта
    }

    /// <summary>
    /// Социальное одолжение
    /// </summary>
    [System.Serializable]
    public class SocialFavor
    {
        public FavorType type;                  // Тип одолжения
        public string description;              // Описание
        public float value;                     // Ценность
    }

    /// <summary>
    /// Типы взаимодействий
    /// </summary>
    public enum SocialInteractionType
    {
        Networking,         // Нетворкинг
        Favor,              // Одолжение
        Business,           // Деловая встреча
        Charity,            // Благотворительность
        Ceremony            // Церемония
    }

    /// <summary>
    /// Типы одолжений
    /// </summary>
    public enum FavorType
    {
        Business,           // Деловая помощь
        Legal,              // Юридическая помощь
        Reputation,         // Репутационная помощь
        Information         // Информационная помощь
    }

    /// <summary>
    /// Данные SocialSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class SocialSystemSaveData
    {
        public Dictionary<SocialGroup, float> groupReputation;
        public Dictionary<SocialGroup, float> groupInfluence;
        public Dictionary<SocialGroup, float> groupLoyalty;
        public Dictionary<SocialGroup, bool> groupMembership;
        public List<NetworkConnection> networkConnections;
        public Dictionary<string, float> lastInteractionTime;
        public SocialStatus currentSocialStatus;
    }
}
