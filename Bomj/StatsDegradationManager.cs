using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер деградации характеристик игрока со временем
    /// </summary>
    public class StatsDegradationManager : MonoBehaviour
    {
        [Header("Настройки деградации")]
        [SerializeField] private float hungerIncreaseRate = 5f;     // Скорость роста голода в час
        [SerializeField] private float moodDecreaseRate = 2f;       // Скорость ухудшения настроения в час
        [SerializeField] private float healthDecreaseRate = 3f;     // Скорость потери здоровья от голода в час
        
        [Header("Пороговые значения")]
        [SerializeField] private float hungerHealthDamageThreshold = 70f;   // При каком голоде начинается урон здоровью
        [SerializeField] private float hungerMoodDamageThreshold = 50f;     // При каком голоде начинается урон настроению
        
        [Header("Временные интервалы")]
        [SerializeField] private float updateInterval = 1f;        // Интервал обновления в секундах
        
        private PlayerStats playerStats;
        private float timeSinceLastUpdate = 0f;
        private bool isActive = true;
        private float locationDegradationModifier = 1f; // Модификатор от текущей локации

        /// <summary>
        /// Активна ли деградация характеристик
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        private void Start()
        {
            // Найти компонент PlayerStats
            playerStats = GetComponent<PlayerStats>();
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }

            if (playerStats == null)
            {
                Debug.LogError("StatsDegradationManager: Не найден компонент PlayerStats!");
                enabled = false;
            }

            // Подписаться на события
            GameEvents.OnPlayerDeath += OnPlayerDeath;
        }

        private void Update()
        {
            if (!isActive || playerStats == null || playerStats.IsDead)
                return;

            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                ProcessDegradation(timeSinceLastUpdate);
                timeSinceLastUpdate = 0f;
            }
        }

        /// <summary>
        /// Обработать деградацию характеристик
        /// </summary>
        /// <param name="deltaTime">Время с последнего обновления</param>
        private void ProcessDegradation(float deltaTime)
        {
            float timeMultiplier = deltaTime / 3600f; // Преобразование в часы
            
            // Увеличение голода
            ProcessHungerIncrease(timeMultiplier);
            
            // Влияние голода на другие характеристики
            ProcessHungerEffects(timeMultiplier);
            
            // Естественное ухудшение настроения
            ProcessMoodDegradation(timeMultiplier);
        }

        /// <summary>
        /// Обработать увеличение голода
        /// </summary>
        /// <param name="timeMultiplier">Множитель времени в часах</param>
        private void ProcessHungerIncrease(float timeMultiplier)
        {
            float hungerIncrease = hungerIncreaseRate * timeMultiplier * locationDegradationModifier;
            playerStats.ChangeHunger(hungerIncrease);
        }

        /// <summary>
        /// Обработать влияние голода на другие характеристики
        /// </summary>
        /// <param name="timeMultiplier">Множитель времени в часах</param>
        private void ProcessHungerEffects(float timeMultiplier)
        {
            float currentHunger = playerStats.Hunger;
            
            // Урон здоровью от сильного голода
            if (currentHunger >= hungerHealthDamageThreshold)
            {
                float hungerSeverity = (currentHunger - hungerHealthDamageThreshold) / (playerStats.MaxHunger - hungerHealthDamageThreshold);
                float healthDamage = healthDecreaseRate * hungerSeverity * timeMultiplier * locationDegradationModifier;
                playerStats.ChangeHealth(-healthDamage);
            }
            
            // Урон настроению от голода
            if (currentHunger >= hungerMoodDamageThreshold)
            {
                float hungerSeverity = (currentHunger - hungerMoodDamageThreshold) / (playerStats.MaxHunger - hungerMoodDamageThreshold);
                float moodDamage = moodDecreaseRate * hungerSeverity * timeMultiplier * locationDegradationModifier;
                playerStats.ChangeMood(-moodDamage);
            }
        }

        /// <summary>
        /// Обработать естественное ухудшение настроения
        /// </summary>
        /// <param name="timeMultiplier">Множитель времени в часах</param>
        private void ProcessMoodDegradation(float timeMultiplier)
        {
            // Настроение ухудшается медленнее, если здоровье и голод в норме
            float moodDecrease = moodDecreaseRate * timeMultiplier * locationDegradationModifier;
            
            // Модификаторы в зависимости от состояния
            if (playerStats.IsLowHealth)
            {
                moodDecrease *= 1.5f; // Низкое здоровье ухудшает настроение быстрее
            }
            
            if (playerStats.IsHighHunger)
            {
                moodDecrease *= 1.3f; // Голод тоже влияет на настроение
            }
            
            playerStats.ChangeMood(-moodDecrease);
        }

        /// <summary>
        /// Установить скорость роста голода
        /// </summary>
        /// <param name="rate">Новая скорость роста голода в час</param>
        public void SetHungerIncreaseRate(float rate)
        {
            hungerIncreaseRate = Mathf.Max(0f, rate);
        }

        /// <summary>
        /// Установить скорость ухудшения настроения
        /// </summary>
        /// <param name="rate">Новая скорость ухудшения настроения в час</param>
        public void SetMoodDecreaseRate(float rate)
        {
            moodDecreaseRate = Mathf.Max(0f, rate);
        }

        /// <summary>
        /// Установить скорость потери здоровья от голода
        /// </summary>
        /// <param name="rate">Новая скорость потери здоровья в час</param>
        public void SetHealthDecreaseRate(float rate)
        {
            healthDecreaseRate = Mathf.Max(0f, rate);
        }

        /// <summary>
        /// Установить модификатор деградации от локации
        /// </summary>
        /// <param name="modifier">Модификатор деградации (1.0 = нормальная скорость, 0.5 = вдвое медленнее, 2.0 = вдвое быстрее)</param>
        public void SetLocationDegradationModifier(float modifier)
        {
            locationDegradationModifier = Mathf.Max(0.1f, modifier); // Минимум 0.1 чтобы не было полной остановки
            Debug.Log($"Модификатор деградации от локации установлен: {locationDegradationModifier:F2}");
        }

        /// <summary>
        /// Получить текущий модификатор деградации от локации
        /// </summary>
        public float GetLocationDegradationModifier()
        {
            return locationDegradationModifier;
        }

        /// <summary>
        /// Приостановить деградацию на указанное время
        /// </summary>
        /// <param name="duration">Продолжительность паузы в секундах</param>
        public void PauseDegradation(float duration)
        {
            StartCoroutine(PauseDegradationCoroutine(duration));
        }

        /// <summary>
        /// Корутина для временной приостановки деградации
        /// </summary>
        private System.Collections.IEnumerator PauseDegradationCoroutine(float duration)
        {
            bool wasActive = isActive;
            isActive = false;
            
            yield return new WaitForSeconds(duration);
            
            isActive = wasActive;
        }

        /// <summary>
        /// Получить информацию о текущих скоростях деградации
        /// </summary>
        public DegradationInfo GetDegradationInfo()
        {
            return new DegradationInfo
            {
                hungerRate = hungerIncreaseRate,
                moodRate = moodDecreaseRate,
                healthRate = healthDecreaseRate,
                isActive = isActive
            };
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            isActive = false; // Остановить деградацию при смерти
        }

        private void OnDestroy()
        {
            // Отписаться от событий
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
        }

        /// <summary>
        /// Отладочная информация в редакторе
        /// </summary>
        private void OnValidate()
        {
            hungerIncreaseRate = Mathf.Max(0f, hungerIncreaseRate);
            moodDecreaseRate = Mathf.Max(0f, moodDecreaseRate);
            healthDecreaseRate = Mathf.Max(0f, healthDecreaseRate);
            updateInterval = Mathf.Max(0.1f, updateInterval);
        }
    }

    /// <summary>
    /// Информация о скоростях деградации
    /// </summary>
    [System.Serializable]
    public class DegradationInfo
    {
        public float hungerRate;        // Скорость роста голода
        public float moodRate;          // Скорость ухудшения настроения
        public float healthRate;        // Скорость потери здоровья
        public bool isActive;           // Активна ли деградация
    }
}