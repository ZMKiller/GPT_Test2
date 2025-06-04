using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система характеристик игрока
    /// </summary>
    [Serializable]
    public class PlayerStats : MonoBehaviour
    {
        [Header("Базовые характеристики")]
        [SerializeField] private float maxHealth = 100f;        // Максимальное здоровье
        [SerializeField] private float maxHunger = 100f;        // Максимальный голод
        [SerializeField] private float maxMood = 100f;          // Максимальное настроение
        
        [Header("Текущие значения")]
        [SerializeField] private float currentHealth = 100f;    // Текущее здоровье
        [SerializeField] private float currentHunger = 0f;      // Текущий голод (0 = сыт, 100 = очень голоден)
        [SerializeField] private float currentMood = 75f;       // Текущее настроение
        [SerializeField] private float currentMoney = 0f;       // Текущие деньги
        [SerializeField] private int currentLevel = 1;          // Текущий уровень
        [SerializeField] private float currentExperience = 0f;  // Текущий опыт

        [Header("Прогрессия")]
        [SerializeField] private float experienceToNextLevel = 100f; // Опыт до следующего уровня
        [SerializeField] private float experienceMultiplier = 1.5f;  // Множитель роста опыта

        [Header("Критические значения")]
        [SerializeField] private float lowHealthThreshold = 20f;     // Порог низкого здоровья
        [SerializeField] private float highHungerThreshold = 80f;    // Порог высокого голода

        [Header("Модификаторы")]
        [SerializeField] private bool enableSkillModifiers = true;   // Включить модификаторы от навыков
        [SerializeField] private bool enableItemModifiers = true;    // Включить модификаторы от предметов

        // Компоненты системы прогрессии
        private SkillSystem skillSystem;
        private ShopSystem shopSystem;
        private LevelSystem levelSystem;

        // Модификаторы характеристик
        private Dictionary<StatType, List<StatModifier>> statModifiers = new Dictionary<StatType, List<StatModifier>>();
        private Dictionary<StatType, float> cachedModifiers = new Dictionary<StatType, float>();
        private bool modifiersCacheValid = false;

        // Публичные свойства для чтения характеристик
        public float Health => currentHealth;
        public float MaxHealth => GetModifiedStat(StatType.Health, maxHealth);
        public float HealthPercentage => currentHealth / Mathf.Max(MaxHealth, 0.001f);

        public float Hunger => currentHunger;
        public float MaxHunger => GetModifiedStat(StatType.Hunger, maxHunger);
        public float HungerPercentage => currentHunger / Mathf.Max(MaxHunger, 0.001f);

        public float Mood => currentMood;
        public float MaxMood => GetModifiedStat(StatType.Mood, maxMood);
        public float MoodPercentage => currentMood / Mathf.Max(MaxMood, 0.001f);

        public float Money => currentMoney;
        public int Level => currentLevel;
        public float Experience => currentExperience;
        public float ExperienceToNext => experienceToNextLevel;
        public float ExperiencePercentage => experienceToNextLevel > 0 ? currentExperience / experienceToNextLevel : 0f;

        // Дополнительные свойства
        public float HealthRegenRate => GetModifiedStat(StatType.HealthRegen, 0f);
        public float HungerRate => GetModifiedStat(StatType.HungerRate, 1f);
        public float MoodRegenRate => GetModifiedStat(StatType.MoodRegen, 0f);
        public float EarningsMultiplier => GetModifiedStat(StatType.EarningsMultiplier, 1f);
        public float ExperienceMultiplier => GetModifiedStat(StatType.ExperienceMultiplier, 1f);

        public bool IsDead => currentHealth <= 0f;
        public bool IsLowHealth => currentHealth <= lowHealthThreshold;
        public bool IsHighHunger => currentHunger >= highHungerThreshold;

        private void Awake()
        {
            // Получение компонентов системы прогрессии
            skillSystem = GetComponent<SkillSystem>();
            shopSystem = FindObjectOfType<ShopSystem>();
            levelSystem = GetComponent<LevelSystem>();

            // Инициализация модификаторов
            InitializeModifiers();
        }

        private void Start()
        {
            // Инициализация начальных значений
            ValidateStats();

            // Подписка на события
            SubscribeToEvents();

            // Первоначальное обновление модификаторов
            RefreshAllModifiers();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Изменить здоровье
        /// </summary>
        /// <param name="amount">Количество изменения (может быть отрицательным)</param>
        public void ChangeHealth(float amount)
        {
            float oldValue = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            
            if (Math.Abs(oldValue - currentHealth) > 0.01f)
            {
                GameEvents.TriggerStatChanged(StatType.Health, oldValue, currentHealth, maxHealth);
                
                // Проверка критических состояний
                if (IsLowHealth && !IsDead)
                {
                    GameEvents.TriggerLowHealth();
                }
                
                if (IsDead)
                {
                    GameEvents.TriggerPlayerDeath();
                }
            }
        }

        /// <summary>
        /// Изменить голод
        /// </summary>
        /// <param name="amount">Количество изменения (положительное = больше голода)</param>
        public void ChangeHunger(float amount)
        {
            float oldValue = currentHunger;
            currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
            
            if (Math.Abs(oldValue - currentHunger) > 0.01f)
            {
                GameEvents.TriggerStatChanged(StatType.Hunger, oldValue, currentHunger, maxHunger);
                
                // Проверка высокого голода
                if (IsHighHunger)
                {
                    GameEvents.TriggerHighHunger();
                }
            }
        }

        /// <summary>
        /// Изменить настроение
        /// </summary>
        /// <param name="amount">Количество изменения</param>
        public void ChangeMood(float amount)
        {
            float oldValue = currentMood;
            currentMood = Mathf.Clamp(currentMood + amount, 0f, maxMood);
            
            if (Math.Abs(oldValue - currentMood) > 0.01f)
            {
                GameEvents.TriggerStatChanged(StatType.Mood, oldValue, currentMood, maxMood);
            }
        }

        /// <summary>
        /// Изменить количество денег
        /// </summary>
        /// <param name="amount">Количество изменения</param>
        public void ChangeMoney(float amount)
        {
            float oldValue = currentMoney;
            currentMoney = Mathf.Max(currentMoney + amount, 0f); // Деньги не могут быть отрицательными
            
            if (Math.Abs(oldValue - currentMoney) > 0.01f)
            {
                GameEvents.TriggerStatChanged(StatType.Money, oldValue, currentMoney, float.MaxValue);
                
                // Событие заработка денег (только при положительном изменении)
                if (amount > 0)
                {
                    GameEvents.TriggerMoneyEarned(amount);
                }
            }
        }

        /// <summary>
        /// Добавить опыт и проверить повышение уровня
        /// </summary>
        /// <param name="amount">Количество опыта</param>
        public void AddExperience(float amount)
        {
            if (amount <= 0) return;

            float oldValue = currentExperience;
            currentExperience += amount;
            
            GameEvents.TriggerStatChanged(StatType.Experience, oldValue, currentExperience, experienceToNextLevel);

            // Проверка повышения уровня
            CheckLevelUp();
        }

        /// <summary>
        /// Проверить и обработать повышение уровня
        /// </summary>
        private void CheckLevelUp()
        {
            while (currentExperience >= experienceToNextLevel)
            {
                int oldLevel = currentLevel;
                float experienceOverflow = currentExperience - experienceToNextLevel;
                
                currentLevel++;
                currentExperience = experienceOverflow;
                experienceToNextLevel *= experienceMultiplier;
                
                // Бонусы за повышение уровня
                ChangeHealth(maxHealth * 0.1f); // Восстановить 10% здоровья
                ChangeMood(maxMood * 0.2f);     // Улучшить настроение на 20%
                
                GameEvents.TriggerStatChanged(StatType.Level, oldLevel, currentLevel, float.MaxValue);
                GameEvents.TriggerLevelUp(oldLevel, currentLevel, experienceOverflow);
            }
        }

        /// <summary>
        /// Покормить игрока (уменьшить голод)
        /// </summary>
        /// <param name="foodValue">Питательность еды</param>
        public void Feed(float foodValue)
        {
            float hungerReduced = Mathf.Min(currentHunger, foodValue);
            ChangeHunger(-hungerReduced);
            GameEvents.TriggerFoodConsumed(hungerReduced);
        }

        /// <summary>
        /// Дать отдых игроку (улучшить настроение)
        /// </summary>
        /// <param name="restValue">Количество восстановленного настроения</param>
        public void Rest(float restValue)
        {
            float moodBefore = currentMood;
            ChangeMood(restValue);
            float moodRestored = currentMood - moodBefore;
            GameEvents.TriggerRestTaken(moodRestored);
        }

        /// <summary>
        /// Получить модификатор заработка на основе настроения
        /// </summary>
        /// <returns>Множитель заработка (0.5 - 2.0)</returns>
        public float GetEarningModifier()
        {
            float moodPercentage = MoodPercentage;
            return Mathf.Lerp(0.5f, 2.0f, moodPercentage);
        }

        /// <summary>
        /// Получить максимальное значение для указанной характеристики
        /// </summary>
        /// <param name="statType">Тип характеристики</param>
        /// <returns>Максимальное значение</returns>
        public float GetMaxValue(StatType statType)
        {
            switch (statType)
            {
                case StatType.Health:
                    return MaxHealth;
                case StatType.Hunger:
                    return MaxHunger;
                case StatType.Mood:
                    return MaxMood;
                default:
                    return float.MaxValue; // Для характеристик без ограничений
            }
        }

        /// <summary>
        /// Проверить можно ли тратить указанную сумму денег
        /// </summary>
        /// <param name="amount">Сумма для проверки</param>
        /// <returns>True если денег достаточно</returns>
        public bool CanAfford(float amount)
        {
            return currentMoney >= amount;
        }

        /// <summary>
        /// Проверить, можно ли потратить указанную сумму денег
        /// </summary>
        /// <param name="amount">Сумма для проверки</param>
        /// <returns>True если денег достаточно</returns>
        public bool CanSpend(float amount)
        {
            return CanAfford(amount);
        }

        /// <summary>
        /// Сбросить характеристики до начальных значений
        /// </summary>
        public void ResetStats()
        {
            currentHealth = maxHealth;
            currentHunger = 0f;
            currentMood = 75f;
            currentMoney = 0f;
            currentLevel = 1;
            currentExperience = 0f;
            experienceToNextLevel = 100f;
        }

        /// <summary>
        /// Валидация характеристик (проверка корректности значений)
        /// </summary>
        private void ValidateStats()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            currentMood = Mathf.Clamp(currentMood, 0f, maxMood);
            currentMoney = Mathf.Max(currentMoney, 0f);
            currentLevel = Mathf.Max(currentLevel, 1);
            currentExperience = Mathf.Max(currentExperience, 0f);
        }

        #region Modifiers System

        /// <summary>
        /// Инициализация системы модификаторов
        /// </summary>
        private void InitializeModifiers()
        {
            // Инициализация словарей модификаторов
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                statModifiers[statType] = new List<StatModifier>();
                cachedModifiers[statType] = 0f;
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (skillSystem != null)
            {
                skillSystem.OnSkillUpdated += OnSkillUpdated;
            }

            GameEvents.OnItemPurchased += OnItemPurchased;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (skillSystem != null)
            {
                skillSystem.OnSkillUpdated -= OnSkillUpdated;
            }

            GameEvents.OnItemPurchased -= OnItemPurchased;
        }

        /// <summary>
        /// Получить модифицированное значение характеристики
        /// </summary>
        private float GetModifiedStat(StatType statType, float baseValue)
        {
            if (!modifiersCacheValid)
            {
                RefreshModifiersCache();
            }

            float modifier = cachedModifiers.ContainsKey(statType) ? cachedModifiers[statType] : 0f;
            return baseValue + modifier;
        }

        /// <summary>
        /// Добавить модификатор статистики
        /// </summary>
        public void AddStatModifier(StatModifier modifier)
        {
            if (statModifiers.ContainsKey(modifier.statType))
            {
                statModifiers[modifier.statType].Add(modifier);
                modifiersCacheValid = false;
            }
        }

        /// <summary>
        /// Удалить модификатор статистики
        /// </summary>
        public void RemoveStatModifier(StatModifier modifier)
        {
            if (statModifiers.ContainsKey(modifier.statType))
            {
                statModifiers[modifier.statType].Remove(modifier);
                modifiersCacheValid = false;
            }
        }

        /// <summary>
        /// Изменить значение характеристики
        /// </summary>
        /// <param name="statType">Тип характеристики</param>
        /// <param name="amount">Количество изменения</param>
        public void ModifyStat(StatType statType, float amount)
        {
            switch (statType)
            {
                case StatType.Health:
                    ChangeHealth(amount);
                    break;
                case StatType.Hunger:
                    ChangeHunger(amount);
                    break;
                case StatType.Mood:
                    ChangeMood(amount);
                    break;
                case StatType.Money:
                    ChangeMoney(amount);
                    break;
                case StatType.Experience:
                    AddExperience(amount);
                    break;
                default:
                    Debug.LogWarning($"ModifyStat не поддерживает тип характеристики: {statType}");
                    break;
            }
        }

        /// <summary>
        /// Удалить все модификаторы с определенным источником
        /// </summary>
        public void RemoveModifiersBySource(string source)
        {
            bool removed = false;
            foreach (var modifierList in statModifiers.Values)
            {
                for (int i = modifierList.Count - 1; i >= 0; i--)
                {
                    if (modifierList[i].source == source)
                    {
                        modifierList.RemoveAt(i);
                        removed = true;
                    }
                }
            }

            if (removed)
            {
                modifiersCacheValid = false;
            }
        }

        /// <summary>
        /// Обновить кеш модификаторов
        /// </summary>
        private void RefreshModifiersCache()
        {
            foreach (StatType statType in Enum.GetValues(typeof(StatType)))
            {
                float totalModifier = 0f;
                
                if (statModifiers.ContainsKey(statType))
                {
                    foreach (var modifier in statModifiers[statType])
                    {
                        switch (modifier.operation)
                        {
                            case ModifierOperation.Add:
                                totalModifier += modifier.value;
                                break;
                            case ModifierOperation.Multiply:
                                totalModifier *= modifier.value;
                                break;
                            case ModifierOperation.Override:
                                totalModifier = modifier.value;
                                break;
                        }
                    }
                }

                cachedModifiers[statType] = totalModifier;
            }

            modifiersCacheValid = true;
        }

        /// <summary>
        /// Обновить все модификаторы
        /// </summary>
        public void RefreshAllModifiers()
        {
            // Очистить все модификаторы
            foreach (var modifierList in statModifiers.Values)
            {
                modifierList.Clear();
            }

            // Добавить модификаторы от навыков
            if (enableSkillModifiers && skillSystem != null)
            {
                AddSkillModifiers();
            }

            // Добавить модификаторы от предметов
            if (enableItemModifiers && shopSystem != null)
            {
                AddItemModifiers();
            }

            modifiersCacheValid = false;
        }

        /// <summary>
        /// Добавить модификаторы от навыков
        /// </summary>
        private void AddSkillModifiers()
        {
            if (skillSystem == null) return;

            // Модификаторы от харизмы
            float charismaBonus = skillSystem.GetSkillBonus(SkillType.Charisma);
            if (charismaBonus > 0)
            {
                AddStatModifier(new StatModifier(StatType.EarningsMultiplier, charismaBonus, ModifierOperation.Add, "Charisma"));
                AddStatModifier(new StatModifier(StatType.CharismaBonus, charismaBonus, ModifierOperation.Add, "Charisma"));
            }

            // Модификаторы от образования
            float educationBonus = skillSystem.GetSkillBonus(SkillType.Education);
            if (educationBonus > 0)
            {
                AddStatModifier(new StatModifier(StatType.LearningSpeedMultiplier, educationBonus, ModifierOperation.Add, "Education"));
                AddStatModifier(new StatModifier(StatType.ExperienceMultiplier, educationBonus * 0.5f, ModifierOperation.Add, "Education"));
            }

            // Модификаторы от физической формы
            float fitnessBonus = skillSystem.GetSkillBonus(SkillType.Fitness);
            if (fitnessBonus > 0)
            {
                AddStatModifier(new StatModifier(StatType.Health, fitnessBonus * 10f, ModifierOperation.Add, "Fitness"));
                AddStatModifier(new StatModifier(StatType.HealthRegen, fitnessBonus * 0.1f, ModifierOperation.Add, "Fitness"));
                AddStatModifier(new StatModifier(StatType.HungerRate, -fitnessBonus * 0.1f, ModifierOperation.Add, "Fitness"));
            }

            // Модификаторы от удачи
            float luckBonus = skillSystem.GetSkillBonus(SkillType.Luck);
            if (luckBonus > 0)
            {
                AddStatModifier(new StatModifier(StatType.LuckModifier, luckBonus, ModifierOperation.Add, "Luck"));
                AddStatModifier(new StatModifier(StatType.CriticalChance, luckBonus * 0.01f, ModifierOperation.Add, "Luck"));
            }

            // Модификаторы от деловых навыков
            float businessBonus = skillSystem.GetSkillBonus(SkillType.Business);
            if (businessBonus > 0)
            {
                AddStatModifier(new StatModifier(StatType.BusinessSkillBonus, businessBonus, ModifierOperation.Add, "Business"));
                AddStatModifier(new StatModifier(StatType.WorkEfficiencyMultiplier, businessBonus, ModifierOperation.Add, "Business"));
                AddStatModifier(new StatModifier(StatType.NegotiationBonus, businessBonus, ModifierOperation.Add, "Business"));
            }
        }

        /// <summary>
        /// Добавить модификаторы от предметов
        /// </summary>
        private void AddItemModifiers()
        {
            if (shopSystem == null) return;

            var purchasedItems = shopSystem.GetPurchasedItems();
            foreach (var itemId in purchasedItems)
            {
                var item = shopSystem.GetItemById(itemId);
                if (item != null && item.statEffects != null)
                {
                    foreach (var effect in item.statEffects)
                    {
                        AddStatModifier(new StatModifier(effect.Key, effect.Value, ModifierOperation.Add, $"Item_{itemId}"));
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик обновления навыков
        /// </summary>
        private void OnSkillUpdated(SkillType skillType, int newLevel)
        {
            RefreshAllModifiers();
        }

        /// <summary>
        /// Обработчик покупки предмета
        /// </summary>
        private void OnItemPurchased(ShopEventData data)
        {
            RefreshAllModifiers();
        }

        /// <summary>
        /// Получить все активные модификаторы для статистики
        /// </summary>
        public List<StatModifier> GetModifiersForStat(StatType statType)
        {
            return statModifiers.ContainsKey(statType) ? 
                   new List<StatModifier>(statModifiers[statType]) : 
                   new List<StatModifier>();
        }

        /// <summary>
        /// Получить общий модификатор для статистики
        /// </summary>
        public float GetTotalModifierForStat(StatType statType)
        {
            if (!modifiersCacheValid)
            {
                RefreshModifiersCache();
            }

            return cachedModifiers.ContainsKey(statType) ? cachedModifiers[statType] : 0f;
        }

        #endregion

        /// <summary>
        /// Получить данные сохранения
        /// </summary>
        public PlayerStatsData GetSaveData()
        {
            return new PlayerStatsData
            {
                health = currentHealth,
                hunger = currentHunger,
                mood = currentMood,
                money = currentMoney,
                level = currentLevel,
                experience = currentExperience,
                experienceToNext = experienceToNextLevel
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(PlayerStatsData data)
        {
            currentHealth = data.health;
            currentHunger = data.hunger;
            currentMood = data.mood;
            currentMoney = data.money;
            currentLevel = data.level;
            currentExperience = data.experience;
            experienceToNextLevel = data.experienceToNext;
            
            ValidateStats();
        }
    }

    /// <summary>
    /// Данные для сохранения характеристик игрока
    /// </summary>
    [Serializable]
    public class PlayerStatsData
    {
        public float health;
        public float hunger;
        public float mood;
        public float money;
        public int level;
        public float experience;
        public float experienceToNext;
    }

    /// <summary>
    /// Модификатор статистики
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        public StatType statType;           // Тип статистики
        public float value;                 // Значение модификатора
        public ModifierOperation operation; // Операция применения
        public string source;               // Источник модификатора
        public int priority;                // Приоритет применения
        public float duration;              // Длительность действия
        public float startTime;             // Время начала

        public StatModifier(StatType statType, float value, ModifierOperation operation, string source, int priority = 0)
        {
            this.statType = statType;
            this.value = value;
            this.operation = operation;
            this.source = source;
            this.priority = priority;
            this.duration = 0f;
            this.startTime = Time.time;
        }

        public StatModifier(StatType statType, float value, ModifierOperation operation, string source, float duration)
            : this(statType, value, operation, source, 0)
        {
            this.duration = duration;
        }

        public override bool Equals(object obj)
        {
            if (obj is StatModifier other)
            {
                return statType == other.statType && 
                       Mathf.Approximately(value, other.value) && 
                       operation == other.operation && 
                       source == other.source;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return statType.GetHashCode() ^ value.GetHashCode() ^ operation.GetHashCode() ^ source.GetHashCode();
        }
    }

    /// <summary>
    /// Операции применения модификаторов
    /// </summary>
    public enum ModifierOperation
    {
        Add,        // Добавить значение
        Multiply,   // Умножить на значение
        Override    // Переопределить значение
    }
}