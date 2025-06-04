using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Данные события изменения характеристики
    /// </summary>
    [Serializable]
    public class StatChangedEventData
    {
        public StatType statType;           // Тип характеристики
        public float oldValue;              // Старое значение
        public float newValue;              // Новое значение
        public float maxValue;              // Максимальное значение

        public StatChangedEventData(StatType type, float oldVal, float newVal, float maxVal)
        {
            statType = type;
            oldValue = oldVal;
            newValue = newVal;
            maxValue = maxVal;
        }
    }

    /// <summary>
    /// Данные события повышения уровня
    /// </summary>
    [Serializable]
    public class LevelUpEventData
    {
        public int oldLevel;                // Старый уровень
        public int newLevel;                // Новый уровень
        public float experienceOverflow;    // Избыток опыта

        public LevelUpEventData(int oldLvl, int newLvl, float expOverflow)
        {
            oldLevel = oldLvl;
            newLevel = newLvl;
            experienceOverflow = expOverflow;
        }
    }

    /// <summary>
    /// Данные события взаимодействия с NPC
    /// </summary>
    [Serializable]
    public class NPCInteractionEventData
    {
        public NPCType npcType;             // Тип NPC
        public float moneyAmount;           // Сумма денег
        public Vector3 worldPosition;       // Позиция взаимодействия
        public float playerMoodModifier;    // Модификатор настроения игрока
        public float playerLevelModifier;   // Модификатор уровня игрока

        public NPCInteractionEventData(NPCType type, float money, Vector3 position, float moodMod, float levelMod)
        {
            npcType = type;
            moneyAmount = money;
            worldPosition = position;
            playerMoodModifier = moodMod;
            playerLevelModifier = levelMod;
        }
    }

    /// <summary>
    /// Данные события заработка денег
    /// </summary>
    [Serializable]
    public class MoneyEventData
    {
        public double amount;               // Количество денег
        public string source;               // Источник дохода
        public DateTime timestamp;          // Время события

        public MoneyEventData(double amount, string source)
        {
            this.amount = amount;
            this.source = source;
            this.timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Данные события повышения уровня (обновленная версия)
    /// </summary>
    [Serializable]
    public class LevelUpData
    {
        public int oldLevel;                // Старый уровень
        public int newLevel;                // Новый уровень
        public float experienceOverflow;    // Избыток опыта
        public int skillPointsGained;       // Полученные очки навыков
        public double moneyBonus;           // Денежный бонус
        public List<string> unlockedFeatures; // Разблокированные функции

        public LevelUpData(int oldLevel, int newLevel, float expOverflow, int skillPoints, double money)
        {
            this.oldLevel = oldLevel;
            this.newLevel = newLevel;
            this.experienceOverflow = expOverflow;
            this.skillPointsGained = skillPoints;
            this.moneyBonus = money;
            this.unlockedFeatures = new List<string>();
        }
    }

    /// <summary>
    /// Данные события улучшения навыка
    /// </summary>
    [Serializable]
    public class SkillUpgradeEventData
    {
        public SkillType skillType;         // Тип навыка
        public int oldLevel;                // Старый уровень
        public int newLevel;                // Новый уровень
        public int pointsSpent;             // Потраченные очки

        public SkillUpgradeEventData(SkillType skill, int oldLevel, int newLevel, int points)
        {
            this.skillType = skill;
            this.oldLevel = oldLevel;
            this.newLevel = newLevel;
            this.pointsSpent = points;
        }
    }

    /// <summary>
    /// Данные события завершения работы
    /// </summary>
    [Serializable]
    public class JobEventData
    {
        public JobType jobType;             // Тип работы
        public double salary;               // Зарплата
        public double payment;              // Итоговая выплата
        public int hoursWorked;             // Отработанные часы
        public int experienceGained;        // Полученный опыт
        public bool wasSuccessful;          // Успешно ли завершена
        public TimeSpan workDuration;       // Продолжительность работы

        public JobEventData(JobType job, double salary, int exp, bool success, TimeSpan duration)
        {
            this.jobType = job;
            this.salary = salary;
            this.experienceGained = exp;
            this.wasSuccessful = success;
            this.workDuration = duration;
            this.payment = salary;
            this.hoursWorked = (int)duration.TotalHours;
        }
    }

    /// <summary>
    /// Данные события завершения образования
    /// </summary>
    [Serializable]
    public class EducationEventData
    {
        public EducationType educationType; // Тип образования
        public double cost;                 // Стоимость
        public Dictionary<SkillType, int> skillsImproved; // Улучшенные навыки
        public int experienceGained;        // Полученный опыт
        public bool certificateEarned;      // Получен ли сертификат

        public EducationEventData(EducationType education, double cost, int exp, bool certificate)
        {
            this.educationType = education;
            this.cost = cost;
            this.experienceGained = exp;
            this.certificateEarned = certificate;
            this.skillsImproved = new Dictionary<SkillType, int>();
        }
    }

    /// <summary>
    /// Данные события покупки в магазине
    /// </summary>
    [Serializable]
    public class ShopEventData
    {
        public string itemId;               // ID товара
        public string itemName;             // Название товара
        public ShopCategory category;       // Категория
        public ItemQuality quality;         // Качество
        public double price;                // Цена
        public Dictionary<StatType, float> statEffects; // Эффекты на характеристики

        public ShopEventData(string id, string name, ShopCategory cat, ItemQuality qual, double price)
        {
            this.itemId = id;
            this.itemName = name;
            this.category = cat;
            this.quality = qual;
            this.price = price;
            this.statEffects = new Dictionary<StatType, float>();
        }
    }

    /// <summary>
    /// Система событий игры
    /// </summary>
    public static class GameEvents
    {
        // События характеристик
        public static event Action<StatChangedEventData> OnStatChanged;
        // Deprecated: old level-up event using simple data
        // public static event Action<LevelUpEventData> OnLevelUp;
        
        // События состояния здоровья
        public static event Action OnLowHealth;
        public static event Action OnHighHunger;
        public static event Action OnPlayerDeath;
        
        // События игрового процесса
        // Deprecated: old money earned event
        // public static event Action<float> OnMoneyEarned;    // float - количество заработанных денег
        public static event Action<float> OnFoodConsumed;   // float - количество восстановленного голода
        public static event Action<float> OnRestTaken;      // float - количество восстановленного настроения
        
        // События NPC системы
        public static event Action<NPCInteractionEventData> OnNPCInteraction;  // Взаимодействие с NPC
        public static event Action<NPCType> OnNPCSpawned;                      // Спавн NPC
        public static event Action<NPCType> OnNPCDespawned;                    // Удаление NPC
        public static event Action<TimeOfDay> OnTimeOfDayChanged;              // Смена времени дня
        
        // События локаций
        public static event Action<LocationType, LocationData> OnLocationChanged;     // Смена локации
        public static event Action<LocationType> OnLocationUnlocked;                  // Разблокировка локации
        public static event Action<LocationType, LocationType> OnLocationTransitionStarted; // Начало перехода
        public static event Action<LocationType> OnLocationTransitionCompleted;       // Завершение перехода
        
        // События уведомлений
        public static event Action<string, NotificationType, NotificationPriority> OnNotificationRequested;

        // События системы прогрессии
        public static event Action<LevelUpData> OnLevelUp; // Обновленная версия для новой системы
        public static event Action<SkillUpgradeEventData> OnSkillUpgraded;
        public static event Action<JobEventData> OnJobCompleted;
        public static event Action<EducationEventData> OnEducationCompleted;
        public static event Action<ShopEventData> OnItemPurchased;
        public static event Action<int> OnPrestige;

        // События денежной системы (обновленные)
        public static event Action<MoneyEventData> OnMoneyEarned; // Обновленная версия

        // События системы времени
        public static event Action<DateTime> OnTimeChanged;                        // Изменение времени
        public static event Action<TimePeriod, TimePeriod> OnTimePeriodChanged;   // Смена периода дня
        public static event Action<Season> OnSeasonChanged;                       // Смена сезона
        public static event Action<int> OnDayChanged;                             // Смена дня
        public static event Action<int> OnHourChanged;                            // Смена часа
        public static event Action<TimeSpeed> OnTimeSpeedChanged;                 // Изменение скорости времени
        
        // События погоды
        public static event Action<WeatherType, WeatherIntensity> OnWeatherChanged; // Смена погоды
        public static event Action<WeatherType> OnWeatherStarted;                   // Начало погоды
        public static event Action<WeatherType> OnWeatherStopped;                   // Конец погоды
        
        // События сна
        public static event Action<float> OnFatigueChanged;                       // Изменение усталости
        public static event Action<FatigueLevel> OnFatigueLevelChanged;           // Смена уровня усталости
        public static event Action<SleepLocation, float> OnSleepStarted;          // Начало сна
        public static event Action<SleepLocation, float> OnSleepCompleted;        // Завершение сна
        
        // События времени дня и геймплея
        public static event Action<TimePeriod, TimeBasedModifiers> OnTimeBasedModifiersChanged; // Смена модификаторов времени
        public static event Action<Location, bool> OnLocationAvailabilityChanged;  // Изменение доступности локации
        public static event Action<float> OnPriceMultiplierChanged;               // Изменение множителя цен
        
        // События временных событий
        public static event Action<TimeBasedEventData> OnTimeEventStarted;        // Начало временного события
        public static event Action<TimeBasedEventData, bool> OnTimeEventCompleted; // Завершение временного события
        public static event Action<TimeBasedEventData> OnTimeEventExpired;        // Истечение временного события

        /// <summary>
        /// Вызвать событие изменения характеристики
        /// </summary>
        public static void TriggerStatChanged(StatType statType, float oldValue, float newValue, float maxValue)
        {
            OnStatChanged?.Invoke(new StatChangedEventData(statType, oldValue, newValue, maxValue));
        }

        // Legacy overload kept for backward compatibility
        public static void TriggerLevelUp(int oldLevel, int newLevel, float experienceOverflow)
        {
            OnLevelUp?.Invoke(new LevelUpData(oldLevel, newLevel, experienceOverflow, 0, 0));
        }

        /// <summary>
        /// Вызвать событие низкого здоровья
        /// </summary>
        public static void TriggerLowHealth()
        {
            OnLowHealth?.Invoke();
        }

        /// <summary>
        /// Вызвать событие высокого голода
        /// </summary>
        public static void TriggerHighHunger()
        {
            OnHighHunger?.Invoke();
        }

        /// <summary>
        /// Вызвать событие смерти игрока
        /// </summary>
        public static void TriggerPlayerDeath()
        {
            OnPlayerDeath?.Invoke();
        }

        // Legacy overload kept for backward compatibility
        public static void TriggerMoneyEarned(float amount)
        {
            OnMoneyEarned?.Invoke(new MoneyEventData(amount, "legacy"));
        }

        /// <summary>
        /// Вызвать событие потребления еды
        /// </summary>
        public static void TriggerFoodConsumed(float hungerRestored)
        {
            OnFoodConsumed?.Invoke(hungerRestored);
        }

        /// <summary>
        /// Вызвать событие отдыха
        /// </summary>
        public static void TriggerRestTaken(float moodRestored)
        {
            OnRestTaken?.Invoke(moodRestored);
        }

        /// <summary>
        /// Вызвать событие взаимодействия с NPC
        /// </summary>
        public static void TriggerNPCInteraction(NPCType npcType, float moneyAmount, Vector3 worldPosition, 
                                                float playerMoodModifier, float playerLevelModifier)
        {
            OnNPCInteraction?.Invoke(new NPCInteractionEventData(npcType, moneyAmount, worldPosition, 
                                                                playerMoodModifier, playerLevelModifier));
        }

        /// <summary>
        /// Вызвать событие спавна NPC
        /// </summary>
        public static void TriggerNPCSpawned(NPCType npcType)
        {
            OnNPCSpawned?.Invoke(npcType);
        }

        /// <summary>
        /// Вызвать событие удаления NPC
        /// </summary>
        public static void TriggerNPCDespawned(NPCType npcType)
        {
            OnNPCDespawned?.Invoke(npcType);
        }

        /// <summary>
        /// Вызвать событие смены времени дня
        /// </summary>
        public static void TriggerTimeOfDayChanged(TimeOfDay newTimeOfDay)
        {
            OnTimeOfDayChanged?.Invoke(newTimeOfDay);
        }

        /// <summary>
        /// Вызвать событие смены локации
        /// </summary>
        public static void TriggerLocationChanged(LocationType newLocationType, LocationData locationData)
        {
            OnLocationChanged?.Invoke(newLocationType, locationData);
        }

        /// <summary>
        /// Вызвать событие разблокировки локации
        /// </summary>
        public static void TriggerLocationUnlocked(LocationType locationType)
        {
            OnLocationUnlocked?.Invoke(locationType);
        }

        /// <summary>
        /// Вызвать событие начала перехода между локациями
        /// </summary>
        public static void TriggerLocationTransitionStarted(LocationType fromLocation, LocationType toLocation)
        {
            OnLocationTransitionStarted?.Invoke(fromLocation, toLocation);
        }

        /// <summary>
        /// Вызвать событие завершения перехода между локациями
        /// </summary>
        public static void TriggerLocationTransitionCompleted(LocationType locationType)
        {
            OnLocationTransitionCompleted?.Invoke(locationType);
        }

        /// <summary>
        /// Вызвать запрос на показ уведомления
        /// </summary>
        public static void TriggerNotification(string message, NotificationType type = NotificationType.Info, 
                                              NotificationPriority priority = NotificationPriority.Normal)
        {
            OnNotificationRequested?.Invoke(message, type, priority);
        }

        /// <summary>
        /// Вызвать событие повышения уровня (новая версия)
        /// </summary>
        public static void TriggerLevelUp(LevelUpData levelUpData)
        {
            OnLevelUp?.Invoke(levelUpData);
        }

        /// <summary>
        /// Вызвать событие улучшения навыка
        /// </summary>
        public static void TriggerSkillUpgrade(SkillUpgradeEventData skillUpgradeData)
        {
            OnSkillUpgraded?.Invoke(skillUpgradeData);
        }

        /// <summary>
        /// Вызвать событие завершения работы
        /// </summary>
        public static void TriggerJobCompleted(JobEventData jobData)
        {
            OnJobCompleted?.Invoke(jobData);
        }

        /// <summary>
        /// Вызвать событие завершения образования
        /// </summary>
        public static void TriggerEducationCompleted(EducationEventData educationData)
        {
            OnEducationCompleted?.Invoke(educationData);
        }

        /// <summary>
        /// Вызвать событие покупки товара
        /// </summary>
        public static void TriggerItemPurchased(ShopEventData shopData)
        {
            OnItemPurchased?.Invoke(shopData);
        }

        /// <summary>
        /// Вызвать событие престижа
        /// </summary>
        public static void TriggerPrestige(int prestigeLevel)
        {
            OnPrestige?.Invoke(prestigeLevel);
        }

        /// <summary>
        /// Вызвать событие заработка денег (новая версия)
        /// </summary>
        public static void TriggerMoneyEarned(MoneyEventData moneyData)
        {
            OnMoneyEarned?.Invoke(moneyData);
        }

        // Методы для событий системы времени

        /// <summary>
        /// Вызвать событие изменения времени
        /// </summary>
        public static void TriggerTimeChanged(DateTime newTime)
        {
            OnTimeChanged?.Invoke(newTime);
        }

        /// <summary>
        /// Вызвать событие смены периода дня
        /// </summary>
        public static void TriggerTimePeriodChanged(TimePeriod oldPeriod, TimePeriod newPeriod)
        {
            OnTimePeriodChanged?.Invoke(oldPeriod, newPeriod);
        }

        /// <summary>
        /// Вызвать событие смены сезона
        /// </summary>
        public static void TriggerSeasonChanged(Season newSeason)
        {
            OnSeasonChanged?.Invoke(newSeason);
        }

        /// <summary>
        /// Вызвать событие смены дня
        /// </summary>
        public static void TriggerDayChanged(int newDay)
        {
            OnDayChanged?.Invoke(newDay);
        }

        /// <summary>
        /// Вызвать событие смены часа
        /// </summary>
        public static void TriggerHourChanged(int newHour)
        {
            OnHourChanged?.Invoke(newHour);
        }

        /// <summary>
        /// Вызвать событие изменения скорости времени
        /// </summary>
        public static void TriggerTimeSpeedChanged(TimeSpeed newSpeed)
        {
            OnTimeSpeedChanged?.Invoke(newSpeed);
        }

        /// <summary>
        /// Вызвать событие смены погоды
        /// </summary>
        public static void TriggerWeatherChanged(WeatherType weather, WeatherIntensity intensity)
        {
            OnWeatherChanged?.Invoke(weather, intensity);
        }

        /// <summary>
        /// Вызвать событие начала погоды
        /// </summary>
        public static void TriggerWeatherStarted(WeatherType weather)
        {
            OnWeatherStarted?.Invoke(weather);
        }

        /// <summary>
        /// Вызвать событие окончания погоды
        /// </summary>
        public static void TriggerWeatherStopped(WeatherType weather)
        {
            OnWeatherStopped?.Invoke(weather);
        }

        /// <summary>
        /// Вызвать событие изменения усталости
        /// </summary>
        public static void TriggerFatigueChanged(float newFatigue)
        {
            OnFatigueChanged?.Invoke(newFatigue);
        }

        /// <summary>
        /// Вызвать событие смены уровня усталости
        /// </summary>
        public static void TriggerFatigueLevelChanged(FatigueLevel newLevel)
        {
            OnFatigueLevelChanged?.Invoke(newLevel);
        }

        /// <summary>
        /// Вызвать событие начала сна
        /// </summary>
        public static void TriggerSleepStarted(SleepLocation location, float hours)
        {
            OnSleepStarted?.Invoke(location, hours);
        }

        /// <summary>
        /// Вызвать событие завершения сна
        /// </summary>
        public static void TriggerSleepCompleted(SleepLocation location, float hours)
        {
            OnSleepCompleted?.Invoke(location, hours);
        }

        /// <summary>
        /// Вызвать событие смены модификаторов времени
        /// </summary>
        public static void TriggerTimeBasedModifiersChanged(TimePeriod period, TimeBasedModifiers modifiers)
        {
            OnTimeBasedModifiersChanged?.Invoke(period, modifiers);
        }

        /// <summary>
        /// Вызвать событие изменения доступности локации
        /// </summary>
        public static void TriggerLocationAvailabilityChanged(Location location, bool available)
        {
            OnLocationAvailabilityChanged?.Invoke(location, available);
        }

        /// <summary>
        /// Вызвать событие изменения множителя цен
        /// </summary>
        public static void TriggerPriceMultiplierChanged(float multiplier)
        {
            OnPriceMultiplierChanged?.Invoke(multiplier);
        }

        /// <summary>
        /// Вызвать событие начала временного события
        /// </summary>
        public static void TriggerTimeEventStarted(TimeBasedEventData eventData)
        {
            OnTimeEventStarted?.Invoke(eventData);
        }

        /// <summary>
        /// Вызвать событие завершения временного события
        /// </summary>
        public static void TriggerTimeEventCompleted(TimeBasedEventData eventData, bool success)
        {
            OnTimeEventCompleted?.Invoke(eventData, success);
        }

        /// <summary>
        /// Вызвать событие истечения временного события
        /// </summary>
        public static void TriggerTimeEventExpired(TimeBasedEventData eventData)
        {
            OnTimeEventExpired?.Invoke(eventData);
        }

        /// <summary>
        /// Очистить все подписки на события (используется при выходе из игры)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnStatChanged = null;
            OnLevelUp = null;
            OnLowHealth = null;
            OnHighHunger = null;
            OnPlayerDeath = null;
            OnMoneyEarned = null;
            OnFoodConsumed = null;
            OnRestTaken = null;
            OnNPCInteraction = null;
            OnNPCSpawned = null;
            OnNPCDespawned = null;
            OnTimeOfDayChanged = null;
            OnLocationChanged = null;
            OnLocationUnlocked = null;
            OnLocationTransitionStarted = null;
            OnLocationTransitionCompleted = null;
            OnNotificationRequested = null;
            OnSkillUpgraded = null;
            OnJobCompleted = null;
            OnEducationCompleted = null;
            OnItemPurchased = null;
            OnPrestige = null;

            // Очистка событий системы времени
            OnTimeChanged = null;
            OnTimePeriodChanged = null;
            OnSeasonChanged = null;
            OnDayChanged = null;
            OnHourChanged = null;
            OnTimeSpeedChanged = null;
            OnWeatherChanged = null;
            OnWeatherStarted = null;
            OnWeatherStopped = null;
            OnFatigueChanged = null;
            OnFatigueLevelChanged = null;
            OnSleepStarted = null;
            OnSleepCompleted = null;
            OnTimeBasedModifiersChanged = null;
            OnLocationAvailabilityChanged = null;
            OnPriceMultiplierChanged = null;
            OnTimeEventStarted = null;
            OnTimeEventCompleted = null;
            OnTimeEventExpired = null;
        }
    }
}