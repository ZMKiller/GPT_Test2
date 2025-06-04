using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система боевых событий и их последствий
    /// </summary>
    public class CombatEvents : MonoBehaviour
    {
        [Header("Настройки боевых событий")]
        [SerializeField] private bool enableCombatEvents = true;
        [SerializeField] private float eventCheckInterval = 60f;       // Интервал проверки событий
        [SerializeField] private float baseEventChance = 0.1f;         // Базовый шанс события
        [SerializeField] private bool allowInjuries = true;            // Разрешить травмы
        [SerializeField] private bool allowRetaliation = true;         // Разрешить месть

        [Header("Настройки травм")]
        [SerializeField] private float injuryChance = 0.3f;            // Шанс травмы после боя
        [SerializeField] private float severeInjuryChance = 0.1f;      // Шанс серьезной травмы
        [SerializeField] private int injuryDuration = 300;             // Длительность травмы (секунды)
        [SerializeField] private float injuryStatReduction = 0.2f;     // Снижение характеристик

        [Header("Настройки репутации")]
        [SerializeField] private float reputationGainPerWin = 5f;      // Репутация за победу
        [SerializeField] private float reputationLossPerLoss = 3f;     // Потеря репутации
        [SerializeField] private float maxReputationEffect = 50f;      // Максимальный эффект репутации

        [Header("Последствия боев")]
        [SerializeField] private float witnessReportChance = 0.4f;     // Шанс донесения свидетелей
        [SerializeField] private float hospitalChance = 0.2f;          // Шанс госпитализации
        [SerializeField] private double hospitalCost = 200;            // Стоимость лечения

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceInjuries = false;
        [SerializeField] private bool disableConsequences = false;

        // Состояние системы
        private float lastEventCheck = 0f;
        private List<CombatConsequenceData> activeConsequences = new List<CombatConsequenceData>();
        private Dictionary<EnemyType, int> enemyVictoryCount = new Dictionary<EnemyType, int>();
        private Dictionary<EnemyType, float> enemyRelationship = new Dictionary<EnemyType, float>();

        // Компоненты
        private CombatSystem combatSystem;
        private PlayerStats playerStats;
        private SkillSystem skillSystem;
        private MoneySystem moneySystem;
        private PoliceSystem policeSystem;
        private LocationManager locationManager;
        private NotificationSystem notificationSystem;
        private AchievementSystem achievementSystem;

        // События
        public event Action<InjuryType, float> OnPlayerInjured;
        public event Action<InjuryType> OnPlayerHealed;
        public event Action<float> OnReputationChanged;
        public event Action<CombatConsequence, object> OnCombatConsequence;
        public event Action<EnemyType, bool> OnEnemyRelationshipChanged; // bool = improved

        // Свойства
        public bool HasActiveInjuries => activeConsequences.Exists(c => c.consequenceType == CombatConsequence.Injury);
        public float CurrentReputation => GetCurrentReputation();
        public List<CombatConsequenceData> ActiveConsequences => new List<CombatConsequenceData>(activeConsequences);

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            combatSystem = FindObjectOfType<CombatSystem>();
            playerStats = FindObjectOfType<PlayerStats>();
            skillSystem = FindObjectOfType<SkillSystem>();
            moneySystem = FindObjectOfType<MoneySystem>();
            policeSystem = FindObjectOfType<PoliceSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
            achievementSystem = FindObjectOfType<AchievementSystem>();
        }

        private void Start()
        {
            if (enableCombatEvents)
            {
                SubscribeToEvents();
                InitializeEnemyRelationships();
            }

            Debug.Log("CombatEvents инициализирован");
        }

        private void Update()
        {
            if (enableCombatEvents)
            {
                UpdateActiveConsequences();
                CheckPeriodicEvents();
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
            if (combatSystem != null)
            {
                combatSystem.OnCombatEnded += OnCombatEnded;
                combatSystem.OnCombatAction += OnCombatAction;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (combatSystem != null)
            {
                combatSystem.OnCombatEnded -= OnCombatEnded;
                combatSystem.OnCombatAction -= OnCombatAction;
            }
        }

        /// <summary>
        /// Инициализировать отношения с врагами
        /// </summary>
        private void InitializeEnemyRelationships()
        {
            foreach (EnemyType enemyType in Enum.GetValues(typeof(EnemyType)))
            {
                enemyVictoryCount[enemyType] = 0;
                enemyRelationship[enemyType] = 0f; // Нейтральные отношения
            }
        }

        #endregion

        #region Combat Event Handling

        /// <summary>
        /// Обработчик окончания боя
        /// </summary>
        private void OnCombatEnded(CombatData combatData, CombatState endState)
        {
            if (disableConsequences)
                return;

            // Определить исход для игрока
            bool playerWon = endState == CombatState.PlayerWon;
            bool playerFled = endState == CombatState.PlayerFled;
            bool playerLost = endState == CombatState.PlayerLost;

            // Обработать последствия
            ProcessCombatConsequences(combatData, endState);

            // Обновить репутацию
            UpdateReputation(combatData, playerWon);

            // Обновить отношения с врагами
            UpdateEnemyRelationships(combatData, playerWon);

            // Проверить травмы
            if (allowInjuries && (playerLost || combatData.totalDamageReceived > 0))
            {
                CheckForInjuries(combatData, endState);
            }

            // Проверить свидетелей
            CheckWitnessReports(combatData);

            // Специальные события
            CheckSpecialCombatEvents(combatData, endState);

            if (showDebugInfo)
            {
                Debug.Log($"Обработаны последствия боя. Исход: {endState}, " +
                         $"Урон получен: {combatData.totalDamageReceived:F0}");
            }
        }

        /// <summary>
        /// Обработчик боевых действий
        /// </summary>
        private void OnCombatAction(CombatAction action, CombatResult result)
        {
            // Обработать особые результаты действий
            switch (result)
            {
                case CombatResult.Critical:
                    HandleCriticalHit(action);
                    break;

                case CombatResult.Stunned:
                    HandleStunEffect();
                    break;

                case CombatResult.Fled:
                    HandleFleeAttempt(action);
                    break;
            }
        }

        #endregion

        #region Consequence Processing

        /// <summary>
        /// Обработать последствия боя
        /// </summary>
        private void ProcessCombatConsequences(CombatData combatData, CombatState endState)
        {
            switch (endState)
            {
                case CombatState.PlayerWon:
                    ProcessVictoryConsequences(combatData);
                    break;

                case CombatState.PlayerLost:
                    ProcessDefeatConsequences(combatData);
                    break;

                case CombatState.PlayerFled:
                    ProcessFleeConsequences(combatData);
                    break;
            }
        }

        /// <summary>
        /// Обработать последствия победы
        /// </summary>
        private void ProcessVictoryConsequences(CombatData combatData)
        {
            // Положительные последствия
            ApplyConsequence(CombatConsequence.ReputationGain, reputationGainPerWin);
            
            // Опыт в бою
            if (skillSystem != null)
            {
                float experience = CalculateCombatExperience(combatData);
                skillSystem.AddSkillExperience(SkillType.Physical_Fitness, experience, "Победа в бою");
            }

            // Достижения
            CheckVictoryAchievements(combatData);

            // Шанс найти что-то ценное
            if (UnityEngine.Random.value < 0.2f)
            {
                ApplyConsequence(CombatConsequence.ItemFound, "лекарство");
            }

            if (showDebugInfo)
            {
                Debug.Log("Обработаны последствия победы");
            }
        }

        /// <summary>
        /// Обработать последствия поражения
        /// </summary>
        private void ProcessDefeatConsequences(CombatData combatData)
        {
            // Потеря денег
            if (moneySystem != null)
            {
                double currentMoney = moneySystem.GetMoneyDouble();
                double lossAmount = currentMoney * UnityEngine.Random.Range(0.1f, 0.3f);
                
                if (lossAmount > 0)
                {
                    moneySystem.SpendMoney(lossAmount, "Потеря в бою");
                    ApplyConsequence(CombatConsequence.MoneyLoss, lossAmount);
                }
            }

            // Потеря репутации
            ApplyConsequence(CombatConsequence.ReputationLoss, reputationLossPerLoss);

            // Возможная госпитализация
            if (UnityEngine.Random.value < hospitalChance)
            {
                ApplyConsequence(CombatConsequence.Hospitalization, hospitalCost);
            }

            // Возможное вмешательство полиции
            if (policeSystem != null && UnityEngine.Random.value < 0.3f)
            {
                policeSystem.ReportFighting();
            }

            if (showDebugInfo)
            {
                Debug.Log("Обработаны последствия поражения");
            }
        }

        /// <summary>
        /// Обработать последствия побега
        /// </summary>
        private void ProcessFleeConsequences(CombatData combatData)
        {
            // Небольшая потеря репутации
            ApplyConsequence(CombatConsequence.ReputationLoss, reputationLossPerLoss * 0.5f);

            // Потеря настроения
            if (playerStats != null)
            {
                playerStats.ChangeMood(-10f);
            }

            // Шанс преследования
            if (UnityEngine.Random.value < 0.3f)
            {
                ApplyConsequence(CombatConsequence.Pursuit, 120f); // 2 минуты преследования
            }

            if (showDebugInfo)
            {
                Debug.Log("Обработаны последствия побега");
            }
        }

        #endregion

        #region Injury System

        /// <summary>
        /// Проверить травмы после боя
        /// </summary>
        private void CheckForInjuries(CombatData combatData, CombatState endState)
        {
            if (!allowInjuries && !forceInjuries)
                return;

            float injuryRoll = UnityEngine.Random.value;
            float injuryThreshold = CalculateInjuryThreshold(combatData, endState);

            if (forceInjuries || injuryRoll < injuryThreshold)
            {
                InjuryType injuryType = DetermineInjuryType(combatData);
                float severity = CalculateInjurySeverity(combatData, endState);
                
                ApplyInjury(injuryType, severity);
            }
        }

        /// <summary>
        /// Рассчитать порог травмы
        /// </summary>
        private float CalculateInjuryThreshold(CombatData combatData, CombatState endState)
        {
            float threshold = injuryChance;

            // Увеличить шанс при большом полученном уроне
            float damageRatio = combatData.totalDamageReceived / (playerStats?.MaxHealth ?? 100f);
            threshold += damageRatio * 0.5f;

            // Увеличить шанс при поражении
            if (endState == CombatState.PlayerLost)
            {
                threshold += 0.3f;
            }

            // Снизить шанс при высокой физической форме
            if (skillSystem != null)
            {
                float fitness = skillSystem.GetSkillLevel(SkillType.Physical_Fitness);
                threshold -= fitness * 0.02f;
            }

            return Mathf.Clamp01(threshold);
        }

        /// <summary>
        /// Определить тип травмы
        /// </summary>
        private InjuryType DetermineInjuryType(CombatData combatData)
        {
            // Основано на типе урона и оружии
            float roll = UnityEngine.Random.value;

            if (roll < 0.3f)
                return InjuryType.Bruises;      // Синяки
            else if (roll < 0.5f)
                return InjuryType.Cut;          // Порезы
            else if (roll < 0.7f)
                return InjuryType.Sprain;       // Растяжение
            else if (roll < 0.9f)
                return InjuryType.Fracture;     // Перелом
            else
                return InjuryType.Concussion;   // Сотрясение
        }

        /// <summary>
        /// Рассчитать серьезность травмы
        /// </summary>
        private float CalculateInjurySeverity(CombatData combatData, CombatState endState)
        {
            float severity = UnityEngine.Random.Range(0.3f, 1f);

            // Увеличить при большом уроне
            float damageRatio = combatData.totalDamageReceived / (playerStats?.MaxHealth ?? 100f);
            severity += damageRatio * 0.5f;

            // Серьезная травма при поражении
            if (endState == CombatState.PlayerLost && UnityEngine.Random.value < severeInjuryChance)
            {
                severity = 1f;
            }

            return Mathf.Clamp01(severity);
        }

        /// <summary>
        /// Применить травму
        /// </summary>
        private void ApplyInjury(InjuryType injuryType, float severity)
        {
            var injuryData = new CombatConsequenceData
            {
                consequenceType = CombatConsequence.Injury,
                startTime = Time.time,
                duration = injuryDuration * severity,
                severity = severity,
                data = injuryType
            };

            activeConsequences.Add(injuryData);

            // Применить эффекты травмы
            ApplyInjuryEffects(injuryType, severity);

            OnPlayerInjured?.Invoke(injuryType, severity);
            OnCombatConsequence?.Invoke(CombatConsequence.Injury, injuryType);

            // Показать уведомление
            if (notificationSystem != null)
            {
                string message = GetInjuryMessage(injuryType, severity);
                notificationSystem.ShowNotification("Травма!", message, NotificationType.Warning);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Получена травма: {injuryType} (серьезность: {severity:F1})");
            }
        }

        /// <summary>
        /// Применить эффекты травмы
        /// </summary>
        private void ApplyInjuryEffects(InjuryType injuryType, float severity)
        {
            if (playerStats == null)
                return;

            float reductionAmount = injuryStatReduction * severity;
            float duration = injuryDuration * severity;

            switch (injuryType)
            {
                case InjuryType.Bruises:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.Mood, -reductionAmount * 20f, ModifierOperation.Add, 
                        "Синяки", duration));
                    break;

                case InjuryType.Cut:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.Health, -reductionAmount * 15f, ModifierOperation.Add, 
                        "Порезы", duration));
                    break;

                case InjuryType.Sprain:
                    if (skillSystem != null)
                    {
                        skillSystem.AddSkillModifier(new SkillModifier(
                            SkillType.Physical_Fitness, -reductionAmount, ModifierOperation.Multiply, 
                            "Растяжение", duration));
                    }
                    break;

                case InjuryType.Fracture:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.Health, -reductionAmount * 30f, ModifierOperation.Add, 
                        "Перелом", duration));
                    if (skillSystem != null)
                    {
                        skillSystem.AddSkillModifier(new SkillModifier(
                            SkillType.Physical_Fitness, -reductionAmount * 1.5f, ModifierOperation.Multiply, 
                            "Перелом", duration));
                    }
                    break;

                case InjuryType.Concussion:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.Mood, -reductionAmount * 25f, ModifierOperation.Add, 
                        "Сотрясение", duration));
                    if (skillSystem != null)
                    {
                        skillSystem.AddSkillModifier(new SkillModifier(
                            SkillType.Intelligence, -reductionAmount, ModifierOperation.Multiply, 
                            "Сотрясение", duration));
                    }
                    break;
            }
        }

        #endregion

        #region Reputation System

        /// <summary>
        /// Обновить репутацию
        /// </summary>
        private void UpdateReputation(CombatData combatData, bool playerWon)
        {
            float reputationChange = 0f;

            if (playerWon)
            {
                reputationChange = reputationGainPerWin;
                
                // Бонус за сложных врагов
                foreach (var enemy in combatData.enemies)
                {
                    reputationChange += enemy.difficulty * 2f;
                }
            }
            else
            {
                reputationChange = -reputationLossPerLoss;
            }

            // Применить изменение репутации
            ApplyConsequence(CombatConsequence.ReputationGain, reputationChange);
        }

        /// <summary>
        /// Получить текущую репутацию
        /// </summary>
        private float GetCurrentReputation()
        {
            // TODO: Интеграция с системой репутации или сохранением
            float reputation = 0f;
            
            foreach (var consequence in activeConsequences)
            {
                if (consequence.consequenceType == CombatConsequence.ReputationGain)
                {
                    reputation += (float)consequence.data;
                }
                else if (consequence.consequenceType == CombatConsequence.ReputationLoss)
                {
                    reputation -= (float)consequence.data;
                }
            }

            return Mathf.Clamp(reputation, -maxReputationEffect, maxReputationEffect);
        }

        #endregion

        #region Enemy Relationships

        /// <summary>
        /// Обновить отношения с врагами
        /// </summary>
        private void UpdateEnemyRelationships(CombatData combatData, bool playerWon)
        {
            foreach (var enemy in combatData.enemies)
            {
                if (playerWon)
                {
                    enemyVictoryCount[enemy.enemyType]++;
                    
                    // Враги начинают избегать игрока после многих поражений
                    if (enemyVictoryCount[enemy.enemyType] >= 3)
                    {
                        enemyRelationship[enemy.enemyType] = Mathf.Max(-1f, 
                            enemyRelationship[enemy.enemyType] - 0.2f);
                        
                        OnEnemyRelationshipChanged?.Invoke(enemy.enemyType, false);
                    }
                }
                else
                {
                    // Враги становятся агрессивнее после побед
                    enemyRelationship[enemy.enemyType] = Mathf.Min(1f, 
                        enemyRelationship[enemy.enemyType] + 0.1f);
                    
                    OnEnemyRelationshipChanged?.Invoke(enemy.enemyType, true);
                }
            }
        }

        #endregion

        #region Special Events

        /// <summary>
        /// Проверить свидетелей
        /// </summary>
        private void CheckWitnessReports(CombatData combatData)
        {
            if (policeSystem == null)
                return;

            Location location = locationManager?.CurrentLocation ?? Location.Street;
            float reportChance = witnessReportChance * GetLocationWitnessMultiplier(location);

            if (UnityEngine.Random.value < reportChance)
            {
                policeSystem.ReportFighting();
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Свидетели", 
                        "Кто-то сообщил о драке в полицию", NotificationType.Warning);
                }

                ApplyConsequence(CombatConsequence.WitnessReport, location);

                if (showDebugInfo)
                {
                    Debug.Log($"Свидетели сообщили о драке в {location}");
                }
            }
        }

        /// <summary>
        /// Проверить специальные события боя
        /// </summary>
        private void CheckSpecialCombatEvents(CombatData combatData, CombatState endState)
        {
            // Случайные помощники
            if (endState == CombatState.PlayerLost && UnityEngine.Random.value < 0.1f)
            {
                TriggerRandomHelper();
            }

            // Месть врагов
            if (allowRetaliation && endState == CombatState.PlayerWon && UnityEngine.Random.value < 0.15f)
            {
                ScheduleRetaliation(combatData);
            }

            // Медицинская помощь
            if (combatData.totalDamageReceived > 50f && UnityEngine.Random.value < 0.2f)
            {
                TriggerMedicalAssistance();
            }
        }

        /// <summary>
        /// Периодические события
        /// </summary>
        private void CheckPeriodicEvents()
        {
            if (Time.time - lastEventCheck < eventCheckInterval)
                return;

            lastEventCheck = Time.time;

            // Проверить случайные события
            if (UnityEngine.Random.value < baseEventChance)
            {
                TriggerRandomCombatEvent();
            }
        }

        #endregion

        #region Event Triggers

        /// <summary>
        /// Случайный помощник
        /// </summary>
        private void TriggerRandomHelper()
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Помощь!", 
                    "Прохожий помог вам и вызвал скорую", NotificationType.Info);
            }

            // Восстановить немного здоровья
            if (playerStats != null)
            {
                playerStats.ChangeHealth(20f);
            }

            ApplyConsequence(CombatConsequence.RandomHelp, "прохожий");

            if (showDebugInfo)
            {
                Debug.Log("Получена помощь от прохожего");
            }
        }

        /// <summary>
        /// Запланировать месть
        /// </summary>
        private void ScheduleRetaliation(CombatData combatData)
        {
            float retaliationTime = UnityEngine.Random.Range(300f, 1200f); // 5-20 минут
            
            var retaliationData = new CombatConsequenceData
            {
                consequenceType = CombatConsequence.Retaliation,
                startTime = Time.time,
                duration = retaliationTime,
                data = combatData.enemies[0].enemyType // Тип врага для мести
            };

            activeConsequences.Add(retaliationData);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Угроза", 
                    "Враги могут захотеть отомстить...", NotificationType.Warning);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Запланирована месть через {retaliationTime:F0} секунд");
            }
        }

        /// <summary>
        /// Медицинская помощь
        /// </summary>
        private void TriggerMedicalAssistance()
        {
            if (moneySystem != null && moneySystem.CanAfford(hospitalCost * 0.5))
            {
                moneySystem.SpendMoney(hospitalCost * 0.5, "Медицинская помощь");
                
                if (playerStats != null)
                {
                    playerStats.ChangeHealth(30f);
                }

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Медпомощь", 
                        "Вам оказали медицинскую помощь", NotificationType.Info);
                }

                ApplyConsequence(CombatConsequence.MedicalAssistance, hospitalCost * 0.5);
            }
        }

        /// <summary>
        /// Случайное боевое событие
        /// </summary>
        private void TriggerRandomCombatEvent()
        {
            float roll = UnityEngine.Random.value;

            if (roll < 0.3f)
            {
                // Улучшение репутации
                ApplyConsequence(CombatConsequence.ReputationGain, 2f);
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Репутация", 
                        "Ваша боевая репутация растет", NotificationType.Info);
                }
            }
            else if (roll < 0.6f)
            {
                // Находка полезного предмета
                ApplyConsequence(CombatConsequence.ItemFound, "аптечка");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Находка", 
                        "Вы нашли полезный предмет", NotificationType.Info);
                }
            }
            else if (roll < 0.9f)
            {
                // Предложение о работе
                ApplyConsequence(CombatConsequence.JobOffer, "охранник");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Предложение", 
                        "Вам предложили работу охранника", NotificationType.Info);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log("Сработало случайное боевое событие");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Применить последствие
        /// </summary>
        private void ApplyConsequence(CombatConsequence consequenceType, object data)
        {
            var consequenceData = new CombatConsequenceData
            {
                consequenceType = consequenceType,
                startTime = Time.time,
                data = data
            };

            activeConsequences.Add(consequenceData);
            OnCombatConsequence?.Invoke(consequenceType, data);

            if (showDebugInfo)
            {
                Debug.Log($"Применено последствие: {consequenceType} = {data}");
            }
        }

        /// <summary>
        /// Обновить активные последствия
        /// </summary>
        private void UpdateActiveConsequences()
        {
            for (int i = activeConsequences.Count - 1; i >= 0; i--)
            {
                var consequence = activeConsequences[i];
                
                if (consequence.duration > 0 && 
                    Time.time - consequence.startTime >= consequence.duration)
                {
                    // Последствие истекло
                    ProcessExpiredConsequence(consequence);
                    activeConsequences.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Обработать истекшее последствие
        /// </summary>
        private void ProcessExpiredConsequence(CombatConsequenceData consequence)
        {
            switch (consequence.consequenceType)
            {
                case CombatConsequence.Injury:
                    var injuryType = (InjuryType)consequence.data;
                    OnPlayerHealed?.Invoke(injuryType);
                    
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Выздоровление", 
                            $"Травма ({GetInjuryName(injuryType)}) прошла", NotificationType.Info);
                    }
                    break;

                case CombatConsequence.Retaliation:
                    // Запустить месть
                    if (combatSystem != null && combatSystem.CanStartCombat())
                    {
                        TriggerRetaliationEvent((EnemyType)consequence.data);
                    }
                    break;

                case CombatConsequence.Pursuit:
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Безопасность", 
                            "Преследование прекратилось", NotificationType.Info);
                    }
                    break;
            }

            if (showDebugInfo)
            {
                Debug.Log($"Истекшее последствие: {consequence.consequenceType}");
            }
        }

        /// <summary>
        /// Запустить событие мести
        /// </summary>
        private void TriggerRetaliationEvent(EnemyType enemyType)
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Месть!", 
                    "Враги пришли за местью!", NotificationType.Warning);
            }

            // TODO: Создать врагов для мести через EnemySystem
            if (showDebugInfo)
            {
                Debug.Log($"Месть от {enemyType}");
            }
        }
         #endregion
        #region Event Handlers

        /// <summary>
        /// Обработать критический удар
        /// </summary>
        private void HandleCriticalHit(CombatAction action)
        {
            if (skillSystem != null)
            {
                skillSystem.AddSkillExperience(SkillType.Physical_Fitness, 2f, "Критический удар");
            }
        }

        /// <summary>
        /// Обработать эффект оглушения
        /// </summary>
        private void HandleStunEffect()
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Оглушение", 
                    "Противник оглушен!", NotificationType.Info);
            }
        }

        /// <summary>
        /// Обработать попытку побега
        /// </summary>
        private void HandleFleeAttempt(CombatAction action)
        {
            if (skillSystem != null)
            {
                skillSystem.AddSkillExperience(SkillType.Physical_Fitness, 1f, "Попытка побега");
            }
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Рассчитать опыт от боя
        /// </summary>
        private float CalculateCombatExperience(CombatData combatData)
        {
            float experience = 5f; // Базовый опыт
            
            experience += combatData.totalDamageDealt * 0.1f;
            experience += combatData.playerCriticals * 2f;
            experience += combatData.duration * 0.1f;

            foreach (var enemy in combatData.enemies)
            {
                if (enemy.currentHealth <= 0) // Побежденный враг
                {
                    experience += enemy.difficulty * 5f;
                }
            }

            return experience;
        }

        /// <summary>
        /// Получить множитель свидетелей для локации
        /// </summary>
        private float GetLocationWitnessMultiplier(Location location)
        {
            switch (location)
            {
                case Location.BusinessDistrict: return 2.0f;
                case Location.Shop: return 1.5f;
                case Location.University: return 1.3f;
                case Location.Hospital: return 1.2f;
                case Location.Park: return 0.8f;
                case Location.Street: return 1.0f;
                case Location.Industrial: return 0.6f;
                case Location.Abandoned: return 0.2f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Получить сообщение о травме
        /// </summary>
        private string GetInjuryMessage(InjuryType injuryType, float severity)
        {
            string severityText = severity > 0.8f ? "серьезные" : severity > 0.5f ? "умеренные" : "легкие";
            string injuryName = GetInjuryName(injuryType);
            
            return $"Получены {severityText} {injuryName}";
        }

        /// <summary>
        /// Получить название травмы
        /// </summary>
        private string GetInjuryName(InjuryType injuryType)
        {
            switch (injuryType)
            {
                case InjuryType.Bruises: return "синяки";
                case InjuryType.Cut: return "порезы";
                case InjuryType.Sprain: return "растяжения";
                case InjuryType.Fracture: return "переломы";
                case InjuryType.Concussion: return "сотрясение";
                default: return "травмы";
            }
        }

        /// <summary>
        /// Проверить достижения за победу
        /// </summary>
        private void CheckVictoryAchievements(CombatData combatData)
        {
            if (achievementSystem == null)
                return;

            // TODO: Интеграция с системой достижений
            // achievementSystem.CheckCombatAchievements(combatData);
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public CombatEventsSaveData GetSaveData()
        {
            return new CombatEventsSaveData
            {
                lastEventCheck = lastEventCheck,
                activeConsequences = new List<CombatConsequenceData>(activeConsequences),
                enemyVictoryCount = new Dictionary<EnemyType, int>(enemyVictoryCount),
                enemyRelationship = new Dictionary<EnemyType, float>(enemyRelationship)
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(CombatEventsSaveData data)
        {
            lastEventCheck = data.lastEventCheck;
            activeConsequences = data.activeConsequences ?? new List<CombatConsequenceData>();
            enemyVictoryCount = data.enemyVictoryCount ?? new Dictionary<EnemyType, int>();
            enemyRelationship = data.enemyRelationship ?? new Dictionary<EnemyType, float>();

            // Инициализировать недостающие отношения
            InitializeEnemyRelationships();

            if (showDebugInfo)
            {
                Debug.Log($"CombatEvents загружен. Последствий: {activeConsequences.Count}, " +
                         $"Отношений с врагами: {enemyRelationship.Count}");
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Force Injury")]
        private void DebugForceInjury()
        {
            ApplyInjury(InjuryType.Bruises, 0.5f);
        }

        [ContextMenu("Heal All Injuries")]
        private void DebugHealAllInjuries()
        {
            var injuries = activeConsequences.FindAll(c => c.consequenceType == CombatConsequence.Injury);
            foreach (var injury in injuries)
            {
                ProcessExpiredConsequence(injury);
                activeConsequences.Remove(injury);
            }
        }

        [ContextMenu("Show Active Consequences")]
        private void DebugShowActiveConsequences()
        {
            Debug.Log($"Активных последствий: {activeConsequences.Count}");
            foreach (var consequence in activeConsequences)
            {
                float remaining = consequence.duration > 0 ? 
                    consequence.duration - (Time.time - consequence.startTime) : 0f;
                Debug.Log($"- {consequence.consequenceType}: {consequence.data} " +
                         $"(осталось: {remaining:F1}с)");
            }
        }

        [ContextMenu("Trigger Random Event")]
        private void DebugTriggerRandomEvent()
        {
            TriggerRandomCombatEvent();
        }

        #endregion
    }

    /// <summary>
    /// Типы травм
    /// </summary>
    public enum InjuryType
    {
        Bruises,        // Синяки
        Cut,            // Порезы
        Sprain,         // Растяжение
        Fracture,       // Перелом
        Concussion      // Сотрясение
    }

    /// <summary>
    /// Данные последствия боя
    /// </summary>
    [System.Serializable]
    public class CombatConsequenceData
    {
        public CombatConsequence consequenceType;   // Тип последствия
        public float startTime;                     // Время начала
        public float duration;                      // Длительность (0 = мгновенное)
        public float severity;                      // Серьезность (0-1)
        public object data;                         // Дополнительные данные
    }

    /// <summary>
    /// Данные CombatEvents для сохранения
    /// </summary>
    [System.Serializable]
    public class CombatEventsSaveData
    {
        public float lastEventCheck;
        public List<CombatConsequenceData> activeConsequences;
        public Dictionary<EnemyType, int> enemyVictoryCount;
        public Dictionary<EnemyType, float> enemyRelationship;
    }
}
