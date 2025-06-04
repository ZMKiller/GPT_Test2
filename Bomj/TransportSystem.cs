using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система транспорта
    /// </summary>
    public class TransportSystem : MonoBehaviour
    {
        [Header("Настройки системы транспорта")]
        [SerializeField] private bool enableTransportSystem = true;
        [SerializeField] private float maintenanceInterval = 300f;     // Интервал обслуживания (секунды)
        [SerializeField] private float fuelConsumptionRate = 1f;       // Базовый расход топлива
        [SerializeField] private bool allowTransportTheft = true;      // Разрешить кражу транспорта

        [Header("Экономические настройки")]
        [SerializeField] private double dailyParkingCost = 5;          // Стоимость парковки в день
        [SerializeField] private double maintenanceBaseCost = 20;      // Базовая стоимость обслуживания
        [SerializeField] private double insuranceBaseCost = 10;        // Базовая стоимость страховки
        [SerializeField] private float publicTransportCostPerKm = 0.5f; // Стоимость общ. транспорта

        [Header("Модификаторы")]
        [SerializeField] private float speedMultiplierBase = 1f;       // Базовый множитель скорости
        [SerializeField] private float imageBonus = 10f;               // Бонус к имиджу от транспорта
        [SerializeField] private float comfortModifier = 5f;           // Влияние на комфорт

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool unlimitedFuel = false;
        [SerializeField] private bool freeMaintenance = false;

        // Состояние системы
        private Dictionary<TransportType, Vehicle> ownedVehicles = new Dictionary<TransportType, Vehicle>();
        private TransportType currentTransport = TransportType.None;
        private float lastMaintenanceTime = 0f;
        private double totalDistanceTraveled = 0.0;
        private Dictionary<TransportType, float> vehicleUsageTime = new Dictionary<TransportType, float>();

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;
        private NotificationSystem notificationSystem;
        private AchievementSystem achievementSystem;

        // События
        public event Action<TransportType> OnTransportChanged;
        public event Action<Vehicle> OnVehicleAcquired;
        public event Action<Vehicle> OnVehicleLost;
        public event Action<Vehicle, float> OnVehicleConditionChanged;
        public event Action<TransportType, double> OnMaintenancePaid;
        public event Action<TransportType> OnVehicleBrokenDown;

        // Свойства
        public TransportType CurrentTransport => currentTransport;
        public bool HasTransport => currentTransport != TransportType.None;
        public Vehicle CurrentVehicle => ownedVehicles.ContainsKey(currentTransport) ? ownedVehicles[currentTransport] : null;
        public Dictionary<TransportType, Vehicle> OwnedVehicles => new Dictionary<TransportType, Vehicle>(ownedVehicles);
        public float SpeedMultiplier => CalculateSpeedMultiplier();

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            locationManager = FindObjectOfType<LocationManager>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
            achievementSystem = FindObjectOfType<AchievementSystem>();
        }

        private void Start()
        {
            if (enableTransportSystem)
            {
                InitializeTransportSystem();
                SubscribeToEvents();
            }

            Debug.Log("TransportSystem инициализирован");
        }

        private void Update()
        {
            if (enableTransportSystem)
            {
                UpdateTransportMaintenance();
                UpdateFuelConsumption();
                UpdateUsageTime();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация системы транспорта
        /// </summary>
        private void InitializeTransportSystem()
        {
            lastMaintenanceTime = Time.time;
            
            // Инициализировать время использования
            foreach (TransportType type in Enum.GetValues(typeof(TransportType)))
            {
                vehicleUsageTime[type] = 0f;
            }
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

        #region Vehicle Management

        /// <summary>
        /// Купить транспорт
        /// </summary>
        public bool PurchaseVehicle(TransportType type, double price)
        {
            if (ownedVehicles.ContainsKey(type))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Транспорт {type} уже есть в собственности");
                }
                return false;
            }

            if (moneySystem == null || !moneySystem.CanAfford(price))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Недостаточно денег для покупки {type}: ${price}");
                }
                return false;
            }

            moneySystem.SpendMoney(price, $"Покупка {GetTransportName(type)}");

            var vehicle = CreateVehicle(type, TransportAcquisitionType.Purchase);
            ownedVehicles[type] = vehicle;

            OnVehicleAcquired?.Invoke(vehicle);

            if (showDebugInfo)
            {
                Debug.Log($"Куплен транспорт: {type} за ${price}");
            }

            return true;
        }

        /// <summary>
        /// Арендовать транспорт
        /// </summary>
        public bool RentVehicle(TransportType type, double pricePerHour, float hours)
        {
            double totalCost = pricePerHour * hours;

            if (moneySystem == null || !moneySystem.CanAfford(totalCost))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Недостаточно денег для аренды {type}: ${totalCost}");
                }
                return false;
            }

            moneySystem.SpendMoney(totalCost, $"Аренда {GetTransportName(type)}");

            var vehicle = CreateVehicle(type, TransportAcquisitionType.Rent);
            vehicle.isRented = true;
            vehicle.rentExpirationTime = Time.time + (hours * 3600f);

            ownedVehicles[type] = vehicle;
            OnVehicleAcquired?.Invoke(vehicle);

            if (showDebugInfo)
            {
                Debug.Log($"Арендован транспорт: {type} на {hours} часов за ${totalCost}");
            }

            return true;
        }

        /// <summary>
        /// Продать транспорт
        /// </summary>
        public bool SellVehicle(TransportType type)
        {
            if (!ownedVehicles.ContainsKey(type))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Транспорт {type} не найден в собственности");
                }
                return false;
            }

            var vehicle = ownedVehicles[type];
            if (vehicle.isRented)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Нельзя продать арендованный транспорт {type}");
                }
                return false;
            }

            double sellPrice = CalculateSellPrice(vehicle);
            
            if (moneySystem != null)
            {
                moneySystem.AddMoney(sellPrice, $"Продажа {GetTransportName(type)}");
            }

            // Если использовался проданный транспорт
            if (currentTransport == type)
            {
                ChangeTransport(TransportType.None);
            }

            OnVehicleLost?.Invoke(vehicle);
            ownedVehicles.Remove(type);

            if (showDebugInfo)
            {
                Debug.Log($"Продан транспорт: {type} за ${sellPrice}");
            }

            return true;
        }

        /// <summary>
        /// Изменить транспорт
        /// </summary>
        public bool ChangeTransport(TransportType newTransport)
        {
            if (newTransport != TransportType.None && !ownedVehicles.ContainsKey(newTransport))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Транспорт {newTransport} не найден в собственности");
                }
                return false;
            }

            // Проверить состояние транспорта
            if (newTransport != TransportType.None)
            {
                var vehicle = ownedVehicles[newTransport];
                if (vehicle.condition == TransportCondition.Broken)
                {
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Поломка", 
                            $"{GetTransportName(newTransport)} сломан и не может быть использован", 
                            NotificationType.Warning);
                    }
                    return false;
                }

                // Проверить топливо
                if (vehicle.fuelLevel <= 0 && RequiresFuel(newTransport))
                {
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Нет топлива", 
                            $"У {GetTransportName(newTransport)} закончилось топливо", 
                            NotificationType.Warning);
                    }
                    return false;
                }
            }

            TransportType oldTransport = currentTransport;
            currentTransport = newTransport;

            OnTransportChanged?.Invoke(newTransport);
            ApplyTransportEffects(oldTransport, newTransport);

            if (showDebugInfo)
            {
                Debug.Log($"Транспорт изменен: {oldTransport} -> {newTransport}");
            }

            return true;
        }

        /// <summary>
        /// Создать транспорт
        /// </summary>
        private Vehicle CreateVehicle(TransportType type, TransportAcquisitionType acquisitionType)
        {
            var vehicle = new Vehicle
            {
                type = type,
                acquisitionType = acquisitionType,
                purchaseTime = Time.time,
                condition = TransportCondition.Good,
                fuelLevel = GetMaxFuelCapacity(type),
                maxFuelCapacity = GetMaxFuelCapacity(type),
                odometer = 0f,
                isRented = false,
                rentExpirationTime = 0f
            };

            // Случайное состояние для найденного/украденного транспорта
            if (acquisitionType == TransportAcquisitionType.Found || acquisitionType == TransportAcquisitionType.Theft)
            {
                vehicle.condition = (TransportCondition)UnityEngine.Random.Range(1, 5); // Исключаем Broken
                vehicle.fuelLevel = UnityEngine.Random.Range(0.1f, 0.8f) * vehicle.maxFuelCapacity;
            }

            return vehicle;
        }

        #endregion

        #region Maintenance and Fuel

        /// <summary>
        /// Обновить техническое обслуживание
        /// </summary>
        private void UpdateTransportMaintenance()
        {
            if (Time.time - lastMaintenanceTime < maintenanceInterval)
                return;

            lastMaintenanceTime = Time.time;

            // Обновить состояние всех транспортных средств
            var vehiclesToUpdate = new List<TransportType>(ownedVehicles.Keys);
            foreach (var type in vehiclesToUpdate)
            {
                UpdateVehicleCondition(type);
                CheckRentExpiration(type);
            }
        }

        /// <summary>
        /// Обновить состояние транспорта
        /// </summary>
        private void UpdateVehicleCondition(TransportType type)
        {
            if (!ownedVehicles.ContainsKey(type))
                return;

            var vehicle = ownedVehicles[type];
            float degradationRate = CalculateDegradationRate(vehicle);

            // Естественное ухудшение состояния
            float conditionLoss = degradationRate * (maintenanceInterval / 3600f); // За час
            vehicle.conditionValue = Mathf.Max(0, vehicle.conditionValue - conditionLoss);

            // Обновить состояние на основе значения
            var oldCondition = vehicle.condition;
            vehicle.UpdateConditionFromValue();

            if (oldCondition != vehicle.condition)
            {
                OnVehicleConditionChanged?.Invoke(vehicle, vehicle.conditionValue);

                if (vehicle.condition == TransportCondition.Broken)
                {
                    OnVehicleBrokenDown?.Invoke(type);
                    
                    if (currentTransport == type)
                    {
                        ChangeTransport(TransportType.None);
                    }

                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Поломка", 
                            $"{GetTransportName(type)} сломался!", NotificationType.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// Проверить истечение аренды
        /// </summary>
        private void CheckRentExpiration(TransportType type)
        {
            if (!ownedVehicles.ContainsKey(type))
                return;

            var vehicle = ownedVehicles[type];
            if (!vehicle.isRented)
                return;

            if (Time.time >= vehicle.rentExpirationTime)
            {
                // Аренда истекла
                if (currentTransport == type)
                {
                    ChangeTransport(TransportType.None);
                }

                OnVehicleLost?.Invoke(vehicle);
                ownedVehicles.Remove(type);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Аренда истекла", 
                        $"Аренда {GetTransportName(type)} истекла", NotificationType.Info);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Аренда истекла: {type}");
                }
            }
        }

        /// <summary>
        /// Обновить расход топлива
        /// </summary>
        private void UpdateFuelConsumption()
        {
            if (unlimitedFuel || currentTransport == TransportType.None)
                return;

            if (!RequiresFuel(currentTransport))
                return;

            if (!ownedVehicles.ContainsKey(currentTransport))
                return;

            var vehicle = ownedVehicles[currentTransport];
            float consumption = CalculateFuelConsumption(vehicle) * Time.deltaTime;

            vehicle.fuelLevel = Mathf.Max(0, vehicle.fuelLevel - consumption);

            // Если топливо закончилось
            if (vehicle.fuelLevel <= 0)
            {
                ChangeTransport(TransportType.None);
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Топливо закончилось", 
                        $"У {GetTransportName(currentTransport)} закончилось топливо", 
                        NotificationType.Warning);
                }
            }
        }

        /// <summary>
        /// Обновить время использования
        /// </summary>
        private void UpdateUsageTime()
        {
            if (currentTransport != TransportType.None)
            {
                vehicleUsageTime[currentTransport] += Time.deltaTime;
            }
        }

        /// <summary>
        /// Заправить транспорт
        /// </summary>
        public bool RefuelVehicle(TransportType type, float amount)
        {
            if (!ownedVehicles.ContainsKey(type))
                return false;

            var vehicle = ownedVehicles[type];
            double cost = CalculateFuelCost(type, amount);

            if (moneySystem == null || !moneySystem.CanAfford(cost))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Недостаточно денег для заправки: ${cost}");
                }
                return false;
            }

            moneySystem.SpendMoney(cost, $"Заправка {GetTransportName(type)}");

            float actualAmount = Mathf.Min(amount, vehicle.maxFuelCapacity - vehicle.fuelLevel);
            vehicle.fuelLevel += actualAmount;

            if (showDebugInfo)
            {
                Debug.Log($"Заправлен {type}: {actualAmount} литров за ${cost}");
            }

            return true;
        }

        /// <summary>
        /// Починить транспорт
        /// </summary>
        public bool RepairVehicle(TransportType type)
        {
            if (!ownedVehicles.ContainsKey(type))
                return false;

            var vehicle = ownedVehicles[type];
            double cost = CalculateRepairCost(vehicle);

            if (freeMaintenance)
                cost = 0;

            if (moneySystem == null || !moneySystem.CanAfford(cost))
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Недостаточно денег для ремонта: ${cost}");
                }
                return false;
            }

            if (cost > 0)
            {
                moneySystem.SpendMoney(cost, $"Ремонт {GetTransportName(type)}");
            }

            vehicle.condition = TransportCondition.Good;
            vehicle.conditionValue = 75f;

            OnMaintenancePaid?.Invoke(type, cost);

            if (showDebugInfo)
            {
                Debug.Log($"Отремонтирован {type} за ${cost}");
            }

            return true;
        }

        #endregion

        #region Transport Effects

        /// <summary>
        /// Применить эффекты транспорта
        /// </summary>
        private void ApplyTransportEffects(TransportType oldTransport, TransportType newTransport)
        {
            if (playerStats == null)
                return;

            // Удалить старые эффекты
            if (oldTransport != TransportType.None)
            {
                RemoveTransportModifiers(oldTransport);
            }

            // Применить новые эффекты
            if (newTransport != TransportType.None)
            {
                ApplyTransportModifiers(newTransport);
            }
        }

        /// <summary>
        /// Применить модификаторы транспорта
        /// </summary>
        private void ApplyTransportModifiers(TransportType type)
        {
            var modifiers = GetTransportModifiers(type);
            
            foreach (var modifier in modifiers)
            {
                playerStats.AddStatModifier(modifier);
            }
        }

        /// <summary>
        /// Удалить модификаторы транспорта
        /// </summary>
        private void RemoveTransportModifiers(TransportType type)
        {
            playerStats.RemoveStatModifiersBySource($"Транспорт: {GetTransportName(type)}");
        }

        /// <summary>
        /// Получить модификаторы транспорта
        /// </summary>
        private List<StatModifier> GetTransportModifiers(TransportType type)
        {
            var modifiers = new List<StatModifier>();
            string sourceName = $"Транспорт: {GetTransportName(type)}";

            switch (type)
            {
                case TransportType.Bicycle:
                    modifiers.Add(new StatModifier(StatType.MoodRegen, 2f, ModifierOperation.Add, sourceName));
                    modifiers.Add(new StatModifier(StatType.HealthRegen, 1f, ModifierOperation.Add, sourceName));
                    break;

                case TransportType.Motorcycle:
                    modifiers.Add(new StatModifier(StatType.MoodRegen, 5f, ModifierOperation.Add, sourceName));
                    modifiers.Add(new StatModifier(StatType.CharismaBonus, 3f, ModifierOperation.Add, sourceName));
                    break;

                case TransportType.Car:
                    modifiers.Add(new StatModifier(StatType.CharismaBonus, 10f, ModifierOperation.Add, sourceName));
                    modifiers.Add(new StatModifier(StatType.BusinessSkillBonus, 5f, ModifierOperation.Add, sourceName));
                    modifiers.Add(new StatModifier(StatType.MoodRegen, 3f, ModifierOperation.Add, sourceName));
                    break;

                case TransportType.PublicTransport:
                    modifiers.Add(new StatModifier(StatType.MoodRegen, -1f, ModifierOperation.Add, sourceName));
                    break;
            }

            return modifiers;
        }

        #endregion

        #region Calculations

        /// <summary>
        /// Рассчитать множитель скорости
        /// </summary>
        private float CalculateSpeedMultiplier()
        {
            switch (currentTransport)
            {
                case TransportType.None: return 1f;
                case TransportType.Bicycle: return 2f;
                case TransportType.Motorcycle: return 4f;
                case TransportType.Car: return 5f;
                case TransportType.PublicTransport: return 3f;
                case TransportType.Taxi: return 4f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Рассчитать скорость деградации
        /// </summary>
        private float CalculateDegradationRate(Vehicle vehicle)
        {
            float baseRate = 2f; // 2% в час при использовании

            // Увеличить скорость для старых транспортных средств
            float age = (Time.time - vehicle.purchaseTime) / 86400f; // Возраст в днях
            baseRate += age * 0.1f;

            // Увеличить для часто используемого транспорта
            float usage = vehicleUsageTime[vehicle.type] / 3600f; // Часы использования
            baseRate += usage * 0.01f;

            return baseRate;
        }

        /// <summary>
        /// Рассчитать расход топлива
        /// </summary>
        private float CalculateFuelConsumption(Vehicle vehicle)
        {
            float baseConsumption = GetBaseFuelConsumption(vehicle.type);
            
            // Увеличить расход для поврежденного транспорта
            float conditionMultiplier = 1f + (1f - vehicle.GetConditionMultiplier()) * 0.5f;
            
            return baseConsumption * conditionMultiplier * fuelConsumptionRate;
        }

        /// <summary>
        /// Рассчитать стоимость топлива
        /// </summary>
        private double CalculateFuelCost(TransportType type, float amount)
        {
            float pricePerLiter = GetFuelPrice(type);
            return amount * pricePerLiter;
        }

        /// <summary>
        /// Рассчитать стоимость ремонта
        /// </summary>
        private double CalculateRepairCost(Vehicle vehicle)
        {
            double baseCost = maintenanceBaseCost;
            
            // Увеличить стоимость в зависимости от степени повреждения
            float damageMultiplier = 1f + (1f - vehicle.GetConditionMultiplier()) * 2f;
            
            // Увеличить стоимость для дорогих транспортных средств
            double vehicleValue = GetVehicleBasePrice(vehicle.type);
            double valueMultiplier = 1.0 + (vehicleValue / 10000.0) * 0.5;
            
            return baseCost * damageMultiplier * valueMultiplier;
        }

        /// <summary>
        /// Рассчитать цену продажи
        /// </summary>
        private double CalculateSellPrice(Vehicle vehicle)
        {
            double basePrice = GetVehicleBasePrice(vehicle.type);
            
            // Учесть состояние
            double conditionMultiplier = vehicle.GetConditionMultiplier();
            
            // Учесть возраст
            float age = (Time.time - vehicle.purchaseTime) / 86400f; // Дни
            double ageMultiplier = Mathf.Max(0.1f, 1f - age * 0.02f); // -2% в день
            
            return basePrice * conditionMultiplier * ageMultiplier * 0.7; // 70% от стоимости
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Получить название транспорта
        /// </summary>
        private string GetTransportName(TransportType type)
        {
            switch (type)
            {
                case TransportType.None: return "Пешком";
                case TransportType.Bicycle: return "Велосипед";
                case TransportType.Motorcycle: return "Мотоцикл";
                case TransportType.Car: return "Машина";
                case TransportType.PublicTransport: return "Общественный транспорт";
                case TransportType.Taxi: return "Такси";
                case TransportType.Train: return "Поезд";
                case TransportType.Bus: return "Автобус";
                default: return type.ToString();
            }
        }

        /// <summary>
        /// Проверить, требует ли транспорт топливо
        /// </summary>
        private bool RequiresFuel(TransportType type)
        {
            switch (type)
            {
                case TransportType.Motorcycle:
                case TransportType.Car:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Получить максимальную емкость топливного бака
        /// </summary>
        private float GetMaxFuelCapacity(TransportType type)
        {
            switch (type)
            {
                case TransportType.Motorcycle: return 15f;
                case TransportType.Car: return 50f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Получить базовый расход топлива
        /// </summary>
        private float GetBaseFuelConsumption(TransportType type)
        {
            switch (type)
            {
                case TransportType.Motorcycle: return 0.003f; // л/сек
                case TransportType.Car: return 0.008f; // л/сек
                default: return 0f;
            }
        }

        /// <summary>
        /// Получить цену топлива
        /// </summary>
        private float GetFuelPrice(TransportType type)
        {
            switch (type)
            {
                case TransportType.Motorcycle: return 1.2f; // $/литр
                case TransportType.Car: return 1.5f; // $/литр
                default: return 0f;
            }
        }

        /// <summary>
        /// Получить базовую цену транспорта
        /// </summary>
        private double GetVehicleBasePrice(TransportType type)
        {
            switch (type)
            {
                case TransportType.Bicycle: return 200;
                case TransportType.Motorcycle: return 3000;
                case TransportType.Car: return 15000;
                default: return 0;
            }
        }

        /// <summary>
        /// Можно ли использовать транспорт в локации
        /// </summary>
        public bool CanUseTransportInLocation(TransportType transport, Location location)
        {
            switch (location)
            {
                case Location.Library:
                case Location.Hospital:
                    // В некоторых местах нельзя парковаться
                    return transport == TransportType.None || transport == TransportType.PublicTransport;
                    
                case Location.Industrial:
                case Location.Abandoned:
                    // В промышленных районах высокий риск кражи
                    return true;
                    
                default:
                    return true;
            }
        }

        /// <summary>
        /// Получить стоимость поездки
        /// </summary>
        public double GetTripCost(TransportType transport, float distance)
        {
            switch (transport)
            {
                case TransportType.None:
                    return 0;
                    
                case TransportType.PublicTransport:
                    return distance * publicTransportCostPerKm;
                    
                case TransportType.Taxi:
                    return distance * 2f; // $2 за км
                    
                case TransportType.Bicycle:
                case TransportType.Motorcycle:
                case TransportType.Car:
                    if (ownedVehicles.ContainsKey(transport))
                    {
                        float fuelConsumption = GetBaseFuelConsumption(transport) * distance * 100; // Примерный расчет
                        return CalculateFuelCost(transport, fuelConsumption);
                    }
                    return 0;
                    
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public TransportSystemSaveData GetSaveData()
        {
            return new TransportSystemSaveData
            {
                ownedVehicles = new Dictionary<TransportType, Vehicle>(ownedVehicles),
                currentTransport = currentTransport,
                lastMaintenanceTime = lastMaintenanceTime,
                totalDistanceTraveled = totalDistanceTraveled,
                vehicleUsageTime = new Dictionary<TransportType, float>(vehicleUsageTime)
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(TransportSystemSaveData data)
        {
            ownedVehicles = data.ownedVehicles ?? new Dictionary<TransportType, Vehicle>();
            currentTransport = data.currentTransport;
            lastMaintenanceTime = data.lastMaintenanceTime;
            totalDistanceTraveled = data.totalDistanceTraveled;
            vehicleUsageTime = data.vehicleUsageTime ?? new Dictionary<TransportType, float>();

            // Применить эффекты текущего транспорта
            if (currentTransport != TransportType.None)
            {
                ApplyTransportModifiers(currentTransport);
            }

            if (showDebugInfo)
            {
                Debug.Log($"TransportSystem загружен. Транспорта: {ownedVehicles.Count}, " +
                         $"Текущий: {currentTransport}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(Location oldLocation, Location newLocation)
        {
            // Добавить пройденное расстояние
            float distance = CalculateDistance(oldLocation, newLocation);
            totalDistanceTraveled += distance;

            // Обновить одометр текущего транспорта
            if (currentTransport != TransportType.None && ownedVehicles.ContainsKey(currentTransport))
            {
                ownedVehicles[currentTransport].odometer += distance;
            }

            // Проверить совместимость транспорта с новой локацией
            if (!CanUseTransportInLocation(currentTransport, newLocation))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Ограничение", 
                        $"В {newLocation} нельзя использовать {GetTransportName(currentTransport)}", 
                        NotificationType.Warning);
                }
                // Возможно, принудительно изменить на пешком
            }

            // Проверить риск кражи в опасных районах
            CheckTheftRisk(newLocation);
        }

        /// <summary>
        /// Проверить риск кражи транспорта
        /// </summary>
        private void CheckTheftRisk(Location location)
        {
            if (!allowTransportTheft || currentTransport == TransportType.None)
                return;

            float theftRisk = GetLocationTheftRisk(location);
            
            if (UnityEngine.Random.value < theftRisk)
            {
                // Транспорт украден
                if (ownedVehicles.ContainsKey(currentTransport))
                {
                    var vehicle = ownedVehicles[currentTransport];
                    OnVehicleLost?.Invoke(vehicle);
                    ownedVehicles.Remove(currentTransport);
                    
                    if (notificationSystem != null)
                    {
                        notificationSystem.ShowNotification("Кража!", 
                            $"Ваш {GetTransportName(currentTransport)} украли!", 
                            NotificationType.Warning);
                    }
                    
                    ChangeTransport(TransportType.None);
                }
            }
        }

        /// <summary>
        /// Получить риск кражи для локации
        /// </summary>
        private float GetLocationTheftRisk(Location location)
        {
            switch (location)
            {
                case Location.Abandoned: return 0.05f; // 5%
                case Location.Industrial: return 0.03f; // 3%
                case Location.Street: return 0.01f; // 1%
                default: return 0f;
            }
        }

        /// <summary>
        /// Рассчитать расстояние между локациями
        /// </summary>
        private float CalculateDistance(Location from, Location to)
        {
            // Примерные расстояния между локациями
            if (from == to)
                return 0f;
                
            return UnityEngine.Random.Range(1f, 5f); // 1-5 км
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Add Test Vehicle")]
        private void DebugAddTestVehicle()
        {
            PurchaseVehicle(TransportType.Car, 100);
        }

        [ContextMenu("Change to Car")]
        private void DebugChangeToCar()
        {
            ChangeTransport(TransportType.Car);
        }

        [ContextMenu("Break Current Vehicle")]
        private void DebugBreakCurrentVehicle()
        {
            if (currentTransport != TransportType.None && ownedVehicles.ContainsKey(currentTransport))
            {
                ownedVehicles[currentTransport].condition = TransportCondition.Broken;
                ownedVehicles[currentTransport].conditionValue = 0f;
                OnVehicleBrokenDown?.Invoke(currentTransport);
            }
        }

        [ContextMenu("Show Transport Info")]
        private void DebugShowTransportInfo()
        {
            Debug.Log($"Текущий транспорт: {currentTransport}");
            Debug.Log($"Множитель скорости: {SpeedMultiplier}");
            Debug.Log($"Транспорта в собственности: {ownedVehicles.Count}");
            Debug.Log($"Общее расстояние: {totalDistanceTraveled} км");
            
            foreach (var kvp in ownedVehicles)
            {
                var vehicle = kvp.Value;
                Debug.Log($"- {kvp.Key}: {vehicle.condition} ({vehicle.conditionValue:F1}%), " +
                         $"Топливо: {vehicle.fuelLevel:F1}/{vehicle.maxFuelCapacity:F1}, " +
                         $"Пробег: {vehicle.odometer:F1} км");
            }
        }

        #endregion
    }

    /// <summary>
    /// Класс транспортного средства
    /// </summary>
    [System.Serializable]
    public class Vehicle
    {
        public TransportType type;                          // Тип транспорта
        public TransportAcquisitionType acquisitionType;   // Способ получения
        public float purchaseTime;                          // Время приобретения
        public TransportCondition condition;               // Состояние
        public float conditionValue = 100f;                // Значение состояния (0-100)
        public float fuelLevel;                             // Уровень топлива
        public float maxFuelCapacity;                       // Максимальная емкость бака
        public float odometer;                              // Пробег
        public bool isRented;                               // Арендованный
        public float rentExpirationTime;                    // Время истечения аренды

        /// <summary>
        /// Получить множитель состояния
        /// </summary>
        public float GetConditionMultiplier()
        {
            return conditionValue / 100f;
        }

        /// <summary>
        /// Обновить состояние на основе значения
        /// </summary>
        public void UpdateConditionFromValue()
        {
            if (conditionValue <= 0f)
                condition = TransportCondition.Broken;
            else if (conditionValue <= 20f)
                condition = TransportCondition.Poor;
            else if (conditionValue <= 40f)
                condition = TransportCondition.Fair;
            else if (conditionValue <= 70f)
                condition = TransportCondition.Good;
            else if (conditionValue <= 90f)
                condition = TransportCondition.Excellent;
            else
                condition = TransportCondition.Perfect;
        }

        /// <summary>
        /// Получить описание транспорта
        /// </summary>
        public string GetDescription()
        {
            string desc = $"{type} ({condition})";
            if (isRented)
            {
                float timeLeft = rentExpirationTime - Time.time;
                desc += $" - Аренда: {timeLeft / 3600f:F1}ч";
            }
            return desc;
        }
    }

    /// <summary>
    /// Данные TransportSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class TransportSystemSaveData
    {
        public Dictionary<TransportType, Vehicle> ownedVehicles;
        public TransportType currentTransport;
        public float lastMaintenanceTime;
        public double totalDistanceTraveled;
        public Dictionary<TransportType, float> vehicleUsageTime;
    }
}
