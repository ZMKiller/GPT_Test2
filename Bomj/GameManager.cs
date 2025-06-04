using System;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Центральный менеджер игры "От бомжа до миллионера"
    /// Управляет общим состоянием игры и координирует работу всех систем
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Ссылки на компоненты")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private StatsDegradationManager degradationManager;
        
        [Header("Система сохранений")]
        [SerializeField] private bool enableSaveSystem = true;
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool useLegacySaves = false; // Для обратной совместимости
        
        [Header("Настройки сложности")]
        [SerializeField] private float difficultyMultiplier = 1f;
        
        [Header("Время дня")]
        [SerializeField] private TimeOfDay currentTimeOfDay = TimeOfDay.Day;
        [SerializeField] private float timeOfDayDuration = 300f; // 5 минут на время дня
        [SerializeField] private bool autoChangeTimeOfDay = true;
        
        // Состояние игры
        private GameState currentGameState = GameState.MainMenu;
        private bool isPaused = false;
        private float gameTime = 0f;
        private float timeSinceLastSave = 0f;
        
        // Системы сохранений
        private SaveSystem saveSystem;
        private AutoSaveManager autoSaveManager;
        private SaveSlotManager slotManager;
        
        // События
        public static event Action<GameState> OnGameStateChanged;
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action OnGameReset;
        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;
        public static event Action<TimeOfDay> OnTimeOfDayChanged;

        // Синглтон для глобального доступа
        public static GameManager Instance { get; private set; }

        // Публичные свойства
        public GameState CurrentGameState => currentGameState;
        public bool IsPaused => isPaused;
        public float GameTime => gameTime;
        public PlayerStats PlayerStats => playerStats;
        public StatsDegradationManager DegradationManager => degradationManager;
        public TimeOfDay GetCurrentTimeOfDay() => currentTimeOfDay;

        private void Awake()
        {
            // Реализация синглтона
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Подписка на события
            SubscribeToEvents();
            
            // Попытка автоматической загрузки
            if (enableSaveSystem && loadOnStart && HasAnySaveData())
            {
                // Попробовать загрузить последнее сохранение
                LoadLastSave();
            }
            else
            {
                StartNewGame();
            }
        }

        private void Update()
        {
            if (currentGameState == GameState.Playing && !isPaused)
            {
                gameTime += Time.deltaTime;
                
                // Обновление времени дня
                if (autoChangeTimeOfDay)
                {
                    UpdateTimeOfDay();
                }
            }
        }
        
        /// <summary>
        /// Обновление времени дня
        /// </summary>
        private void UpdateTimeOfDay()
        {
            // Проверяем, нужно ли сменить время дня
            float timeInCurrentPeriod = gameTime % timeOfDayDuration;
            int periodIndex = (int)(gameTime / timeOfDayDuration) % 4;
            TimeOfDay newTimeOfDay = (TimeOfDay)periodIndex;
            
            if (newTimeOfDay != currentTimeOfDay)
            {
                TimeOfDay oldTimeOfDay = currentTimeOfDay;
                currentTimeOfDay = newTimeOfDay;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
                GameEvents.TriggerTimeOfDayChanged(currentTimeOfDay);
            }
        }

        /// <summary>
        /// Инициализация игры
        /// </summary>
        private void InitializeGame()
        {
            // Найти компоненты, если они не назначены
            if (playerStats == null)
                playerStats = FindObjectOfType<PlayerStats>();
            
            if (degradationManager == null)
                degradationManager = FindObjectOfType<StatsDegradationManager>();
            
            // Инициализация систем сохранения
            if (enableSaveSystem)
            {
                InitializeSaveSystems();
            }
            
            // Проверка наличия необходимых компонентов
            if (playerStats == null)
            {
                Debug.LogError("GameManager: Не найден компонент PlayerStats!");
            }
            
            if (degradationManager == null)
            {
                Debug.LogError("GameManager: Не найден компонент StatsDegradationManager!");
            }
        }
        
        /// <summary>
        /// Инициализация систем сохранения
        /// </summary>
        private void InitializeSaveSystems()
        {
            saveSystem = SaveSystem.Instance;
            autoSaveManager = FindObjectOfType<AutoSaveManager>();
            slotManager = FindObjectOfType<SaveSlotManager>();
            
            if (saveSystem == null)
            {
                Debug.LogWarning("GameManager: SaveSystem не найден!");
            }
            
            if (autoSaveManager == null)
            {
                Debug.LogWarning("GameManager: AutoSaveManager не найден!");
            }
            
            if (slotManager == null)
            {
                Debug.LogWarning("GameManager: SaveSlotManager не найден!");
            }
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnPlayerDeath += OnPlayerDeath;
            GameEvents.OnLevelUp += OnPlayerLevelUp;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            GameEvents.OnLevelUp -= OnPlayerLevelUp;
        }

        /// <summary>
        /// Начать новую игру
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log("Начинаем новую игру");
            
            // Сброс характеристик игрока
            if (playerStats != null)
            {
                playerStats.ResetStats();
            }
            
            // Активация деградации
            if (degradationManager != null)
            {
                degradationManager.IsActive = true;
            }
            
            // Сброс игрового времени
            gameTime = 0f;
            timeSinceLastSave = 0f;
            
            // Изменение состояния игры
            ChangeGameState(GameState.Playing);
            
            OnGameReset?.Invoke();
        }

        /// <summary>
        /// Поставить игру на паузу
        /// </summary>
        public void PauseGame()
        {
            if (currentGameState == GameState.Playing && !isPaused)
            {
                isPaused = true;
                Time.timeScale = 0f;
                
                if (degradationManager != null)
                {
                    degradationManager.IsActive = false;
                }
                
                ChangeGameState(GameState.Paused);
                OnGamePaused?.Invoke();
                
                Debug.Log("Игра поставлена на паузу");
            }
        }

        /// <summary>
        /// Возобновить игру
        /// </summary>
        public void ResumeGame()
        {
            if (currentGameState == GameState.Paused && isPaused)
            {
                isPaused = false;
                Time.timeScale = 1f;
                
                if (degradationManager != null)
                {
                    degradationManager.IsActive = true;
                }
                
                ChangeGameState(GameState.Playing);
                OnGameResumed?.Invoke();
                
                Debug.Log("Игра возобновлена");
            }
        }

        /// <summary>
        /// Переключить паузу
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        /// <summary>
        /// Изменить состояние игры
        /// </summary>
        /// <param name="newState">Новое состояние</param>
        private void ChangeGameState(GameState newState)
        {
            if (currentGameState != newState)
            {
                GameState oldState = currentGameState;
                currentGameState = newState;
                
                Debug.Log($"Состояние игры изменено с {oldState} на {newState}");
                OnGameStateChanged?.Invoke(newState);
            }
        }

        /// <summary>
        /// Сохранить игру в текущий слот
        /// </summary>
        public void SaveGame()
        {
            if (!enableSaveSystem || saveSystem == null)
            {
                Debug.LogWarning("Система сохранений отключена или не найдена");
                return;
            }

            try
            {
                SaveOperationResult result = saveSystem.SaveCurrentGame();
                
                if (result.IsSuccess)
                {
                    OnGameSaved?.Invoke();
                    Debug.Log("Игра сохранена в текущий слот");
                }
                else
                {
                    Debug.LogError($"Ошибка сохранения: {result.message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при сохранении игры: {e.Message}");
            }
        }

        /// <summary>
        /// Сохранить игру в указанный слот
        /// </summary>
        public void SaveGame(int slotIndex, string saveName = "")
        {
            if (!enableSaveSystem || saveSystem == null)
            {
                Debug.LogWarning("Система сохранений отключена или не найдена");
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(saveName))
                {
                    saveName = $"Сохранение {DateTime.Now:dd.MM.yyyy HH:mm}";
                }

                SaveOperationResult result = saveSystem.SaveGame(slotIndex, saveName);
                
                if (result.IsSuccess)
                {
                    OnGameSaved?.Invoke();
                    Debug.Log($"Игра сохранена в слот {slotIndex}");
                }
                else
                {
                    Debug.LogError($"Ошибка сохранения в слот {slotIndex}: {result.message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при сохранении игры в слот {slotIndex}: {e.Message}");
            }
        }

        /// <summary>
        /// Загрузить игру из указанного слота
        /// </summary>
        public void LoadGame(int slotIndex)
        {
            if (!enableSaveSystem || saveSystem == null)
            {
                Debug.LogWarning("Система сохранений отключена или не найдена");
                StartNewGame();
                return;
            }

            try
            {
                SaveOperationResult result = saveSystem.LoadGame(slotIndex);
                
                if (result.IsSuccess)
                {
                    ChangeGameState(GameState.Playing);
                    OnGameLoaded?.Invoke();
                    Debug.Log($"Игра загружена из слота {slotIndex}");
                }
                else
                {
                    Debug.LogError($"Ошибка загрузки из слота {slotIndex}: {result.message}");
                    StartNewGame();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при загрузке игры из слота {slotIndex}: {e.Message}");
                StartNewGame();
            }
        }

        /// <summary>
        /// Загрузить последнее сохранение
        /// </summary>
        private void LoadLastSave()
        {
            if (slotManager == null)
            {
                // Попробовать загрузить старое сохранение для обратной совместимости
                if (useLegacySaves && HasLegacySaveData())
                {
                    LoadLegacyGame();
                }
                else
                {
                    StartNewGame();
                }
                return;
            }

            // Найти последний использованный слот
            int currentSlot = slotManager.GetCurrentSlot();
            if (currentSlot != -1 && !slotManager.IsSlotEmpty(currentSlot))
            {
                LoadGame(currentSlot);
            }
            else
            {
                // Найти самое свежее сохранение
                var slots = slotManager.GetSlotsSortedByDate(true);
                var latestSlot = slots.Find(s => !s.isEmpty && !s.isCorrupted);
                
                if (latestSlot != null)
                {
                    LoadGame(latestSlot.slotIndex);
                }
                else
                {
                    StartNewGame();
                }
            }
        }

        /// <summary>
        /// Проверить наличие любых сохранений
        /// </summary>
        public bool HasAnySaveData()
        {
            if (!enableSaveSystem)
                return false;

            if (slotManager != null)
            {
                var slots = slotManager.GetAllSlots();
                return slots.Exists(s => !s.isEmpty);
            }

            // Проверка старых сохранений для обратной совместимости
            return useLegacySaves && HasLegacySaveData();
        }

        /// <summary>
        /// Проверить наличие старых сохранений
        /// </summary>
        private bool HasLegacySaveData()
        {
            return PlayerPrefs.HasKey("GameSaveData");
        }

        /// <summary>
        /// Загрузить старое сохранение
        /// </summary>
        private void LoadLegacyGame()
        {
            try
            {
                if (HasLegacySaveData())
                {
                    string jsonData = PlayerPrefs.GetString("GameSaveData");
                    GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                    
                    gameTime = saveData.gameTime;
                    difficultyMultiplier = saveData.difficultyMultiplier;
                    
                    if (playerStats != null && saveData.playerStatsData != null)
                    {
                        playerStats.LoadData(saveData.playerStatsData);
                    }
                    
                    ChangeGameState(GameState.Playing);
                    OnGameLoaded?.Invoke();
                    
                    Debug.Log("Старое сохранение загружено");
                    
                    // Предложить мигрировать на новую систему
                    if (enableSaveSystem)
                    {
                        Debug.Log("Рекомендуется пересохранить игру для перехода на новую систему сохранений");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка при загрузке старого сохранения: {e.Message}");
                StartNewGame();
            }
        }

        /// <summary>
        /// Установить множитель сложности
        /// </summary>
        /// <param name="multiplier">Новый множитель сложности</param>
        public void SetDifficultyMultiplier(float multiplier)
        {
            difficultyMultiplier = Mathf.Max(0.1f, multiplier);
            
            // Применить к менеджеру деградации
            if (degradationManager != null)
            {
                var info = degradationManager.GetDegradationInfo();
                degradationManager.SetHungerIncreaseRate(info.hungerRate * difficultyMultiplier);
                degradationManager.SetMoodDecreaseRate(info.moodRate * difficultyMultiplier);
                degradationManager.SetHealthDecreaseRate(info.healthRate * difficultyMultiplier);
            }
        }

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            Debug.Log("Игрок умер!");
            ChangeGameState(GameState.GameOver);
            
            if (degradationManager != null)
            {
                degradationManager.IsActive = false;
            }
        }

        /// <summary>
        /// Обработчик повышения уровня игрока
        /// </summary>
        private void OnPlayerLevelUp(LevelUpData data)
        {
            Debug.Log($"Поздравляем! Достигнут {data.newLevel} уровень!");
        }

        /// <summary>
        /// Установить время дня
        /// </summary>
        public void SetTimeOfDay(TimeOfDay newTimeOfDay)
        {
            if (currentTimeOfDay != newTimeOfDay)
            {
                currentTimeOfDay = newTimeOfDay;
                GameEvents.TriggerTimeOfDayChanged(newTimeOfDay);
                Debug.Log($"Время дня изменено на: {newTimeOfDay}");
            }
        }

        /// <summary>
        /// Получить текущее время дня
        /// </summary>
        public TimeOfDay GetTimeOfDay()
        {
            return currentTimeOfDay;
        }

        /// <summary>
        /// Получить данные состояния игры для сохранения
        /// </summary>
        public GameStateSaveData GetGameStateData()
        {
            return new GameStateSaveData
            {
                version = SaveValidation.GetCurrentSaveVersion(),
                saveDate = DateTime.Now.ToBinary(),
                gameTime = gameTime,
                difficultyMultiplier = difficultyMultiplier,
                timeOfDay = currentTimeOfDay,
                checksum = "" // Будет вычислена в SaveSystem
            };
        }

        /// <summary>
        /// Применить данные состояния игры при загрузке
        /// </summary>
        public void ApplyGameStateData(GameStateSaveData gameStateData)
        {
            if (gameStateData == null) return;

            gameTime = gameStateData.gameTime;
            difficultyMultiplier = gameStateData.difficultyMultiplier;
            SetTimeOfDay(gameStateData.timeOfDay);

            // Применить множитель сложности
            SetDifficultyMultiplier(difficultyMultiplier);
        }

        /// <summary>
        /// Выйти в главное меню
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (enableSaveSystem)
            {
                SaveGame(); // Автосохранение перед выходом
            }
            
            ChangeGameState(GameState.MainMenu);
            
            if (degradationManager != null)
            {
                degradationManager.IsActive = false;
            }
        }

        /// <summary>
        /// Полностью выйти из игры
        /// </summary>
        public void QuitGame()
        {
            if (enableSaveSystem)
            {
                SaveGame(); // Сохранить перед выходом
            }
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentGameState == GameState.Playing && enableSaveSystem)
            {
                SaveGame(); // Автосохранение при сворачивании
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && currentGameState == GameState.Playing && enableSaveSystem)
            {
                SaveGame(); // Автосохранение при потере фокуса
            }
        }
    }

    /// <summary>
    /// Структура данных для сохранения игры (устаревшая, для обратной совместимости)
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public float gameTime;
        public float difficultyMultiplier;
        public PlayerStatsData playerStatsData;
        public DegradationInfo degradationInfo;
        public long saveDateTime;
        public TimeOfDay timeOfDay = TimeOfDay.Day;
    }
}