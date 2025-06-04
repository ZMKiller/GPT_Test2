using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система полиции и правопорядка
    /// </summary>
    public class PoliceSystem : MonoBehaviour
    {
        [Header("Настройки полицейской системы")]
        [SerializeField] private bool enablePoliceSystem = true;
        [SerializeField] private float policeResponseTime = 30f;       // Время отклика полиции в секундах
        [SerializeField] private float wantedDecayRate = 1f;           // Скорость снижения розыска (в час)
        [SerializeField] private bool allowBribes = true;              // Разрешить взятки
        [SerializeField] private bool allowNegotiation = true;         // Разрешить переговоры

        [Header("Штрафы и наказания")]
        [SerializeField] private double baseFightingFine = 50;         // Базовый штраф за драку
        [SerializeField] private double baseWeaponFine = 100;          // Базовый штраф за оружие
        [SerializeField] private double baseTheftFine = 200;           // Базовый штраф за кражу
        [SerializeField] private int baseJailTime = 60;                // Базовое время в тюрьме (секунды)
        [SerializeField] private float fineMultiplier = 1.5f;          // Множитель штрафа с каждым уровнем розыска

        [Header("Взятки и переговоры")]
        [SerializeField] private float baseBribeChance = 0.3f;         // Базовый шанс принятия взятки
        [SerializeField] private float bribeMultiplier = 2f;           // Множитель стоимости взятки
        [SerializeField] private float negotiationChance = 0.4f;       // Шанс успешных переговоров
        [SerializeField] private float charismaBribeBonus = 0.05f;     // Бонус от харизмы к взяткам

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool disableArrest = false;
        [SerializeField] private WantedLevel debugWantedLevel = WantedLevel.None;

        // Текущее состояние
        private WantedLevel currentWantedLevel = WantedLevel.None;
        private float wantedTimer = 0f;
        private float lastCrimeTime = 0f;
        private List<PoliceRecord> crimeHistory = new List<PoliceRecord>();
        private bool isUnderArrest = false;
        private bool policeCalled = false;

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;
        private NotificationSystem notificationSystem;

        // События
        public event Action<WantedLevel> OnWantedLevelChanged;
        public event Action<CrimeType, double> OnFineIssued;
        public event Action<int> OnPlayerArrested; // Время в тюрьме
        public event Action<double> OnBribePaid;
        public event Action<CrimeType> OnCrimeReported;
        public event Action OnPoliceArrival;

        // Свойства
        public WantedLevel CurrentWantedLevel => currentWantedLevel;
        public bool IsWanted => currentWantedLevel > WantedLevel.None;
        public bool IsUnderArrest => isUnderArrest;
        public float TimeUntilWantedDecay => wantedTimer;
        public List<PoliceRecord> CrimeHistory => new List<PoliceRecord>(crimeHistory);
        public float CurrentActivityLevel => 1f + (int)currentWantedLevel * 0.1f;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = FindObjectOfType<SkillSystem>();
            locationManager = FindObjectOfType<LocationManager>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
        }

        private void Start()
        {
            if (enablePoliceSystem)
            {
                SubscribeToEvents();
            }

            Debug.Log("PoliceSystem инициализирован");
        }

        private void Update()
        {
            if (enablePoliceSystem)
            {
                UpdateWantedLevel();
                CheckPoliceEncounter();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

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

        #region Crime Reporting

        /// <summary>
        /// Сообщить о преступлении
        /// </summary>
        public void ReportCrime(CrimeType crimeType, float severity = 1f)
        {
            if (!enablePoliceSystem)
                return;

            // Добавить запись в историю преступлений
            var record = new PoliceRecord
            {
                crimeType = crimeType,
                timestamp = Time.time,
                location = locationManager?.CurrentLocation ?? Location.Street,
                severity = severity,
                resolved = false
            };

            crimeHistory.Add(record);
            lastCrimeTime = Time.time;

            // Увеличить уровень розыска
            IncreaseWantedLevel(crimeType, severity);

            OnCrimeReported?.Invoke(crimeType);

            // Вызвать полицию с задержкой
            if (!policeCalled)
            {
                StartCoroutine(CallPolice());
            }

            if (showDebugInfo)
            {
                Debug.Log($"Преступление зарегистрировано: {crimeType} (степень: {severity:F1})");
            }
        }

        /// <summary>
        /// Сообщить о драке
        /// </summary>
        public void ReportFighting()
        {
            ReportCrime(CrimeType.Fighting, 0.5f);
        }

        /// <summary>
        /// Сообщить о ношении оружия
        /// </summary>
        public void ReportWeaponViolation(WeaponType weaponType)
        {
            float severity = GetWeaponSeverity(weaponType);
            ReportCrime(CrimeType.IllegalWeapon, severity);
        }

        /// <summary>
        /// Сообщить о краже
        /// </summary>
        public void ReportTheft(double stolenAmount)
        {
            float severity = Mathf.Clamp01((float)(stolenAmount / 100.0)); // Нормализация
            ReportCrime(CrimeType.Theft, severity);
        }

        /// <summary>
        /// Сообщить о нападении
        /// </summary>
        public void ReportAssault(float damage)
        {
            float severity = Mathf.Clamp01(damage / 50f); // Нормализация по урону
            ReportCrime(CrimeType.Assault, severity);
        }

        /// <summary>
        /// Сообщить о хулиганстве
        /// </summary>
        public void ReportVandalism()
        {
            ReportCrime(CrimeType.Vandalism, 0.3f);
        }

        #endregion

        #region Wanted Level Management

        /// <summary>
        /// Увеличить уровень розыска
        /// </summary>
        private void IncreaseWantedLevel(CrimeType crimeType, float severity)
        {
            WantedLevel oldLevel = currentWantedLevel;
            float increase = GetCrimeWantedIncrease(crimeType) * severity;

            // Преобразовать в уровни розыска
            int newLevelInt = (int)currentWantedLevel + Mathf.CeilToInt(increase);
            currentWantedLevel = (WantedLevel)Mathf.Clamp(newLevelInt, 0, (int)WantedLevel.MostWanted);

            // Обновить таймер
            wantedTimer = GetWantedDecayTime(currentWantedLevel);

            if (oldLevel != currentWantedLevel)
            {
                OnWantedLevelChanged?.Invoke(currentWantedLevel);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Уровень розыска изменен: {oldLevel} -> {currentWantedLevel}");
                }
            }
        }

        /// <summary>
        /// Обновить уровень розыска
        /// </summary>
        private void UpdateWantedLevel()
        {
            if (currentWantedLevel == WantedLevel.None)
                return;

            wantedTimer -= Time.deltaTime;

            if (wantedTimer <= 0f)
            {
                DecreaseWantedLevel();
            }
        }

        /// <summary>
        /// Снизить уровень розыска
        /// </summary>
        private void DecreaseWantedLevel()
        {
            if (currentWantedLevel == WantedLevel.None)
                return;

            WantedLevel oldLevel = currentWantedLevel;
            currentWantedLevel = (WantedLevel)Mathf.Max(0, (int)currentWantedLevel - 1);
            
            if (currentWantedLevel > WantedLevel.None)
            {
                wantedTimer = GetWantedDecayTime(currentWantedLevel);
            }
            else
            {
                wantedTimer = 0f;
            }

            OnWantedLevelChanged?.Invoke(currentWantedLevel);

            if (showDebugInfo)
            {
                Debug.Log($"Уровень розыска снижен: {oldLevel} -> {currentWantedLevel}");
            }
        }

        /// <summary>
        /// Очистить розыск
        /// </summary>
        public void ClearWantedLevel()
        {
            WantedLevel oldLevel = currentWantedLevel;
            currentWantedLevel = WantedLevel.None;
            wantedTimer = 0f;
            policeCalled = false;

            OnWantedLevelChanged?.Invoke(currentWantedLevel);

            if (showDebugInfo && oldLevel != WantedLevel.None)
            {
                Debug.Log("Розыск очищен");
            }
        }

        #endregion

        #region Police Encounters

        /// <summary>
        /// Проверить встречу с полицией
        /// </summary>
        private void CheckPoliceEncounter()
        {
            if (!IsWanted || isUnderArrest)
                return;

            // Шанс встречи с полицией зависит от локации и уровня розыска
            float encounterChance = GetPoliceEncounterChance();
            
            if (UnityEngine.Random.value < encounterChance * Time.deltaTime)
            {
                StartPoliceEncounter();
            }
        }

        /// <summary>
        /// Начать встречу с полицией
        /// </summary>
        private void StartPoliceEncounter()
        {
            if (showDebugInfo)
            {
                Debug.Log("Встреча с полицией!");
            }

            // Показать уведомление
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Полиция!", "Вас остановил полицейский", NotificationType.Warning);
            }

            // Начать процедуру ареста или переговоров
            StartCoroutine(HandlePoliceEncounter());
        }

        /// <summary>
        /// Обработать встречу с полицией
        /// </summary>
        private IEnumerator HandlePoliceEncounter()
        {
            yield return new WaitForSeconds(1f);

            // Определить действие полиции
            PoliceAction action = DeterminePoliceAction();

            switch (action)
            {
                case PoliceAction.Fine:
                    IssueFine();
                    break;

                case PoliceAction.Arrest:
                    if (!disableArrest)
                    {
                        ArrestPlayer();
                    }
                    else
                    {
                        IssueFine();
                    }
                    break;

                case PoliceAction.Warning:
                    IssueWarning();
                    break;

                case PoliceAction.Chase:
                    StartChase();
                    break;
            }
        }

        /// <summary>
        /// Определить действие полиции
        /// </summary>
        private PoliceAction DeterminePoliceAction()
        {
            switch (currentWantedLevel)
            {
                case WantedLevel.Suspicious:
                    return UnityEngine.Random.value < 0.7f ? PoliceAction.Warning : PoliceAction.Fine;

                case WantedLevel.Minor:
                    return UnityEngine.Random.value < 0.5f ? PoliceAction.Fine : PoliceAction.Warning;

                case WantedLevel.Moderate:
                    return UnityEngine.Random.value < 0.6f ? PoliceAction.Fine : PoliceAction.Arrest;

                case WantedLevel.Serious:
                    return UnityEngine.Random.value < 0.3f ? PoliceAction.Fine : PoliceAction.Arrest;

                case WantedLevel.Dangerous:
                case WantedLevel.MostWanted:
                    return UnityEngine.Random.value < 0.2f ? PoliceAction.Chase : PoliceAction.Arrest;

                default:
                    return PoliceAction.Warning;
            }
        }

        #endregion

        #region Police Actions

        /// <summary>
        /// Выписать штраф
        /// </summary>
        private void IssueFine()
        {
            double fineAmount = CalculateFineAmount();
            
            if (moneySystem != null && moneySystem.CanAfford(fineAmount))
            {
                moneySystem.SpendMoney(fineAmount, "Штраф полиции");
                OnFineIssued?.Invoke(GetMostRecentCrimeType(), fineAmount);
                
                // Снизить уровень розыска
                currentWantedLevel = (WantedLevel)Mathf.Max(0, (int)currentWantedLevel - 1);
                OnWantedLevelChanged?.Invoke(currentWantedLevel);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Штраф", $"Вы заплатили штраф ${fineAmount}", NotificationType.Money);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Выписан штраф: ${fineAmount}");
                }
            }
            else
            {
                // Не может заплатить штраф - арест
                ArrestPlayer();
            }
        }

        /// <summary>
        /// Арестовать игрока
        /// </summary>
        private void ArrestPlayer()
        {
            int jailTime = CalculateJailTime();
            isUnderArrest = true;

            OnPlayerArrested?.Invoke(jailTime);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Арест!", $"Вы арестованы на {jailTime} секунд", NotificationType.Warning);
            }

            // Забрать часть денег
            if (moneySystem != null)
            {
                double currentMoney = moneySystem.GetMoneyDouble();
                double confiscatedAmount = currentMoney * 0.1; // 10% конфискация
                if (confiscatedAmount > 0)
                {
                    moneySystem.SpendMoney(confiscatedAmount, "Конфискация при аресте");
                }
            }

            StartCoroutine(ServeJailTime(jailTime));

            if (showDebugInfo)
            {
                Debug.Log($"Игрок арестован на {jailTime} секунд");
            }
        }

        /// <summary>
        /// Выдать предупреждение
        /// </summary>
        private void IssueWarning()
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Предупреждение", "Полиция вас предупредила", NotificationType.Info);
            }

            // Небольшое снижение розыска
            wantedTimer = Mathf.Max(0, wantedTimer - 30f);

            if (showDebugInfo)
            {
                Debug.Log("Полиция выдала предупреждение");
            }
        }

        /// <summary>
        /// Начать погоню
        /// </summary>
        private void StartChase()
        {
            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Погоня!", "Полиция начала погоню!", NotificationType.Warning);
            }

            // Увеличить уровень розыска
            IncreaseWantedLevel(CrimeType.ResistingArrest, 1f);

            if (showDebugInfo)
            {
                Debug.Log("Началась полицейская погоня");
            }
        }

        /// <summary>
        /// Отсидеть в тюрьме
        /// </summary>
        private IEnumerator ServeJailTime(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            
            isUnderArrest = false;
            ClearWantedLevel();

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Освобождение", "Вы освобождены из тюрьмы", NotificationType.Info);
            }

            if (showDebugInfo)
            {
                Debug.Log("Игрок освобожден из тюрьмы");
            }
        }

        #endregion

        #region Bribes and Negotiation

        /// <summary>
        /// Попытаться дать взятку
        /// </summary>
        public bool AttemptBribe(double amount)
        {
            if (!allowBribes || !IsWanted)
                return false;

            if (moneySystem == null || !moneySystem.CanAfford(amount))
                return false;

            float bribeChance = CalculateBribeSuccessChance(amount);
            
            if (UnityEngine.Random.value < bribeChance)
            {
                moneySystem.SpendMoney(amount, "Взятка полиции");
                OnBribePaid?.Invoke(amount);
                
                // Снизить розыск
                currentWantedLevel = (WantedLevel)Mathf.Max(0, (int)currentWantedLevel - 2);
                wantedTimer = GetWantedDecayTime(currentWantedLevel);
                OnWantedLevelChanged?.Invoke(currentWantedLevel);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Взятка принята", $"Заплачено ${amount}", NotificationType.Money);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Взятка принята: ${amount}");
                }

                return true;
            }
            else
            {
                // Взятка отклонена - ухудшение ситуации
                IncreaseWantedLevel(CrimeType.BriberyAttempt, 0.5f);

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Взятка отклонена", "Ситуация ухудшилась", NotificationType.Warning);
                }

                if (showDebugInfo)
                {
                    Debug.Log("Взятка отклонена");
                }

                return false;
            }
        }

        /// <summary>
        /// Попытаться договориться
        /// </summary>
        public bool AttemptNegotiation()
        {
            if (!allowNegotiation || !IsWanted)
                return false;

            float negotiationSuccessChance = CalculateNegotiationChance();
            
            if (UnityEngine.Random.value < negotiationSuccessChance)
            {
                // Успешные переговоры
                wantedTimer = Mathf.Max(0, wantedTimer - 60f); // Снизить время розыска

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Переговоры", "Вам удалось договориться", NotificationType.Info);
                }

                if (showDebugInfo)
                {
                    Debug.Log("Переговоры успешны");
                }

                return true;
            }
            else
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Переговоры провалились", "Полиция не слушает", NotificationType.Warning);
                }

                if (showDebugInfo)
                {
                    Debug.Log("Переговоры провалились");
                }

                return false;
            }
        }

        #endregion

        #region Calculations

        /// <summary>
        /// Рассчитать сумму штрафа
        /// </summary>
        private double CalculateFineAmount()
        {
            CrimeType recentCrime = GetMostRecentCrimeType();
            double baseFine = GetBaseFineForCrime(recentCrime);
            
            // Увеличить штраф с уровнем розыска
            float multiplier = 1f + ((int)currentWantedLevel * (fineMultiplier - 1f));
            
            return baseFine * multiplier;
        }

        /// <summary>
        /// Рассчитать время в тюрьме
        /// </summary>
        private int CalculateJailTime()
        {
            int baseTime = baseJailTime;
            
            // Увеличить время с уровнем розыска
            baseTime += (int)currentWantedLevel * 30;
            
            return baseTime;
        }

        /// <summary>
        /// Рассчитать шанс успеха взятки
        /// </summary>
        private float CalculateBribeSuccessChance(double amount)
        {
            float baseChance = baseBribeChance;
            
            // Бонус от суммы взятки
            double recommendedBribe = CalculateFineAmount() * bribeMultiplier;
            float amountBonus = Mathf.Clamp01((float)(amount / recommendedBribe));
            baseChance += amountBonus * 0.3f;
            
            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                baseChance += charisma * charismaBribeBonus;
            }
            
            // Штраф за высокий уровень розыска
            baseChance -= (int)currentWantedLevel * 0.1f;
            
            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Рассчитать шанс успешных переговоров
        /// </summary>
        private float CalculateNegotiationChance()
        {
            float chance = negotiationChance;
            
            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                chance += charisma * 0.05f;
            }
            
            // Штраф за уровень розыска
            chance -= (int)currentWantedLevel * 0.15f;
            
            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Получить шанс встречи с полицией
        /// </summary>
        private float GetPoliceEncounterChance()
        {
            float baseChance = 0.01f; // 1% в секунду
            
            // Увеличить шанс с уровнем розыска
            baseChance *= (1f + (int)currentWantedLevel);
            
            // Модификатор локации
            if (locationManager != null)
            {
                baseChance *= GetLocationPoliceMultiplier(locationManager.CurrentLocation);
            }
            
            // Модификатор времени суток
            if (timeOfDayManager != null)
            {
                var period = timeOfDayManager.CurrentTimePeriod;
                if (period == TimePeriod.Night || period == TimePeriod.LateNight)
                {
                    baseChance *= 0.5f; // Меньше полиции ночью
                }
            }
            
            return baseChance;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Вызвать полицию
        /// </summary>
        private IEnumerator CallPolice()
        {
            policeCalled = true;
            yield return new WaitForSeconds(policeResponseTime);
            
            OnPoliceArrival?.Invoke();
            
            if (showDebugInfo)
            {
                Debug.Log("Полиция прибыла на место происшествия");
            }
            
            policeCalled = false;
        }

        /// <summary>
        /// Получить степень серьезности оружия
        /// </summary>
        private float GetWeaponSeverity(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Pistol: return 1.0f;
                case WeaponType.Knife: return 0.7f;
                case WeaponType.Machete: return 0.8f;
                case WeaponType.Sword: return 0.9f;
                case WeaponType.Axe: return 0.6f;
                case WeaponType.Club: return 0.4f;
                case WeaponType.Bat: return 0.3f;
                case WeaponType.Chain: return 0.5f;
                case WeaponType.BrokenBottle: return 0.3f;
                case WeaponType.PepperSpray: return 0.1f;
                case WeaponType.Taser: return 0.2f;
                default: return 0.2f;
            }
        }

        /// <summary>
        /// Получить увеличение розыска за тип преступления
        /// </summary>
        private float GetCrimeWantedIncrease(CrimeType crimeType)
        {
            switch (crimeType)
            {
                case CrimeType.Fighting: return 1f;
                case CrimeType.IllegalWeapon: return 1.5f;
                case CrimeType.Theft: return 2f;
                case CrimeType.Assault: return 2.5f;
                case CrimeType.Vandalism: return 0.5f;
                case CrimeType.ResistingArrest: return 2f;
                case CrimeType.BriberyAttempt: return 1f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Получить базовый штраф за преступление
        /// </summary>
        private double GetBaseFineForCrime(CrimeType crimeType)
        {
            switch (crimeType)
            {
                case CrimeType.Fighting: return baseFightingFine;
                case CrimeType.IllegalWeapon: return baseWeaponFine;
                case CrimeType.Theft: return baseTheftFine;
                case CrimeType.Assault: return baseFightingFine * 2;
                case CrimeType.Vandalism: return baseFightingFine * 0.5;
                case CrimeType.ResistingArrest: return baseFightingFine * 3;
                case CrimeType.BriberyAttempt: return baseFightingFine * 2;
                default: return baseFightingFine;
            }
        }

        /// <summary>
        /// Получить время снижения розыска
        /// </summary>
        private float GetWantedDecayTime(WantedLevel level)
        {
            switch (level)
            {
                case WantedLevel.Suspicious: return 120f; // 2 минуты
                case WantedLevel.Minor: return 300f; // 5 минут
                case WantedLevel.Moderate: return 600f; // 10 минут
                case WantedLevel.Serious: return 1200f; // 20 минут
                case WantedLevel.Dangerous: return 1800f; // 30 минут
                case WantedLevel.MostWanted: return 3600f; // 1 час
                default: return 0f;
            }
        }

        /// <summary>
        /// Получить множитель полиции для локации
        /// </summary>
        private float GetLocationPoliceMultiplier(Location location)
        {
            switch (location)
            {
                case Location.BusinessDistrict: return 2.0f; // Много полиции
                case Location.Shop: return 1.5f;
                case Location.Hospital: return 1.3f;
                case Location.University: return 1.2f;
                case Location.JobCenter: return 1.1f;
                case Location.Street: return 1.0f;
                case Location.Park: return 0.8f;
                case Location.Industrial: return 0.6f;
                case Location.Abandoned: return 0.3f; // Мало полиции
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Получить последний тип преступления
        /// </summary>
        private CrimeType GetMostRecentCrimeType()
        {
            if (crimeHistory.Count == 0)
                return CrimeType.Fighting;

            return crimeHistory[crimeHistory.Count - 1].crimeType;
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public PoliceSystemSaveData GetSaveData()
        {
            return new PoliceSystemSaveData
            {
                currentWantedLevel = currentWantedLevel,
                wantedTimer = wantedTimer,
                lastCrimeTime = lastCrimeTime,
                crimeHistory = new List<PoliceRecord>(crimeHistory),
                isUnderArrest = isUnderArrest
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(PoliceSystemSaveData data)
        {
            currentWantedLevel = data.currentWantedLevel;
            wantedTimer = data.wantedTimer;
            lastCrimeTime = data.lastCrimeTime;
            crimeHistory = data.crimeHistory ?? new List<PoliceRecord>();
            isUnderArrest = data.isUnderArrest;

            if (showDebugInfo)
            {
                Debug.Log($"PoliceSystem загружен. Розыск: {currentWantedLevel}, " +
                         $"Преступлений: {crimeHistory.Count}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик смены локации
        /// </summary>
        private void OnLocationChanged(LocationType oldLocation, LocationType newLocation)
        {
            // В некоторых локациях увеличивается шанс встречи с полицией
            if (IsWanted && IsHighSecurityLocation((Location)newLocation))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Вход в зону повышенной безопасности: {newLocation}");
                }
            }
        }

        /// <summary>
        /// Проверить, является ли локация зоной повышенной безопасности
        /// </summary>
        private bool IsHighSecurityLocation(Location location)
        {
            switch (location)
            {
                case Location.BusinessDistrict:
                case Location.Hospital:
                case Location.University:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Report Test Crime")]
        private void DebugReportTestCrime()
        {
            ReportCrime(CrimeType.Fighting, 1f);
        }

        [ContextMenu("Increase Wanted Level")]
        private void DebugIncreaseWantedLevel()
        {
            IncreaseWantedLevel(CrimeType.Fighting, 1f);
        }

        [ContextMenu("Clear Wanted Level")]
        private void DebugClearWantedLevel()
        {
            ClearWantedLevel();
        }

        [ContextMenu("Start Police Encounter")]
        private void DebugStartPoliceEncounter()
        {
            if (IsWanted)
            {
                StartPoliceEncounter();
            }
            else
            {
                Debug.LogWarning("Игрок не разыскивается");
            }
        }

        [ContextMenu("Show Police Info")]
        private void DebugShowPoliceInfo()
        {
            Debug.Log($"Уровень розыска: {currentWantedLevel}");
            Debug.Log($"Время до снижения: {wantedTimer:F1}с");
            Debug.Log($"Под арестом: {isUnderArrest}");
            Debug.Log($"История преступлений: {crimeHistory.Count}");
            
            foreach (var record in crimeHistory)
            {
                Debug.Log($"- {record.crimeType} в {record.location} (серьезность: {record.severity:F1})");
            }
        }

        #endregion
    }

    /// <summary>
    /// Запись о преступлении
    /// </summary>
    [System.Serializable]
    public class PoliceRecord
    {
        public CrimeType crimeType;      // Тип преступления
        public float timestamp;          // Время совершения
        public Location location;        // Место совершения
        public float severity;           // Серьезность (0-1)
        public bool resolved;            // Решено ли дело
        public double fine;              // Сумма штрафа
        public int jailTime;             // Время в тюрьме
    }

    /// <summary>
    /// Действия полиции
    /// </summary>
    public enum PoliceAction
    {
        Warning,    // Предупреждение
        Fine,       // Штраф
        Arrest,     // Арест
        Chase       // Погоня
    }

    /// <summary>
    /// Данные PoliceSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class PoliceSystemSaveData
    {
        public WantedLevel currentWantedLevel;
        public float wantedTimer;
        public float lastCrimeTime;
        public List<PoliceRecord> crimeHistory;
        public bool isUnderArrest;
    }
}
