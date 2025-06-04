using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система образования и обучения
    /// </summary>
    public class EducationSystem : MonoBehaviour
    {
        [Header("Настройки образования")]
        [SerializeField] private bool enableEducationSystem = true;
        [SerializeField] private float studySpeedMultiplier = 1f;
        [SerializeField] private int maxCoursesPerDay = 1;

        [Header("Звуки")]
        [SerializeField] private AudioClip courseStartSound;
        [SerializeField] private AudioClip courseCompleteSound;
        [SerializeField] private AudioClip degreeObtainedSound;

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private LevelSystem levelSystem;
        private AudioSource audioSource;

        // Состояние обучения
        private EducationCourse currentCourse;
        private bool isStudying = false;
        private float studyTimeRemaining = 0f;
        private int coursesCompletedToday = 0;
        private DateTime lastStudyDay = DateTime.MinValue;

        // Доступные курсы и достижения
        private List<EducationCourse> availableCourses = new List<EducationCourse>();
        private List<EducationDegree> obtainedDegrees = new List<EducationDegree>();
        private List<EducationCourse> completedCourses = new List<EducationCourse>();

        // События
        public event Action<EducationCourse> OnCourseStarted;
        public event Action<EducationCourse> OnEducationStarted;
        public event Action<EducationCourse, EducationResult> OnCourseCompleted;
        public event Action<EducationCourse, EducationResult> OnEducationCompleted;
        public event Action<EducationDegree> OnDegreeObtained;
        public event Action<List<EducationCourse>> OnAvailableCoursesUpdated;

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

            // Инициализация курсов
            InitializeCourses();
        }

        private void Start()
        {
            if (!enableEducationSystem)
            {
                enabled = false;
                return;
            }

            if (playerStats == null || moneySystem == null)
            {
                Debug.LogError("EducationSystem: Не найдены необходимые компоненты!");
                enabled = false;
                return;
            }

            // Подписка на события
            SubscribeToEvents();

            // Проверка нового дня
            CheckNewDay();

            Debug.Log("EducationSystem инициализирован");
        }

        private void Update()
        {
            if (isStudying)
            {
                UpdateStudy();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация доступных курсов
        /// </summary>
        private void InitializeCourses()
        {
            CreateCourses();
            UpdateAvailableCourses();
        }

        /// <summary>
        /// Создание всех курсов
        /// </summary>
        private void CreateCourses()
        {
            availableCourses.Clear();

            // Базовые навыки
            availableCourses.Add(new EducationCourse(
                "Базовая грамотность", EducationType.BasicSkills, 100, 1,
                "Основы чтения, письма и арифметики",
                new Dictionary<SkillType, int>(),
                new Dictionary<SkillType, int> { { SkillType.Education, 10 } },
                2f, "Сертификат базовой грамотности"
            ));

            availableCourses.Add(new EducationCourse(
                "Коммуникативные навыки", EducationType.BasicSkills, 250, 5,
                "Развитие навыков общения и презентации",
                new Dictionary<SkillType, int> { { SkillType.Education, 5 } },
                new Dictionary<SkillType, int> { { SkillType.Charisma, 15 } },
                4f, "Сертификат коммуникативных навыков"
            ));

            // Компьютерные курсы
            availableCourses.Add(new EducationCourse(
                "Основы компьютерной грамотности", EducationType.ComputerCourse, 500, 8,
                "Изучение основ работы с компьютером",
                new Dictionary<SkillType, int> { { SkillType.Education, 10 } },
                new Dictionary<SkillType, int> { { SkillType.Education, 20 } },
                6f, "Сертификат компьютерной грамотности"
            ));

            availableCourses.Add(new EducationCourse(
                "Веб-дизайн", EducationType.ComputerCourse, 1500, 15,
                "Создание веб-сайтов и интерфейсов",
                new Dictionary<SkillType, int> { { SkillType.Education, 25 } },
                new Dictionary<SkillType, int> { { SkillType.Education, 30 }, { SkillType.Business, 10 } },
                12f, "Диплом веб-дизайнера"
            ));

            availableCourses.Add(new EducationCourse(
                "Программирование", EducationType.ComputerCourse, 3000, 20,
                "Изучение основ программирования",
                new Dictionary<SkillType, int> { { SkillType.Education, 35 } },
                new Dictionary<SkillType, int> { { SkillType.Education, 40 }, { SkillType.Business, 15 } },
                20f, "Диплом программиста"
            ));

            // Языковые курсы
            availableCourses.Add(new EducationCourse(
                "Английский язык (базовый)", EducationType.LanguageCourse, 800, 10,
                "Основы английского языка",
                new Dictionary<SkillType, int> { { SkillType.Education, 15 } },
                new Dictionary<SkillType, int> { { SkillType.Charisma, 20 }, { SkillType.Education, 15 } },
                8f, "Сертификат английского языка (A2)"
            ));

            availableCourses.Add(new EducationCourse(
                "Английский язык (продвинутый)", EducationType.LanguageCourse, 2000, 20,
                "Продвинутый уровень английского языка",
                new Dictionary<SkillType, int> { { SkillType.Education, 30 }, { SkillType.Charisma, 15 } },
                new Dictionary<SkillType, int> { { SkillType.Charisma, 30 }, { SkillType.Business, 20 } },
                16f, "Диплом английского языка (C1)"
            ));

            availableCourses.Add(new EducationCourse(
                "Деловой английский", EducationType.LanguageCourse, 3500, 25,
                "Английский язык для бизнеса",
                new Dictionary<SkillType, int> { { SkillType.Education, 40 }, { SkillType.Business, 15 } },
                new Dictionary<SkillType, int> { { SkillType.Business, 35 }, { SkillType.Charisma, 25 } },
                12f, "Диплом делового английского"
            ));

            // Бизнес-школа
            availableCourses.Add(new EducationCourse(
                "Основы предпринимательства", EducationType.BusinessSchool, 2500, 20,
                "Базовые принципы ведения бизнеса",
                new Dictionary<SkillType, int> { { SkillType.Education, 25 } },
                new Dictionary<SkillType, int> { { SkillType.Business, 30 } },
                15f, "Сертификат предпринимателя"
            ));

            availableCourses.Add(new EducationCourse(
                "Финансовый менеджмент", EducationType.BusinessSchool, 5000, 30,
                "Управление финансами предприятия",
                new Dictionary<SkillType, int> { { SkillType.Business, 25 }, { SkillType.Education, 35 } },
                new Dictionary<SkillType, int> { { SkillType.Business, 40 } },
                20f, "Диплом финансового менеджера"
            ));

            availableCourses.Add(new EducationCourse(
                "Маркетинг и реклама", EducationType.BusinessSchool, 4000, 25,
                "Стратегии продвижения и рекламы",
                new Dictionary<SkillType, int> { { SkillType.Charisma, 30 }, { SkillType.Education, 30 } },
                new Dictionary<SkillType, int> { { SkillType.Charisma, 35 }, { SkillType.Business, 30 } },
                18f, "Диплом маркетолога"
            ));

            // Университет
            availableCourses.Add(new EducationCourse(
                "Экономическое образование", EducationType.University, 15000, 35,
                "Высшее экономическое образование",
                new Dictionary<SkillType, int> { { SkillType.Education, 50 } },
                new Dictionary<SkillType, int> { { SkillType.Business, 50 }, { SkillType.Education, 30 } },
                40f, "Диплом экономиста"
            ));

            availableCourses.Add(new EducationCourse(
                "Юридическое образование", EducationType.University, 18000, 35,
                "Высшее юридическое образование",
                new Dictionary<SkillType, int> { { SkillType.Education, 50 }, { SkillType.Charisma, 30 } },
                new Dictionary<SkillType, int> { { SkillType.Charisma, 50 }, { SkillType.Education, 40 } },
                45f, "Диплом юриста"
            ));

            // MBA
            availableCourses.Add(new EducationCourse(
                "MBA - Мастер делового администрирования", EducationType.MBA, 50000, 40,
                "Престижная программа MBA",
                new Dictionary<SkillType, int> 
                { 
                    { SkillType.Education, 70 }, 
                    { SkillType.Business, 60 },
                    { SkillType.Charisma, 50 }
                },
                new Dictionary<SkillType, int> 
                { 
                    { SkillType.Business, 80 }, 
                    { SkillType.Charisma, 60 },
                    { SkillType.Education, 50 }
                },
                60f, "Диплом MBA"
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
        /// Получить доступные курсы
        /// </summary>
        public List<EducationCourse> GetAvailableCourses()
        {
            UpdateAvailableCourses();
            return availableCourses.Where(course => IsCourseAvailable(course)).ToList();
        }

        /// <summary>
        /// Получить курсы по типу образования
        /// </summary>
        public List<EducationCourse> GetCoursesByType(EducationType educationType)
        {
            return availableCourses.Where(course => 
                course.educationType == educationType && IsCourseAvailable(course)).ToList();
        }

        /// <summary>
        /// Проверить доступность курса
        /// </summary>
        public bool IsCourseAvailable(EducationCourse course)
        {
            // Проверка уровня
            if (playerStats.Level < course.levelRequirement)
                return false;

            // Проверка навыков
            if (skillSystem != null && !skillSystem.CheckSkillRequirements(course.prerequisites))
                return false;

            // Проверка денег
            if (!moneySystem.CanAfford(course.cost))
                return false;

            // Проверка лимита курсов в день
            if (coursesCompletedToday >= maxCoursesPerDay)
                return false;

            // Проверка, не завершен ли уже курс
            if (completedCourses.Any(c => c.title == course.title))
                return false;

            return true;
        }

        /// <summary>
        /// Начать обучение на курсе
        /// </summary>
        public bool StartCourse(EducationCourse course)
        {
            if (isStudying)
            {
                Debug.Log("Уже обучаетесь на курсе!");
                return false;
            }

            if (!IsCourseAvailable(course))
            {
                Debug.Log($"Курс {course.title} недоступен");
                return false;
            }

            // Оплата курса
            if (!moneySystem.SpendMoney(course.cost, $"Education: {course.title}"))
            {
                Debug.Log($"Недостаточно денег для курса {course.title}");
                return false;
            }

            // Начало обучения
            currentCourse = course;
            isStudying = true;
            studyTimeRemaining = course.duration * 3600f; // Перевод в секунды

            // Эффекты
            PlayCourseStartSound();
            OnCourseStarted?.Invoke(course);
            OnEducationStarted?.Invoke(course);
            
            GameEvents.TriggerNotification(
                $"Начато обучение: {course.title}",
                NotificationType.Info
            );

            Debug.Log($"Начато обучение: {course.title} (Продолжительность: {course.duration}ч)");
            return true;
        }

        /// <summary>
        /// Прекратить обучение досрочно
        /// </summary>
        public void QuitCourse()
        {
            if (!isStudying) return;

            // Частичный возврат средств
            float completionRatio = 1f - (studyTimeRemaining / (currentCourse.duration * 3600f));
            double refund = currentCourse.cost * (1f - completionRatio) * 0.3; // 30% от оставшейся части

            if (refund > 0)
            {
                moneySystem.AddMoney(refund, $"Course refund: {currentCourse.title}");
            }

            // Завершение
            isStudying = false;
            currentCourse = null;
            studyTimeRemaining = 0f;

            GameEvents.TriggerNotification("Обучение прекращено досрочно", NotificationType.Warning);
            Debug.Log($"Обучение прекращено досрочно. Возврат средств: {GameUtils.FormatMoney((float)refund)}");
        }

        /// <summary>
        /// Получить текущий курс
        /// </summary>
        public EducationCourse GetCurrentCourse()
        {
            return currentCourse;
        }

        /// <summary>
        /// Проверить, обучается ли игрок
        /// </summary>
        public bool IsStudying()
        {
            return isStudying;
        }

        /// <summary>
        /// Получить прогресс текущего обучения (0-1)
        /// </summary>
        public float GetStudyProgress()
        {
            if (!isStudying || currentCourse == null) return 0f;

            float totalTime = currentCourse.duration * 3600f;
            return 1f - (studyTimeRemaining / totalTime);
        }

        public float GetCurrentCourseProgress()
        {
            return GetStudyProgress();
        }

        public bool CanStartCourse(EducationCourse course)
        {
            return !isStudying && IsCourseAvailable(course);
        }

        /// <summary>
        /// Получить оставшееся время обучения в секундах
        /// </summary>
        public float GetRemainingStudyTime()
        {
            return studyTimeRemaining;
        }

        /// <summary>
        /// Получить завершенные курсы
        /// </summary>
        public List<EducationCourse> GetCompletedCourses()
        {
            return new List<EducationCourse>(completedCourses);
        }

        /// <summary>
        /// Получить полученные дипломы/сертификаты
        /// </summary>
        public List<EducationDegree> GetObtainedDegrees()
        {
            return new List<EducationDegree>(obtainedDegrees);
        }

        /// <summary>
        /// Получить название типа образования
        /// </summary>
        public string GetEducationTypeName(EducationType educationType)
        {
            switch (educationType)
            {
                case EducationType.BasicSkills: return "Базовые навыки";
                case EducationType.ComputerCourse: return "Компьютерные курсы";
                case EducationType.LanguageCourse: return "Языковые курсы";
                case EducationType.BusinessSchool: return "Бизнес-школа";
                case EducationType.University: return "Университет";
                case EducationType.MBA: return "MBA программа";
                default: return educationType.ToString();
            }
        }

        /// <summary>
        /// Проверить наличие диплома
        /// </summary>
        public bool HasDegree(string degreeName)
        {
            return obtainedDegrees.Any(degree => degree.name == degreeName);
        }

        #endregion

        #region Study Processing

        /// <summary>
        /// Обновление процесса обучения
        /// </summary>
        private void UpdateStudy()
        {
            if (!isStudying || currentCourse == null) return;

            studyTimeRemaining -= Time.deltaTime * studySpeedMultiplier;

            // Применение усталости от учебы
            float fatigueThisFrame = (5f / 3600f) * Time.deltaTime; // 5 единиц усталости за час
            ApplyStudyFatigue(fatigueThisFrame);

            // Проверка завершения
            if (studyTimeRemaining <= 0)
            {
                CompleteStudy();
            }
        }

        /// <summary>
        /// Применение усталости от учебы
        /// </summary>
        private void ApplyStudyFatigue(float fatigue)
        {
            // Небольшая усталость от учебы
            playerStats.AddMood(-fatigue * 2f);
            playerStats.AddHunger(fatigue);
        }

        /// <summary>
        /// Завершение обучения
        /// </summary>
        private void CompleteStudy()
        {
            if (currentCourse == null) return;

            // Получение навыков
            if (skillSystem != null && currentCourse.skillRewards != null)
            {
                int totalSkillPoints = 0;
                foreach (var skillReward in currentCourse.skillRewards)
                {
                    // Прямое повышение уровня навыка
                    for (int i = 0; i < skillReward.Value; i++)
                    {
                        if (skillSystem.CanUpgradeSkill(skillReward.Key))
                        {
                            skillSystem.UpgradeSkill(skillReward.Key);
                            totalSkillPoints++;
                        }
                    }
                }
            }

            // Опыт за обучение
            if (levelSystem != null)
            {
                int experience = GetCourseExperience(currentCourse);
                levelSystem.AddExperience(experience, "Course Completed");
            }

            // Добавление диплома/сертификата
            if (!string.IsNullOrEmpty(currentCourse.degreeAwarded))
            {
                var degree = new EducationDegree
                {
                    name = currentCourse.degreeAwarded,
                    educationType = currentCourse.educationType,
                    dateObtained = DateTime.Now,
                    institution = GetInstitutionName(currentCourse.educationType)
                };
                
                obtainedDegrees.Add(degree);
                OnDegreeObtained?.Invoke(degree);
                PlayDegreeObtainedSound();
                
                GameEvents.TriggerNotification(
                    $"Получен диплом: {degree.name}",
                    NotificationType.Achievement
                );
            }

            // Статистика
            coursesCompletedToday++;
            completedCourses.Add(currentCourse);

            // Результат обучения
            EducationResult result = new EducationResult
            {
                course = currentCourse,
                skillPointsGained = currentCourse.skillRewards?.Values.Sum() ?? 0,
                experienceGained = GetCourseExperience(currentCourse),
                completionTime = DateTime.Now,
                degreeObtained = currentCourse.degreeAwarded
            };

            // Эффекты и события
            PlayCourseCompleteSound();
            OnCourseCompleted?.Invoke(currentCourse, result);
            OnEducationCompleted?.Invoke(currentCourse, result);
            
            GameEvents.TriggerEducationCompleted(new EducationEventData
            {
                educationType = currentCourse.educationType,
                cost = currentCourse.cost,
                skillPointsGained = result.skillPointsGained
            });

            GameEvents.TriggerNotification(
                $"Курс завершен: {currentCourse.title}",
                NotificationType.Success
            );

            Debug.Log($"Курс завершен: {currentCourse.title}. " +
                     $"Навыки улучшены, получен опыт: {GetCourseExperience(currentCourse)}");

            // Очистка состояния
            isStudying = false;
            currentCourse = null;
            studyTimeRemaining = 0f;
        }

        /// <summary>
        /// Получить опыт за курс
        /// </summary>
        private int GetCourseExperience(EducationCourse course)
        {
            // Базовый опыт на основе стоимости и продолжительности
            int baseExp = (int)(course.cost / 100 + course.duration * 2);
            
            // Минимум 10 опыта
            return Mathf.Max(10, baseExp);
        }

        /// <summary>
        /// Получить название учебного заведения
        /// </summary>
        private string GetInstitutionName(EducationType educationType)
        {
            switch (educationType)
            {
                case EducationType.BasicSkills: return "Центр базового образования";
                case EducationType.ComputerCourse: return "IT Академия";
                case EducationType.LanguageCourse: return "Языковая школа";
                case EducationType.BusinessSchool: return "Высшая школа бизнеса";
                case EducationType.University: return "Государственный университет";
                case EducationType.MBA: return "Международная школа бизнеса";
                default: return "Образовательное учреждение";
            }
        }

        #endregion

        #region Course Management

        /// <summary>
        /// Обновление доступных курсов
        /// </summary>
        private void UpdateAvailableCourses()
        {
            OnAvailableCoursesUpdated?.Invoke(GetAvailableCourses());
        }

        /// <summary>
        /// Проверка нового дня
        /// </summary>
        private void CheckNewDay()
        {
            DateTime today = DateTime.Today;
            if (lastStudyDay.Date != today)
            {
                coursesCompletedToday = 0;
                lastStudyDay = today;
                Debug.Log("Новый учебный день! Лимит курсов сброшен.");
            }
        }

        #endregion

        #region Audio

        /// <summary>
        /// Воспроизвести звук начала курса
        /// </summary>
        private void PlayCourseStartSound()
        {
            if (courseStartSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(courseStartSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук завершения курса
        /// </summary>
        private void PlayCourseCompleteSound()
        {
            if (courseCompleteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(courseCompleteSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук получения диплома
        /// </summary>
        private void PlayDegreeObtainedSound()
        {
            if (degreeObtainedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(degreeObtainedSound);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            UpdateAvailableCourses();
        }

        /// <summary>
        /// Обработчик улучшения навыков
        /// </summary>
        private void OnSkillUpgraded(SkillUpgradeEventData data)
        {
            UpdateAvailableCourses();
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public EducationSystemSaveData GetSaveData()
        {
            var completedCoursesData = new List<CompletedCourseData>();
            foreach (var course in completedCourses)
            {
                completedCoursesData.Add(new CompletedCourseData
                {
                    title = course.title,
                    educationType = course.educationType.ToString(),
                    completionTime = DateTime.Now.ToBinary()
                });
            }

            var degreesData = new List<DegreeData>();
            foreach (var degree in obtainedDegrees)
            {
                degreesData.Add(new DegreeData
                {
                    name = degree.name,
                    educationType = degree.educationType.ToString(),
                    dateObtained = degree.dateObtained.ToBinary(),
                    institution = degree.institution
                });
            }

            return new EducationSystemSaveData
            {
                currentCourseTitle = currentCourse?.title ?? "",
                isStudying = this.isStudying,
                studyTimeRemaining = this.studyTimeRemaining,
                coursesCompletedToday = this.coursesCompletedToday,
                lastStudyDay = this.lastStudyDay.ToBinary(),
                completedCourses = completedCoursesData,
                obtainedDegrees = degreesData
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(EducationSystemSaveData data)
        {
            if (data != null)
            {
                // Восстановление текущего курса
                if (!string.IsNullOrEmpty(data.currentCourseTitle) && data.isStudying)
                {
                    currentCourse = availableCourses.FirstOrDefault(c => c.title == data.currentCourseTitle);
                    if (currentCourse != null)
                    {
                        isStudying = true;
                        studyTimeRemaining = data.studyTimeRemaining;
                    }
                }

                // Восстановление статистики
                coursesCompletedToday = data.coursesCompletedToday;
                lastStudyDay = DateTime.FromBinary(data.lastStudyDay);

                // Восстановление завершенных курсов
                if (data.completedCourses != null)
                {
                    completedCourses.Clear();
                    foreach (var courseData in data.completedCourses)
                    {
                        var course = availableCourses.FirstOrDefault(c => c.title == courseData.title);
                        if (course != null)
                        {
                            completedCourses.Add(course);
                        }
                    }
                }

                // Восстановление дипломов
                if (data.obtainedDegrees != null)
                {
                    obtainedDegrees.Clear();
                    foreach (var degreeData in data.obtainedDegrees)
                    {
                        if (Enum.TryParse<EducationType>(degreeData.educationType, out EducationType eduType))
                        {
                            obtainedDegrees.Add(new EducationDegree
                            {
                                name = degreeData.name,
                                educationType = eduType,
                                dateObtained = DateTime.FromBinary(degreeData.dateObtained),
                                institution = degreeData.institution
                            });
                        }
                    }
                }

                // Проверка нового дня
                CheckNewDay();
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Показать доступные курсы")]
        private void DebugShowAvailableCourses()
        {
            var courses = GetAvailableCourses();
            Debug.Log($"Доступно курсов: {courses.Count}");
            foreach (var course in courses)
            {
                Debug.Log($"{course.title} - {GameUtils.FormatMoney((float)course.cost)} " +
                         $"(Уровень: {course.levelRequirement}, Продолжительность: {course.duration}ч)");
            }
        }

        [ContextMenu("Начать случайный курс")]
        private void DebugStartRandomCourse()
        {
            var availableCourses = GetAvailableCourses();
            if (availableCourses.Count > 0)
            {
                var randomCourse = availableCourses[UnityEngine.Random.Range(0, availableCourses.Count)];
                StartCourse(randomCourse);
            }
            else
            {
                Debug.Log("Нет доступных курсов");
            }
        }

        [ContextMenu("Завершить текущий курс")]
        private void DebugCompleteCurrentCourse()
        {
            if (isStudying)
            {
                studyTimeRemaining = 0f;
            }
            else
            {
                Debug.Log("Не обучаетесь");
            }
        }

        [ContextMenu("Показать статистику образования")]
        private void DebugShowEducationStats()
        {
            Debug.Log($"Текущий курс: {currentCourse?.title ?? "Нет"}");
            Debug.Log($"Обучается: {isStudying}");
            Debug.Log($"Курсов сегодня: {coursesCompletedToday}/{maxCoursesPerDay}");
            Debug.Log($"Всего завершено курсов: {completedCourses.Count}");
            Debug.Log($"Получено дипломов: {obtainedDegrees.Count}");
            
            if (isStudying)
            {
                Debug.Log($"Прогресс: {GetStudyProgress():P1}");
                Debug.Log($"Осталось времени: {studyTimeRemaining / 3600f:F1} часов");
            }
        }

        #endregion
    }

    /// <summary>
    /// Образовательный курс
    /// </summary>
    [System.Serializable]
    public class EducationCourse
    {
        public string title;
        public EducationType educationType;
        public double cost;
        public int levelRequirement;
        public string description;
        public Dictionary<SkillType, int> prerequisites;
        public Dictionary<SkillType, int> skillRewards;
        public float duration; // Продолжительность в часах
        public string degreeAwarded;

        public EducationCourse(string title, EducationType educationType, double cost, int levelRequirement,
                              string description, Dictionary<SkillType, int> prerequisites,
                              Dictionary<SkillType, int> skillRewards, float duration, string degreeAwarded)
        {
            this.title = title;
            this.educationType = educationType;
            this.cost = cost;
            this.levelRequirement = levelRequirement;
            this.description = description;
            this.prerequisites = prerequisites ?? new Dictionary<SkillType, int>();
            this.skillRewards = skillRewards ?? new Dictionary<SkillType, int>();
            this.duration = duration;
            this.degreeAwarded = degreeAwarded;
        }
    }

    /// <summary>
    /// Диплом/сертификат
    /// </summary>
    [System.Serializable]
    public class EducationDegree
    {
        public string name;
        public EducationType educationType;
        public DateTime dateObtained;
        public string institution;
    }

    /// <summary>
    /// Результат обучения
    /// </summary>
    [System.Serializable]
    public class EducationResult
    {
        public EducationCourse course;
        public int skillPointsGained;
        public int experienceGained;
        public DateTime completionTime;
        public string degreeObtained;
    }

    /// <summary>
    /// Данные системы образования для сохранения
    /// </summary>
}
