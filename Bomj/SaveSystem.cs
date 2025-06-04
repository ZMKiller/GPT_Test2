using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

using Newtonsoft.Json;


namespace HomelessToMillionaire
{
    /// <summary>
    /// Центральная система сохранений игры
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Header("Настройки сохранения")]
        [Tooltip("Папка для сохранений относительно persistentDataPath")]
        public string saveFolder = "Saves";

        [Tooltip("Максимальное количество слотов сохранения")]
        public int maxSaveSlots = 5;

        [Tooltip("Включить шифрование сохранений")]
        public bool enableEncryption = true;

        [Tooltip("Создавать резервные копии")]
        public bool createBackups = true;

        [Tooltip("Максимальное количество резервных копий")]
        public int maxBackups = 3;

        [Header("Валидация")]
        [Tooltip("Проверять контрольные суммы")]
        public bool validateChecksums = true;

        [Tooltip("Автоматически восстанавливать поврежденные данные")]
        public bool autoRepairCorrupted = true;

        // Компоненты
        private PlayerStats playerStats;
        private LocationManager locationManager;
        private GameManager gameManager;
        
        // Компоненты системы прогрессии
        private SkillSystem skillSystem;
        private ShopSystem shopSystem;
        private JobSystem jobSystem;
        private EducationSystem educationSystem;
        private AchievementSystem achievementSystem;
        private LevelSystem levelSystem;
        
        // Компоненты системы времени
        private TimeOfDayManager timeOfDayManager;
        private WeatherSystem weatherSystem;
        private SleepSystem sleepSystem;
        private TimeBasedGameplay timeBasedGameplay;
        private TimeBasedEvents timeBasedEvents;

        // Состояние
        private string saveDirectory;
        private readonly string encryptionKey = "HomelessToMillionaire2024Key"; // В продакшене использовать более безопасный ключ
        private Dictionary<int, CompleteSaveData> cachedSaves = new Dictionary<int, CompleteSaveData>();

        // События
        public event Action<SaveOperationResult> OnSaveCompleted;
        public event Action<SaveOperationResult> OnLoadCompleted;
        public event Action<int, CompleteSaveData> OnDataLoaded;

        // Константы
        private const string SAVE_EXTENSION = ".sav";
        private const string BACKUP_EXTENSION = ".bak";
        private const string SETTINGS_FILE = "settings.cfg";

        // Паттерн Singleton
        private static SaveSystem instance;
        public static SaveSystem Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<SaveSystem>();
                return instance;
            }
        }

        #region Unity Methods

        private void Awake()
        {
            // Singleton паттерн
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSaveSystem();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Получение ссылок на компоненты
            gameManager = GameManager.Instance;
            playerStats = gameManager?.PlayerStats;
            locationManager = FindObjectOfType<LocationManager>();

            // Получение компонентов системы прогрессии
            InitializeProgressionComponents();
            
            // Получение компонентов системы времени
            InitializeTimeComponents();

            // Загрузка настроек при старте
            LoadSettings();
        }

        /// <summary>
        /// Инициализация компонентов системы прогрессии
        /// </summary>
        private void InitializeProgressionComponents()
        {
            skillSystem = FindObjectOfType<SkillSystem>();
            shopSystem = FindObjectOfType<ShopSystem>();
            jobSystem = FindObjectOfType<JobSystem>();
            educationSystem = FindObjectOfType<EducationSystem>();
            achievementSystem = FindObjectOfType<AchievementSystem>();
            levelSystem = FindObjectOfType<LevelSystem>();

            // Можно также попробовать найти их через GameManager или PlayerStats
            if (skillSystem == null && playerStats != null)
                skillSystem = playerStats.GetComponent<SkillSystem>();
            
            if (levelSystem == null && playerStats != null)
                levelSystem = playerStats.GetComponent<LevelSystem>();

            Debug.Log($"SaveSystem: Найдены компоненты прогрессии - " +
                     $"Skills: {skillSystem != null}, Shop: {shopSystem != null}, " +
                     $"Jobs: {jobSystem != null}, Education: {educationSystem != null}, " +
                     $"Achievements: {achievementSystem != null}, Level: {levelSystem != null}");
        }

        /// <summary>
        /// Инициализация компонентов системы времени
        /// </summary>
        private void InitializeTimeComponents()
        {
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            weatherSystem = FindObjectOfType<WeatherSystem>();
            sleepSystem = FindObjectOfType<SleepSystem>();
            timeBasedGameplay = FindObjectOfType<TimeBasedGameplay>();
            timeBasedEvents = FindObjectOfType<TimeBasedEvents>();

            Debug.Log($"SaveSystem: Найдены компоненты времени - " +
                     $"TimeOfDay: {timeOfDayManager != null}, Weather: {weatherSystem != null}, " +
                     $"Sleep: {sleepSystem != null}, TimeGameplay: {timeBasedGameplay != null}, " +
                     $"TimeEvents: {timeBasedEvents != null}");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Сохранить при сворачивании приложения (мобильные платформы)
                SaveCurrentGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Сохранить при потере фокуса
                SaveCurrentGame();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализировать систему сохранений
        /// </summary>
        private void InitializeSaveSystem()
        {
            // Создать папку для сохранений
            saveDirectory = Path.Combine(Application.persistentDataPath, saveFolder);
            
            try
            {
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                    Debug.Log($"Создана папка сохранений: {saveDirectory}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка создания папки сохранений: {e.Message}");
            }

            Debug.Log($"SaveSystem инициализирован. Папка сохранений: {saveDirectory}");
        }

        #endregion

        #region Public Save/Load Methods

        /// <summary>
        /// Сохранить текущую игру в указанный слот
        /// </summary>
        public SaveOperationResult SaveGame(int slotIndex, string saveName = "")
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= maxSaveSlots)
                {
                    return new SaveOperationResult(SaveResult.UnknownError, $"Неверный индекс слота: {slotIndex}");
                }

                // Собрать данные для сохранения
                CompleteSaveData saveData = GatherSaveData(saveName);
                if (saveData == null)
                {
                    return new SaveOperationResult(SaveResult.UnknownError, "Не удалось собрать данные для сохранения");
                }

                // Валидация данных
                if (!ValidateSaveData(saveData))
                {
                    return new SaveOperationResult(SaveResult.CorruptedData, "Данные не прошли валидацию");
                }

                // Вычислить контрольную сумму
                if (validateChecksums)
                {
                    saveData.checksum = CalculateChecksum(saveData);
                }

                // Сохранить в файл
                string filePath = GetSaveFilePath(slotIndex);
                SaveOperationResult result = SaveToFile(saveData, filePath);

                if (result.IsSuccess)
                {
                    // Создать резервную копию
                    if (createBackups)
                    {
                        CreateBackup(filePath, slotIndex);
                    }

                    // Кешировать данные
                    cachedSaves[slotIndex] = saveData;

                    Debug.Log($"Игра сохранена в слот {slotIndex}: {saveName}");
                }

                OnSaveCompleted?.Invoke(result);
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка сохранения игры: {e.Message}");
                var result = new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
                OnSaveCompleted?.Invoke(result);
                return result;
            }
        }

        /// <summary>
        /// Загрузить игру из указанного слота
        /// </summary>
        public SaveOperationResult LoadGame(int slotIndex)
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= maxSaveSlots)
                {
                    return new SaveOperationResult(SaveResult.UnknownError, $"Неверный индекс слота: {slotIndex}");
                }

                string filePath = GetSaveFilePath(slotIndex);
                if (!File.Exists(filePath))
                {
                    return new SaveOperationResult(SaveResult.FileNotFound, $"Файл сохранения не найден: {filePath}");
                }

                // Загрузить из файла
                SaveOperationResult loadResult = LoadFromFile(filePath);
                if (!loadResult.IsSuccess)
                {
                    // Попробовать восстановить из резервной копии
                    if (createBackups)
                    {
                        loadResult = TryRestoreFromBackup(slotIndex);
                    }
                    
                    if (!loadResult.IsSuccess)
                    {
                        OnLoadCompleted?.Invoke(loadResult);
                        return loadResult;
                    }
                }

                // Применить загруженные данные
                CompleteSaveData saveData = JsonConvert.DeserializeObject<CompleteSaveData>(loadResult.message);
                SaveOperationResult applyResult = ApplySaveData(saveData);

                if (applyResult.IsSuccess)
                {
                    // Кешировать данные
                    cachedSaves[slotIndex] = saveData;
                    OnDataLoaded?.Invoke(slotIndex, saveData);
                    Debug.Log($"Игра загружена из слота {slotIndex}");
                }

                OnLoadCompleted?.Invoke(applyResult);
                return applyResult;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка загрузки игры: {e.Message}");
                var result = new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
                OnLoadCompleted?.Invoke(result);
                return result;
            }
        }

        /// <summary>
        /// Сохранить текущую игру (используется для автосохранения)
        /// </summary>
        public SaveOperationResult SaveCurrentGame()
        {
            // Найти последний использованный слот или использовать слот 0
            int currentSlot = GetCurrentSaveSlot();
            return SaveGame(currentSlot, "Автосохранение");
        }

        /// <summary>
        /// Получить информацию о всех слотах сохранения
        /// </summary>
        public List<SaveSlotInfo> GetSaveSlots()
        {
            List<SaveSlotInfo> slots = new List<SaveSlotInfo>();

            for (int i = 0; i < maxSaveSlots; i++)
            {
                SaveSlotInfo slotInfo = GetSaveSlotInfo(i);
                slots.Add(slotInfo);
            }

            return slots;
        }

        /// <summary>
        /// Получить информацию о конкретном слоте
        /// </summary>
        public SaveSlotInfo GetSaveSlotInfo(int slotIndex)
        {
            string filePath = GetSaveFilePath(slotIndex);
            
            if (!File.Exists(filePath))
            {
                return new SaveSlotInfo { isEmpty = true };
            }

            try
            {
                // Попробовать загрузить из кеша
                if (cachedSaves.ContainsKey(slotIndex))
                {
                    return cachedSaves[slotIndex].GetSlotInfo();
                }

                // Загрузить только метаданные для быстрого отображения
                SaveOperationResult result = LoadFromFile(filePath);
                if (result.IsSuccess)
                {
                    CompleteSaveData saveData = JsonConvert.DeserializeObject<CompleteSaveData>(result.message);
                    return saveData.GetSlotInfo();
                }
                else
                {
                    return new SaveSlotInfo 
                    { 
                        isEmpty = false, 
                        isCorrupted = true, 
                        saveName = $"Поврежденное сохранение {slotIndex}" 
                    };
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка получения информации о слоте {slotIndex}: {e.Message}");
                return new SaveSlotInfo 
                { 
                    isEmpty = false, 
                    isCorrupted = true, 
                    saveName = "Ошибка чтения" 
                };
            }
        }

        /// <summary>
        /// Удалить сохранение из слота
        /// </summary>
        public SaveOperationResult DeleteSave(int slotIndex)
        {
            try
            {
                if (slotIndex < 0 || slotIndex >= maxSaveSlots)
                {
                    return new SaveOperationResult(SaveResult.UnknownError, $"Неверный индекс слота: {slotIndex}");
                }

                string filePath = GetSaveFilePath(slotIndex);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Удалить резервные копии
                for (int i = 0; i < maxBackups; i++)
                {
                    string backupPath = GetBackupFilePath(slotIndex, i);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }

                // Удалить из кеша
                if (cachedSaves.ContainsKey(slotIndex))
                {
                    cachedSaves.Remove(slotIndex);
                }

                Debug.Log($"Сохранение удалено из слота {slotIndex}");
                return new SaveOperationResult(SaveResult.Success, "Сохранение успешно удалено");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка удаления сохранения: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        #endregion

        #region Settings Save/Load

        /// <summary>
        /// Сохранить настройки игры
        /// </summary>
        public SaveOperationResult SaveSettings()
        {
            try
            {
                SettingsSaveData settings = GatherSettingsData();
                string filePath = Path.Combine(saveDirectory, SETTINGS_FILE);
                
                return SaveToFile(settings, filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка сохранения настроек: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        /// <summary>
        /// Загрузить настройки игры
        /// </summary>
        public SaveOperationResult LoadSettings()
        {
            try
            {
                string filePath = Path.Combine(saveDirectory, SETTINGS_FILE);
                if (!File.Exists(filePath))
                {
                    // Создать настройки по умолчанию
                    SettingsSaveData defaultSettings = new SettingsSaveData();
                    defaultSettings.ApplyDefaults();
                    ApplySettingsData(defaultSettings);
                    return SaveSettings(); // Сохранить значения по умолчанию
                }

                SaveOperationResult result = LoadFromFile(filePath);
                if (result.IsSuccess)
                {
                    SettingsSaveData settings = JsonConvert.DeserializeObject<SettingsSaveData>(result.message);
                    ApplySettingsData(settings);
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка загрузки настроек: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        #endregion

        #region Data Gathering

        /// <summary>
        /// Собрать все данные для сохранения
        /// </summary>
        private CompleteSaveData GatherSaveData(string saveName)
        {
            try
            {
                CompleteSaveData saveData = new CompleteSaveData();

                // Состояние игры
                saveData.gameState = GatherGameStateData(saveName);

                // Данные игрока
                saveData.playerData = GatherPlayerData();

                // Данные локаций
                saveData.locationData = GatherLocationData();

                // Настройки
                saveData.settingsData = GatherSettingsData();

                // Статистика
                saveData.statisticsData = GatherStatisticsData();

                // Данные системы прогрессии
                saveData.progressionData = GatherProgressionData();

                // Данные системы времени
                saveData.timeData = GatherTimeData();

                saveData.lastModified = DateTime.Now;
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка сбора данных для сохранения: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Собрать данные состояния игры
        /// </summary>
        private GameStateSaveData GatherGameStateData(string saveName)
        {
            GameStateSaveData data = new GameStateSaveData();
            
            if (!string.IsNullOrEmpty(saveName))
                data.saveName = saveName;
            
            data.gameVersion = Application.version;
            data.saveCount++;
            
            if (gameManager != null)
            {
                data.currentTimeOfDay = gameManager.GetCurrentTimeOfDay().ToString();
                data.gameSpeed = Time.timeScale;
            }

            return data;
        }

        /// <summary>
        /// Собрать данные игрока
        /// </summary>
        private PlayerSaveData GatherPlayerData()
        {
            PlayerSaveData data = new PlayerSaveData();

            if (playerStats != null)
            {
                data.health = playerStats.Health;
                data.maxHealth = playerStats.MaxHealth;
                data.hunger = playerStats.Hunger;
                data.maxHunger = playerStats.MaxHunger;
                data.mood = playerStats.Mood;
                data.maxMood = playerStats.MaxMood;
                data.money = playerStats.Money;
                data.level = playerStats.Level;
                data.experience = playerStats.Experience;
                data.experienceToNextLevel = playerStats.ExperienceToNextLevel;
            }

            return data;
        }

        /// <summary>
        /// Собрать данные локаций
        /// </summary>
        private LocationSaveData GatherLocationData()
        {
            LocationSaveData data = new LocationSaveData();

            if (locationManager != null)
            {
                data.currentLocation = locationManager.GetCurrentLocationType().ToString();
                
                var unlockedLocations = locationManager.GetUnlockedLocations();
                data.unlockedLocations = new List<string>();
                foreach (var location in unlockedLocations)
                {
                    data.unlockedLocations.Add(location.ToString());
                }
            }

            return data;
        }

        /// <summary>
        /// Собрать данные настроек
        /// </summary>
        private SettingsSaveData GatherSettingsData()
        {
            SettingsSaveData data = new SettingsSaveData();
            
            // Загрузить настройки из PlayerPrefs
            data.masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            data.musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            data.sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            data.isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
            
            data.qualityLevel = PlayerPrefs.GetInt("QualityLevel", 2);
            data.resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
            data.isFullscreen = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;
            data.fpsLimit = PlayerPrefs.GetInt("FPSLimit", 60);
            
            data.autoSaveEnabled = PlayerPrefs.GetInt("AutoSaveEnabled", 1) == 1;
            data.autoSaveInterval = PlayerPrefs.GetFloat("AutoSaveInterval", 60f);
            data.showNotifications = PlayerPrefs.GetInt("ShowNotifications", 1) == 1;
            data.warningAnimations = PlayerPrefs.GetInt("WarningAnimations", 1) == 1;

            return data;
        }

        /// <summary>
        /// Собрать статистические данные
        /// </summary>
        private StatisticsSaveData GatherStatisticsData()
        {
            StatisticsSaveData data = new StatisticsSaveData();
            
            // Загрузить статистику из PlayerPrefs или других источников
            data.totalPlayTime = PlayerPrefs.GetFloat("TotalPlayTime", 0f) + Time.realtimeSinceStartup;
            data.totalGameSessions = PlayerPrefs.GetInt("TotalGameSessions", 0) + 1;
            
            if (playerStats != null)
            {
                data.totalMoneyEarned = PlayerPrefs.GetFloat("TotalMoneyEarned", 0f);
                data.totalMoneySpent = PlayerPrefs.GetFloat("TotalMoneySpent", 0f);
                data.highestMoneyAmount = Mathf.Max(PlayerPrefs.GetFloat("HighestMoneyAmount", 0f), playerStats.Money);
                data.totalLevelUps = PlayerPrefs.GetInt("TotalLevelUps", 0);
                data.totalDeaths = PlayerPrefs.GetInt("TotalDeaths", 0);
                data.totalNPCInteractions = PlayerPrefs.GetInt("TotalNPCInteractions", 0);
            }

            data.lastPlayDate = DateTime.Now;

            return data;
        }

        /// <summary>
        /// Собрать данные системы прогрессии
        /// </summary>
        private ProgressionSaveData GatherProgressionData()
        {
            ProgressionSaveData data = new ProgressionSaveData();

            // Данные навыков
            if (skillSystem != null)
            {
                data.skillSystemData = skillSystem.GetSaveData();
            }

            // Данные магазина
            if (shopSystem != null)
            {
                data.shopSystemData = shopSystem.GetSaveData();
            }

            // Данные работы
            if (jobSystem != null)
            {
                data.jobSystemData = jobSystem.GetSaveData();
            }

            // Данные образования
            if (educationSystem != null)
            {
                data.educationSystemData = educationSystem.GetSaveData();
            }

            // Данные достижений
            if (achievementSystem != null)
            {
                data.achievementSystemData = achievementSystem.GetSaveData();
            }

            // Данные уровня
            if (levelSystem != null)
            {
                data.levelSystemData = levelSystem.GetSaveData();
            }

            return data;
        }

        /// <summary>
        /// Собрать данные системы времени
        /// </summary>
        private TimeSaveData GatherTimeData()
        {
            TimeSaveData data = new TimeSaveData();

            // Данные времени дня
            if (timeOfDayManager != null)
            {
                data.timeOfDayManager = timeOfDayManager.GetSaveData();
            }

            // Данные погоды
            if (weatherSystem != null)
            {
                data.weatherSystem = weatherSystem.GetSaveData();
            }

            // Данные сна
            if (sleepSystem != null)
            {
                data.sleepSystem = sleepSystem.GetSaveData();
            }

            // Данные влияния времени на геймплей
            if (timeBasedGameplay != null)
            {
                data.timeBasedGameplay = timeBasedGameplay.GetSaveData();
            }

            // Данные событий времени
            if (timeBasedEvents != null)
            {
                data.timeBasedEvents = timeBasedEvents.GetSaveData();
            }

            return data;
        }

        #endregion

        #region Data Application

        /// <summary>
        /// Применить загруженные данные ко всем системам
        /// </summary>
        private SaveOperationResult ApplySaveData(CompleteSaveData saveData)
        {
            try
            {
                // Применить данные игрока
                if (playerStats != null)
                {
                    ApplyPlayerData(saveData.playerData);
                }

                // Применить данные локаций
                if (locationManager != null)
                {
                    ApplyLocationData(saveData.locationData);
                }

                // Применить настройки
                ApplySettingsData(saveData.settingsData);

                // Применить состояние игры
                ApplyGameStateData(saveData.gameState);

                // Обновить статистику
                ApplyStatisticsData(saveData.statisticsData);

                // Применить данные прогрессии
                if (saveData.progressionData != null)
                {
                    ApplyProgressionData(saveData.progressionData);
                }

                // Применить данные времени
                if (saveData.timeData != null)
                {
                    ApplyTimeData(saveData.timeData);
                }

                return new SaveOperationResult(SaveResult.Success, "Данные успешно применены");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка применения данных: {e.Message}");
                return new SaveOperationResult(SaveResult.UnknownError, e.Message, "", e);
            }
        }

        /// <summary>
        /// Применить данные игрока
        /// </summary>
        private void ApplyPlayerData(PlayerSaveData data)
        {
            if (playerStats == null) return;

            playerStats.SetHealth(data.health);
            playerStats.SetHunger(data.hunger);
            playerStats.SetMood(data.mood);
            playerStats.SetMoney(data.money);
            playerStats.SetLevel(data.level);
            playerStats.SetExperience(data.experience);
        }

        /// <summary>
        /// Применить данные локаций
        /// </summary>
        private void ApplyLocationData(LocationSaveData data)
        {
            if (locationManager == null) return;

            // Разблокировать локации
            foreach (string locationStr in data.unlockedLocations)
            {
                if (Enum.TryParse<LocationType>(locationStr, out LocationType locationType))
                {
                    locationManager.UnlockLocation(locationType);
                }
            }

            // Установить текущую локацию
            if (Enum.TryParse<LocationType>(data.currentLocation, out LocationType currentLocation))
            {
                locationManager.TryMoveToLocation(currentLocation);
            }
        }

        /// <summary>
        /// Применить настройки
        /// </summary>
        private void ApplySettingsData(SettingsSaveData data)
        {
            // Сохранить в PlayerPrefs
            PlayerPrefs.SetFloat("MasterVolume", data.masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", data.musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", data.sfxVolume);
            PlayerPrefs.SetInt("IsMuted", data.isMuted ? 1 : 0);
            
            PlayerPrefs.SetInt("QualityLevel", data.qualityLevel);
            PlayerPrefs.SetInt("ResolutionIndex", data.resolutionIndex);
            PlayerPrefs.SetInt("IsFullscreen", data.isFullscreen ? 1 : 0);
            PlayerPrefs.SetInt("FPSLimit", data.fpsLimit);
            
            PlayerPrefs.SetInt("AutoSaveEnabled", data.autoSaveEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("AutoSaveInterval", data.autoSaveInterval);
            PlayerPrefs.SetInt("ShowNotifications", data.showNotifications ? 1 : 0);
            PlayerPrefs.SetInt("WarningAnimations", data.warningAnimations ? 1 : 0);

            // Применить настройки графики
            QualitySettings.SetQualityLevel(data.qualityLevel);
            Application.targetFrameRate = data.fpsLimit;
            
            // Уведомить другие системы об изменении настроек
            GameEvents.TriggerNotification("Настройки загружены", NotificationType.Info);
        }

        /// <summary>
        /// Применить состояние игры
        /// </summary>
        private void ApplyGameStateData(GameStateSaveData data)
        {
            if (gameManager != null && data != null)
            {
                gameManager.ApplyGameStateData(data);
            }
        }

        /// <summary>
        /// Применить статистические данные
        /// </summary>
        private void ApplyStatisticsData(StatisticsSaveData data)
        {
            // Сохранить статистику в PlayerPrefs для дальнейшего использования
            PlayerPrefs.SetFloat("TotalPlayTime", data.totalPlayTime);
            PlayerPrefs.SetInt("TotalGameSessions", data.totalGameSessions);
            PlayerPrefs.SetFloat("TotalMoneyEarned", data.totalMoneyEarned);
            PlayerPrefs.SetFloat("TotalMoneySpent", data.totalMoneySpent);
            PlayerPrefs.SetFloat("HighestMoneyAmount", data.highestMoneyAmount);
            PlayerPrefs.SetInt("TotalLevelUps", data.totalLevelUps);
            PlayerPrefs.SetInt("TotalDeaths", data.totalDeaths);
            PlayerPrefs.SetInt("TotalNPCInteractions", data.totalNPCInteractions);
        }

        /// <summary>
        /// Применить данные системы прогрессии
        /// </summary>
        private void ApplyProgressionData(ProgressionSaveData data)
        {
            // Применить данные навыков
            if (skillSystem != null && data.skillSystemData != null)
            {
                skillSystem.LoadData(data.skillSystemData);
            }

            // Применить данные магазина
            if (shopSystem != null && data.shopSystemData != null)
            {
                shopSystem.LoadData(data.shopSystemData);
            }

            // Применить данные работы
            if (jobSystem != null && data.jobSystemData != null)
            {
                jobSystem.LoadData(data.jobSystemData);
            }

            // Применить данные образования
            if (educationSystem != null && data.educationSystemData != null)
            {
                educationSystem.LoadData(data.educationSystemData);
            }

            // Применить данные достижений
            if (achievementSystem != null && data.achievementSystemData != null)
            {
                achievementSystem.LoadData(data.achievementSystemData);
            }

            // Применить данные уровня
            if (levelSystem != null && data.levelSystemData != null)
            {
                levelSystem.LoadData(data.levelSystemData);
            }

            Debug.Log("Данные системы прогрессии успешно применены");
        }

        /// <summary>
        /// Применить данные системы времени
        /// </summary>
        private void ApplyTimeData(TimeSaveData data)
        {
            // Применить данные времени дня
            if (timeOfDayManager != null && data.timeOfDayManager != null)
            {
                timeOfDayManager.LoadData(data.timeOfDayManager);
            }

            // Применить данные погоды
            if (weatherSystem != null && data.weatherSystem != null)
            {
                weatherSystem.LoadData(data.weatherSystem);
            }

            // Применить данные сна
            if (sleepSystem != null && data.sleepSystem != null)
            {
                sleepSystem.LoadData(data.sleepSystem);
            }

            // Применить данные влияния времени на геймплей
            if (timeBasedGameplay != null && data.timeBasedGameplay != null)
            {
                timeBasedGameplay.LoadData(data.timeBasedGameplay);
            }

            // Применить данные событий времени
            if (timeBasedEvents != null && data.timeBasedEvents != null)
            {
                timeBasedEvents.LoadData(data.timeBasedEvents);
            }

            Debug.Log("Данные системы времени успешно применены");
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Сохранить данные в файл
        /// </summary>
        private SaveOperationResult SaveToFile<T>(T data, string filePath)
        {
            try
            {
                string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
                
                if (enableEncryption)
                {
                    jsonData = EncryptString(jsonData);
                }

                File.WriteAllText(filePath, jsonData, Encoding.UTF8);
                return new SaveOperationResult(SaveResult.Success, "Файл успешно сохранен", filePath);
            }
            catch (UnauthorizedAccessException e)
            {
                return new SaveOperationResult(SaveResult.PermissionDenied, $"Нет доступа к файлу: {e.Message}", filePath, e);
            }
            catch (Exception e)
            {
                return new SaveOperationResult(SaveResult.UnknownError, $"Ошибка записи файла: {e.Message}", filePath, e);
            }
        }

        /// <summary>
        /// Загрузить данные из файла
        /// </summary>
        private SaveOperationResult LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new SaveOperationResult(SaveResult.FileNotFound, "Файл не найден", filePath);
                }

                string jsonData = File.ReadAllText(filePath, Encoding.UTF8);
                
                if (enableEncryption)
                {
                    jsonData = DecryptString(jsonData);
                }

                // Валидация JSON
                try
                {
                    var testParse = JsonConvert.DeserializeObject(jsonData);
                }
                catch (JsonException e)
                {
                    return new SaveOperationResult(SaveResult.CorruptedData, $"Поврежденные данные JSON: {e.Message}", filePath, e);
                }

                return new SaveOperationResult(SaveResult.Success, jsonData, filePath);
            }
            catch (UnauthorizedAccessException e)
            {
                return new SaveOperationResult(SaveResult.PermissionDenied, $"Нет доступа к файлу: {e.Message}", filePath, e);
            }
            catch (Exception e)
            {
                return new SaveOperationResult(SaveResult.UnknownError, $"Ошибка чтения файла: {e.Message}", filePath, e);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Получить путь к файлу сохранения
        /// </summary>
        private string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(saveDirectory, $"save_{slotIndex}{SAVE_EXTENSION}");
        }

        /// <summary>
        /// Получить путь к резервной копии
        /// </summary>
        private string GetBackupFilePath(int slotIndex, int backupIndex)
        {
            return Path.Combine(saveDirectory, $"save_{slotIndex}_backup_{backupIndex}{BACKUP_EXTENSION}");
        }

        /// <summary>
        /// Получить текущий слот сохранения
        /// </summary>
        private int GetCurrentSaveSlot()
        {
            return PlayerPrefs.GetInt("CurrentSaveSlot", 0);
        }

        /// <summary>
        /// Создать резервную копию
        /// </summary>
        private void CreateBackup(string originalFilePath, int slotIndex)
        {
            try
            {
                // Сдвинуть существующие резервные копии
                for (int i = maxBackups - 1; i > 0; i--)
                {
                    string currentBackup = GetBackupFilePath(slotIndex, i - 1);
                    string nextBackup = GetBackupFilePath(slotIndex, i);
                    
                    if (File.Exists(currentBackup))
                    {
                        if (File.Exists(nextBackup))
                            File.Delete(nextBackup);
                        File.Move(currentBackup, nextBackup);
                    }
                }

                // Создать новую резервную копию
                string latestBackup = GetBackupFilePath(slotIndex, 0);
                File.Copy(originalFilePath, latestBackup, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось создать резервную копию: {e.Message}");
            }
        }

        /// <summary>
        /// Попробовать восстановить из резервной копии
        /// </summary>
        private SaveOperationResult TryRestoreFromBackup(int slotIndex)
        {
            for (int i = 0; i < maxBackups; i++)
            {
                string backupPath = GetBackupFilePath(slotIndex, i);
                if (File.Exists(backupPath))
                {
                    SaveOperationResult result = LoadFromFile(backupPath);
                    if (result.IsSuccess)
                    {
                        Debug.Log($"Данные восстановлены из резервной копии {i}");
                        return result;
                    }
                }
            }

            return new SaveOperationResult(SaveResult.CorruptedData, "Не удалось восстановить из резервных копий");
        }

        /// <summary>
        /// Валидировать данные сохранения
        /// </summary>
        private bool ValidateSaveData(CompleteSaveData saveData)
        {
            if (saveData == null) return false;

            // Проверить версию
            if (saveData.gameState.version < SaveVersion.MIN_SUPPORTED_VERSION)
            {
                Debug.LogWarning($"Устаревшая версия сохранения: {saveData.gameState.version}");
                return false;
            }

            // Проверить контрольную сумму
            if (validateChecksums && !string.IsNullOrEmpty(saveData.checksum))
            {
                string currentChecksum = CalculateChecksum(saveData);
                if (currentChecksum != saveData.checksum)
                {
                    Debug.LogWarning("Контрольная сумма не совпадает");
                    if (!autoRepairCorrupted)
                        return false;
                }
            }

            // Валидировать все компоненты
            return saveData.IsValid();
        }

        /// <summary>
        /// Вычислить контрольную сумму данных
        /// </summary>
        private string CalculateChecksum(CompleteSaveData saveData)
        {
            try
            {
                // Временно обнулить контрольную сумму для расчета
                string originalChecksum = saveData.checksum;
                saveData.checksum = "";

                string jsonData = JsonConvert.SerializeObject(saveData);
                
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonData));
                    string checksum = Convert.ToBase64String(hashBytes);
                    
                    saveData.checksum = originalChecksum; // Восстановить оригинальную контрольную сумму
                    return checksum;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка вычисления контрольной суммы: {e.Message}");
                return "";
            }
        }

        /// <summary>
        /// Зашифровать строку
        /// </summary>
        private string EncryptString(string plainText)
        {
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes.Take(32).Concat(new byte[32 - keyBytes.Length]).ToArray();
                    aes.IV = keyBytes.Take(16).Concat(new byte[16 - keyBytes.Length]).ToArray();

                    using (var encryptor = aes.CreateEncryptor())
                    using (var msEncrypt = new MemoryStream())
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                        csEncrypt.FlushFinalBlock();
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка шифрования: {e.Message}");
                return plainText; // Возвращаем незашифрованный текст при ошибке
            }
        }

        /// <summary>
        /// Расшифровать строку
        /// </summary>
        private string DecryptString(string cipherText)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptionKey);

                using (var aes = Aes.Create())
                {
                    aes.Key = keyBytes.Take(32).Concat(new byte[32 - keyBytes.Length]).ToArray();
                    aes.IV = keyBytes.Take(16).Concat(new byte[16 - keyBytes.Length]).ToArray();

                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(cipherBytes))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка расшифровки: {e.Message}");
                return cipherText; // Возвращаем зашифрованный текст при ошибке
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Сохранить текущую игру")]
        private void DebugSaveCurrentGame()
        {
            SaveCurrentGame();
        }

        [ContextMenu("Загрузить игру (слот 0)")]
        private void DebugLoadGame()
        {
            LoadGame(0);
        }

        [ContextMenu("Показать информацию о слотах")]
        private void DebugShowSlotInfo()
        {
            var slots = GetSaveSlots();
            foreach (var slot in slots)
            {
                Debug.Log($"Слот: {slot.saveName}, Уровень: {slot.level}, Деньги: {slot.money}");
            }
        }

        [ContextMenu("Очистить все сохранения")]
        private void DebugClearAllSaves()
        {
            for (int i = 0; i < maxSaveSlots; i++)
            {
                DeleteSave(i);
            }
        }

        #endregion
    }
}
