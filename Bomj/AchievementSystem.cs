using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система достижений
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        [Header("Настройки достижений")]
        [SerializeField] private bool enableAchievements = true;
        [SerializeField] private bool showSecretAchievements = false;
        [SerializeField] private float notificationDuration = 5f;

        [Header("Звуки")]
        [SerializeField] private AudioClip achievementUnlockedSound;
        [SerializeField] private AudioClip milestoneSound;
        [SerializeField] private AudioClip secretAchievementSound;

        // Компоненты
        private PlayerStats playerStats;
        private SkillSystem skillSystem;
        private MoneySystem moneySystem;
        private LevelSystem levelSystem;
        private AudioSource audioSource;

        // Достижения
        private List<Achievement> allAchievements = new List<Achievement>();
        private List<Achievement> unlockedAchievements = new List<Achievement>();
        private Dictionary<string, AchievementProgress> achievementProgress = new Dictionary<string, AchievementProgress>();

        // События
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<Achievement, float> OnAchievementProgress;
        public event Action<List<Achievement>> OnAchievementsUpdated;

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            playerStats = GetComponent<PlayerStats>();
            skillSystem = GetComponent<SkillSystem>();
            moneySystem = FindObjectOfType<MoneySystem>();
            levelSystem = GetComponent<LevelSystem>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Инициализация достижений
            InitializeAchievements();
        }

        private void Start()
        {
            if (!enableAchievements)
            {
                enabled = false;
                return;
            }

            if (playerStats == null)
            {
                Debug.LogError("AchievementSystem: PlayerStats не найден!");
                enabled = false;
                return;
            }

            // Подписка на события
            SubscribeToEvents();

            Debug.Log("AchievementSystem инициализирован");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация достижений
        /// </summary>
        private void InitializeAchievements()
        {
            CreateAchievements();
            InitializeProgress();
        }

        /// <summary>
        /// Создание всех достижений
        /// </summary>
        private void CreateAchievements()
        {
            allAchievements.Clear();

            // Достижения уровня
            CreateLevelAchievements();

            // Достижения денег
            CreateMoneyAchievements();

            // Достижения навыков
            CreateSkillAchievements();

            // Достижения работы
            CreateJobAchievements();

            // Достижения покупок
            CreatePurchaseAchievements();

            // Достижения выживания
            CreateSurvivalAchievements();

            // Социальные достижения
            CreateSocialAchievements();

            // Достижения образования
            CreateEducationAchievements();

            // Специальные достижения
            CreateSpecialAchievements();
        }

        /// <summary>
        /// Создание достижений уровня
        /// </summary>
        private void CreateLevelAchievements()
        {
            allAchievements.Add(new Achievement(
                "first_steps", "Первые шаги", "Достигните 5 уровня",
                AchievementType.Level, AchievementCategory.Progress, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 100),
                    new AchievementReward(RewardType.SkillPoints, 2)
                }
            ));

            allAchievements.Add(new Achievement(
                "getting_serious", "Всерьез взялся", "Достигните 15 уровня",
                AchievementType.Level, AchievementCategory.Progress, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 500),
                    new AchievementReward(RewardType.SkillPoints, 5)
                }
            ));

            allAchievements.Add(new Achievement(
                "expert", "Эксперт", "Достигните 30 уровня",
                AchievementType.Level, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 2000),
                    new AchievementReward(RewardType.SkillPoints, 10)
                }
            ));

            allAchievements.Add(new Achievement(
                "master", "Мастер", "Достигните максимального уровня",
                AchievementType.Level, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 10000),
                    new AchievementReward(RewardType.SkillPoints, 20)
                }
            ));
        }

        /// <summary>
        /// Создание достижений денег
        /// </summary>
        private void CreateMoneyAchievements()
        {
            allAchievements.Add(new Achievement(
                "first_hundred", "Первая сотня", "Накопите 100 денег",
                AchievementType.Money, AchievementCategory.Progress, false,
                new AchievementReward[] { new AchievementReward(RewardType.Experience, 50) }
            ));

            allAchievements.Add(new Achievement(
                "thousand_club", "Клуб тысячников", "Накопите 1000 денег",
                AchievementType.Money, AchievementCategory.Progress, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 3) }
            ));

            allAchievements.Add(new Achievement(
                "first_millionaire", "Первый миллион", "Накопите 1,000,000 денег",
                AchievementType.Money, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 100000),
                    new AchievementReward(RewardType.SkillPoints, 15)
                }
            ));

            allAchievements.Add(new Achievement(
                "money_maker", "Делатель денег", "Заработайте 10,000 денег за всё время",
                AchievementType.Money, AchievementCategory.Challenge, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 5) }
            ));
        }

        /// <summary>
        /// Создание достижений навыков
        /// </summary>
        private void CreateSkillAchievements()
        {
            var skillTypes = Enum.GetValues(typeof(SkillType)).Cast<SkillType>();
            
            foreach (var skill in skillTypes)
            {
                string skillName = GetSkillDisplayName(skill);
                
                allAchievements.Add(new Achievement(
                    $"skill_{skill.ToString().ToLower()}_master", $"Мастер {skillName}",
                    $"Достигните 50 уровня навыка {skillName}",
                    AchievementType.Skill, AchievementCategory.Progress, false,
                    new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 5) }
                ));
            }

            allAchievements.Add(new Achievement(
                "jack_of_all_trades", "Мастер на все руки", "Достигните 25 уровня во всех навыках",
                AchievementType.Skill, AchievementCategory.Challenge, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.SkillPoints, 20),
                    new AchievementReward(RewardType.Money, 5000)
                }
            ));
        }

        /// <summary>
        /// Создание достижений работы
        /// </summary>
        private void CreateJobAchievements()
        {
            allAchievements.Add(new Achievement(
                "first_job", "Первая работа", "Выполните первую работу",
                AchievementType.Job, AchievementCategory.Progress, false,
                new AchievementReward[] { new AchievementReward(RewardType.Experience, 100) }
            ));

            allAchievements.Add(new Achievement(
                "hard_worker", "Трудяга", "Выполните 50 работ",
                AchievementType.Job, AchievementCategory.Challenge, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 8) }
            ));

            allAchievements.Add(new Achievement(
                "ceo_achieved", "Достигнут CEO", "Получите работу CEO",
                AchievementType.Job, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 50000),
                    new AchievementReward(RewardType.SkillPoints, 15)
                }
            ));
        }

        /// <summary>
        /// Создание достижений покупок
        /// </summary>
        private void CreatePurchaseAchievements()
        {
            allAchievements.Add(new Achievement(
                "first_purchase", "Первая покупка", "Совершите первую покупку",
                AchievementType.Purchase, AchievementCategory.Progress, false,
                new AchievementReward[] { new AchievementReward(RewardType.Experience, 25) }
            ));

            allAchievements.Add(new Achievement(
                "luxury_buyer", "Покупатель люкса", "Купите товар люксового качества",
                AchievementType.Purchase, AchievementCategory.Milestone, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 5) }
            ));

            allAchievements.Add(new Achievement(
                "big_spender", "Большой транжира", "Потратьте 100,000 денег за всё время",
                AchievementType.Purchase, AchievementCategory.Challenge, false,
                new AchievementReward[] { new AchievementReward(RewardType.Money, 10000) }
            ));
        }

        /// <summary>
        /// Создание достижений выживания
        /// </summary>
        private void CreateSurvivalAchievements()
        {
            allAchievements.Add(new Achievement(
                "survivor", "Выживший", "Играйте 1 час без смерти",
                AchievementType.Survival, AchievementCategory.Challenge, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 3) }
            ));

            allAchievements.Add(new Achievement(
                "iron_man", "Железный человек", "Достигните 20 уровня без смерти",
                AchievementType.Survival, AchievementCategory.Challenge, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.SkillPoints, 10),
                    new AchievementReward(RewardType.Money, 2000)
                }
            ));
        }

        /// <summary>
        /// Создание социальных достижений
        /// </summary>
        private void CreateSocialAchievements()
        {
            allAchievements.Add(new Achievement(
                "people_person", "Душа компании", "Взаимодействуйте с 100 NPC",
                AchievementType.Social, AchievementCategory.Progress, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 5) }
            ));

            allAchievements.Add(new Achievement(
                "businessman_friend", "Друг бизнесменов", "Взаимодействуйте с 10 бизнесменами",
                AchievementType.Social, AchievementCategory.Challenge, false,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 8) }
            ));
        }

        /// <summary>
        /// Создание достижений образования
        /// </summary>
        private void CreateEducationAchievements()
        {
            allAchievements.Add(new Achievement(
                "student", "Студент", "Завершите первый курс",
                AchievementType.Education, AchievementCategory.Progress, false,
                new AchievementReward[] { new AchievementReward(RewardType.Experience, 100) }
            ));

            allAchievements.Add(new Achievement(
                "graduate", "Выпускник", "Получите университетский диплом",
                AchievementType.Education, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.SkillPoints, 15),
                    new AchievementReward(RewardType.Money, 5000)
                }
            ));

            allAchievements.Add(new Achievement(
                "mba_holder", "Обладатель MBA", "Получите диплом MBA",
                AchievementType.Education, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.SkillPoints, 25),
                    new AchievementReward(RewardType.Money, 20000)
                }
            ));
        }

        /// <summary>
        /// Создание специальных достижений
        /// </summary>
        private void CreateSpecialAchievements()
        {
            allAchievements.Add(new Achievement(
                "rags_to_riches", "Из грязи в князи", "Достигните миллиона, начав с 0",
                AchievementType.Special, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.Money, 500000),
                    new AchievementReward(RewardType.SkillPoints, 50)
                }
            ));

            allAchievements.Add(new Achievement(
                "speed_runner", "Спидранер", "Достигните 25 уровня за 2 часа игрового времени",
                AchievementType.Special, AchievementCategory.Challenge, true,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.SkillPoints, 30),
                    new AchievementReward(RewardType.Money, 10000)
                }
            ));

            allAchievements.Add(new Achievement(
                "prestige_master", "Мастер престижа", "Выполните престиж 5 раз",
                AchievementType.Special, AchievementCategory.Milestone, false,
                new AchievementReward[] { 
                    new AchievementReward(RewardType.SkillPoints, 100),
                    new AchievementReward(RewardType.Money, 1000000)
                }
            ));

            allAchievements.Add(new Achievement(
                "easter_egg", "Пасхальное яйцо", "Найдите скрытую секретную функцию",
                AchievementType.Special, AchievementCategory.Secret, true,
                new AchievementReward[] { new AchievementReward(RewardType.SkillPoints, 10) }
            ));
        }

        /// <summary>
        /// Инициализация прогресса достижений
        /// </summary>
        private void InitializeProgress()
        {
            foreach (var achievement in allAchievements)
            {
                achievementProgress[achievement.id] = new AchievementProgress
                {
                    achievementId = achievement.id,
                    currentProgress = 0f,
                    isUnlocked = false,
                    unlockTime = DateTime.MinValue
                };
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnSkillUpgraded += OnSkillUpgraded;
            GameEvents.OnJobCompleted += OnJobCompleted;
            GameEvents.OnEducationCompleted += OnEducationCompleted;
            GameEvents.OnItemPurchased += OnItemPurchased;
            GameEvents.OnNPCInteraction += OnNPCInteraction;
            GameEvents.OnPrestige += OnPrestige;
            GameEvents.OnMoneyEarned += OnMoneyEarned;
            GameEvents.OnPlayerDeath += OnPlayerDeath;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnSkillUpgraded -= OnSkillUpgraded;
            GameEvents.OnJobCompleted -= OnJobCompleted;
            GameEvents.OnEducationCompleted -= OnEducationCompleted;
            GameEvents.OnItemPurchased -= OnItemPurchased;
            GameEvents.OnNPCInteraction -= OnNPCInteraction;
            GameEvents.OnPrestige -= OnPrestige;
            GameEvents.OnMoneyEarned -= OnMoneyEarned;
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить все достижения
        /// </summary>
        public List<Achievement> GetAllAchievements()
        {
            if (showSecretAchievements)
            {
                return allAchievements;
            }
            else
            {
                return allAchievements.Where(a => !a.isSecret || IsAchievementUnlocked(a.id)).ToList();
            }
        }

        /// <summary>
        /// Получить разблокированные достижения
        /// </summary>
        public List<Achievement> GetUnlockedAchievements()
        {
            return new List<Achievement>(unlockedAchievements);
        }

        /// <summary>
        /// Получить достижения по категории
        /// </summary>
        public List<Achievement> GetAchievementsByCategory(AchievementCategory category)
        {
            return GetAllAchievements().Where(a => a.category == category).ToList();
        }

        /// <summary>
        /// Получить достижения по типу
        /// </summary>
        public List<Achievement> GetAchievementsByType(AchievementType type)
        {
            return GetAllAchievements().Where(a => a.type == type).ToList();
        }

        /// <summary>
        /// Проверить, разблокировано ли достижение
        /// </summary>
        public bool IsAchievementUnlocked(string achievementId)
        {
            return achievementProgress.ContainsKey(achievementId) && 
                   achievementProgress[achievementId].isUnlocked;
        }

        /// <summary>
        /// Получить прогресс достижения (0-1)
        /// </summary>
        public float GetAchievementProgress(string achievementId)
        {
            return achievementProgress.ContainsKey(achievementId) ? 
                   achievementProgress[achievementId].currentProgress : 0f;
        }

        /// <summary>
        /// Разблокировать достижение вручную
        /// </summary>
        public bool UnlockAchievement(string achievementId)
        {
            var achievement = allAchievements.FirstOrDefault(a => a.id == achievementId);
            if (achievement != null && !IsAchievementUnlocked(achievementId))
            {
                ProcessAchievementUnlock(achievement);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Получить процент завершенности всех достижений
        /// </summary>
        public float GetOverallProgress()
        {
            var visibleAchievements = GetAllAchievements();
            if (visibleAchievements.Count == 0) return 1f;

            float totalProgress = 0f;
            foreach (var achievement in visibleAchievements)
            {
                totalProgress += GetAchievementProgress(achievement.id);
            }

            return totalProgress / visibleAchievements.Count;
        }

        /// <summary>
        /// Получить количество разблокированных достижений
        /// </summary>
        public int GetUnlockedCount()
        {
            return unlockedAchievements.Count;
        }

        /// <summary>
        /// Получить общее количество достижений
        /// </summary>
        public int GetTotalCount()
        {
            return GetAllAchievements().Count;
        }

        #endregion

        #region Achievement Processing

        /// <summary>
        /// Обработка разблокировки достижения
        /// </summary>
        private void ProcessAchievementUnlock(Achievement achievement)
        {
            if (IsAchievementUnlocked(achievement.id)) return;

            // Обновление прогресса
            achievementProgress[achievement.id].isUnlocked = true;
            achievementProgress[achievement.id].currentProgress = 1f;
            achievementProgress[achievement.id].unlockTime = DateTime.Now;

            // Добавление в список разблокированных
            unlockedAchievements.Add(achievement);

            // Выдача наград
            GiveAchievementRewards(achievement);

            // Эффекты
            PlayAchievementSound(achievement);
            ShowAchievementNotification(achievement);

            // События
            OnAchievementUnlocked?.Invoke(achievement);
            OnAchievementsUpdated?.Invoke(GetAllAchievements());

            Debug.Log($"Достижение разблокировано: {achievement.title}");
        }

        /// <summary>
        /// Обновление прогресса достижения
        /// </summary>
        private void UpdateAchievementProgress(string achievementId, float progress)
        {
            if (!achievementProgress.ContainsKey(achievementId)) return;
            if (IsAchievementUnlocked(achievementId)) return;

            var oldProgress = achievementProgress[achievementId].currentProgress;
            achievementProgress[achievementId].currentProgress = Mathf.Clamp01(progress);

            var achievement = allAchievements.FirstOrDefault(a => a.id == achievementId);
            if (achievement != null)
            {
                OnAchievementProgress?.Invoke(achievement, progress);

                // Проверка разблокировки
                if (progress >= 1f)
                {
                    ProcessAchievementUnlock(achievement);
                }
            }
        }

        /// <summary>
        /// Выдача наград за достижение
        /// </summary>
        private void GiveAchievementRewards(Achievement achievement)
        {
            if (achievement.rewards == null) return;

            foreach (var reward in achievement.rewards)
            {
                switch (reward.type)
                {
                    case RewardType.Money:
                        if (moneySystem != null)
                        {
                            moneySystem.AddMoney(reward.amount, $"Achievement: {achievement.title}");
                        }
                        break;

                    case RewardType.Experience:
                        if (levelSystem != null)
                        {
                            levelSystem.AddExperience((int)reward.amount, "Achievement Reward");
                        }
                        break;

                    case RewardType.SkillPoints:
                        if (skillSystem != null)
                        {
                            skillSystem.AddSkillPoints((int)reward.amount);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Воспроизведение звука достижения
        /// </summary>
        private void PlayAchievementSound(Achievement achievement)
        {
            AudioClip soundToPlay = null;

            if (achievement.isSecret && secretAchievementSound != null)
                soundToPlay = secretAchievementSound;
            else if (achievement.category == AchievementCategory.Milestone && milestoneSound != null)
                soundToPlay = milestoneSound;
            else if (achievementUnlockedSound != null)
                soundToPlay = achievementUnlockedSound;

            if (soundToPlay != null && audioSource != null)
            {
                audioSource.PlayOneShot(soundToPlay);
            }
        }

        /// <summary>
        /// Показ уведомления о достижении
        /// </summary>
        private void ShowAchievementNotification(Achievement achievement)
        {
            string message = $"🏆 {achievement.title}";
            NotificationType notificationType = achievement.isSecret ? 
                NotificationType.Achievement : NotificationType.Success;

            GameEvents.TriggerNotification(message, notificationType, NotificationPriority.High);
        }

        /// <summary>
        /// Получить отображаемое имя навыка
        /// </summary>
        private string GetSkillDisplayName(SkillType skillType)
        {
            if (skillSystem != null)
            {
                return skillSystem.GetSkillName(skillType);
            }

            switch (skillType)
            {
                case SkillType.Charisma: return "Харизма";
                case SkillType.Education: return "Образование";
                case SkillType.Fitness: return "Физическая форма";
                case SkillType.Luck: return "Удача";
                case SkillType.Business: return "Деловые навыки";
                default: return skillType.ToString();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            // Проверка достижений уровня
            CheckLevelAchievements(data.newLevel);
        }

        /// <summary>
        /// Обработчик улучшения навыков
        /// </summary>
        private void OnSkillUpgraded(SkillUpgradeEventData data)
        {
            // Проверка достижений навыков
            CheckSkillAchievements();
        }

        /// <summary>
        /// Обработчик завершения работы
        /// </summary>
        private void OnJobCompleted(JobEventData data)
        {
            // Проверка достижений работы
            CheckJobAchievements(data.jobType);
        }

        /// <summary>
        /// Обработчик завершения образования
        /// </summary>
        private void OnEducationCompleted(EducationEventData data)
        {
            // Проверка достижений образования
            CheckEducationAchievements(data.educationType);
        }

        /// <summary>
        /// Обработчик покупки товара
        /// </summary>
        private void OnItemPurchased(ShopEventData data)
        {
            // Проверка достижений покупок
            CheckPurchaseAchievements(data);
        }

        /// <summary>
        /// Обработчик взаимодействия с NPC
        /// </summary>
        private void OnNPCInteraction(NPCInteractionEventData data)
        {
            // Проверка социальных достижений
            CheckSocialAchievements(data.npcType);
        }

        /// <summary>
        /// Обработчик престижа
        /// </summary>
        private void OnPrestige(int prestigeLevel)
        {
            // Проверка достижений престижа
            CheckPrestigeAchievements(prestigeLevel);
        }

        /// <summary>
        /// Обработчик заработка денег
        /// </summary>
        private void OnMoneyEarned(MoneyEventData data)
        {
            // Проверка денежных достижений
            CheckMoneyAchievements();
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            // Сброс некоторых достижений выживания
            // (например, счетчики времени без смерти)
        }

        #endregion

        #region Achievement Checks

        /// <summary>
        /// Проверка достижений уровня
        /// </summary>
        private void CheckLevelAchievements(int level)
        {
            if (level >= 5) UpdateAchievementProgress("first_steps", 1f);
            if (level >= 15) UpdateAchievementProgress("getting_serious", 1f);
            if (level >= 30) UpdateAchievementProgress("expert", 1f);
            if (level >= 50) UpdateAchievementProgress("master", 1f);
        }

        /// <summary>
        /// Проверка достижений навыков
        /// </summary>
        private void CheckSkillAchievements()
        {
            if (skillSystem == null) return;

            // Проверка мастерства отдельных навыков
            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                int skillLevel = skillSystem.GetSkillLevel(skillType);
                if (skillLevel >= 50)
                {
                    UpdateAchievementProgress($"skill_{skillType.ToString().ToLower()}_master", 1f);
                }
            }

            // Проверка мастера на все руки
            bool allSkillsAt25 = true;
            foreach (SkillType skillType in Enum.GetValues(typeof(SkillType)))
            {
                if (skillSystem.GetSkillLevel(skillType) < 25)
                {
                    allSkillsAt25 = false;
                    break;
                }
            }

            if (allSkillsAt25)
            {
                UpdateAchievementProgress("jack_of_all_trades", 1f);
            }
        }

        /// <summary>
        /// Проверка достижений работы
        /// </summary>
        private void CheckJobAchievements(JobType jobType)
        {
            // Первая работа
            UpdateAchievementProgress("first_job", 1f);

            // CEO достижение
            if (jobType == JobType.CEO)
            {
                UpdateAchievementProgress("ceo_achieved", 1f);
            }

            // Трудяга (нужно отслеживать количество)
            // Это требует дополнительного счетчика
        }

        /// <summary>
        /// Проверка достижений образования
        /// </summary>
        private void CheckEducationAchievements(EducationType educationType)
        {
            // Первый курс
            UpdateAchievementProgress("student", 1f);

            // Университетское образование
            if (educationType == EducationType.University)
            {
                UpdateAchievementProgress("graduate", 1f);
            }

            // MBA
            if (educationType == EducationType.MBA)
            {
                UpdateAchievementProgress("mba_holder", 1f);
            }
        }

        /// <summary>
        /// Проверка достижений покупок
        /// </summary>
        private void CheckPurchaseAchievements(ShopEventData data)
        {
            // Первая покупка
            UpdateAchievementProgress("first_purchase", 1f);

            // Покупка люкса
            if (data.quality == ItemQuality.Luxury)
            {
                UpdateAchievementProgress("luxury_buyer", 1f);
            }
        }

        /// <summary>
        /// Проверка социальных достижений
        /// </summary>
        private void CheckSocialAchievements(NPCType npcType)
        {
            // Эти достижения требуют счетчиков
            // Пока что просто заглушки
        }

        /// <summary>
        /// Проверка денежных достижений
        /// </summary>
        private void CheckMoneyAchievements()
        {
            if (moneySystem == null) return;

            double currentMoney = moneySystem.GetMoney();

            if (currentMoney >= 100) UpdateAchievementProgress("first_hundred", 1f);
            if (currentMoney >= 1000) UpdateAchievementProgress("thousand_club", 1f);
            if (currentMoney >= 1000000) UpdateAchievementProgress("first_millionaire", 1f);
        }

        /// <summary>
        /// Проверка достижений престижа
        /// </summary>
        private void CheckPrestigeAchievements(int prestigeLevel)
        {
            if (prestigeLevel >= 5)
            {
                UpdateAchievementProgress("prestige_master", 1f);
            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public AchievementSystemSaveData GetSaveData()
        {
            var progressData = new List<AchievementProgressData>();
            foreach (var progress in achievementProgress.Values)
            {
                progressData.Add(new AchievementProgressData
                {
                    achievementId = progress.achievementId,
                    currentProgress = progress.currentProgress,
                    isUnlocked = progress.isUnlocked,
                    unlockTime = progress.unlockTime.ToBinary()
                });
            }

            return new AchievementSystemSaveData
            {
                achievementProgress = progressData
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(AchievementSystemSaveData data)
        {
            if (data?.achievementProgress != null)
            {
                foreach (var progressData in data.achievementProgress)
                {
                    if (achievementProgress.ContainsKey(progressData.achievementId))
                    {
                        achievementProgress[progressData.achievementId].currentProgress = progressData.currentProgress;
                        achievementProgress[progressData.achievementId].isUnlocked = progressData.isUnlocked;
                        achievementProgress[progressData.achievementId].unlockTime = 
                            DateTime.FromBinary(progressData.unlockTime);

                        // Восстановление разблокированных достижений
                        if (progressData.isUnlocked)
                        {
                            var achievement = allAchievements.FirstOrDefault(a => a.id == progressData.achievementId);
                            if (achievement != null && !unlockedAchievements.Contains(achievement))
                            {
                                unlockedAchievements.Add(achievement);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Показать все достижения")]
        private void DebugShowAllAchievements()
        {
            var achievements = GetAllAchievements();
            Debug.Log($"Всего достижений: {achievements.Count}");
            foreach (var achievement in achievements)
            {
                bool unlocked = IsAchievementUnlocked(achievement.id);
                float progress = GetAchievementProgress(achievement.id);
                Debug.Log($"{achievement.title} - {(unlocked ? "Разблокировано" : $"Прогресс: {progress:P1}")}");
            }
        }

        [ContextMenu("Разблокировать случайное достижение")]
        private void DebugUnlockRandomAchievement()
        {
            var lockedAchievements = allAchievements.Where(a => !IsAchievementUnlocked(a.id)).ToList();
            if (lockedAchievements.Count > 0)
            {
                var randomAchievement = lockedAchievements[UnityEngine.Random.Range(0, lockedAchievements.Count)];
                UnlockAchievement(randomAchievement.id);
            }
        }

        [ContextMenu("Показать статистику достижений")]
        private void DebugShowAchievementStats()
        {
            Debug.Log($"Разблокировано достижений: {GetUnlockedCount()}/{GetTotalCount()}");
            Debug.Log($"Общий прогресс: {GetOverallProgress():P1}");
            
            foreach (AchievementCategory category in Enum.GetValues(typeof(AchievementCategory)))
            {
                var categoryAchievements = GetAchievementsByCategory(category);
                int unlockedInCategory = categoryAchievements.Count(a => IsAchievementUnlocked(a.id));
                Debug.Log($"{category}: {unlockedInCategory}/{categoryAchievements.Count}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Достижение
    /// </summary>
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string title;
        public string description;
        public AchievementType type;
        public AchievementCategory category;
        public bool isSecret;
        public AchievementReward[] rewards;

        public Achievement(string id, string title, string description, AchievementType type, 
                          AchievementCategory category, bool isSecret, AchievementReward[] rewards)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.type = type;
            this.category = category;
            this.isSecret = isSecret;
            this.rewards = rewards ?? new AchievementReward[0];
        }
    }

    /// <summary>
    /// Награда за достижение
    /// </summary>
    [System.Serializable]
    public class AchievementReward
    {
        public RewardType type;
        public double amount;

        public AchievementReward(RewardType type, double amount)
        {
            this.type = type;
            this.amount = amount;
        }
    }

    /// <summary>
    /// Прогресс достижения
    /// </summary>
    [System.Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public float currentProgress; // 0-1
        public bool isUnlocked;
        public DateTime unlockTime;
    }

    /// <summary>
    /// Данные системы достижений для сохранения
    /// </summary>
    [System.Serializable]
    public class AchievementSystemSaveData
    {
        public List<AchievementProgressData> achievementProgress = new List<AchievementProgressData>();

        public bool IsValid() => achievementProgress != null;
        public void ApplyDefaults()
        {
            achievementProgress = new List<AchievementProgressData>();
        }
    }

    /// <summary>
    /// Данные прогресса достижения для сохранения
    /// </summary>
    [System.Serializable]
    public class AchievementProgressData
    {
        public string achievementId;
        public float currentProgress;
        public bool isUnlocked;
        public long unlockTime;
    }
}
