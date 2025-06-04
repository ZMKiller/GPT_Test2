using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Версия сохранения для совместимости
    /// </summary>
    public static class SaveVersion
    {
        public const int CURRENT_VERSION = 1;
        public const int MIN_SUPPORTED_VERSION = 1;
    }

    /// <summary>
    /// Базовый класс для всех данных сохранения
    /// </summary>
    [System.Serializable]
    public abstract class BaseSaveData
    {
        public int version = SaveVersion.CURRENT_VERSION;
        public DateTime lastSaved = DateTime.Now;

        /// <summary>
        /// Проверить валидность данных
        /// </summary>
        public abstract bool IsValid();

        /// <summary>
        /// Применить данные по умолчанию
        /// </summary>
        public abstract void ApplyDefaults();
    }

    /// <summary>
    /// Данные характеристик игрока для сохранения
    /// </summary>
    [System.Serializable]
    public class PlayerSaveData : BaseSaveData
    {
        [Header("Основные характеристики")]
        public float health = 100f;
        public float maxHealth = 100f;
        public float hunger = 0f;
        public float maxHunger = 100f;
        public float mood = 100f;
        public float maxMood = 100f;
        public float money = 0f;

        [Header("Прогрессия")]
        public int level = 1;
        public float experience = 0f;
        public float experienceToNextLevel = 100f;

        [Header("Статистика")]
        public float totalMoneyEarned = 0f;
        public float totalMoneySpent = 0f;
        public int totalNPCInteractions = 0;
        public float totalPlayTime = 0f;

        [Header("Настройки деградации")]
        public bool degradationEnabled = true;
        public float degradationMultiplier = 1f;

        public override bool IsValid()
        {
            return health >= 0 && health <= maxHealth * 1.1f && // Небольшая погрешность
                   hunger >= 0 && hunger <= maxHunger * 1.1f &&
                   mood >= 0 && mood <= maxMood * 1.1f &&
                   money >= 0 && money <= 999999999f && // Разумный лимит денег
                   level >= 1 && level <= 1000 && // Разумный лимит уровня
                   experience >= 0 &&
                   totalPlayTime >= 0;
        }

        public override void ApplyDefaults()
        {
            health = 100f;
            maxHealth = 100f;
            hunger = 0f;
            maxHunger = 100f;
            mood = 100f;
            maxMood = 100f;
            money = 0f;
            level = 1;
            experience = 0f;
            experienceToNextLevel = 100f;
            totalMoneyEarned = 0f;
            totalMoneySpent = 0f;
            totalNPCInteractions = 0;
            totalPlayTime = 0f;
            degradationEnabled = true;
            degradationMultiplier = 1f;
        }
    }

    /// <summary>
    /// Данные локаций для сохранения
    /// </summary>
    [System.Serializable]
    public class LocationSaveData : BaseSaveData
    {
        [Header("Состояние локаций")]
        public string currentLocation = "Street";
        public List<string> unlockedLocations = new List<string> { "Street" };
        public Dictionary<string, DateTime> locationUnlockTimes = new Dictionary<string, DateTime>();

        [Header("Статистика локаций")]
        public Dictionary<string, float> timeSpentInLocations = new Dictionary<string, float>();
        public Dictionary<string, int> visitCounts = new Dictionary<string, int>();
        public Dictionary<string, float> moneyEarnedInLocations = new Dictionary<string, float>();

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(currentLocation) &&
                   unlockedLocations != null &&
                   unlockedLocations.Count > 0 &&
                   unlockedLocations.Contains("Street"); // Улица всегда должна быть разблокирована
        }

        public override void ApplyDefaults()
        {
            currentLocation = "Street";
            unlockedLocations = new List<string> { "Street" };
            locationUnlockTimes = new Dictionary<string, DateTime>();
            timeSpentInLocations = new Dictionary<string, float>();
            visitCounts = new Dictionary<string, int>();
            moneyEarnedInLocations = new Dictionary<string, float>();
        }
    }

    /// <summary>
    /// Настройки игры для сохранения
    /// </summary>
    [System.Serializable]
    public class SettingsSaveData : BaseSaveData
    {
        [Header("Звуковые настройки")]
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public bool isMuted = false;

        [Header("Графические настройки")]
        public int qualityLevel = 2;
        public int resolutionIndex = 0;
        public bool isFullscreen = true;
        public int fpsLimit = 60;

        [Header("Игровые настройки")]
        public bool autoSaveEnabled = true;
        public float autoSaveInterval = 60f;
        public bool showNotifications = true;
        public bool warningAnimations = true;
        public bool showTutorial = true;

        [Header("UI настройки")]
        public string language = "Russian";
        public float uiScale = 1f;
        public bool showFPS = false;
        public bool compactUI = false;

        public override bool IsValid()
        {
            return masterVolume >= 0 && masterVolume <= 1f &&
                   musicVolume >= 0 && musicVolume <= 1f &&
                   sfxVolume >= 0 && sfxVolume <= 1f &&
                   qualityLevel >= 0 && qualityLevel <= 5 &&
                   fpsLimit >= 30 && fpsLimit <= 300 &&
                   autoSaveInterval >= 10f && autoSaveInterval <= 3600f &&
                   uiScale >= 0.5f && uiScale <= 2f;
        }

        public override void ApplyDefaults()
        {
            masterVolume = 1f;
            musicVolume = 0.8f;
            sfxVolume = 1f;
            isMuted = false;
            qualityLevel = 2;
            resolutionIndex = 0;
            isFullscreen = true;
            fpsLimit = 60;
            autoSaveEnabled = true;
            autoSaveInterval = 60f;
            showNotifications = true;
            warningAnimations = true;
            showTutorial = true;
            language = "Russian";
            uiScale = 1f;
            showFPS = false;
            compactUI = false;
        }
    }

    /// <summary>
    /// Статистика игрока для сохранения
    /// </summary>
    [System.Serializable]
    public class StatisticsSaveData : BaseSaveData
    {
        [Header("Общая статистика")]
        public float totalPlayTime = 0f;
        public int totalGameSessions = 0;
        // number of times the player launched the game
        public int timesPlayed = 0;
        public DateTime firstPlayDate = DateTime.Now;
        public DateTime lastPlayDate = DateTime.Now;

        [Header("Игровая статистика")]
        public float totalMoneyEarned = 0f;
        public float totalMoneySpent = 0f;
        public float highestMoneyAmount = 0f;
        public int totalLevelUps = 0;
        public int totalDeaths = 0;

        [Header("NPC статистика")]
        public int totalNPCInteractions = 0;
        public Dictionary<string, int> npcInteractionCounts = new Dictionary<string, int>();
        public Dictionary<string, float> moneyFromNPCTypes = new Dictionary<string, float>();

        [Header("Локации статистика")]
        public Dictionary<string, float> timeInLocations = new Dictionary<string, float>();
        public Dictionary<string, int> locationVisits = new Dictionary<string, int>();
        public int totalLocationChanges = 0;

        [Header("Достижения")]
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<string, DateTime> achievementUnlockDates = new Dictionary<string, DateTime>();

        public override bool IsValid()
        {
            return totalPlayTime >= 0 &&
                   totalGameSessions >= 0 &&
                   totalMoneyEarned >= 0 &&
                   totalMoneySpent >= 0 &&
                   totalLevelUps >= 0 &&
                   totalDeaths >= 0 &&
                   totalNPCInteractions >= 0;
        }

        public override void ApplyDefaults()
        {
            totalPlayTime = 0f;
            totalGameSessions = 0;
            firstPlayDate = DateTime.Now;
            lastPlayDate = DateTime.Now;
            totalMoneyEarned = 0f;
            totalMoneySpent = 0f;
            highestMoneyAmount = 0f;
            totalLevelUps = 0;
            totalDeaths = 0;
            totalNPCInteractions = 0;
            npcInteractionCounts = new Dictionary<string, int>();
            moneyFromNPCTypes = new Dictionary<string, float>();
            timeInLocations = new Dictionary<string, float>();
            locationVisits = new Dictionary<string, int>();
            totalLocationChanges = 0;
            unlockedAchievements = new List<string>();
            achievementUnlockDates = new Dictionary<string, DateTime>();
        }
    }

    /// <summary>
    /// Общее состояние игры для сохранения
    /// </summary>
    [System.Serializable]
    public class GameStateSaveData : BaseSaveData
    {
        [Header("Версия и метаданные")]
        public string version = "1.0.0";
        public long saveDate = 0;
        public string checksum = "";
        public string saveName = "";
        public DateTime createdDate = DateTime.Now;
        public string gameVersion = "";

        [Header("Состояние игры")]
        public float gameTime = 0f;
        public float difficultyMultiplier = 1f;
        public TimeOfDay timeOfDay = TimeOfDay.Day;
        // store current time period as string for backwards compatibility
        public string currentTimeOfDay = TimeOfDay.Day.ToString();
        public string currentScene = "GameScene";
        public float gameSpeed = 1f;

        [Header("Прогресс игры")]
        public bool isFirstLaunch = true;
        public bool tutorialCompleted = false;
        public int saveCount = 1;

        [Header("Дополнительные данные")]
        public Dictionary<string, string> customData = new Dictionary<string, string>();

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(version) &&
                   saveDate > 0 &&
                   gameTime >= 0 &&
                   difficultyMultiplier > 0 && difficultyMultiplier <= 10f &&
                   gameSpeed > 0 && gameSpeed <= 10f &&
                   !string.IsNullOrEmpty(currentScene);
        }

        public override void ApplyDefaults()
        {
            version = SaveValidation.GetCurrentSaveVersion();
            saveDate = DateTime.Now.ToBinary();
            checksum = "";
            saveName = string.Empty;
            createdDate = DateTime.Now;
            gameVersion = version;
            gameTime = 0f;
            difficultyMultiplier = 1f;
            timeOfDay = TimeOfDay.Day;
            currentScene = "GameScene";
            gameSpeed = 1f;
            isFirstLaunch = true;
            tutorialCompleted = false;
            saveCount = 1;
            customData = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Полные данные сохранения игры
    /// </summary>
    [System.Serializable]
    public class CompleteSaveData
    {
        [Header("Основные данные")]
        public GameStateSaveData gameState = new GameStateSaveData();
        public PlayerSaveData playerData = new PlayerSaveData();
        public LocationSaveData locationData = new LocationSaveData();
        public SettingsSaveData settingsData = new SettingsSaveData();
        public StatisticsSaveData statisticsData = new StatisticsSaveData();
        public ProgressionSaveData progressionData = new ProgressionSaveData();
        public TimeSaveData timeData = new TimeSaveData();

        [Header("Метаданные")]
        public string checksum = "";
        public bool isCorrupted = false;
        public DateTime lastModified = DateTime.Now;

        /// <summary>
        /// Проверить валидность всех данных
        /// </summary>
        public bool IsValid()
        {
            return gameState.IsValid() &&
                   playerData.IsValid() &&
                   locationData.IsValid() &&
                   settingsData.IsValid() &&
                   statisticsData.IsValid() &&
                   (progressionData?.IsValid() ?? true) &&
                   (timeData?.IsValid() ?? true);
        }

        /// <summary>
        /// Применить значения по умолчанию ко всем данным
        /// </summary>
        public void ApplyDefaults()
        {
            gameState.ApplyDefaults();
            playerData.ApplyDefaults();
            locationData.ApplyDefaults();
            settingsData.ApplyDefaults();
            statisticsData.ApplyDefaults();
            
            if (progressionData == null)
                progressionData = new ProgressionSaveData();
            progressionData.ApplyDefaults();
            
            if (timeData == null)
                timeData = new TimeSaveData();
            timeData.ApplyDefaults();
            
            isCorrupted = false;
            lastModified = DateTime.Now;
            checksum = "";
        }

        /// <summary>
        /// Получить информацию о сохранении для UI
        /// </summary>
        public SaveSlotInfo GetSlotInfo()
        {
            return new SaveSlotInfo
            {
                saveName = gameState.saveName,
                level = playerData.level,
                money = playerData.money,
                currentLocation = locationData.currentLocation,
                playTime = statisticsData.totalPlayTime,
                lastPlayed = statisticsData.lastPlayDate,
                createdDate = gameState.createdDate,
                gameVersion = gameState.gameVersion,
                isCorrupted = isCorrupted
            };
        }
    }

    /// <summary>
    /// Информация о слоте сохранения для отображения в UI
    /// </summary>
    [System.Serializable]
    public class SaveSlotInfo
    {
        public int slotIndex;
        public string saveName;
        public int level;
        public float money;
        public string currentLocation;
        public float playTime;
        public DateTime lastPlayed;
        public DateTime createdDate;
        public string gameVersion;
        public bool isCorrupted;
        public bool isEmpty;

        /// <summary>
        /// Получить форматированную строку времени игры
        /// </summary>
        public string GetFormattedPlayTime()
        {
            TimeSpan time = TimeSpan.FromSeconds(playTime);
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}ч {time.Minutes}м";
            else
                return $"{time.Minutes}м {time.Seconds}с";
        }

        /// <summary>
        /// Получить форматированную дату последней игры
        /// </summary>
        public string GetFormattedLastPlayed()
        {
            TimeSpan timeSince = DateTime.Now - lastPlayed;
            
            if (timeSince.TotalDays >= 1)
                return $"{(int)timeSince.TotalDays} дн. назад";
            else if (timeSince.TotalHours >= 1)
                return $"{(int)timeSince.TotalHours} ч. назад";
            else if (timeSince.TotalMinutes >= 1)
                return $"{(int)timeSince.TotalMinutes} мин. назад";
            else
                return "Только что";
        }

        /// <summary>
        /// Получить локализованное название локации
        /// </summary>
        public string GetLocalizedLocationName()
        {
            switch (currentLocation)
            {
                case "Street": return "Улица";
                case "Park": return "Парк";
                case "BusinessDistrict": return "Бизнес-район";
                case "ShoppingMall": return "Торговый центр";
                case "Slums": return "Трущобы";
                case "TrainStation": return "Вокзал";
                case "NightClub": return "Ночной клуб";
                default: return currentLocation;
            }
        }
    }

    /// <summary>
    /// Исключения системы сохранений
    /// </summary>
    public class SaveSystemException : System.Exception
    {
        public SaveSystemException(string message) : base(message) { }
        public SaveSystemException(string message, System.Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Результат операции сохранения/загрузки
    /// </summary>
    public enum SaveResult
    {
        Success,
        FileNotFound,
        CorruptedData,
        InvalidVersion,
        PermissionDenied,
        UnknownError
    }

    /// <summary>
    /// Информация о результате операции
    /// </summary>
    [System.Serializable]
    public class SaveOperationResult
    {
        public SaveResult result;
        public string message;
        public string filePath;
        public System.Exception exception;

        public bool IsSuccess => result == SaveResult.Success;

        public SaveOperationResult(SaveResult result, string message = "", string filePath = "", System.Exception exception = null)
        {
            this.result = result;
            this.message = message;
            this.filePath = filePath;
            this.exception = exception;
        }
    }

    /// <summary>
    /// Данные системы прогрессии для сохранения
    /// </summary>
    [System.Serializable]
    public class ProgressionSaveData : BaseSaveData
    {
        [Header("Данные систем прогрессии")]
        public SkillSystemSaveData skillSystemData;
        public ShopSystemSaveData shopSystemData;
        public JobSystemSaveData jobSystemData;
        public EducationSystemSaveData educationSystemData;
        public AchievementSystemSaveData achievementSystemData;
        public LevelSystemSaveData levelSystemData;

        public override bool IsValid()
        {
            return version >= SaveVersion.MIN_SUPPORTED_VERSION &&
                   (skillSystemData?.IsValid() ?? true) &&
                   (shopSystemData?.IsValid() ?? true) &&
                   (jobSystemData?.IsValid() ?? true) &&
                   (educationSystemData?.IsValid() ?? true) &&
                   (achievementSystemData?.IsValid() ?? true) &&
                   (levelSystemData?.IsValid() ?? true);
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            
            skillSystemData = new SkillSystemSaveData();
            shopSystemData = new ShopSystemSaveData();
            jobSystemData = new JobSystemSaveData();
            educationSystemData = new EducationSystemSaveData();
            achievementSystemData = new AchievementSystemSaveData();
            levelSystemData = new LevelSystemSaveData();
        }
    }

    /// <summary>
    /// Данные системы навыков для сохранения
    /// </summary>
    [System.Serializable]
    public class SkillSystemSaveData : BaseSaveData
    {
        public Dictionary<SkillType, int> skillLevels = new Dictionary<SkillType, int>();
        // Legacy field for older saves
        public Dictionary<string, int> skills = new Dictionary<string, int>();
        public int availableSkillPoints = 0;

        public override bool IsValid()
        {
            return skillLevels != null && availableSkillPoints >= 0;
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            skillLevels = new Dictionary<SkillType, int>();
            availableSkillPoints = 0;
        }
    }

    /// <summary>
    /// Данные системы магазина для сохранения
    /// </summary>
    [System.Serializable]
    public class ShopSystemSaveData : BaseSaveData
    {
        public List<PurchasedItemData> purchasedItems = new List<PurchasedItemData>();

        public override bool IsValid()
        {
            return purchasedItems != null;
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            purchasedItems = new List<PurchasedItemData>();
        }
    }

    /// <summary>
    /// Данные системы работы для сохранения
    /// </summary>
    [System.Serializable]
    public class JobSystemSaveData : BaseSaveData
    {
        // Legacy fields for compatibility
        public string currentJobTitle = string.Empty;
        public bool isWorking = false;
        public float workTimeRemaining = 0f;
        public int jobsCompletedToday = 0;
        public long lastWorkDay = 0;
        public List<CompletedJobData> completedJobs = new List<CompletedJobData>();

        // Newer fields
        public JobType? currentJobType;
        public float currentJobProgress = 0f;
        public DateTime currentJobStartTime;
        public DateTime lastJobDate;
        public Dictionary<JobType, int> jobCompletionCounts = new Dictionary<JobType, int>();

        public override bool IsValid()
        {
            return currentJobProgress >= 0f && currentJobProgress <= 1f &&
                   jobsCompletedToday >= 0 &&
                   completedJobs != null &&
                   jobCompletionCounts != null;
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            currentJobTitle = string.Empty;
            isWorking = false;
            workTimeRemaining = 0f;
            jobsCompletedToday = 0;
            lastWorkDay = DateTime.MinValue.ToBinary();
            completedJobs = new List<CompletedJobData>();

            currentJobType = null;
            currentJobProgress = 0f;
            currentJobStartTime = DateTime.MinValue;
            lastJobDate = DateTime.MinValue;
            jobCompletionCounts = new Dictionary<JobType, int>();
        }
    }

    /// <summary>
    /// Данные системы образования для сохранения
    /// </summary>
    [System.Serializable]
    public class EducationSystemSaveData : BaseSaveData
    {
        // Legacy fields for backward compatibility
        public string currentCourseTitle = string.Empty;
        public bool isStudying = false;
        public float studyTimeRemaining = 0f;
        public long lastStudyDay = 0;

        public EducationType? currentCourseType;
        public float currentCourseProgress = 0f;
        public DateTime currentCourseStartTime;
        public int coursesCompletedToday = 0;
        public DateTime lastCourseDate;
        public List<CompletedCourseData> completedCourses = new List<CompletedCourseData>();
        public List<DegreeData> obtainedDegrees = new List<DegreeData>();
        public List<string> earnedCertificates = new List<string>();

        public override bool IsValid()
        {
            return currentCourseProgress >= 0f && currentCourseProgress <= 1f &&
                   coursesCompletedToday >= 0 &&
                   completedCourses != null &&
                   earnedCertificates != null;
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            currentCourseTitle = string.Empty;
            isStudying = false;
            studyTimeRemaining = 0f;
            lastStudyDay = DateTime.MinValue.ToBinary();
            currentCourseType = null;
            currentCourseProgress = 0f;
            currentCourseStartTime = DateTime.MinValue;
            coursesCompletedToday = 0;
            lastCourseDate = DateTime.MinValue;
            completedCourses = new List<CompletedCourseData>();
            obtainedDegrees = new List<DegreeData>();
            earnedCertificates = new List<string>();
        }
    }

    /// <summary>
    /// Данные системы уровней для сохранения
    /// </summary>
    [System.Serializable]
    public class LevelSystemSaveData : BaseSaveData
    {
        public int prestigeLevel = 0;
        // timestamp of last level-up for legacy features
        public long lastLevelUpTime = 0;

        public override bool IsValid()
        {
            return prestigeLevel >= 0;
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            prestigeLevel = 0;
            lastLevelUpTime = DateTime.MinValue.ToBinary();
        }
    }

    /// <summary>
    /// Объединенные данные системы времени для сохранения
    /// </summary>
    [System.Serializable]
    public class TimeSaveData : BaseSaveData
    {
        public TimeOfDayManagerSaveData timeOfDayManager = new TimeOfDayManagerSaveData();
        public WeatherSystemSaveData weatherSystem = new WeatherSystemSaveData();
        public SleepSystemSaveData sleepSystem = new SleepSystemSaveData();
        public TimeBasedGameplaySaveData timeBasedGameplay = new TimeBasedGameplaySaveData();
        public TimeBasedEventsSaveData timeBasedEvents = new TimeBasedEventsSaveData();

        public override bool IsValid()
        {
            return timeOfDayManager != null &&
                   weatherSystem != null &&
                   sleepSystem != null &&
                   timeBasedGameplay != null &&
                   timeBasedEvents != null;
        }

        public override void ApplyDefaults()
        {
            version = SaveVersion.CURRENT_VERSION;
            lastSaved = DateTime.Now;
            
            if (timeOfDayManager == null)
                timeOfDayManager = new TimeOfDayManagerSaveData();
            
            if (weatherSystem == null)
                weatherSystem = new WeatherSystemSaveData();
            
            if (sleepSystem == null)
                sleepSystem = new SleepSystemSaveData();
            
            if (timeBasedGameplay == null)
                timeBasedGameplay = new TimeBasedGameplaySaveData();
            
            if (timeBasedEvents == null)
                timeBasedEvents = new TimeBasedEventsSaveData();
        }
    }

    /// <summary>
    /// Данные TimeOfDayManager для сохранения
    /// </summary>
    [System.Serializable]
    public class TimeOfDayManagerSaveData
    {
        public long currentDateTime; // DateTime.ToBinary()
        public TimePeriod currentTimePeriod = TimePeriod.Day;
        public Season currentSeason = Season.Spring;
        public TimeSpeed currentTimeSpeed = TimeSpeed.Normal;
        public bool enableTimeFlow = true;
        public float realSecondsPerGameHour = 60f;
        public float timeMultiplier = 1f;
        public int currentDay = 1;
        public bool isPaused = false;
    }
}
