using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система черного рынка
    /// </summary>
    public class BlackMarketSystem : MonoBehaviour
    {
        [Header("Настройки черного рынка")]
        [SerializeField] private bool enableBlackMarket = true;
        [SerializeField] private float marketRefreshInterval = 7200f;       // Интервал обновления товаров (секунды)
        [SerializeField] private int maxActiveDeals = 5;                    // Максимум активных сделок
        [SerializeField] private float baseDetectionRisk = 0.15f;           // Базовый риск обнаружения

        [Header("Экономические настройки")]
        [SerializeField] private double baseProfitMultiplier = 2.5;         // Базовый множитель прибыли
        [SerializeField] private double highRiskProfitMultiplier = 5.0;     // Множитель для высокорисковых сделок
        [SerializeField] private double contraband_BasePrice = 100;         // Базовая цена контрабанды
        [SerializeField] private double stolenGoods_BasePrice = 50;         // Базовая цена краденого

        [Header("Настройки группировок")]
        [SerializeField] private float loyaltyRequirement = 40f;            // Требуемая лояльность для доступа
        [SerializeField] private float trustBuildRate = 2f;                 // Скорость накопления доверия
        [SerializeField] private float trustDecayRate = 1f;                 // Скорость потери доверия

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool noDetectionRisk = false;
        [SerializeField] private bool freeMarketAccess = false;

        // Состояние системы
        private List<BlackMarketDeal> availableDeals = new List<BlackMarketDeal>();
        private List<BlackMarketDeal> activeDeals = new List<BlackMarketDeal>();
        private List<BlackMarketDeal> completedDeals = new List<BlackMarketDeal>();
        private Dictionary<CriminalGroup, float> groupTrust = new Dictionary<CriminalGroup, float>();
        private Dictionary<CriminalGroup, bool> groupAccess = new Dictionary<CriminalGroup, bool>();
        private float lastMarketRefresh = 0f;
        private float playerNotoriety = 0f;                                 // Известность игрока в криминальном мире

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private FriendSystem friendSystem;
        private SocialSystem socialSystem;
        private FineSystem fineSystem;
        private LocationManager locationManager;
        private NotificationSystem notificationSystem;

        // События
        public event Action<BlackMarketDeal> OnDealAvailable;
        public event Action<BlackMarketDeal> OnDealStarted;
        public event Action<BlackMarketDeal> OnDealCompleted;
        public event Action<BlackMarketDeal> OnDealFailed;
        public event Action<CriminalGroup> OnGroupAccessGranted;
        public event Action<CriminalGroup> OnGroupAccessRevoked;
        public event Action OnMarketRaided;

        // Свойства
        public List<BlackMarketDeal> AvailableDeals => new List<BlackMarketDeal>(availableDeals);
        public List<BlackMarketDeal> ActiveDeals => new List<BlackMarketDeal>(activeDeals);
        public float PlayerNotoriety => playerNotoriety;
        public bool HasMarketAccess => HasAnyGroupAccess();
        public int CompletedDealsCount => completedDeals.Count;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = FindObjectOfType<SkillSystem>();
            friendSystem = FindObjectOfType<FriendSystem>();
            socialSystem = FindObjectOfType<SocialSystem>();
            fineSystem = FindObjectOfType<FineSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
        }

        private void Start()
        {
            if (enableBlackMarket)
            {
                InitializeBlackMarket();
                SubscribeToEvents();
            }

            Debug.Log("BlackMarketSystem инициализирован");
        }

        private void Update()
        {
            if (enableBlackMarket)
            {
                UpdateBlackMarket();
                UpdateActiveDeals();
                CheckMarketRefresh();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация черного рынка
        /// </summary>
        private void InitializeBlackMarket()
        {
            // Инициализировать доверие группировок
            foreach (CriminalGroup group in Enum.GetValues(typeof(CriminalGroup)))
            {
                groupTrust[group] = 0f;
                groupAccess[group] = false;
            }

            // Начальный доступ для мелких группировок
            if (freeMarketAccess)
            {
                foreach (CriminalGroup group in Enum.GetValues(typeof(CriminalGroup)))
                {
                    groupAccess[group] = true;
                    groupTrust[group] = 50f;
                }
            }
            else
            {
                groupAccess[CriminalGroup.StreetThugs] = true;
                groupTrust[CriminalGroup.StreetThugs] = 20f;
            }

            lastMarketRefresh = Time.time;
            RefreshMarket();
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (friendSystem != null)
            {
                friendSystem.OnFriendAdded += OnFriendAdded;
            }

            if (socialSystem != null)
            {
                socialSystem.OnReputationChanged += OnReputationChanged;
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
            }

            if (socialSystem != null)
            {
                socialSystem.OnReputationChanged -= OnReputationChanged;
            }
        }

        #endregion

        #region Black Market Update

        /// <summary>
        /// Обновить черный рынок
        /// </summary>
        private void UpdateBlackMarket()
        {
            UpdateGroupTrust();
            UpdatePlayerNotoriety();
            CheckGroupAccess();
        }

        /// <summary>
        /// Обновить доверие группировок
        /// </summary>
        private void UpdateGroupTrust()
        {
            foreach (CriminalGroup group in Enum.GetValues(typeof(CriminalGroup)))
            {
                if (groupTrust[group] > 0f && !HasRecentDealsWithGroup(group))
                {
                    // Естественная деградация доверия
                    float decay = trustDecayRate * Time.deltaTime;
                    groupTrust[group] = Mathf.Max(0f, groupTrust[group] - decay);
                    
                    // Потеря доступа при низком доверии
                    if (groupTrust[group] < loyaltyRequirement * 0.5f)
                    {
                        if (groupAccess[group])
                        {
                            groupAccess[group] = false;
                            OnGroupAccessRevoked?.Invoke(group);
                            
                            if (notificationSystem != null)
                            {
                                notificationSystem.ShowNotification("Потеря доступа", 
                                    $"Вы потеряли доверие {GetGroupName(group)}", 
                                    NotificationType.Warning);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Обновить известность игрока
        /// </summary>
        private void UpdatePlayerNotoriety()
        {
            // Известность основана на количестве и сложности сделок
            float newNotoriety = 0f;
            
            foreach (var deal in completedDeals)
            {
                newNotoriety += GetDealNotorietyValue(deal);
            }
            
            playerNotoriety = newNotoriety;
        }

        /// <summary>
        /// Проверить доступ к группировкам
        /// </summary>
        private void CheckGroupAccess()
        {
            foreach (CriminalGroup group in Enum.GetValues(typeof(CriminalGroup)))
            {
                if (!groupAccess[group] && CanAccessGroup(group))
                {
                    GrantGroupAccess(group);
                }
            }
        }

        /// <summary>
        /// Проверить обновление рынка
        /// </summary>
        private void CheckMarketRefresh()
        {
            if (Time.time - lastMarketRefresh >= marketRefreshInterval)
            {
                RefreshMarket();
                lastMarketRefresh = Time.time;
            }
        }

        #endregion

        #region Market Management

        /// <summary>
        /// Обновить рынок
        /// </summary>
        private void RefreshMarket()
        {
            availableDeals.Clear();
            
            // Генерировать новые сделки для каждой доступной группировки
            foreach (CriminalGroup group in Enum.GetValues(typeof(CriminalGroup)))
            {
                if (groupAccess[group])
                {
                    int dealCount = GetDealCountForGroup(group);
                    for (int i = 0; i < dealCount; i++)
                    {
                        var deal = GenerateDeal(group);
                        if (deal != null)
                        {
                            availableDeals.Add(deal);
                            OnDealAvailable?.Invoke(deal);
                        }
                    }
                }
            }

            if (notificationSystem != null && availableDeals.Count > 0)
            {
                notificationSystem.ShowNotification("Новые предложения", 
                    $"Доступно {availableDeals.Count} новых сделок", NotificationType.Info);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Рынок обновлен. Доступно сделок: {availableDeals.Count}");
            }
        }

        /// <summary>
        /// Получить количество сделок для группировки
        /// </summary>
        private int GetDealCountForGroup(CriminalGroup group)
        {
            int baseCount = 1;
            
            // Больше сделок от более влиятельных группировок
            switch (group)
            {
                case CriminalGroup.StreetThugs: return 2;
                case CriminalGroup.SmugglingRing: return 3;
                case CriminalGroup.OrganizedCrime: return 2;
                case CriminalGroup.Cartel: return 1;
                case CriminalGroup.CyberCriminals: return 2;
                default: return baseCount;
            }
        }

        /// <summary>
        /// Сгенерировать сделку
        /// </summary>
        private BlackMarketDeal GenerateDeal(CriminalGroup group)
        {
            var dealTypes = GetAvailableDealTypes(group);
            if (dealTypes.Count == 0)
                return null;

            BlackMarketDealType dealType = dealTypes[UnityEngine.Random.Range(0, dealTypes.Count)];
            
            var deal = new BlackMarketDeal
            {
                id = System.Guid.NewGuid().ToString(),
                dealType = dealType,
                group = group,
                title = GetDealTitle(dealType),
                description = GetDealDescription(dealType, group),
                investment = CalculateDealInvestment(dealType, group),
                potentialProfit = CalculateDealProfit(dealType, group),
                riskLevel = CalculateDealRisk(dealType, group),
                duration = GetDealDuration(dealType),
                requirements = GetDealRequirements(dealType, group),
                availableUntil = Time.time + 86400f, // Доступно 24 часа
                isActive = false,
                isCompleted = false
            };

            return deal;
        }

        /// <summary>
        /// Получить доступные типы сделок для группировки
        /// </summary>
        private List<BlackMarketDealType> GetAvailableDealTypes(CriminalGroup group)
        {
            var dealTypes = new List<BlackMarketDealType>();
            
            switch (group)
            {
                case CriminalGroup.StreetThugs:
                    dealTypes.AddRange(new[] { 
                        BlackMarketDealType.StolenGoods, 
                        BlackMarketDealType.DrugDealing,
                        BlackMarketDealType.Protection
                    });
                    break;
                    
                case CriminalGroup.SmugglingRing:
                    dealTypes.AddRange(new[] { 
                        BlackMarketDealType.Contraband, 
                        BlackMarketDealType.StolenGoods,
                        BlackMarketDealType.WeaponTrafficking
                    });
                    break;
                    
                case CriminalGroup.OrganizedCrime:
                    dealTypes.AddRange(new[] { 
                        BlackMarketDealType.MoneyLaundering, 
                        BlackMarketDealType.Protection,
                        BlackMarketDealType.CorruptionScheme
                    });
                    break;
                    
                case CriminalGroup.Cartel:
                    dealTypes.AddRange(new[] { 
                        BlackMarketDealType.DrugDealing, 
                        BlackMarketDealType.WeaponTrafficking,
                        BlackMarketDealType.HumanTrafficking
                    });
                    break;
                    
                case CriminalGroup.CyberCriminals:
                    dealTypes.AddRange(new[] { 
                        BlackMarketDealType.CyberCrime, 
                        BlackMarketDealType.IdentityTheft,
                        BlackMarketDealType.MoneyLaundering
                    });
                    break;
            }
            
            // Фильтровать по требованиям игрока
            dealTypes.RemoveAll(dealType => !MeetsPlayerRequirements(dealType));
            
            return dealTypes;
        }

        /// <summary>
        /// Проверить соответствие требованиям игрока
        /// </summary>
        private bool MeetsPlayerRequirements(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.CyberCrime:
                    return skillSystem != null && skillSystem.GetSkillLevel(SkillType.Computer) >= 5f;
                case BlackMarketDealType.WeaponTrafficking:
                    return playerNotoriety >= 50f;
                case BlackMarketDealType.HumanTrafficking:
                    return playerNotoriety >= 80f && groupTrust[CriminalGroup.Cartel] >= 70f;
                default:
                    return true;
            }
        }

        #endregion

        #region Deal Management

        /// <summary>
        /// Начать сделку
        /// </summary>
        public bool StartDeal(string dealId)
        {
            var deal = availableDeals.Find(d => d.id == dealId);
            if (deal == null)
                return false;

            if (activeDeals.Count >= maxActiveDeals)
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Слишком много сделок", 
                        "Завершите другие сделки перед началом новой", NotificationType.Warning);
                }
                return false;
            }

            if (!CanAffordDeal(deal))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Недостаточно денег", 
                        $"Нужно ${deal.investment} для участия в сделке", NotificationType.Warning);
                }
                return false;
            }

            if (!MeetsDealRequirements(deal))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Не выполнены требования", 
                        "У вас недостаточный уровень для этой сделки", NotificationType.Warning);
                }
                return false;
            }

            // Инвестировать деньги
            if (moneySystem != null)
            {
                moneySystem.SpendMoney(deal.investment, $"Инвестиция в сделку: {deal.title}");
            }

            // Переместить сделку в активные
            availableDeals.Remove(deal);
            deal.isActive = true;
            deal.startTime = Time.time;
            activeDeals.Add(deal);

            OnDealStarted?.Invoke(deal);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Сделка начата", 
                    $"Начата сделка: {deal.title}", NotificationType.Info);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Начата сделка: {deal.title} с группировкой {deal.group}");
            }

            return true;
        }

        /// <summary>
        /// Обновить активные сделки
        /// </summary>
        private void UpdateActiveDeals()
        {
            for (int i = activeDeals.Count - 1; i >= 0; i--)
            {
                var deal = activeDeals[i];
                
                if (Time.time - deal.startTime >= deal.duration)
                {
                    CompleteDeal(deal);
                }
            }
        }

        /// <summary>
        /// Завершить сделку
        /// </summary>
        private void CompleteDeal(BlackMarketDeal deal)
        {
            activeDeals.Remove(deal);
            deal.isCompleted = true;
            deal.completionTime = Time.time;

            bool isSuccess = DetermineDealSuccess(deal);
            
            if (isSuccess)
            {
                ProcessSuccessfulDeal(deal);
            }
            else
            {
                ProcessFailedDeal(deal);
            }

            completedDeals.Add(deal);
        }

        /// <summary>
        /// Определить успех сделки
        /// </summary>
        private bool DetermineDealSuccess(BlackMarketDeal deal)
        {
            float successChance = CalculateDealSuccessChance(deal);
            
            if (noDetectionRisk)
                successChance = 1f;

            return UnityEngine.Random.value < successChance;
        }

        /// <summary>
        /// Рассчитать шанс успеха сделки
        /// </summary>
        private float CalculateDealSuccessChance(BlackMarketDeal deal)
        {
            float baseChance = 0.7f; // 70% базовый шанс
            
            // Модификатор риска
            baseChance -= deal.riskLevel * 0.3f;
            
            // Бонус от доверия группировки
            baseChance += groupTrust[deal.group] * 0.003f;
            
            // Бонус от навыков
            if (skillSystem != null)
            {
                float stealth = skillSystem.GetSkillLevel(SkillType.Stealth);
                float streetwise = skillSystem.GetSkillLevel(SkillType.Streetwise);
                baseChance += (stealth + streetwise) * 0.02f;
            }
            
            // Штраф от активности полиции
            // TODO: Интеграция с PoliceSystem
            
            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Обработать успешную сделку
        /// </summary>
        private void ProcessSuccessfulDeal(BlackMarketDeal deal)
        {
            // Получить прибыль
            if (moneySystem != null)
            {
                double totalProfit = deal.investment + deal.potentialProfit;
                moneySystem.AddMoney(totalProfit, $"Прибыль от сделки: {deal.title}");
            }

            // Увеличить доверие группировки
            ChangeGroupTrust(deal.group, trustBuildRate * 3f, "Успешная сделка");
            
            // Увеличить известность
            IncreaseNotoriety(GetDealNotorietyValue(deal));

            // Улучшить криминальную репутацию
            if (socialSystem != null)
            {
                socialSystem.ChangeReputation(SocialGroup.Criminal, 2f, "Успешная сделка");
            }

            // Добавить опыт навыков
            if (skillSystem != null)
            {
                skillSystem.AddSkillExperience(SkillType.Streetwise, 10f, "Черный рынок");
                
                if (deal.dealType == BlackMarketDealType.CyberCrime)
                {
                    skillSystem.AddSkillExperience(SkillType.Computer, 15f, "Кибер-преступления");
                }
            }

            OnDealCompleted?.Invoke(deal);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Сделка завершена", 
                    $"Успешно завершена сделка: {deal.title}\nПрибыль: ${deal.potentialProfit}", 
                    NotificationType.Money);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Успешно завершена сделка: {deal.title}, прибыль: ${deal.potentialProfit}");
            }
        }

        /// <summary>
        /// Обработать неудачную сделку
        /// </summary>
        private void ProcessFailedDeal(BlackMarketDeal deal)
        {
            // Потерять инвестицию (уже потрачена)
            
            // Снизить доверие группировки
            ChangeGroupTrust(deal.group, -trustBuildRate * 2f, "Неудачная сделка");

            // Риск ареста или штрафов
            ProcessDealConsequences(deal);

            OnDealFailed?.Invoke(deal);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Сделка провалилась", 
                    $"Провалена сделка: {deal.title}\nПотеряно: ${deal.investment}", 
                    NotificationType.Warning);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Провалена сделка: {deal.title}, потери: ${deal.investment}");
            }
        }

        /// <summary>
        /// Обработать последствия провала сделки
        /// </summary>
        private void ProcessDealConsequences(BlackMarketDeal deal)
        {
            float consequenceChance = deal.riskLevel * 0.5f;
            
            if (UnityEngine.Random.value < consequenceChance)
            {
                // Определить тип последствий
                var consequences = new[] { "fine", "arrest_risk", "reputation_loss", "injury" };
                string consequence = consequences[UnityEngine.Random.Range(0, consequences.Length)];
                
                switch (consequence)
                {
                    case "fine":
                        ProcessFineConsequence(deal);
                        break;
                    case "arrest_risk":
                        ProcessArrestRiskConsequence(deal);
                        break;
                    case "reputation_loss":
                        ProcessReputationLossConsequence(deal);
                        break;
                    case "injury":
                        ProcessInjuryConsequence(deal);
                        break;
                }
            }
        }

        /// <summary>
        /// Обработать штраф как последствие
        /// </summary>
        private void ProcessFineConsequence(BlackMarketDeal deal)
        {
            if (fineSystem != null)
            {
                ViolationType violationType = GetViolationTypeForDeal(deal.dealType);
                fineSystem.CommitViolation(violationType);
            }
        }

        /// <summary>
        /// Обработать риск ареста
        /// </summary>
        private void ProcessArrestRiskConsequence(BlackMarketDeal deal)
        {
            // TODO: Интеграция с PoliceSystem для повышения розыска
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Внимание полиции", 
                    "Ваша деятельность привлекла внимание правоохранительных органов", 
                    NotificationType.Critical);
            }
        }

        /// <summary>
        /// Обработать потерю репутации
        /// </summary>
        private void ProcessReputationLossConsequence(BlackMarketDeal deal)
        {
            if (socialSystem != null)
            {
                socialSystem.ChangeReputation(SocialGroup.Criminal, -5f, "Провал на черном рынке");
                socialSystem.ChangeReputation(SocialGroup.Community, -3f, "Криминальная деятельность");
            }
        }

        /// <summary>
        /// Обработать травму
        /// </summary>
        private void ProcessInjuryConsequence(BlackMarketDeal deal)
        {
            if (playerStats != null)
            {
                float healthLoss = UnityEngine.Random.Range(10f, 30f);
                playerStats.ChangeHealth(-healthLoss);
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Травма", 
                        "Вы получили травму во время сделки", NotificationType.Warning);
                }
            }
        }

        #endregion

        #region Group Access Management

        /// <summary>
        /// Можно ли получить доступ к группировке
        /// </summary>
        private bool CanAccessGroup(CriminalGroup group)
        {
            switch (group)
            {
                case CriminalGroup.StreetThugs:
                    return true; // Всегда доступно
                    
                case CriminalGroup.SmugglingRing:
                    return playerNotoriety >= 20f || HasCriminalFriend();
                    
                case CriminalGroup.OrganizedCrime:
                    return playerNotoriety >= 50f && groupTrust[CriminalGroup.StreetThugs] >= 60f;
                    
                case CriminalGroup.Cartel:
                    return playerNotoriety >= 80f && groupTrust[CriminalGroup.SmugglingRing] >= 70f;
                    
                case CriminalGroup.CyberCriminals:
                    return skillSystem != null && skillSystem.GetSkillLevel(SkillType.Computer) >= 7f;
                    
                default:
                    return false;
            }
        }

        /// <summary>
        /// Предоставить доступ к группировке
        /// </summary>
        private void GrantGroupAccess(CriminalGroup group)
        {
            groupAccess[group] = true;
            groupTrust[group] = 30f; // Начальное доверие
            
            OnGroupAccessGranted?.Invoke(group);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Новый доступ", 
                    $"Получен доступ к {GetGroupName(group)}", NotificationType.Info);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Получен доступ к группировке: {group}");
            }
        }

        /// <summary>
        /// Изменить доверие группировки
        /// </summary>
        private void ChangeGroupTrust(CriminalGroup group, float amount, string reason = "")
        {
            float oldTrust = groupTrust[group];
            groupTrust[group] = Mathf.Clamp(groupTrust[group] + amount, 0f, 100f);
            
            if (showDebugInfo && Mathf.Abs(amount) >= 2f)
            {
                Debug.Log($"Доверие {group}: {oldTrust:F1} → {groupTrust[group]:F1} ({reason})");
            }
        }

        /// <summary>
        /// Увеличить известность
        /// </summary>
        private void IncreaseNotoriety(float amount)
        {
            float oldNotoriety = playerNotoriety;
            playerNotoriety += amount;
            
            if (showDebugInfo)
            {
                Debug.Log($"Известность: {oldNotoriety:F1} → {playerNotoriety:F1} (+{amount:F1})");
            }
        }

        /// <summary>
        /// Есть ли доступ к любой группировке
        /// </summary>
        private bool HasAnyGroupAccess()
        {
            foreach (bool access in groupAccess.Values)
            {
                if (access) return true;
            }
            return false;
        }

        /// <summary>
        /// Есть ли криминальный друг
        /// </summary>
        private bool HasCriminalFriend()
        {
            if (friendSystem == null)
                return false;

            var friends = friendSystem.Friends;
            foreach (var friend in friends.Values)
            {
                if (friend.type == FriendType.Criminal)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Есть ли недавние сделки с группировкой
        /// </summary>
        private bool HasRecentDealsWithGroup(CriminalGroup group)
        {
            float recentTime = 604800f; // 1 неделя
            
            foreach (var deal in completedDeals)
            {
                if (deal.group == group && Time.time - deal.completionTime < recentTime)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Может ли позволить себе сделку
        /// </summary>
        private bool CanAffordDeal(BlackMarketDeal deal)
        {
            return moneySystem != null && moneySystem.CanAfford(deal.investment);
        }

        /// <summary>
        /// Соответствует ли требованиям сделки
        /// </summary>
        private bool MeetsDealRequirements(BlackMarketDeal deal)
        {
            if (deal.requirements.minimumNotoriety > playerNotoriety)
                return false;
                
            if (deal.requirements.minimumTrust > groupTrust[deal.group])
                return false;
                
            if (skillSystem != null && deal.requirements.requiredSkill != SkillType.None)
            {
                float skillLevel = skillSystem.GetSkillLevel(deal.requirements.requiredSkill);
                if (skillLevel < deal.requirements.minimumSkillLevel)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Рассчитать инвестицию для сделки
        /// </summary>
        private double CalculateDealInvestment(BlackMarketDealType dealType, CriminalGroup group)
        {
            double baseInvestment = GetBaseDealInvestment(dealType);
            
            // Модификатор группировки
            baseInvestment *= GetGroupInvestmentMultiplier(group);
            
            // Случайная вариация
            baseInvestment *= UnityEngine.Random.Range(0.7f, 1.3f);
            
            return Math.Round(baseInvestment, 2);
        }

        /// <summary>
        /// Рассчитать прибыль для сделки
        /// </summary>
        private double CalculateDealProfit(BlackMarketDealType dealType, CriminalGroup group)
        {
            double investment = CalculateDealInvestment(dealType, group);
            double multiplier = GetProfitMultiplier(dealType);
            
            return Math.Round(investment * multiplier, 2);
        }

        /// <summary>
        /// Рассчитать риск сделки
        /// </summary>
        private float CalculateDealRisk(BlackMarketDealType dealType, CriminalGroup group)
        {
            float baseRisk = GetBaseDealRisk(dealType);
            
            // Модификатор группировки
            baseRisk *= GetGroupRiskMultiplier(group);
            
            // Модификатор текущего статуса игрока
            if (socialSystem != null)
            {
                float criminalRep = socialSystem.GroupReputation[SocialGroup.Criminal];
                baseRisk *= (1f - criminalRep * 0.003f); // Лучшая репутация = меньший риск
            }
            
            return Mathf.Clamp01(baseRisk);
        }

        /// <summary>
        /// Получить базовую инвестицию для типа сделки
        /// </summary>
        private double GetBaseDealInvestment(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods: return 200;
                case BlackMarketDealType.Contraband: return 500;
                case BlackMarketDealType.DrugDealing: return 300;
                case BlackMarketDealType.WeaponTrafficking: return 1000;
                case BlackMarketDealType.MoneyLaundering: return 2000;
                case BlackMarketDealType.CyberCrime: return 800;
                case BlackMarketDealType.Protection: return 400;
                case BlackMarketDealType.CorruptionScheme: return 1500;
                case BlackMarketDealType.HumanTrafficking: return 3000;
                case BlackMarketDealType.IdentityTheft: return 600;
                default: return 300;
            }
        }

        /// <summary>
        /// Получить множитель прибыли
        /// </summary>
        private double GetProfitMultiplier(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods: return 1.8;
                case BlackMarketDealType.Contraband: return 2.5;
                case BlackMarketDealType.DrugDealing: return 3.0;
                case BlackMarketDealType.WeaponTrafficking: return 4.0;
                case BlackMarketDealType.MoneyLaundering: return 1.2;
                case BlackMarketDealType.CyberCrime: return 2.8;
                case BlackMarketDealType.Protection: return 2.0;
                case BlackMarketDealType.CorruptionScheme: return 3.5;
                case BlackMarketDealType.HumanTrafficking: return 5.0;
                case BlackMarketDealType.IdentityTheft: return 2.2;
                default: return baseProfitMultiplier;
            }
        }

        /// <summary>
        /// Получить базовый риск для типа сделки
        /// </summary>
        private float GetBaseDealRisk(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods: return 0.2f;
                case BlackMarketDealType.Contraband: return 0.4f;
                case BlackMarketDealType.DrugDealing: return 0.5f;
                case BlackMarketDealType.WeaponTrafficking: return 0.7f;
                case BlackMarketDealType.MoneyLaundering: return 0.3f;
                case BlackMarketDealType.CyberCrime: return 0.4f;
                case BlackMarketDealType.Protection: return 0.3f;
                case BlackMarketDealType.CorruptionScheme: return 0.6f;
                case BlackMarketDealType.HumanTrafficking: return 0.9f;
                case BlackMarketDealType.IdentityTheft: return 0.5f;
                default: return baseDetectionRisk;
            }
        }

        /// <summary>
        /// Получить множитель инвестиции группировки
        /// </summary>
        private float GetGroupInvestmentMultiplier(CriminalGroup group)
        {
            switch (group)
            {
                case CriminalGroup.StreetThugs: return 0.8f;
                case CriminalGroup.SmugglingRing: return 1.2f;
                case CriminalGroup.OrganizedCrime: return 1.5f;
                case CriminalGroup.Cartel: return 2.0f;
                case CriminalGroup.CyberCriminals: return 1.3f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Получить множитель риска группировки
        /// </summary>
        private float GetGroupRiskMultiplier(CriminalGroup group)
        {
            switch (group)
            {
                case CriminalGroup.StreetThugs: return 1.2f;     // Непрофессиональные
                case CriminalGroup.SmugglingRing: return 0.9f;
                case CriminalGroup.OrganizedCrime: return 0.7f;  // Более профессиональные
                case CriminalGroup.Cartel: return 0.6f;
                case CriminalGroup.CyberCriminals: return 0.8f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Получить длительность сделки
        /// </summary>
        private float GetDealDuration(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods: return 1800f;    // 30 минут
                case BlackMarketDealType.Contraband: return 3600f;     // 1 час
                case BlackMarketDealType.DrugDealing: return 2700f;    // 45 минут
                case BlackMarketDealType.WeaponTrafficking: return 7200f; // 2 часа
                case BlackMarketDealType.MoneyLaundering: return 10800f;  // 3 часа
                case BlackMarketDealType.CyberCrime: return 5400f;     // 1.5 часа
                case BlackMarketDealType.Protection: return 14400f;    // 4 часа
                case BlackMarketDealType.CorruptionScheme: return 21600f; // 6 часов
                case BlackMarketDealType.HumanTrafficking: return 28800f; // 8 часов
                case BlackMarketDealType.IdentityTheft: return 7200f;  // 2 часа
                default: return 3600f;
            }
        }

        /// <summary>
        /// Получить требования для сделки
        /// </summary>
        private DealRequirements GetDealRequirements(BlackMarketDealType dealType, CriminalGroup group)
        {
            var requirements = new DealRequirements();
            
            switch (dealType)
            {
                case BlackMarketDealType.CyberCrime:
                    requirements.requiredSkill = SkillType.Computer;
                    requirements.minimumSkillLevel = 5f;
                    break;
                case BlackMarketDealType.WeaponTrafficking:
                    requirements.minimumNotoriety = 40f;
                    requirements.minimumTrust = 50f;
                    break;
                case BlackMarketDealType.HumanTrafficking:
                    requirements.minimumNotoriety = 70f;
                    requirements.minimumTrust = 80f;
                    break;
                case BlackMarketDealType.CorruptionScheme:
                    requirements.minimumNotoriety = 60f;
                    requirements.minimumTrust = 70f;
                    break;
            }
            
            return requirements;
        }

        /// <summary>
        /// Получить ценность известности для сделки
        /// </summary>
        private float GetDealNotorietyValue(BlackMarketDeal deal)
        {
            float baseValue = deal.riskLevel * 10f;
            
            switch (deal.dealType)
            {
                case BlackMarketDealType.HumanTrafficking: return baseValue * 3f;
                case BlackMarketDealType.WeaponTrafficking: return baseValue * 2f;
                case BlackMarketDealType.CorruptionScheme: return baseValue * 2.5f;
                default: return baseValue;
            }
        }

        /// <summary>
        /// Получить тип нарушения для сделки
        /// </summary>
        private ViolationType GetViolationTypeForDeal(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods: return ViolationType.Theft;
                case BlackMarketDealType.DrugDealing: return ViolationType.IllegalBusiness;
                case BlackMarketDealType.WeaponTrafficking: return ViolationType.IllegalBusiness;
                case BlackMarketDealType.CyberCrime: return ViolationType.IllegalBusiness;
                default: return ViolationType.IllegalBusiness;
            }
        }

        /// <summary>
        /// Получить заголовок сделки
        /// </summary>
        private string GetDealTitle(BlackMarketDealType dealType)
        {
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods: return "Сбыт краденого";
                case BlackMarketDealType.Contraband: return "Контрабанда";
                case BlackMarketDealType.DrugDealing: return "Торговля наркотиками";
                case BlackMarketDealType.WeaponTrafficking: return "Торговля оружием";
                case BlackMarketDealType.MoneyLaundering: return "Отмывание денег";
                case BlackMarketDealType.CyberCrime: return "Кибер-преступления";
                case BlackMarketDealType.Protection: return "Рэкет";
                case BlackMarketDealType.CorruptionScheme: return "Коррупционная схема";
                case BlackMarketDealType.HumanTrafficking: return "Торговля людьми";
                case BlackMarketDealType.IdentityTheft: return "Кража личности";
                default: return dealType.ToString();
            }
        }

        /// <summary>
        /// Получить описание сделки
        /// </summary>
        private string GetDealDescription(BlackMarketDealType dealType, CriminalGroup group)
        {
            string groupName = GetGroupName(group);
            
            switch (dealType)
            {
                case BlackMarketDealType.StolenGoods:
                    return $"{groupName} предлагает помочь со сбытом краденых товаров";
                case BlackMarketDealType.Contraband:
                    return $"{groupName} организует контрабандную операцию";
                case BlackMarketDealType.DrugDealing:
                    return $"{groupName} предлагает участие в торговле наркотиками";
                default:
                    return $"Сделка с {groupName}: {GetDealTitle(dealType)}";
            }
        }

        /// <summary>
        /// Получить название группировки
        /// </summary>
        private string GetGroupName(CriminalGroup group)
        {
            switch (group)
            {
                case CriminalGroup.StreetThugs: return "Уличные бандиты";
                case CriminalGroup.SmugglingRing: return "Контрабандисты";
                case CriminalGroup.OrganizedCrime: return "Организованная преступность";
                case CriminalGroup.Cartel: return "Картель";
                case CriminalGroup.CyberCriminals: return "Кибер-преступники";
                default: return group.ToString();
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public BlackMarketSystemSaveData GetSaveData()
        {
            return new BlackMarketSystemSaveData
            {
                availableDeals = new List<BlackMarketDeal>(availableDeals),
                activeDeals = new List<BlackMarketDeal>(activeDeals),
                completedDeals = new List<BlackMarketDeal>(completedDeals),
                groupTrust = new Dictionary<CriminalGroup, float>(groupTrust),
                groupAccess = new Dictionary<CriminalGroup, bool>(groupAccess),
                lastMarketRefresh = lastMarketRefresh,
                playerNotoriety = playerNotoriety
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(BlackMarketSystemSaveData data)
        {
            availableDeals = data.availableDeals ?? new List<BlackMarketDeal>();
            activeDeals = data.activeDeals ?? new List<BlackMarketDeal>();
            completedDeals = data.completedDeals ?? new List<BlackMarketDeal>();
            groupTrust = data.groupTrust ?? new Dictionary<CriminalGroup, float>();
            groupAccess = data.groupAccess ?? new Dictionary<CriminalGroup, bool>();
            lastMarketRefresh = data.lastMarketRefresh;
            playerNotoriety = data.playerNotoriety;

            if (showDebugInfo)
            {
                Debug.Log($"BlackMarketSystem загружен. Доступных сделок: {availableDeals.Count}, " +
                         $"Активных: {activeDeals.Count}, Известность: {playerNotoriety:F1}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик добавления друга
        /// </summary>
        private void OnFriendAdded(Friend friend)
        {
            if (friend.type == FriendType.Criminal)
            {
                // Криминальные друзья дают доступ к группировкам
                if (!groupAccess[CriminalGroup.SmugglingRing])
                {
                    GrantGroupAccess(CriminalGroup.SmugglingRing);
                }
            }
        }

        /// <summary>
        /// Обработчик изменения репутации
        /// </summary>
        private void OnReputationChanged(SocialGroup group, float newReputation)
        {
            if (group == SocialGroup.Criminal && newReputation >= 80f)
            {
                // Высокая криминальная репутация открывает доступ к картелю
                if (!groupAccess[CriminalGroup.Cartel] && CanAccessGroup(CriminalGroup.Cartel))
                {
                    GrantGroupAccess(CriminalGroup.Cartel);
                }
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Refresh Market")]
        private void DebugRefreshMarket()
        {
            RefreshMarket();
        }

        [ContextMenu("Grant All Access")]
        private void DebugGrantAllAccess()
        {
            foreach (CriminalGroup group in Enum.GetValues(typeof(CriminalGroup)))
            {
                GrantGroupAccess(group);
                groupTrust[group] = 80f;
            }
        }

        [ContextMenu("Show Market Status")]
        private void DebugShowMarketStatus()
        {
            Debug.Log($"Статистика черного рынка:");
            Debug.Log($"Известность: {playerNotoriety:F1}");
            Debug.Log($"Доступных сделок: {availableDeals.Count}");
            Debug.Log($"Активных сделок: {activeDeals.Count}");
            Debug.Log($"Завершенных сделок: {completedDeals.Count}");
            
            Debug.Log("Доступ к группировкам:");
            foreach (var group in groupAccess.Keys)
            {
                Debug.Log($"- {group}: {(groupAccess[group] ? "Да" : "Нет")}, " +
                         $"Доверие: {groupTrust[group]:F1}");
            }
        }

        [ContextMenu("Complete All Active Deals")]
        private void DebugCompleteActiveDeals()
        {
            for (int i = activeDeals.Count - 1; i >= 0; i--)
            {
                CompleteDeal(activeDeals[i]);
            }
        }

        #endregion
    }

    /// <summary>
    /// Сделка черного рынка
    /// </summary>
    [System.Serializable]
    public class BlackMarketDeal
    {
        public string id;                           // Уникальный ID
        public BlackMarketDealType dealType;        // Тип сделки
        public CriminalGroup group;                 // Группировка
        public string title;                        // Заголовок
        public string description;                  // Описание
        public double investment;                   // Требуемая инвестиция
        public double potentialProfit;              // Потенциальная прибыль
        public float riskLevel;                     // Уровень риска (0-1)
        public float duration;                      // Длительность (секунды)
        public DealRequirements requirements;       // Требования
        public float availableUntil;                // Доступно до
        public bool isActive;                       // Активна ли
        public bool isCompleted;                    // Завершена ли
        public float startTime;                     // Время начала
        public float completionTime;                // Время завершения
    }

    /// <summary>
    /// Требования для сделки
    /// </summary>
    [System.Serializable]
    public class DealRequirements
    {
        public float minimumNotoriety;              // Минимальная известность
        public float minimumTrust;                  // Минимальное доверие группировки
        public SkillType requiredSkill = SkillType.None; // Требуемый навык
        public float minimumSkillLevel;             // Минимальный уровень навыка
    }

    /// <summary>
    /// Данные BlackMarketSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class BlackMarketSystemSaveData
    {
        public List<BlackMarketDeal> availableDeals;
        public List<BlackMarketDeal> activeDeals;
        public List<BlackMarketDeal> completedDeals;
        public Dictionary<CriminalGroup, float> groupTrust;
        public Dictionary<CriminalGroup, bool> groupAccess;
        public float lastMarketRefresh;
        public float playerNotoriety;
    }
}
