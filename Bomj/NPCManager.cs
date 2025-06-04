using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Менеджер управления прохожими (NPC)
    /// </summary>
    public class NPCManager : MonoBehaviour
    {
        [Header("Ссылки на ресурсы")]
        [SerializeField] private GameObject npcPrefab;                  // Префаб NPC
        [SerializeField] private NPCDatabase npcDatabase;              // База данных типов NPC
        [SerializeField] private Transform npcParent;                  // Родительский объект для NPC
        
        [Header("Настройки спавна")]
        [SerializeField] private int maxNPCCount = 10;                 // Максимальное количество NPC
        [SerializeField] private float spawnInterval = 2f;             // Интервал между спавном
        [SerializeField] private float spawnRadius = 15f;              // Радиус спавна от игрока
        [SerializeField] private float despawnDistance = 20f;          // Расстояние удаления NPC
        
        [Header("Границы карты")]
        [SerializeField] private Vector2 mapBounds = new Vector2(50f, 30f); // Размеры карты
        [SerializeField] private bool useScreenBounds = true;               // Использовать границы экрана
        
        [Header("Настройки времени дня")]
        [SerializeField] private TimeOfDay currentTimeOfDay = TimeOfDay.Day;
        [SerializeField] private float timeOfDayDuration = 300f;            // Продолжительность времени дня в секундах
        [SerializeField] private bool autoChangeTimeOfDay = true;           // Автоматическая смена времени дня
        
        [Header("Настройки популяции")]
        [SerializeField] private AnimationCurve populationByTime = AnimationCurve.Linear(0f, 0.3f, 1f, 1f); // Кривая популяции по времени дня
        [SerializeField] private float baseSpawnRate = 1f;                  // Базовая скорость спавна
        
        [Header("Интеграция с локациями")]
        [SerializeField] private bool useLocationSystem = true;             // Использовать систему локаций
        [SerializeField] private float locationSpawnModifier = 1f;          // Модификатор спавна для локаций
        
        // Приватные переменные
        private List<NPCController> activeNPCs = new List<NPCController>();
        private Transform playerTransform;
        private Camera mainCamera;
        private float lastSpawnTime;
        private float timeOfDayTimer;
        private Coroutine spawnCoroutine;
        
        // Система локаций
        private LocationManager locationManager;
        private LocationData currentLocationData;
        private NPCType[] currentAvailableNPCTypes;
        
        // События
        public Action<NPCController> OnNPCSpawned;
        public Action<NPCController> OnNPCDespawned;
        public Action<TimeOfDay> OnTimeOfDayChanged;
        public Action<float> OnTotalMoneyEarned; // Общий заработок от всех NPC

        // Статистика
        private float totalMoneyEarned = 0f;
        private int totalNPCsSpawned = 0;

        #region Unity Callbacks

        private void Start()
        {
            InitializeManager();
            StartSpawning();
        }

        private void Update()
        {
            UpdateTimeOfDay();
            CleanupDistantNPCs();
            UpdateNPCStatistics();
        }

        private void OnDestroy()
        {
            StopSpawning();
            
            // Отписка от событий
            GameEvents.OnPlayerDeath -= OnPlayerDeath;
            if (useLocationSystem)
            {
                GameEvents.OnLocationChanged -= OnLocationChanged;
            }
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать менеджер
        /// </summary>
        private void InitializeManager()
        {
            // Найти игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("NPCManager: Игрок не найден!");
            }

            // Получить главную камеру
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }

            // Создать родительский объект для NPC если не назначен
            if (npcParent == null)
            {
                GameObject parent = new GameObject("NPCs");
                npcParent = parent.transform;
            }

            // Проверить ресурсы
            if (npcPrefab == null)
            {
                Debug.LogError("NPCManager: Префаб NPC не назначен!");
            }

            if (npcDatabase == null)
            {
                Debug.LogError("NPCManager: База данных NPC не назначена!");
            }

            // Подписаться на события игры
            if (GameManager.Instance != null)
            {
                GameEvents.OnPlayerDeath += OnPlayerDeath;
            }

            // Инициализация системы локаций
            if (useLocationSystem)
            {
                locationManager = FindObjectOfType<LocationManager>();
                if (locationManager != null)
                {
                    GameEvents.OnLocationChanged += OnLocationChanged;
                    UpdateCurrentLocationData();
                    Debug.Log("NPCManager интегрирован с системой локаций");
                }
                else
                {
                    Debug.LogWarning("LocationManager не найден, система локаций отключена");
                    useLocationSystem = false;
                }
            }

            Debug.Log("NPCManager инициализирован");
        }

        #endregion

        #region Управление спавном

        /// <summary>
        /// Начать спавн NPC
        /// </summary>
        public void StartSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
            }
            
            spawnCoroutine = StartCoroutine(SpawnCoroutine());
            Debug.Log("Спавн NPC запущен");
        }

        /// <summary>
        /// Остановить спавн NPC
        /// </summary>
        public void StopSpawning()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            Debug.Log("Спавн NPC остановлен");
        }

        /// <summary>
        /// Корутина спавна NPC
        /// </summary>
        private IEnumerator SpawnCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(GetCurrentSpawnInterval());
                
                if (CanSpawnNPC())
                {
                    SpawnRandomNPC();
                }
            }
        }

        /// <summary>
        /// Получить текущий интервал спавна с учетом времени дня и локации
        /// </summary>
        private float GetCurrentSpawnInterval()
        {
            float timeOfDayMultiplier = GetTimeOfDayPopulationMultiplier();
            float baseInterval = GetLocationModifiedSpawnInterval();
            return baseInterval / (baseSpawnRate * timeOfDayMultiplier);
        }

        /// <summary>
        /// Проверить можно ли спавнить NPC
        /// </summary>
        private bool CanSpawnNPC()
        {
            // Проверить лимит
            if (activeNPCs.Count >= maxNPCCount)
                return false;

            // Проверить наличие игрока
            if (playerTransform == null)
                return false;

            // Проверить состояние игры
            if (GameManager.Instance != null && GameManager.Instance.CurrentGameState != GameState.Playing)
                return false;

            return true;
        }

        /// <summary>
        /// Спавнить случайного NPC
        /// </summary>
        private void SpawnRandomNPC()
        {
            if (npcDatabase == null || npcPrefab == null)
                return;

            // Получить случайный тип NPC с учетом локации
            NPCType selectedNPCType = GetRandomNPCTypeForLocation();
            NPCData npcData = npcDatabase.GetNPCData(selectedNPCType);
            if (npcData == null)
            {
                // Fallback: попробовать получить любые данные NPC
                npcData = npcDatabase.GetRandomNPCData(currentTimeOfDay);
                if (npcData == null)
                    return;
            }

            // Найти позицию спавна
            Vector2 spawnPosition = GetRandomSpawnPosition();
            Vector2 moveDirection = GetRandomMoveDirection(spawnPosition);

            // Создать NPC
            GameObject npcObject = Instantiate(npcPrefab, spawnPosition, Quaternion.identity, npcParent);
            NPCController npcController = npcObject.GetComponent<NPCController>();
            
            if (npcController != null)
            {
                npcController.Initialize(npcData, moveDirection);
                npcController.OnNPCDespawn += OnNPCDespawn;
                npcController.OnMoneyGiven += OnMoneyGivenByNPC;
                
                activeNPCs.Add(npcController);
                totalNPCsSpawned++;
                
                OnNPCSpawned?.Invoke(npcController);
                
                Debug.Log($"Спавнен NPC {npcData.Name} в позиции {spawnPosition}");
            }
            else
            {
                Debug.LogError("NPCManager: Префаб NPC не содержит компонент NPCController!");
                Destroy(npcObject);
            }
        }

        /// <summary>
        /// Получить случайную позицию спавна
        /// </summary>
        private Vector2 GetRandomSpawnPosition()
        {
            if (playerTransform == null)
                return Vector2.zero;

            Vector2 playerPos = playerTransform.position;
            
            // Определить границы спавна
            Vector2 spawnBounds = useScreenBounds ? GetScreenBounds() : mapBounds;
            
            Vector2 spawnPosition;
            int attempts = 0;
            const int maxAttempts = 10;
            
            do
            {
                // Случайный угол
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(spawnRadius * 0.8f, spawnRadius);
                
                spawnPosition = playerPos + new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
                
                attempts++;
            }
            while (!IsPositionValid(spawnPosition, spawnBounds) && attempts < maxAttempts);
            
            return spawnPosition;
        }

        /// <summary>
        /// Получить границы экрана в мировых координатах
        /// </summary>
        private Vector2 GetScreenBounds()
        {
            if (mainCamera == null)
                return mapBounds;

            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            
            return new Vector2(width, height);
        }

        /// <summary>
        /// Проверить валидность позиции
        /// </summary>
        private bool IsPositionValid(Vector2 position, Vector2 bounds)
        {
            Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
            Vector2 minBounds = playerPos - bounds * 0.6f;
            Vector2 maxBounds = playerPos + bounds * 0.6f;
            
            return position.x >= minBounds.x && position.x <= maxBounds.x &&
                   position.y >= minBounds.y && position.y <= maxBounds.y;
        }

        /// <summary>
        /// Получить случайное направление движения
        /// </summary>
        private Vector2 GetRandomMoveDirection(Vector2 spawnPosition)
        {
            if (playerTransform == null)
                return Vector2.right;

            Vector2 playerPos = playerTransform.position;
            Vector2 toPlayer = (playerPos - spawnPosition).normalized;
            
            // 70% шанс двигаться к игроку, 30% - в случайном направлении
            if (Random.Range(0f, 1f) < 0.7f)
            {
                // Направление к игроку с небольшим отклонением
                float angleOffset = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angleOffset);
                float sin = Mathf.Sin(angleOffset);
                
                return new Vector2(
                    toPlayer.x * cos - toPlayer.y * sin,
                    toPlayer.x * sin + toPlayer.y * cos
                );
            }
            else
            {
                // Случайное направление
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            }
        }

        #endregion

        #region Управление жизненным циклом NPC

        /// <summary>
        /// Удалить далекие NPC
        /// </summary>
        private void CleanupDistantNPCs()
        {
            if (playerTransform == null)
                return;

            for (int i = activeNPCs.Count - 1; i >= 0; i--)
            {
                if (activeNPCs[i] == null)
                {
                    activeNPCs.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(activeNPCs[i].transform.position, playerTransform.position);
                if (distance > despawnDistance)
                {
                    activeNPCs[i].DespawnNPC();
                }
            }
        }

        /// <summary>
        /// Обработчик исчезновения NPC
        /// </summary>
        private void OnNPCDespawn(NPCController npc)
        {
            if (activeNPCs.Contains(npc))
            {
                activeNPCs.Remove(npc);
                OnNPCDespawned?.Invoke(npc);
            }
        }

        /// <summary>
        /// Обработчик выдачи денег NPC
        /// </summary>
        private void OnMoneyGivenByNPC(NPCController npc, float amount)
        {
            totalMoneyEarned += amount;
            OnTotalMoneyEarned?.Invoke(totalMoneyEarned);
        }

        /// <summary>
        /// Удалить всех NPC
        /// </summary>
        public void DespawnAllNPCs()
        {
            for (int i = activeNPCs.Count - 1; i >= 0; i--)
            {
                if (activeNPCs[i] != null)
                {
                    activeNPCs[i].DespawnNPC();
                }
            }
            activeNPCs.Clear();
        }

        #endregion

        #region Время дня

        /// <summary>
        /// Обновить время дня
        /// </summary>
        private void UpdateTimeOfDay()
        {
            if (!autoChangeTimeOfDay)
                return;

            timeOfDayTimer += Time.deltaTime;
            
            if (timeOfDayTimer >= timeOfDayDuration)
            {
                ChangeTimeOfDay();
                timeOfDayTimer = 0f;
            }
        }

        /// <summary>
        /// Сменить время дня
        /// </summary>
        private void ChangeTimeOfDay()
        {
            TimeOfDay newTimeOfDay = GetNextTimeOfDay(currentTimeOfDay);
            SetTimeOfDay(newTimeOfDay);
        }

        /// <summary>
        /// Получить следующее время дня
        /// </summary>
        private TimeOfDay GetNextTimeOfDay(TimeOfDay current)
        {
            switch (current)
            {
                case TimeOfDay.Morning:
                    return TimeOfDay.Day;
                case TimeOfDay.Day:
                    return TimeOfDay.Evening;
                case TimeOfDay.Evening:
                    return TimeOfDay.Night;
                case TimeOfDay.Night:
                    return TimeOfDay.Morning;
                default:
                    return TimeOfDay.Day;
            }
        }

        /// <summary>
        /// Установить время дня
        /// </summary>
        public void SetTimeOfDay(TimeOfDay newTimeOfDay)
        {
            if (currentTimeOfDay != newTimeOfDay)
            {
                currentTimeOfDay = newTimeOfDay;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
                
                Debug.Log($"Время дня изменено на: {currentTimeOfDay}");
            }
        }

        /// <summary>
        /// Получить множитель популяции для текущего времени дня
        /// </summary>
        private float GetTimeOfDayPopulationMultiplier()
        {
            float normalizedTime = GetNormalizedTimeOfDay();
            return populationByTime.Evaluate(normalizedTime);
        }

        /// <summary>
        /// Получить нормализованное время дня (0-1)
        /// </summary>
        private float GetNormalizedTimeOfDay()
        {
            switch (currentTimeOfDay)
            {
                case TimeOfDay.Morning:
                    return 0.25f;
                case TimeOfDay.Day:
                    return 0.75f;
                case TimeOfDay.Evening:
                    return 0.5f;
                case TimeOfDay.Night:
                    return 0f;
                default:
                    return 0.5f;
            }
        }

        #endregion

        #region Статистика

        /// <summary>
        /// Обновить статистику NPC
        /// </summary>
        private void UpdateNPCStatistics()
        {
            // Можно добавить дополнительную логику статистики
        }

        /// <summary>
        /// Получить статистику NPC
        /// </summary>
        public NPCStatistics GetStatistics()
        {
            return new NPCStatistics
            {
                activeNPCCount = activeNPCs.Count,
                totalNPCsSpawned = totalNPCsSpawned,
                totalMoneyEarned = totalMoneyEarned,
                currentTimeOfDay = currentTimeOfDay,
                timeOfDayProgress = timeOfDayTimer / timeOfDayDuration
            };
        }

        #endregion

        #region События игры

        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDeath()
        {
            StopSpawning();
            DespawnAllNPCs();
        }

        #endregion

        #region Интеграция с локациями

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType newLocation, LocationData locationData)
        {
            currentLocationData = locationData;
            UpdateCurrentLocationData();
            
            Debug.Log($"NPCManager: Локация изменена на {newLocation}");
        }

        /// <summary>
        /// Обновить данные текущей локации
        /// </summary>
        private void UpdateCurrentLocationData()
        {
            if (!useLocationSystem || locationManager == null)
            {
                currentAvailableNPCTypes = System.Enum.GetValues(typeof(NPCType)) as NPCType[];
                return;
            }

            currentLocationData = locationManager.GetCurrentLocationData();
            
            if (currentLocationData != null && currentLocationData.availableNPCTypes != null && currentLocationData.availableNPCTypes.Length > 0)
            {
                currentAvailableNPCTypes = currentLocationData.availableNPCTypes;
            }
            else
            {
                // Если в локации не указаны доступные NPC, используем всех
                currentAvailableNPCTypes = System.Enum.GetValues(typeof(NPCType)) as NPCType[];
            }
        }

        /// <summary>
        /// Обработчик смены локации (публичный метод для вызова извне)
        /// </summary>
        public void OnLocationChanged(LocationType newLocation)
        {
            if (useLocationSystem && locationManager != null)
            {
                currentLocationData = locationManager.GetCurrentLocationData();
                UpdateCurrentLocationData();
                
                // Перезапустить спавн с новыми параметрами локации
                if (spawnCoroutine != null)
                {
                    StopCoroutine(spawnCoroutine);
                    spawnCoroutine = StartCoroutine(SpawnCoroutine());
                }
            }
        }

        /// <summary>
        /// Получить модифицированную вероятность спавна для типа NPC в текущей локации
        /// </summary>
        private float GetLocationSpawnModifier(NPCType npcType)
        {
            if (!useLocationSystem || currentLocationData == null)
                return 1f;

            return currentLocationData.GetNPCSpawnModifier(npcType) * locationSpawnModifier;
        }

        /// <summary>
        /// Получить доступные типы NPC для текущей локации
        /// </summary>
        private NPCType[] GetAvailableNPCTypesForCurrentLocation()
        {
            if (!useLocationSystem || currentAvailableNPCTypes == null || currentAvailableNPCTypes.Length == 0)
            {
                // Возвращаем все типы NPC если система локаций не используется
                return System.Enum.GetValues(typeof(NPCType)) as NPCType[];
            }

            return currentAvailableNPCTypes;
        }

        /// <summary>
        /// Получить случайный тип NPC с учетом локации
        /// </summary>
        private NPCType GetRandomNPCTypeForLocation()
        {
            NPCType[] availableTypes = GetAvailableNPCTypesForCurrentLocation();
            
            if (availableTypes == null || availableTypes.Length == 0)
            {
                return NPCType.Poor; // Fallback
            }

            // Если используется система локаций, учитываем модификаторы спавна
            if (useLocationSystem && currentLocationData != null)
            {
                return GetWeightedRandomNPCType(availableTypes);
            }
            else
            {
                return availableTypes[Random.Range(0, availableTypes.Length)];
            }
        }

        /// <summary>
        /// Получить взвешенный случайный тип NPC с учетом модификаторов локации
        /// </summary>
        private NPCType GetWeightedRandomNPCType(NPCType[] availableTypes)
        {
            float totalWeight = 0f;
            Dictionary<NPCType, float> weights = new Dictionary<NPCType, float>();

            // Рассчитать веса для каждого типа NPC
            foreach (NPCType npcType in availableTypes)
            {
                float weight = GetLocationSpawnModifier(npcType);
                weights[npcType] = weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                return availableTypes[Random.Range(0, availableTypes.Length)];
            }

            // Выбрать случайный тип на основе весов
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }

            // Fallback
            return availableTypes[availableTypes.Length - 1];
        }

        /// <summary>
        /// Получить модифицированный интервал спавна для текущей локации
        /// </summary>
        private float GetLocationModifiedSpawnInterval()
        {
            if (!useLocationSystem || currentLocationData == null)
                return spawnInterval;

            // В локациях с высоким модификатором заработка обычно больше людей
            float locationModifier = currentLocationData.moneyModifier;
            float modifiedInterval = spawnInterval / Mathf.Sqrt(locationModifier); // Квадратный корень для плавного изменения
            
            return Mathf.Max(0.5f, modifiedInterval); // Минимальный интервал 0.5 секунды
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Получить всех активных NPC
        /// </summary>
        public List<NPCController> GetActiveNPCs()
        {
            return new List<NPCController>(activeNPCs);
        }

        /// <summary>
        /// Получить количество активных NPC
        /// </summary>
        public int GetActiveNPCCount()
        {
            return activeNPCs.Count;
        }

        /// <summary>
        /// Установить максимальное количество NPC
        /// </summary>
        public void SetMaxNPCCount(int count)
        {
            maxNPCCount = Mathf.Max(0, count);
        }

        /// <summary>
        /// Установить интервал спавна
        /// </summary>
        public void SetSpawnInterval(float interval)
        {
            spawnInterval = Mathf.Max(0.1f, interval);
        }

        /// <summary>
        /// Принудительно спавнить NPC определенного типа
        /// </summary>
        public void ForceSpawnNPC(NPCType npcType)
        {
            if (npcDatabase == null)
                return;

            NPCData npcData = npcDatabase.GetNPCData(npcType);
            if (npcData != null && CanSpawnNPC())
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();
                Vector2 moveDirection = GetRandomMoveDirection(spawnPosition);

                GameObject npcObject = Instantiate(npcPrefab, spawnPosition, Quaternion.identity, npcParent);
                NPCController npcController = npcObject.GetComponent<NPCController>();
                
                if (npcController != null)
                {
                    npcController.Initialize(npcData, moveDirection);
                    npcController.OnNPCDespawn += OnNPCDespawn;
                    npcController.OnMoneyGiven += OnMoneyGivenByNPC;
                    
                    activeNPCs.Add(npcController);
                    totalNPCsSpawned++;
                    
                    OnNPCSpawned?.Invoke(npcController);
                }
            }
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Отрисовка гизмо для отладки
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null)
                return;

            // Радиус спавна
            Gizmos.color = Color.green;
            Gizmos.DrawWireCircle(playerTransform.position, spawnRadius);
            
            // Радиус удаления
            Gizmos.color = Color.red;
            Gizmos.DrawWireCircle(playerTransform.position, despawnDistance);
            
            // Границы карты
            Vector2 bounds = useScreenBounds ? GetScreenBounds() : mapBounds;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(playerTransform.position, bounds);
        }

        #endregion
    }

    /// <summary>
    /// Статистика работы NPCManager
    /// </summary>
    [System.Serializable]
    public class NPCStatistics
    {
        public int activeNPCCount;
        public int totalNPCsSpawned;
        public float totalMoneyEarned;
        public TimeOfDay currentTimeOfDay;
        public float timeOfDayProgress;
    }
}