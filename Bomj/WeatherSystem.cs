using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система погоды
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Настройки погоды")]
        [SerializeField] private bool enableWeatherSystem = true;
        [SerializeField] private float weatherChangeInterval = 300f; // 5 минут реального времени
        [SerializeField] private bool allowWeatherOverride = true;
        [SerializeField] private bool seasonalWeather = true; // Погода зависит от сезона

        [Header("Вероятности погоды по сезонам")]
        [SerializeField] private WeatherProbabilities springWeather = new WeatherProbabilities();
        [SerializeField] private WeatherProbabilities summerWeather = new WeatherProbabilities();
        [SerializeField] private WeatherProbabilities autumnWeather = new WeatherProbabilities();
        [SerializeField] private WeatherProbabilities winterWeather = new WeatherProbabilities();

        [Header("Влияние погоды на игру")]
        [SerializeField] private float rainMoodPenalty = -0.1f;         // Дождь снижает настроение
        [SerializeField] private float sunnyMoodBonus = 0.05f;          // Солнце улучшает настроение
        [SerializeField] private float stormEarningsReduction = -0.3f;  // Гроза снижает заработок
        [SerializeField] private float fogVisibilityReduction = 0.5f;   // Туман снижает видимость
        [SerializeField] private float snowMovementReduction = -0.2f;   // Снег замедляет движение

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceWeatherType = false;
        [SerializeField] private WeatherType debugWeatherType = WeatherType.Clear;
        [SerializeField] private WeatherIntensity debugIntensity = WeatherIntensity.Moderate;

        // Текущее состояние погоды
        private WeatherType currentWeather;
        private WeatherIntensity currentIntensity;
        private Season currentSeason;
        private float timeToNextWeatherChange;
        private bool weatherInitialized = false;

        // Компоненты
        private TimeOfDayManager timeOfDayManager;
        private PlayerStats playerStats;

        // События
        public event Action<WeatherType, WeatherIntensity> OnWeatherChanged;
        public event Action<WeatherType> OnWeatherStarted;
        public event Action<WeatherType> OnWeatherStopped;

        // Свойства
        public WeatherType CurrentWeather => currentWeather;
        public WeatherIntensity CurrentIntensity => currentIntensity;
        public bool IsRaining => currentWeather == WeatherType.Rainy || currentWeather == WeatherType.Heavy_Rain;
        public bool IsSnowing => currentWeather == WeatherType.Snow;
        public bool IsStormy => currentWeather == WeatherType.Storm;
        public bool IsFoggy => currentWeather == WeatherType.Fog;
        public bool IsClear => currentWeather == WeatherType.Clear || currentWeather == WeatherType.Cloudy;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            playerStats = FindObjectOfType<PlayerStats>();

            // Инициализация вероятностей по умолчанию
            InitializeDefaultProbabilities();
        }

        private void Start()
        {
            if (enableWeatherSystem)
            {
                InitializeWeather();
                SubscribeToEvents();
                StartCoroutine(WeatherUpdateCoroutine());
            }

            weatherInitialized = true;
            Debug.Log("WeatherSystem инициализирован");
        }

        private void Update()
        {
            if (forceWeatherType && Application.isEditor)
            {
                if (currentWeather != debugWeatherType || currentIntensity != debugIntensity)
                {
                    SetWeather(debugWeatherType, debugIntensity);
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация погоды
        /// </summary>
        private void InitializeWeather()
        {
            // Получить текущий сезон
            if (timeOfDayManager != null)
            {
                currentSeason = timeOfDayManager.CurrentSeason;
            }
            else
            {
                currentSeason = Season.Spring;
            }

            // Установить начальную погоду
            currentWeather = WeatherType.Clear;
            currentIntensity = WeatherIntensity.None;
            timeToNextWeatherChange = weatherChangeInterval;

            // Генерировать случайную погоду
            GenerateRandomWeather();

            if (showDebugInfo)
            {
                Debug.Log($"Погода инициализирована: {GetWeatherDescription()}");
            }
        }

        /// <summary>
        /// Инициализация вероятностей погоды по умолчанию
        /// </summary>
        private void InitializeDefaultProbabilities()
        {
            // Весна - дождливая
            springWeather.clearChance = 30f;
            springWeather.cloudyChance = 25f;
            springWeather.rainyChance = 30f;
            springWeather.heavyRainChance = 10f;
            springWeather.snowChance = 0f;
            springWeather.fogChance = 5f;
            springWeather.stormChance = 0f;

            // Лето - ясное с грозами
            summerWeather.clearChance = 50f;
            summerWeather.cloudyChance = 20f;
            summerWeather.rainyChance = 10f;
            summerWeather.heavyRainChance = 5f;
            summerWeather.snowChance = 0f;
            summerWeather.fogChance = 5f;
            summerWeather.stormChance = 10f;

            // Осень - пасмурная и дождливая
            autumnWeather.clearChance = 20f;
            autumnWeather.cloudyChance = 35f;
            autumnWeather.rainyChance = 25f;
            autumnWeather.heavyRainChance = 10f;
            autumnWeather.snowChance = 5f;
            autumnWeather.fogChance = 5f;
            autumnWeather.stormChance = 0f;

            // Зима - снежная и туманная
            winterWeather.clearChance = 15f;
            winterWeather.cloudyChance = 30f;
            winterWeather.rainyChance = 10f;
            winterWeather.heavyRainChance = 0f;
            winterWeather.snowChance = 35f;
            winterWeather.fogChance = 10f;
            winterWeather.stormChance = 0f;
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnSeasonChanged += OnSeasonChanged;
                timeOfDayManager.OnTimePeriodChanged += OnTimePeriodChanged;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnSeasonChanged -= OnSeasonChanged;
                timeOfDayManager.OnTimePeriodChanged -= OnTimePeriodChanged;
            }
        }

        #endregion

        #region Weather Generation

        /// <summary>
        /// Корутина обновления погоды
        /// </summary>
        private IEnumerator WeatherUpdateCoroutine()
        {
            while (enabled)
            {
                if (enableWeatherSystem && !forceWeatherType)
                {
                    timeToNextWeatherChange -= Time.deltaTime;
                    
                    if (timeToNextWeatherChange <= 0f)
                    {
                        GenerateRandomWeather();
                        timeToNextWeatherChange = weatherChangeInterval + UnityEngine.Random.Range(-60f, 60f); // ±1 минута
                    }
                }

                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// Генерировать случайную погоду
        /// </summary>
        private void GenerateRandomWeather()
        {
            WeatherProbabilities probabilities = GetWeatherProbabilities();
            WeatherType newWeather = SelectRandomWeather(probabilities);
            WeatherIntensity newIntensity = SelectRandomIntensity(newWeather);

            SetWeather(newWeather, newIntensity);
        }

        /// <summary>
        /// Получить вероятности погоды для текущего сезона
        /// </summary>
        private WeatherProbabilities GetWeatherProbabilities()
        {
            if (!seasonalWeather)
            {
                return springWeather; // Использовать весенние настройки как универсальные
            }

            switch (currentSeason)
            {
                case Season.Spring: return springWeather;
                case Season.Summer: return summerWeather;
                case Season.Autumn: return autumnWeather;
                case Season.Winter: return winterWeather;
                default: return springWeather;
            }
        }

        /// <summary>
        /// Выбрать случайную погоду на основе вероятностей
        /// </summary>
        private WeatherType SelectRandomWeather(WeatherProbabilities probabilities)
        {
            float totalWeight = probabilities.GetTotalWeight();
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            if (randomValue < (cumulativeWeight += probabilities.clearChance))
                return WeatherType.Clear;
            if (randomValue < (cumulativeWeight += probabilities.cloudyChance))
                return WeatherType.Cloudy;
            if (randomValue < (cumulativeWeight += probabilities.rainyChance))
                return WeatherType.Rainy;
            if (randomValue < (cumulativeWeight += probabilities.heavyRainChance))
                return WeatherType.Heavy_Rain;
            if (randomValue < (cumulativeWeight += probabilities.snowChance))
                return WeatherType.Snow;
            if (randomValue < (cumulativeWeight += probabilities.fogChance))
                return WeatherType.Fog;
            if (randomValue < (cumulativeWeight += probabilities.stormChance))
                return WeatherType.Storm;

            return WeatherType.Clear; // Fallback
        }

        /// <summary>
        /// Выбрать случайную интенсивность для типа погоды
        /// </summary>
        private WeatherIntensity SelectRandomIntensity(WeatherType weatherType)
        {
            switch (weatherType)
            {
                case WeatherType.Clear:
                case WeatherType.Cloudy:
                    return WeatherIntensity.None;

                case WeatherType.Rainy:
                case WeatherType.Snow:
                case WeatherType.Fog:
                    // Легкая до умеренной
                    return UnityEngine.Random.value < 0.7f ? WeatherIntensity.Light : WeatherIntensity.Moderate;

                case WeatherType.Heavy_Rain:
                    // Умеренная до сильной
                    return UnityEngine.Random.value < 0.5f ? WeatherIntensity.Moderate : WeatherIntensity.Heavy;

                case WeatherType.Storm:
                    // Всегда сильная или экстремальная
                    return UnityEngine.Random.value < 0.3f ? WeatherIntensity.Heavy : WeatherIntensity.Extreme;

                default:
                    return WeatherIntensity.Light;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Установить погоду вручную
        /// </summary>
        public void SetWeather(WeatherType weather, WeatherIntensity intensity = WeatherIntensity.Moderate)
        {
            WeatherType previousWeather = currentWeather;
            
            currentWeather = weather;
            currentIntensity = intensity;

            // Применить эффекты погоды
            ApplyWeatherEffects();

            // Уведомления о событиях
            if (previousWeather != currentWeather)
            {
                OnWeatherStopped?.Invoke(previousWeather);
                OnWeatherStarted?.Invoke(currentWeather);
            }

            OnWeatherChanged?.Invoke(currentWeather, currentIntensity);

            if (showDebugInfo)
            {
                Debug.Log($"Погода изменена: {GetWeatherDescription()}");
            }
        }

        /// <summary>
        /// Установить ясную погоду
        /// </summary>
        public void SetClearWeather()
        {
            SetWeather(WeatherType.Clear, WeatherIntensity.None);
        }

        /// <summary>
        /// Запустить дождь
        /// </summary>
        public void StartRain(WeatherIntensity intensity = WeatherIntensity.Moderate)
        {
            WeatherType rainType = intensity >= WeatherIntensity.Heavy ? 
                                   WeatherType.Heavy_Rain : WeatherType.Rainy;
            SetWeather(rainType, intensity);
        }

        /// <summary>
        /// Запустить снег
        /// </summary>
        public void StartSnow(WeatherIntensity intensity = WeatherIntensity.Moderate)
        {
            SetWeather(WeatherType.Snow, intensity);
        }

        /// <summary>
        /// Запустить грозу
        /// </summary>
        public void StartStorm(WeatherIntensity intensity = WeatherIntensity.Heavy)
        {
            SetWeather(WeatherType.Storm, intensity);
        }

        /// <summary>
        /// Включить/выключить принудительную погоду
        /// </summary>
        public void SetForceWeather(bool enabled, WeatherType weather = WeatherType.Clear, 
                                   WeatherIntensity intensity = WeatherIntensity.None)
        {
            forceWeatherType = enabled;
            if (enabled)
            {
                debugWeatherType = weather;
                debugIntensity = intensity;
                SetWeather(weather, intensity);
            }
        }

        /// <summary>
        /// Получить описание текущей погоды
        /// </summary>
        public string GetWeatherDescription()
        {
            string weatherName = GetWeatherName(currentWeather);
            string intensityName = GetIntensityName(currentIntensity);
            
            if (currentIntensity == WeatherIntensity.None)
            {
                return weatherName;
            }
            else
            {
                return $"{intensityName} {weatherName.ToLower()}";
            }
        }

        /// <summary>
        /// Получить модификатор настроения от погоды
        /// </summary>
        public float GetMoodModifier()
        {
            switch (currentWeather)
            {
                case WeatherType.Clear:
                    return sunnyMoodBonus;
                
                case WeatherType.Rainy:
                case WeatherType.Heavy_Rain:
                    return rainMoodPenalty * GetIntensityMultiplier();
                
                case WeatherType.Storm:
                    return rainMoodPenalty * 2f * GetIntensityMultiplier();
                
                case WeatherType.Snow:
                    return rainMoodPenalty * 0.5f * GetIntensityMultiplier();
                
                case WeatherType.Fog:
                    return rainMoodPenalty * 0.3f;
                
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Получить модификатор заработка от погоды
        /// </summary>
        public float GetEarningsModifier()
        {
            switch (currentWeather)
            {
                case WeatherType.Storm:
                    return stormEarningsReduction * GetIntensityMultiplier();
                
                case WeatherType.Heavy_Rain:
                    return stormEarningsReduction * 0.5f * GetIntensityMultiplier();
                
                case WeatherType.Rainy:
                    return stormEarningsReduction * 0.3f * GetIntensityMultiplier();
                
                case WeatherType.Snow:
                    return stormEarningsReduction * 0.4f * GetIntensityMultiplier();
                
                case WeatherType.Fog:
                    return stormEarningsReduction * 0.2f;
                
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Получить модификатор видимости от погоды
        /// </summary>
        public float GetVisibilityModifier()
        {
            switch (currentWeather)
            {
                case WeatherType.Fog:
                    return fogVisibilityReduction * GetIntensityMultiplier();
                
                case WeatherType.Heavy_Rain:
                case WeatherType.Storm:
                    return fogVisibilityReduction * 0.3f * GetIntensityMultiplier();
                
                case WeatherType.Snow:
                    return fogVisibilityReduction * 0.4f * GetIntensityMultiplier();
                
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public WeatherSystemSaveData GetSaveData()
        {
            return new WeatherSystemSaveData
            {
                currentWeather = currentWeather,
                currentIntensity = currentIntensity,
                currentSeason = currentSeason,
                timeToNextWeatherChange = timeToNextWeatherChange
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(WeatherSystemSaveData data)
        {
            currentWeather = data.currentWeather;
            currentIntensity = data.currentIntensity;
            currentSeason = data.currentSeason;
            timeToNextWeatherChange = data.timeToNextWeatherChange;

            ApplyWeatherEffects();
            OnWeatherChanged?.Invoke(currentWeather, currentIntensity);

            if (showDebugInfo)
            {
                Debug.Log($"Погода загружена: {GetWeatherDescription()}");
            }
        }

        #endregion

        #region Weather Effects

        /// <summary>
        /// Применить эффекты погоды к игре
        /// </summary>
        private void ApplyWeatherEffects()
        {
            if (playerStats == null) return;

            // Применить модификаторы характеристик от погоды
            ApplyWeatherStatModifiers();
        }

        /// <summary>
        /// Применить модификаторы характеристик от погоды
        /// </summary>
        private void ApplyWeatherStatModifiers()
        {
            // Удалить старые модификаторы погоды
            playerStats.RemoveModifiersBySource("Weather");

            // Добавить новые модификаторы
            float moodMod = GetMoodModifier();
            if (Mathf.Abs(moodMod) > 0.01f)
            {
                playerStats.AddStatModifier(new StatModifier(
                    StatType.MoodRegen, moodMod, ModifierOperation.Add, "Weather"));
            }

            float earningsMod = GetEarningsModifier();
            if (Mathf.Abs(earningsMod) > 0.01f)
            {
                playerStats.AddStatModifier(new StatModifier(
                    StatType.EarningsMultiplier, earningsMod, ModifierOperation.Add, "Weather"));
            }

            // Особые эффекты от снега
            if (currentWeather == WeatherType.Snow)
            {
                float movementMod = snowMovementReduction * GetIntensityMultiplier();
                playerStats.AddStatModifier(new StatModifier(
                    StatType.HungerRate, -movementMod, ModifierOperation.Add, "Weather"));
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Получить множитель интенсивности
        /// </summary>
        private float GetIntensityMultiplier()
        {
            switch (currentIntensity)
            {
                case WeatherIntensity.None: return 0f;
                case WeatherIntensity.Light: return 0.5f;
                case WeatherIntensity.Moderate: return 1f;
                case WeatherIntensity.Heavy: return 1.5f;
                case WeatherIntensity.Extreme: return 2f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Получить название погоды
        /// </summary>
        private string GetWeatherName(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Clear: return "Ясно";
                case WeatherType.Cloudy: return "Облачно";
                case WeatherType.Rainy: return "Дождь";
                case WeatherType.Heavy_Rain: return "Сильный дождь";
                case WeatherType.Snow: return "Снег";
                case WeatherType.Fog: return "Туман";
                case WeatherType.Storm: return "Гроза";
                default: return weather.ToString();
            }
        }

        /// <summary>
        /// Получить название интенсивности
        /// </summary>
        private string GetIntensityName(WeatherIntensity intensity)
        {
            switch (intensity)
            {
                case WeatherIntensity.Light: return "Легкий";
                case WeatherIntensity.Moderate: return "Умеренный";
                case WeatherIntensity.Heavy: return "Сильный";
                case WeatherIntensity.Extreme: return "Экстремальный";
                default: return "";
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены сезона
        /// </summary>
        private void OnSeasonChanged(Season newSeason)
        {
            currentSeason = newSeason;
            
            if (showDebugInfo)
            {
                Debug.Log($"Сезон изменен на {newSeason}, обновляем вероятности погоды");
            }
        }

        /// <summary>
        /// Обработчик смены времени дня
        /// </summary>
        private void OnTimePeriodChanged(TimePeriod oldPeriod, TimePeriod newPeriod)
        {
            // Некоторые типы погоды более вероятны в определенное время
            // Например, туман утром, грозы вечером
            
            if (newPeriod == TimePeriod.EarlyMorning && UnityEngine.Random.value < 0.3f)
            {
                // Утренний туман
                if (currentWeather == WeatherType.Clear || currentWeather == WeatherType.Cloudy)
                {
                    SetWeather(WeatherType.Fog, WeatherIntensity.Light);
                }
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Generate Random Weather")]
        private void DebugGenerateRandomWeather()
        {
            GenerateRandomWeather();
        }

        [ContextMenu("Set Clear Weather")]
        private void DebugSetClearWeather()
        {
            SetClearWeather();
        }

        [ContextMenu("Start Light Rain")]
        private void DebugStartLightRain()
        {
            StartRain(WeatherIntensity.Light);
        }

        [ContextMenu("Start Heavy Rain")]
        private void DebugStartHeavyRain()
        {
            StartRain(WeatherIntensity.Heavy);
        }

        [ContextMenu("Start Storm")]
        private void DebugStartStorm()
        {
            StartStorm();
        }

        [ContextMenu("Start Snow")]
        private void DebugStartSnow()
        {
            StartSnow();
        }

        [ContextMenu("Start Fog")]
        private void DebugStartFog()
        {
            SetWeather(WeatherType.Fog, WeatherIntensity.Moderate);
        }

        [ContextMenu("Show Weather Info")]
        private void DebugShowWeatherInfo()
        {
            Debug.Log($"Текущая погода: {GetWeatherDescription()}");
            Debug.Log($"Модификатор настроения: {GetMoodModifier():P1}");
            Debug.Log($"Модификатор заработка: {GetEarningsModifier():P1}");
            Debug.Log($"Модификатор видимости: {GetVisibilityModifier():P1}");
            Debug.Log($"До смены погоды: {timeToNextWeatherChange:F0} секунд");
        }

        #endregion
    }

    /// <summary>
    /// Вероятности различных типов погоды
    /// </summary>
    [System.Serializable]
    public class WeatherProbabilities
    {
        [Range(0f, 100f)] public float clearChance = 40f;
        [Range(0f, 100f)] public float cloudyChance = 30f;
        [Range(0f, 100f)] public float rainyChance = 15f;
        [Range(0f, 100f)] public float heavyRainChance = 5f;
        [Range(0f, 100f)] public float snowChance = 5f;
        [Range(0f, 100f)] public float fogChance = 3f;
        [Range(0f, 100f)] public float stormChance = 2f;

        public float GetTotalWeight()
        {
            return clearChance + cloudyChance + rainyChance + heavyRainChance + 
                   snowChance + fogChance + stormChance;
        }
    }

    /// <summary>
    /// Данные WeatherSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class WeatherSystemSaveData
    {
        public WeatherType currentWeather;
        public WeatherIntensity currentIntensity;
        public Season currentSeason;
        public float timeToNextWeatherChange;
    }
}
