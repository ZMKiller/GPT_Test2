using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Компонент меню паузы
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Header("UI элементы")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        
        [Header("Информационные элементы")]
        [SerializeField] private Text gameTimeText;
        [SerializeField] private Text moneyText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text statsText;
        
        [Header("Анимация")]
        [SerializeField] private Animator menuAnimator;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        [Header("Подтверждение выхода")]
        [SerializeField] private GameObject confirmationDialog;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;
        [SerializeField] private Text confirmationText;
        
        // Состояние меню
        private bool isShowingConfirmation = false;
        private System.Action pendingAction;

        #region Unity Callbacks

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupButtonHandlers();
            UpdateMenuInfo();
        }

        private void OnEnable()
        {
            UpdateMenuInfo();
            StartCoroutine(FadeIn());
        }

        private void OnDisable()
        {
            HideConfirmationDialog();
        }

        private void Update()
        {
            // Обновлять информацию периодически
            UpdateMenuInfo();
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать компоненты
        /// </summary>
        private void InitializeComponents()
        {
            // Получить CanvasGroup если не назначен
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            // Скрыть диалог подтверждения
            if (confirmationDialog != null)
            {
                confirmationDialog.SetActive(false);
            }
        }

        /// <summary>
        /// Настроить обработчики кнопок
        /// </summary>
        private void SetupButtonHandlers()
        {
            // Основные кнопки
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Кнопки подтверждения
            if (confirmYesButton != null)
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            
            if (confirmNoButton != null)
                confirmNoButton.onClick.AddListener(OnConfirmNo);
        }

        #endregion

        #region Обработчики кнопок

        /// <summary>
        /// Обработчик кнопки "Продолжить"
        /// </summary>
        private void OnContinueClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePauseMenu();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Настройки"
        /// </summary>
        private void OnSettingsClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenSettings();
            }
        }

        /// <summary>
        /// Обработчик кнопки "Главное меню"
        /// </summary>
        private void OnMainMenuClicked()
        {
            ShowConfirmationDialog(
                "Вы уверены, что хотите выйти в главное меню?\nНесохраненный прогресс будет потерян.",
                () => {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ReturnToMainMenu();
                    }
                }
            );
        }

        /// <summary>
        /// Обработчик кнопки "Выход"
        /// </summary>
        private void OnQuitClicked()
        {
            ShowConfirmationDialog(
                "Вы уверены, что хотите выйти из игры?\nНесохраненный прогресс будет потерян.",
                () => {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.QuitGame();
                    }
                }
            );
        }

        /// <summary>
        /// Обработчик подтверждения "Да"
        /// </summary>
        private void OnConfirmYes()
        {
            HideConfirmationDialog();
            pendingAction?.Invoke();
            pendingAction = null;
        }

        /// <summary>
        /// Обработчик подтверждения "Нет"
        /// </summary>
        private void OnConfirmNo()
        {
            HideConfirmationDialog();
            pendingAction = null;
        }

        #endregion

        #region Обновление информации

        /// <summary>
        /// Обновить информацию в меню
        /// </summary>
        private void UpdateMenuInfo()
        {
            UpdateGameTime();
            UpdatePlayerInfo();
            UpdateStats();
        }

        /// <summary>
        /// Обновить время игры
        /// </summary>
        private void UpdateGameTime()
        {
            if (gameTimeText != null && GameManager.Instance != null)
            {
                float gameTime = GameManager.Instance.GameTime;
                gameTimeText.text = $"Время игры: {GameUtils.FormatGameTime(gameTime)}";
            }
        }

        /// <summary>
        /// Обновить информацию о игроке
        /// </summary>
        private void UpdatePlayerInfo()
        {
            PlayerStats playerStats = GameManager.Instance?.PlayerStats;
            if (playerStats == null) return;

            // Деньги
            if (moneyText != null)
            {
                moneyText.text = $"Деньги: {GameUtils.FormatMoney(playerStats.Money)}";
            }

            // Уровень
            if (levelText != null)
            {
                levelText.text = $"Уровень: {playerStats.Level}";
            }
        }

        /// <summary>
        /// Обновить статистику
        /// </summary>
        private void UpdateStats()
        {
            if (statsText == null) return;

            PlayerStats playerStats = GameManager.Instance?.PlayerStats;
            if (playerStats == null) return;

            string stats = $"Здоровье: {playerStats.Health:F0}/{playerStats.MaxHealth:F0}\n";
            stats += $"Голод: {playerStats.Hunger:F0}/{playerStats.MaxHunger:F0}\n";
            stats += $"Настроение: {playerStats.Mood:F0}/{playerStats.MaxMood:F0}\n";
            stats += $"Опыт: {playerStats.Experience:F0}/{playerStats.ExperienceToNext:F0}";

            statsText.text = stats;
        }

        #endregion

        #region Диалог подтверждения

        /// <summary>
        /// Показать диалог подтверждения
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="onConfirm">Действие при подтверждении</param>
        private void ShowConfirmationDialog(string message, System.Action onConfirm)
        {
            if (confirmationDialog == null) return;

            pendingAction = onConfirm;
            isShowingConfirmation = true;

            if (confirmationText != null)
            {
                confirmationText.text = message;
            }

            confirmationDialog.SetActive(true);

            // Анимация появления
            StartCoroutine(AnimateConfirmationDialog(true));
        }

        /// <summary>
        /// Скрыть диалог подтверждения
        /// </summary>
        private void HideConfirmationDialog()
        {
            if (confirmationDialog == null || !isShowingConfirmation) return;

            isShowingConfirmation = false;
            StartCoroutine(AnimateConfirmationDialog(false));
        }

        /// <summary>
        /// Анимация диалога подтверждения
        /// </summary>
        private IEnumerator AnimateConfirmationDialog(bool show)
        {
            CanvasGroup dialogCanvasGroup = confirmationDialog.GetComponent<CanvasGroup>();
            if (dialogCanvasGroup == null)
            {
                dialogCanvasGroup = confirmationDialog.AddComponent<CanvasGroup>();
            }

            RectTransform dialogRect = confirmationDialog.GetComponent<RectTransform>();
            
            float duration = 0.3f;
            float elapsed = 0f;

            if (show)
            {
                // Анимация появления
                dialogCanvasGroup.alpha = 0f;
                dialogRect.localScale = Vector3.zero;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    
                    dialogCanvasGroup.alpha = t;
                    dialogRect.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, 
                                                       Mathf.SmoothStep(0f, 1f, t));
                    
                    yield return null;
                }

                dialogCanvasGroup.alpha = 1f;
                dialogRect.localScale = Vector3.one;
            }
            else
            {
                // Анимация исчезновения
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    
                    dialogCanvasGroup.alpha = 1f - t;
                    dialogRect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, 
                                                       Mathf.SmoothStep(0f, 1f, t));
                    
                    yield return null;
                }

                confirmationDialog.SetActive(false);
            }
        }

        #endregion

        #region Анимация меню

        /// <summary>
        /// Анимация появления
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Анимация через Animator если есть
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger("FadeIn");
            }

            float elapsed = 0f;
            
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;
                
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
                
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Анимация исчезновения
        /// </summary>
        public IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Анимация через Animator если есть
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger("FadeOut");
            }

            float elapsed = 0f;
            
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;
                
                canvasGroup.alpha = Mathf.SmoothStep(1f, 0f, t);
                
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Установить интерактивность меню
        /// </summary>
        /// <param name="interactive">Интерактивность</param>
        public void SetInteractive(bool interactive)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactive;
                canvasGroup.blocksRaycasts = interactive;
            }

            // Отключить кнопки индивидуально если нет CanvasGroup
            if (canvasGroup == null)
            {
                if (continueButton != null) continueButton.interactable = interactive;
                if (settingsButton != null) settingsButton.interactable = interactive;
                if (mainMenuButton != null) mainMenuButton.interactable = interactive;
                if (quitButton != null) quitButton.interactable = interactive;
            }
        }

        /// <summary>
        /// Принудительно обновить информацию
        /// </summary>
        public void RefreshInfo()
        {
            UpdateMenuInfo();
        }

        /// <summary>
        /// Проверить показан ли диалог подтверждения
        /// </summary>
        public bool IsShowingConfirmation()
        {
            return isShowingConfirmation;
        }

        /// <summary>
        /// Закрыть диалог подтверждения (для внешнего управления)
        /// </summary>
        public void CancelConfirmation()
        {
            if (isShowingConfirmation)
            {
                OnConfirmNo();
            }
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Показать тестовый диалог подтверждения
        /// </summary>
        [ContextMenu("Test Confirmation Dialog")]
        private void TestConfirmationDialog()
        {
            if (Application.isPlaying)
            {
                ShowConfirmationDialog("Это тестовый диалог подтверждения.\nВы хотите продолжить?", 
                                     () => Debug.Log("Подтверждено!"));
            }
        }

        /// <summary>
        /// Принудительно обновить информацию (для отладки)
        /// </summary>
        [ContextMenu("Force Update Info")]
        private void ForceUpdateInfo()
        {
            if (Application.isPlaying)
            {
                UpdateMenuInfo();
            }
        }

        #endregion
    }
}