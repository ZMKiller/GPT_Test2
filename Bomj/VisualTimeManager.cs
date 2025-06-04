using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер визуальных эффектов времени дня
    /// </summary>
    public class VisualTimeManager : MonoBehaviour
    {
        [Header("Ссылки на объекты")]
        [SerializeField] private Light sunLight;              // Основной свет (солнце)
        [SerializeField] private Light moonLight;             // Лунный свет
        [SerializeField] private Transform sunTransform;      // Трансформ солнца для анимации
        [SerializeField] private Transform moonTransform;     // Трансформ луны для анимации
        [SerializeField] private Camera mainCamera;           // Основная камера
        [SerializeField] private Material skyboxMaterial;     // Материал неба

        [Header("Настройки освещения")]
        [SerializeField] private Gradient sunColorGradient;   // Цвет солнца в течение дня
        [SerializeField] private AnimationCurve sunIntensityCurve; // Интенсивность солнца
        [SerializeField] private Gradient ambientColorGradient; // Цвет окружающего света
        [SerializeField] private AnimationCurve ambientIntensityCurve; // Интенсивность окружающего света

        [Header("Настройки неба")]
        [SerializeField] private Gradient skyTopColorGradient;    // Верхний цвет неба
        [SerializeField] private Gradient skyHorizonColorGradient; // Цвет горизонта
        [SerializeField] private AnimationCurve starsBrightness;   // Яркость звезд
        [SerializeField] private AnimationCurve cloudOpacity;      // Прозрачность облаков

        [Header("Настройки солнца и луны")]
        [SerializeField] private float sunMoonRadius = 50f;       // Радиус орбиты солнца/луны
        [SerializeField] private Vector3 sunriseDirection = Vector3.right; // Направление восхода
        [SerializeField] private float maxSunAngle = 80f;         // Максимальный угол солнца

        [Header("Эффекты погоды")]
        [SerializeField] private ParticleSystem rainParticles;   // Частицы дождя
        [SerializeField] private ParticleSystem snowParticles;   // Частицы снега
        [SerializeField] private ParticleSystem fogParticles;    // Частицы тумана
        [SerializeField] private Light[] streetLights;           // Уличные фонари

        [Header("Настройки переходов")]
        [SerializeField] private float transitionSpeed = 1f;     // Скорость переходов
        [SerializeField] private bool smoothTransitions = true;  // Плавные переходы
        [SerializeField] private bool enableWeatherEffects = true; // Включить эффекты погоды

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool overrideTimeOfDay = false;
        [SerializeField] private float debugTimeOfDay = 0.5f;    // 0 = полночь, 0.5 = полдень

        // Текущее состояние
        private TimeOfDayManager timeOfDayManager;
        private WeatherSystem weatherSystem;
        private float currentTimeNormalized = 0.5f;  // 0-1, где 0 = полночь, 1 = полночь следующего дня
        private TimePeriod currentTimePeriod;
        private bool visualsInitialized = false;

        // Кэшированные значения для оптимизации
        private Color currentSunColor;
        private float currentSunIntensity;
        private Color currentAmbientColor;
        private float currentAmbientIntensity;
        private Vector3 currentSunRotation;
        private Vector3 currentMoonRotation;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            weatherSystem = FindObjectOfType<WeatherSystem>();
            
            if (mainCamera == null)
                mainCamera = Camera.main;

            // Инициализация значений по умолчанию
            InitializeDefaultValues();
        }

        private void Start()
        {
            if (timeOfDayManager != null)
            {
                SubscribeToTimeEvents();
                UpdateVisualsFromTime();
            }
            else
            {
                Debug.LogWarning("VisualTimeManager: TimeOfDayManager не найден!");
            }

            visualsInitialized = true;
            Debug.Log("VisualTimeManager инициализирован");
        }

        private void Update()
        {
            if (overrideTimeOfDay)
            {
                currentTimeNormalized = debugTimeOfDay;
                UpdateAllVisuals();
            }
            else if (smoothTransitions)
            {
                UpdateSmoothTransitions();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromTimeEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация значений по умолчанию
        /// </summary>
        private void InitializeDefaultValues()
        {
            // Создать градиенты по умолчанию если не заданы
            if (sunColorGradient == null)
            {
                sunColorGradient = CreateDefaultSunColorGradient();
            }

            if (sunIntensityCurve == null)
            {
                sunIntensityCurve = CreateDefaultSunIntensityCurve();
            }

            if (ambientColorGradient == null)
            {
                ambientColorGradient = CreateDefaultAmbientColorGradient();
            }

            if (ambientIntensityCurve == null)
            {
                ambientIntensityCurve = CreateDefaultAmbientIntensityCurve();
            }

            if (skyTopColorGradient == null)
            {
                skyTopColorGradient = CreateDefaultSkyTopColorGradient();
            }

            if (skyHorizonColorGradient == null)
            {
                skyHorizonColorGradient = CreateDefaultSkyHorizonColorGradient();
            }

            if (starsBrightness == null)
            {
                starsBrightness = CreateDefaultStarsBrightnessCurve();
            }

            if (cloudOpacity == null)
            {
                cloudOpacity = CreateDefaultCloudOpacityCurve();
            }

            // Настроить начальные значения освещения
            if (sunLight != null)
            {
                sunLight.type = LightType.Directional;
                sunLight.shadows = LightShadows.Soft;
            }

            if (moonLight != null)
            {
                moonLight.type = LightType.Directional;
                moonLight.intensity = 0.2f;
                moonLight.color = Color.blue;
                moonLight.shadows = LightShadows.Soft;
            }
        }

        /// <summary>
        /// Подписка на события времени
        /// </summary>
        private void SubscribeToTimeEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimeChanged += OnTimeChanged;
                timeOfDayManager.OnTimePeriodChanged += OnTimePeriodChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged += OnWeatherChanged;
            }
        }

        /// <summary>
        /// Отписка от событий времени
        /// </summary>
        private void UnsubscribeFromTimeEvents()
        {
            if (timeOfDayManager != null)
            {
                timeOfDayManager.OnTimeChanged -= OnTimeChanged;
                timeOfDayManager.OnTimePeriodChanged -= OnTimePeriodChanged;
            }

            if (weatherSystem != null)
            {
                weatherSystem.OnWeatherChanged -= OnWeatherChanged;
            }
        }

        #endregion

        #region Visual Updates

        /// <summary>
        /// Обновить визуальные эффекты на основе времени
        /// </summary>
        private void UpdateVisualsFromTime()
        {
            if (timeOfDayManager == null) return;

            // Конвертировать время в нормализованное значение (0-1)
            DateTime currentTime = timeOfDayManager.CurrentDateTime;
            currentTimeNormalized = (currentTime.Hour + currentTime.Minute / 60f) / 24f;
            currentTimePeriod = timeOfDayManager.CurrentTimePeriod;

            UpdateAllVisuals();
        }

        /// <summary>
        /// Обновить все визуальные эффекты
        /// </summary>
        private void UpdateAllVisuals()
        {
            UpdateSunAndMoonPosition();
            UpdateLighting();
            UpdateSkybox();
            UpdateStreetLights();

            if (showDebugInfo)
            {
                Debug.Log($"Визуальные эффекты обновлены. Время: {currentTimeNormalized:F2} ({currentTimePeriod})");
            }
        }

        /// <summary>
        /// Обновить плавные переходы
        /// </summary>
        private void UpdateSmoothTransitions()
        {
            if (!visualsInitialized) return;

            float deltaTime = Time.deltaTime * transitionSpeed;

            // Плавное изменение освещения
            if (sunLight != null)
            {
                Color targetSunColor = sunColorGradient.Evaluate(currentTimeNormalized);
                float targetSunIntensity = sunIntensityCurve.Evaluate(currentTimeNormalized);

                currentSunColor = Color.Lerp(currentSunColor, targetSunColor, deltaTime);
                currentSunIntensity = Mathf.Lerp(currentSunIntensity, targetSunIntensity, deltaTime);

                sunLight.color = currentSunColor;
                sunLight.intensity = currentSunIntensity;
            }

            // Плавное изменение окружающего света
            Color targetAmbientColor = ambientColorGradient.Evaluate(currentTimeNormalized);
            float targetAmbientIntensity = ambientIntensityCurve.Evaluate(currentTimeNormalized);

            currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, deltaTime);
            currentAmbientIntensity = Mathf.Lerp(currentAmbientIntensity, targetAmbientIntensity, deltaTime);

            RenderSettings.ambientLight = currentAmbientColor * currentAmbientIntensity;
        }

        /// <summary>
        /// Обновить позицию солнца и луны
        /// </summary>
        private void UpdateSunAndMoonPosition()
        {
            // Расчет углов для солнца и луны
            float sunAngle = currentTimeNormalized * 360f - 90f; // -90 чтобы полдень был сверху
            float moonAngle = sunAngle + 180f; // Луна противоположно солнцу

            // Ограничить угол солнца
            sunAngle = Mathf.Clamp(sunAngle, -maxSunAngle, maxSunAngle);

            // Позиция солнца
            if (sunTransform != null)
            {
                Vector3 sunDirection = Quaternion.Euler(sunAngle, 0, 0) * sunriseDirection;
                sunTransform.position = transform.position + sunDirection * sunMoonRadius;
                sunTransform.LookAt(transform.position);
                currentSunRotation = sunTransform.eulerAngles;
            }

            // Позиция луны
            if (moonTransform != null)
            {
                Vector3 moonDirection = Quaternion.Euler(moonAngle, 0, 0) * sunriseDirection;
                moonTransform.position = transform.position + moonDirection * sunMoonRadius;
                moonTransform.LookAt(transform.position);
                currentMoonRotation = moonTransform.eulerAngles;
            }

            // Направление солнечного света
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle + 90f, 30f, 0);
            }

            // Направление лунного света
            if (moonLight != null)
            {
                moonLight.transform.rotation = Quaternion.Euler(moonAngle + 90f, 30f, 0);
            }
        }

        /// <summary>
        /// Обновить освещение
        /// </summary>
        private void UpdateLighting()
        {
            if (!smoothTransitions)
            {
                // Мгновенное изменение освещения
                if (sunLight != null)
                {
                    currentSunColor = sunColorGradient.Evaluate(currentTimeNormalized);
                    currentSunIntensity = sunIntensityCurve.Evaluate(currentTimeNormalized);
                    
                    sunLight.color = currentSunColor;
                    sunLight.intensity = currentSunIntensity;
                    sunLight.enabled = currentSunIntensity > 0.01f;
                }

                // Окружающий свет
                currentAmbientColor = ambientColorGradient.Evaluate(currentTimeNormalized);
                currentAmbientIntensity = ambientIntensityCurve.Evaluate(currentTimeNormalized);
                RenderSettings.ambientLight = currentAmbientColor * currentAmbientIntensity;
            }

            // Лунный свет
            if (moonLight != null)
            {
                bool isNight = currentTimePeriod == TimePeriod.Night || currentTimePeriod == TimePeriod.LateNight;
                moonLight.enabled = isNight;
                moonLight.intensity = isNight ? 0.3f : 0f;
            }
        }

        /// <summary>
        /// Обновить скайбокс
        /// </summary>
        private void UpdateSkybox()
        {
            if (skyboxMaterial != null)
            {
                // Цвета неба
                Color skyTopColor = skyTopColorGradient.Evaluate(currentTimeNormalized);
                Color skyHorizonColor = skyHorizonColorGradient.Evaluate(currentTimeNormalized);

                // Установить цвета в материал скайбокса
                skyboxMaterial.SetColor("_SkyTint", skyTopColor);
                skyboxMaterial.SetColor("_GroundColor", skyHorizonColor);

                // Яркость звезд
                float starsIntensity = starsBrightness.Evaluate(currentTimeNormalized);
                skyboxMaterial.SetFloat("_Exposure", starsIntensity);

                // Прозрачность облаков
                float clouds = cloudOpacity.Evaluate(currentTimeNormalized);
                skyboxMaterial.SetFloat("_CloudCoverage", clouds);
            }
        }

        /// <summary>
        /// Обновить уличные фонари
        /// </summary>
        private void UpdateStreetLights()
        {
            if (streetLights == null || streetLights.Length == 0) return;

            bool shouldBeOn = currentTimePeriod == TimePeriod.Evening || 
                             currentTimePeriod == TimePeriod.Night || 
                             currentTimePeriod == TimePeriod.LateNight;

            foreach (var light in streetLights)
            {
                if (light != null)
                {
                    light.enabled = shouldBeOn;
                }
            }
        }

        #endregion

        #region Weather Effects

        /// <summary>
        /// Обновить эффекты погоды
        /// </summary>
        private void UpdateWeatherEffects(WeatherType weather, WeatherIntensity intensity)
        {
            if (!enableWeatherEffects) return;

            // Остановить все эффекты
            StopAllWeatherEffects();

            // Запустить нужный эффект
            switch (weather)
            {
                case WeatherType.Rainy:
                case WeatherType.Heavy_Rain:
                    StartRainEffect(intensity);
                    break;

                case WeatherType.Snow:
                    StartSnowEffect(intensity);
                    break;

                case WeatherType.Fog:
                    StartFogEffect(intensity);
                    break;

                case WeatherType.Storm:
                    StartStormEffect(intensity);
                    break;
            }
        }

        /// <summary>
        /// Остановить все эффекты погоды
        /// </summary>
        private void StopAllWeatherEffects()
        {
            if (rainParticles != null) rainParticles.Stop();
            if (snowParticles != null) snowParticles.Stop();
            if (fogParticles != null) fogParticles.Stop();
        }

        /// <summary>
        /// Запустить эффект дождя
        /// </summary>
        private void StartRainEffect(WeatherIntensity intensity)
        {
            if (rainParticles == null) return;

            var emission = rainParticles.emission;
            
            switch (intensity)
            {
                case WeatherIntensity.Light:
                    emission.rateOverTime = 50;
                    break;
                case WeatherIntensity.Moderate:
                    emission.rateOverTime = 150;
                    break;
                case WeatherIntensity.Heavy:
                    emission.rateOverTime = 300;
                    break;
                case WeatherIntensity.Extreme:
                    emission.rateOverTime = 500;
                    break;
            }

            rainParticles.Play();
        }

        /// <summary>
        /// Запустить эффект снега
        /// </summary>
        private void StartSnowEffect(WeatherIntensity intensity)
        {
            if (snowParticles == null) return;

            var emission = snowParticles.emission;
            
            switch (intensity)
            {
                case WeatherIntensity.Light:
                    emission.rateOverTime = 20;
                    break;
                case WeatherIntensity.Moderate:
                    emission.rateOverTime = 60;
                    break;
                case WeatherIntensity.Heavy:
                    emission.rateOverTime = 120;
                    break;
                case WeatherIntensity.Extreme:
                    emission.rateOverTime = 200;
                    break;
            }

            snowParticles.Play();
        }

        /// <summary>
        /// Запустить эффект тумана
        /// </summary>
        private void StartFogEffect(WeatherIntensity intensity)
        {
            if (fogParticles == null) return;

            var emission = fogParticles.emission;
            emission.rateOverTime = 30;
            
            // Изменить дальность видимости
            switch (intensity)
            {
                case WeatherIntensity.Light:
                    RenderSettings.fogDensity = 0.01f;
                    break;
                case WeatherIntensity.Moderate:
                    RenderSettings.fogDensity = 0.02f;
                    break;
                case WeatherIntensity.Heavy:
                    RenderSettings.fogDensity = 0.04f;
                    break;
                case WeatherIntensity.Extreme:
                    RenderSettings.fogDensity = 0.08f;
                    break;
            }

            RenderSettings.fog = true;
            fogParticles.Play();
        }

        /// <summary>
        /// Запустить эффект грозы
        /// </summary>
        private void StartStormEffect(WeatherIntensity intensity)
        {
            // Комбинировать дождь и молнии
            StartRainEffect(WeatherIntensity.Heavy);
            
            // TODO: Добавить эффекты молний
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик изменения времени
        /// </summary>
        private void OnTimeChanged(DateTime newTime)
        {
            UpdateVisualsFromTime();
        }

        /// <summary>
        /// Обработчик смены периода времени
        /// </summary>
        private void OnTimePeriodChanged(TimePeriod oldPeriod, TimePeriod newPeriod)
        {
            currentTimePeriod = newPeriod;
            
            if (showDebugInfo)
            {
                Debug.Log($"Визуальные эффекты: смена периода {oldPeriod} -> {newPeriod}");
            }
        }

        /// <summary>
        /// Обработчик изменения погоды
        /// </summary>
        private void OnWeatherChanged(WeatherType weather, WeatherIntensity intensity)
        {
            UpdateWeatherEffects(weather, intensity);
        }

        #endregion

        #region Default Gradients and Curves

        /// <summary>
        /// Создать градиент цвета солнца по умолчанию
        /// </summary>
        private Gradient CreateDefaultSunColorGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];

            // Ночь (синий)
            colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.8f), 0f);
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);

            // Рассвет (оранжевый)
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.25f);
            alphaKeys[1] = new GradientAlphaKey(1f, 0.25f);

            // День (желтый/белый)
            colorKeys[2] = new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.5f);
            alphaKeys[2] = new GradientAlphaKey(1f, 0.5f);

            // Закат (красный)
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.75f);
            alphaKeys[3] = new GradientAlphaKey(1f, 0.75f);

            // Ночь (синий)
            colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.8f), 1f);
            alphaKeys[4] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Создать кривую интенсивности солнца по умолчанию
        /// </summary>
        private AnimationCurve CreateDefaultSunIntensityCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0f);    // Полночь
            curve.AddKey(0.25f, 0.5f); // Рассвет
            curve.AddKey(0.5f, 1.5f);  // Полдень
            curve.AddKey(0.75f, 0.3f); // Закат
            curve.AddKey(1f, 0f);    // Полночь
            return curve;
        }

        /// <summary>
        /// Создать градиент окружающего света по умолчанию
        /// </summary>
        private Gradient CreateDefaultAmbientColorGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0f);   // Ночь
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);

            colorKeys[1] = new GradientColorKey(new Color(0.7f, 0.7f, 0.9f), 0.5f); // День
            alphaKeys[1] = new GradientAlphaKey(1f, 0.5f);

            colorKeys[2] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1f);   // Ночь
            alphaKeys[2] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Создать кривую интенсивности окружающего света по умолчанию
        /// </summary>
        private AnimationCurve CreateDefaultAmbientIntensityCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.2f);    // Ночь
            curve.AddKey(0.5f, 1f);    // День
            curve.AddKey(1f, 0.2f);    // Ночь
            return curve;
        }

        /// <summary>
        /// Создать градиент верхней части неба по умолчанию
        /// </summary>
        private Gradient CreateDefaultSkyTopColorGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

            colorKeys[0] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0f);  // Ночь
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);

            colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.5f);    // День
            alphaKeys[1] = new GradientAlphaKey(1f, 0.5f);

            colorKeys[2] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 1f);  // Ночь
            alphaKeys[2] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Создать градиент горизонта неба по умолчанию
        /// </summary>
        private Gradient CreateDefaultSkyHorizonColorGradient()
        {
            Gradient gradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.15f), 0f);   // Ночь
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);

            colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0.5f);    // День
            alphaKeys[1] = new GradientAlphaKey(1f, 0.5f);

            colorKeys[2] = new GradientColorKey(new Color(0.1f, 0.1f, 0.15f), 1f);   // Ночь
            alphaKeys[2] = new GradientAlphaKey(1f, 1f);

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Создать кривую яркости звезд по умолчанию
        /// </summary>
        private AnimationCurve CreateDefaultStarsBrightnessCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1.3f);    // Ночь - звезды видны
            curve.AddKey(0.3f, 0.5f);  // Рассвет - звезды тускнеют
            curve.AddKey(0.5f, 0.2f);  // День - звезды не видны
            curve.AddKey(0.7f, 0.5f);  // Закат - звезды появляются
            curve.AddKey(1f, 1.3f);    // Ночь - звезды видны
            return curve;
        }

        /// <summary>
        /// Создать кривую прозрачности облаков по умолчанию
        /// </summary>
        private AnimationCurve CreateDefaultCloudOpacityCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.3f);    // Ночь
            curve.AddKey(0.5f, 0.7f);  // День
            curve.AddKey(1f, 0.3f);    // Ночь
            return curve;
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Set Morning Visuals")]
        private void DebugSetMorningVisuals()
        {
            debugTimeOfDay = 0.33f; // 8:00
            overrideTimeOfDay = true;
        }

        [ContextMenu("Set Noon Visuals")]
        private void DebugSetNoonVisuals()
        {
            debugTimeOfDay = 0.5f; // 12:00
            overrideTimeOfDay = true;
        }

        [ContextMenu("Set Evening Visuals")]
        private void DebugSetEveningVisuals()
        {
            debugTimeOfDay = 0.75f; // 18:00
            overrideTimeOfDay = true;
        }

        [ContextMenu("Set Night Visuals")]
        private void DebugSetNightVisuals()
        {
            debugTimeOfDay = 0f; // 00:00
            overrideTimeOfDay = true;
        }

        [ContextMenu("Disable Time Override")]
        private void DebugDisableTimeOverride()
        {
            overrideTimeOfDay = false;
        }

        [ContextMenu("Test Rain Effect")]
        private void DebugTestRainEffect()
        {
            StartRainEffect(WeatherIntensity.Moderate);
        }

        [ContextMenu("Stop All Weather")]
        private void DebugStopAllWeather()
        {
            StopAllWeatherEffects();
        }

        #endregion
    }
}
