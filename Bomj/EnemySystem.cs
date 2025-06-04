using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система управления врагами и боевыми событиями
    /// </summary>
    public class EnemySystem : MonoBehaviour
    {
        [Header("Настройки системы врагов")]
        [SerializeField] private bool enableEnemySystem = true;
        [SerializeField] private float encounterCheckInterval = 30f;    // Интервал проверки встреч
        [SerializeField] private float baseEncounterChance = 0.05f;     // Базовый шанс встречи (5%)
        [SerializeField] private int maxEnemiesPerEncounter = 3;        // Максимум врагов за встречу

        [Header("Модификаторы вероятности")]
        [SerializeField] private float nightEncounterMultiplier = 2f;   // Множитель для ночного времени
        [SerializeField] private float wealthTargetMultiplier = 1.5f;   // Множитель при наличии денег
        [SerializeField] private float reputationProtection = 0.8f;     // Защита от репутации
        [SerializeField] private float levelScalingFactor = 0.1f;       // Масштабирование по уровню

        [Header("Настройки врагов")]
        [SerializeField] private bool allowAmbushes = true;             // Разрешить засады
        [SerializeField] private bool scaleEnemyLevel = true;           // Масштабировать уровень врагов
        [SerializeField] private bool allowEnemyGroups = true;          // Разрешить группы врагов
        [SerializeField] private float ambushChance = 0.2f;             // Шанс засады

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceEncounters = false;
        [SerializeField] private EnemyType debugEnemyType = EnemyType.Thug;

        // Состояние системы
        private float lastEncounterCheck = 0f;
        private List<EnemyEncounter> recentEncounters = new List<EnemyEncounter>();
        private bool isEncounterActive = false;

        // Компоненты
        private CombatSystem combatSystem;
        private PlayerStats playerStats;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;
        private MoneySystem moneySystem;
        private PoliceSystem policeSystem;
        private NotificationSystem notificationSystem;

        // События
        public event Action<EnemyEncounter> OnEnemyEncounterStarted;
        public event Action<EnemyEncounter, bool> OnEnemyEncounterEnded; // bool = victory
        public event Action<EnemyType, Location> OnEnemySpotted;
        public event Action<float> OnThreatLevelChanged;

        // Свойства
        public bool IsEncounterActive => isEncounterActive;
        public float CurrentThreatLevel => CalculateCurrentThreatLevel();
        public List<EnemyEncounter> RecentEncounters => new List<EnemyEncounter>(recentEncounters);

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            combatSystem = FindObjectOfType<CombatSystem>();
            playerStats = FindObjectOfType<PlayerStats>();
            skillSystem = FindObjectOfType<SkillSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            moneySystem = FindObjectOfType<MoneySystem>();
            policeSystem = FindObjectOfType<PoliceSystem>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
        }

        private void Start()
        {
            if (enableEnemySystem)
            {
                SubscribeToEvents();
                lastEncounterCheck = Time.time;
            }

            Debug.Log("EnemySystem инициализирован");
        }

        private void Update()
        {
            if (enableEnemySystem && !isEncounterActive)
            {
                CheckForEncounters();
                CleanupOldEncounters();
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
            }

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
            if (combatSystem != null)
            {
                combatSystem.OnCombatEnded -= OnCombatEnded;
            }

            if (locationManager != null)
            {
                locationManager.OnLocationChanged -= OnLocationChanged;
            }
        }

        #endregion

        #region Encounter Management

        /// <summary>
        /// Проверить возможность встречи с врагами
        /// </summary>
        private void CheckForEncounters()
        {
            if (Time.time - lastEncounterCheck < encounterCheckInterval)
                return;

            lastEncounterCheck = Time.time;

            // Рассчитать шанс встречи
            float encounterChance = CalculateEncounterChance();
            
            if (forceEncounters || UnityEngine.Random.value < encounterChance)
            {
                TriggerRandomEncounter();
            }
        }

        /// <summary>
        /// Запустить случайную встречу
        /// </summary>
        public void TriggerRandomEncounter()
        {
            if (isEncounterActive || combatSystem == null || combatSystem.IsInCombat)
                return;

            // Определить тип встречи
            CombatEventType eventType = DetermineEncounterType();
            
            // Создать врагов
            List<Enemy> enemies = GenerateEnemies();
            
            if (enemies.Count == 0)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("Не удалось создать врагов для встречи");
                }
                return;
            }

            // Создать данные встречи
            var encounter = new EnemyEncounter
            {
                eventType = eventType,
                enemies = enemies,
                location = locationManager?.CurrentLocation ?? Location.Street,
                startTime = Time.time,
                playerLevel = playerStats?.CurrentLevel ?? 1,
                threatLevel = CalculateEncounterThreat(enemies)
            };

            recentEncounters.Add(encounter);
            isEncounterActive = true;

            OnEnemyEncounterStarted?.Invoke(encounter);

            // Показать уведомление
            ShowEncounterNotification(encounter);

            // Дать игроку время на подготовку
            StartCoroutine(PrepareForEncounter(encounter));

            if (showDebugInfo)
            {
                Debug.Log($"Встреча с врагами: {eventType}, Врагов: {enemies.Count}, " +
                         $"Угроза: {encounter.threatLevel:F1}");
            }
        }

        /// <summary>
        /// Подготовка к встрече
        /// </summary>
        private IEnumerator PrepareForEncounter(EnemyEncounter encounter)
        {
            yield return new WaitForSeconds(2f);

            // Начать бой
            if (combatSystem != null && combatSystem.CanStartCombat())
            {
                combatSystem.StartCombat(encounter.enemies, encounter.eventType);
            }
            else
            {
                // Не удалось начать бой
                EndEncounter(encounter, false);
            }
        }

        /// <summary>
        /// Завершить встречу
        /// </summary>
        private void EndEncounter(EnemyEncounter encounter, bool playerVictory)
        {
            encounter.endTime = Time.time;
            encounter.duration = encounter.endTime - encounter.startTime;
            encounter.playerWon = playerVictory;

            isEncounterActive = false;
            OnEnemyEncounterEnded?.Invoke(encounter, playerVictory);

            if (showDebugInfo)
            {
                Debug.Log($"Встреча завершена. Победа игрока: {playerVictory}, " +
                         $"Длительность: {encounter.duration:F1}с");
            }
        }

        #endregion

        #region Enemy Generation

        /// <summary>
        /// Создать врагов для встречи
        /// </summary>
        private List<Enemy> GenerateEnemies()
        {
            var enemies = new List<Enemy>();
            
            Location currentLocation = locationManager?.CurrentLocation ?? Location.Street;
            int enemyCount = DetermineEnemyCount();
            
            for (int i = 0; i < enemyCount; i++)
            {
                EnemyType enemyType = SelectEnemyTypeForLocation(currentLocation);
                Enemy enemy = CreateEnemyForEncounter(enemyType);
                
                if (enemy != null)
                {
                    enemies.Add(enemy);
                }
            }

            return enemies;
        }

        /// <summary>
        /// Определить количество врагов
        /// </summary>
        private int DetermineEnemyCount()
        {
            if (!allowEnemyGroups)
                return 1;

            // Базовый шанс на одного врага
            float groupChance = 0.3f;
            
            // Увеличить шанс группы в опасных местах
            Location currentLocation = locationManager?.CurrentLocation ?? Location.Street;
            if (IsDangerousLocation(currentLocation))
            {
                groupChance += 0.2f;
            }

            // Ночное время увеличивает шанс группы
            if (timeOfDayManager != null)
            {
                var period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                {
                    groupChance += 0.3f;
                }
            }

            int count = 1;
            while (count < maxEnemiesPerEncounter && UnityEngine.Random.value < groupChance)
            {
                count++;
                groupChance *= 0.5f; // Снизить шанс для каждого дополнительного врага
            }

            return count;
        }

        /// <summary>
        /// Выбрать тип врага для локации
        /// </summary>
        private EnemyType SelectEnemyTypeForLocation(Location location)
        {
            List<EnemyType> possibleEnemies = GetPossibleEnemiesForLocation(location);
            
            if (possibleEnemies.Count == 0)
            {
                return EnemyType.Thug; // Fallback
            }

            // Учесть уровень игрока и время суток для выбора
            var filteredEnemies = FilterEnemiesByDifficulty(possibleEnemies);
            
            return filteredEnemies[UnityEngine.Random.Range(0, filteredEnemies.Count)];
        }

        /// <summary>
        /// Получить возможных врагов для локации
        /// </summary>
        private List<EnemyType> GetPossibleEnemiesForLocation(Location location)
        {
            var enemies = new List<EnemyType>();

            switch (location)
            {
                case Location.Street:
                    enemies.AddRange(new[] { EnemyType.Thug, EnemyType.Robber, EnemyType.DrunkPerson, 
                                           EnemyType.Bully, EnemyType.Gang_Member });
                    break;

                case Location.Park:
                    enemies.AddRange(new[] { EnemyType.Thug, EnemyType.DrunkPerson, EnemyType.Dealer, 
                                           EnemyType.Bully });
                    break;

                case Location.BusinessDistrict:
                    enemies.AddRange(new[] { EnemyType.Robber, EnemyType.SecurityGuard, EnemyType.Police });
                    break;

                case Location.Industrial:
                    enemies.AddRange(new[] { EnemyType.Gang_Member, EnemyType.Bandit, EnemyType.StreetFighter,
                                           EnemyType.SecurityGuard });
                    break;

                case Location.Abandoned:
                    enemies.AddRange(new[] { EnemyType.Bandit, EnemyType.Gang_Member, EnemyType.StreetFighter,
                                           EnemyType.Dealer });
                    break;

                case Location.Shop:
                    enemies.AddRange(new[] { EnemyType.Robber, EnemyType.SecurityGuard, EnemyType.Thug });
                    break;

                case Location.Hospital:
                    enemies.AddRange(new[] { EnemyType.SecurityGuard, EnemyType.Police });
                    break;

                case Location.University:
                    enemies.AddRange(new[] { EnemyType.Bully, EnemyType.SecurityGuard });
                    break;

                default:
                    enemies.AddRange(new[] { EnemyType.Thug, EnemyType.Robber });
                    break;
            }

            return enemies;
        }

        /// <summary>
        /// Фильтровать врагов по сложности
        /// </summary>
        private List<EnemyType> FilterEnemiesByDifficulty(List<EnemyType> enemies)
        {
            if (!scaleEnemyLevel || playerStats == null)
                return enemies;

            int playerLevel = playerStats.CurrentLevel;
            var filteredEnemies = new List<EnemyType>();

            foreach (var enemyType in enemies)
            {
                float enemyDifficulty = GetEnemyBaseDifficulty(enemyType);
                float levelDifference = Mathf.Abs(enemyDifficulty * 10 - playerLevel);
                
                // Больше шансов на врагов подходящего уровня
                int weight = Mathf.Max(1, 5 - Mathf.RoundToInt(levelDifference));
                for (int i = 0; i < weight; i++)
                {
                    filteredEnemies.Add(enemyType);
                }
            }

            return filteredEnemies.Count > 0 ? filteredEnemies : enemies;
        }

        /// <summary>
        /// Создать врага для встречи
        /// </summary>
        private Enemy CreateEnemyForEncounter(EnemyType enemyType)
        {
            int playerLevel = playerStats?.CurrentLevel ?? 1;
            int minLevel = Mathf.Max(1, playerLevel - 2);
            int maxLevel = playerLevel + 2;

            Enemy enemy = Enemy.CreateRandomEnemy(enemyType, minLevel, maxLevel);
            
            // Дополнительные модификации
            ApplyLocationModifiers(enemy);
            ApplyTimeModifiers(enemy);
            
            return enemy;
        }

        /// <summary>
        /// Применить модификаторы локации
        /// </summary>
        private void ApplyLocationModifiers(Enemy enemy)
        {
            Location currentLocation = locationManager?.CurrentLocation ?? Location.Street;
            
            switch (currentLocation)
            {
                case Location.BusinessDistrict:
                    // Враги более агрессивны в деловом районе
                    enemy.difficulty *= 1.2f;
                    enemy.moneyReward *= 1.5;
                    break;

                case Location.Abandoned:
                    // Враги более опасны в заброшенных местах
                    enemy.maxHealth *= 1.3f;
                    enemy.currentHealth = enemy.maxHealth;
                    enemy.difficulty *= 1.4f;
                    break;

                case Location.Industrial:
                    // Враги с лучшим оружием
                    enemy.maxDamage *= 1.2f;
                    enemy.minDamage *= 1.2f;
                    break;

                case Location.Park:
                    // Более слабые враги в парке днем
                    if (timeOfDayManager != null && timeOfDayManager.CurrentTimePeriod == TimePeriod.Day)
                    {
                        enemy.difficulty *= 0.8f;
                    }
                    break;
            }
        }

        /// <summary>
        /// Применить модификаторы времени
        /// </summary>
        private void ApplyTimeModifiers(Enemy enemy)
        {
            if (timeOfDayManager == null)
                return;

            var period = timeOfDayManager.CurrentTimePeriod;
            
            switch (period)
            {
                case TimePeriod.Night:
                case TimePeriod.LateNight:
                    // Ночью враги агрессивнее и опаснее
                    enemy.aggressiveness = Mathf.Min(1f, enemy.aggressiveness + 0.2f);
                    enemy.moneyReward *= 1.3;
                    enemy.difficulty *= 1.1f;
                    break;

                case TimePeriod.EarlyMorning:
                    // Рано утром враги могут быть пьяными или уставшими
                    if (UnityEngine.Random.value < 0.3f)
                    {
                        enemy.accuracy *= 0.8f;
                        enemy.difficulty *= 0.9f;
                    }
                    break;
            }
        }

        #endregion

        #region Encounter Calculations

        /// <summary>
        /// Рассчитать шанс встречи
        /// </summary>
        private float CalculateEncounterChance()
        {
            float chance = baseEncounterChance;
            
            // Модификатор локации
            if (locationManager != null)
            {
                chance *= GetLocationDangerMultiplier(locationManager.CurrentLocation);
            }
            
            // Модификатор времени суток
            if (timeOfDayManager != null)
            {
                var period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                {
                    chance *= nightEncounterMultiplier;
                }
                else if (period == TimePeriod.EarlyMorning)
                {
                    chance *= 1.3f; // Увеличенный риск рано утром
                }
            }
            
            // Модификатор денег (богатые цели привлекают грабителей)
            if (moneySystem != null)
            {
                double money = moneySystem.GetMoneyDouble();
                if (money > 100)
                {
                    float wealthFactor = Mathf.Clamp01((float)(money / 1000.0));
                    chance *= (1f + wealthFactor * (wealthTargetMultiplier - 1f));
                }
            }
            
            // Модификатор репутации/навыков
            if (skillSystem != null)
            {
                float streetSmarts = skillSystem.GetSkillLevel(SkillType.Luck);
                chance *= (1f - streetSmarts * 0.02f); // -2% за уровень удачи
            }
            
            // Модификатор уровня розыска (разыскиваемые привлекают больше внимания)
            if (policeSystem != null && policeSystem.IsWanted)
            {
                chance *= 1.5f;
            }

            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Определить тип встречи
        /// </summary>
        private CombatEventType DetermineEncounterType()
        {
            float ambushRoll = UnityEngine.Random.value;
            
            if (allowAmbushes && ambushRoll < ambushChance)
            {
                return CombatEventType.Ambush;
            }
            
            // Распределение типов встреч
            float roll = UnityEngine.Random.value;
            
            if (roll < 0.4f)
                return CombatEventType.RandomEncounter;
            else if (roll < 0.7f)
                return CombatEventType.Robbery;
            else if (roll < 0.85f)
                return CombatEventType.TerritoryDispute;
            else if (roll < 0.95f)
                return CombatEventType.Gang_Conflict;
            else
                return CombatEventType.Revenge;
        }

        /// <summary>
        /// Рассчитать уровень угрозы встречи
        /// </summary>
        private float CalculateEncounterThreat(List<Enemy> enemies)
        {
            float totalThreat = 0f;
            
            foreach (var enemy in enemies)
            {
                float enemyThreat = enemy.difficulty * enemy.level;
                enemyThreat += (enemy.maxHealth / 50f); // Нормализация здоровья
                enemyThreat += (enemy.maxDamage / 20f); // Нормализация урона
                
                totalThreat += enemyThreat;
            }
            
            return totalThreat;
        }

        /// <summary>
        /// Рассчитать текущий уровень угрозы
        /// </summary>
        private float CalculateCurrentThreatLevel()
        {
            float threatLevel = 0f;
            
            // Базовая угроза от локации
            if (locationManager != null)
            {
                threatLevel += GetLocationDangerMultiplier(locationManager.CurrentLocation) * 0.3f;
            }
            
            // Угроза от времени суток
            if (timeOfDayManager != null)
            {
                var period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                {
                    threatLevel += 0.4f;
                }
            }
            
            // Угроза от уровня розыска
            if (policeSystem != null && policeSystem.IsWanted)
            {
                threatLevel += (int)policeSystem.CurrentWantedLevel * 0.1f;
            }
            
            // Угроза от недавних встреч
            int recentCombats = 0;
            float recentTime = Time.time - 300f; // Последние 5 минут
            foreach (var encounter in recentEncounters)
            {
                if (encounter.startTime > recentTime)
                {
                    recentCombats++;
                }
            }
            threatLevel += recentCombats * 0.2f;
            
            return Mathf.Clamp01(threatLevel);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Получить множитель опасности локации
        /// </summary>
        private float GetLocationDangerMultiplier(Location location)
        {
            switch (location)
            {
                case Location.Abandoned: return 3.0f;
                case Location.Industrial: return 2.5f;
                case Location.Street: return 1.5f;
                case Location.Park: return 1.2f;
                case Location.Shop: return 1.0f;
                case Location.BusinessDistrict: return 0.8f;
                case Location.Hospital: return 0.3f;
                case Location.University: return 0.4f;
                case Location.Library: return 0.2f;
                case Location.JobCenter: return 0.5f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Проверить, является ли локация опасной
        /// </summary>
        private bool IsDangerousLocation(Location location)
        {
            return GetLocationDangerMultiplier(location) >= 2.0f;
        }

        /// <summary>
        /// Получить базовую сложность врага
        /// </summary>
        private float GetEnemyBaseDifficulty(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.DrunkPerson: return 0.2f;
                case EnemyType.Thug: return 0.3f;
                case EnemyType.Bully: return 0.4f;
                case EnemyType.Robber: return 0.5f;
                case EnemyType.Dealer: return 0.4f;
                case EnemyType.SecurityGuard: return 0.6f;
                case EnemyType.Bandit: return 0.7f;
                case EnemyType.Gang_Member: return 0.75f;
                case EnemyType.StreetFighter: return 0.8f;
                case EnemyType.Police: return 1.0f;
                default: return 0.5f;
            }
        }

        /// <summary>
        /// Показать уведомление о встрече
        /// </summary>
        private void ShowEncounterNotification(EnemyEncounter encounter)
        {
            if (notificationSystem == null)
                return;

            string title = GetEncounterTitle(encounter.eventType);
            string message = GetEncounterMessage(encounter);
            NotificationType type = encounter.threatLevel > 0.7f ? NotificationType.Warning : NotificationType.Info;

            notificationSystem.ShowNotification(title, message, type);
        }

        /// <summary>
        /// Получить заголовок встречи
        /// </summary>
        private string GetEncounterTitle(CombatEventType eventType)
        {
            switch (eventType)
            {
                case CombatEventType.Ambush: return "Засада!";
                case CombatEventType.Robbery: return "Ограбление!";
                case CombatEventType.TerritoryDispute: return "Конфликт!";
                case CombatEventType.Gang_Conflict: return "Банда!";
                case CombatEventType.Revenge: return "Месть!";
                default: return "Опасность!";
            }
        }

        /// <summary>
        /// Получить сообщение встречи
        /// </summary>
        private string GetEncounterMessage(EnemyEncounter encounter)
        {
            int enemyCount = encounter.enemies.Count;
            string enemyText = enemyCount == 1 ? "враг появился" : $"{enemyCount} врагов появилось";
            
            return $"{enemyText} поблизости!";
        }

        /// <summary>
        /// Очистить старые встречи
        /// </summary>
        private void CleanupOldEncounters()
        {
            float cutoffTime = Time.time - 3600f; // Удалить встречи старше часа
            recentEncounters.RemoveAll(e => e.startTime < cutoffTime);
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public EnemySystemSaveData GetSaveData()
        {
            return new EnemySystemSaveData
            {
                lastEncounterCheck = lastEncounterCheck,
                recentEncounters = new List<EnemyEncounter>(recentEncounters),
                isEncounterActive = isEncounterActive
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(EnemySystemSaveData data)
        {
            lastEncounterCheck = data.lastEncounterCheck;
            recentEncounters = data.recentEncounters ?? new List<EnemyEncounter>();
            isEncounterActive = data.isEncounterActive;

            if (showDebugInfo)
            {
                Debug.Log($"EnemySystem загружен. Встреч: {recentEncounters.Count}, " +
                         $"Активна встреча: {isEncounterActive}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик окончания боя
        /// </summary>
        private void OnCombatEnded(CombatData combatData, CombatState endState)
        {
            // Найти соответствующую встречу
            var encounter = recentEncounters.Find(e => e.startTime <= combatData.startTime && 
                                                      e.endTime == 0);
            
            if (encounter != null)
            {
                bool playerWon = endState == CombatState.PlayerWon;
                EndEncounter(encounter, playerWon);
            }
        }

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(Location oldLocation, Location newLocation)
        {
            // Обновить уровень угрозы
            float newThreatLevel = CalculateCurrentThreatLevel();
            OnThreatLevelChanged?.Invoke(newThreatLevel);

            if (showDebugInfo)
            {
                Debug.Log($"Локация изменена: {oldLocation} -> {newLocation}, " +
                         $"Уровень угрозы: {newThreatLevel:F2}");
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Force Random Encounter")]
        private void DebugForceRandomEncounter()
        {
            TriggerRandomEncounter();
        }

        [ContextMenu("Force Specific Enemy")]
        private void DebugForceSpecificEnemy()
        {
            if (combatSystem != null && combatSystem.CanStartCombat())
            {
                var enemies = new List<Enemy> { Enemy.CreateTestEnemy(debugEnemyType) };
                combatSystem.StartCombat(enemies, CombatEventType.RandomEncounter);
            }
        }

        [ContextMenu("Show Threat Level")]
        private void DebugShowThreatLevel()
        {
            float threatLevel = CalculateCurrentThreatLevel();
            Debug.Log($"Текущий уровень угрозы: {threatLevel:F2}");
            Debug.Log($"Шанс встречи: {CalculateEncounterChance():F3}");
        }

        [ContextMenu("Show Recent Encounters")]
        private void DebugShowRecentEncounters()
        {
            Debug.Log($"Недавних встреч: {recentEncounters.Count}");
            foreach (var encounter in recentEncounters)
            {
                Debug.Log($"- {encounter.eventType} в {encounter.location}, " +
                         $"Врагов: {encounter.enemies.Count}, Угроза: {encounter.threatLevel:F1}");
            }
        }

        [ContextMenu("Clear Encounter History")]
        private void DebugClearEncounterHistory()
        {
            recentEncounters.Clear();
            Debug.Log("История встреч очищена");
        }

        #endregion
    }

    /// <summary>
    /// Данные встречи с врагами
    /// </summary>
    [System.Serializable]
    public class EnemyEncounter
    {
        public CombatEventType eventType;       // Тип события
        public List<Enemy> enemies;             // Список врагов
        public Location location;               // Место встречи
        public float startTime;                 // Время начала
        public float endTime;                   // Время окончания
        public float duration;                  // Длительность
        public int playerLevel;                 // Уровень игрока
        public float threatLevel;               // Уровень угрозы
        public bool playerWon;                  // Победил ли игрок
        
        public EnemyEncounter()
        {
            enemies = new List<Enemy>();
        }
    }

    /// <summary>
    /// Данные EnemySystem для сохранения
    /// </summary>
    [System.Serializable]
    public class EnemySystemSaveData
    {
        public float lastEncounterCheck;
        public List<EnemyEncounter> recentEncounters;
        public bool isEncounterActive;
    }
}
