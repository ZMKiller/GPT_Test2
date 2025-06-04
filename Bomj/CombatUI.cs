using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Пользовательский интерфейс боевой системы
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        [Header("Основные UI элементы")]
        [SerializeField] private GameObject combatPanel;               // Главная панель боя
        [SerializeField] private GameObject actionButtonsPanel;        // Панель кнопок действий
        [SerializeField] private GameObject enemyInfoPanel;            // Панель информации о врагах
        [SerializeField] private GameObject combatLogPanel;            // Панель лога боя
        
        [Header("Информация об игроке")]
        [SerializeField] private Slider playerHealthSlider;            // Слайдер здоровья игрока
        [SerializeField] private TextMeshProUGUI playerHealthText;     // Текст здоровья игрока
        [SerializeField] private TextMeshProUGUI playerWeaponText;     // Текст текущего оружия
        [SerializeField] private Image playerStatusIcon;               // Иконка статуса игрока
        
        [Header("Информация о врагах")]
        [SerializeField] private Transform enemyListParent;            // Родитель списка врагов
        [SerializeField] private GameObject enemyInfoPrefab;           // Префаб информации о враге
        [SerializeField] private TextMeshProUGUI currentEnemyText;     // Текст текущего врага
        
        [Header("Кнопки действий")]
        [SerializeField] private Button attackButton;                 // Кнопка атаки
        [SerializeField] private Button defendButton;                 // Кнопка защиты
        [SerializeField] private Button dodgeButton;                  // Кнопка уклонения
        [SerializeField] private Button useItemButton;                // Кнопка использования предмета
        [SerializeField] private Button fleeButton;                   // Кнопка побега
        [SerializeField] private Button negotiateButton;              // Кнопка переговоров
        [SerializeField] private Button intimidateButton;             // Кнопка запугивания
        
        [Header("Лог боя")]
        [SerializeField] private ScrollRect combatLogScrollRect;       // Скролл лога
        [SerializeField] private TextMeshProUGUI combatLogText;        // Текст лога боя
        [SerializeField] private int maxLogEntries = 50;               // Максимум записей в логе
        
        [Header("Результаты боя")]
        [SerializeField] private GameObject combatResultPanel;         // Панель результатов
        [SerializeField] private TextMeshProUGUI resultTitleText;      // Заголовок результата
        [SerializeField] private TextMeshProUGUI resultDetailsText;    // Детали результата
        [SerializeField] private Button continueButton;               // Кнопка продолжения
        
        [Header("Анимация и эффекты")]
        [SerializeField] private Animator combatAnimator;              // Аниматор боя
        [SerializeField] private ParticleSystem hitEffect;            // Эффект попадания
        [SerializeField] private ParticleSystem criticalEffect;       // Эффект критического удара
        [SerializeField] private AudioSource combatAudioSource;       // Источник звука боя
        
        [Header("Настройки")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private float actionButtonCooldown = 1f;     // Кулдаун кнопок
        [SerializeField] private float logScrollSpeed = 2f;           // Скорость прокрутки лога
        [SerializeField] private bool autoScrollLog = true;           // Автопрокрутка лога
        
        // Состояние UI
        private CombatSystem combatSystem;
        private WeaponSystem weaponSystem;
        private PlayerStats playerStats;
        private NotificationSystem notificationSystem;
        
        private List<GameObject> enemyInfoObjects = new List<GameObject>();
        private List<string> combatLogEntries = new List<string>();
        private bool isWaitingForAction = false;
        private float lastActionTime = 0f;
        
        // События
        public event Action<CombatAction, int> OnCombatActionRequested; // Action, targetIndex
        
        #region Unity Methods
        
        private void Awake()
        {
            // Найти компоненты
            combatSystem = FindObjectOfType<CombatSystem>();
            weaponSystem = FindObjectOfType<WeaponSystem>();
            playerStats = FindObjectOfType<PlayerStats>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
            
            // Инициализация UI
            InitializeUI();
        }
        
        private void Start()
        {
            SubscribeToEvents();
            
            // Скрыть UI боя в начале
            if (combatPanel != null)
            {
                combatPanel.SetActive(false);
            }
            
            Debug.Log("CombatUI инициализирован");
        }
        
        private void Update()
        {
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Инициализация UI
        /// </summary>
        private void InitializeUI()
        {
            // Настройка кнопок
            SetupActionButtons();
            
            // Настройка панелей
            SetupPanels();
            
            // Инициализация лога
            if (combatLogText != null)
            {
                combatLogText.text = "";
            }
        }
        
        /// <summary>
        /// Настройка кнопок действий
        /// </summary>
        private void SetupActionButtons()
        {
            if (attackButton != null)
                attackButton.onClick.AddListener(() => RequestCombatAction(CombatAction.Attack));
            
            if (defendButton != null)
                defendButton.onClick.AddListener(() => RequestCombatAction(CombatAction.Defend));
            
            if (dodgeButton != null)
                dodgeButton.onClick.AddListener(() => RequestCombatAction(CombatAction.Dodge));
            
            if (useItemButton != null)
                useItemButton.onClick.AddListener(() => RequestCombatAction(CombatAction.UseItem));
            
            if (fleeButton != null)
                fleeButton.onClick.AddListener(() => RequestCombatAction(CombatAction.Flee));
            
            if (negotiateButton != null)
                negotiateButton.onClick.AddListener(() => RequestCombatAction(CombatAction.Negotiate));
            
            if (intimidateButton != null)
                intimidateButton.onClick.AddListener(() => RequestCombatAction(CombatAction.Intimidate));
            
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        /// <summary>
        /// Настройка панелей
        /// </summary>
        private void SetupPanels()
        {
            if (combatResultPanel != null)
            {
                combatResultPanel.SetActive(false);
            }
            
            if (combatLogPanel != null && combatLogScrollRect != null)
            {
                // Настройка автопрокрутки лога
                combatLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (combatSystem != null)
            {
                combatSystem.OnCombatStateChanged += OnCombatStateChanged;
                combatSystem.OnCombatStarted += OnCombatStarted;
                combatSystem.OnCombatEnded += OnCombatEnded;
                combatSystem.OnCombatAction += OnCombatAction;
                combatSystem.OnPlayerHealthChanged += OnPlayerHealthChanged;
                combatSystem.OnEnemyHealthChanged += OnEnemyHealthChanged;
            }
            
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponEquipped += OnWeaponEquipped;
                weaponSystem.OnWeaponUnequipped += OnWeaponUnequipped;
            }
            
            if (playerStats != null)
            {
                playerStats.OnHealthChanged += OnPlayerStatsHealthChanged;
            }
        }
        
        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (combatSystem != null)
            {
                combatSystem.OnCombatStateChanged -= OnCombatStateChanged;
                combatSystem.OnCombatStarted -= OnCombatStarted;
                combatSystem.OnCombatEnded -= OnCombatEnded;
                combatSystem.OnCombatAction -= OnCombatAction;
                combatSystem.OnPlayerHealthChanged -= OnPlayerHealthChanged;
                combatSystem.OnEnemyHealthChanged -= OnEnemyHealthChanged;
            }
            
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponEquipped -= OnWeaponEquipped;
                weaponSystem.OnWeaponUnequipped -= OnWeaponUnequipped;
            }
            
            if (playerStats != null)
            {
                playerStats.OnHealthChanged -= OnPlayerStatsHealthChanged;
            }
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Обновление UI
        /// </summary>
        private void UpdateUI()
        {
            UpdatePlayerInfo();
            UpdateActionButtons();
            UpdateCombatStatus();
        }
        
        /// <summary>
        /// Обновить информацию об игроке
        /// </summary>
        private void UpdatePlayerInfo()
        {
            if (playerStats == null)
                return;
            
            // Обновить здоровье
            if (playerHealthSlider != null)
            {
                float healthPercent = playerStats.CurrentHealth / playerStats.MaxHealth;
                playerHealthSlider.value = healthPercent;
            }
            
            if (playerHealthText != null)
            {
                playerHealthText.text = $"{playerStats.CurrentHealth:F0}/{playerStats.MaxHealth:F0}";
            }
            
            // Обновить оружие
            if (playerWeaponText != null && weaponSystem != null)
            {
                var currentWeapon = weaponSystem.CurrentWeapon;
                if (currentWeapon != null)
                {
                    playerWeaponText.text = currentWeapon.GetDisplayName();
                }
                else
                {
                    playerWeaponText.text = "Кулаки";
                }
            }
        }
        
        /// <summary>
        /// Обновить кнопки действий
        /// </summary>
        private void UpdateActionButtons()
        {
            if (combatSystem == null || !combatSystem.IsInCombat)
                return;
            
            bool canAct = combatSystem.CurrentState == CombatState.PlayerTurn && 
                         !isWaitingForAction &&
                         Time.time - lastActionTime >= actionButtonCooldown;
            
            var availableActions = combatSystem.GetAvailableActions();
            
            // Обновить доступность кнопок
            UpdateButtonAvailability(attackButton, canAct && availableActions.Contains(CombatAction.Attack));
            UpdateButtonAvailability(defendButton, canAct && availableActions.Contains(CombatAction.Defend));
            UpdateButtonAvailability(dodgeButton, canAct && availableActions.Contains(CombatAction.Dodge));
            UpdateButtonAvailability(useItemButton, canAct && availableActions.Contains(CombatAction.UseItem));
            UpdateButtonAvailability(fleeButton, canAct && availableActions.Contains(CombatAction.Flee));
            UpdateButtonAvailability(negotiateButton, canAct && availableActions.Contains(CombatAction.Negotiate));
            UpdateButtonAvailability(intimidateButton, canAct && availableActions.Contains(CombatAction.Intimidate));
        }
        
        /// <summary>
        /// Обновить доступность кнопки
        /// </summary>
        private void UpdateButtonAvailability(Button button, bool available)
        {
            if (button != null)
            {
                button.interactable = available;
                
                // Визуальная индикация доступности
                var colors = button.colors;
                colors.normalColor = available ? Color.white : Color.gray;
                button.colors = colors;
            }
        }
        
        /// <summary>
        /// Обновить статус боя
        /// </summary>
        private void UpdateCombatStatus()
        {
            if (combatSystem == null || !combatSystem.IsInCombat)
                return;
            
            // Обновить информацию о текущем враге
            if (currentEnemyText != null)
            {
                var currentEnemy = combatSystem.CurrentEnemy;
                if (currentEnemy != null)
                {
                    currentEnemyText.text = $"Противник: {currentEnemy.GetDisplayName()}";
                }
            }
            
            // Обновить иконку статуса игрока
            UpdatePlayerStatusIcon();
        }
        
        /// <summary>
        /// Обновить иконку статуса игрока
        /// </summary>
        private void UpdatePlayerStatusIcon()
        {
            if (playerStatusIcon == null || combatSystem == null)
                return;
            
            switch (combatSystem.CurrentState)
            {
                case CombatState.PlayerTurn:
                    playerStatusIcon.color = Color.green;
                    break;
                case CombatState.EnemyTurn:
                    playerStatusIcon.color = Color.red;
                    break;
                case CombatState.InCombat:
                    playerStatusIcon.color = Color.yellow;
                    break;
                default:
                    playerStatusIcon.color = Color.white;
                    break;
            }
        }
        
        #endregion
        
        #region Enemy Info Display
        
        /// <summary>
        /// Обновить информацию о врагах
        /// </summary>
        private void UpdateEnemyInfo(List<Enemy> enemies)
        {
            if (enemyListParent == null || enemyInfoPrefab == null)
                return;
            
            // Очистить старую информацию
            ClearEnemyInfo();
            
            // Создать новую информацию
            foreach (var enemy in enemies)
            {
                CreateEnemyInfoObject(enemy);
            }
        }
        
        /// <summary>
        /// Создать объект информации о враге
        /// </summary>
        private void CreateEnemyInfoObject(Enemy enemy)
        {
            GameObject enemyObj = Instantiate(enemyInfoPrefab, enemyListParent);
            enemyInfoObjects.Add(enemyObj);
            
            // Настроить компоненты объекта
            var nameText = enemyObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var healthSlider = enemyObj.transform.Find("HealthSlider")?.GetComponent<Slider>();
            var healthText = enemyObj.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
            var weaponText = enemyObj.transform.Find("WeaponText")?.GetComponent<TextMeshProUGUI>();
            
            if (nameText != null)
            {
                nameText.text = enemy.GetDisplayName();
            }
            
            if (healthSlider != null)
            {
                healthSlider.value = enemy.GetHealthPercentage();
            }
            
            if (healthText != null)
            {
                healthText.text = $"{enemy.currentHealth:F0}/{enemy.maxHealth:F0}";
            }
            
            if (weaponText != null)
            {
                weaponText.text = GetWeaponName(enemy.weaponType);
            }
            
            // Добавить кнопку выбора цели (если нужно)
            var targetButton = enemyObj.GetComponent<Button>();
            if (targetButton != null)
            {
                int enemyIndex = enemyInfoObjects.Count - 1;
                targetButton.onClick.AddListener(() => SelectEnemyTarget(enemyIndex));
            }
        }
        
        /// <summary>
        /// Очистить информацию о врагах
        /// </summary>
        private void ClearEnemyInfo()
        {
            foreach (var obj in enemyInfoObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            enemyInfoObjects.Clear();
        }
        
        /// <summary>
        /// Выбрать цель-врага
        /// </summary>
        private void SelectEnemyTarget(int enemyIndex)
        {
            // TODO: Реализовать выбор цели для атаки
            if (showDebugInfo)
            {
                Debug.Log($"Выбрана цель: враг #{enemyIndex}");
            }
        }
        
        #endregion
        
        #region Combat Log
        
        /// <summary>
        /// Добавить запись в лог боя
        /// </summary>
        private void AddToCombatLog(string message)
        {
            combatLogEntries.Add($"[{Time.time:F1}] {message}");
            
            // Ограничить размер лога
            if (combatLogEntries.Count > maxLogEntries)
            {
                combatLogEntries.RemoveAt(0);
            }
            
            // Обновить текст лога
            UpdateCombatLogDisplay();
            
            // Автопрокрутка
            if (autoScrollLog)
            {
                StartCoroutine(ScrollToBottom());
            }
        }
        
        /// <summary>
        /// Обновить отображение лога
        /// </summary>
        private void UpdateCombatLogDisplay()
        {
            if (combatLogText != null)
            {
                combatLogText.text = string.Join("\n", combatLogEntries);
            }
        }
        
        /// <summary>
        /// Прокрутить лог вниз
        /// </summary>
        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            
            if (combatLogScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                combatLogScrollRect.verticalNormalizedPosition = 0f;
            }
        }
        
        /// <summary>
        /// Очистить лог боя
        /// </summary>
        private void ClearCombatLog()
        {
            combatLogEntries.Clear();
            UpdateCombatLogDisplay();
        }
        
        #endregion
        
        #region Action Handling
        
        /// <summary>
        /// Запросить боевое действие
        /// </summary>
        private void RequestCombatAction(CombatAction action)
        {
            if (!CanPerformAction())
                return;
            
            isWaitingForAction = true;
            lastActionTime = Time.time;
            
            // Анимация кнопки
            StartCoroutine(AnimateActionButton(action));
            
            // Отправить запрос на действие
            OnCombatActionRequested?.Invoke(action, 0); // TODO: Реализовать выбор цели
            
            // Передать действие в CombatSystem
            if (combatSystem != null)
            {
                combatSystem.PerformPlayerAction(action, 0);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"Запрошено действие: {action}");
            }
        }
        
        /// <summary>
        /// Проверить, можно ли выполнить действие
        /// </summary>
        private bool CanPerformAction()
        {
            return combatSystem != null &&
                   combatSystem.CurrentState == CombatState.PlayerTurn &&
                   !isWaitingForAction &&
                   Time.time - lastActionTime >= actionButtonCooldown;
        }
        
        /// <summary>
        /// Анимация кнопки действия
        /// </summary>
        private IEnumerator AnimateActionButton(CombatAction action)
        {
            Button actionButton = GetActionButton(action);
            if (actionButton == null)
                yield break;
            
            // Эффект нажатия
            Vector3 originalScale = actionButton.transform.localScale;
            actionButton.transform.localScale = originalScale * 0.9f;
            
            yield return new WaitForSeconds(0.1f);
            
            actionButton.transform.localScale = originalScale;
            
            yield return new WaitForSeconds(actionButtonCooldown - 0.1f);
            
            isWaitingForAction = false;
        }
        
        /// <summary>
        /// Получить кнопку для действия
        /// </summary>
        private Button GetActionButton(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.Attack: return attackButton;
                case CombatAction.Defend: return defendButton;
                case CombatAction.Dodge: return dodgeButton;
                case CombatAction.UseItem: return useItemButton;
                case CombatAction.Flee: return fleeButton;
                case CombatAction.Negotiate: return negotiateButton;
                case CombatAction.Intimidate: return intimidateButton;
                default: return null;
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        /// <summary>
        /// Показать эффект попадания
        /// </summary>
        private void ShowHitEffect(CombatResult result)
        {
            switch (result)
            {
                case CombatResult.Critical:
                    if (criticalEffect != null)
                    {
                        criticalEffect.Play();
                    }
                    break;
                    
                case CombatResult.Hit:
                    if (hitEffect != null)
                    {
                        hitEffect.Play();
                    }
                    break;
            }
            
            // Анимация
            if (combatAnimator != null)
            {
                combatAnimator.SetTrigger(result.ToString());
            }
        }
        
        /// <summary>
        /// Воспроизвести звук боя
        /// </summary>
        private void PlayCombatSound(CombatAction action, CombatResult result)
        {
            if (combatAudioSource == null)
                return;
            
            // TODO: Загрузить и воспроизвести соответствующие звуки
            // AudioClip soundClip = GetCombatSound(action, result);
            // if (soundClip != null)
            // {
            //     combatAudioSource.PlayOneShot(soundClip);
            // }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Обработчик изменения состояния боя
        /// </summary>
        private void OnCombatStateChanged(CombatState newState)
        {
            switch (newState)
            {
                case CombatState.NotInCombat:
                    HideCombatUI();
                    break;
                    
                case CombatState.PreCombat:
                case CombatState.InCombat:
                case CombatState.PlayerTurn:
                case CombatState.EnemyTurn:
                    ShowCombatUI();
                    break;
                    
                case CombatState.PlayerWon:
                case CombatState.PlayerLost:
                case CombatState.PlayerFled:
                    ShowCombatResult(newState);
                    break;
            }
            
            AddToCombatLog($"Состояние боя: {GetStateName(newState)}");
        }
        
        /// <summary>
        /// Обработчик начала боя
        /// </summary>
        private void OnCombatStarted(CombatData combatData)
        {
            ShowCombatUI();
            ClearCombatLog();
            
            if (combatData.enemies != null)
            {
                UpdateEnemyInfo(combatData.enemies);
            }
            
            AddToCombatLog("Бой начался!");
            
            if (showDebugInfo)
            {
                Debug.Log($"UI: Бой начался против {combatData.enemies?.Count ?? 0} врагов");
            }
        }
        
        /// <summary>
        /// Обработчик окончания боя
        /// </summary>
        private void OnCombatEnded(CombatData combatData, CombatState endState)
        {
            ShowCombatResult(endState);
            
            string resultMessage = GetCombatResultMessage(endState);
            AddToCombatLog(resultMessage);
            
            if (showDebugInfo)
            {
                Debug.Log($"UI: Бой завершен - {endState}");
            }
        }
        
        /// <summary>
        /// Обработчик боевого действия
        /// </summary>
        private void OnCombatAction(CombatAction action, CombatResult result)
        {
            string actionMessage = GetActionMessage(action, result);
            AddToCombatLog(actionMessage);
            
            ShowHitEffect(result);
            PlayCombatSound(action, result);
            
            if (showDebugInfo)
            {
                Debug.Log($"UI: Действие - {action} = {result}");
            }
        }
        
        /// <summary>
        /// Обработчик изменения здоровья игрока (от CombatSystem)
        /// </summary>
        private void OnPlayerHealthChanged(float newHealth)
        {
            // Обновление происходит в UpdatePlayerInfo()
        }
        
        /// <summary>
        /// Обработчик изменения здоровья игрока (от PlayerStats)
        /// </summary>
        private void OnPlayerStatsHealthChanged(float newHealth)
        {
            // Обновление происходит в UpdatePlayerInfo()
        }
        
        /// <summary>
        /// Обработчик изменения здоровья врага
        /// </summary>
        private void OnEnemyHealthChanged(int enemyIndex, float newHealth)
        {
            if (enemyIndex >= 0 && enemyIndex < enemyInfoObjects.Count)
            {
                var enemyObj = enemyInfoObjects[enemyIndex];
                if (enemyObj != null)
                {
                    var healthSlider = enemyObj.transform.Find("HealthSlider")?.GetComponent<Slider>();
                    var healthText = enemyObj.transform.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
                    
                    if (combatSystem != null && combatSystem.ActiveCombat != null && 
                        enemyIndex < combatSystem.ActiveCombat.enemies.Count)
                    {
                        var enemy = combatSystem.ActiveCombat.enemies[enemyIndex];
                        
                        if (healthSlider != null)
                        {
                            healthSlider.value = enemy.GetHealthPercentage();
                        }
                        
                        if (healthText != null)
                        {
                            healthText.text = $"{enemy.currentHealth:F0}/{enemy.maxHealth:F0}";
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Обработчик экипировки оружия
        /// </summary>
        private void OnWeaponEquipped(Weapon weapon)
        {
            UpdatePlayerInfo();
        }
        
        /// <summary>
        /// Обработчик снятия оружия
        /// </summary>
        private void OnWeaponUnequipped(Weapon weapon)
        {
            UpdatePlayerInfo();
        }
        
        /// <summary>
        /// Обработчик кнопки "Продолжить"
        /// </summary>
        private void OnContinueClicked()
        {
            HideCombatResult();
            HideCombatUI();
        }
        
        #endregion
        
        #region UI Display Control
        
        /// <summary>
        /// Показать UI боя
        /// </summary>
        private void ShowCombatUI()
        {
            if (combatPanel != null)
            {
                combatPanel.SetActive(true);
            }
            
            if (combatResultPanel != null)
            {
                combatResultPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Скрыть UI боя
        /// </summary>
        private void HideCombatUI()
        {
            if (combatPanel != null)
            {
                combatPanel.SetActive(false);
            }
            
            ClearEnemyInfo();
        }
        
        /// <summary>
        /// Показать результат боя
        /// </summary>
        private void ShowCombatResult(CombatState endState)
        {
            if (combatResultPanel == null)
                return;
            
            combatResultPanel.SetActive(true);
            
            if (resultTitleText != null)
            {
                resultTitleText.text = GetResultTitle(endState);
                resultTitleText.color = GetResultColor(endState);
            }
            
            if (resultDetailsText != null)
            {
                resultDetailsText.text = GetResultDetails(endState);
            }
        }
        
        /// <summary>
        /// Скрыть результат боя
        /// </summary>
        private void HideCombatResult()
        {
            if (combatResultPanel != null)
            {
                combatResultPanel.SetActive(false);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Получить название оружия
        /// </summary>
        private string GetWeaponName(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.None: return "Кулаки";
                case WeaponType.Knife: return "Нож";
                case WeaponType.Bat: return "Бита";
                case WeaponType.Club: return "Дубинка";
                case WeaponType.Pistol: return "Пистолет";
                default: return weaponType.ToString();
            }
        }
        
        /// <summary>
        /// Получить название состояния
        /// </summary>
        private string GetStateName(CombatState state)
        {
            switch (state)
            {
                case CombatState.NotInCombat: return "Не в бою";
                case CombatState.PreCombat: return "Подготовка";
                case CombatState.InCombat: return "В бою";
                case CombatState.PlayerTurn: return "Ход игрока";
                case CombatState.EnemyTurn: return "Ход врага";
                case CombatState.PlayerWon: return "Победа";
                case CombatState.PlayerLost: return "Поражение";
                case CombatState.PlayerFled: return "Побег";
                default: return state.ToString();
            }
        }
        
        /// <summary>
        /// Получить сообщение о результате боя
        /// </summary>
        private string GetCombatResultMessage(CombatState endState)
        {
            switch (endState)
            {
                case CombatState.PlayerWon: return "Вы одержали победу!";
                case CombatState.PlayerLost: return "Вы потерпели поражение...";
                case CombatState.PlayerFled: return "Вы сбежали с поля боя.";
                default: return "Бой завершен.";
            }
        }
        
        /// <summary>
        /// Получить сообщение о действии
        /// </summary>
        private string GetActionMessage(CombatAction action, CombatResult result)
        {
            string actionName = GetActionName(action);
            string resultName = GetResultName(result);
            
            return $"{actionName} - {resultName}";
        }
        
        /// <summary>
        /// Получить название действия
        /// </summary>
        private string GetActionName(CombatAction action)
        {
            switch (action)
            {
                case CombatAction.Attack: return "Атака";
                case CombatAction.Defend: return "Защита";
                case CombatAction.Dodge: return "Уклонение";
                case CombatAction.UseItem: return "Использование предмета";
                case CombatAction.Flee: return "Побег";
                case CombatAction.Negotiate: return "Переговоры";
                case CombatAction.Intimidate: return "Запугивание";
                default: return action.ToString();
            }
        }
        
        /// <summary>
        /// Получить название результата
        /// </summary>
        private string GetResultName(CombatResult result)
        {
            switch (result)
            {
                case CombatResult.Hit: return "Попадание";
                case CombatResult.Miss: return "Промах";
                case CombatResult.Critical: return "Критический удар";
                case CombatResult.Blocked: return "Заблокировано";
                case CombatResult.Dodged: return "Уклонение";
                case CombatResult.Fled: return "Побег";
                case CombatResult.Stunned: return "Оглушение";
                default: return result.ToString();
            }
        }
        
        /// <summary>
        /// Получить заголовок результата
        /// </summary>
        private string GetResultTitle(CombatState endState)
        {
            switch (endState)
            {
                case CombatState.PlayerWon: return "ПОБЕДА!";
                case CombatState.PlayerLost: return "ПОРАЖЕНИЕ";
                case CombatState.PlayerFled: return "ПОБЕГ";
                default: return "РЕЗУЛЬТАТ";
            }
        }
        
        /// <summary>
        /// Получить цвет результата
        /// </summary>
        private Color GetResultColor(CombatState endState)
        {
            switch (endState)
            {
                case CombatState.PlayerWon: return Color.green;
                case CombatState.PlayerLost: return Color.red;
                case CombatState.PlayerFled: return Color.yellow;
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// Получить детали результата
        /// </summary>
        private string GetResultDetails(CombatState endState)
        {
            if (combatSystem?.ActiveCombat == null)
                return "";
            
            var combat = combatSystem.ActiveCombat;
            string details = $"Длительность: {combat.duration:F1}с\n";
            details += $"Урон нанесен: {combat.totalDamageDealt:F0}\n";
            details += $"Урон получен: {combat.totalDamageReceived:F0}\n";
            
            if (combat.moneyGained > 0)
            {
                details += $"Получено денег: ${combat.moneyGained:F0}\n";
            }
            
            if (combat.moneyLost > 0)
            {
                details += $"Потеряно денег: ${combat.moneyLost:F0}\n";
            }
            
            if (combat.experienceGained > 0)
            {
                details += $"Получено опыта: {combat.experienceGained:F0}\n";
            }
            
            return details;
        }
        
        #endregion
        
        #region Context Menu Debug
        
        [ContextMenu("Show Test Combat UI")]
        private void DebugShowTestCombatUI()
        {
            ShowCombatUI();
            AddToCombatLog("Тестовая запись в лог боя");
        }
        
        [ContextMenu("Hide Combat UI")]
        private void DebugHideCombatUI()
        {
            HideCombatUI();
        }
        
        [ContextMenu("Test Combat Result - Victory")]
        private void DebugTestVictoryResult()
        {
            ShowCombatResult(CombatState.PlayerWon);
        }
        
        [ContextMenu("Test Combat Result - Defeat")]
        private void DebugTestDefeatResult()
        {
            ShowCombatResult(CombatState.PlayerLost);
        }
        
        [ContextMenu("Add Test Log Entry")]
        private void DebugAddTestLogEntry()
        {
            AddToCombatLog("Тестовая запись в лог");
        }
        
        [ContextMenu("Clear Combat Log")]
        private void DebugClearCombatLog()
        {
            ClearCombatLog();
        }
        
        #endregion
    }
}
