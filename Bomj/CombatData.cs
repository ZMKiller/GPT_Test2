using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Данные активного боя
    /// </summary>
    [System.Serializable]
    public class CombatData
    {
        [Header("Основная информация")]
        public CombatEventType eventType;           // Тип боевого события
        public float startTime;                     // Время начала боя
        public float endTime;                       // Время окончания боя
        public float duration;                      // Длительность боя
        
        [Header("Участники")]
        public List<Enemy> enemies;                 // Список врагов
        public float playerStartHealth;             // Начальное здоровье игрока
        public float playerEndHealth;               // Конечное здоровье игрока
        
        [Header("Состояние боя")]
        public bool playerDefending;                // Игрок защищается
        public bool playerDodging;                  // Игрок уклоняется
        public int currentRound;                    // Текущий раунд
        public int totalRounds;                     // Общее количество раундов
        
        [Header("Статистика")]
        public int playerHits;                      // Попадания игрока
        public int playerMisses;                    // Промахи игрока
        public int playerCriticals;                 // Критические удары игрока
        public int enemyHits;                       // Попадания врагов
        public int enemyMisses;                     // Промахи врагов
        public float totalDamageDealt;              // Общий нанесенный урон
        public float totalDamageReceived;           // Общий полученный урон
        
        [Header("Лог боя")]
        public List<string> combatLog;              // Лог действий в бою
        
        [Header("Результаты")]
        public double moneyGained;                  // Полученные деньги
        public double moneyLost;                    // Потерянные деньги
        public float experienceGained;              // Полученный опыт
        public List<string> itemsGained;            // Полученные предметы
        public List<string> itemsLost;              // Потерянные предметы
        
        public CombatData()
        {
            enemies = new List<Enemy>();
            combatLog = new List<string>();
            itemsGained = new List<string>();
            itemsLost = new List<string>();
            currentRound = 1;
        }
        
        /// <summary>
        /// Добавить запись в лог боя
        /// </summary>
        public void AddLogEntry(string message)
        {
            string timestamp = $"[R{currentRound}]";
            combatLog.Add($"{timestamp} {message}");
        }
        
        /// <summary>
        /// Получить сводку боя
        /// </summary>
        public string GetCombatSummary()
        {
            return $"Бой ({eventType}) - {duration:F1}с, " +
                   $"Урон: {totalDamageDealt:F0}/{totalDamageReceived:F0}, " +
                   $"Попадания: {playerHits}/{playerHits + playerMisses}";
        }
    }
    
    /// <summary>
    /// Информация о враге
    /// </summary>
    [System.Serializable]
    public class Enemy
    {
        [Header("Основная информация")]
        public EnemyType enemyType;                 // Тип врага
        public string customName;                   // Пользовательское имя
        public int level;                           // Уровень врага
        public float difficulty;                    // Сложность (0-1)
        
        [Header("Характеристики")]
        public float maxHealth;                     // Максимальное здоровье
        public float currentHealth;                 // Текущее здоровье
        public float minDamage;                     // Минимальный урон
        public float maxDamage;                     // Максимальный урон
        public float accuracy;                      // Точность (0-1)
        public float criticalChance;                // Шанс критического удара (0-1)
        public float blockChance;                   // Шанс блока (0-1)
        public float dodgeChance;                   // Шанс уклонения (0-1)
        
        [Header("AI и поведение")]
        public CombatAI aiType;                     // Тип AI
        public float aggressiveness;                // Агрессивность (0-1)
        public float cowardice;                     // Трусость (0-1)
        public bool canFlee;                        // Может ли сбежать
        public bool canNegotiate;                   // Можно ли договориться
        
        [Header("Награды")]
        public double moneyReward;                  // Награда деньгами
        public float experienceReward;              // Награда опытом
        public List<string> itemRewards;            // Возможные предметы
        public float itemDropChance;                // Шанс выпадения предметов
        
        [Header("Оружие и снаряжение")]
        public WeaponType weaponType;               // Тип оружия
        public WeaponCondition weaponCondition;     // Состояние оружия
        public bool hasArmor;                       // Есть ли броня
        public float armorValue;                    // Значение брони
        
        public Enemy()
        {
            itemRewards = new List<string>();
            level = 1;
            difficulty = 0.5f;
            currentHealth = maxHealth;
        }
        
        /// <summary>
        /// Получить отображаемое имя
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(customName))
                return customName;
                
            return GetEnemyTypeName(enemyType);
        }
        
        /// <summary>
        /// Получить название типа врага
        /// </summary>
        private string GetEnemyTypeName(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Thug: return "Хулиган";
                case EnemyType.Robber: return "Грабитель";
                case EnemyType.Bandit: return "Бандит";
                case EnemyType.DrunkPerson: return "Пьяный";
                case EnemyType.StreetFighter: return "Уличный боец";
                case EnemyType.Police: return "Полицейский";
                case EnemyType.SecurityGuard: return "Охранник";
                case EnemyType.Gang_Member: return "Член банды";
                case EnemyType.Dealer: return "Торговец";
                case EnemyType.Bully: return "Задира";
                default: return type.ToString();
            }
        }
        
        /// <summary>
        /// Получить описание врага
        /// </summary>
        public string GetDescription()
        {
            return $"{GetDisplayName()} (Ур.{level}) - {currentHealth:F0}/{maxHealth:F0} HP, " +
                   $"Урон: {minDamage:F0}-{maxDamage:F0}";
        }
        
        /// <summary>
        /// Проверить жив ли враг
        /// </summary>
        public bool IsAlive()
        {
            return currentHealth > 0;
        }
        
        /// <summary>
        /// Получить процент здоровья
        /// </summary>
        public float GetHealthPercentage()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }
        
        /// <summary>
        /// Применить урон
        /// </summary>
        public float TakeDamage(float damage)
        {
            float actualDamage = damage;
            
            // Учесть броню
            if (hasArmor)
            {
                actualDamage *= (1f - armorValue);
            }
            
            float oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - actualDamage);
            
            return oldHealth - currentHealth; // Фактический урон
        }
        
        /// <summary>
        /// Восстановить здоровье
        /// </summary>
        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        
        /// <summary>
        /// Создать тестового врага
        /// </summary>
        public static Enemy CreateTestEnemy(EnemyType type)
        {
            Enemy enemy = new Enemy();
            enemy.enemyType = type;
            
            switch (type)
            {
                case EnemyType.Thug:
                    enemy.level = 1;
                    enemy.maxHealth = 40f;
                    enemy.minDamage = 5f;
                    enemy.maxDamage = 12f;
                    enemy.accuracy = 0.7f;
                    enemy.criticalChance = 0.05f;
                    enemy.blockChance = 0.1f;
                    enemy.dodgeChance = 0.15f;
                    enemy.aiType = CombatAI.Aggressive;
                    enemy.difficulty = 0.3f;
                    enemy.moneyReward = 10;
                    enemy.experienceReward = 5f;
                    enemy.weaponType = WeaponType.None;
                    break;
                    
                case EnemyType.Robber:
                    enemy.level = 2;
                    enemy.maxHealth = 50f;
                    enemy.minDamage = 8f;
                    enemy.maxDamage = 15f;
                    enemy.accuracy = 0.75f;
                    enemy.criticalChance = 0.1f;
                    enemy.blockChance = 0.05f;
                    enemy.dodgeChance = 0.2f;
                    enemy.aiType = CombatAI.Tactical;
                    enemy.difficulty = 0.5f;
                    enemy.moneyReward = 25;
                    enemy.experienceReward = 8f;
                    enemy.weaponType = WeaponType.Knife;
                    enemy.canNegotiate = true;
                    break;
                    
                case EnemyType.Bandit:
                    enemy.level = 3;
                    enemy.maxHealth = 70f;
                    enemy.minDamage = 12f;
                    enemy.maxDamage = 20f;
                    enemy.accuracy = 0.8f;
                    enemy.criticalChance = 0.15f;
                    enemy.blockChance = 0.15f;
                    enemy.dodgeChance = 0.1f;
                    enemy.aiType = CombatAI.Balanced;
                    enemy.difficulty = 0.7f;
                    enemy.moneyReward = 50;
                    enemy.experienceReward = 12f;
                    enemy.weaponType = WeaponType.Bat;
                    break;
                    
                case EnemyType.DrunkPerson:
                    enemy.level = 1;
                    enemy.maxHealth = 35f;
                    enemy.minDamage = 3f;
                    enemy.maxDamage = 8f;
                    enemy.accuracy = 0.5f;
                    enemy.criticalChance = 0.02f;
                    enemy.blockChance = 0.05f;
                    enemy.dodgeChance = 0.05f;
                    enemy.aiType = CombatAI.Cowardly;
                    enemy.difficulty = 0.2f;
                    enemy.moneyReward = 5;
                    enemy.experienceReward = 2f;
                    enemy.weaponType = WeaponType.BrokenBottle;
                    enemy.canFlee = true;
                    break;
                    
                case EnemyType.StreetFighter:
                    enemy.level = 4;
                    enemy.maxHealth = 80f;
                    enemy.minDamage = 15f;
                    enemy.maxDamage = 25f;
                    enemy.accuracy = 0.85f;
                    enemy.criticalChance = 0.2f;
                    enemy.blockChance = 0.25f;
                    enemy.dodgeChance = 0.3f;
                    enemy.aiType = CombatAI.Berserker;
                    enemy.difficulty = 0.8f;
                    enemy.moneyReward = 75;
                    enemy.experienceReward = 20f;
                    enemy.weaponType = WeaponType.None;
                    break;
                    
                case EnemyType.Police:
                    enemy.level = 5;
                    enemy.maxHealth = 100f;
                    enemy.minDamage = 20f;
                    enemy.maxDamage = 35f;
                    enemy.accuracy = 0.9f;
                    enemy.criticalChance = 0.1f;
                    enemy.blockChance = 0.2f;
                    enemy.dodgeChance = 0.15f;
                    enemy.aiType = CombatAI.Police;
                    enemy.difficulty = 1.0f;
                    enemy.moneyReward = 0; // Нет денег за полицейского
                    enemy.experienceReward = 5f; // Мало опыта
                    enemy.weaponType = WeaponType.Pistol;
                    enemy.hasArmor = true;
                    enemy.armorValue = 0.3f;
                    enemy.canNegotiate = true;
                    break;
                    
                case EnemyType.SecurityGuard:
                    enemy.level = 3;
                    enemy.maxHealth = 65f;
                    enemy.minDamage = 10f;
                    enemy.maxDamage = 18f;
                    enemy.accuracy = 0.75f;
                    enemy.criticalChance = 0.08f;
                    enemy.blockChance = 0.2f;
                    enemy.dodgeChance = 0.1f;
                    enemy.aiType = CombatAI.Defensive;
                    enemy.difficulty = 0.6f;
                    enemy.moneyReward = 30;
                    enemy.experienceReward = 10f;
                    enemy.weaponType = WeaponType.Club;
                    break;
                    
                case EnemyType.Gang_Member:
                    enemy.level = 4;
                    enemy.maxHealth = 75f;
                    enemy.minDamage = 12f;
                    enemy.maxDamage = 22f;
                    enemy.accuracy = 0.8f;
                    enemy.criticalChance = 0.12f;
                    enemy.blockChance = 0.1f;
                    enemy.dodgeChance = 0.2f;
                    enemy.aiType = CombatAI.Aggressive;
                    enemy.difficulty = 0.75f;
                    enemy.moneyReward = 60;
                    enemy.experienceReward = 15f;
                    enemy.weaponType = WeaponType.Chain;
                    break;
                    
                case EnemyType.Dealer:
                    enemy.level = 2;
                    enemy.maxHealth = 45f;
                    enemy.minDamage = 6f;
                    enemy.maxDamage = 12f;
                    enemy.accuracy = 0.7f;
                    enemy.criticalChance = 0.05f;
                    enemy.blockChance = 0.05f;
                    enemy.dodgeChance = 0.25f;
                    enemy.aiType = CombatAI.Cowardly;
                    enemy.difficulty = 0.4f;
                    enemy.moneyReward = 40; // У дилеров много денег
                    enemy.experienceReward = 6f;
                    enemy.weaponType = WeaponType.Knife;
                    enemy.canFlee = true;
                    enemy.canNegotiate = true;
                    break;
                    
                case EnemyType.Bully:
                    enemy.level = 2;
                    enemy.maxHealth = 55f;
                    enemy.minDamage = 8f;
                    enemy.maxDamage = 14f;
                    enemy.accuracy = 0.75f;
                    enemy.criticalChance = 0.08f;
                    enemy.blockChance = 0.15f;
                    enemy.dodgeChance = 0.1f;
                    enemy.aiType = CombatAI.Balanced;
                    enemy.difficulty = 0.45f;
                    enemy.moneyReward = 20;
                    enemy.experienceReward = 7f;
                    enemy.weaponType = WeaponType.None;
                    break;
            }
            
            enemy.currentHealth = enemy.maxHealth;
            return enemy;
        }
        
        /// <summary>
        /// Создать случайного врага определенного типа
        /// </summary>
        public static Enemy CreateRandomEnemy(EnemyType type, int minLevel = 1, int maxLevel = 5)
        {
            Enemy enemy = CreateTestEnemy(type);
            
            // Случайный уровень
            enemy.level = UnityEngine.Random.Range(minLevel, maxLevel + 1);
            
            // Масштабирование характеристик по уровню
            float levelMultiplier = 1f + (enemy.level - 1) * 0.2f;
            enemy.maxHealth *= levelMultiplier;
            enemy.minDamage *= levelMultiplier;
            enemy.maxDamage *= levelMultiplier;
            enemy.moneyReward *= enemy.level;
            enemy.experienceReward *= enemy.level;
            
            // Случайные вариации ±10%
            float variation = UnityEngine.Random.Range(0.9f, 1.1f);
            enemy.maxHealth *= variation;
            enemy.minDamage *= variation;
            enemy.maxDamage *= variation;
            
            enemy.currentHealth = enemy.maxHealth;
            
            return enemy;
        }
    }
    
    /// <summary>
    /// Результат боевого действия с подробностями
    /// </summary>
    [System.Serializable]
    public class CombatActionResult
    {
        public CombatAction action;         // Выполненное действие
        public CombatResult result;         // Результат действия
        public float damage;                // Нанесенный урон
        public bool isCritical;             // Критический удар
        public string description;          // Описание результата
        public string actorName;            // Имя выполнившего действие
        public string targetName;           // Имя цели
        
        public CombatActionResult(CombatAction act, CombatResult res, string desc)
        {
            action = act;
            result = res;
            description = desc;
        }
    }
}
