using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система работы и трудоустройства
    /// </summary>
    public class JobSystem : MonoBehaviour
    {
        [Header("Настройки работы")]
        [SerializeField] private bool enableJobSystem = true;
        [SerializeField] private float workSessionDuration = 8f; // 8 часов
        [SerializeField] private float fatigueRate = 10f; // Усталость за час работы
        [SerializeField] private int maxJobsPerDay = 2;

        [Header("Звуки")]
        [SerializeField] private AudioClip jobStartSound;
        [SerializeField] private AudioClip jobCompleteSound;
        [SerializeField] private AudioClip jobFailSound;

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LevelSystem levelSystem;
        private AudioSource audioSource;

        // Состояние работы
        private Job currentJob;
        private bool isWorking = false;
        private float workTimeRemaining = 0f;
        private int jobsCompletedToday = 0;
        private DateTime lastWorkDay = DateTime.MinValue;

        // Доступные работы
        private List<Job> availableJobs = new List<Job>();
        private List<Job> completedJobs = new List<Job>();

        // События
        public event Action<Job> OnJobStarted;
        public event Action<Job, JobResult> OnJobCompleted;
        public event Action<Job> OnJobFailed;
        public event Action<List<Job>> OnAvailableJobsUpdated;

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            playerStats = GetComponent<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = GetComponent<SkillSystem>();
            levelSystem = GetComponent<LevelSystem>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Инициализация работ
            InitializeJobs();
        }

        private void Start()
        {
            if (!enableJobSystem)
            {
                enabled = false;
                return;
            }

            if (playerStats == null || moneySystem == null)
            {
                Debug.LogError("JobSystem: Не найдены необходимые компоненты!");
                enabled = false;
                return;
            }

            // Подписка на события
            SubscribeToEvents();

            // Проверка нового дня
            CheckNewDay();

            Debug.Log("JobSystem инициализирован");
        }

        private void Update()
        {
            if (isWorking)
            {
                UpdateWork();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация доступных работ
        /// </summary>
        private void InitializeJobs()
        {
            CreateJobs();
            UpdateAvailableJobs();
        }

        /// <summary>
        /// Создание всех типов работ
        /// </summary>
        private void CreateJobs()
        {
            availableJobs.Clear();

            // Уборщик
            availableJobs.Add(new Job(
                "Уборщик офисов", JobType.Cleaner, 15, 1,
                "Уборка офисных помещений",
                new Dictionary<SkillType, int>(),
                new Dictionary<StatType, float> { { StatType.Health, -5 }, { StatType.Mood, -10 } },
                WorkShift.Morning, 6f
            ));

            availableJobs.Add(new Job(
                "Уборщик торгового центра", JobType.Cleaner, 20, 3,
                "Уборка и поддержание чистоты в торговом центре",
                new Dictionary<SkillType, int>(),
                new Dictionary<StatType, float> { { StatType.Health, -8 }, { StatType.Mood, -15 } },
                WorkShift.Evening, 8f
            ));

            // Охранник
            availableJobs.Add(new Job(
                "Охранник склада", JobType.Security, 25, 5,
                "Охрана складских помещений",
                new Dictionary<SkillType, int> { { SkillType.Fitness, 10 } },
                new Dictionary<StatType, float> { { StatType.Health, -3 }, { StatType.Mood, -5 } },
                WorkShift.Night, 12f
            ));

            availableJobs.Add(new Job(
                "Охранник банка", JobType.Security, 45, 15,
                "Охрана банковского учреждения",
                new Dictionary<SkillType, int> { { SkillType.Fitness, 25 }, { SkillType.Education, 10 } },
                new Dictionary<StatType, float> { { StatType.Health, -5 }, { StatType.Mood, 5 } },
                WorkShift.Day, 8f
            ));

            // Продавец
            availableJobs.Add(new Job(
                "Продавец-консультант", JobType.Seller, 30, 8,
                "Консультация покупателей и продажа товаров",
                new Dictionary<SkillType, int> { { SkillType.Charisma, 15 } },
                new Dictionary<StatType, float> { { StatType.Mood, 10 } },
                WorkShift.Day, 8f
            ));

            availableJobs.Add(new Job(
                "Продавец премиум товаров", JobType.Seller, 60, 20,
                "Продажа дорогих товаров состоятельным клиентам",
                new Dictionary<SkillType, int> { { SkillType.Charisma, 35 }, { SkillType.Education, 15 } },
                new Dictionary<StatType, float> { { StatType.Mood, 20 } },
                WorkShift.Day, 8f
            ));

            // Менеджер
            availableJobs.Add(new Job(
                "Офис-менеджер", JobType.Manager, 80, 25,
                "Управление офисными процессами",
                new Dictionary<SkillType, int> { { SkillType.Education, 30 }, { SkillType.Charisma, 20 } },
                new Dictionary<StatType, float> { { StatType.Mood, 15 } },
                WorkShift.Day, 8f
            ));

            availableJobs.Add(new Job(
                "Менеджер по продажам", JobType.Manager, 120, 30,
                "Управление отделом продаж",
                new Dictionary<SkillType, int> { { SkillType.Business, 25 }, { SkillType.Charisma, 30 } },
                new Dictionary<StatType, float> { { StatType.Mood, 25 } },
                WorkShift.Day, 9f
            ));

            // Бизнесмен
            availableJobs.Add(new Job(
                "Консультант по бизнесу", JobType.Businessman, 200, 35,
                "Консультирование малого бизнеса",
                new Dictionary<SkillType, int> { { SkillType.Business, 50 }, { SkillType.Education, 40 } },
                new Dictionary<StatType, float> { { StatType.Mood, 30 } },
                WorkShift.Day, 6f
            ));

            availableJobs.Add(new Job(
                "Инвестиционный консультант", JobType.Businessman, 350, 40,
                "Консультирование по инвестициям",
                new Dictionary<SkillType, int> { { SkillType.Business, 70 }, { SkillType.Education, 60 } },
                new Dictionary<StatType, float> { { StatType.Mood, 40 } },
                WorkShift.Day, 8f
            ));

            // CEO
            availableJobs.Add(new Job(
                "Исполнительный директор", JobType.CEO, 1000, 45,
                "Управление крупной компанией",
                new Dictionary<SkillType, int> 
                { 
                    { SkillType.Business, 90 }, 
                    { SkillType.Education, 80 },
                    { SkillType.Charisma, 70 }
                },
                new Dictionary<StatType, float> { { StatType.Mood, 50 } },
                WorkShift.Day, 10f
            ));
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnSkillUpgraded += OnSkillUpgraded;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnSkillUpgraded -= OnSkillUpgraded;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить доступные работы
        /// </summary>
        public List<Job> GetAvailableJobs()
        {
            UpdateAvailableJobs();
            return availableJobs.Where(job => IsJobAvailable(job)).ToList();
        }

        /// <summary>
        /// Получить работы по типу
        /// </summary>
        public List<Job> GetJobsByType(JobType jobType)
        {
            return availableJobs.Where(job => job.jobType == jobType && IsJobAvailable(job)).ToList();
        }

        /// <summary>
        /// Проверить доступность работы
        /// </summary>
        public bool IsJobAvailable(Job job)
        {
            // Проверка уровня
            if (playerStats.Level < job.levelRequirement)
                return false;

            // Проверка навыков
            if (skillSystem != null && !skillSystem.CheckSkillRequirements(job.skillRequirements))
                return false;

            // Проверка лимита работ в день
            if (jobsCompletedToday >= maxJobsPerDay)
                return false;

            // Проверка здоровья
            if (playerStats.Health < 20)
                return false;

            return true;
        }

        /// <summary>
        /// Начать работу
        /// </summary>
        public bool StartJob(Job job)
        {
            if (isWorking)
            {
                Debug.Log("Уже работаете!");
                return false;
            }

            if (!IsJobAvailable(job))
            {
                Debug.Log($"Работа {job.title} недоступна");
                return false;
            }

            // Начало работы
            currentJob = job;
            isWorking = true;
            workTimeRemaining = job.duration * 3600f; // Перевод в секунды

            // Эффекты
            PlayJobStartSound();
            OnJobStarted?.Invoke(job);
            
            GameEvents.TriggerNotification(
                $"Начата работа: {job.title}",
                NotificationType.Info
            );

            Debug.Log($"Начата работа: {job.title} (Продолжительность: {job.duration}ч)");
            return true;
        }

        /// <summary>
        /// Завершить работу досрочно
        /// </summary>
        public void QuitJob()
        {
            if (!isWorking) return;

            // Частичная оплата
            float completionRatio = 1f - (workTimeRemaining / (currentJob.duration * 3600f));
            double partialPayment = currentJob.payment * completionRatio * 0.5; // 50% от пропорциональной оплаты

            if (partialPayment > 0)
            {
                moneySystem.AddMoney(partialPayment, $"Partial payment: {currentJob.title}");
            }

            // Завершение
            isWorking = false;
            currentJob = null;
            workTimeRemaining = 0f;

            GameEvents.TriggerNotification("Работа завершена досрочно", NotificationType.Warning);
            Debug.Log($"Работа завершена досрочно. Получено: {GameUtils.FormatMoney(partialPayment)}");
        }

        /// <summary>
        /// Получить текущую работу
        /// </summary>
        public Job GetCurrentJob()
        {
            return currentJob;
        }

        /// <summary>
        /// Проверить, работает ли игрок
        /// </summary>
        public bool IsWorking()
        {
            return isWorking;
        }

        /// <summary>
        /// Получить прогресс текущей работы (0-1)
        /// </summary>
        public float GetWorkProgress()
        {
            if (!isWorking || currentJob == null) return 0f;
            
            float totalTime = currentJob.duration * 3600f;
            return 1f - (workTimeRemaining / totalTime);
        }

        /// <summary>
        /// Получить оставшееся время работы в секундах
        /// </summary>
        public float GetRemainingWorkTime()
        {
            return workTimeRemaining;
        }

        /// <summary>
        /// Получить количество выполненных работ сегодня
        /// </summary>
        public int GetJobsCompletedToday()
        {
            return jobsCompletedToday;
        }

        /// <summary>
        /// Получить историю выполненных работ
        /// </summary>
        public List<Job> GetCompletedJobs()
        {
            return new List<Job>(completedJobs);
        }

        /// <summary>
        /// Получить название типа работы
        /// </summary>
        public string GetJobTypeName(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.None: return "Безработный";
                case JobType.Cleaner: return "Уборщик";
                case JobType.Security: return "Охранник";
                case JobType.Seller: return "Продавец";
                case JobType.Manager: return "Менеджер";
                case JobType.Businessman: return "Бизнесмен";
                case JobType.CEO: return "Директор";
                default: return jobType.ToString();
            }
        }

        /// <summary>
        /// Получить название рабочей смены
        /// </summary>
        public string GetShiftName(WorkShift shift)
        {
            switch (shift)
            {
                case WorkShift.Morning: return "Утренняя смена";
                case WorkShift.Day: return "Дневная смена";
                case WorkShift.Evening: return "Вечерняя смена";
                case WorkShift.Night: return "Ночная смена";
                default: return shift.ToString();
            }
        }

        #endregion

        #region Work Processing

        /// <summary>
        /// Обновление процесса работы
        /// </summary>
        private void UpdateWork()
        {
            if (!isWorking || currentJob == null) return;

            workTimeRemaining -= Time.deltaTime;

            // Применение усталости
            float fatigueThisFrame = (fatigueRate / 3600f) * Time.deltaTime;
            ApplyWorkFatigue(fatigueThisFrame);

            // Проверка завершения
            if (workTimeRemaining <= 0)
            {
                CompleteWork();
            }

            // Проверка провала (низкое здоровье)
            if (playerStats.Health <= 0)
            {
                FailWork("Здоровье упало до критического уровня");
            }
        }

        /// <summary>
        /// Применение усталости от работы
        /// </summary>
        private void ApplyWorkFatigue(float fatigue)
        {
            // Применение эффектов работы
            if (currentJob.statEffects != null)
            {
                foreach (var effect in currentJob.statEffects)
                {
                    float effectThisFrame = (effect.Value / currentJob.duration) * (fatigue / fatigueRate) * 3600f;
                    ApplyStatEffect(effect.Key, effectThisFrame);
                }
            }
        }

        /// <summary>
        /// Завершение работы
        /// </summary>
        private void CompleteWork()
        {
            if (currentJob == null) return;

            // Расчет оплаты с бонусами
            double payment = CalculateJobPayment(currentJob);
            
            // Выплата
            moneySystem.AddMoney(payment, $"Job payment: {currentJob.title}");
            
            // Опыт за работу
            if (levelSystem != null)
            {
                int experience = GetJobExperience(currentJob);
                levelSystem.AddExperience(experience, "Job Completed");
            }

            // Статистика
            jobsCompletedToday++;
            completedJobs.Add(currentJob);

            // Результат работы
            JobResult result = new JobResult
            {
                job = currentJob,
                payment = payment,
                experienceGained = GetJobExperience(currentJob),
                completionTime = DateTime.Now,
                wasSuccessful = true
            };

            // Эффекты и события
            PlayJobCompleteSound();
            OnJobCompleted?.Invoke(currentJob, result);
            
            GameEvents.TriggerJobCompleted(new JobEventData
            {
                jobType = currentJob.jobType,
                payment = payment,
                hoursWorked = (int)currentJob.duration
            });

            GameEvents.TriggerNotification(
                $"Работа завершена! Получено: {GameUtils.FormatMoney(payment)}",
                NotificationType.Success
            );

            Debug.Log($"Работа завершена: {currentJob.title}. " +
                     $"Получено: {GameUtils.FormatMoney(payment)}, Опыт: {GetJobExperience(currentJob)}");

            // Очистка состояния
            isWorking = false;
            currentJob = null;
            workTimeRemaining = 0f;
        }

        /// <summary>
        /// Провал работы
        /// </summary>
        private void FailWork(string reason)
        {
            if (currentJob == null) return;

            // Результат провала
            JobResult result = new JobResult
            {
                job = currentJob,
                payment = 0,
                experienceGained = 0,
                completionTime = DateTime.Now,
                wasSuccessful = false,
                failureReason = reason
            };

            // Эффекты и события
            PlayJobFailSound();
            OnJobFailed?.Invoke(currentJob);
            
            GameEvents.TriggerNotification(
                $"Работа провалена: {reason}",
                NotificationType.Danger
            );

            Debug.Log($"Работа провалена: {currentJob.title}. Причина: {reason}");

            // Очистка состояния
            isWorking = false;
            currentJob = null;
            workTimeRemaining = 0f;
        }

        /// <summary>
        /// Расчет оплаты за работу
        /// </summary>
        private double CalculateJobPayment(Job job)
        {
            double basePayment = job.payment;
            
            // Бонус от навыков
            if (skillSystem != null)
            {
                switch (job.jobType)
                {
                    case JobType.Seller:
                    case JobType.Manager:
                        basePayment *= skillSystem.GetCharismaEarningsModifier();
                        break;
                    
                    case JobType.Businessman:
                    case JobType.CEO:
                        basePayment *= skillSystem.GetBusinessIncomeModifier();
                        break;
                }
            }

            // Случайный бонус от удачи
            if (skillSystem != null)
            {
                float luckBonus = skillSystem.GetLuckModifier();
                if (UnityEngine.Random.Range(0f, 1f) < luckBonus)
                {
                    basePayment *= 1.5; // 50% бонус от удачи
                    GameEvents.TriggerNotification("Удачный день! Бонус к зарплате!", NotificationType.Achievement);
                }
            }

            return basePayment;
        }

        /// <summary>
        /// Получить опыт за работу
        /// </summary>
        private int GetJobExperience(Job job)
        {
            int baseExp = (int)(job.payment / 10); // 1 опыт за каждые 10 денег зарплаты
            
            // Минимум 5 опыта
            return Mathf.Max(5, baseExp);
        }

        /// <summary>
        /// Применить эффект на характеристику
        /// </summary>
        private void ApplyStatEffect(StatType statType, float value)
        {
            switch (statType)
            {
                case StatType.Health:
                    playerStats.AddHealth(value);
                    break;
                
                case StatType.Hunger:
                    playerStats.AddHunger(value);
                    break;
                
                case StatType.Mood:
                    playerStats.AddMood(value);
                    break;
            }
        }

        #endregion

        #region Job Management

        /// <summary>
        /// Обновление доступных работ
        /// </summary>
        private void UpdateAvailableJobs()
        {
            OnAvailableJobsUpdated?.Invoke(GetAvailableJobs());
        }

        /// <summary>
        /// Проверка нового дня
        /// </summary>
        private void CheckNewDay()
        {
            DateTime today = DateTime.Today;
            if (lastWorkDay.Date != today)
            {
                jobsCompletedToday = 0;
                lastWorkDay = today;
                Debug.Log("Новый рабочий день! Лимит работ сброшен.");
            }
        }

        #endregion

        #region Audio

        /// <summary>
        /// Воспроизвести звук начала работы
        /// </summary>
        private void PlayJobStartSound()
        {
            if (jobStartSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jobStartSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук завершения работы
        /// </summary>
        private void PlayJobCompleteSound()
        {
            if (jobCompleteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jobCompleteSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук провала работы
        /// </summary>
        private void PlayJobFailSound()
        {
            if (jobFailSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jobFailSound);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            UpdateAvailableJobs();
        }

        /// <summary>
        /// Обработчик улучшения навыков
        /// </summary>
        private void OnSkillUpgraded(SkillUpgradeEventData data)
        {
            UpdateAvailableJobs();
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public JobSystemSaveData GetSaveData()
        {
            var completedJobsData = new List<CompletedJobData>();
            foreach (var job in completedJobs)
            {
                completedJobsData.Add(new CompletedJobData
                {
                    title = job.title,
                    jobType = job.jobType.ToString(),
                    payment = job.payment,
                    completionTime = DateTime.Now.ToBinary()
                });
            }

            return new JobSystemSaveData
            {
                currentJobTitle = currentJob?.title ?? "",
                isWorking = this.isWorking,
                workTimeRemaining = this.workTimeRemaining,
                jobsCompletedToday = this.jobsCompletedToday,
                lastWorkDay = this.lastWorkDay.ToBinary(),
                completedJobs = completedJobsData
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(JobSystemSaveData data)
        {
            if (data != null)
            {
                // Восстановление текущей работы
                if (!string.IsNullOrEmpty(data.currentJobTitle) && data.isWorking)
                {
                    currentJob = availableJobs.FirstOrDefault(j => j.title == data.currentJobTitle);
                    if (currentJob != null)
                    {
                        isWorking = true;
                        workTimeRemaining = data.workTimeRemaining;
                    }
                }

                // Восстановление статистики
                jobsCompletedToday = data.jobsCompletedToday;
                lastWorkDay = DateTime.FromBinary(data.lastWorkDay);

                // Восстановление завершенных работ
                if (data.completedJobs != null)
                {
                    completedJobs.Clear();
                    foreach (var jobData in data.completedJobs)
                    {
                        var job = availableJobs.FirstOrDefault(j => j.title == jobData.title);
                        if (job != null)
                        {
                            completedJobs.Add(job);
                        }
                    }
                }

                // Проверка нового дня
                CheckNewDay();
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Показать доступные работы")]
        private void DebugShowAvailableJobs()
        {
            var jobs = GetAvailableJobs();
            Debug.Log($"Доступно работ: {jobs.Count}");
            foreach (var job in jobs)
            {
                Debug.Log($"{job.title} - {GameUtils.FormatMoney(job.payment)}/час " +
                         $"(Уровень: {job.levelRequirement}, Продолжительность: {job.duration}ч)");
            }
        }

        [ContextMenu("Начать случайную работу")]
        private void DebugStartRandomJob()
        {
            var availableJobs = GetAvailableJobs();
            if (availableJobs.Count > 0)
            {
                var randomJob = availableJobs[UnityEngine.Random.Range(0, availableJobs.Count)];
                StartJob(randomJob);
            }
            else
            {
                Debug.Log("Нет доступных работ");
            }
        }

        [ContextMenu("Завершить текущую работу")]
        private void DebugCompleteCurrentJob()
        {
            if (isWorking)
            {
                workTimeRemaining = 0f;
            }
            else
            {
                Debug.Log("Не работаете");
            }
        }

        [ContextMenu("Показать статистику работы")]
        private void DebugShowJobStats()
        {
            Debug.Log($"Текущая работа: {currentJob?.title ?? "Нет"}");
            Debug.Log($"Работает: {isWorking}");
            Debug.Log($"Работ сегодня: {jobsCompletedToday}/{maxJobsPerDay}");
            Debug.Log($"Всего завершено работ: {completedJobs.Count}");
            
            if (isWorking)
            {
                Debug.Log($"Прогресс: {GetWorkProgress():P1}");
                Debug.Log($"Осталось времени: {workTimeRemaining / 3600f:F1} часов");
            }
        }

        #endregion
    }

    /// <summary>
    /// Работа
    /// </summary>
    [System.Serializable]
    public class Job
    {
        public string title;
        public JobType jobType;
        public double payment; // За час
        public int levelRequirement;
        public string description;
        public Dictionary<SkillType, int> skillRequirements;
        public Dictionary<StatType, float> statEffects; // Эффекты за час работы
        public WorkShift shift;
        public float duration; // Продолжительность в часах

        public Job(string title, JobType jobType, double payment, int levelRequirement,
                  string description, Dictionary<SkillType, int> skillRequirements,
                  Dictionary<StatType, float> statEffects, WorkShift shift, float duration)
        {
            this.title = title;
            this.jobType = jobType;
            this.payment = payment;
            this.levelRequirement = levelRequirement;
            this.description = description;
            this.skillRequirements = skillRequirements ?? new Dictionary<SkillType, int>();
            this.statEffects = statEffects ?? new Dictionary<StatType, float>();
            this.shift = shift;
            this.duration = duration;
        }
    }

    /// <summary>
    /// Результат выполнения работы
    /// </summary>
    [System.Serializable]
    public class JobResult
    {
        public Job job;
        public double payment;
        public int experienceGained;
        public DateTime completionTime;
        public bool wasSuccessful;
        public string failureReason;
    }


    /// <summary>
    /// Данные системы работы для сохранения
    /// </summary>
    [System.Serializable]

}
