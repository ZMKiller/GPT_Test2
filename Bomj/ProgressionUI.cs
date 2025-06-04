using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HomelessToMillionaire
{
    /// <summary>
    /// UI системы прогрессии персонажа
    /// </summary>
    public class ProgressionUI : MonoBehaviour
    {
        [Header("Основные панели")]
        [SerializeField] private GameObject progressionPanel;
        [SerializeField] private GameObject skillsPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject jobCenterPanel;
        [SerializeField] private GameObject educationPanel;
        [SerializeField] private GameObject achievementsPanel;

        [Header("Кнопки навигации")]
        [SerializeField] private Button skillsTabButton;
        [SerializeField] private Button shopTabButton;
        [SerializeField] private Button jobCenterTabButton;
        [SerializeField] private Button educationTabButton;
        [SerializeField] private Button achievementsTabButton;
        [SerializeField] private Button closeButton;

        [Header("Панель навыков")]
        [SerializeField] private Transform skillsContainer;
        [SerializeField] private GameObject skillItemPrefab;
        [SerializeField] private TextMeshProUGUI availableSkillPointsText;
        [SerializeField] private Button resetSkillsButton;

        [Header("Панель магазина")]
        [SerializeField] private Transform categoryButtonsContainer;
        [SerializeField] private Transform shopItemsContainer;
        [SerializeField] private GameObject categoryButtonPrefab;
        [SerializeField] private GameObject shopItemPrefab;
        [SerializeField] private TextMeshProUGUI currentMoneyText;

        [Header("Панель центра занятости")]
        [SerializeField] private Transform jobsContainer;
        [SerializeField] private GameObject jobItemPrefab;
        [SerializeField] private TextMeshProUGUI currentJobText;
        [SerializeField] private Button quitJobButton;

        [Header("Панель образования")]
        [SerializeField] private Transform coursesContainer;
        [SerializeField] private GameObject courseItemPrefab;
        [SerializeField] private TextMeshProUGUI currentCourseText;
        [SerializeField] private Button quitCourseButton;

        [Header("Панель достижений")]
        [SerializeField] private Transform achievementsContainer;
        [SerializeField] private GameObject achievementItemPrefab;
        [SerializeField] private TextMeshProUGUI achievementStatsText;
        [SerializeField] private Slider achievementProgressSlider;

        [Header("Настройки")]
        [SerializeField] private Color unlockedTabColor = Color.white;
        [SerializeField] private Color lockedTabColor = Color.gray;
        [SerializeField] private Color selectedTabColor = Color.yellow;

        // Компоненты
        private SkillSystem skillSystem;
        private ShopSystem shopSystem;
        private JobSystem jobSystem;
        private EducationSystem educationSystem;
        private AchievementSystem achievementSystem;
        private MoneySystem moneySystem;
        private PlayerStats playerStats;

        // Состояние UI
        private ProgressionTab currentTab = ProgressionTab.Skills;
        private ShopCategory currentShopCategory = ShopCategory.Food;
        private Dictionary<Button, ProgressionTab> tabButtons = new Dictionary<Button, ProgressionTab>();
        private List<GameObject> skillItems = new List<GameObject>();
        private List<GameObject> shopItems = new List<GameObject>();
        private List<GameObject> jobItems = new List<GameObject>();
        private List<GameObject> courseItems = new List<GameObject>();
        private List<GameObject> achievementItems = new List<GameObject>();

        // События
        public event Action<ProgressionTab> OnTabChanged;
        public event Action OnProgressionPanelOpened;
        public event Action OnProgressionPanelClosed;

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            skillSystem = FindObjectOfType<SkillSystem>();
            shopSystem = FindObjectOfType<ShopSystem>();
            jobSystem = FindObjectOfType<JobSystem>();
            educationSystem = FindObjectOfType<EducationSystem>();
            achievementSystem = FindObjectOfType<AchievementSystem>();
            moneySystem = FindObjectOfType<MoneySystem>();
            playerStats = FindObjectOfType<PlayerStats>();

            // Настройка кнопок
            SetupTabButtons();
            SetupUIEvents();
        }

        private void Start()
        {
            // Скрыть панель по умолчанию
            if (progressionPanel != null)
                progressionPanel.SetActive(false);

            // Подписка на события
            SubscribeToEvents();

            // Инициализация UI
            RefreshAllPanels();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Настройка кнопок вкладок
        /// </summary>
        private void SetupTabButtons()
        {
            if (skillsTabButton != null)
            {
                tabButtons[skillsTabButton] = ProgressionTab.Skills;
                skillsTabButton.onClick.AddListener(() => SwitchTab(ProgressionTab.Skills));
            }

            if (shopTabButton != null)
            {
                tabButtons[shopTabButton] = ProgressionTab.Shop;
                shopTabButton.onClick.AddListener(() => SwitchTab(ProgressionTab.Shop));
            }

            if (jobCenterTabButton != null)
            {
                tabButtons[jobCenterTabButton] = ProgressionTab.JobCenter;
                jobCenterTabButton.onClick.AddListener(() => SwitchTab(ProgressionTab.JobCenter));
            }

            if (educationTabButton != null)
            {
                tabButtons[educationTabButton] = ProgressionTab.Education;
                educationTabButton.onClick.AddListener(() => SwitchTab(ProgressionTab.Education));
            }

            if (achievementsTabButton != null)
            {
                tabButtons[achievementsTabButton] = ProgressionTab.Achievements;
                achievementsTabButton.onClick.AddListener(() => SwitchTab(ProgressionTab.Achievements));
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseProgressionPanel);
            }
        }

        /// <summary>
        /// Настройка UI событий
        /// </summary>
        private void SetupUIEvents()
        {
            if (resetSkillsButton != null)
            {
                resetSkillsButton.onClick.AddListener(OnResetSkillsClicked);
            }

            if (quitJobButton != null)
            {
                quitJobButton.onClick.AddListener(OnQuitJobClicked);
            }

            if (quitCourseButton != null)
            {
                quitCourseButton.onClick.AddListener(OnQuitCourseClicked);
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (skillSystem != null)
                skillSystem.OnSkillUpdated += OnSkillUpdated;

            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged += OnMoneyChanged;
            }

            if (jobSystem != null)
            {
                jobSystem.OnJobStarted += OnJobStarted;
                jobSystem.OnJobCompleted += OnJobCompleted;
            }

            if (educationSystem != null)
            {
                educationSystem.OnEducationStarted += OnEducationStarted;
                educationSystem.OnEducationCompleted += OnEducationCompleted;
            }

            if (achievementSystem != null)
            {
                achievementSystem.OnAchievementUnlocked += OnAchievementUnlocked;
                achievementSystem.OnAchievementsUpdated += OnAchievementsUpdated;
            }

            GameEvents.OnLevelUp += OnLevelUp;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (skillSystem != null)
                skillSystem.OnSkillUpdated -= OnSkillUpdated;

            if (moneySystem != null)
            {
                moneySystem.OnMoneyChanged -= OnMoneyChanged;
            }

            if (jobSystem != null)
            {
                jobSystem.OnJobStarted -= OnJobStarted;
                jobSystem.OnJobCompleted -= OnJobCompleted;
            }

            if (educationSystem != null)
            {
                educationSystem.OnEducationStarted -= OnEducationStarted;
                educationSystem.OnEducationCompleted -= OnEducationCompleted;
            }

            if (achievementSystem != null)
            {
                achievementSystem.OnAchievementUnlocked -= OnAchievementUnlocked;
                achievementSystem.OnAchievementsUpdated -= OnAchievementsUpdated;
            }

            GameEvents.OnLevelUp -= OnLevelUp;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Открыть панель прогрессии
        /// </summary>
        public void OpenProgressionPanel()
        {
            if (progressionPanel != null)
            {
                progressionPanel.SetActive(true);
                RefreshAllPanels();
                OnProgressionPanelOpened?.Invoke();
            }
        }

        /// <summary>
        /// Закрыть панель прогрессии
        /// </summary>
        public void CloseProgressionPanel()
        {
            if (progressionPanel != null)
            {
                progressionPanel.SetActive(false);
                OnProgressionPanelClosed?.Invoke();
            }
        }

        /// <summary>
        /// Переключить вкладку
        /// </summary>
        public void SwitchTab(ProgressionTab tab)
        {
            if (currentTab == tab) return;

            currentTab = tab;
            UpdateTabVisuals();
            RefreshCurrentPanel();
            OnTabChanged?.Invoke(tab);
        }

        /// <summary>
        /// Получить текущую вкладку
        /// </summary>
        public ProgressionTab GetCurrentTab()
        {
            return currentTab;
        }

        /// <summary>
        /// Обновить все панели
        /// </summary>
        public void RefreshAllPanels()
        {
            RefreshSkillsPanel();
            RefreshShopPanel();
            RefreshJobCenterPanel();
            RefreshEducationPanel();
            RefreshAchievementsPanel();
            UpdateTabVisuals();
        }

        #endregion

        #region Tab Management

        /// <summary>
        /// Обновить визуальное состояние вкладок
        /// </summary>
        private void UpdateTabVisuals()
        {
            // Скрыть все панели
            HideAllPanels();

            // Показать нужную панель
            switch (currentTab)
            {
                case ProgressionTab.Skills:
                    if (skillsPanel != null) skillsPanel.SetActive(true);
                    break;
                case ProgressionTab.Shop:
                    if (shopPanel != null) shopPanel.SetActive(true);
                    break;
                case ProgressionTab.JobCenter:
                    if (jobCenterPanel != null) jobCenterPanel.SetActive(true);
                    break;
                case ProgressionTab.Education:
                    if (educationPanel != null) educationPanel.SetActive(true);
                    break;
                case ProgressionTab.Achievements:
                    if (achievementsPanel != null) achievementsPanel.SetActive(true);
                    break;
            }

            // Обновить цвета кнопок
            UpdateTabButtonColors();
        }

        /// <summary>
        /// Скрыть все панели
        /// </summary>
        private void HideAllPanels()
        {
            if (skillsPanel != null) skillsPanel.SetActive(false);
            if (shopPanel != null) shopPanel.SetActive(false);
            if (jobCenterPanel != null) jobCenterPanel.SetActive(false);
            if (educationPanel != null) educationPanel.SetActive(false);
            if (achievementsPanel != null) achievementsPanel.SetActive(false);
        }

        /// <summary>
        /// Обновить цвета кнопок вкладок
        /// </summary>
        private void UpdateTabButtonColors()
        {
            foreach (var kvp in tabButtons)
            {
                Button button = kvp.Key;
                ProgressionTab tab = kvp.Value;

                if (button != null)
                {
                    ColorBlock colors = button.colors;
                    
                    if (tab == currentTab)
                    {
                        colors.normalColor = selectedTabColor;
                    }
                    else if (IsTabUnlocked(tab))
                    {
                        colors.normalColor = unlockedTabColor;
                    }
                    else
                    {
                        colors.normalColor = lockedTabColor;
                    }

                    button.colors = colors;
                    button.interactable = IsTabUnlocked(tab);
                }
            }
        }

        /// <summary>
        /// Проверить, разблокирована ли вкладка
        /// </summary>
        private bool IsTabUnlocked(ProgressionTab tab)
        {
            if (playerStats == null) return true;

            switch (tab)
            {
                case ProgressionTab.Skills:
                    return true; // Всегда доступно

                case ProgressionTab.Shop:
                    return playerStats.GetLevel() >= 2; // Доступно с 2 уровня

                case ProgressionTab.JobCenter:
                    return playerStats.GetLevel() >= 3; // Доступно с 3 уровня

                case ProgressionTab.Education:
                    return playerStats.GetLevel() >= 5; // Доступно с 5 уровня

                case ProgressionTab.Achievements:
                    return playerStats.GetLevel() >= 1; // Доступно с 1 уровня

                default:
                    return true;
            }
        }

        /// <summary>
        /// Обновить текущую панель
        /// </summary>
        private void RefreshCurrentPanel()
        {
            switch (currentTab)
            {
                case ProgressionTab.Skills:
                    RefreshSkillsPanel();
                    break;
                case ProgressionTab.Shop:
                    RefreshShopPanel();
                    break;
                case ProgressionTab.JobCenter:
                    RefreshJobCenterPanel();
                    break;
                case ProgressionTab.Education:
                    RefreshEducationPanel();
                    break;
                case ProgressionTab.Achievements:
                    RefreshAchievementsPanel();
                    break;
            }
        }

        #endregion

        #region Skills Panel

        /// <summary>
        /// Обновить панель навыков
        /// </summary>
        private void RefreshSkillsPanel()
        {
            if (skillSystem == null || skillsContainer == null || skillItemPrefab == null) return;

            // Очистить существующие элементы
            ClearContainer(skillsContainer, skillItems);

            // Обновить текст доступных очков
            if (availableSkillPointsText != null)
            {
                int availablePoints = skillSystem.GetAvailableSkillPoints();
                availableSkillPointsText.text = $"Доступно очков: {availablePoints}";
            }

            // Создать элементы навыков
            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                CreateSkillItem(skillType);
            }

            // Обновить кнопку сброса
            if (resetSkillsButton != null)
            {
                resetSkillsButton.interactable = skillSystem.GetAvailableSkillPoints() < skillSystem.GetTotalSpentSkillPoints();
            }
        }

        /// <summary>
        /// Создать элемент навыка
        /// </summary>
        private void CreateSkillItem(SkillType skillType)
        {
            GameObject skillItem = Instantiate(skillItemPrefab, skillsContainer);
            skillItems.Add(skillItem);

            // Получить компоненты
            var nameText = skillItem.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var levelText = skillItem.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();
            var upgradeButton = skillItem.transform.Find("UpgradeButton")?.GetComponent<Button>();
            var costText = skillItem.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            var bonusText = skillItem.transform.Find("BonusText")?.GetComponent<TextMeshProUGUI>();

            // Заполнить данные
            int currentLevel = skillSystem.GetSkillLevel(skillType);
            int upgradeCost = skillSystem.GetSkillUpgradeCost(skillType);
            bool canUpgrade = skillSystem.CanUpgradeSkill(skillType);

            if (nameText != null)
                nameText.text = skillSystem.GetSkillName(skillType);

            if (levelText != null)
                levelText.text = $"Уровень: {currentLevel}";

            if (costText != null)
                costText.text = $"Стоимость: {upgradeCost}";

            if (bonusText != null)
                bonusText.text = GetSkillBonusDescription(skillType, currentLevel);

            if (upgradeButton != null)
            {
                upgradeButton.interactable = canUpgrade;
                upgradeButton.onClick.AddListener(() => OnSkillUpgradeClicked(skillType));
            }
        }

        /// <summary>
        /// Получить описание бонуса навыка
        /// </summary>
        private string GetSkillBonusDescription(SkillType skillType, int level)
        {
            if (skillSystem == null) return "";

            float bonus = skillSystem.GetSkillBonus(skillType);
            
            switch (skillType)
            {
                case SkillType.Charisma:
                    return $"Доход от NPC: +{bonus:P0}";
                case SkillType.Education:
                    return $"Скорость обучения: +{bonus:P0}";
                case SkillType.Fitness:
                    return $"Здоровье: +{bonus:P0}";
                case SkillType.Luck:
                    return $"Удача: +{bonus:P0}";
                case SkillType.Business:
                    return $"Доход от работы: +{bonus:P0}";
                default:
                    return $"Бонус: +{bonus:P0}";
            }
        }

        #endregion

        #region Shop Panel

        /// <summary>
        /// Обновить панель магазина
        /// </summary>
        private void RefreshShopPanel()
        {
            if (shopSystem == null) return;

            RefreshShopCategories();
            RefreshShopItems();
            UpdateMoneyDisplay();
        }

        /// <summary>
        /// Обновить категории магазина
        /// </summary>
        private void RefreshShopCategories()
        {
            if (categoryButtonsContainer == null || categoryButtonPrefab == null) return;

            // Очистить существующие кнопки
            foreach (Transform child in categoryButtonsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создать кнопки категорий
            var availableCategories = shopSystem.GetAvailableCategories();
            foreach (var category in availableCategories)
            {
                CreateCategoryButton(category);
            }
        }

        /// <summary>
        /// Создать кнопку категории
        /// </summary>
        private void CreateCategoryButton(ShopCategory category)
        {
            GameObject categoryButton = Instantiate(categoryButtonPrefab, categoryButtonsContainer);
            
            var button = categoryButton.GetComponent<Button>();
            var text = categoryButton.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
                text.text = GetCategoryDisplayName(category);

            if (button != null)
            {
                button.onClick.AddListener(() => {
                    currentShopCategory = category;
                    RefreshShopItems();
                });

                // Выделить текущую категорию
                ColorBlock colors = button.colors;
                colors.normalColor = (category == currentShopCategory) ? selectedTabColor : unlockedTabColor;
                button.colors = colors;
            }
        }

        /// <summary>
        /// Обновить товары магазина
        /// </summary>
        private void RefreshShopItems()
        {
            if (shopSystem == null || shopItemsContainer == null || shopItemPrefab == null) return;

            // Очистить существующие элементы
            ClearContainer(shopItemsContainer, shopItems);

            // Получить товары категории
            var items = shopSystem.GetAvailableItemsByCategory(currentShopCategory);
            foreach (var item in items)
            {
                CreateShopItem(item);
            }
        }

        /// <summary>
        /// Создать элемент товара
        /// </summary>
        private void CreateShopItem(ShopItem item)
        {
            GameObject shopItemObj = Instantiate(shopItemPrefab, shopItemsContainer);
            shopItems.Add(shopItemObj);

            // Получить компоненты
            var nameText = shopItemObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var priceText = shopItemObj.transform.Find("PriceText")?.GetComponent<TextMeshProUGUI>();
            var buyButton = shopItemObj.transform.Find("BuyButton")?.GetComponent<Button>();
            var descriptionText = shopItemObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var qualityImage = shopItemObj.transform.Find("QualityImage")?.GetComponent<Image>();

            // Заполнить данные
            bool canBuy = shopSystem.CanBuyItem(item.id);

            if (nameText != null)
                nameText.text = item.name;

            if (priceText != null)
                priceText.text = $"${item.price:F0}";

            if (descriptionText != null)
                descriptionText.text = item.description;

            if (qualityImage != null)
                qualityImage.color = GetQualityColor(item.quality);

            if (buyButton != null)
            {
                buyButton.interactable = canBuy;
                buyButton.onClick.AddListener(() => OnShopItemBuyClicked(item.id));
            }
        }

        /// <summary>
        /// Получить отображаемое имя категории
        /// </summary>
        private string GetCategoryDisplayName(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Food: return "Еда";
                case ShopCategory.Clothing: return "Одежда";
                case ShopCategory.Housing: return "Жилье";
                case ShopCategory.Transport: return "Транспорт";
                case ShopCategory.Education: return "Образование";
                default: return category.ToString();
            }
        }

        /// <summary>
        /// Получить цвет качества товара
        /// </summary>
        private Color GetQualityColor(ItemQuality quality)
        {
            switch (quality)
            {
                case ItemQuality.Poor: return Color.gray;
                case ItemQuality.Common: return Color.white;
                case ItemQuality.Good: return Color.green;
                case ItemQuality.Excellent: return Color.blue;
                case ItemQuality.Luxury: return Color.magenta;
                default: return Color.white;
            }
        }

        #endregion

        #region Job Center Panel

        /// <summary>
        /// Обновить панель центра занятости
        /// </summary>
        private void RefreshJobCenterPanel()
        {
            if (jobSystem == null) return;

            UpdateCurrentJobDisplay();
            RefreshJobsList();
        }

        /// <summary>
        /// Обновить отображение текущей работы
        /// </summary>
        private void UpdateCurrentJobDisplay()
        {
            if (currentJobText == null) return;

            var currentJob = jobSystem.GetCurrentJob();
            if (currentJob != null)
            {
                float progress = jobSystem.GetCurrentJobProgress();
                currentJobText.text = $"Текущая работа: {GetJobDisplayName(currentJob.jobType)}\nПрогресс: {progress:P0}";
                
                if (quitJobButton != null)
                    quitJobButton.gameObject.SetActive(true);
            }
            else
            {
                currentJobText.text = "Нет текущей работы";
                
                if (quitJobButton != null)
                    quitJobButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Обновить список работ
        /// </summary>
        private void RefreshJobsList()
        {
            if (jobsContainer == null || jobItemPrefab == null) return;

            // Очистить существующие элементы
            ClearContainer(jobsContainer, jobItems);

            // Получить доступные работы
            var availableJobs = jobSystem.GetAvailableJobs();
            foreach (var job in availableJobs)
            {
                CreateJobItem(job);
            }
        }

        /// <summary>
        /// Создать элемент работы
        /// </summary>
        private void CreateJobItem(Job job)
        {
            GameObject jobItem = Instantiate(jobItemPrefab, jobsContainer);
            jobItems.Add(jobItem);

            // Получить компоненты
            var nameText = jobItem.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var salaryText = jobItem.transform.Find("SalaryText")?.GetComponent<TextMeshProUGUI>();
            var startButton = jobItem.transform.Find("StartButton")?.GetComponent<Button>();
            var requirementsText = jobItem.transform.Find("RequirementsText")?.GetComponent<TextMeshProUGUI>();

            // Заполнить данные
            bool canStart = jobSystem.CanStartJob(job);

            if (nameText != null)
                nameText.text = GetJobDisplayName(job.jobType);

            if (salaryText != null)
                salaryText.text = $"Зарплата: ${job.payment:F0}/час";

            if (requirementsText != null)
                requirementsText.text = GetJobRequirementsText(job);

            if (startButton != null)
            {
                startButton.interactable = canStart;
                startButton.onClick.AddListener(() => OnJobStartClicked(job.jobType));
            }
        }

        /// <summary>
        /// Получить отображаемое имя работы
        /// </summary>
        private string GetJobDisplayName(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.Cleaner: return "Уборщик";
                case JobType.Security: return "Охранник";
                case JobType.Cashier: return "Кассир";
                case JobType.Salesman: return "Продавец";
                case JobType.Manager: return "Менеджер";
                case JobType.CEO: return "Директор";
                case JobType.Entrepreneur: return "Предприниматель";
                default: return jobType.ToString();
            }
        }

        /// <summary>
        /// Получить текст требований работы
        /// </summary>
        private string GetJobRequirementsText(Job job)
        {
            var requirements = new List<string>();

            if (job.levelRequirement > 1)
                requirements.Add($"Уровень: {job.levelRequirement}");

            if (job.skillRequirements != null)
            {
                foreach (var req in job.skillRequirements)
                {
                    if (req.Value > 0)
                        requirements.Add($"{GetSkillDisplayName(req.Key)}: {req.Value}");
                }
            }

            return requirements.Count > 0 ? string.Join(", ", requirements) : "Нет требований";
        }

        /// <summary>
        /// Получить отображаемое имя навыка
        /// </summary>
        private string GetSkillDisplayName(SkillType skillType)
        {
            if (skillSystem != null)
                return skillSystem.GetSkillName(skillType);

            switch (skillType)
            {
                case SkillType.Charisma: return "Харизма";
                case SkillType.Education: return "Образование";
                case SkillType.Fitness: return "Физ. форма";
                case SkillType.Luck: return "Удача";
                case SkillType.Business: return "Бизнес";
                default: return skillType.ToString();
            }
        }

        #endregion

        #region Education Panel

        /// <summary>
        /// Обновить панель образования
        /// </summary>
        private void RefreshEducationPanel()
        {
            if (educationSystem == null) return;

            UpdateCurrentCourseDisplay();
            RefreshCoursesList();
        }

        /// <summary>
        /// Обновить отображение текущего курса
        /// </summary>
        private void UpdateCurrentCourseDisplay()
        {
            if (currentCourseText == null) return;

            var currentCourse = educationSystem.GetCurrentCourse();
            if (currentCourse != null)
            {
                float progress = educationSystem.GetCurrentCourseProgress();
                currentCourseText.text = $"Текущий курс: {GetEducationDisplayName(currentCourse.educationType)}\nПрогресс: {progress:P0}";
                
                if (quitCourseButton != null)
                    quitCourseButton.gameObject.SetActive(true);
            }
            else
            {
                currentCourseText.text = "Нет текущего курса";
                
                if (quitCourseButton != null)
                    quitCourseButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Обновить список курсов
        /// </summary>
        private void RefreshCoursesList()
        {
            if (coursesContainer == null || courseItemPrefab == null) return;

            // Очистить существующие элементы
            ClearContainer(coursesContainer, courseItems);

            // Получить доступные курсы
            var availableCourses = educationSystem.GetAvailableCourses();
            foreach (var course in availableCourses)
            {
                CreateCourseItem(course);
            }
        }

        /// <summary>
        /// Создать элемент курса
        /// </summary>
        private void CreateCourseItem(EducationCourse course)
        {
            GameObject courseItem = Instantiate(courseItemPrefab, coursesContainer);
            courseItems.Add(courseItem);

            // Получить компоненты
            var nameText = courseItem.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            var costText = courseItem.transform.Find("CostText")?.GetComponent<TextMeshProUGUI>();
            var startButton = courseItem.transform.Find("StartButton")?.GetComponent<Button>();
            var requirementsText = courseItem.transform.Find("RequirementsText")?.GetComponent<TextMeshProUGUI>();
            var rewardText = courseItem.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();

            // Заполнить данные
            bool canStart = educationSystem.CanStartCourse(course);

            if (nameText != null)
                nameText.text = GetEducationDisplayName(course.educationType);

            if (costText != null)
                costText.text = $"Стоимость: ${course.cost:F0}";

            if (requirementsText != null)
                requirementsText.text = GetEducationRequirementsText(course);

            if (rewardText != null)
                rewardText.text = GetEducationRewardText(course);

            if (startButton != null)
            {
                startButton.interactable = canStart;
                startButton.onClick.AddListener(() => OnCourseStartClicked(course.educationType));
            }
        }

        /// <summary>
        /// Получить отображаемое имя образования
        /// </summary>
        private string GetEducationDisplayName(EducationType educationType)
        {
            switch (educationType)
            {
                case EducationType.ComputerCourse: return "Компьютерные курсы";
                case EducationType.LanguageCourse: return "Языковые курсы";
                case EducationType.BusinessSchool: return "Бизнес-школа";
                case EducationType.University: return "Университет";
                case EducationType.MBA: return "MBA";
                default: return educationType.ToString();
            }
        }

        /// <summary>
        /// Получить текст требований образования
        /// </summary>
        private string GetEducationRequirementsText(EducationCourse course)
        {
            var requirements = new List<string>();

            if (course.levelRequirement > 1)
                requirements.Add($"Уровень: {course.levelRequirement}");

            if (course.prerequisites != null)
            {
                foreach (var req in course.prerequisites)
                {
                    if (req.Value > 0)
                        requirements.Add($"{GetSkillDisplayName(req.Key)}: {req.Value}");
                }
            }

            return requirements.Count > 0 ? string.Join(", ", requirements) : "Нет требований";
        }

        /// <summary>
        /// Получить текст наград образования
        /// </summary>
        private string GetEducationRewardText(EducationCourse course)
        {
            var rewards = new List<string>();

            if (course.skillRewards != null)
            {
                foreach (var improvement in course.skillRewards)
                {
                    if (improvement.Value > 0)
                        rewards.Add($"{GetSkillDisplayName(improvement.Key)}: +{improvement.Value}");
                }
            }

            return rewards.Count > 0 ? string.Join(", ", rewards) : "Нет наград";
        }

        #endregion

        #region Achievements Panel

        /// <summary>
        /// Обновить панель достижений
        /// </summary>
        private void RefreshAchievementsPanel()
        {
            if (achievementSystem == null) return;

            UpdateAchievementStats();
            RefreshAchievementsList();
        }

        /// <summary>
        /// Обновить статистику достижений
        /// </summary>
        private void UpdateAchievementStats()
        {
            if (achievementStatsText != null)
            {
                int unlocked = achievementSystem.GetUnlockedCount();
                int total = achievementSystem.GetTotalCount();
                float progress = achievementSystem.GetOverallProgress();

                achievementStatsText.text = $"Достижений разблокировано: {unlocked}/{total} ({progress:P0})";
            }

            if (achievementProgressSlider != null)
            {
                achievementProgressSlider.value = achievementSystem.GetOverallProgress();
            }
        }

        /// <summary>
        /// Обновить список достижений
        /// </summary>
        private void RefreshAchievementsList()
        {
            if (achievementsContainer == null || achievementItemPrefab == null) return;

            // Очистить существующие элементы
            ClearContainer(achievementsContainer, achievementItems);

            // Получить все достижения
            var achievements = achievementSystem.GetAllAchievements();
            foreach (var achievement in achievements)
            {
                CreateAchievementItem(achievement);
            }
        }

        /// <summary>
        /// Создать элемент достижения
        /// </summary>
        private void CreateAchievementItem(Achievement achievement)
        {
            GameObject achievementItem = Instantiate(achievementItemPrefab, achievementsContainer);
            achievementItems.Add(achievementItem);

            // Получить компоненты
            var titleText = achievementItem.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            var descriptionText = achievementItem.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var progressSlider = achievementItem.transform.Find("ProgressSlider")?.GetComponent<Slider>();
            var iconImage = achievementItem.transform.Find("IconImage")?.GetComponent<Image>();
            var rewardText = achievementItem.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();

            // Заполнить данные
            bool isUnlocked = achievementSystem.IsAchievementUnlocked(achievement.id);
            float progress = achievementSystem.GetAchievementProgress(achievement.id);

            if (titleText != null)
                titleText.text = achievement.title;

            if (descriptionText != null)
                descriptionText.text = achievement.description;

            if (progressSlider != null)
            {
                progressSlider.value = progress;
                progressSlider.gameObject.SetActive(!isUnlocked);
            }

            if (iconImage != null)
            {
                iconImage.color = isUnlocked ? Color.white : Color.gray;
            }

            if (rewardText != null)
            {
                rewardText.text = GetAchievementRewardText(achievement);
                rewardText.gameObject.SetActive(isUnlocked);
            }
        }

        /// <summary>
        /// Получить текст наград достижения
        /// </summary>
        private string GetAchievementRewardText(Achievement achievement)
        {
            if (achievement.rewards == null || achievement.rewards.Length == 0)
                return "Нет наград";

            var rewards = new List<string>();
            foreach (var reward in achievement.rewards)
            {
                switch (reward.type)
                {
                    case RewardType.Money:
                        rewards.Add($"${reward.amount:F0}");
                        break;
                    case RewardType.Experience:
                        rewards.Add($"{reward.amount} опыта");
                        break;
                    case RewardType.SkillPoints:
                        rewards.Add($"{reward.amount} очков навыков");
                        break;
                }
            }

            return "Награды: " + string.Join(", ", rewards);
        }

        #endregion

        #region UI Helpers

        /// <summary>
        /// Очистить контейнер от элементов
        /// </summary>
        private void ClearContainer(Transform container, List<GameObject> items)
        {
            foreach (var item in items)
            {
                if (item != null)
                    Destroy(item);
            }
            items.Clear();
        }

        /// <summary>
        /// Обновить отображение денег
        /// </summary>
        private void UpdateMoneyDisplay()
        {
            if (currentMoneyText != null && moneySystem != null)
            {
                currentMoneyText.text = $"Деньги: ${moneySystem.GetMoney():F0}";
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Клик по улучшению навыка
        /// </summary>
        private void OnSkillUpgradeClicked(SkillType skillType)
        {
            if (skillSystem != null)
            {
                skillSystem.UpgradeSkill(skillType);
            }
        }

        /// <summary>
        /// Клик по сбросу навыков
        /// </summary>
        private void OnResetSkillsClicked()
        {
            if (skillSystem != null)
            {
                // Показать диалог подтверждения
                // skillSystem.ResetAllSkills();
                Debug.Log("Сброс навыков пока не реализован");
            }
        }

        /// <summary>
        /// Клик по покупке товара
        /// </summary>
        private void OnShopItemBuyClicked(string itemId)
        {
            if (shopSystem != null)
            {
                var item = shopSystem.GetItemById(itemId);
                if (item != null)
                    shopSystem.BuyItem(item);
            }
        }

        /// <summary>
        /// Клик по началу работы
        /// </summary>
        private void OnJobStartClicked(JobType jobType)
        {
            if (jobSystem != null)
            {
                var job = jobSystem.GetJobsByType(jobType).FirstOrDefault();
                if (job != null)
                    jobSystem.StartJob(job);
            }
        }

        /// <summary>
        /// Клик по завершению работы
        /// </summary>
        private void OnQuitJobClicked()
        {
            if (jobSystem != null)
            {
                jobSystem.QuitJob();
            }
        }

        /// <summary>
        /// Клик по началу курса
        /// </summary>
        private void OnCourseStartClicked(EducationType educationType)
        {
            if (educationSystem != null)
            {
                var course = educationSystem.GetCoursesByType(educationType).FirstOrDefault();
                if (course != null)
                    educationSystem.StartCourse(course);
            }
        }

        /// <summary>
        /// Клик по завершению курса
        /// </summary>
        private void OnQuitCourseClicked()
        {
            if (educationSystem != null)
            {
                educationSystem.QuitCourse();
            }
        }

        /// <summary>
        /// Обработчик обновления навыка
        /// </summary>
        private void OnSkillUpdated(SkillType skillType, int newLevel)
        {
            if (currentTab == ProgressionTab.Skills)
            {
                RefreshSkillsPanel();
            }

            // Обновить другие панели, которые зависят от навыков
            if (currentTab == ProgressionTab.JobCenter)
            {
                RefreshJobCenterPanel();
            }

            if (currentTab == ProgressionTab.Education)
            {
                RefreshEducationPanel();
            }
        }

        /// <summary>
        /// Обработчик изменения денег
        /// </summary>
        private void OnMoneyChanged(double newAmount, double change, string source)
        {
            UpdateMoneyDisplay();

            if (currentTab == ProgressionTab.Shop)
            {
                RefreshShopPanel();
            }
        }

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            // Обновить доступность вкладок
            UpdateTabVisuals();

            // Обновить текущую панель
            RefreshCurrentPanel();
        }

        /// <summary>
        /// Обработчик начала работы
        /// </summary>
        private void OnJobStarted(Job job)
        {
            if (currentTab == ProgressionTab.JobCenter)
            {
                RefreshJobCenterPanel();
            }
        }

        /// <summary>
        /// Обработчик завершения работы
        /// </summary>
        private void OnJobCompleted(Job job, JobResult result)
        {
            if (currentTab == ProgressionTab.JobCenter)
            {
                RefreshJobCenterPanel();
            }
        }

        /// <summary>
        /// Обработчик начала образования
        /// </summary>
        private void OnEducationStarted(EducationCourse course)
        {
            if (currentTab == ProgressionTab.Education)
            {
                RefreshEducationPanel();
            }
        }

        /// <summary>
        /// Обработчик завершения образования
        /// </summary>
        private void OnEducationCompleted(EducationCourse course, EducationResult result)
        {
            if (currentTab == ProgressionTab.Education)
            {
                RefreshEducationPanel();
            }
        }

        /// <summary>
        /// Обработчик разблокировки достижения
        /// </summary>
        private void OnAchievementUnlocked(Achievement achievement)
        {
            if (currentTab == ProgressionTab.Achievements)
            {
                RefreshAchievementsPanel();
            }
        }

        /// <summary>
        /// Обработчик обновления достижений
        /// </summary>
        private void OnAchievementsUpdated(List<Achievement> achievements)
        {
            if (currentTab == ProgressionTab.Achievements)
            {
                RefreshAchievementsPanel();
            }
        }

        #endregion
    }

    /// <summary>
    /// Типы вкладок прогрессии
    /// </summary>
    public enum ProgressionTab
    {
        Skills,
        Shop,
        JobCenter,
        Education,
        Achievements
    }
}
