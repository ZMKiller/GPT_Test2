using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система оружия
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("Настройки системы оружия")]
        [SerializeField] private bool enableWeaponSystem = true;
        [SerializeField] private bool allowWeaponBreaking = true;
        [SerializeField] private bool allowConcealedCarry = true;
        [SerializeField] private float durabilityLossPerUse = 2f;

        [Header("Ограничения оружия")]
        [SerializeField] private int maxWeaponsCarried = 3;
        [SerializeField] private bool policeDetectWeapons = true;
        [SerializeField] private float detectionRadius = 5f;
        [SerializeField] private float concealmentSkillBonus = 0.1f;

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool unlimitedDurability = false;

        // Текущее состояние
        private List<Weapon> carriedWeapons = new List<Weapon>();
        private Weapon currentWeapon;
        private WeaponPlacement currentPlacement = WeaponPlacement.Hidden;
        private float lastMaintenanceTime = 0f;

        // Компоненты
        private PlayerStats playerStats;
        private SkillSystem skillSystem;
        private PoliceSystem policeSystem;
        private MoneySystem moneySystem;
        private LocationManager locationManager;

        // События
        public event Action<Weapon> OnWeaponEquipped;
        public event Action<Weapon> OnWeaponUnequipped;
        public event Action<Weapon, float> OnWeaponDurabilityChanged;
        public event Action<Weapon> OnWeaponBroken;
        public event Action<Weapon> OnWeaponFound;
        public event Action<WeaponType> OnWeaponDetected; // Полицией или охраной

        // Свойства
        public Weapon CurrentWeapon => currentWeapon;
        public List<Weapon> CarriedWeapons => new List<Weapon>(carriedWeapons);
        public WeaponPlacement CurrentPlacement => currentPlacement;
        public bool HasWeapon => currentWeapon != null;
        public bool IsArmed => HasWeapon && currentWeapon.weaponType != WeaponType.None;

        /// <summary>
        /// Получить текущее оружие
        /// </summary>
        public Weapon GetCurrentWeapon()
        {
            return currentWeapon;
        }

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            skillSystem = FindObjectOfType<SkillSystem>();
            policeSystem = FindObjectOfType<PoliceSystem>();
            moneySystem = FindObjectOfType<MoneySystem>();
            locationManager = FindObjectOfType<LocationManager>();
        }

        private void Start()
        {
            if (enableWeaponSystem)
            {
                InitializeWeaponSystem();
                SubscribeToEvents();
            }

            Debug.Log("WeaponSystem инициализирован");
        }

        private void Update()
        {
            if (enableWeaponSystem)
            {
                UpdateWeaponMaintenance();
                CheckWeaponDetection();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация системы оружия
        /// </summary>
        private void InitializeWeaponSystem()
        {
            // Добавить базовое "оружие" - кулаки
            var fists = Weapon.CreateFists();
            carriedWeapons.Add(fists);
            currentWeapon = fists;
            
            lastMaintenanceTime = Time.time;
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (locationManager != null)
            {
                locationManager.OnLocationChanged += OnLocationChanged;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (locationManager != null)
            {
                locationManager.OnLocationChanged -= OnLocationChanged;
            }
        }

        #endregion

        #region Weapon Management

        /// <summary>
        /// Экипировать оружие
        /// </summary>
        public bool EquipWeapon(Weapon weapon, WeaponPlacement placement = WeaponPlacement.Hidden)
        {
            if (weapon == null || !carriedWeapons.Contains(weapon))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("Попытка экипировать недоступное оружие");
                }
                return false;
            }

            // Снять текущее оружие
            if (currentWeapon != null)
            {
                OnWeaponUnequipped?.Invoke(currentWeapon);
            }

            currentWeapon = weapon;
            currentPlacement = placement;
            OnWeaponEquipped?.Invoke(weapon);

            if (showDebugInfo)
            {
                Debug.Log($"Экипировано оружие: {weapon.GetDisplayName()} ({placement})");
            }

            return true;
        }

        /// <summary>
        /// Снять оружие
        /// </summary>
        public void UnequipWeapon()
        {
            if (currentWeapon != null && currentWeapon.weaponType != WeaponType.None)
            {
                OnWeaponUnequipped?.Invoke(currentWeapon);
                
                // Вернуться к кулакам
                var fists = GetWeaponByType(WeaponType.None);
                if (fists != null)
                {
                    currentWeapon = fists;
                    currentPlacement = WeaponPlacement.Hidden;
                }

                if (showDebugInfo)
                {
                    Debug.Log("Оружие снято");
                }
            }
        }

        /// <summary>
        /// Добавить оружие в инвентарь
        /// </summary>
        public bool AddWeapon(Weapon weapon)
        {
            if (weapon == null)
                return false;

            if (carriedWeapons.Count >= maxWeaponsCarried)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Нельзя нести больше {maxWeaponsCarried} предметов оружия");
                }
                return false;
            }

            carriedWeapons.Add(weapon);
            OnWeaponFound?.Invoke(weapon);

            if (showDebugInfo)
            {
                Debug.Log($"Добавлено оружие: {weapon.GetDisplayName()}");
            }

            return true;
        }

        /// <summary>
        /// Удалить оружие из инвентаря
        /// </summary>
        public bool RemoveWeapon(Weapon weapon)
        {
            if (weapon == null || weapon.weaponType == WeaponType.None)
                return false;

            if (currentWeapon == weapon)
            {
                UnequipWeapon();
            }

            bool removed = carriedWeapons.Remove(weapon);
            
            if (removed && showDebugInfo)
            {
                Debug.Log($"Удалено оружие: {weapon.GetDisplayName()}");
            }

            return removed;
        }

        /// <summary>
        /// Найти случайное оружие
        /// </summary>
        public Weapon FindRandomWeapon(Location location)
        {
            WeaponType[] possibleWeapons = GetPossibleWeaponsForLocation(location);
            
            if (possibleWeapons.Length == 0)
                return null;

            WeaponType foundType = possibleWeapons[UnityEngine.Random.Range(0, possibleWeapons.Length)];
            Weapon foundWeapon = Weapon.CreateWeapon(foundType);
            
            // Случайное состояние найденного оружия
            foundWeapon.condition = (WeaponCondition)UnityEngine.Random.Range(1, 6); // Исключаем Broken
            foundWeapon.durability = UnityEngine.Random.Range(30f, 90f);

            if (showDebugInfo)
            {
                Debug.Log($"Найдено оружие: {foundWeapon.GetDisplayName()} в {location}");
            }

            return foundWeapon;
        }

        /// <summary>
        /// Получить возможные типы оружия для локации
        /// </summary>
        private WeaponType[] GetPossibleWeaponsForLocation(Location location)
        {
            switch (location)
            {
                case Location.Street:
                    return new[] { WeaponType.BrokenBottle, WeaponType.Chain, WeaponType.Knife };
                
                case Location.Park:
                    return new[] { WeaponType.Bat, WeaponType.Hammer };
                
                case Location.Shop:
                    return new[] { WeaponType.Knife, WeaponType.Hammer };
                
                case Location.BusinessDistrict:
                    return new[] { WeaponType.PepperSpray, WeaponType.Taser };
                
                case Location.Industrial:
                    return new[] { WeaponType.Hammer, WeaponType.Chain, WeaponType.Club };
                
                default:
                    return new WeaponType[0];
            }
        }

        #endregion

        #region Weapon Usage

        /// <summary>
        /// Использовать оружие (снижение прочности)
        /// </summary>
        public void UseWeapon()
        {
            if (currentWeapon == null || currentWeapon.weaponType == WeaponType.None)
                return;

            if (!unlimitedDurability && allowWeaponBreaking)
            {
                float durabilityLoss = durabilityLossPerUse;
                
                // Учесть навыки обращения с оружием
                if (skillSystem != null)
                {
                    float physicalFitness = skillSystem.GetSkillLevel(SkillType.Physical_Fitness);
                    durabilityLoss *= (1f - physicalFitness * 0.02f); // Снижение потери на 2% за уровень
                }

                currentWeapon.durability = Mathf.Max(0, currentWeapon.durability - durabilityLoss);
                OnWeaponDurabilityChanged?.Invoke(currentWeapon, currentWeapon.durability);

                // Проверить поломку
                if (currentWeapon.durability <= 0)
                {
                    BreakWeapon(currentWeapon);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Использовано оружие {currentWeapon.GetDisplayName()}. " +
                             $"Прочность: {currentWeapon.durability:F1}");
                }
            }
        }

        /// <summary>
        /// Сломать оружие
        /// </summary>
        private void BreakWeapon(Weapon weapon)
        {
            weapon.condition = WeaponCondition.Broken;
            weapon.durability = 0;
            OnWeaponBroken?.Invoke(weapon);

            if (currentWeapon == weapon)
            {
                UnequipWeapon();
            }

            if (showDebugInfo)
            {
                Debug.Log($"Оружие сломано: {weapon.GetDisplayName()}");
            }
        }

        /// <summary>
        /// Починить оружие
        /// </summary>
        public bool RepairWeapon(Weapon weapon, double cost)
        {
            if (weapon == null || weapon.condition != WeaponCondition.Broken)
                return false;

            if (moneySystem != null && !moneySystem.CanAfford(cost))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Недостаточно денег для ремонта. Нужно: ${cost}");
                }
                return false;
            }

            if (moneySystem != null)
            {
                moneySystem.SpendMoney(cost, $"Ремонт {weapon.GetDisplayName()}");
            }

            weapon.condition = WeaponCondition.Good;
            weapon.durability = 80f;

            if (showDebugInfo)
            {
                Debug.Log($"Оружие отремонтировано: {weapon.GetDisplayName()} за ${cost}");
            }

            return true;
        }

        #endregion

        #region Weapon Detection

        /// <summary>
        /// Проверить обнаружение оружия
        /// </summary>
        private void CheckWeaponDetection()
        {
            if (!policeDetectWeapons || !IsArmed || currentPlacement == WeaponPlacement.Hidden)
                return;

            // Проверить наличие полиции поблизости
            if (IsPoliceNearby())
            {
                float detectionChance = CalculateDetectionChance();
                
                if (UnityEngine.Random.value < detectionChance * Time.deltaTime)
                {
                    OnWeaponDetected?.Invoke(currentWeapon.weaponType);
                    
                    if (policeSystem != null)
                    {
                        policeSystem.ReportWeaponViolation(currentWeapon.weaponType);
                    }

                    if (showDebugInfo)
                    {
                        Debug.Log($"Полиция обнаружила оружие: {currentWeapon.GetDisplayName()}");
                    }
                }
            }
        }

        /// <summary>
        /// Проверить наличие полиции поблизости
        /// </summary>
        private bool IsPoliceNearby()
        {
            // TODO: Интеграция с NPCManager для проверки полицейских поблизости
            return locationManager != null && 
                   (locationManager.CurrentLocation == Location.BusinessDistrict || 
                    locationManager.CurrentLocation == Location.Shop);
        }

        /// <summary>
        /// Рассчитать шанс обнаружения оружия
        /// </summary>
        private float CalculateDetectionChance()
        {
            float baseChance = 0.1f; // 10% в секунду для видимого оружия

            switch (currentPlacement)
            {
                case WeaponPlacement.Visible:
                    baseChance = 0.5f;
                    break;
                case WeaponPlacement.InHand:
                    baseChance = 0.8f;
                    break;
                case WeaponPlacement.Holstered:
                    baseChance = 0.2f;
                    break;
                case WeaponPlacement.Concealed:
                    baseChance = 0.05f;
                    break;
                case WeaponPlacement.Hidden:
                    return 0f;
            }

            // Модификаторы от навыков
            if (skillSystem != null && allowConcealedCarry)
            {
                float luck = skillSystem.GetSkillLevel(SkillType.Luck);
                baseChance *= (1f - luck * concealmentSkillBonus);
            }

            // Модификатор от типа оружия
            switch (currentWeapon.weaponType)
            {
                case WeaponType.Pistol:
                    baseChance *= 1.5f; // Огнестрельное оружие легче заметить
                    break;
                case WeaponType.Knife:
                    baseChance *= 0.8f;
                    break;
                case WeaponType.PepperSpray:
                case WeaponType.Taser:
                    baseChance *= 0.6f; // Легальные средства самообороны
                    break;
            }

            return baseChance;
        }

        #endregion

        #region Weapon Maintenance

        /// <summary>
        /// Обновить техническое обслуживание оружия
        /// </summary>
        private void UpdateWeaponMaintenance()
        {
            if (Time.time - lastMaintenanceTime >= 60f) // Каждую минуту
            {
                PerformMaintenance();
                lastMaintenanceTime = Time.time;
            }
        }

        /// <summary>
        /// Выполнить техническое обслуживание
        /// </summary>
        private void PerformMaintenance()
        {
            foreach (var weapon in carriedWeapons)
            {
                if (weapon.weaponType == WeaponType.None)
                    continue;

                // Естественное ухудшение состояния
                if (weapon.condition > WeaponCondition.Broken)
                {
                    weapon.durability = Mathf.Max(0, weapon.durability - 0.5f);
                    
                    // Обновить состояние на основе прочности
                    weapon.UpdateConditionFromDurability();
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Получить оружие по типу
        /// </summary>
        public Weapon GetWeaponByType(WeaponType weaponType)
        {
            return carriedWeapons.Find(w => w.weaponType == weaponType);
        }

        /// <summary>
        /// Получить доступные типы оружия
        /// </summary>
        public List<WeaponType> GetAvailableWeaponTypes()
        {
            var types = new List<WeaponType>();
            foreach (var weapon in carriedWeapons)
            {
                if (weapon.condition != WeaponCondition.Broken)
                {
                    types.Add(weapon.weaponType);
                }
            }
            return types;
        }

        /// <summary>
        /// Может ли игрок нести больше оружия
        /// </summary>
        public bool CanCarryMoreWeapons()
        {
            return carriedWeapons.Count < maxWeaponsCarried;
        }

        /// <summary>
        /// Получить общий вес оружия
        /// </summary>
        public float GetTotalWeaponWeight()
        {
            float totalWeight = 0f;
            foreach (var weapon in carriedWeapons)
            {
                totalWeight += weapon.weight;
            }
            return totalWeight;
        }

        /// <summary>
        /// Изменить размещение текущего оружия
        /// </summary>
        public void SetWeaponPlacement(WeaponPlacement placement)
        {
            if (IsArmed)
            {
                WeaponPlacement oldPlacement = currentPlacement;
                currentPlacement = placement;

                if (showDebugInfo && oldPlacement != placement)
                {
                    Debug.Log($"Изменено размещение оружия: {oldPlacement} -> {placement}");
                }
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public WeaponSystemSaveData GetSaveData()
        {
            return new WeaponSystemSaveData
            {
                carriedWeapons = new List<Weapon>(carriedWeapons),
                currentWeaponType = currentWeapon?.weaponType ?? WeaponType.None,
                currentPlacement = currentPlacement,
                lastMaintenanceTime = lastMaintenanceTime
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(WeaponSystemSaveData data)
        {
            carriedWeapons = data.carriedWeapons ?? new List<Weapon>();
            currentPlacement = data.currentPlacement;
            lastMaintenanceTime = data.lastMaintenanceTime;

            // Найти текущее оружие
            currentWeapon = GetWeaponByType(data.currentWeaponType);
            if (currentWeapon == null && carriedWeapons.Count > 0)
            {
                currentWeapon = carriedWeapons[0]; // Fallback к первому оружию
            }

            if (showDebugInfo)
            {
                Debug.Log($"WeaponSystem загружен. Оружия: {carriedWeapons.Count}, " +
                         $"Текущее: {currentWeapon?.GetDisplayName() ?? "Нет"}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType oldLocation, LocationType newLocation)
        {
            // В некоторых локациях нужно скрывать оружие
            if (IsArmed && IsRestrictedLocation((Location)newLocation))
            {
                if (currentPlacement == WeaponPlacement.Visible || 
                    currentPlacement == WeaponPlacement.InHand)
                {
                    SetWeaponPlacement(WeaponPlacement.Hidden);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Оружие автоматически скрыто в {newLocation}");
                    }
                }
            }
        }

        /// <summary>
        /// Проверить, является ли локация ограниченной для оружия
        /// </summary>
        private bool IsRestrictedLocation(Location location)
        {
            switch (location)
            {
                case Location.Hospital:
                case Location.University:
                case Location.Library:
                case Location.JobCenter:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Add Random Weapon")]
        private void DebugAddRandomWeapon()
        {
            WeaponType[] types = { WeaponType.Knife, WeaponType.Bat, WeaponType.Club, WeaponType.PepperSpray };
            WeaponType randomType = types[UnityEngine.Random.Range(0, types.Length)];
            Weapon randomWeapon = Weapon.CreateWeapon(randomType);
            AddWeapon(randomWeapon);
        }

        [ContextMenu("Break Current Weapon")]
        private void DebugBreakCurrentWeapon()
        {
            if (IsArmed && currentWeapon.weaponType != WeaponType.None)
            {
                BreakWeapon(currentWeapon);
            }
        }

        [ContextMenu("Show Weapon Info")]
        private void DebugShowWeaponInfo()
        {
            Debug.Log($"Текущее оружие: {currentWeapon?.GetDisplayName() ?? "Нет"}");
            Debug.Log($"Размещение: {currentPlacement}");
            Debug.Log($"Всего оружия: {carriedWeapons.Count}/{maxWeaponsCarried}");
            Debug.Log($"Общий вес: {GetTotalWeaponWeight():F1} кг");
            
            foreach (var weapon in carriedWeapons)
            {
                Debug.Log($"- {weapon.GetDisplayName()}: {weapon.condition} ({weapon.durability:F0}%)");
            }
        }

        [ContextMenu("Test Weapon Detection")]
        private void DebugTestWeaponDetection()
        {
            if (IsArmed)
            {
                OnWeaponDetected?.Invoke(currentWeapon.weaponType);
            }
        }

        #endregion
    }

    /// <summary>
    /// Данные WeaponSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class WeaponSystemSaveData
    {
        public List<Weapon> carriedWeapons;
        public WeaponType currentWeaponType;
        public WeaponPlacement currentPlacement;
        public float lastMaintenanceTime;
    }
}
