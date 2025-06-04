using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Центральный менеджер пользовательского интерфейса
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Основные UI панели")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject gameUIPanel;            // Основной игровой UI
        [SerializeField] private GameObject pauseMenuPanel;         // Меню паузы
        [SerializeField] private GameObject settingsPanel;          // Панель настроек
        [SerializeField] private GameObject gameOverPanel;          // Экран окончания игры
        
        [Header("Компоненты UI")]
        [SerializeField] private StatsPanel statsPanel;
        [SerializeField] private MoneyDisplay moneyDisplay;
        [SerializeField] private NotificationSystem notificationSystem;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private SettingsPanel settingsPanel_component;
        
        [Header("Кнопки управления")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingsButton;
        
        [Header("Анимация переходов")]
        [SerializeField] private Animator uiAnimator;
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Фон затемнения")]
        [SerializeField] private Image overlayBackground;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.5f);
        
        // Состояние UI
        private UIState currentState = UIState.Game;
        private UIState previousState = UIState.Game;
        private bool isTransitioning = false;
        
        // События
        public Action<UIState> OnUIStateChanged;
        public Action OnUITransitionStart;
        public Action OnUITransitionComplete;
        
        // Синглтон
        public static UIManager Instance { get; private set; }
        
        // Публичные свойства
        public UIState CurrentState => currentState;
        public bool IsTransitioning => isTransitioning;
        public StatsPanel StatsPanel => statsPanel;
        public MoneyDisplay MoneyDisplay => moneyDisplay;
        public NotificationSystem NotificationSystem => notificationSystem;

        #region Unity Callbacks

        private void Awake()
        {
            // Реализация синглтона
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupEventHandlers();
            SetUIState(UIState.Game, false);
        }

        private void Update()
        {
            HandleInput();
        }

        private void OnDestroy()
        {
            RemoveEventHandlers();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать UI систему
        /// </summary>
        private void InitializeUI()
        {
            // Найти компоненты если они не назначены
            if (mainCanvas == null)
                mainCanvas = GetComponentInChildren<Canvas>();
            
            if (uiAnimator == null)
                uiAnimator = GetComponent<Animator>();
            
            // Создать фон затемнения если не назначен
            if (overlayBackground == null)
            {
                CreateOverlayBackground();
            }
            
            // Инициализировать компоненты
            InitializeComponents();
            
            Debug.Log("UIManager инициализирован");
        }

        /// <summary>
        /// Инициализировать UI компоненты
        /// </summary>
        private void InitializeComponents()
        {
            // Найти компоненты если они не назначены
            if (statsPanel == null)
                statsPanel = FindObjectOfType<StatsPanel>();
            
            if (moneyDisplay == null)
                moneyDisplay = FindObjectOfType<MoneyDisplay>();
            
            if (notificationSystem == null)
                notificationSystem = FindObjectOfType<NotificationSystem>();
            
            if (pauseMenu == null)
                pauseMenu = FindObjectOfType<PauseMenu>();
            
            if (settingsPanel_component == null)
                settingsPanel_component = FindObjectOfType<SettingsPanel>();
        }

        /// <summary>
        /// Создать фон затемнения
        /// </summary>
        private void CreateOverlayBackground()
        {
            GameObject overlayObj = new GameObject("OverlayBackground");
            overlayObj.transform.SetParent(mainCanvas.transform, false);
            
            overlayBackground = overlayObj.AddComponent<Image>();
            overlayBackground.color = Color.clear;
            overlayBackground.raycastTarget = true;
            
            // Установить на весь экран
            RectTransform rectTransform = overlayObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Поместить за другими UI элементами
            overlayObj.transform.SetAsFirstSibling();
        }

        #endregion

        #region Управление состояниями UI

        /// <summary>
        /// Установить состояние UI
        /// </summary>
        /// <param name="newState">Новое состояние</param>
        /// <param name="animated">Использовать анимацию</param>
        public void SetUIState(UIState newState, bool animated = true)
        {
            if (currentState == newState || isTransitioning)
                return;

            if (animated)
            {
                StartCoroutine(TransitionToState(newState));
            }
            else
            {
                ActivateState(newState);
            }
        }

        /// <summary>
        /// Корутина перехода между состояниями
        /// </summary>
        private IEnumerator TransitionToState(UIState newState)
        {
            isTransitioning = true;
            OnUITransitionStart?.Invoke();

            // Анимация исчезновения текущего состояния
            if (uiAnimator != null)
            {
                uiAnimator.SetTrigger("FadeOut");
                yield return new WaitForSeconds(transitionDuration * 0.5f);
            }

            // Изменить состояние
            ActivateState(newState);

            // Анимация появления нового состояния
            if (uiAnimator != null)
            {
                uiAnimator.SetTrigger("FadeIn");
                yield return new WaitForSeconds(transitionDuration * 0.5f);
            }

            isTransitioning = false;
            OnUITransitionComplete?.Invoke();
        }

        /// <summary>
        /// Активировать состояние UI
        /// </summary>
        private void ActivateState(UIState newState)
        {
            previousState = currentState;
            currentState = newState;

            // Скрыть все панели
            DeactivateAllPanels();

            // Показать нужную панель
            switch (newState)
            {
                case UIState.Game:
                    ActivateGameUI();
                    break;
                case UIState.Pause:
                    ActivatePauseMenu();
                    break;
                case UIState.Settings:
                    ActivateSettings();
                    break;
                case UIState.GameOver:
                    ActivateGameOver();
                    break;
            }

            OnUIStateChanged?.Invoke(newState);
            Debug.Log($"UI состояние изменено на: {newState}");
        }

        /// <summary>
        /// Скрыть все панели
        /// </summary>
        private void DeactivateAllPanels()
        {
            if (gameUIPanel != null) gameUIPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            
            SetOverlayVisible(false);
        }

        /// <summary>
        /// Активировать игровой UI
        /// </summary>
        private void ActivateGameUI()
        {
            if (gameUIPanel != null) gameUIPanel.SetActive(true);
            SetOverlayVisible(false);
        }

        /// <summary>
        /// Активировать меню паузы
        /// </summary>
        private void ActivatePauseMenu()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
            SetOverlayVisible(true);
        }

        /// <summary>
        /// Активировать настройки
        /// </summary>
        private void ActivateSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
            SetOverlayVisible(true);
        }

        /// <summary>
        /// Активировать экран окончания игры
        /// </summary>
        private void ActivateGameOver()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            SetOverlayVisible(true);
        }

        /// <summary>
        /// Установить видимость фона затемнения
        /// </summary>
        private void SetOverlayVisible(bool visible)
        {
            if (overlayBackground != null)
            {
                if (visible)
                {
                    overlayBackground.color = overlayColor;
                    overlayBackground.gameObject.SetActive(true);
                }
                else
                {
                    overlayBackground.color = Color.clear;
                    overlayBackground.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Навигация

        /// <summary>
        /// Открыть меню паузы
        /// </summary>
        public void OpenPauseMenu()
        {
            if (currentState == UIState.Game)
            {
                GameManager.Instance?.PauseGame();
                SetUIState(UIState.Pause);
            }
        }

        /// <summary>
        /// Закрыть меню паузы
        /// </summary>
        public void ClosePauseMenu()
        {
            if (currentState == UIState.Pause)
            {
                GameManager.Instance?.ResumeGame();
                SetUIState(UIState.Game);
            }
        }

        /// <summary>
        /// Открыть настройки
        /// </summary>
        public void OpenSettings()
        {
            SetUIState(UIState.Settings);
        }

        /// <summary>
        /// Закрыть настройки
        /// </summary>
        public void CloseSettings()
        {
            SetUIState(previousState);
        }

        /// <summary>
        /// Вернуться к предыдущему состоянию
        /// </summary>
        public void GoBack()
        {
            switch (currentState)
            {
                case UIState.Pause:
                    ClosePauseMenu();
                    break;
                case UIState.Settings:
                    CloseSettings();
                    break;
                default:
                    OpenPauseMenu();
                    break;
            }
        }

        /// <summary>
        /// Выйти в главное меню
        /// </summary>
        public void ReturnToMainMenu()
        {
            GameManager.Instance?.ReturnToMainMenu();
            SetUIState(UIState.MainMenu);
        }

        /// <summary>
        /// Выйти из игры
        /// </summary>
        public void QuitGame()
        {
            GameManager.Instance?.QuitGame();
        }

        #endregion

        #region Обработка ввода

        /// <summary>
        /// Обработать пользовательский ввод
        /// </summary>
        private void HandleInput()
        {
            // ESC для открытия/закрытия меню
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GoBack();
            }

            // P для паузы
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (currentState == UIState.Game)
                    OpenPauseMenu();
                else if (currentState == UIState.Pause)
                    ClosePauseMenu();
            }
        }

        #endregion

        #region События

        /// <summary>
        /// Настроить обработчики событий
        /// </summary>
        private void SetupEventHandlers()
        {
            // Кнопки
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OpenPauseMenu);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            // События игры
            if (GameManager.Instance != null)
            {
                GameEvents.OnPlayerDeath += OnPlayerDeath;
                GameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }

        /// <summary>
        /// Удалить обработчики событий
        /// </summary>
        private void RemoveEventHandlers()
        {
            // Кнопки
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OpenPauseMenu);
            
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OpenSettings);

            // События игры
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            SetUIState(UIState.GameOver);
        }

        /// <summary>
        /// Обработчик изменения состояния игры
        /// </summary>
        private void OnGameStateChanged(GameState gameState)
        {
            switch (gameState)
            {
                case GameState.Playing:
                    if (currentState != UIState.Game)
                        SetUIState(UIState.Game);
                    break;
                case GameState.Paused:
                    if (currentState != UIState.Pause)
                        SetUIState(UIState.Pause);
                    break;
                case GameState.GameOver:
                    SetUIState(UIState.GameOver);
                    break;
                case GameState.MainMenu:
                    SetUIState(UIState.MainMenu);
                    break;
            }
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Показать уведомление
        /// </summary>
        /// <param name="message">Текст уведомления</param>
        /// <param name="type">Тип уведомления</param>
        /// <param name="priority">Приоритет</param>
        public void ShowNotification(string message, NotificationType type = NotificationType.Info, 
                                   NotificationPriority priority = NotificationPriority.Normal)
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification(message, type, priority);
            }
        }

        /// <summary>
        /// Обновить отображение денег
        /// </summary>
        /// <param name="amount">Новая сумма</param>
        public void UpdateMoneyDisplay(float amount)
        {
            if (moneyDisplay != null)
            {
                moneyDisplay.UpdateMoney(amount);
            }
        }

        /// <summary>
        /// Показать изменение денег
        /// </summary>
        /// <param name="change">Изменение суммы</param>
        /// <param name="isPositive">Положительное изменение</param>
        public void ShowMoneyChange(float change, bool isPositive)
        {
            if (moneyDisplay != null)
            {
                moneyDisplay.ShowMoneyChange(change, isPositive);
            }
        }

        /// <summary>
        /// Обновить панель характеристик
        /// </summary>
        public void UpdateStatsPanel()
        {
            if (statsPanel != null)
            {
                statsPanel.UpdateDisplay();
            }
        }

        /// <summary>
        /// Установить интерактивность UI
        /// </summary>
        /// <param name="interactive">Интерактивность</param>
        public void SetUIInteractive(bool interactive)
        {
            if (mainCanvas != null)
            {
                GraphicRaycaster raycaster = mainCanvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycaster.enabled = interactive;
                }
            }
        }

        /// <summary>
        /// Получить информацию о текущем состоянии UI
        /// </summary>
        public UIStateInfo GetUIStateInfo()
        {
            return new UIStateInfo
            {
                currentState = currentState,
                previousState = previousState,
                isTransitioning = isTransitioning
            };
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Показать тестовое уведомление
        /// </summary>
        [ContextMenu("Show Test Notification")]
        private void ShowTestNotification()
        {
            if (Application.isPlaying)
            {
                ShowNotification("Тестовое уведомление", NotificationType.Info);
            }
        }

        /// <summary>
        /// Переключить состояние UI для тестирования
        /// </summary>
        [ContextMenu("Toggle Pause Menu")]
        private void TogglePauseMenuDebug()
        {
            if (Application.isPlaying)
            {
                if (currentState == UIState.Game)
                    OpenPauseMenu();
                else
                    ClosePauseMenu();
            }
        }

        #endregion
    }

    /// <summary>
    /// Информация о состоянии UI
    /// </summary>
    [System.Serializable]
    public class UIStateInfo
    {
        public UIState currentState;
        public UIState previousState;
        public bool isTransitioning;
    }
}