using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система эффектов локаций - применение постоянных и временных эффектов
    /// </summary>
    public class LocationEffects : MonoBehaviour
    {
        [Header("Настройки эффектов")]
        [Tooltip("Интервал применения постоянных эффектов (секунды)")]
        public float effectUpdateInterval = 10f;

        [Tooltip("Мультипликатор силы эффектов")]
        [Range(0.1f, 3.0f)]
        public float effectStrengthMultiplier = 1.0f;

        [Header("Визуальные эффекты")]
        [Tooltip("Система частиц для положительных эффектов")]
        public ParticleSystem positiveEffectsParticles;

        [Tooltip("Система частиц для отрицательных эффектов")]
        public ParticleSystem negativeEffectsParticles;

        [Tooltip("Звуки эффектов")]
        public AudioClip[] effectSounds;

        // Компоненты
        private LocationManager locationManager;
        private PlayerStats playerStats;
        private StatsDegradationManager degradationManager;
        private AudioSource audioSource;

        // Состояние
        private Coroutine effectUpdateCoroutine;
        private Dictionary<StatType, float> activeEffects = new Dictionary<StatType, float>();
        private LocationData currentLocationData;
        private float lastEffectTime;

        // Константы
        private const float EFFECT_AMOUNT_PER_INTERVAL = 2f; // Базовое количество восстановления за интервал

        #region Unity Methods

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            // Получение ссылок на системы
            locationManager = FindObjectOfType<LocationManager>();
            playerStats = GameManager.Instance?.PlayerStats;
            degradationManager = FindObjectOfType<StatsDegradationManager>();

            if (locationManager == null)
            {
                Debug.LogError("LocationManager не найден! LocationEffects не может работать.");
                return;
            }

            // Подписка на события
            GameEvents.OnLocationChanged += OnLocationChanged;

            // Запуск системы эффектов
            StartLocationEffects();
        }

        private void OnDestroy()
        {
            // Остановка корутин
            if (effectUpdateCoroutine != null)
            {
                StopCoroutine(effectUpdateCoroutine);
            }

            // Отписка от событий
            GameEvents.OnLocationChanged -= OnLocationChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Запустить систему эффектов локации
        /// </summary>
        public void StartLocationEffects()
        {
            if (effectUpdateCoroutine != null)
            {
                StopCoroutine(effectUpdateCoroutine);
            }

            currentLocationData = locationManager?.GetCurrentLocationData();
            if (currentLocationData != null)
            {
                UpdateActiveEffects();
                effectUpdateCoroutine = StartCoroutine(ApplyLocationEffectsRoutine());
            }
        }

        /// <summary>
        /// Остановить систему эффектов локации
        /// </summary>
        public void StopLocationEffects()
        {
            if (effectUpdateCoroutine != null)
            {
                StopCoroutine(effectUpdateCoroutine);
                effectUpdateCoroutine = null;
            }

            ClearActiveEffects();
        }

        /// <summary>
        /// Применить мгновенный эффект локации
        /// </summary>
        public void ApplyInstantLocationEffect(LocationData locationData, float multiplier = 1.0f)
        {
            if (locationData == null || playerStats == null) return;

            foreach (var modifier in locationData.statModifiers)
            {
                // Применяем только положительные мгновенные эффекты
                if (modifier.modifier > 1.0f)
                {
                    float amount = CalculateEffectAmount(modifier, multiplier);
                    playerStats.ModifyStat(modifier.statType, amount);

                    // Визуальные эффекты
                    PlayEffectVisuals(true);
                    PlayEffectSound();

                    Debug.Log($"Применен мгновенный эффект {modifier.statType}: +{amount:F1}");
                }
            }
        }

        /// <summary>
        /// Получить текущие активные эффекты
        /// </summary>
        public Dictionary<StatType, float> GetActiveEffects()
        {
            return new Dictionary<StatType, float>(activeEffects);
        }

        /// <summary>
        /// Проверить, активен ли эффект для характеристики
        /// </summary>
        public bool IsEffectActive(StatType statType)
        {
            return activeEffects.ContainsKey(statType) && activeEffects[statType] != 0f;
        }

        /// <summary>
        /// Получить силу эффекта для характеристики
        /// </summary>
        public float GetEffectStrength(StatType statType)
        {
            return activeEffects.ContainsKey(statType) ? activeEffects[statType] : 0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Корутина применения эффектов локации
        /// </summary>
        private IEnumerator ApplyLocationEffectsRoutine()
        {
            while (currentLocationData != null)
            {
                yield return new WaitForSeconds(effectUpdateInterval);

                ApplyPassiveLocationEffects();
                lastEffectTime = Time.time;
            }
        }

        /// <summary>
        /// Применить пассивные эффекты локации
        /// </summary>
        private void ApplyPassiveLocationEffects()
        {
            if (currentLocationData == null || playerStats == null) return;

            bool hasPositiveEffect = false;
            bool hasNegativeEffect = false;

            foreach (var modifier in currentLocationData.statModifiers)
            {
                if (Mathf.Approximately(modifier.modifier, 1.0f)) continue;

                float effectAmount = CalculateEffectAmount(modifier);
                
                if (effectAmount != 0)
                {
                    playerStats.ModifyStat(modifier.statType, effectAmount);

                    if (effectAmount > 0)
                        hasPositiveEffect = true;
                    else
                        hasNegativeEffect = true;

                    Debug.Log($"Пассивный эффект локации {modifier.statType}: {effectAmount:F1}");
                }
            }

            // Визуальные эффекты
            if (hasPositiveEffect)
                PlayEffectVisuals(true);
            if (hasNegativeEffect)
                PlayEffectVisuals(false);

            // Звуковые эффекты
            if (hasPositiveEffect || hasNegativeEffect)
                PlayEffectSound();

            // Применить модификатор деградации к системе деградации
            if (degradationManager != null)
            {
                degradationManager.SetLocationDegradationModifier(currentLocationData.degradationModifier);
            }
        }

        /// <summary>
        /// Рассчитать количество эффекта
        /// </summary>
        private float CalculateEffectAmount(LocationStatModifier modifier, float multiplier = 1.0f)
        {
            float baseAmount = EFFECT_AMOUNT_PER_INTERVAL * effectStrengthMultiplier * multiplier;

            if (modifier.isPercentage)
            {
                // Процентный модификатор
                float percentageEffect = (modifier.modifier - 1.0f); // Превращаем множитель в процент изменения
                return baseAmount * percentageEffect;
            }
            else
            {
                // Абсолютный модификатор
                return modifier.modifier * multiplier;
            }
        }

        /// <summary>
        /// Обновить активные эффекты
        /// </summary>
        private void UpdateActiveEffects()
        {
            activeEffects.Clear();

            if (currentLocationData == null) return;

            foreach (var modifier in currentLocationData.statModifiers)
            {
                if (!Mathf.Approximately(modifier.modifier, 1.0f))
                {
                    activeEffects[modifier.statType] = modifier.modifier;
                }
            }
        }

        /// <summary>
        /// Очистить активные эффекты
        /// </summary>
        private void ClearActiveEffects()
        {
            activeEffects.Clear();

            // Сбросить модификатор деградации
            if (degradationManager != null)
            {
                degradationManager.SetLocationDegradationModifier(1.0f);
            }
        }

        /// <summary>
        /// Воспроизвести визуальные эффекты
        /// </summary>
        private void PlayEffectVisuals(bool isPositive)
        {
            ParticleSystem targetParticles = isPositive ? positiveEffectsParticles : negativeEffectsParticles;
            
            if (targetParticles != null)
            {
                if (!targetParticles.isPlaying)
                {
                    targetParticles.Play();
                }
                else
                {
                    // Добавить больше частиц к уже проигрывающейся системе
                    targetParticles.Emit(10);
                }
            }
        }

        /// <summary>
        /// Воспроизвести звуковой эффект
        /// </summary>
        private void PlayEffectSound()
        {
            if (effectSounds != null && effectSounds.Length > 0 && audioSource != null)
            {
                AudioClip soundToPlay = effectSounds[Random.Range(0, effectSounds.Length)];
                if (soundToPlay != null)
                {
                    audioSource.PlayOneShot(soundToPlay, 0.3f); // Тихо, чтобы не мешать
                }
            }
        }

        /// <summary>
        /// Применить специальные эффекты локации
        /// </summary>
        private void ApplySpecialLocationEffects()
        {
            if (currentLocationData == null) return;

            switch (currentLocationData.locationType)
            {
                case LocationType.Park:
                    // Парк: медленное восстановление настроения и здоровья
                    ApplyParkEffects();
                    break;

                case LocationType.Slums:
                    // Трущобы: риск потери здоровья, но возможность найти деньги
                    ApplySlumEffects();
                    break;

                case LocationType.NightClub:
                    // Ночной клуб: быстрая потеря здоровья, но прирост денег
                    ApplyNightClubEffects();
                    break;

                case LocationType.BusinessDistrict:
                    // Бизнес-район: стресс (потеря настроения), но хороший заработок
                    ApplyBusinessDistrictEffects();
                    break;

                case LocationType.TrainStation:
                    // Вокзал: шум и суета влияют на настроение
                    ApplyTrainStationEffects();
                    break;
            }
        }

        /// <summary>
        /// Эффекты парка
        /// </summary>
        private void ApplyParkEffects()
        {
            // Парк восстанавливает настроение и немного здоровья
            if (Random.Range(0f, 1f) < 0.7f) // 70% шанс
            {
                playerStats.ModifyStat(StatType.Mood, 3f);
                playerStats.ModifyStat(StatType.Health, 1f);

                GameEvents.TriggerNotification("Прогулка в парке улучшила ваше настроение", NotificationType.Success);
            }
        }

        /// <summary>
        /// Эффекты трущоб
        /// </summary>
        private void ApplySlumEffects()
        {
            // Трущобы: риск потери здоровья
            if (Random.Range(0f, 1f) < 0.3f) // 30% шанс
            {
                playerStats.ModifyStat(StatType.Health, -2f);
                playerStats.ModifyStat(StatType.Mood, -1f);

                GameEvents.TriggerNotification("Опасная обстановка в трущобах повлияла на здоровье", NotificationType.Warning);
            }
            
            // Но иногда можно найти деньги
            else if (Random.Range(0f, 1f) < 0.1f) // 10% шанс
            {
                float foundMoney = Random.Range(10f, 50f);
                MoneySystem moneySystem = FindObjectOfType<MoneySystem>();
                if (moneySystem != null)
                {
                    moneySystem.AddMoney(foundMoney, transform.position);
                    GameEvents.TriggerNotification($"Нашли {GameUtils.FormatMoney(foundMoney)} в трущобах!", NotificationType.Success);
                }
            }
        }

        /// <summary>
        /// Эффекты ночного клуба
        /// </summary>
        private void ApplyNightClubEffects()
        {
            // Ночной клуб вреден для здоровья
            playerStats.ModifyStat(StatType.Health, -1.5f);
            
            // Но поднимает настроение
            if (Random.Range(0f, 1f) < 0.6f)
            {
                playerStats.ModifyStat(StatType.Mood, 2f);
            }
        }

        /// <summary>
        /// Эффекты бизнес-района
        /// </summary>
        private void ApplyBusinessDistrictEffects()
        {
            // Стресс от суеты бизнес-района
            if (Random.Range(0f, 1f) < 0.4f)
            {
                playerStats.ModifyStat(StatType.Mood, -1f);
                GameEvents.TriggerNotification("Суета бизнес-района действует на нервы", NotificationType.Info);
            }
        }

        /// <summary>
        /// Эффекты вокзала
        /// </summary>
        private void ApplyTrainStationEffects()
        {
            // Шум и суета вокзала
            if (Random.Range(0f, 1f) < 0.5f)
            {
                playerStats.ModifyStat(StatType.Mood, -0.5f);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType newLocation, LocationData locationData)
        {
            currentLocationData = locationData;
            
            // Перезапуск системы эффектов для новой локации
            StartLocationEffects();

            // Применить мгновенный эффект входа в локацию
            ApplyInstantLocationEffect(locationData, 0.5f); // Половина обычной силы для мгновенного эффекта
        }

        #endregion

        #region Public Integration Methods

        /// <summary>
        /// Получить модификатор деградации для текущей локации
        /// </summary>
        public float GetCurrentDegradationModifier()
        {
            return currentLocationData?.degradationModifier ?? 1.0f;
        }

        /// <summary>
        /// Получить модификатор заработка для текущей локации
        /// </summary>
        public float GetCurrentMoneyModifier()
        {
            return currentLocationData?.moneyModifier ?? 1.0f;
        }

        /// <summary>
        /// Применить случайное событие локации
        /// </summary>
        public void ApplyRandomLocationEvent()
        {
            if (currentLocationData == null) return;

            var randomEvent = currentLocationData.GetRandomEvent();
            if (randomEvent.HasValue)
            {
                var eventData = randomEvent.Value;

                // Применить эффекты события
                foreach (var effect in eventData.effects)
                {
                    float amount = CalculateEffectAmount(effect, 1.5f); // События сильнее обычных эффектов
                    playerStats.ModifyStat(effect.statType, amount);
                }

                // Применить награду деньгами
                if (eventData.moneyReward != 0)
                {
                    MoneySystem moneySystem = FindObjectOfType<MoneySystem>();
                    if (moneySystem != null)
                    {
                        if (eventData.moneyReward > 0)
                        {
                            moneySystem.AddMoney(eventData.moneyReward, transform.position);
                        }
                        else
                        {
                            moneySystem.SpendMoney(-eventData.moneyReward);
                        }
                    }
                }

                // Визуальные и звуковые эффекты
                PlayEffectVisuals(eventData.moneyReward >= 0);
                PlayEffectSound();

                Debug.Log($"Применено случайное событие: {eventData.eventName}");
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Применить эффекты локации")]
        private void TestApplyLocationEffects()
        {
            ApplyPassiveLocationEffects();
        }

        [ContextMenu("Применить случайное событие")]
        private void TestRandomEvent()
        {
            ApplyRandomLocationEvent();
        }

        [ContextMenu("Показать активные эффекты")]
        private void ShowActiveEffects()
        {
            Debug.Log("Активные эффекты локации:");
            foreach (var effect in activeEffects)
            {
                Debug.Log($"{effect.Key}: {effect.Value}");
            }
        }

        #endregion
    }
}
