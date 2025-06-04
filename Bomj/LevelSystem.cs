using System;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система уровней и опыта персонажа
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        [Header("Настройки опыта")]
        [SerializeField] private int baseExperienceRequirement = 100;
        [SerializeField] private float experienceMultiplier = 1.2f;
        [SerializeField] private int maxLevel = 50;
        [SerializeField] private bool enablePrestige = true;
        [SerializeField] private int prestigeLevel = 0;

        [Header("Награды за уровень")]
        [SerializeField] private int baseSkillPointsReward = 2;
        [SerializeField] private double baseLevelUpMoneyReward = 50.0;
        [SerializeField] private float moneyRewardMultiplier = 1.5f;

        [Header("Визуальные эффекты")]
        [SerializeField] private GameObject levelUpEffectPrefab;
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField] private float effectDuration = 3f;

        // Компоненты
        private PlayerStats playerStats;
        private SkillSystem skillSystem;
        private MoneySystem moneySystem;
        private AudioSource audioSource;

        // Кешированные значения
        private int[] experienceRequirements;
        private bool isInitialized = false;

        // События
        public event Action<LevelUpData> OnLevelUp;
        public event Action<int> OnExperienceGained;
        public event Action<int> OnPrestigeUp;

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            playerStats = GetComponent<PlayerStats>();
            skillSystem = GetComponent<SkillSystem>();
            moneySystem = FindObjectOfType<MoneySystem>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Инициализация таблицы опыта
            InitializeExperienceTable();
        }

        private void Start()
        {
            // Проверка ссылок
            if (playerStats == null)
            {
                Debug.LogError("LevelSystem: PlayerStats не найден!");
                enabled = false;
                return;
            }

            // Подписка на события
            SubscribeToEvents();
            isInitialized = true;

            Debug.Log("LevelSystem инициализирован");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация таблицы требований опыта
        /// </summary>
        private void InitializeExperienceTable()
        {
            experienceRequirements = new int[maxLevel + 1];
            experienceRequirements[0] = 0;

            for (int level = 1; level <= maxLevel; level++)
            {
                experienceRequirements[level] = Mathf.RoundToInt(
                    baseExperienceRequirement * Mathf.Pow(experienceMultiplier, level - 1)
                );
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnMoneyEarned += OnMoneyEarned;
            GameEvents.OnNPCInteraction += OnNPCInteraction;
            GameEvents.OnJobCompleted += OnJobCompleted;
            GameEvents.OnEducationCompleted += OnEducationCompleted;
            GameEvents.OnItemPurchased += OnItemPurchased;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnMoneyEarned -= OnMoneyEarned;
            GameEvents.OnNPCInteraction -= OnNPCInteraction;
            GameEvents.OnJobCompleted -= OnJobCompleted;
            GameEvents.OnEducationCompleted -= OnEducationCompleted;
            GameEvents.OnItemPurchased -= OnItemPurchased;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавить опыт игроку
        /// </summary>
        public void AddExperience(int amount, string source = "")
        {
            if (!isInitialized || amount <= 0) return;

            int currentLevel = playerStats.Level;
            int currentExp = playerStats.Experience;
            int newExp = currentExp + amount;

            playerStats.SetExperience(newExp);
            OnExperienceGained?.Invoke(amount);

            Debug.Log($"Получен опыт: +{amount} ({source})");

            // Проверка повышения уровня
            CheckLevelUp(currentLevel, newExp);
        }

        /// <summary>
        /// Получить требование опыта для уровня
        /// </summary>
        public int GetExperienceRequirement(int level)
        {
            if (level <= 0 || level > maxLevel) return int.MaxValue;
            return experienceRequirements[level];
        }

        /// <summary>
        /// Получить общее требование опыта для достижения уровня
        /// </summary>
        public int GetTotalExperienceRequirement(int level)
        {
            if (level <= 0) return 0;
            if (level > maxLevel) level = maxLevel;

            int total = 0;
            for (int i = 1; i <= level; i++)
            {
                total += experienceRequirements[i];
            }
            return total;
        }

        /// <summary>
        /// Получить прогресс до следующего уровня (0-1)
        /// </summary>
        public float GetLevelProgress()
        {
            int currentLevel = playerStats.Level;
            if (currentLevel >= maxLevel) return 1f;

            int currentExp = playerStats.Experience;
            int totalExpForCurrentLevel = GetTotalExperienceRequirement(currentLevel);
            int totalExpForNextLevel = GetTotalExperienceRequirement(currentLevel + 1);
            
            float progress = (float)(currentExp - totalExpForCurrentLevel) / 
                           (float)(totalExpForNextLevel - totalExpForCurrentLevel);
            
            return Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Получить опыт до следующего уровня
        /// </summary>
        public int GetExperienceToNextLevel()
        {
            int currentLevel = playerStats.Level;
            if (currentLevel >= maxLevel) return 0;

            int currentExp = playerStats.Experience;
            int requiredExp = GetTotalExperienceRequirement(currentLevel + 1);
            
            return Mathf.Max(0, requiredExp - currentExp);
        }

        /// <summary>
        /// Проверить возможность престижа
        /// </summary>
        public bool CanPrestige()
        {
            return enablePrestige && playerStats.Level >= maxLevel;
        }

        /// <summary>
        /// Выполнить престиж
        /// </summary>
        public void Prestige()
        {
            if (!CanPrestige()) return;

            prestigeLevel++;
            
            // Сброс уровня и опыта
            playerStats.SetLevel(1);
            playerStats.SetExperience(0);
            
            // Сохранение части навыков (например, 25%)
            if (skillSystem != null)
            {
                skillSystem.ApplyPrestigeBonus(prestigeLevel);
            }
            
            // Престиж бонусы
            ApplyPrestigeBonuses();
            
            OnPrestigeUp?.Invoke(prestigeLevel);
            GameEvents.TriggerPrestige(prestigeLevel);
            
            Debug.Log($"Престиж выполнен! Уровень престижа: {prestigeLevel}");
        }

        /// <summary>
        /// Получить уровень престижа
        /// </summary>
        public int GetPrestigeLevel()
        {
            return prestigeLevel;
        }

        /// <summary>
        /// Установить уровень престижа (для загрузки)
        /// </summary>
        public void SetPrestigeLevel(int level)
        {
            prestigeLevel = level;
        }

        #endregion

        #region Level Up Logic

        /// <summary>
        /// Проверка повышения уровня
        /// </summary>
        private void CheckLevelUp(int oldLevel, int currentExp)
        {
            int newLevel = CalculateLevelFromExperience(currentExp);
            
            if (newLevel > oldLevel)
            {
                ProcessLevelUp(oldLevel, newLevel);
            }
        }

        /// <summary>
        /// Вычислить уровень по опыту
        /// </summary>
        private int CalculateLevelFromExperience(int experience)
        {
            for (int level = 1; level <= maxLevel; level++)
            {
                if (experience < GetTotalExperienceRequirement(level))
                {
                    return level - 1;
                }
            }
            return maxLevel;
        }

        /// <summary>
        /// Обработка повышения уровня
        /// </summary>
        private void ProcessLevelUp(int oldLevel, int newLevel)
        {
            playerStats.SetLevel(newLevel);
            
            // Подсчет наград за все пропущенные уровни
            int totalSkillPoints = 0;
            double totalMoney = 0;
            
            for (int level = oldLevel + 1; level <= newLevel; level++)
            {
                totalSkillPoints += GetSkillPointsReward(level);
                totalMoney += GetMoneyReward(level);
            }
            
            // Выдача наград
            if (skillSystem != null && totalSkillPoints > 0)
            {
                skillSystem.AddSkillPoints(totalSkillPoints);
            }
            
            if (moneySystem != null && totalMoney > 0)
            {
                moneySystem.AddMoney(totalMoney, "Level Up Bonus");
            }
            
            // Создание данных о повышении уровня
            LevelUpData levelUpData = new LevelUpData
            {
                oldLevel = oldLevel,
                newLevel = newLevel,
                skillPointsGained = totalSkillPoints,
                moneyGained = totalMoney,
                unlockedFeatures = GetUnlockedFeatures(newLevel)
            };
            
            // Визуальные эффекты
            PlayLevelUpEffects();
            
            // События
            OnLevelUp?.Invoke(levelUpData);
            GameEvents.TriggerLevelUp(levelUpData);
            
            Debug.Log($"Повышение уровня! {oldLevel} → {newLevel} " +
                     $"(+{totalSkillPoints} очков навыков, +{GameUtils.FormatMoney(totalMoney)})");
        }

        /// <summary>
        /// Получить награду очков навыков за уровень
        /// </summary>
        private int GetSkillPointsReward(int level)
        {
            int baseReward = baseSkillPointsReward;
            
            // Бонусы за определенные уровни
            if (level % 10 == 0) baseReward += 2; // Каждый 10-й уровень
            if (level % 25 == 0) baseReward += 5; // Каждый 25-й уровень
            
            // Бонус от престижа
            baseReward += prestigeLevel;
            
            return baseReward;
        }

        /// <summary>
        /// Получить денежную награду за уровень
        /// </summary>
        private double GetMoneyReward(int level)
        {
            double reward = baseLevelUpMoneyReward * Math.Pow(moneyRewardMultiplier, level - 1);
            
            // Бонус от престижа
            reward *= (1.0 + prestigeLevel * 0.1);
            
            return reward;
        }

        /// <summary>
        /// Получить разблокированные функции
        /// </summary>
        private string[] GetUnlockedFeatures(int level)
        {
            var features = new System.Collections.Generic.List<string>();
            
            // Разблокировки по уровням
            switch (level)
            {
                case 5:
                    features.Add("Магазин одежды");
                    break;
                case 10:
                    features.Add("Система работы");
                    break;
                case 15:
                    features.Add("Образовательные курсы");
                    break;
                case 20:
                    features.Add("Жилье");
                    break;
                case 25:
                    features.Add("Транспорт");
                    break;
                case 30:
                    features.Add("Бизнес-возможности");
                    break;
                case 40:
                    features.Add("Люксовые товары");
                    break;
                case 50:
                    features.Add("Престиж система");
                    break;
            }
            
            return features.ToArray();
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Воспроизведение эффектов повышения уровня
        /// </summary>
        private void PlayLevelUpEffects()
        {
            // Звуковой эффект
            if (levelUpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(levelUpSound);
            }
            
            // Визуальный эффект
            if (levelUpEffectPrefab != null)
            {
                GameObject effect = Instantiate(levelUpEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, effectDuration);
            }
            
            // Уведомление
            GameEvents.TriggerNotification(
                $"Поздравляем! Достигнут {playerStats.Level} уровень!",
                NotificationType.Achievement,
                NotificationPriority.High
            );
        }

        /// <summary>
        /// Применение бонусов престижа
        /// </summary>
        private void ApplyPrestigeBonuses()
        {
            // Увеличение базовых характеристик
            float prestigeBonus = 1.0f + (prestigeLevel * 0.05f);
            
            // Можно добавить постоянные бонусы от престижа
            // Например, увеличение базового здоровья, скорости восстановления и т.д.
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик заработка денег
        /// </summary>
        private void OnMoneyEarned(MoneyEventData data)
        {
            if (data.amount > 0)
            {
                // Опыт за заработок (1 опыт за каждые 10 монет)
                int exp = Mathf.Max(1, Mathf.RoundToInt((float)data.amount / 10));
                AddExperience(exp, "Money Earned");
            }
        }

        /// <summary>
        /// Обработчик взаимодействия с NPC
        /// </summary>
        private void OnNPCInteraction(NPCInteractionEventData data)
        {
            // Опыт за социальные взаимодействия
            int exp = GetNPCInteractionExperience(data.npcType);
            AddExperience(exp, "NPC Interaction");
        }

        /// <summary>
        /// Получить опыт за взаимодействие с NPC
        /// </summary>
        private int GetNPCInteractionExperience(NPCType npcType)
        {
            switch (npcType)
            {
                case NPCType.Poor: return 1;
                case NPCType.Normal: return 2;
                case NPCType.Rich: return 5;
                case NPCType.Tourist: return 3;
                case NPCType.Businessman: return 10;
                default: return 1;
            }
        }

        /// <summary>
        /// Обработчик завершения работы
        /// </summary>
        private void OnJobCompleted(JobEventData data)
        {
            int exp = GetJobCompletionExperience(data.jobType);
            AddExperience(exp, "Job Completed");
        }

        /// <summary>
        /// Получить опыт за завершение работы
        /// </summary>
        private int GetJobCompletionExperience(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.Cleaner: return 5;
                case JobType.Security: return 8;
                case JobType.Seller: return 10;
                case JobType.Manager: return 15;
                case JobType.Businessman: return 25;
                case JobType.CEO: return 50;
                default: return 1;
            }
        }

        /// <summary>
        /// Обработчик завершения образования
        /// </summary>
        private void OnEducationCompleted(EducationEventData data)
        {
            int exp = GetEducationExperience(data.educationType);
            AddExperience(exp, "Education Completed");
        }

        /// <summary>
        /// Получить опыт за образование
        /// </summary>
        private int GetEducationExperience(EducationType educationType)
        {
            switch (educationType)
            {
                case EducationType.BasicSkills: return 20;
                case EducationType.ComputerCourse: return 30;
                case EducationType.LanguageCourse: return 35;
                case EducationType.BusinessSchool: return 50;
                case EducationType.University: return 100;
                case EducationType.MBA: return 200;
                default: return 10;
            }
        }

        /// <summary>
        /// Обработчик покупки предметов
        /// </summary>
        private void OnItemPurchased(ShopEventData data)
        {
            // Опыт за крупные покупки
            if (data.totalCost > 1000)
            {
                int exp = Mathf.RoundToInt((float)data.totalCost / 1000);
                AddExperience(exp, "Expensive Purchase");
            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public LevelSystemSaveData GetSaveData()
        {
            return new LevelSystemSaveData
            {
                prestigeLevel = this.prestigeLevel,
                lastLevelUpTime = DateTime.Now.ToBinary()
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(LevelSystemSaveData data)
        {
            if (data != null)
            {
                this.prestigeLevel = data.prestigeLevel;
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Добавить 100 опыта")]
        private void DebugAddExperience()
        {
            AddExperience(100, "Debug");
        }

        [ContextMenu("Повысить уровень")]
        private void DebugLevelUp()
        {
            int expToNext = GetExperienceToNextLevel();
            AddExperience(expToNext, "Debug Level Up");
        }

        [ContextMenu("Выполнить престиж")]
        private void DebugPrestige()
        {
            if (CanPrestige())
            {
                Prestige();
            }
            else
            {
                Debug.Log("Престиж недоступен");
            }
        }

        [ContextMenu("Показать информацию об уровне")]
        private void DebugShowLevelInfo()
        {
            Debug.Log($"Уровень: {playerStats.Level}, Опыт: {playerStats.Experience}, " +
                     $"До следующего уровня: {GetExperienceToNextLevel()}, " +
                     $"Прогресс: {GetLevelProgress():P1}, Престиж: {prestigeLevel}");
        }

        #endregion
    }

    /// <summary>
    /// Данные о повышении уровня
    /// </summary>
    [System.Serializable]
    public class LevelUpData
    {
        public int oldLevel;
        public int newLevel;
        public int skillPointsGained;
        public double moneyGained;
        public string[] unlockedFeatures;
        
        public DateTime timestamp = DateTime.Now;
    }

    /// <summary>
    /// Данные системы уровней для сохранения
    /// </summary>
    [System.Serializable]
    public class LevelSystemSaveData
    {
        public int prestigeLevel = 0;
        public long lastLevelUpTime = 0;
    }

    /// <summary>
    /// Данные событий для новых систем
    /// </summary>
    [System.Serializable]
    public class JobEventData
    {
        public JobType jobType;
        public double payment;
        public int hoursWorked;
        public DateTime timestamp = DateTime.Now;
    }

    [System.Serializable]
    public class EducationEventData
    {
        public EducationType educationType;
        public double cost;
        public int skillPointsGained;
        public DateTime completionTime = DateTime.Now;
    }

    [System.Serializable]
    public class ShopEventData
    {
        public ShopCategory category;
        public string itemName;
        public double totalCost;
        public ItemQuality quality;
        public DateTime purchaseTime = DateTime.Now;
    }
}
