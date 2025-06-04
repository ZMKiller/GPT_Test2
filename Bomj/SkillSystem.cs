using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система навыков персонажа
    /// </summary>
    public class SkillSystem : MonoBehaviour
    {
        [Header("Настройки навыков")]
        [SerializeField] private int maxSkillLevel = 100;
        [SerializeField] private int startingSkillPoints = 5;
        [SerializeField] private int skillPointsPerLevel = 2;

        [Header("Стоимость улучшения")]
        [SerializeField] private int baseSkillCost = 1;
        [SerializeField] private float skillCostMultiplier = 1.1f;

        [Header("Эффекты")]
        [SerializeField] private AudioClip skillUpSound;
        [SerializeField] private GameObject skillUpEffectPrefab;

        // Компоненты
        private PlayerStats playerStats;
        private AudioSource audioSource;

        // Навыки
        private Dictionary<SkillType, int> skills = new Dictionary<SkillType, int>();
        private Dictionary<SkillType, float> skillExperience = new();
        private int availableSkillPoints = 0;

        // Бонусы
        private Dictionary<SkillType, float> skillBonuses = new Dictionary<SkillType, float>();

        // Модификаторы навыков
        private Dictionary<SkillType, List<SkillModifier>> skillModifiers = new();

        // События
        public event Action<SkillType, int> OnSkillUpgraded;
        public event Action<int> OnSkillPointsChanged;
        public event Action<SkillType, float> OnSkillBonusChanged;
        public event Action<SkillType, int> OnSkillUpdated;

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            playerStats = GetComponent<PlayerStats>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Инициализация навыков
            InitializeSkills();
        }

        private void Start()
        {
            // Проверка ссылок
            if (playerStats == null)
            {
                Debug.LogError("SkillSystem: PlayerStats не найден!");
                enabled = false;
                return;
            }

            // Подписка на события
            SubscribeToEvents();

            // Начальные очки навыков
            availableSkillPoints = startingSkillPoints;

            Debug.Log("SkillSystem инициализирован");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация навыков
        /// </summary>
        private void InitializeSkills()
        {
            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                skills[skillType] = 0;
                skillBonuses[skillType] = 0f;
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnLevelUp += OnLevelUp;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить уровень навыка
        /// </summary>
        public int GetSkillLevel(SkillType skillType)
        {
            return skills.ContainsKey(skillType) ? skills[skillType] : 0;
        }

        /// <summary>
        /// Получить бонус от навыка
        /// </summary>
        public float GetSkillBonus(SkillType skillType)
        {
            return skillBonuses.ContainsKey(skillType) ? skillBonuses[skillType] : 0f;
        }

        /// <summary>
        /// Получить все навыки
        /// </summary>
        public Dictionary<SkillType, int> GetAllSkills()
        {
            return new Dictionary<SkillType, int>(skills);
        }

        /// <summary>
        /// Получить доступные очки навыков
        /// </summary>
        public int GetAvailableSkillPoints()
        {
            return availableSkillPoints;
        }

        /// <summary>
        /// Добавить очки навыков
        /// </summary>
        public void AddSkillPoints(int amount)
        {
            if (amount <= 0) return;

            availableSkillPoints += amount;
            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            Debug.Log($"Получено очков навыков: +{amount} (всего: {availableSkillPoints})");
        }

        /// <summary>
        /// Улучшить навык
        /// </summary>
        public bool UpgradeSkill(SkillType skillType)
        {
            int currentLevel = GetSkillLevel(skillType);
            int cost = GetSkillUpgradeCost(skillType);

            // Проверки
            if (currentLevel >= maxSkillLevel)
            {
                Debug.Log($"Навык {skillType} уже максимального уровня");
                return false;
            }

            if (availableSkillPoints < cost)
            {
                Debug.Log($"Недостаточно очков навыков для улучшения {skillType}");
                return false;
            }

            // Улучшение
            skills[skillType] = currentLevel + 1;
            availableSkillPoints -= cost;

            // Пересчет бонусов
            RecalculateSkillBonus(skillType);

            // Эффекты
            PlaySkillUpEffects();

            // События
            OnSkillUpgraded?.Invoke(skillType, skills[skillType]);
            OnSkillUpdated?.Invoke(skillType, skills[skillType]);
            OnSkillPointsChanged?.Invoke(availableSkillPoints);
            GameEvents.TriggerSkillUpgrade(new SkillUpgradeEventData
            {
                skillType = skillType,
                newLevel = skills[skillType],
                pointsSpent = cost
            });

            Debug.Log($"Навык {skillType} улучшен до уровня {skills[skillType]} (стоимость: {cost})");
            return true;
        }

        /// <summary>
        /// Добавить опыт навыку
        /// </summary>
        public void AddSkillExperience(SkillType skillType, float amount, string reason = "")
        {
            if (!skillExperience.ContainsKey(skillType))
                skillExperience[skillType] = 0f;
            skillExperience[skillType] += amount;
        }

        /// <summary>
        /// Добавить временный модификатор навыка
        /// </summary>
        public void AddSkillModifier(SkillModifier modifier)
        {
            if (!skillModifiers.ContainsKey(modifier.skillType))
                skillModifiers[modifier.skillType] = new List<SkillModifier>();
            skillModifiers[modifier.skillType].Add(modifier);
        }

        /// <summary>
        /// Получить стоимость улучшения навыка
        /// </summary>
        public int GetSkillUpgradeCost(SkillType skillType)
        {
            int currentLevel = GetSkillLevel(skillType);
            return Mathf.RoundToInt(baseSkillCost * Mathf.Pow(skillCostMultiplier, currentLevel));
        }

        /// <summary>
        /// Проверить возможность улучшения навыка
        /// </summary>
        public bool CanUpgradeSkill(SkillType skillType)
        {
            int currentLevel = GetSkillLevel(skillType);
            int cost = GetSkillUpgradeCost(skillType);

            return currentLevel < maxSkillLevel && availableSkillPoints >= cost;
        }

        /// <summary>
        /// Получить описание навыка
        /// </summary>
        public string GetSkillDescription(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Charisma:
                    return "Повышает заработок от взаимодействий с людьми и открывает социальные возможности";
                
                case SkillType.Education:
                    return "Открывает доступ к лучшим работам и образовательным программам";
                
                case SkillType.Fitness:
                    return "Улучшает здоровье, выносливость и снижает скорость деградации";
                
                case SkillType.Luck:
                    return "Увеличивает шанс положительных случайных событий и бонусов";
                
                case SkillType.Business:
                    return "Повышает доходы от бизнеса и открывает предпринимательские возможности";
                
                default:
                    return "Неизвестный навык";
            }
        }

        /// <summary>
        /// Получить название навыка
        /// </summary>
        public string GetSkillName(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.Charisma: return "Харизма";
                case SkillType.Education: return "Образование";
                case SkillType.Fitness: return "Физическая форма";
                case SkillType.Luck: return "Удача";
                case SkillType.Business: return "Деловые навыки";
                default: return skillType.ToString();
            }
        }

        /// <summary>
        /// Применить бонус престижа
        /// </summary>
        public void ApplyPrestigeBonus(int prestigeLevel)
        {
            // Сохранить часть навыков (25% за каждый уровень престижа)
            float retentionRate = 0.25f * prestigeLevel;
            retentionRate = Mathf.Clamp01(retentionRate);

            var originalSkills = new Dictionary<SkillType, int>(skills);

            foreach (var skillType in originalSkills.Keys)
            {
                int originalLevel = originalSkills[skillType];
                int retainedLevel = Mathf.RoundToInt(originalLevel * retentionRate);
                skills[skillType] = retainedLevel;
                RecalculateSkillBonus(skillType);
            }

            // Добавить бонусные очки навыков за престиж
            int bonusPoints = prestigeLevel * 10;
            AddSkillPoints(bonusPoints);

            Debug.Log($"Применен бонус престижа: сохранено {retentionRate:P0} навыков, " +
                     $"получено {bonusPoints} бонусных очков");
        }

        #endregion

        #region Skill Bonus Calculation

        /// <summary>
        /// Пересчитать бонус навыка
        /// </summary>
        private void RecalculateSkillBonus(SkillType skillType)
        {
            int level = GetSkillLevel(skillType);
            float bonus = CalculateSkillBonus(skillType, level);
            
            skillBonuses[skillType] = bonus;
            OnSkillBonusChanged?.Invoke(skillType, bonus);

            // Применить бонус к соответствующим системам
            ApplySkillBonus(skillType, bonus);
        }

        /// <summary>
        /// Вычислить бонус навыка
        /// </summary>
        private float CalculateSkillBonus(SkillType skillType, int level)
        {
            switch (skillType)
            {
                case SkillType.Charisma:
                    // Бонус к заработку от людей: +2% за уровень
                    return level * 0.02f;
                
                case SkillType.Education:
                    // Бонус к опыту: +1% за уровень
                    return level * 0.01f;
                
                case SkillType.Fitness:
                    // Снижение деградации здоровья: -1% за уровень
                    return level * 0.01f;
                
                case SkillType.Luck:
                    // Бонус к случайным событиям: +0.5% за уровень
                    return level * 0.005f;
                
                case SkillType.Business:
                    // Бонус к доходам от работы: +1.5% за уровень
                    return level * 0.015f;
                
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Применить бонус навыка
        /// </summary>
        private void ApplySkillBonus(SkillType skillType, float bonus)
        {
            switch (skillType)
            {
                case SkillType.Fitness:
                    // Применить к системе деградации
                    var degradationManager = FindObjectOfType<StatsDegradationManager>();
                    if (degradationManager != null)
                    {
                        degradationManager.SetFitnessModifier(1f - bonus);
                    }
                    break;
                
                // Другие навыки применяются в соответствующих системах
                // при расчете заработка, опыта и т.д.
            }
        }

        /// <summary>
        /// Получить модификатор заработка от харизмы
        /// </summary>
        public float GetCharismaEarningsModifier()
        {
            return 1f + GetSkillBonus(SkillType.Charisma);
        }

        /// <summary>
        /// Получить модификатор опыта от образования
        /// </summary>
        public float GetEducationExperienceModifier()
        {
            return 1f + GetSkillBonus(SkillType.Education);
        }

        /// <summary>
        /// Получить модификатор удачи
        /// </summary>
        public float GetLuckModifier()
        {
            return GetSkillBonus(SkillType.Luck);
        }

        /// <summary>
        /// Получить модификатор бизнес доходов
        /// </summary>
        public float GetBusinessIncomeModifier()
        {
            return 1f + GetSkillBonus(SkillType.Business);
        }

        /// <summary>
        /// Проверить требования навыков
        /// </summary>
        public bool CheckSkillRequirements(Dictionary<SkillType, int> requirements)
        {
            if (requirements == null) return true;

            foreach (var requirement in requirements)
            {
                if (GetSkillLevel(requirement.Key) < requirement.Value)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Воспроизведение эффектов улучшения навыка
        /// </summary>
        private void PlaySkillUpEffects()
        {
            // Звуковой эффект
            if (skillUpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(skillUpSound);
            }
            
            // Визуальный эффект
            if (skillUpEffectPrefab != null)
            {
                GameObject effect = Instantiate(skillUpEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            // Добавить очки навыков за повышение уровня
            AddSkillPoints(skillPointsPerLevel);
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public SkillSystemSaveData GetSaveData()
        {
            var skillsData = new Dictionary<string, int>();
            var levelData = new Dictionary<SkillType, int>();
            foreach (var skill in skills)
            {
                skillsData[skill.Key.ToString()] = skill.Value;
                levelData[skill.Key] = skill.Value;
            }

            return new SkillSystemSaveData
            {
                skills = skillsData,
                skillLevels = levelData,
                availableSkillPoints = this.availableSkillPoints
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(SkillSystemSaveData data)
        {
            if (data != null)
            {
                if (data.skillLevels != null && data.skillLevels.Count > 0)
                {
                    foreach (var kvp in data.skillLevels)
                    {
                        skills[kvp.Key] = kvp.Value;
                        RecalculateSkillBonus(kvp.Key);
                    }
                }
                else if (data.skills != null)
                {
                    foreach (var skillData in data.skills)
                    {
                        if (Enum.TryParse<SkillType>(skillData.Key, out SkillType skillType))
                        {
                            skills[skillType] = skillData.Value;
                            RecalculateSkillBonus(skillType);
                        }
                    }
                }
                availableSkillPoints = data.availableSkillPoints;
            }

            OnSkillPointsChanged?.Invoke(availableSkillPoints);
        }

        #endregion

        #region Context Menu

        [ContextMenu("Добавить 10 очков навыков")]
        private void DebugAddSkillPoints()
        {
            AddSkillPoints(10);
        }

        [ContextMenu("Улучшить все навыки")]
        private void DebugUpgradeAllSkills()
        {
            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                if (CanUpgradeSkill(skillType))
                {
                    UpgradeSkill(skillType);
                }
            }
        }

        [ContextMenu("Показать информацию о навыках")]
        private void DebugShowSkillInfo()
        {
            Debug.Log($"Доступно очков навыков: {availableSkillPoints}");
            foreach (var skill in skills)
            {
                float bonus = GetSkillBonus(skill.Key);
                int cost = GetSkillUpgradeCost(skill.Key);
                Debug.Log($"{GetSkillName(skill.Key)}: {skill.Value}/{maxSkillLevel} " +
                         $"(Бонус: {bonus:P1}, Стоимость улучшения: {cost})");
            }
        }

        [ContextMenu("Сбросить навыки")]
        private void DebugResetSkills()
        {
            // Возврат очков
            foreach (var skill in skills)
            {
                for (int i = 1; i <= skill.Value; i++)
                {
                    int cost = Mathf.RoundToInt(baseSkillCost * Mathf.Pow(skillCostMultiplier, i - 1));
                    availableSkillPoints += cost;
                }
            }

            // Сброс навыков
            InitializeSkills();
            OnSkillPointsChanged?.Invoke(availableSkillPoints);

            Debug.Log("Навыки сброшены, очки возвращены");
        }

        #endregion
    }


    [Serializable]
    public class SkillModifier
    {
        public SkillType skillType;
        public float value;
        public ModifierOperation operation;
        public string source;
        public float duration;
        public float startTime;

        public SkillModifier(SkillType skillType, float value, ModifierOperation operation, string source, float duration)
        {
            this.skillType = skillType;
            this.value = value;
            this.operation = operation;
            this.source = source;
            this.duration = duration;
            this.startTime = Time.time;
        }
    }
}
