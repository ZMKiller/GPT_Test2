using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// ScriptableObject для хранения данных о типах прохожих
    /// </summary>
    [CreateAssetMenu(fileName = "NPCData", menuName = "Homeless to Millionaire/NPC Data")]
    public class NPCData : ScriptableObject
    {
        [Header("Основные характеристики")]
        [SerializeField] private NPCType npcType;                   // Тип прохожего
        [SerializeField] private string npcName;                    // Название типа
        [SerializeField] private Sprite npcSprite;                  // Спрайт прохожего
        
        [Header("Экономические параметры")]
        [SerializeField] private float minMoney = 1f;               // Минимальная сумма денег
        [SerializeField] private float maxMoney = 5f;               // Максимальная сумма денег
        [SerializeField] private float generosityModifier = 1f;     // Модификатор щедрости
        
        [Header("Движение")]
        [SerializeField] private float minSpeed = 2f;               // Минимальная скорость
        [SerializeField] private float maxSpeed = 4f;               // Максимальная скорость
        [SerializeField] private float detectionRadius = 2f;        // Радиус обнаружения игрока
        
        [Header("Появление")]
        [SerializeField] private float spawnWeight = 1f;            // Вес появления (вероятность)
        [SerializeField] private bool availableAtMorning = true;    // Доступен утром
        [SerializeField] private bool availableAtDay = true;        // Доступен днем
        [SerializeField] private bool availableAtEvening = true;    // Доступен вечером
        [SerializeField] private bool availableAtNight = false;     // Доступен ночью
        
        [Header("Поведение")]
        [SerializeField] private bool givesMoneyContinuously = false; // Дает деньги постоянно или один раз
        [SerializeField] private float moneyGivingCooldown = 1f;     // Кулдаун между выдачей денег
        [SerializeField] private float despawnTime = 10f;           // Время до исчезновения если не взаимодействует
        
        // Публичные свойства для чтения
        public NPCType Type => npcType;
        public string Name => npcName;
        public Sprite Sprite => npcSprite;
        public float MinMoney => minMoney;
        public float MaxMoney => maxMoney;
        public float GenerosityModifier => generosityModifier;
        public float MinSpeed => minSpeed;
        public float MaxSpeed => maxSpeed;
        public float DetectionRadius => detectionRadius;
        public float SpawnWeight => spawnWeight;
        public bool GivesMoneyContinuously => givesMoneyContinuously;
        public float MoneyGivingCooldown => moneyGivingCooldown;
        public float DespawnTime => despawnTime;

        /// <summary>
        /// Проверить доступность в указанное время дня
        /// </summary>
        /// <param name="timeOfDay">Время дня</param>
        /// <returns>True если NPC доступен в это время</returns>
        public bool IsAvailableAt(TimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case TimeOfDay.Morning:
                    return availableAtMorning;
                case TimeOfDay.Day:
                    return availableAtDay;
                case TimeOfDay.Evening:
                    return availableAtEvening;
                case TimeOfDay.Night:
                    return availableAtNight;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Получить случайную сумму денег, которую может дать этот NPC
        /// </summary>
        /// <param name="playerMoodModifier">Модификатор настроения игрока</param>
        /// <param name="playerLevelModifier">Модификатор уровня игрока</param>
        /// <returns>Сумма денег</returns>
        public float GetRandomMoneyAmount(float playerMoodModifier = 1f, float playerLevelModifier = 1f)
        {
            float baseMoney = Random.Range(minMoney, maxMoney);
            return baseMoney * generosityModifier * playerMoodModifier * playerLevelModifier;
        }

        /// <summary>
        /// Получить случайную скорость движения
        /// </summary>
        /// <returns>Скорость движения</returns>
        public float GetRandomSpeed()
        {
            return Random.Range(minSpeed, maxSpeed);
        }

        /// <summary>
        /// Получить описание типа NPC
        /// </summary>
        /// <returns>Текстовое описание</returns>
        public string GetDescription()
        {
            switch (npcType)
            {
                case NPCType.Poor:
                    return "Бедный прохожий. Дает мало денег, но появляется часто.";
                case NPCType.Normal:
                    return "Обычный прохожий. Средняя сумма денег.";
                case NPCType.Rich:
                    return "Богатый прохожий. Дает много денег.";
                case NPCType.Tourist:
                    return "Турист. Может дать как мало, так и много денег.";
                case NPCType.Businessman:
                    return "Бизнесмен. Очень щедрый, но появляется редко.";
                default:
                    return "Прохожий";
            }
        }

        /// <summary>
        /// Валидация данных в редакторе
        /// </summary>
        private void OnValidate()
        {
            // Проверка корректности значений
            minMoney = Mathf.Max(0f, minMoney);
            maxMoney = Mathf.Max(minMoney, maxMoney);
            minSpeed = Mathf.Max(0.1f, minSpeed);
            maxSpeed = Mathf.Max(minSpeed, maxSpeed);
            detectionRadius = Mathf.Max(0.1f, detectionRadius);
            spawnWeight = Mathf.Max(0f, spawnWeight);
            generosityModifier = Mathf.Max(0f, generosityModifier);
            moneyGivingCooldown = Mathf.Max(0.1f, moneyGivingCooldown);
            despawnTime = Mathf.Max(1f, despawnTime);
            
            // Установка имени по умолчанию
            if (string.IsNullOrEmpty(npcName))
            {
                npcName = npcType.ToString();
            }
        }
    }

    /// <summary>
    /// Коллекция данных о всех типах NPC
    /// </summary>
    [CreateAssetMenu(fileName = "NPCDatabase", menuName = "Homeless to Millionaire/NPC Database")]
    public class NPCDatabase : ScriptableObject
    {
        [Header("База данных NPC")]
        [SerializeField] private NPCData[] npcTypes;

        /// <summary>
        /// Получить все типы NPC
        /// </summary>
        public NPCData[] GetAllNPCTypes()
        {
            return npcTypes;
        }

        /// <summary>
        /// Получить данные NPC по типу
        /// </summary>
        /// <param name="type">Тип NPC</param>
        /// <returns>Данные NPC или null</returns>
        public NPCData GetNPCData(NPCType type)
        {
            foreach (var npcData in npcTypes)
            {
                if (npcData != null && npcData.Type == type)
                {
                    return npcData;
                }
            }
            return null;
        }

        /// <summary>
        /// Получить случайный тип NPC с учетом веса и времени дня
        /// </summary>
        /// <param name="timeOfDay">Время дня</param>
        /// <returns>Данные случайного NPC</returns>
        public NPCData GetRandomNPCData(TimeOfDay timeOfDay)
        {
            // Собрать доступных NPC
            var availableNPCs = new System.Collections.Generic.List<NPCData>();
            var weights = new System.Collections.Generic.List<float>();
            
            foreach (var npcData in npcTypes)
            {
                if (npcData != null && npcData.IsAvailableAt(timeOfDay) && npcData.SpawnWeight > 0)
                {
                    availableNPCs.Add(npcData);
                    weights.Add(npcData.SpawnWeight);
                }
            }

            if (availableNPCs.Count == 0)
                return null;

            // Выбрать случайного NPC с учетом веса
            return GetWeightedRandomNPC(availableNPCs.ToArray(), weights.ToArray());
        }

        /// <summary>
        /// Выбрать случайного NPC с учетом весов
        /// </summary>
        /// <param name="npcs">Массив доступных NPC</param>
        /// <param name="weights">Массив весов</param>
        /// <returns>Выбранный NPC</returns>
        private NPCData GetWeightedRandomNPC(NPCData[] npcs, float[] weights)
        {
            if (npcs.Length == 0) return null;

            float totalWeight = 0f;
            foreach (float weight in weights)
            {
                totalWeight += weight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < npcs.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return npcs[i];
                }
            }

            return npcs[npcs.Length - 1]; // Возврат последнего как fallback
        }

        /// <summary>
        /// Валидация базы данных
        /// </summary>
        private void OnValidate()
        {
            if (npcTypes == null)
            {
                npcTypes = new NPCData[0];
            }
        }
    }
}