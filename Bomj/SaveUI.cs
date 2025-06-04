using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Пользовательский интерфейс системы сохранений
    /// </summary>
    public class SaveUI : MonoBehaviour
    {
        [Header("Основные панели")]
        [Tooltip("Главная панель выбора слотов")]
        public GameObject mainPanel;

        [Tooltip("Панель создания нового сохранения")]
        public GameObject newSavePanel;

        [Tooltip("Панель подтверждения действий")]
        public GameObject confirmationPanel;

        [Tooltip("Панель загрузки")]
        public GameObject loadingPanel;

        [Header("Список слотов")]
        [Tooltip("Контейнер для элементов слотов")]
        public Transform slotsContainer;

        [Tooltip("Префаб элемента слота")]
        public GameObject slotItemPrefab;

        [Tooltip("Кнопка создания нового сохранения")]
        public Button newGameButton;

        [Tooltip("Кнопка обновления списка")]
        public Button refreshButton;

        [Header("Создание нового сохранения")]
        [Tooltip("Поле ввода имени сохранения")]
        public TMP_InputField saveNameInput;

        [Tooltip("Кнопка создания")]
        public Button createButton;

        [Tooltip("Кнопка отмены создания")]
        public Button cancelCreateButton;

        [Header("Подтверждение")]
        [Tooltip("Текст сообщения подтверждения")]
        public TextMeshProUGUI confirmationText;

        [Tooltip("Кнопка подтверждения")]
        public Button confirmButton;

        [Tooltip("Кнопка отмены")]
        public Button cancelButton;

        [Header("Загрузка")]
        [Tooltip("Текст состояния загрузки")]
        public TextMeshProUGUI loadingText;

        [Tooltip("Прогресс-бар загрузки")]
        public Slider loadingProgressBar;

        [Header("Автосохранение")]
        [Tooltip("Индикатор автосохранения")]
        public GameObject autoSaveIndicator;

        [Tooltip("Текст индикатора автосохранения")]
        public TextMeshProUGUI autoSaveText;

        [Tooltip("Иконка автосохранения")]
        public Image autoSaveIcon;

        [Header("Настройки")]
        [Tooltip("Максимальная длина имени сохранения")]
        public int maxSaveNameLength = 30;

        [Tooltip("Анимация переходов")]
        public Animator panelAnimator;

        [Tooltip("Звуки интерфейса")]
        public AudioClip buttonClickSound;
        public AudioClip saveSuccessSound;
        public AudioClip loadSuccessSound;
        public AudioClip errorSound;

        // Компоненты
        private SaveSystem saveSystem;
        private SaveSlotManager slotManager;
        private AutoSaveManager autoSaveManager;
        private AudioSource audioSource;

        // Состояние
        private List<SaveSlotItem> slotItems = new List<SaveSlotItem>();
        private int selectedSlotIndex = -1;
        private Action<bool> currentConfirmationAction;
        private SaveSlotInfo pendingSlotInfo;

        // События
        public event Action<int> OnSlotSelected;
        public event Action<int> OnSlotLoaded;
        public event Action<int, string> OnSlotCreated;
        public event Action<int> OnSlotDeleted;

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
            saveSystem = SaveSystem.Instance;
            slotManager = FindObjectOfType<SaveSlotManager>();
            autoSaveManager = FindObjectOfType<AutoSaveManager>();

            if (saveSystem == null)
            {
                Debug.LogError("SaveUI: SaveSystem не найден!");
                enabled = false;
                return;
            }

            // Инициализация UI
            InitializeUI();

            // Подписка на события
            SubscribeToEvents();

            // Загрузка списка слотов
            RefreshSlotsList();

            Debug.Log("SaveUI инициализирован");
        }

        private void OnDestroy()
        {
            // Отписка от событий
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализировать пользовательский интерфейс
        /// </summary>
        private void InitializeUI()
        {
            // Настройка кнопок
            if (newGameButton != null)
                newGameButton.onClick.AddListener(ShowNewGamePanel);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshSlotsList);

            if (createButton != null)
                createButton.onClick.AddListener(CreateNewSave);

            if (cancelCreateButton != null)
                cancelCreateButton.onClick.AddListener(HideNewGamePanel);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmAction);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelAction);

            // Настройка поля ввода
            if (saveNameInput != null)
            {
                saveNameInput.characterLimit = maxSaveNameLength;
                saveNameInput.onValueChanged.AddListener(OnSaveNameChanged);
            }

            // Скрыть панели при запуске
            if (newSavePanel != null) newSavePanel.SetActive(false);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (autoSaveIndicator != null) autoSaveIndicator.SetActive(false);
        }

        /// <summary>
        /// Подписаться на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (saveSystem != null)
            {
                saveSystem.OnSaveCompleted += OnSaveCompleted;
                saveSystem.OnLoadCompleted += OnLoadCompleted;
            }

            if (slotManager != null)
            {
                slotManager.OnSlotsUpdated += OnSlotsUpdated;
                slotManager.OnSlotDeleted += OnSlotDeletedHandler;
            }

            if (autoSaveManager != null)
            {
                autoSaveManager.OnAutoSaveStarted += OnAutoSaveStarted;
                autoSaveManager.OnAutoSaveCompleted += OnAutoSaveCompleted;
            }
        }

        /// <summary>
        /// Отписаться от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (saveSystem != null)
            {
                saveSystem.OnSaveCompleted -= OnSaveCompleted;
                saveSystem.OnLoadCompleted -= OnLoadCompleted;
            }

            if (slotManager != null)
            {
                slotManager.OnSlotsUpdated -= OnSlotsUpdated;
                slotManager.OnSlotDeleted -= OnSlotDeletedHandler;
            }

            if (autoSaveManager != null)
            {
                autoSaveManager.OnAutoSaveStarted -= OnAutoSaveStarted;
                autoSaveManager.OnAutoSaveCompleted -= OnAutoSaveCompleted;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Показать UI сохранений
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(true);
            
            RefreshSlotsList();
            
            if (panelAnimator != null)
                panelAnimator.SetTrigger("Show");
        }

        /// <summary>
        /// Скрыть UI сохранений
        /// </summary>
        public void Hide()
        {
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger("Hide");
                // Деактивация будет через анимацию
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Обновить список слотов
        /// </summary>
        public void RefreshSlotsList()
        {
            if (slotManager == null) return;

            List<SaveSlotInfo> slots = slotManager.GetAllSlots();
            UpdateSlotsList(slots);
        }

        /// <summary>
        /// Выбрать слот
        /// </summary>
        public void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            
            // Обновить визуальное состояние слотов
            for (int i = 0; i < slotItems.Count; i++)
            {
                slotItems[i].SetSelected(i == slotIndex);
            }

            OnSlotSelected?.Invoke(slotIndex);
            PlaySound(buttonClickSound);
        }

        /// <summary>
        /// Загрузить выбранный слот
        /// </summary>
        public void LoadSelectedSlot()
        {
            if (selectedSlotIndex == -1) return;

            SaveSlotInfo slot = slotManager.GetSlot(selectedSlotIndex);
            if (slot == null || slot.isEmpty) return;

            if (slot.isCorrupted)
            {
                ShowConfirmation(
                    "Сохранение повреждено. Попробовать загрузить?",
                    (confirmed) => {
                        if (confirmed) LoadSlot(selectedSlotIndex);
                    }
                );
                return;
            }

            LoadSlot(selectedSlotIndex);
        }

        /// <summary>
        /// Удалить выбранный слот
        /// </summary>
        public void DeleteSelectedSlot()
        {
            if (selectedSlotIndex == -1) return;

            SaveSlotInfo slot = slotManager.GetSlot(selectedSlotIndex);
            if (slot == null || slot.isEmpty) return;

            ShowConfirmation(
                $"Удалить сохранение '{slot.saveName}'? Это действие нельзя отменить.",
                (confirmed) => {
                    if (confirmed) DeleteSlot(selectedSlotIndex);
                }
            );
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Обновить список слотов в UI
        /// </summary>
        private void UpdateSlotsList(List<SaveSlotInfo> slots)
        {
            // Очистить существующие элементы
            foreach (SaveSlotItem item in slotItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            slotItems.Clear();

            // Создать новые элементы
            for (int i = 0; i < slots.Count; i++)
            {
                CreateSlotItem(i, slots[i]);
            }
        }

        /// <summary>
        /// Создать элемент слота
        /// </summary>
        private void CreateSlotItem(int slotIndex, SaveSlotInfo slotInfo)
        {
            if (slotItemPrefab == null || slotsContainer == null) return;

            GameObject itemObject = Instantiate(slotItemPrefab, slotsContainer);
            SaveSlotItem slotItem = itemObject.GetComponent<SaveSlotItem>();

            if (slotItem == null)
                slotItem = itemObject.AddComponent<SaveSlotItem>();

            slotItem.Initialize(slotIndex, slotInfo, this);
            slotItems.Add(slotItem);
        }

        /// <summary>
        /// Загрузить слот
        /// </summary>
        private void LoadSlot(int slotIndex)
        {
            ShowLoadingPanel("Загрузка сохранения...");

            SaveOperationResult result = slotManager.LoadFromSlot(slotIndex);
            
            if (result.IsSuccess)
            {
                OnSlotLoaded?.Invoke(slotIndex);
                Hide();
            }
            else
            {
                HideLoadingPanel();
                ShowError($"Ошибка загрузки: {result.message}");
            }
        }

        /// <summary>
        /// Удалить слот
        /// </summary>
        private void DeleteSlot(int slotIndex)
        {
            SaveOperationResult result = slotManager.DeleteSlot(slotIndex);
            
            if (result.IsSuccess)
            {
                OnSlotDeleted?.Invoke(slotIndex);
                RefreshSlotsList();
                selectedSlotIndex = -1;
            }
            else
            {
                ShowError($"Ошибка удаления: {result.message}");
            }
        }

        #endregion

        #region New Game Panel

        /// <summary>
        /// Показать панель создания нового сохранения
        /// </summary>
        private void ShowNewGamePanel()
        {
            if (newSavePanel == null) return;

            newSavePanel.SetActive(true);
            
            // Установить имя по умолчанию
            if (saveNameInput != null)
            {
                saveNameInput.text = $"Новая игра {DateTime.Now:dd.MM.yyyy HH:mm}";
                saveNameInput.Select();
            }

            // Проверить доступность кнопки создания
            UpdateCreateButtonState();

            PlaySound(buttonClickSound);
        }

        /// <summary>
        /// Скрыть панель создания нового сохранения
        /// </summary>
        private void HideNewGamePanel()
        {
            if (newSavePanel != null)
                newSavePanel.SetActive(false);

            PlaySound(buttonClickSound);
        }

        /// <summary>
        /// Создать новое сохранение
        /// </summary>
        private void CreateNewSave()
        {
            if (saveNameInput == null) return;

            string saveName = saveNameInput.text.Trim();
            if (string.IsNullOrEmpty(saveName))
            {
                ShowError("Введите название сохранения");
                return;
            }

            // Найти доступный слот
            int availableSlot = slotManager.GetNextAvailableSlot();
            if (availableSlot == -1)
            {
                ShowError("Нет доступных слотов для сохранения");
                return;
            }

            // Создать новое сохранение
            ShowLoadingPanel("Создание нового сохранения...");

            SaveOperationResult result = slotManager.CreateNewSave(availableSlot, saveName);
            
            if (result.IsSuccess)
            {
                OnSlotCreated?.Invoke(availableSlot, saveName);
                HideNewGamePanel();
                HideLoadingPanel();
                RefreshSlotsList();
                Hide(); // Закрыть экран сохранений и начать игру
            }
            else
            {
                HideLoadingPanel();
                ShowError($"Ошибка создания сохранения: {result.message}");
            }
        }

        /// <summary>
        /// Обработчик изменения имени сохранения
        /// </summary>
        private void OnSaveNameChanged(string value)
        {
            UpdateCreateButtonState();
        }

        /// <summary>
        /// Обновить состояние кнопки создания
        /// </summary>
        private void UpdateCreateButtonState()
        {
            if (createButton == null || saveNameInput == null) return;

            bool canCreate = !string.IsNullOrEmpty(saveNameInput.text.Trim()) &&
                            slotManager.HasAvailableSlots();

            createButton.interactable = canCreate;
        }

        #endregion

        #region Confirmation Panel

        /// <summary>
        /// Показать диалог подтверждения
        /// </summary>
        private void ShowConfirmation(string message, Action<bool> callback)
        {
            if (confirmationPanel == null) return;

            confirmationPanel.SetActive(true);
            
            if (confirmationText != null)
                confirmationText.text = message;

            currentConfirmationAction = callback;
            PlaySound(buttonClickSound);
        }

        /// <summary>
        /// Подтвердить действие
        /// </summary>
        private void ConfirmAction()
        {
            currentConfirmationAction?.Invoke(true);
            HideConfirmationPanel();
        }

        /// <summary>
        /// Отменить действие
        /// </summary>
        private void CancelAction()
        {
            currentConfirmationAction?.Invoke(false);
            HideConfirmationPanel();
        }

        /// <summary>
        /// Скрыть панель подтверждения
        /// </summary>
        private void HideConfirmationPanel()
        {
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);

            currentConfirmationAction = null;
            PlaySound(buttonClickSound);
        }

        #endregion

        #region Loading Panel

        /// <summary>
        /// Показать панель загрузки
        /// </summary>
        private void ShowLoadingPanel(string message)
        {
            if (loadingPanel == null) return;

            loadingPanel.SetActive(true);
            
            if (loadingText != null)
                loadingText.text = message;

            if (loadingProgressBar != null)
                loadingProgressBar.value = 0f;
        }

        /// <summary>
        /// Обновить прогресс загрузки
        /// </summary>
        private void UpdateLoadingProgress(float progress)
        {
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;
        }

        /// <summary>
        /// Скрыть панель загрузки
        /// </summary>
        private void HideLoadingPanel()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        #endregion

        #region Auto Save Indicator

        /// <summary>
        /// Показать индикатор автосохранения
        /// </summary>
        private void ShowAutoSaveIndicator(string message)
        {
            if (autoSaveIndicator == null) return;

            autoSaveIndicator.SetActive(true);
            
            if (autoSaveText != null)
                autoSaveText.text = message;

            // Анимация иконки
            if (autoSaveIcon != null)
            {
                // Здесь можно добавить анимацию вращения или мигания
            }
        }

        /// <summary>
        /// Скрыть индикатор автосохранения
        /// </summary>
        private void HideAutoSaveIndicator()
        {
            if (autoSaveIndicator != null)
                autoSaveIndicator.SetActive(false);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик завершения сохранения
        /// </summary>
        private void OnSaveCompleted(SaveOperationResult result)
        {
            HideLoadingPanel();
            
            if (result.IsSuccess)
            {
                PlaySound(saveSuccessSound);
                RefreshSlotsList();
            }
            else
            {
                PlaySound(errorSound);
                ShowError($"Ошибка сохранения: {result.message}");
            }
        }

        /// <summary>
        /// Обработчик завершения загрузки
        /// </summary>
        private void OnLoadCompleted(SaveOperationResult result)
        {
            HideLoadingPanel();
            
            if (result.IsSuccess)
            {
                PlaySound(loadSuccessSound);
            }
            else
            {
                PlaySound(errorSound);
                ShowError($"Ошибка загрузки: {result.message}");
            }
        }

        /// <summary>
        /// Обработчик обновления слотов
        /// </summary>
        private void OnSlotsUpdated(List<SaveSlotInfo> slots)
        {
            UpdateSlotsList(slots);
        }

        /// <summary>
        /// Обработчик удаления слота
        /// </summary>
        private void OnSlotDeletedHandler(int slotIndex)
        {
            if (selectedSlotIndex == slotIndex)
                selectedSlotIndex = -1;
        }

        /// <summary>
        /// Обработчик начала автосохранения
        /// </summary>
        private void OnAutoSaveStarted()
        {
            ShowAutoSaveIndicator("Автосохранение...");
        }

        /// <summary>
        /// Обработчик завершения автосохранения
        /// </summary>
        private void OnAutoSaveCompleted(SaveOperationResult result)
        {
            if (result.IsSuccess)
            {
                ShowAutoSaveIndicator("Сохранено");
                Invoke(nameof(HideAutoSaveIndicator), 2f);
            }
            else
            {
                ShowAutoSaveIndicator("Ошибка сохранения");
                Invoke(nameof(HideAutoSaveIndicator), 3f);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Показать сообщение об ошибке
        /// </summary>
        private void ShowError(string message)
        {
            GameEvents.TriggerNotification(message, NotificationType.Danger);
            PlaySound(errorSound);
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

        #region Context Menu

        [ContextMenu("Обновить слоты")]
        private void DebugRefreshSlots()
        {
            RefreshSlotsList();
        }

        [ContextMenu("Показать панель нового сохранения")]
        private void DebugShowNewGamePanel()
        {
            ShowNewGamePanel();
        }

        [ContextMenu("Показать тестовое подтверждение")]
        private void DebugShowConfirmation()
        {
            ShowConfirmation("Это тестовое сообщение подтверждения", (confirmed) => {
                Debug.Log($"Подтверждение: {confirmed}");
            });
        }

        #endregion
    }

    /// <summary>
    /// Элемент слота сохранения в UI
    /// </summary>
    public class SaveSlotItem : MonoBehaviour
    {
        [Header("UI элементы")]
        public TextMeshProUGUI slotIndexText;
        public TextMeshProUGUI saveNameText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI locationText;
        public TextMeshProUGUI playTimeText;
        public TextMeshProUGUI lastPlayedText;
        public Image statusIcon;
        public Button selectButton;
        public Button loadButton;
        public Button deleteButton;
        public GameObject emptySlotPanel;
        public GameObject filledSlotPanel;
        public GameObject corruptedIndicator;

        [Header("Визуальные состояния")]
        public Color normalColor = Color.white;
        public Color selectedColor = Color.cyan;
        public Color corruptedColor = Color.red;

        // Состояние
        private int slotIndex;
        private SaveSlotInfo slotInfo;
        private SaveUI parentUI;
        private bool isSelected = false;

        /// <summary>
        /// Инициализировать элемент слота
        /// </summary>
        public void Initialize(int index, SaveSlotInfo info, SaveUI parent)
        {
            slotIndex = index;
            slotInfo = info;
            parentUI = parent;

            UpdateDisplay();
            SetupButtons();
        }

        /// <summary>
        /// Обновить отображение
        /// </summary>
        private void UpdateDisplay()
        {
            // Индекс слота
            if (slotIndexText != null)
                slotIndexText.text = $"Слот {slotIndex + 1}";

            // Проверить, пустой ли слот
            bool isEmpty = slotInfo == null || slotInfo.isEmpty;
            
            if (emptySlotPanel != null) emptySlotPanel.SetActive(isEmpty);
            if (filledSlotPanel != null) filledSlotPanel.SetActive(!isEmpty);

            if (isEmpty) return;

            // Заполнить информацию о сохранении
            if (saveNameText != null)
                saveNameText.text = slotInfo.saveName;

            if (levelText != null)
                levelText.text = $"Уровень {slotInfo.level}";

            if (moneyText != null)
                moneyText.text = GameUtils.FormatMoney(slotInfo.money);

            if (locationText != null)
                locationText.text = slotInfo.GetLocalizedLocationName();

            if (playTimeText != null)
                playTimeText.text = $"Время игры: {slotInfo.GetFormattedPlayTime()}";

            if (lastPlayedText != null)
                lastPlayedText.text = slotInfo.GetFormattedLastPlayed();

            // Показать индикатор повреждения
            if (corruptedIndicator != null)
                corruptedIndicator.SetActive(slotInfo.isCorrupted);

            // Цвет статуса
            Color statusColor = slotInfo.isCorrupted ? corruptedColor : normalColor;
            if (statusIcon != null)
                statusIcon.color = statusColor;
        }

        /// <summary>
        /// Настроить кнопки
        /// </summary>
        private void SetupButtons()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => parentUI.SelectSlot(slotIndex));
            }

            if (loadButton != null)
            {
                loadButton.onClick.RemoveAllListeners();
                loadButton.onClick.AddListener(() => {
                    parentUI.SelectSlot(slotIndex);
                    parentUI.LoadSelectedSlot();
                });
                
                // Активность кнопки загрузки
                bool isEmpty = slotInfo == null || slotInfo.isEmpty;
                loadButton.gameObject.SetActive(!isEmpty);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => {
                    parentUI.SelectSlot(slotIndex);
                    parentUI.DeleteSelectedSlot();
                });
                
                // Активность кнопки удаления
                bool isEmpty = slotInfo == null || slotInfo.isEmpty;
                deleteButton.gameObject.SetActive(!isEmpty);
            }
        }

        /// <summary>
        /// Установить состояние выделения
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            // Изменить визуальное состояние
            Image background = GetComponent<Image>();
            if (background != null)
            {
                background.color = selected ? selectedColor : normalColor;
            }
        }
    }
}
