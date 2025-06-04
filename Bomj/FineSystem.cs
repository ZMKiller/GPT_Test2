using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Расширенная система штрафов и нарушений
    /// </summary>
    public class FineSystem : MonoBehaviour
    {
        [Header("Настройки системы штрафов")]
        [SerializeField] private bool enableFineSystem = true;
        [SerializeField] private int maxUnpaidFines = 10;                  // Максимум неоплаченных штрафов
        [SerializeField] private float fineEscalationTime = 604800f;       // Время эскалации штрафа (секунды)
        [SerializeField] private float arrestWarningTime = 86400f;         // Время предупреждения об аресте

        [Header("Базовые штрафы")]
        [SerializeField] private double minorViolationFine = 50;           // Мелкое нарушение
        [SerializeField] private double publicDisorderFine = 150;          // Нарушение общественного порядка
        [SerializeField] private double vagrancyFine = 100;                // Бродяжничество
        [SerializeField] private double theftFine = 500;                   // Кража
        [SerializeField] private double aggressionFine = 300;              // Агрессия

        [Header("Модификаторы штрафов")]
        [SerializeField] private float repeatOffenseMultiplier = 1.5f;     // Множитель повторного нарушения
        [SerializeField] private float locationSeverityMultiplier = 1.2f;  // Множитель в зависимости от локации
        [SerializeField] private float timeOfDayMultiplier = 1.3f;         // Множитель времени суток

        [Header("Взятки и убеждение")]
        [SerializeField] private float baseBribeChance = 0.3f;             // Базовый шанс взятки
        [SerializeField] private float basePersuasionChance = 0.2f;        // Базовый шанс убеждения
        [SerializeField] private double bribeMultiplier = 2.0;             // Множитель взятки

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool freeFromFines = false;
        [SerializeField] private bool instantArrest = false;

        // Состояние системы
        private List<Fine> unpaidFines = new List<Fine>();
        private List<Fine> paidFines = new List<Fine>();
        private Dictionary<ViolationType, int> violationHistory = new Dictionary<ViolationType, int>();
        private Dictionary<ViolationType, float> lastViolationTime = new Dictionary<ViolationType, float>();
        private bool isArrestWarning = false;
        private float arrestWarningTime = 0f;

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;
        private NotificationSystem notificationSystem;
        private PoliceSystem policeSystem;

        // События
        public event Action<Fine> OnFineIssued;
        public event Action<Fine> OnFinePaid;
        public event Action<Fine> OnFineEscalated;
        public event Action<ViolationType> OnViolationCommitted;
        public event Action OnArrestWarningIssued;
        public event Action OnArrestImminent;

        // Свойства
        public List<Fine> UnpaidFines => new List<Fine>(unpaidFines);
        public List<Fine> PaidFines => new List<Fine>(paidFines);
        public double TotalUnpaidAmount => CalculateTotalUnpaidAmount();
        public int TotalViolations => CalculateTotalViolations();
        public bool HasUnpaidFines => unpaidFines.Count > 0;
        public bool IsArrestWarning => isArrestWarning;

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
            policeSystem = FindObjectOfType<PoliceSystem>();
        }

        private void Start()
        {
            if (enableFineSystem)
            {
                InitializeViolationHistory();
            }

            Debug.Log("FineSystem инициализирован");
        }

        private void Update()
        {
            if (enableFineSystem)
            {
                UpdateFineSystem();
                CheckFineEscalation();
                CheckArrestConditions();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация истории нарушений
        /// </summary>
        private void InitializeViolationHistory()
        {
            foreach (ViolationType violationType in Enum.GetValues(typeof(ViolationType)))
            {
                violationHistory[violationType] = 0;
                lastViolationTime[violationType] = 0f;
            }
        }

        #endregion

        #region Fine System Update

        /// <summary>
        /// Обновить систему штрафов
        /// </summary>
        private void UpdateFineSystem()
        {
            // Обновить статус предупреждения об аресте
            if (isArrestWarning && Time.time - arrestWarningTime > this.arrestWarningTime)
            {
                // Предупреждение истекло - угроза ареста
                OnArrestImminent?.Invoke();
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("ОПАСНОСТЬ!", 
                        "Время на оплату штрафов истекло! Вас могут арестовать!", 
                        NotificationType.Critical);
                }
            }
        }

        /// <summary>
        /// Проверить эскалацию штрафов
        /// </summary>
        private void CheckFineEscalation()
        {
            for (int i = unpaidFines.Count - 1; i >= 0; i--)
            {
                var fine = unpaidFines[i];
                
                if (Time.time - fine.issueTime >= fineEscalationTime)
                {
                    EscalateFine(fine);
                }
            }
        }

        /// <summary>
        /// Проверить условия ареста
        /// </summary>
        private void CheckArrestConditions()
        {
            bool shouldArrest = false;

            // Слишком много неоплаченных штрафов
            if (unpaidFines.Count >= maxUnpaidFines)
            {
                shouldArrest = true;
            }

            // Общая сумма штрафов слишком высока
            if (TotalUnpaidAmount > 2000)
            {
                shouldArrest = true;
            }

            // Есть эскалированные штрафы
            if (unpaidFines.Exists(f => f.isEscalated))
            {
                shouldArrest = true;
            }

            if (shouldArrest && !isArrestWarning)
            {
                IssueArrestWarning();
            }
        }

        #endregion

        #region Violation Processing

        /// <summary>
        /// Зафиксировать нарушение
        /// </summary>
        public bool CommitViolation(ViolationType violationType, Location location = Location.Street)
        {
            if (freeFromFines)
                return false;

            // Шанс того, что нарушение заметят
            float detectionChance = CalculateDetectionChance(violationType, location);
            
            if (UnityEngine.Random.value > detectionChance)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"Нарушение {violationType} не было замечено");
                }
                return false;
            }

            // Обновить статистику нарушений
            violationHistory[violationType]++;
            lastViolationTime[violationType] = Time.time;

            OnViolationCommitted?.Invoke(violationType);

            // Возможность избежать штрафа
            if (TryAvoidFine(violationType))
            {
                return false;
            }

            // Выписать штраф
            IssueFine(violationType, location);
            return true;
        }

        /// <summary>
        /// Рассчитать шанс обнаружения нарушения
        /// </summary>
        private float CalculateDetectionChance(ViolationType violationType, Location location)
        {
            float baseChance = GetBaseDetectionChance(violationType);
            
            // Модификатор локации
            baseChance *= GetLocationDetectionMultiplier(location);
            
            // Модификатор времени суток
            if (timeOfDayManager != null)
            {
                baseChance *= GetTimeDetectionMultiplier(timeOfDayManager.CurrentTimePeriod);
            }

            // Модификатор активности полиции
            if (policeSystem != null)
            {
                baseChance *= policeSystem.CurrentActivityLevel;
            }

            // Скрытность игрока снижает шанс обнаружения
            if (skillSystem != null)
            {
                float stealth = skillSystem.GetSkillLevel(SkillType.Stealth);
                baseChance *= (1f - stealth * 0.05f); // До 50% снижения при максимальной скрытности
            }

            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Получить базовый шанс обнаружения
        /// </summary>
        private float GetBaseDetectionChance(ViolationType violationType)
        {
            switch (violationType)
            {
                case ViolationType.MinorViolation: return 0.3f;
                case ViolationType.PublicDisorder: return 0.6f;
                case ViolationType.Vagrancy: return 0.4f;
                case ViolationType.Theft: return 0.7f;
                case ViolationType.Aggression: return 0.8f;
                case ViolationType.Vandalism: return 0.5f;
                case ViolationType.Trespassing: return 0.6f;
                case ViolationType.IllegalBusiness: return 0.4f;
                default: return 0.5f;
            }
        }

        /// <summary>
        /// Получить множитель обнаружения для локации
        /// </summary>
        private float GetLocationDetectionMultiplier(Location location)
        {
            switch (location)
            {
                case Location.BusinessDistrict: return 1.5f;  // Высокая безопасность
                case Location.Hospital: return 1.3f;
                case Location.University: return 1.2f;
                case Location.Shop: return 1.1f;
                case Location.Park: return 0.8f;
                case Location.Street: return 1.0f;
                case Location.Industrial: return 0.7f;
                case Location.Abandoned: return 0.3f;        // Низкий контроль
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Получить множитель обнаружения для времени суток
        /// </summary>
        private float GetTimeDetectionMultiplier(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.EarlyMorning: return 0.6f;
                case TimePeriod.Morning: return 1.0f;
                case TimePeriod.Day: return 1.2f;      // Больше свидетелей
                case TimePeriod.Evening: return 1.0f;
                case TimePeriod.Night: return 0.7f;
                case TimePeriod.LateNight: return 0.4f; // Меньше патрулей
                default: return 1.0f;
            }
        }

        #endregion

        #region Fine Avoidance

        /// <summary>
        /// Попытаться избежать штрафа
        /// </summary>
        private bool TryAvoidFine(ViolationType violationType)
        {
            // Показать выбор игроку
            if (notificationSystem != null)
            {
                // TODO: Интеграция с UI системой для выбора
                // Пока используем автоматический выбор
                return AutoChooseAvoidanceMethod(violationType);
            }

            return false;
        }

        /// <summary>
        /// Автоматически выбрать метод избежания штрафа
        /// </summary>
        private bool AutoChooseAvoidanceMethod(ViolationType violationType)
        {
            // Попробовать убеждение если есть высокая харизма
            if (skillSystem != null && skillSystem.GetSkillLevel(SkillType.Charisma) >= 7f)
            {
                if (TryPersuasion(violationType))
                    return true;
            }

            // Попробовать взятку если есть деньги
            double bribeAmount = CalculateBribeAmount(violationType);
            if (moneySystem != null && moneySystem.CanAfford(bribeAmount))
            {
                if (TryBribe(violationType, bribeAmount))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Попытаться убедить
        /// </summary>
        public bool TryPersuasion(ViolationType violationType)
        {
            float successChance = CalculatePersuasionChance(violationType);
            
            if (UnityEngine.Random.value < successChance)
            {
                // Успешное убеждение
                if (skillSystem != null)
                {
                    skillSystem.AddSkillExperience(SkillType.Charisma, 5f, "Успешное убеждение");
                }

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Успех!", 
                        "Вам удалось убедить не выписывать штраф", NotificationType.Info);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Успешно избежали штрафа за {violationType} через убеждение");
                }

                return true;
            }
            else
            {
                // Неудачное убеждение может ухудшить ситуацию
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Неудача", 
                        "Попытка убеждения не удалась", NotificationType.Warning);
                }

                return false;
            }
        }

        /// <summary>
        /// Попытаться дать взятку
        /// </summary>
        public bool TryBribe(ViolationType violationType, double amount)
        {
            if (moneySystem == null || !moneySystem.CanAfford(amount))
                return false;

            float successChance = CalculateBribeChance(violationType, amount);
            
            if (UnityEngine.Random.value < successChance)
            {
                // Успешная взятка
                moneySystem.SpendMoney(amount, $"Взятка за {violationType}");

                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Взятка принята", 
                        $"Штраф избежан за ${amount}", NotificationType.Money);
                }

                if (showDebugInfo)
                {
                    Debug.Log($"Успешно избежали штрафа за {violationType} через взятку ${amount}");
                }

                return true;
            }
            else
            {
                // Неудачная взятка - потеряли деньги и получили дополнительное нарушение
                moneySystem.SpendMoney(amount, $"Неудачная взятка за {violationType}");
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Взятка отклонена!", 
                        $"Деньги потеряны, штраф увеличен!", NotificationType.Warning);
                }

                // Добавить нарушение за попытку дачи взятки
                CommitViolation(ViolationType.BribeAttempt);

                return false;
            }
        }

        /// <summary>
        /// Рассчитать шанс успеха убеждения
        /// </summary>
        private float CalculatePersuasionChance(ViolationType violationType)
        {
            float chance = basePersuasionChance;

            // Бонус от харизмы
            if (skillSystem != null)
            {
                float charisma = skillSystem.GetSkillLevel(SkillType.Charisma);
                chance += charisma * 0.05f; // До 50% бонуса
            }

            // Штраф за повторные нарушения
            int violationCount = violationHistory[violationType];
            chance -= violationCount * 0.1f;

            // Штраф за серьезные нарушения
            if (violationType == ViolationType.Theft || violationType == ViolationType.Aggression)
            {
                chance *= 0.5f;
            }

            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Рассчитать шанс успеха взятки
        /// </summary>
        private float CalculateBribeChance(ViolationType violationType, double amount)
        {
            float chance = baseBribeChance;

            // Размер взятки влияет на шанс
            double expectedBribe = CalculateBribeAmount(violationType);
            float amountRatio = (float)(amount / expectedBribe);
            chance *= amountRatio;

            // Штраф за повторные попытки взяток
            int bribeAttempts = violationHistory.ContainsKey(ViolationType.BribeAttempt) ? 
                violationHistory[ViolationType.BribeAttempt] : 0;
            chance -= bribeAttempts * 0.2f;

            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Рассчитать сумму взятки
        /// </summary>
        private double CalculateBribeAmount(ViolationType violationType)
        {
            double baseFine = GetBaseFineAmount(violationType);
            return baseFine * bribeMultiplier;
        }

        #endregion

        #region Fine Management

        /// <summary>
        /// Выписать штраф
        /// </summary>
        private void IssueFine(ViolationType violationType, Location location)
        {
            double amount = CalculateFineAmount(violationType, location);
            
            var fine = new Fine
            {
                id = System.Guid.NewGuid().ToString(),
                violationType = violationType,
                amount = amount,
                location = location,
                issueTime = Time.time,
                dueTime = Time.time + 604800f, // 1 неделя на оплату
                isPaid = false,
                isEscalated = false,
                description = GetViolationDescription(violationType)
            };

            unpaidFines.Add(fine);
            OnFineIssued?.Invoke(fine);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Штраф выписан", 
                    $"{fine.description}: ${amount}", NotificationType.Warning);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Выписан штраф: {violationType} на сумму ${amount}");
            }
        }

        /// <summary>
        /// Рассчитать сумму штрафа
        /// </summary>
        private double CalculateFineAmount(ViolationType violationType, Location location)
        {
            double baseAmount = GetBaseFineAmount(violationType);
            
            // Множитель повторных нарушений
            int violationCount = violationHistory[violationType];
            if (violationCount > 1)
            {
                baseAmount *= Mathf.Pow(repeatOffenseMultiplier, violationCount - 1);
            }

            // Множитель локации
            baseAmount *= GetLocationSeverityMultiplier(location);

            // Множитель времени суток
            if (timeOfDayManager != null)
            {
                baseAmount *= GetTimeSeverityMultiplier(timeOfDayManager.CurrentTimePeriod);
            }

            return Math.Round(baseAmount, 2);
        }

        /// <summary>
        /// Получить базовую сумму штрафа
        /// </summary>
        private double GetBaseFineAmount(ViolationType violationType)
        {
            switch (violationType)
            {
                case ViolationType.MinorViolation: return minorViolationFine;
                case ViolationType.PublicDisorder: return publicDisorderFine;
                case ViolationType.Vagrancy: return vagrancyFine;
                case ViolationType.Theft: return theftFine;
                case ViolationType.Aggression: return aggressionFine;
                case ViolationType.Vandalism: return 200;
                case ViolationType.Trespassing: return 180;
                case ViolationType.IllegalBusiness: return 400;
                case ViolationType.BribeAttempt: return 600;
                default: return 100;
            }
        }

        /// <summary>
        /// Получить множитель серьезности для локации
        /// </summary>
        private float GetLocationSeverityMultiplier(Location location)
        {
            switch (location)
            {
                case Location.BusinessDistrict: return 1.5f;  // Строже в деловом районе
                case Location.Hospital: return 1.3f;
                case Location.University: return 1.2f;
                case Location.Shop: return 1.1f;
                case Location.Park: return 0.9f;
                case Location.Street: return 1.0f;
                case Location.Industrial: return 0.8f;
                case Location.Abandoned: return 0.6f;        // Меньше внимания
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Получить множитель серьезности для времени суток
        /// </summary>
        private float GetTimeSeverityMultiplier(TimePeriod period)
        {
            switch (period)
            {
                case TimePeriod.Day: return 1.2f;      // Днем строже
                case TimePeriod.Evening: return 1.0f;
                case TimePeriod.Night: return 1.3f;     // Ночью опаснее
                case TimePeriod.LateNight: return 1.4f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Оплатить штраф
        /// </summary>
        public bool PayFine(string fineId)
        {
            var fine = unpaidFines.Find(f => f.id == fineId);
            if (fine == null)
                return false;

            if (moneySystem == null || !moneySystem.CanAfford(fine.amount))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Недостаточно денег", 
                        $"Нужно ${fine.amount} для оплаты штрафа", NotificationType.Warning);
                }
                return false;
            }

            moneySystem.SpendMoney(fine.amount, $"Штраф: {fine.description}");
            
            fine.isPaid = true;
            fine.paymentTime = Time.time;
            
            unpaidFines.Remove(fine);
            paidFines.Add(fine);

            OnFinePaid?.Invoke(fine);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Штраф оплачен", 
                    $"Оплачен штраф: ${fine.amount}", NotificationType.Money);
            }

            // Проверить снятие предупреждения об аресте
            CheckRemoveArrestWarning();

            if (showDebugInfo)
            {
                Debug.Log($"Оплачен штраф {fine.id}: ${fine.amount}");
            }

            return true;
        }

        /// <summary>
        /// Оплатить все штрафы
        /// </summary>
        public bool PayAllFines()
        {
            double totalAmount = TotalUnpaidAmount;
            
            if (moneySystem == null || !moneySystem.CanAfford(totalAmount))
            {
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Недостаточно денег", 
                        $"Нужно ${totalAmount} для оплаты всех штрафов", NotificationType.Warning);
                }
                return false;
            }

            int fineCount = unpaidFines.Count;
            
            // Оплатить все штрафы
            for (int i = unpaidFines.Count - 1; i >= 0; i--)
            {
                var fine = unpaidFines[i];
                PayFine(fine.id);
            }

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Все штрафы оплачены", 
                    $"Оплачено {fineCount} штрафов на сумму ${totalAmount}", NotificationType.Money);
            }

            return true;
        }

        /// <summary>
        /// Эскалировать штраф
        /// </summary>
        private void EscalateFine(Fine fine)
        {
            if (fine.isEscalated)
                return;

            fine.isEscalated = true;
            fine.amount *= 2; // Удвоить сумму

            OnFineEscalated?.Invoke(fine);

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("Штраф увеличен!", 
                    $"Неоплаченный штраф увеличен до ${fine.amount}", NotificationType.Critical);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Эскалирован штраф {fine.id}: новая сумма ${fine.amount}");
            }
        }

        #endregion

        #region Arrest Warning System

        /// <summary>
        /// Выдать предупреждение об аресте
        /// </summary>
        private void IssueArrestWarning()
        {
            isArrestWarning = true;
            arrestWarningTime = Time.time;

            OnArrestWarningIssued?.Invoke();

            if (notificationSystem != null)
            {
                notificationSystem.ShowNotification("ПРЕДУПРЕЖДЕНИЕ ОБ АРЕСТЕ!", 
                    $"У вас есть 24 часа чтобы оплатить штрафы или вас арестуют! " +
                    $"Общая сумма: ${TotalUnpaidAmount}", NotificationType.Critical);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Выдано предупреждение об аресте. Неоплаченных штрафов: {unpaidFines.Count}");
            }
        }

        /// <summary>
        /// Проверить снятие предупреждения об аресте
        /// </summary>
        private void CheckRemoveArrestWarning()
        {
            if (!isArrestWarning)
                return;

            // Снять предупреждение если количество штрафов снизилось
            if (unpaidFines.Count < maxUnpaidFines / 2 && TotalUnpaidAmount < 1000)
            {
                isArrestWarning = false;
                
                if (notificationSystem != null)
                {
                    notificationSystem.ShowNotification("Угроза ареста снята", 
                        "Вы оплатили достаточно штрафов", NotificationType.Info);
                }

                if (showDebugInfo)
                {
                    Debug.Log("Предупреждение об аресте снято");
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Рассчитать общую сумму неоплаченных штрафов
        /// </summary>
        private double CalculateTotalUnpaidAmount()
        {
            double total = 0;
            foreach (var fine in unpaidFines)
            {
                total += fine.amount;
            }
            return total;
        }

        /// <summary>
        /// Рассчитать общее количество нарушений
        /// </summary>
        private int CalculateTotalViolations()
        {
            int total = 0;
            foreach (var count in violationHistory.Values)
            {
                total += count;
            }
            return total;
        }

        /// <summary>
        /// Получить описание нарушения
        /// </summary>
        private string GetViolationDescription(ViolationType violationType)
        {
            switch (violationType)
            {
                case ViolationType.MinorViolation: return "Мелкое нарушение";
                case ViolationType.PublicDisorder: return "Нарушение общественного порядка";
                case ViolationType.Vagrancy: return "Бродяжничество";
                case ViolationType.Theft: return "Кража";
                case ViolationType.Aggression: return "Агрессивное поведение";
                case ViolationType.Vandalism: return "Вандализм";
                case ViolationType.Trespassing: return "Проникновение на частную территорию";
                case ViolationType.IllegalBusiness: return "Незаконная торговля";
                case ViolationType.BribeAttempt: return "Попытка дачи взятки";
                default: return violationType.ToString();
            }
        }

        /// <summary>
        /// Получить статистику нарушений игрока
        /// </summary>
        public ViolationStats GetViolationStats()
        {
            return new ViolationStats
            {
                violationHistory = new Dictionary<ViolationType, int>(violationHistory),
                totalUnpaidFines = unpaidFines.Count,
                totalPaidFines = paidFines.Count,
                totalUnpaidAmount = TotalUnpaidAmount,
                totalViolations = TotalViolations,
                isArrestWarning = isArrestWarning
            };
        }

        /// <summary>
        /// Можно ли совершить действие без риска нарушения
        /// </summary>
        public bool CanPerformActionSafely(ViolationType potentialViolation, Location location)
        {
            float detectionChance = CalculateDetectionChance(potentialViolation, location);
            return detectionChance < 0.3f; // Низкий риск
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public FineSystemSaveData GetSaveData()
        {
            return new FineSystemSaveData
            {
                unpaidFines = new List<Fine>(unpaidFines),
                paidFines = new List<Fine>(paidFines),
                violationHistory = new Dictionary<ViolationType, int>(violationHistory),
                lastViolationTime = new Dictionary<ViolationType, float>(lastViolationTime),
                isArrestWarning = isArrestWarning,
                arrestWarningTime = arrestWarningTime
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(FineSystemSaveData data)
        {
            unpaidFines = data.unpaidFines ?? new List<Fine>();
            paidFines = data.paidFines ?? new List<Fine>();
            violationHistory = data.violationHistory ?? new Dictionary<ViolationType, int>();
            lastViolationTime = data.lastViolationTime ?? new Dictionary<ViolationType, float>();
            isArrestWarning = data.isArrestWarning;
            arrestWarningTime = data.arrestWarningTime;

            if (showDebugInfo)
            {
                Debug.Log($"FineSystem загружен. Неоплаченных штрафов: {unpaidFines.Count}, " +
                         $"Оплаченных: {paidFines.Count}, Всего нарушений: {TotalViolations}");
            }
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Issue Test Fine")]
        private void DebugIssueTestFine()
        {
            CommitViolation(ViolationType.MinorViolation);
        }

        [ContextMenu("Issue Multiple Fines")]
        private void DebugIssueMultipleFines()
        {
            var violations = new[] { 
                ViolationType.MinorViolation, ViolationType.PublicDisorder, 
                ViolationType.Vagrancy, ViolationType.Theft 
            };
            
            foreach (var violation in violations)
            {
                CommitViolation(violation);
            }
        }

        [ContextMenu("Pay All Fines")]
        private void DebugPayAllFines()
        {
            PayAllFines();
        }

        [ContextMenu("Force Arrest Warning")]
        private void DebugForceArrestWarning()
        {
            IssueArrestWarning();
        }

        [ContextMenu("Show Fine Stats")]
        private void DebugShowFineStats()
        {
            var stats = GetViolationStats();
            Debug.Log($"Статистика штрафов:");
            Debug.Log($"Неоплаченных: {stats.totalUnpaidFines} (${stats.totalUnpaidAmount})");
            Debug.Log($"Оплаченных: {stats.totalPaidFines}");
            Debug.Log($"Всего нарушений: {stats.totalViolations}");
            Debug.Log($"Предупреждение об аресте: {stats.isArrestWarning}");
        }

        #endregion
    }

    /// <summary>
    /// Класс штрафа
    /// </summary>
    [System.Serializable]
    public class Fine
    {
        public string id;                       // Уникальный ID
        public ViolationType violationType;     // Тип нарушения
        public double amount;                   // Сумма штрафа
        public Location location;               // Место нарушения
        public float issueTime;                 // Время выписки
        public float dueTime;                   // Срок оплаты
        public float paymentTime;               // Время оплаты
        public bool isPaid;                     // Оплачен ли
        public bool isEscalated;                // Эскалирован ли
        public string description;              // Описание

        /// <summary>
        /// Просрочен ли штраф
        /// </summary>
        public bool IsOverdue => !isPaid && Time.time > dueTime;

        /// <summary>
        /// Дней до просрочки
        /// </summary>
        public int DaysUntilDue => Mathf.Max(0, Mathf.FloorToInt((dueTime - Time.time) / 86400f));
    }

    /// <summary>
    /// Статистика нарушений
    /// </summary>
    [System.Serializable]
    public class ViolationStats
    {
        public Dictionary<ViolationType, int> violationHistory;
        public int totalUnpaidFines;
        public int totalPaidFines;
        public double totalUnpaidAmount;
        public int totalViolations;
        public bool isArrestWarning;
    }

    /// <summary>
    /// Данные FineSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class FineSystemSaveData
    {
        public List<Fine> unpaidFines;
        public List<Fine> paidFines;
        public Dictionary<ViolationType, int> violationHistory;
        public Dictionary<ViolationType, float> lastViolationTime;
        public bool isArrestWarning;
        public float arrestWarningTime;
    }
}