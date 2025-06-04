using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Основная система боя
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        [Header("Настройки боевой системы")]
        [SerializeField] private bool enableCombatSystem = true;
        [SerializeField] private float combatAnimationSpeed = 1f;
        [SerializeField] private bool allowFleeing = true;
        [SerializeField] private bool allowNegotiation = true;
        [SerializeField] private bool enableCriticalHits = true;

        [Header("Базовые параметры боя")]
        [SerializeField] private float baseCriticalChance = 5f;       // 5% базовый шанс крита
        [SerializeField] private float baseDodgeChance = 10f;         // 10% базовый шанс уклонения
        [SerializeField] private float baseBlockChance = 15f;         // 15% базовый шанс блока
        [SerializeField] private float fleeSuccessChance = 60f;       // 60% шанс успешного побега
        [SerializeField] private float negotiationBaseChance = 30f;   // 30% базовый шанс переговоров

        [Header("Модификаторы характеристик")]
        [SerializeField] private float strengthDamageMultiplier = 0.1f;  // +10% урона за единицу силы
        [SerializeField] private float fitnessHealthMultiplier = 5f;     // +5 HP за единицу физформы
        [SerializeField] private float luckCriticalBonus = 1f;           // +1% крита за единицу удачи
        [SerializeField] private float charismaNegotiationBonus = 5f;    // +5% переговоров за единицу харизмы

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceCombatResult = false;
        [SerializeField] private CombatResult debugCombatResult = CombatResult.Hit;

        // Текущее состояние боя
        private CombatState currentState = CombatState.NotInCombat;
        private CombatData activeCombat;
        private List<Enemy> activeEnemies = new List<Enemy>();
        private int currentEnemyIndex = 0;
        private float combatTimer = 0f;

        // Компоненты системы
        private PlayerStats playerStats;
        private WeaponSystem weaponSystem;
        private PoliceSystem policeSystem;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;

        // События боя
        public event Action<CombatState> OnCombatStateChanged;
        public event Action<CombatData> OnCombatStarted;
        public event Action<CombatData, CombatState> OnCombatEnded;
        public event Action<CombatAction, CombatResult> OnCombatAction;
        public event Action<float> OnPlayerHealthChanged;
        public event Action<int, float> OnEnemyHealthChanged;

        // Свойства
        public CombatState CurrentState => currentState;
        public bool IsInCombat => currentState == CombatState.InCombat || 
                                  currentState == CombatState.PlayerTurn || 
                                  currentState == CombatState.EnemyTurn;
        public CombatData ActiveCombat => activeCombat;
        public Enemy CurrentEnemy => currentEnemyIndex < activeEnemies.Count ? activeEnemies[currentEnemyIndex] : null;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            weaponSystem = FindObjectOfType<WeaponSystem>();
            policeSystem = FindObjectOfType<PoliceSystem>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = FindObjectOfType<SkillSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
        }

        private void Start()
        {
            if (enableCombatSystem)
            {
                SubscribeToEvents();
            }

            Debug.Log("CombatSystem инициализирован");
        }

        private void Update()
        {
            if (IsInCombat)
            {
                UpdateCombat();
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
            // Подписка на события из других систем
            if (playerStats != null)
            {
                playerStats.OnHealthChanged += OnPlayerHealthChangedInternal;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= OnPlayerHealthChangedInternal;
            }
        }

        #endregion

        #region Combat Management

        /// <summary>
        /// Начать бой
        /// </summary>
        public bool StartCombat(List<Enemy> enemies, CombatEventType eventType = CombatEventType.RandomEncounter)
        {
            if (IsInCombat || enemies == null || enemies.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("Нельзя начать бой: уже в бою или нет врагов");
                }
                return false;
            }

            // Создать данные боя
            activeCombat = new CombatData
            {
                eventType = eventType,
                startTime = Time.time,
                playerStartHealth = playerStats?.CurrentHealth ?? 100f,
                enemies = new List<Enemy>(enemies),
                combatLog = new List<string>()
            };

            activeEnemies = new List<Enemy>(enemies);
            currentEnemyIndex = 0;

            // Инициализировать здоровье врагов
            foreach (var enemy in activeEnemies)
            {
                enemy.currentHealth = enemy.maxHealth;
            }

            // Изменить состояние
            ChangeState(CombatState.PreCombat);
            OnCombatStarted?.Invoke(activeCombat);

            // Начать первый ход
            StartCoroutine(StartCombatSequence());

            if (showDebugInfo)
            {
                Debug.Log($"Бой начат против {enemies.Count} врагов. Тип: {eventType}");
            }

            return true;
        }

        /// <summary>
        /// Последовательность начала боя
        /// </summary>
        private IEnumerator StartCombatSequence()
        {
            yield return new WaitForSeconds(0.5f);

            // Проверить внезапность
            if (CheckSurpriseAttack())
            {
                AddToCombatLog("Внезапная атака!");
                yield return new WaitForSeconds(1f);
            }

            ChangeState(CombatState.PlayerTurn);
        }

        /// <summary>
        /// Завершить бой
        /// </summary>
        public void EndCombat(CombatState endState)
        {
            if (!IsInCombat)
                return;

            activeCombat.endTime = Time.time;
            activeCombat.duration = activeCombat.endTime - activeCombat.startTime;
            
            // Применить результаты боя
            ApplyCombatResults(endState);

            // Изменить состояние
            ChangeState(endState);
            OnCombatEnded?.Invoke(activeCombat, endState);

            // Очистить данные боя
            activeEnemies.Clear();
            currentEnemyIndex = 0;
            combatTimer = 0f;

            if (showDebugInfo)
            {
                Debug.Log($"Бой завершен. Результат: {endState}. Длительность: {activeCombat.duration:F1}с");
            }

            // Вернуться к обычному состоянию
            StartCoroutine(ReturnToNormalState());
        }

        /// <summary>
        /// Вернуться к обычному состоянию
        /// </summary>
        private IEnumerator ReturnToNormalState()
        {
            yield return new WaitForSeconds(2f);
            ChangeState(CombatState.NotInCombat);
            activeCombat = null;
        }

        /// <summary>
        /// Изменить состояние боя
        /// </summary>
        private void ChangeState(CombatState newState)
        {
            CombatState oldState = currentState;
            currentState = newState;
            OnCombatStateChanged?.Invoke(newState);

            if (showDebugInfo && oldState != newState)
            {
                Debug.Log($"Состояние боя: {oldState} -> {newState}");
            }
        }

        /// <summary>
        /// Обновить бой
        /// </summary>
        private void UpdateCombat()
        {
            combatTimer += Time.deltaTime;

            // Проверить окончание боя
            if (CheckCombatEnd())
                return;

            // Обновление хода
            UpdateTurn();
        }

        /// <summary>
        /// Проверить окончание боя
        /// </summary>
        private bool CheckCombatEnd()
        {
            // Проверить смерть игрока
            if (playerStats != null && playerStats.CurrentHealth <= 0)
            {
                EndCombat(CombatState.PlayerLost);
                return true;
            }

            // Проверить смерть всех врагов
            bool allEnemiesDead = true;
            foreach (var enemy in activeEnemies)
            {
                if (enemy.currentHealth > 0)
                {
                    allEnemiesDead = false;
                    break;
                }
            }

            if (allEnemiesDead)
            {
                EndCombat(CombatState.PlayerWon);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Обновить ход
        /// </summary>
        private void UpdateTurn()
        {
            switch (currentState)
            {
                case CombatState.PlayerTurn:
                    // Ожидание действия игрока
                    break;

                case CombatState.EnemyTurn:
                    // Автоматический ход врага
                    if (combatTimer >= 1f) // Задержка для анимации
                    {
                        PerformEnemyAction();
                        combatTimer = 0f;
                    }
                    break;
            }
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Выполнить действие игрока
        /// </summary>
        public bool PerformPlayerAction(CombatAction action, int targetIndex = 0)
        {
            if (currentState != CombatState.PlayerTurn)
                return false;

            if (targetIndex >= activeEnemies.Count || activeEnemies[targetIndex].currentHealth <= 0)
                targetIndex = GetNextAliveEnemyIndex();

            if (targetIndex == -1)
                return false;

            Enemy target = activeEnemies[targetIndex];
            CombatResult result = CombatResult.Miss;

            switch (action)
            {
                case CombatAction.Attack:
                    result = PerformAttack(target);
                    break;

                case CombatAction.Defend:
                    result = PerformDefend();
                    break;

                case CombatAction.Dodge:
                    result = PerformDodge();
                    break;

                case CombatAction.UseItem:
                    result = PerformUseItem();
                    break;

                case CombatAction.Flee:
                    result = PerformFlee();
                    break;

                case CombatAction.Negotiate:
                    result = PerformNegotiate(target);
                    break;

                case CombatAction.Intimidate:
                    result = PerformIntimidate(target);
                    break;
            }

            OnCombatAction?.Invoke(action, result);

            // Обработать результат
            ProcessActionResult(action, result, target);

            // Следующий ход
            if (result != CombatResult.Fled && currentState == CombatState.PlayerTurn)
            {
                ChangeState(CombatState.EnemyTurn);
                combatTimer = 0f;
            }

            return true;
        }

        /// <summary>
        /// Выполнить атаку
        /// </summary>
        private CombatResult PerformAttack(Enemy target)
        {
            if (forceCombatResult)
                return debugCombatResult;

            // Расчет попадания
            float hitChance = CalculateHitChance(target);
            
            if (UnityEngine.Random.value > hitChance)
            {
                AddToCombatLog($"Игрок промахнулся по {target.GetDisplayName()}");
                return CombatResult.Miss;
            }

            // Проверка блока врага
            if (CheckEnemyBlock(target))
            {
                AddToCombatLog($"{target.GetDisplayName()} заблокировал атаку");
                return CombatResult.Blocked;
            }

            // Проверка уклонения врага
            if (CheckEnemyDodge(target))
            {
                AddToCombatLog($"{target.GetDisplayName()} уклонился от атаки");
                return CombatResult.Dodged;
            }

            // Расчет урона
            float damage = CalculatePlayerDamage();
            bool isCritical = CheckCriticalHit();

            if (isCritical)
            {
                damage *= 2f;
                AddToCombatLog($"Критический удар! Урон: {damage:F0}");
            }
            else
            {
                AddToCombatLog($"Игрок атакует. Урон: {damage:F0}");
            }

            // Применить урон
            target.currentHealth = Mathf.Max(0, target.currentHealth - damage);
            OnEnemyHealthChanged?.Invoke(activeEnemies.IndexOf(target), target.currentHealth);

            // Проверить смерть врага
            if (target.currentHealth <= 0)
            {
                AddToCombatLog($"{target.GetDisplayName()} повержен!");
            }

            return isCritical ? CombatResult.Critical : CombatResult.Hit;
        }

        /// <summary>
        /// Выполнить защиту
        /// </summary>
        private CombatResult PerformDefend()
        {
            activeCombat.playerDefending = true;
            AddToCombatLog("Игрок принимает оборонительную позицию");
            return CombatResult.Blocked;
        }

        /// <summary>
        /// Выполнить уклонение
        /// </summary>
        private CombatResult PerformDodge()
        {
            activeCombat.playerDodging = true;
            AddToCombatLog("Игрок готовится к уклонению");
            return CombatResult.Dodged;
        }

        /// <summary>
        /// Использовать предмет
        /// </summary>
        private CombatResult PerformUseItem()
        {
            // TODO: Интеграция с системой инвентаря
            AddToCombatLog("Игрок использует предмет");
            return CombatResult.Hit;
        }

        /// <summary>
        /// Попытка побега
        /// </summary>
        private CombatResult PerformFlee()
        {
            if (!allowFleeing)
            {
                AddToCombatLog("Побег невозможен!");
                return CombatResult.Miss;
            }

            float fleeChance = CalculateFleeChance();
            
            if (UnityEngine.Random.value < fleeChance)
            {
                AddToCombatLog("Игрок успешно сбежал!");
                EndCombat(CombatState.PlayerFled);
                return CombatResult.Fled;
            }
            else
            {
                AddToCombatLog("Побег не удался!");
                return CombatResult.Miss;
            }
        }

        /// <summary>
        /// Переговоры
        /// </summary>
        private CombatResult PerformNegotiate(Enemy target)
        {
            if (!allowNegotiation || target.enemyType == EnemyType.Police)
            {
                AddToCombatLog("Переговоры невозможны!");
                return CombatResult.Miss;
            }

            float negotiationChance = CalculateNegotiationChance(target);
            
            if (UnityEngine.Random.value < negotiationChance)
            {
                AddToCombatLog("Переговоры увенчались успехом!");
                // Возможная взятка
                double bribeAmount = target.difficulty * 10;
                if (moneySystem != null && moneySystem.CanAfford(bribeAmount))
                {
                    moneySystem.SpendMoney(bribeAmount, "Взятка");
                    AddToCombatLog($"Заплачено ${bribeAmount} за мирное решение");
                }
                EndCombat(CombatState.PlayerWon);
                return CombatResult.Hit;
            }
            else
            {
                AddToCombatLog("Переговоры провалились!");
                return CombatResult.Miss;
            }
        }

        /// <summary>
        /// Запугивание
        /// </summary>
        private CombatResult PerformIntimidate(Enemy target)
        {
            float intimidateChance = CalculateIntimidationChance(target);
            
            if (UnityEngine.Random.value < intimidateChance)
            {
                AddToCombatLog($"{target.GetDisplayName()} испуган!");
                target.currentHealth *= 0.8f; // Снижение эффективности
                return CombatResult.Stunned;
            }
            else
            {
                AddToCombatLog("Запугивание не сработало!");
                return CombatResult.Miss;
            }
        }

        #endregion

        #region Enemy Actions

        /// <summary>
        /// Выполнить действие врага
        /// </summary>
        private void PerformEnemyAction()
        {
            Enemy currentEnemy = GetCurrentAliveEnemy();
            if (currentEnemy == null)
            {
                ChangeState(CombatState.PlayerTurn);
                return;
            }

            CombatAction action = DetermineEnemyAction(currentEnemy);
            CombatResult result = ExecuteEnemyAction(currentEnemy, action);

            OnCombatAction?.Invoke(action, result);

            // Перейти к следующему врагу или к ходу игрока
            if (GetNextAliveEnemyIndex() != -1)
            {
                currentEnemyIndex = GetNextAliveEnemyIndex();
            }
            else
            {
                ChangeState(CombatState.PlayerTurn);
                activeCombat.playerDefending = false;
                activeCombat.playerDodging = false;
            }
        }

        /// <summary>
        /// Определить действие врага на основе AI
        /// </summary>
        private CombatAction DetermineEnemyAction(Enemy enemy)
        {
            switch (enemy.aiType)
            {
                case CombatAI.Aggressive:
                    return CombatAction.Attack;

                case CombatAI.Defensive:
                    return UnityEngine.Random.value < 0.3f ? CombatAction.Attack : CombatAction.Defend;

                case CombatAI.Balanced:
                    float chance = UnityEngine.Random.value;
                    if (chance < 0.6f) return CombatAction.Attack;
                    if (chance < 0.8f) return CombatAction.Defend;
                    return CombatAction.Dodge;

                case CombatAI.Cowardly:
                    if (enemy.currentHealth < enemy.maxHealth * 0.3f)
                        return CombatAction.Flee;
                    return UnityEngine.Random.value < 0.4f ? CombatAction.Attack : CombatAction.Defend;

                case CombatAI.Tactical:
                    if (UnityEngine.Random.value < 0.2f) return CombatAction.UseItem;
                    return CombatAction.Attack;

                case CombatAI.Berserker:
                    return CombatAction.Attack; // Всегда атакует

                case CombatAI.Police:
                    if (UnityEngine.Random.value < 0.1f) return CombatAction.Negotiate;
                    return CombatAction.Attack;

                default:
                    return CombatAction.Attack;
            }
        }

        /// <summary>
        /// Выполнить действие врага
        /// </summary>
        private CombatResult ExecuteEnemyAction(Enemy enemy, CombatAction action)
        {
            switch (action)
            {
                case CombatAction.Attack:
                    return PerformEnemyAttack(enemy);

                case CombatAction.Defend:
                    AddToCombatLog($"{enemy.GetDisplayName()} защищается");
                    return CombatResult.Blocked;

                case CombatAction.Dodge:
                    AddToCombatLog($"{enemy.GetDisplayName()} готовится к уклонению");
                    return CombatResult.Dodged;

                case CombatAction.Flee:
                    return PerformEnemyFlee(enemy);

                case CombatAction.UseItem:
                    AddToCombatLog($"{enemy.GetDisplayName()} использует предмет");
                    return CombatResult.Hit;

                case CombatAction.Negotiate:
                    AddToCombatLog($"{enemy.GetDisplayName()} пытается договориться");
                    return CombatResult.Miss;

                default:
                    return PerformEnemyAttack(enemy);
            }
        }

        /// <summary>
        /// Атака врага
        /// </summary>
        private CombatResult PerformEnemyAttack(Enemy enemy)
        {
            // Проверка защиты/уклонения игрока
            if (activeCombat.playerDefending && UnityEngine.Random.value < 0.5f)
            {
                AddToCombatLog($"Игрок заблокировал атаку {enemy.GetDisplayName()}");
                return CombatResult.Blocked;
            }

            if (activeCombat.playerDodging && UnityEngine.Random.value < 0.3f)
            {
                AddToCombatLog($"Игрок уклонился от атаки {enemy.GetDisplayName()}");
                return CombatResult.Dodged;
            }

            // Расчет попадания
            float hitChance = enemy.accuracy;
            if (UnityEngine.Random.value > hitChance)
            {
                AddToCombatLog($"{enemy.GetDisplayName()} промахнулся");
                return CombatResult.Miss;
            }

            // Расчет урона
            float damage = UnityEngine.Random.Range(enemy.minDamage, enemy.maxDamage);
            
            // Критический удар врага
            bool isCritical = UnityEngine.Random.value < enemy.criticalChance;
            if (isCritical)
            {
                damage *= 1.5f;
                AddToCombatLog($"{enemy.GetDisplayName()} наносит критический удар! Урон: {damage:F0}");
            }
            else
            {
                AddToCombatLog($"{enemy.GetDisplayName()} атакует. Урон: {damage:F0}");
            }

            // Применить урон к игроку
            if (playerStats != null)
            {
                playerStats.ChangeHealth(-damage);
            }

            return isCritical ? CombatResult.Critical : CombatResult.Hit;
        }

        /// <summary>
        /// Побег врага
        /// </summary>
        private CombatResult PerformEnemyFlee(Enemy enemy)
        {
            if (UnityEngine.Random.value < 0.7f) // 70% шанс успешного побега врага
            {
                AddToCombatLog($"{enemy.GetDisplayName()} сбежал!");
                enemy.currentHealth = 0; // Удалить из боя
                return CombatResult.Fled;
            }
            else
            {
                AddToCombatLog($"{enemy.GetDisplayName()} не смог сбежать!");
                return CombatResult.Miss;
            }
        }

        #endregion

        #region Combat Calculations

        /// <summary>
        /// Рассчитать шанс попадания игрока
        /// </summary>
        private float CalculateHitChance(Enemy target)
        {
            float baseChance = 0.8f;
            
            // Бонус от физической формы
            if (skillSystem != null)
            {
                float fitness = skillSystem.GetSkillLevel(SkillType.Physical_Fitness);
                baseChance += fitness * 0.02f; // +2% за уровень
            }

            // Бонус от оружия
            if (weaponSystem != null)
            {
                Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    baseChance += currentWeapon.accuracy;
                }
            }

            // Штраф от сложности врага
            baseChance -= target.difficulty * 0.1f;

            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Рассчитать урон игрока
        /// </summary>
        private float CalculatePlayerDamage()
        {
            float baseDamage = 10f; // Базовый урон кулаков

            // Бонус от физической формы
            if (skillSystem != null)
            {
                float fitness = skillSystem.GetSkillLevel(SkillType.Physical_Fitness);
                baseDamage += fitness * strengthDamageMultiplier;
            }

            // Бонус от оружия
            if (weaponSystem != null)
            {
                Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    baseDamage += UnityEngine.Random.Range(currentWeapon.minDamage, currentWeapon.maxDamage);
                }
            }

            // Случайность ±20%
            float variation = UnityEngine.Random.Range(0.8f, 1.2f);
            return baseDamage * variation;
        }

        /// <summary>
        /// Проверить критический удар
        /// </summary>
        private bool CheckCriticalHit()
        {
            if (!enableCriticalHits)
                return false;

            float critChance = baseCriticalChance;

            // Бонус от удачи
            if (skillSystem != null)
            {
                float luck = skillSystem.GetSkillLevel(SkillType.Luck);
                critChance += luck * luckCriticalBonus;
            }

            // Бонус от оружия
            if (weaponSystem != null)
            {
                Weapon currentWeapon = weaponSystem.GetCurrentWeapon();
                if (currentWeapon != null)
                {
                    critChance += currentWeapon.criticalChance;
                }
            }

            return UnityEngine.Random.value < (critChance / 100f);
        }

        /// <summary>
        /// Проверить блок врага
        /// </summary>
        private bool CheckEnemyBlock(Enemy enemy)
        {
            return UnityEngine.Random.value < enemy.blockChance;
        }

        /// <summary>
        /// Проверить уклонение врага
        /// </summary>
        private bool CheckEnemyDodge(Enemy enemy)
        {
            return UnityEngine.Random.value < enemy.dodgeChance;
        }

        /// <summary>
        /// Рассчитать шанс побега
        /// </summary>
        private float CalculateFleeChance()
        {
            float chance = fleeSuccessChance;

            // Бонус от физической формы
            if (skillSystem != null)
            {
                float fitness = skillSystem.GetSkillLevel(SkillType.Physical_Fitness);
                chance += fitness * 2f; // +2% за уровень
            }

            // Штраф от количества врагов
            chance -= (activeEnemies.Count - 1) * 10f;

            return Mathf.Clamp01(chance / 100f);
        }

        /// <summary>
        /// Рассчитать шанс переговоров
        /// </summary>
        private float CalculateNegotiationChance(Enemy enemy)
        {
            float chance = negotiationBaseChance;

            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                chance += charisma * charismaNegotiationBonus;
            }

            // Штраф от агрессивности врага
            if (enemy.aiType == CombatAI.Aggressive || enemy.aiType == CombatAI.Berserker)
                chance *= 0.3f;

            // Бонус для трусливых врагов
            if (enemy.aiType == CombatAI.Cowardly)
                chance *= 1.5f;

            return Mathf.Clamp01(chance / 100f);
        }

        /// <summary>
        /// Рассчитать шанс запугивания
        /// </summary>
        private float CalculateIntimidationChance(Enemy enemy)
        {
            float chance = 0.2f;

            // Бонус от физической формы и харизмы
            if (skillSystem != null)
            {
                float fitness = skillSystem.GetSkillLevel(SkillType.Physical_Fitness);
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                chance += (fitness + charisma) * 0.02f;
            }

            // Штраф от сложности врага
            chance -= enemy.difficulty * 0.05f;

            return Mathf.Clamp01(chance);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Проверить внезапность атаки
        /// </summary>
        private bool CheckSurpriseAttack()
        {
            if (activeCombat.eventType == CombatEventType.Ambush)
                return true;

            // Ночное время увеличивает шанс засады
            if (timeOfDayManager != null)
            {
                var period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                {
                    return UnityEngine.Random.value < 0.3f;
                }
            }

            return UnityEngine.Random.value < 0.1f;
        }

        /// <summary>
        /// Получить следующего живого врага
        /// </summary>
        private int GetNextAliveEnemyIndex()
        {
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                if (activeEnemies[i].currentHealth > 0)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Получить текущего живого врага
        /// </summary>
        private Enemy GetCurrentAliveEnemy()
        {
            int index = GetNextAliveEnemyIndex();
            return index >= 0 ? activeEnemies[index] : null;
        }

        /// <summary>
        /// Добавить запись в лог боя
        /// </summary>
        private void AddToCombatLog(string message)
        {
            if (activeCombat != null)
            {
                activeCombat.combatLog.Add($"[{Time.time:F1}] {message}");
                
                if (showDebugInfo)
                {
                    Debug.Log($"Combat: {message}");
                }
            }
        }

        /// <summary>
        /// Обработать результат действия
        /// </summary>
        private void ProcessActionResult(CombatAction action, CombatResult result, Enemy target)
        {
            // Начислить опыт за различные действия
            if (skillSystem != null)
            {
                float experience = 1f;
                
                switch (result)
                {
                    case CombatResult.Critical:
                        experience = 3f;
                        break;
                    case CombatResult.Hit:
                        experience = 2f;
                        break;
                    case CombatResult.Blocked:
                    case CombatResult.Dodged:
                        experience = 1f;
                        break;
                }

                skillSystem.AddSkillExperience(SkillType.Physical_Fitness, experience, "Бой");
            }
        }

        /// <summary>
        /// Применить результаты боя
        /// </summary>
        private void ApplyCombatResults(CombatState endState)
        {
            switch (endState)
            {
                case CombatState.PlayerWon:
                    ApplyVictoryRewards();
                    break;

                case CombatState.PlayerLost:
                    ApplyDefeatPenalties();
                    break;

                case CombatState.PlayerFled:
                    ApplyFleePenalties();
                    break;
            }
        }

        /// <summary>
        /// Применить награды за победу
        /// </summary>
        private void ApplyVictoryRewards()
        {
            double totalMoney = 0;
            float totalExperience = 0;

            foreach (var enemy in activeEnemies)
            {
                if (enemy.currentHealth <= 0) // Побежденные враги
                {
                    totalMoney += enemy.moneyReward;
                    totalExperience += enemy.experienceReward;
                }
            }

            if (moneySystem != null && totalMoney > 0)
            {
                moneySystem.AddMoney(totalMoney, "Победа в бою");
            }

            if (skillSystem != null && totalExperience > 0)
            {
                skillSystem.AddSkillExperience(SkillType.Physical_Fitness, totalExperience, "Победа в бою");
            }

            AddToCombatLog($"Получено: ${totalMoney}, опыт: {totalExperience}");
        }

        /// <summary>
        /// Применить штрафы за поражение
        /// </summary>
        private void ApplyDefeatPenalties()
        {
            // Потеря денег при поражении
            if (moneySystem != null)
            {
                double currentMoney = moneySystem.GetMoneyDouble();
                double lossAmount = currentMoney * 0.2; // Потеря 20% денег
                
                if (lossAmount > 0)
                {
                    moneySystem.SpendMoney(lossAmount, "Поражение в бою");
                    AddToCombatLog($"Потеряно ${lossAmount:F0}");
                }
            }

            // Травмы - снижение максимального здоровья на время
            if (playerStats != null)
            {
                playerStats.AddStatModifier(new StatModifier(
                    StatType.Health, -10f, ModifierOperation.Add, "Травма от боя", 300f)); // 5 минут
            }
        }

        /// <summary>
        /// Применить штрафы за побег
        /// </summary>
        private void ApplyFleePenalties()
        {
            // Небольшая потеря настроения за трусость
            if (playerStats != null)
            {
                playerStats.ChangeMood(-5f);
            }

            AddToCombatLog("Репутация немного пострадала от побега");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик изменения здоровья игрока
        /// </summary>
        private void OnPlayerHealthChangedInternal(float newHealth)
        {
            OnPlayerHealthChanged?.Invoke(newHealth);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Можно ли начать бой
        /// </summary>
        public bool CanStartCombat()
        {
            return enableCombatSystem && !IsInCombat && playerStats != null && playerStats.CurrentHealth > 0;
        }

        /// <summary>
        /// Получить доступные действия для текущего состояния
        /// </summary>
        public List<CombatAction> GetAvailableActions()
        {
            var actions = new List<CombatAction>();

            if (currentState != CombatState.PlayerTurn)
                return actions;

            // Базовые действия всегда доступны
            actions.Add(CombatAction.Attack);
            actions.Add(CombatAction.Defend);
            actions.Add(CombatAction.Dodge);

            // Условные действия
            if (allowFleeing)
                actions.Add(CombatAction.Flee);

            if (allowNegotiation)
                actions.Add(CombatAction.Negotiate);

            actions.Add(CombatAction.Intimidate);

            // TODO: Проверить наличие предметов для UseItem

            return actions;
        }

        /// <summary>
        /// Получить информацию о текущем состоянии боя
        /// </summary>
        public string GetCombatStatus()
        {
            if (!IsInCombat)
                return "Не в бою";

            int aliveEnemies = 0;
            foreach (var enemy in activeEnemies)
            {
                if (enemy.currentHealth > 0)
                    aliveEnemies++;
            }

            return $"В бою. Врагов: {aliveEnemies}. Ход: {currentState}";
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Start Test Combat")]
        private void DebugStartTestCombat()
        {
            var enemies = new List<Enemy>
            {
                Enemy.CreateTestEnemy(EnemyType.Thug),
                Enemy.CreateTestEnemy(EnemyType.Robber)
            };
            StartCombat(enemies, CombatEventType.RandomEncounter);
        }

        [ContextMenu("Force Player Victory")]
        private void DebugForcePlayerVictory()
        {
            if (IsInCombat)
            {
                EndCombat(CombatState.PlayerWon);
            }
        }

        [ContextMenu("Force Player Defeat")]
        private void DebugForcePlayerDefeat()
        {
            if (IsInCombat)
            {
                EndCombat(CombatState.PlayerLost);
            }
        }

        [ContextMenu("Show Combat Info")]
        private void DebugShowCombatInfo()
        {
            Debug.Log($"Combat Status: {GetCombatStatus()}");
            if (IsInCombat && activeCombat != null)
            {
                Debug.Log($"Enemies: {activeEnemies.Count}");
                Debug.Log($"Event Type: {activeCombat.eventType}");
                Debug.Log($"Duration: {(Time.time - activeCombat.startTime):F1}s");
            }
        }

        #endregion
    }
}
