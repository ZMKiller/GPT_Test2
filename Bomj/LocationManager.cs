using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер локаций - управляет переходами между локациями и их эффектами
    /// </summary>
    public class LocationManager : MonoBehaviour
    {
        [Header("Настройки локаций")]
        [Tooltip("Все доступные локации в игре")]
        public LocationData[] allLocations;

        [Tooltip("Стартовая локация")]
        public LocationType startingLocation = LocationType.Street;

        [Tooltip("Время перехода по умолчанию")]
        public float defaultTransitionTime = 3f;

        [Header("Эффекты переходов")]
        [Tooltip("Аниматор для переходов между локациями")]
        public Animator transitionAnimator;

        [Tooltip("Звук перехода между локациями")]
        public AudioClip transitionSound;

        // Компоненты
        private AudioSource audioSource;
        private PlayerStats playerStats;
        private NPCManager npcManager;
        private MoneySystem moneySystem;

        // Состояние
        private LocationData currentLocationData;
        private LocationType currentLocationType;
        private HashSet<LocationType> unlockedLocations;
        private bool isTransitioning = false;
        private float transitionStartTime;
        private float lastRandomEventCheck;

        // События
        public event Action<LocationType, LocationType> OnLocationChanged;
        public event Action<LocationType> OnLocationUnlocked;

        // Константы
        private const float RANDOM_EVENT_CHECK_INTERVAL = 30f; // Проверка случайных событий каждые 30 секунд
        private const string UNLOCKED_LOCATIONS_KEY = "UnlockedLocations";
        private const string CURRENT_LOCATION_KEY = "CurrentLocation";

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Инициализация множества разблокированных локаций
            unlockedLocations = new HashSet<LocationType>();
        }

        private void Start()
        {
            // Получение ссылок на системы
            playerStats = GameManager.Instance.PlayerStats;
            npcManager = FindObjectOfType<NPCManager>();
            moneySystem = FindObjectOfType<MoneySystem>();

            // Загрузка сохраненных данных
            LoadLocationData();

            // Установка стартовой локации если нет сохраненной
            if (currentLocationData == null)
            {
                SetStartingLocation();
            }

            // Подписка на события
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeOfDayChanged += OnTimeOfDayChanged;
            }

            // Применение эффектов текущей локации
            ApplyLocationEffects();

            lastRandomEventCheck = Time.time;
        }

        private void Update()
        {
            // Обновление перехода
            if (isTransitioning)
            {
                UpdateTransition();
            }

            // Проверка случайных событий
            if (Time.time - lastRandomEventCheck >= RANDOM_EVENT_CHECK_INTERVAL)
            {
                CheckRandomEvents();
                lastRandomEventCheck = Time.time;
            }
        }

        private void OnDestroy()
        {
            // Отписка от событий
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTimeOfDayChanged -= OnTimeOfDayChanged;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Попытаться переместиться в указанную локацию
        /// </summary>
        public bool TryMoveToLocation(LocationType locationType)
        {
            // Проверка, не находимся ли уже в этой локации
            if (currentLocationType == locationType)
            {
                Debug.Log($"Уже находимся в локации: {locationType}");
                return false;
            }

            // Проверка перехода
            if (isTransitioning)
            {
                Debug.Log("Переход уже выполняется");
                return false;
            }

            // Получение данных локации
            LocationData targetLocation = GetLocationData(locationType);
            if (targetLocation == null)
            {
                Debug.LogError($"Данные для локации {locationType} не найдены");
                return false;
            }

            // Проверка доступности
            LocationAccessibility accessibility = CheckLocationAccessibility(locationType);
            if (accessibility != LocationAccessibility.Available)
            {
                HandleInaccessibleLocation(locationType, accessibility, targetLocation);
                return false;
            }

            // Проверка стоимости перехода
            if (targetLocation.transitionCost > 0)
            {
                if (!moneySystem.CanSpend(targetLocation.transitionCost))
                {
                    GameEvents.TriggerNotification(
                        $"Недостаточно денег для перехода в {targetLocation.locationName}. Нужно: {GameUtils.FormatMoney(targetLocation.transitionCost)}",
                        NotificationType.Warning
                    );
                    return false;
                }

                // Списание денег за переход
                moneySystem.SpendMoney(targetLocation.transitionCost);
            }

            // Начать переход
            StartTransition(locationType, targetLocation);
            return true;
        }

        /// <summary>
        /// Получить данные текущей локации
        /// </summary>
        public LocationData GetCurrentLocationData()
        {
            return currentLocationData;
        }

        public Location CurrentLocation
        {
            get
            {
                if (Enum.TryParse<Location>(currentLocationType.ToString(), out var loc))
                    return loc;
                return Location.Street;
            }
        }

        /// <summary>
        /// Получить тип текущей локации
        /// </summary>
        public LocationType GetCurrentLocationType()
        {
            return currentLocationType;
        }

        /// <summary>
        /// Проверить доступность локации
        /// </summary>
        public LocationAccessibility CheckLocationAccessibility(LocationType locationType)
        {
            LocationData locationData = GetLocationData(locationType);
            if (locationData == null)
                return LocationAccessibility.Closed;

            TimeOfDay currentTime = GameManager.Instance?.GetCurrentTimeOfDay() ?? TimeOfDay.Day;
            return locationData.CheckAccessibility(playerStats, currentTime, unlockedLocations);
        }

        /// <summary>
        /// Получить все доступные локации
        /// </summary>
        public List<LocationData> GetAvailableLocations()
        {
            var availableLocations = new List<LocationData>();
            
            foreach (var location in allLocations)
            {
                LocationAccessibility accessibility = CheckLocationAccessibility(location.locationType);
                if (accessibility == LocationAccessibility.Available)
                {
                    availableLocations.Add(location);
                }
            }

            return availableLocations;
        }

        /// <summary>
        /// Получить все разблокированные локации
        /// </summary>
        public HashSet<LocationType> GetUnlockedLocations()
        {
            return new HashSet<LocationType>(unlockedLocations);
        }

        /// <summary>
        /// Установить доступность локации (используется системой времени)
        /// </summary>
        public void SetLocationAvailable(Location location, bool available)
        {
            var data = GetLocationData((LocationType)location);
            if (data != null)
            {
                data.transitionCost = available ? data.transitionCost : float.MaxValue;
            }
        }

        /// <summary>
        /// Разблокировать локацию
        /// </summary>
        public void UnlockLocation(LocationType locationType)
        {
            if (!unlockedLocations.Contains(locationType))
            {
                unlockedLocations.Add(locationType);
                SaveLocationData();

                LocationData locationData = GetLocationData(locationType);
                if (locationData != null)
                {
                    GameEvents.TriggerNotification(
                        $"Открыта новая локация: {locationData.locationName}!",
                        NotificationType.Achievement
                    );
                }

                OnLocationUnlocked?.Invoke(locationType);
                GameEvents.TriggerLocationUnlocked(locationType);
            }
        }

        /// <summary>
        /// Получить модификатор заработка для текущей локации
        /// </summary>
        public float GetCurrentMoneyModifier()
        {
            return currentLocationData?.moneyModifier ?? 1.0f;
        }

        /// <summary>
        /// Получить модификатор деградации для текущей локации
        /// </summary>
        public float GetCurrentDegradationModifier()
        {
            return currentLocationData?.degradationModifier ?? 1.0f;
        }

        /// <summary>
        /// Получить модификатор характеристики для текущей локации
        /// </summary>
        public float GetCurrentStatModifier(StatType statType)
        {
            return currentLocationData?.GetStatModifier(statType) ?? 1.0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Установить стартовую локацию
        /// </summary>
        private void SetStartingLocation()
        {
            // Разблокировать стартовую локацию
            unlockedLocations.Add(startingLocation);

            // Установить как текущую
            LocationData startingLocationData = GetLocationData(startingLocation);
            if (startingLocationData != null)
            {
                currentLocationType = startingLocation;
                currentLocationData = startingLocationData;
                
                // Сохранить данные
                SaveLocationData();

                // Уведомить об изменении
                OnLocationChanged?.Invoke(LocationType.Street, currentLocationType); // Фиктивная предыдущая локация
                GameEvents.TriggerLocationChanged(currentLocationType, startingLocationData);
            }
        }

        /// <summary>
        /// Получить данные локации по типу
        /// </summary>
        private LocationData GetLocationData(LocationType locationType)
        {
            foreach (var location in allLocations)
            {
                if (location.locationType == locationType)
                    return location;
            }
            return null;
        }

        /// <summary>
        /// Начать переход в новую локацию
        /// </summary>
        private void StartTransition(LocationType targetLocationType, LocationData targetLocationData)
        {
            LocationType previousLocation = currentLocationType;

            // Начать переход
            isTransitioning = true;
            transitionStartTime = Time.time;

            // Воспроизвести звук перехода
            if (transitionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(transitionSound);
            }

            // Запустить анимацию перехода
            if (transitionAnimator != null)
            {
                transitionAnimator.SetTrigger("StartTransition");
            }

            // Обновить состояние
            currentLocationType = targetLocationType;
            currentLocationData = targetLocationData;

            // Уведомить о начале перехода
            GameEvents.TriggerNotification(
                $"Переходим в локацию: {targetLocationData.locationName}",
                NotificationType.Info
            );

            // Эффекты перехода на характеристики
            ApplyTransitionEffects(targetLocationData);

            // Сохранить данные
            SaveLocationData();

            // Уведомить об изменении локации
            OnLocationChanged?.Invoke(previousLocation, currentLocationType);
            GameEvents.TriggerLocationChanged(currentLocationType, currentLocationData);
        }

        /// <summary>
        /// Обновить состояние перехода
        /// </summary>
        private void UpdateTransition()
        {
            float transitionDuration = currentLocationData?.transitionTime ?? defaultTransitionTime;
            float elapsedTime = Time.time - transitionStartTime;

            if (elapsedTime >= transitionDuration)
            {
                // Завершить переход
                CompleteTransition();
            }
        }

        /// <summary>
        /// Завершить переход между локациями
        /// </summary>
        private void CompleteTransition()
        {
            isTransitioning = false;

            // Применить эффекты новой локации
            ApplyLocationEffects();

            // Уведомить об успешном переходе
            GameEvents.TriggerNotification(
                $"Прибыли в {currentLocationData.locationName}",
                NotificationType.Success
            );

            // Остановить анимацию перехода
            if (transitionAnimator != null)
            {
                transitionAnimator.SetTrigger("EndTransition");
            }

            // Обновить NPC менеджер
            if (npcManager != null)
            {
                npcManager.OnLocationChanged(currentLocationType);
            }

            // Проверить разблокировку новых локаций
            CheckLocationUnlocks();
        }

        /// <summary>
        /// Применить эффекты текущей локации
        /// </summary>
        private void ApplyLocationEffects()
        {
            if (currentLocationData == null) return;

            // Применить модификаторы характеристик (только положительные эффекты)
            foreach (var modifier in currentLocationData.statModifiers)
            {
                if (modifier.modifier > 1.0f) // Только улучшения
                {
                    float amount = modifier.isPercentage ? 
                        playerStats.GetMaxValue(modifier.statType) * (modifier.modifier - 1.0f) :
                        modifier.modifier;

                    playerStats.ModifyStat(modifier.statType, amount);
                }
            }
        }

        /// <summary>
        /// Применить эффекты перехода (усталость, время и т.д.)
        /// </summary>
        private void ApplyTransitionEffects(LocationData targetLocation)
        {
            float transitionTime = targetLocation?.transitionTime ?? defaultTransitionTime;

            // Усталость от перехода (снижение настроения и здоровья)
            float fatigueAmount = transitionTime * 0.5f; // 0.5 единицы усталости за секунду
            playerStats.ModifyStat(StatType.Mood, -fatigueAmount);
            playerStats.ModifyStat(StatType.Health, -fatigueAmount * 0.3f);

            // Увеличение голода от времени перехода
            float hungerIncrease = transitionTime * 0.3f;
            playerStats.ModifyStat(StatType.Hunger, hungerIncrease);
        }

        /// <summary>
        /// Обработка недоступной локации
        /// </summary>
        private void HandleInaccessibleLocation(LocationType locationType, LocationAccessibility accessibility, LocationData locationData)
        {
            string message = "";
            NotificationType notificationType = NotificationType.Warning;

            switch (accessibility)
            {
                case LocationAccessibility.Locked:
                    message = $"Локация {locationData.locationName} заблокирована.\nТребования:\n{locationData.GetAccessRequirementsDescription()}";
                    break;

                case LocationAccessibility.TimeRestricted:
                    message = $"Локация {locationData.locationName} сейчас недоступна по времени";
                    break;

                case LocationAccessibility.Closed:
                    message = $"Локация {locationData.locationName} навсегда закрыта";
                    notificationType = NotificationType.Danger;
                    break;
            }

            GameEvents.TriggerNotification(message, notificationType);
        }

        /// <summary>
        /// Проверить разблокировку новых локаций
        /// </summary>
        private void CheckLocationUnlocks()
        {
            foreach (var location in allLocations)
            {
                if (!unlockedLocations.Contains(location.locationType))
                {
                    LocationAccessibility accessibility = CheckLocationAccessibility(location.locationType);
                    if (accessibility == LocationAccessibility.Available)
                    {
                        UnlockLocation(location.locationType);
                    }
                }
            }
        }

        /// <summary>
        /// Проверить случайные события
        /// </summary>
        private void CheckRandomEvents()
        {
            if (currentLocationData == null || isTransitioning) return;

            var randomEvent = currentLocationData.GetRandomEvent();
            if (randomEvent.HasValue)
            {
                ExecuteRandomEvent(randomEvent.Value);
            }
        }

        /// <summary>
        /// Выполнить случайное событие
        /// </summary>
        private void ExecuteRandomEvent(LocationData.RandomEvent randomEvent)
        {
            // Применить эффекты события
            foreach (var effect in randomEvent.effects)
            {
                float amount = effect.isPercentage ?
                    playerStats.GetMaxValue(effect.statType) * effect.modifier :
                    effect.modifier;

                playerStats.ModifyStat(effect.statType, amount);
            }

            // Применить награду деньгами
            if (randomEvent.moneyReward != 0)
            {
                if (randomEvent.moneyReward > 0)
                {
                    moneySystem.AddMoney(randomEvent.moneyReward, transform.position);
                }
                else
                {
                    moneySystem.SpendMoney(-randomEvent.moneyReward);
                }
            }

            // Показать уведомление о событии
            NotificationType notificationType = randomEvent.moneyReward >= 0 ? 
                NotificationType.Info : NotificationType.Warning;

            GameEvents.TriggerNotification(
                $"{randomEvent.eventName}\n{randomEvent.description}",
                notificationType
            );

            Debug.Log($"Выполнено случайное событие: {randomEvent.eventName} в локации {currentLocationData.locationName}");
        }

        /// <summary>
        /// Обработчик изменения времени дня
        /// </summary>
        private void OnTimeOfDayChanged(TimeOfDay newTime)
        {
            // Проверить, доступна ли текущая локация в новое время
            LocationAccessibility accessibility = CheckLocationAccessibility(currentLocationType);
            
            if (accessibility == LocationAccessibility.TimeRestricted)
            {
                // Принудительно переместить в доступную локацию
                var availableLocations = GetAvailableLocations();
                if (availableLocations.Count > 0)
                {
                    LocationData fallbackLocation = availableLocations.FirstOrDefault(l => l.locationType == LocationType.Street) 
                                                   ?? availableLocations[0];
                    
                    GameEvents.TriggerNotification(
                        $"Локация {currentLocationData.locationName} закрыта в {GetTimeOfDayName(newTime)}. Перемещаемся в {fallbackLocation.locationName}",
                        NotificationType.Warning
                    );

                    StartTransition(fallbackLocation.locationType, fallbackLocation);
                }
            }

            // Проверить разблокировку новых локаций
            CheckLocationUnlocks();
        }

        /// <summary>
        /// Получить локализованное название времени дня
        /// </summary>
        private string GetTimeOfDayName(TimeOfDay time)
        {
            switch (time)
            {
                case TimeOfDay.Morning: return "утром";
                case TimeOfDay.Day: return "днем";
                case TimeOfDay.Evening: return "вечером";
                case TimeOfDay.Night: return "ночью";
                default: return time.ToString();
            }
        }

        /// <summary>
        /// Сохранить данные о локациях
        /// </summary>
        private void SaveLocationData()
        {
            // Сохранить текущую локацию
            PlayerPrefs.SetString(CURRENT_LOCATION_KEY, currentLocationType.ToString());

            // Сохранить разблокированные локации
            string unlockedLocationsList = string.Join(",", unlockedLocations.Select(l => l.ToString()));
            PlayerPrefs.SetString(UNLOCKED_LOCATIONS_KEY, unlockedLocationsList);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Загрузить данные о локациях
        /// </summary>
        private void LoadLocationData()
        {
            // Загрузить разблокированные локации
            string unlockedLocationsList = PlayerPrefs.GetString(UNLOCKED_LOCATIONS_KEY, "");
            if (!string.IsNullOrEmpty(unlockedLocationsList))
            {
                string[] locationNames = unlockedLocationsList.Split(',');
                foreach (string locationName in locationNames)
                {
                    if (System.Enum.TryParse<LocationType>(locationName, out LocationType locationType))
                    {
                        unlockedLocations.Add(locationType);
                    }
                }
            }

            // Загрузить текущую локацию
            string currentLocationString = PlayerPrefs.GetString(CURRENT_LOCATION_KEY, "");
            if (!string.IsNullOrEmpty(currentLocationString) && 
                System.Enum.TryParse<LocationType>(currentLocationString, out LocationType currentType))
            {
                currentLocationType = currentType;
                currentLocationData = GetLocationData(currentType);
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Разблокировать все локации")]
        private void UnlockAllLocations()
        {
            foreach (var location in allLocations)
            {
                UnlockLocation(location.locationType);
            }
        }

        [ContextMenu("Сброс локаций")]
        private void ResetLocations()
        {
            unlockedLocations.Clear();
            SetStartingLocation();
            Debug.Log("Локации сброшены к начальному состоянию");
        }

        [ContextMenu("Тестовое случайное событие")]
        private void TriggerTestRandomEvent()
        {
            CheckRandomEvents();
        }

        #endregion
    }
}
