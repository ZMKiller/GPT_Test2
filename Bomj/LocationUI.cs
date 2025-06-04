using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Интерфейс локаций - отображение информации о локациях и навигация
    /// </summary>
    public class LocationUI : MonoBehaviour
    {
        [Header("UI Элементы текущей локации")]
        [Tooltip("Текст названия текущей локации")]
        public TextMeshProUGUI currentLocationNameText;

        [Tooltip("Текст описания текущей локации")]
        public TextMeshProUGUI currentLocationDescriptionText;

        [Tooltip("Иконка текущей локации")]
        public Image currentLocationIcon;

        [Tooltip("Панель эффектов текущей локации")]
        public Transform locationEffectsPanel;

        [Tooltip("Префаб элемента эффекта")]
        public GameObject effectItemPrefab;

        [Header("Мини-карта локаций")]
        [Tooltip("Контейнер для кнопок локаций")]
        public Transform locationButtonsContainer;

        [Tooltip("Префаб кнопки локации")]
        public GameObject locationButtonPrefab;

        [Header("Детальная панель локации")]
        [Tooltip("Панель с подробной информацией о локации")]
        public GameObject locationDetailsPanel;

        [Tooltip("Текст названия в детальной панели")]
        public TextMeshProUGUI detailLocationNameText;

        [Tooltip("Текст описания в детальной панели")]
        public TextMeshProUGUI detailLocationDescriptionText;

        [Tooltip("Иконка в детальной панели")]
        public Image detailLocationIcon;

        [Tooltip("Текст требований доступа")]
        public TextMeshProUGUI accessRequirementsText;

        [Tooltip("Кнопка перехода")]
        public Button moveToLocationButton;

        [Tooltip("Текст стоимости перехода")]
        public TextMeshProUGUI transitionCostText;

        [Header("Анимация")]
        [Tooltip("Аниматор для переходов")]
        public Animator locationUIAnimator;

        [Tooltip("Звук нажатия кнопки")]
        public AudioClip buttonClickSound;

        [Tooltip("Звук открытия карты")]
        public AudioClip mapOpenSound;

        // Компоненты
        private LocationManager locationManager;
        private PlayerStats playerStats;
        private AudioSource audioSource;

        // Состояние
        private List<LocationButton> locationButtons = new List<LocationButton>();
        private LocationData selectedLocationData;
        private Dictionary<LocationType, GameObject> effectItems = new Dictionary<LocationType, GameObject>();

        // Цвета доступности
        private Color availableColor = Color.white;
        private Color lockedColor = Color.gray;
        private Color timeRestrictedColor = Color.yellow;
        private Color currentLocationColor = Color.green;

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

            if (locationManager == null)
            {
                Debug.LogError("LocationManager не найден! LocationUI не сможет работать.");
                return;
            }

            // Подписка на события
            GameEvents.OnLocationChanged += OnLocationChanged;
            GameEvents.OnLocationUnlocked += OnLocationUnlocked;
            GameEvents.OnTimeOfDayChanged += OnTimeOfDayChanged;

            // Инициализация UI
            InitializeLocationUI();
            UpdateCurrentLocationDisplay();

            // Скрыть детальную панель
            if (locationDetailsPanel != null)
                locationDetailsPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // Отписка от событий
            GameEvents.OnLocationChanged -= OnLocationChanged;
            GameEvents.OnLocationUnlocked -= OnLocationUnlocked;
            GameEvents.OnTimeOfDayChanged -= OnTimeOfDayChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Показать/скрыть карту локаций
        /// </summary>
        public void ToggleLocationMap()
        {
            bool isActive = locationButtonsContainer.gameObject.activeSelf;
            locationButtonsContainer.gameObject.SetActive(!isActive);

            if (!isActive)
            {
                // Обновить карту при открытии
                UpdateLocationButtons();
                PlaySound(mapOpenSound);

                if (locationUIAnimator != null)
                    locationUIAnimator.SetTrigger("ShowMap");
            }
            else
            {
                if (locationUIAnimator != null)
                    locationUIAnimator.SetTrigger("HideMap");
            }
        }

        /// <summary>
        /// Показать детальную информацию о локации
        /// </summary>
        public void ShowLocationDetails(LocationData locationData)
        {
            if (locationData == null) return;

            selectedLocationData = locationData;

            // Обновить информацию
            if (detailLocationNameText != null)
                detailLocationNameText.text = locationData.locationName;

            if (detailLocationDescriptionText != null)
                detailLocationDescriptionText.text = locationData.description;

            if (detailLocationIcon != null)
            {
                detailLocationIcon.sprite = locationData.locationIcon;
                detailLocationIcon.color = locationData.GetLocationColor();
            }

            // Обновить требования доступа
            UpdateAccessRequirements(locationData);

            // Обновить кнопку перехода
            UpdateMoveButton(locationData);

            // Показать панель
            if (locationDetailsPanel != null)
            {
                locationDetailsPanel.SetActive(true);
                
                if (locationUIAnimator != null)
                    locationUIAnimator.SetTrigger("ShowDetails");
            }
        }

        /// <summary>
        /// Скрыть детальную информацию о локации
        /// </summary>
        public void HideLocationDetails()
        {
            if (locationDetailsPanel != null)
            {
                if (locationUIAnimator != null)
                    locationUIAnimator.SetTrigger("HideDetails");
                else
                    locationDetailsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Попытаться переместиться в выбранную локацию
        /// </summary>
        public void TryMoveToSelectedLocation()
        {
            if (selectedLocationData != null && locationManager != null)
            {
                PlaySound(buttonClickSound);
                
                if (locationManager.TryMoveToLocation(selectedLocationData.locationType))
                {
                    HideLocationDetails();
                }
            }
        }

        /// <summary>
        /// Обновить отображение всех локаций
        /// </summary>
        public void RefreshLocationDisplay()
        {
            UpdateCurrentLocationDisplay();
            UpdateLocationButtons();
            UpdateLocationEffects();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Инициализировать UI локаций
        /// </summary>
        private void InitializeLocationUI()
        {
            CreateLocationButtons();
            UpdateLocationButtons();
            UpdateLocationEffects();
        }

        /// <summary>
        /// Создать кнопки для всех локаций
        /// </summary>
        private void CreateLocationButtons()
        {
            if (locationManager == null || locationButtonsContainer == null || locationButtonPrefab == null)
                return;

            // Очистить существующие кнопки
            foreach (Transform child in locationButtonsContainer)
            {
                Destroy(child.gameObject);
            }
            locationButtons.Clear();

            // Создать кнопки для всех локаций
            foreach (var locationData in locationManager.allLocations)
            {
                GameObject buttonObj = Instantiate(locationButtonPrefab, locationButtonsContainer);
                LocationButton locationButton = buttonObj.GetComponent<LocationButton>();

                if (locationButton == null)
                    locationButton = buttonObj.AddComponent<LocationButton>();

                locationButton.Initialize(locationData, this);
                locationButtons.Add(locationButton);
            }
        }

        /// <summary>
        /// Обновить отображение текущей локации
        /// </summary>
        private void UpdateCurrentLocationDisplay()
        {
            if (locationManager == null) return;

            LocationData currentLocation = locationManager.GetCurrentLocationData();
            if (currentLocation == null) return;

            // Обновить название
            if (currentLocationNameText != null)
                currentLocationNameText.text = currentLocation.locationName;

            // Обновить описание
            if (currentLocationDescriptionText != null)
                currentLocationDescriptionText.text = currentLocation.description;

            // Обновить иконку
            if (currentLocationIcon != null)
            {
                currentLocationIcon.sprite = currentLocation.locationIcon;
                currentLocationIcon.color = currentLocation.GetLocationColor();
            }
        }

        /// <summary>
        /// Обновить кнопки локаций
        /// </summary>
        private void UpdateLocationButtons()
        {
            if (locationManager == null) return;

            LocationType currentLocationType = locationManager.GetCurrentLocationType();

            foreach (var locationButton in locationButtons)
            {
                LocationAccessibility accessibility = locationManager.CheckLocationAccessibility(locationButton.LocationData.locationType);
                bool isCurrent = locationButton.LocationData.locationType == currentLocationType;

                locationButton.UpdateAccessibility(accessibility, isCurrent);
            }
        }

        /// <summary>
        /// Обновить отображение эффектов локации
        /// </summary>
        private void UpdateLocationEffects()
        {
            if (locationManager == null || locationEffectsPanel == null) return;

            // Очистить старые эффекты
            foreach (Transform child in locationEffectsPanel)
            {
                Destroy(child.gameObject);
            }
            effectItems.Clear();

            LocationData currentLocation = locationManager.GetCurrentLocationData();
            if (currentLocation == null) return;

            // Создать элементы эффектов
            CreateEffectItem("Заработок", $"x{currentLocation.moneyModifier:F1}", GetModifierColor(currentLocation.moneyModifier));
            CreateEffectItem("Деградация", $"x{currentLocation.degradationModifier:F1}", GetModifierColor(currentLocation.degradationModifier, true));

            // Добавить модификаторы характеристик
            foreach (var statModifier in currentLocation.statModifiers)
            {
                string statName = GetStatTypeName(statModifier.statType);
                string modifierText = statModifier.isPercentage ? 
                    $"x{statModifier.modifier:F1}" : 
                    $"{(statModifier.modifier >= 0 ? "+" : "")}{statModifier.modifier:F1}";
                
                CreateEffectItem(statName, modifierText, GetModifierColor(statModifier.modifier));
            }
        }

        /// <summary>
        /// Создать элемент эффекта
        /// </summary>
        private void CreateEffectItem(string effectName, string effectValue, Color effectColor)
        {
            if (effectItemPrefab == null) return;

            GameObject effectItem = Instantiate(effectItemPrefab, locationEffectsPanel);
            
            // Найти текстовые компоненты
            TextMeshProUGUI[] texts = effectItem.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = effectName;
                texts[1].text = effectValue;
                texts[1].color = effectColor;
            }
        }

        /// <summary>
        /// Обновить требования доступа
        /// </summary>
        private void UpdateAccessRequirements(LocationData locationData)
        {
            if (accessRequirementsText == null) return;

            LocationAccessibility accessibility = locationManager.CheckLocationAccessibility(locationData.locationType);
            
            switch (accessibility)
            {
                case LocationAccessibility.Available:
                    accessRequirementsText.text = "Доступна";
                    accessRequirementsText.color = Color.green;
                    break;

                case LocationAccessibility.Locked:
                    accessRequirementsText.text = $"Заблокировано:\n{locationData.GetAccessRequirementsDescription()}";
                    accessRequirementsText.color = Color.red;
                    break;

                case LocationAccessibility.TimeRestricted:
                    accessRequirementsText.text = "Недоступна в это время";
                    accessRequirementsText.color = Color.yellow;
                    break;

                case LocationAccessibility.Closed:
                    accessRequirementsText.text = "Навсегда закрыта";
                    accessRequirementsText.color = Color.gray;
                    break;
            }
        }

        /// <summary>
        /// Обновить кнопку перехода
        /// </summary>
        private void UpdateMoveButton(LocationData locationData)
        {
            if (moveToLocationButton == null) return;

            LocationAccessibility accessibility = locationManager.CheckLocationAccessibility(locationData.locationType);
            bool isCurrentLocation = locationManager.GetCurrentLocationType() == locationData.locationType;
            
            moveToLocationButton.interactable = accessibility == LocationAccessibility.Available && !isCurrentLocation;

            // Обновить текст стоимости
            if (transitionCostText != null)
            {
                if (locationData.transitionCost > 0)
                {
                    transitionCostText.text = $"Стоимость: {GameUtils.FormatMoney(locationData.transitionCost)}";
                    
                    // Проверить, хватает ли денег
                    bool canAfford = playerStats != null && playerStats.Money >= locationData.transitionCost;
                    transitionCostText.color = canAfford ? Color.white : Color.red;
                }
                else
                {
                    transitionCostText.text = "Бесплатно";
                    transitionCostText.color = Color.green;
                }
            }
        }

        /// <summary>
        /// Получить цвет модификатора
        /// </summary>
        private Color GetModifierColor(float modifier, bool inverse = false)
        {
            bool isPositive = inverse ? modifier < 1.0f : modifier > 1.0f;
            
            if (Mathf.Approximately(modifier, 1.0f))
                return Color.gray;
            
            return isPositive ? Color.green : Color.red;
        }

        /// <summary>
        /// Получить локализованное название типа характеристики
        /// </summary>
        private string GetStatTypeName(StatType statType)
        {
            switch (statType)
            {
                case StatType.Health: return "Здоровье";
                case StatType.Hunger: return "Голод";
                case StatType.Mood: return "Настроение";
                case StatType.Money: return "Деньги";
                default: return statType.ToString();
            }
        }

        /// <summary>
        /// Воспроизвести звук
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType newLocation, LocationData locationData)
        {
            UpdateCurrentLocationDisplay();
            UpdateLocationButtons();
            UpdateLocationEffects();
        }

        /// <summary>
        /// Обработчик разблокировки локации
        /// </summary>
        private void OnLocationUnlocked(LocationType locationType)
        {
            UpdateLocationButtons();
        }

        /// <summary>
        /// Обработчик смены времени дня
        /// </summary>
        private void OnTimeOfDayChanged(TimeOfDay newTime)
        {
            UpdateLocationButtons();
        }

        #endregion

        #region Context Menu

        [ContextMenu("Обновить UI локаций")]
        private void RefreshUI()
        {
            RefreshLocationDisplay();
        }

        [ContextMenu("Показать все локации")]
        private void ShowAllLocations()
        {
            ToggleLocationMap();
        }

        #endregion
    }

    /// <summary>
    /// Компонент кнопки локации
    /// </summary>
    public class LocationButton : MonoBehaviour
    {
        [Header("UI Элементы")]
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public GameObject lockedIndicator;
        public GameObject currentIndicator;

        // Свойства
        public LocationData LocationData { get; private set; }
        private LocationUI parentUI;

        /// <summary>
        /// Инициализировать кнопку локации
        /// </summary>
        public void Initialize(LocationData locationData, LocationUI parent)
        {
            LocationData = locationData;
            parentUI = parent;

            // Получить компоненты если не заданы
            if (button == null) button = GetComponent<Button>();
            if (iconImage == null) iconImage = GetComponentInChildren<Image>();
            if (nameText == null) nameText = GetComponentInChildren<TextMeshProUGUI>();

            // Настроить отображение
            if (iconImage != null) iconImage.sprite = locationData.locationIcon;
            if (nameText != null) nameText.text = locationData.locationName;

            // Настроить обработчик нажатия
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => parentUI.ShowLocationDetails(locationData));
            }
        }

        /// <summary>
        /// Обновить отображение доступности
        /// </summary>
        public void UpdateAccessibility(LocationAccessibility accessibility, bool isCurrent)
        {
            if (button == null) return;

            // Обновить интерактивность
            button.interactable = accessibility == LocationAccessibility.Available;

            // Обновить цвет
            Color buttonColor = Color.white;
            switch (accessibility)
            {
                case LocationAccessibility.Available:
                    buttonColor = isCurrent ? Color.green : Color.white;
                    break;
                case LocationAccessibility.Locked:
                    buttonColor = Color.gray;
                    break;
                case LocationAccessibility.TimeRestricted:
                    buttonColor = Color.yellow;
                    break;
                case LocationAccessibility.Closed:
                    buttonColor = Color.red;
                    break;
            }

            if (iconImage != null)
                iconImage.color = buttonColor;

            // Обновить индикаторы
            if (lockedIndicator != null)
                lockedIndicator.SetActive(accessibility == LocationAccessibility.Locked);

            if (currentIndicator != null)
                currentIndicator.SetActive(isCurrent);
        }
    }
}
