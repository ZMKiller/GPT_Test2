using UnityEngine;
using System.Collections.Generic;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Модификатор характеристик для локации
    /// </summary>
    [System.Serializable]
    public struct LocationStatModifier
    {
        public StatType statType;       // Тип характеристики
        public float modifier;          // Множитель (1.0 = без изменений, 1.5 = +50%, 0.8 = -20%)
        public bool isPercentage;       // Применяется как процент или абсолютное значение
        public string description;      // Описание эффекта

        public LocationStatModifier(StatType type, float mod, bool percentage = true, string desc = "")
        {
            statType = type;
            modifier = mod;
            isPercentage = percentage;
            description = desc;
        }
    }

    /// <summary>
    /// Требования для доступа к локации
    /// </summary>
    [System.Serializable]
    public struct LocationRequirement
    {
        public int minLevel;            // Минимальный уровень
        public float minMoney;          // Минимальное количество денег
        public TimeOfDay[] allowedTimes; // Разрешенное время дня
        public LocationType[] requiredLocations; // Требуемые открытые локации
        public string requirement;      // Описание требования

        public bool IsMet(PlayerStats stats, TimeOfDay currentTime, HashSet<LocationType> unlockedLocations)
        {
            // Проверка уровня
            if (stats.Level < minLevel) return false;

            // Проверка денег
            if (stats.Money < minMoney) return false;

            // Проверка времени дня
            if (allowedTimes != null && allowedTimes.Length > 0)
            {
                bool timeAllowed = false;
                foreach (var time in allowedTimes)
                {
                    if (time == currentTime)
                    {
                        timeAllowed = true;
                        break;
                    }
                }
                if (!timeAllowed) return false;
            }

            // Проверка требуемых локаций
            if (requiredLocations != null && requiredLocations.Length > 0)
            {
                foreach (var requiredLocation in requiredLocations)
                {
                    if (!unlockedLocations.Contains(requiredLocation))
                        return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Данные о локации - ScriptableObject для настройки локаций
    /// </summary>
    [CreateAssetMenu(fileName = "New Location", menuName = "Homeless To Millionaire/Location Data")]
    public class LocationData : ScriptableObject
    {
        [Header("Основная информация")]
        [Tooltip("Тип локации")]
        public LocationType locationType;

        [Tooltip("Название локации")]
        public string locationName = "Новая локация";

        [Tooltip("Описание локации")]
        [TextArea(3, 5)]
        public string description = "Описание локации";

        [Tooltip("Иконка локации")]
        public Sprite locationIcon;

        [Tooltip("Фоновое изображение локации")]
        public Sprite backgroundImage;

        [Header("Игровые характеристики")]
        [Tooltip("Модификаторы характеристик в этой локации")]
        public LocationStatModifier[] statModifiers;

        [Tooltip("Модификатор заработка денег (1.0 = без изменений)")]
        [Range(0.1f, 5.0f)]
        public float moneyModifier = 1.0f;

        [Tooltip("Модификатор скорости деградации (0.5 = медленнее в 2 раза)")]
        [Range(0.1f, 3.0f)]
        public float degradationModifier = 1.0f;

        [Header("NPC настройки")]
        [Tooltip("Типы NPC, которые появляются в этой локации")]
        public NPCType[] availableNPCTypes;

        [Tooltip("Модификаторы для типов NPC (шанс появления)")]
        public NPCSpawnModifier[] npcSpawnModifiers;

        [Header("Доступность")]
        [Tooltip("Требования для доступа к локации")]
        public LocationRequirement accessRequirement;

        [Tooltip("Стоимость перехода в локацию")]
        public float transitionCost = 0f;

        [Tooltip("Время перехода в секундах")]
        public float transitionTime = 5f;

        [Header("Специальные эффекты")]
        [Tooltip("Описание эффектов локации")]
        [TextArea(2, 4)]
        public string effectsDescription = "";

        [Tooltip("Случайные события в локации")]
        public RandomEvent[] randomEvents;

        /// <summary>
        /// Модификатор спавна NPC
        /// </summary>
        [System.Serializable]
        public struct NPCSpawnModifier
        {
            public NPCType npcType;
            [Range(0f, 5f)]
            public float spawnChanceModifier; // Множитель шанса появления
        }

        /// <summary>
        /// Случайное событие в локации
        /// </summary>
        [System.Serializable]
        public struct RandomEvent
        {
            public string eventName;
            public string description;
            [Range(0f, 1f)]
            public float probability; // Вероятность события
            public LocationStatModifier[] effects; // Эффекты события
            public float moneyReward; // Награда деньгами (может быть отрицательной)
        }

        /// <summary>
        /// Получить модификатор для указанной характеристики
        /// </summary>
        public float GetStatModifier(StatType statType)
        {
            foreach (var modifier in statModifiers)
            {
                if (modifier.statType == statType)
                    return modifier.modifier;
            }
            return 1.0f; // По умолчанию без изменений
        }

        /// <summary>
        /// Получить модификатор спавна для типа NPC
        /// </summary>
        public float GetNPCSpawnModifier(NPCType npcType)
        {
            foreach (var modifier in npcSpawnModifiers)
            {
                if (modifier.npcType == npcType)
                    return modifier.spawnChanceModifier;
            }
            return 1.0f; // По умолчанию без изменений
        }

        /// <summary>
        /// Проверить доступность локации
        /// </summary>
        public LocationAccessibility CheckAccessibility(PlayerStats stats, TimeOfDay currentTime, HashSet<LocationType> unlockedLocations)
        {
            if (!accessRequirement.IsMet(stats, currentTime, unlockedLocations))
            {
                // Проверяем конкретную причину недоступности
                if (accessRequirement.allowedTimes != null && accessRequirement.allowedTimes.Length > 0)
                {
                    bool timeAllowed = false;
                    foreach (var time in accessRequirement.allowedTimes)
                    {
                        if (time == currentTime)
                        {
                            timeAllowed = true;
                            break;
                        }
                    }
                    if (!timeAllowed) return LocationAccessibility.TimeRestricted;
                }

                return LocationAccessibility.Locked;
            }

            return LocationAccessibility.Available;
        }

        /// <summary>
        /// Получить описание требований доступа
        /// </summary>
        public string GetAccessRequirementsDescription()
        {
            var requirements = new List<string>();

            if (accessRequirement.minLevel > 1)
                requirements.Add($"Уровень {accessRequirement.minLevel}+");

            if (accessRequirement.minMoney > 0)
                requirements.Add($"{GameUtils.FormatMoney(accessRequirement.minMoney)}");

            if (accessRequirement.allowedTimes != null && accessRequirement.allowedTimes.Length > 0)
            {
                var timeNames = new List<string>();
                foreach (var time in accessRequirement.allowedTimes)
                {
                    timeNames.Add(GetTimeOfDayName(time));
                }
                requirements.Add($"Время: {string.Join(", ", timeNames)}");
            }

            if (accessRequirement.requiredLocations != null && accessRequirement.requiredLocations.Length > 0)
            {
                requirements.Add("Требуются другие локации");
            }

            if (!string.IsNullOrEmpty(accessRequirement.requirement))
                requirements.Add(accessRequirement.requirement);

            return requirements.Count > 0 ? string.Join("\n", requirements) : "Всегда доступна";
        }

        /// <summary>
        /// Получить локализованное название времени дня
        /// </summary>
        private string GetTimeOfDayName(TimeOfDay time)
        {
            switch (time)
            {
                case TimeOfDay.Morning: return "Утро";
                case TimeOfDay.Day: return "День";
                case TimeOfDay.Evening: return "Вечер";
                case TimeOfDay.Night: return "Ночь";
                default: return time.ToString();
            }
        }

        /// <summary>
        /// Проверить, доступны ли покупки в этой локации
        /// </summary>
        public bool HasShops()
        {
            return locationType == LocationType.ShoppingMall;
        }

        /// <summary>
        /// Получить случайное событие для этой локации
        /// </summary>
        public RandomEvent? GetRandomEvent()
        {
            if (randomEvents == null || randomEvents.Length == 0)
                return null;

            float totalProbability = 0f;
            foreach (var randomEvent in randomEvents)
            {
                totalProbability += randomEvent.probability;
            }

            if (totalProbability <= 0f)
                return null;

            float randomValue = Random.Range(0f, totalProbability);
            float currentProbability = 0f;

            foreach (var randomEvent in randomEvents)
            {
                currentProbability += randomEvent.probability;
                if (randomValue <= currentProbability)
                    return randomEvent;
            }

            return null;
        }

        /// <summary>
        /// Получить цвет локации для UI
        /// </summary>
        public Color GetLocationColor()
        {
            switch (locationType)
            {
                case LocationType.Street: return new Color(0.7f, 0.7f, 0.7f, 1f); // Серый
                case LocationType.Park: return new Color(0.2f, 0.8f, 0.2f, 1f); // Зеленый
                case LocationType.BusinessDistrict: return new Color(0.2f, 0.2f, 0.8f, 1f); // Синий
                case LocationType.ShoppingMall: return new Color(0.8f, 0.6f, 0.2f, 1f); // Оранжевый
                case LocationType.Slums: return new Color(0.6f, 0.3f, 0.1f, 1f); // Коричневый
                case LocationType.TrainStation: return new Color(0.5f, 0.5f, 0.5f, 1f); // Темно-серый
                case LocationType.NightClub: return new Color(0.8f, 0.2f, 0.8f, 1f); // Фиолетовый
                default: return Color.white;
            }
        }
    }
}
