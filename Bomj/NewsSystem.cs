using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система новостей и событий в мире
    /// </summary>
    public class NewsSystem : MonoBehaviour
    {
        [Header("Настройки системы новостей")]
        [SerializeField] private bool enableNewsSystem = true;
        [SerializeField] private float newsUpdateInterval = 3600f;         // Интервал обновления новостей (секунды)
        [SerializeField] private int maxActiveNews = 10;                   // Максимум активных новостей
        [SerializeField] private float newsLifetime = 86400f;              // Время жизни новости (секунды)

        [Header("Настройки влияния")]
        [SerializeField] private float economicImpactMultiplier = 0.2f;    // Множитель экономического влияния
        [SerializeField] private float politicalImpactMultiplier = 0.15f;  // Множитель политического влияния
        [SerializeField] private float socialImpactMultiplier = 0.1f;      // Множитель социального влияния

        [Header("Вероятности новостей")]
        [SerializeField] private float economicNewsChance = 0.3f;          // Шанс экономических новостей
        [SerializeField] private float politicalNewsChance = 0.25f;        // Шанс политических новостей
        [SerializeField] private float socialNewsChance = 0.2f;            // Шанс социальных новостей
        [SerializeField] private float crimeNewsChance = 0.15f;            // Шанс криминальных новостей
        [SerializeField] private float weatherNewsChance = 0.1f;           // Шанс новостей о погоде

        [Header("Дебаг")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool forceNews = false;
        [SerializeField] private NewsType debugNewsType = NewsType.Economic;

        // Состояние системы
        private List<NewsItem> activeNews = new List<NewsItem>();
        private List<NewsItem> newsHistory = new List<NewsItem>();
        private Dictionary<NewsType, float> lastNewsTime = new Dictionary<NewsType, float>();
        private float lastNewsUpdate = 0f;
        private Dictionary<NewsType, List<GameEffect>> activeEffects = new Dictionary<NewsType, List<GameEffect>>();

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private LocationManager locationManager;
        private TimeOfDayManager timeOfDayManager;
        private RandomEventManager randomEventManager;
        private SocialSystem socialSystem;
        private BlackMarketSystem blackMarketSystem;
        private NotificationSystem notificationSystem;

        // События
        public event Action<NewsItem> OnNewsPublished;
        public event Action<NewsItem> OnNewsExpired;
        public event Action<GameEffect> OnGlobalEffectStarted;
        public event Action<GameEffect> OnGlobalEffectEnded;

        // Свойства
        public List<NewsItem> ActiveNews => new List<NewsItem>(activeNews);
        public List<NewsItem> NewsHistory => new List<NewsItem>(newsHistory);
        public int TotalNewsPublished => newsHistory.Count;
        public float LastUpdateTime => lastNewsUpdate;

        #region Unity Methods

        private void Awake()
        {
            // Найти компоненты
            playerStats = FindObjectOfType<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            locationManager = FindObjectOfType<LocationManager>();
            timeOfDayManager = FindObjectOfType<TimeOfDayManager>();
            randomEventManager = FindObjectOfType<RandomEventManager>();
            socialSystem = FindObjectOfType<SocialSystem>();
            blackMarketSystem = FindObjectOfType<BlackMarketSystem>();
            notificationSystem = FindObjectOfType<NotificationSystem>();
        }

        private void Start()
        {
            if (enableNewsSystem)
            {
                InitializeNewsSystem();
                SubscribeToEvents();
            }

            Debug.Log("NewsSystem инициализирован");
        }

        private void Update()
        {
            if (enableNewsSystem)
            {
                UpdateNewsSystem();
                CheckNewsUpdates();
                UpdateActiveEffects();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация системы новостей
        /// </summary>
        private void InitializeNewsSystem()
        {
            // Инициализировать время последних новостей
            foreach (NewsType newsType in Enum.GetValues(typeof(NewsType)))
            {
                lastNewsTime[newsType] = Time.time;
                activeEffects[newsType] = new List<GameEffect>();
            }

            lastNewsUpdate = Time.time;
            
            // Создать начальные новости
            CreateInitialNews();
        }

        /// <summary>
        /// Создать начальные новости
        /// </summary>
        private void CreateInitialNews()
        {
            // Добавить нейтральные новости для заполнения ленты
            PublishNews(CreateWelcomeNews());
            PublishNews(CreateWeatherNews());
            PublishNews(CreateSocialNews());
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            if (randomEventManager != null)
            {
                randomEventManager.OnRandomEventTriggered += OnRandomEventTriggered;
                randomEventManager.OnGlobalEventStarted += OnGlobalEventStarted;
            }

            if (socialSystem != null)
            {
                socialSystem.OnSocialStatusChanged += OnSocialStatusChanged;
            }

            if (blackMarketSystem != null)
            {
                blackMarketSystem.OnDealCompleted += OnBlackMarketDealCompleted;
            }
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (randomEventManager != null)
            {
                randomEventManager.OnRandomEventTriggered -= OnRandomEventTriggered;
                randomEventManager.OnGlobalEventStarted -= OnGlobalEventStarted;
            }

            if (socialSystem != null)
            {
                socialSystem.OnSocialStatusChanged -= OnSocialStatusChanged;
            }

            if (blackMarketSystem != null)
            {
                blackMarketSystem.OnDealCompleted -= OnBlackMarketDealCompleted;
            }
        }

        #endregion

        #region News System Update

        /// <summary>
        /// Обновить систему новостей
        /// </summary>
        private void UpdateNewsSystem()
        {
            UpdateActiveNews();
            RemoveExpiredNews();
        }

        /// <summary>
        /// Обновить активные новости
        /// </summary>
        private void UpdateActiveNews()
        {
            for (int i = activeNews.Count - 1; i >= 0; i--)
            {
                var news = activeNews[i];
                
                // Проверить истечение срока
                if (Time.time - news.publishTime >= newsLifetime)
                {
                    ExpireNews(news);
                }
            }
        }

        /// <summary>
        /// Удалить устаревшие новости
        /// </summary>
        private void RemoveExpiredNews()
        {
            if (activeNews.Count > maxActiveNews)
            {
                // Удалить самые старые новости
                var oldestNews = activeNews[0];
                ExpireNews(oldestNews);
            }
        }

        /// <summary>
        /// Проверить обновления новостей
        /// </summary>
        private void CheckNewsUpdates()
        {
            if (Time.time - lastNewsUpdate < newsUpdateInterval && !forceNews)
                return;

            GenerateRandomNews();
            lastNewsUpdate = Time.time;
        }

        /// <summary>
        /// Обновить активные эффекты
        /// </summary>
        private void UpdateActiveEffects()
        {
            foreach (var newsType in activeEffects.Keys)
            {
                var effects = activeEffects[newsType];
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];
                    
                    if (Time.time - effect.startTime >= effect.duration)
                    {
                        EndGlobalEffect(effect);
                        effects.RemoveAt(i);
                    }
                }
            }
        }

        #endregion

        #region News Generation

        /// <summary>
        /// Сгенерировать случайные новости
        /// </summary>
        private void GenerateRandomNews()
        {
            // Определить тип новости
            NewsType newsType = SelectNewsType();
            
            // Создать новость
            var news = CreateNewsOfType(newsType);
            if (news != null)
            {
                PublishNews(news);
                lastNewsTime[newsType] = Time.time;
            }
        }

        /// <summary>
        /// Выбрать тип новости
        /// </summary>
        private NewsType SelectNewsType()
        {
            var probabilities = new Dictionary<NewsType, float>
            {
                { NewsType.Economic, economicNewsChance },
                { NewsType.Political, politicalNewsChance },
                { NewsType.Social, socialNewsChance },
                { NewsType.Crime, crimeNewsChance },
                { NewsType.Weather, weatherNewsChance }
            };

            // Учесть время последних новостей
            foreach (var newsType in probabilities.Keys)
            {
                float timeSinceLastNews = Time.time - lastNewsTime[newsType];
                if (timeSinceLastNews > newsUpdateInterval * 2) // Давно не было этого типа
                {
                    probabilities[newsType] *= 1.5f;
                }
            }

            // Выбрать случайно с учетом вероятностей
            float totalProbability = 0f;
            foreach (var prob in probabilities.Values)
            {
                totalProbability += prob;
            }

            float randomValue = UnityEngine.Random.value * totalProbability;
            float currentSum = 0f;

            foreach (var pair in probabilities)
            {
                currentSum += pair.Value;
                if (randomValue <= currentSum)
                {
                    return pair.Key;
                }
            }

            return NewsType.Social; // Fallback
        }

        /// <summary>
        /// Создать новость определенного типа
        /// </summary>
        private NewsItem CreateNewsOfType(NewsType newsType)
        {
            switch (newsType)
            {
                case NewsType.Economic:
                    return CreateEconomicNews();
                case NewsType.Political:
                    return CreatePoliticalNews();
                case NewsType.Social:
                    return CreateSocialNews();
                case NewsType.Crime:
                    return CreateCrimeNews();
                case NewsType.Weather:
                    return CreateWeatherNews();
                case NewsType.Sports:
                    return CreateSportsNews();
                case NewsType.Technology:
                    return CreateTechnologyNews();
                default:
                    return CreateSocialNews();
            }
        }

        /// <summary>
        /// Создать экономические новости
        /// </summary>
        private NewsItem CreateEconomicNews()
        {
            var economicEvents = new[]
            {
                ("Центральный банк снизил ключевую ставку", "Ожидается рост экономической активности", NewsImpact.Positive),
                ("Инфляция превысила прогнозы", "Цены на товары и услуги продолжают расти", NewsImpact.Negative),
                ("Открылся новый торговый центр", "Создано 500 новых рабочих мест", NewsImpact.Positive),
                ("Банкротство крупной компании", "Тысячи людей остались без работы", NewsImpact.Negative),
                ("Иностранные инвестиции растут", "Экономика показывает положительную динамику", NewsImpact.Positive),
                ("Валютный кризис углубляется", "Национальная валюта продолжает падать", NewsImpact.Negative)
            };

            var selectedEvent = economicEvents[UnityEngine.Random.Range(0, economicEvents.Length)];
            
            var news = new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Economic,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Экономические новости"
            };

            // Добавить экономические эффекты
            if (selectedEvent.Item3 == NewsImpact.Positive)
            {
                news.effects.Add(CreateEconomicEffect(0.1f, 21600f)); // +10% на 6 часов
            }
            else if (selectedEvent.Item3 == NewsImpact.Negative)
            {
                news.effects.Add(CreateEconomicEffect(-0.15f, 14400f)); // -15% на 4 часа
            }

            return news;
        }

        /// <summary>
        /// Создать политические новости
        /// </summary>
        private NewsItem CreatePoliticalNews()
        {
            var politicalEvents = new[]
            {
                ("Принят новый закон о малом бизнесе", "Предприниматели получат налоговые льготы", NewsImpact.Positive),
                ("Ужесточается контроль за порядком", "Полиция получила дополнительные полномочия", NewsImpact.Negative),
                ("Снижены налоги для граждан", "Правительство стимулирует потребление", NewsImpact.Positive),
                ("Введены новые штрафы", "За нарушения общественного порядка", NewsImpact.Negative),
                ("Запущена программа помощи бездомным", "Выделено дополнительное финансирование", NewsImpact.Positive),
                ("Сокращается социальная поддержка", "Бюджетный дефицит требует экономии", NewsImpact.Negative)
            };

            var selectedEvent = politicalEvents[UnityEngine.Random.Range(0, politicalEvents.Length)];
            
            var news = new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Political,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Политические новости"
            };

            // Добавить политические эффекты
            if (selectedEvent.Item3 == NewsImpact.Positive)
            {
                news.effects.Add(CreatePoliticalEffect(0.05f, 43200f)); // +5% на 12 часов
            }
            else if (selectedEvent.Item3 == NewsImpact.Negative)
            {
                news.effects.Add(CreatePoliticalEffect(-0.1f, 28800f)); // -10% на 8 часов
            }

            return news;
        }

        /// <summary>
        /// Создать социальные новости
        /// </summary>
        private NewsItem CreateSocialNews()
        {
            var socialEvents = new[]
            {
                ("Городской фестиваль собрал тысячи участников", "Жители отмечают рост культурной активности", NewsImpact.Positive),
                ("Растет недовольство горожан", "Проблемы с транспортом и коммунальными услугами", NewsImpact.Negative),
                ("Открыта новая библиотека", "Доступ к образованию стал лучше", NewsImpact.Positive),
                ("Закрытие социального центра", "Сокращение социальных программ", NewsImpact.Negative),
                ("Благотворительная акция помогла сотням людей", "Горожане проявляют солидарность", NewsImpact.Positive),
                ("Участились конфликты в обществе", "Напряженность между группами населения", NewsImpact.Negative)
            };

            var selectedEvent = socialEvents[UnityEngine.Random.Range(0, socialEvents.Length)];
            
            var news = new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Social,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Социальные новости"
            };

            // Добавить социальные эффекты
            if (selectedEvent.Item3 == NewsImpact.Positive)
            {
                news.effects.Add(CreateSocialEffect(0.08f, 18000f)); // +8% на 5 часов
            }
            else if (selectedEvent.Item3 == NewsImpact.Negative)
            {
                news.effects.Add(CreateSocialEffect(-0.12f, 21600f)); // -12% на 6 часов
            }

            return news;
        }

        /// <summary>
        /// Создать криминальные новости
        /// </summary>
        private NewsItem CreateCrimeNews()
        {
            var crimeEvents = new[]
            {
                ("Полиция провела успешную операцию", "Задержана крупная преступная группировка", NewsImpact.Positive),
                ("Рост преступности в городе", "Участились кражи и грабежи", NewsImpact.Negative),
                ("Раскрыто громкое дело о коррупции", "Арестованы несколько чиновников", NewsImpact.Positive),
                ("Банда наркоторговцев остается на свободе", "Полиция усиливает патрулирование", NewsImpact.Negative),
                ("Снижение количества преступлений", "Профилактические меры дают результат", NewsImpact.Positive),
                ("Крупная кража в банке", "Преступники скрылись с крупной суммой", NewsImpact.Negative)
            };

            var selectedEvent = crimeEvents[UnityEngine.Random.Range(0, crimeEvents.Length)];
            
            var news = new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Crime,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Криминальная хроника"
            };

            // Добавить криминальные эффекты
            if (selectedEvent.Item3 == NewsImpact.Positive)
            {
                news.effects.Add(CreateCrimeEffect(-0.2f, 28800f)); // -20% криминальной активности на 8 часов
            }
            else if (selectedEvent.Item3 == NewsImpact.Negative)
            {
                news.effects.Add(CreateCrimeEffect(0.3f, 14400f)); // +30% криминальной активности на 4 часа
            }

            return news;
        }

        /// <summary>
        /// Создать новости о погоде
        /// </summary>
        private NewsItem CreateWeatherNews()
        {
            var weatherEvents = new[]
            {
                ("Солнечная погода порадует горожан", "Температура поднимется до комфортных значений", NewsImpact.Positive),
                ("Ожидается сильный дождь", "Возможны затруднения в движении транспорта", NewsImpact.Negative),
                ("Прекрасная весенняя погода", "Люди выходят на улицы и в парки", NewsImpact.Positive),
                ("Резкое похолодание", "Температура упадет ниже нормы", NewsImpact.Negative),
                ("Легкий ветерок освежит воздух", "Идеальные условия для прогулок", NewsImpact.Neutral),
                ("Туман затруднит видимость", "Водителям следует быть осторожными", NewsImpact.Negative)
            };

            var selectedEvent = weatherEvents[UnityEngine.Random.Range(0, weatherEvents.Length)];
            
            var news = new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Weather,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Прогноз погоды"
            };

            // Добавить эффекты погоды
            if (selectedEvent.Item3 == NewsImpact.Positive)
            {
                news.effects.Add(CreateWeatherEffect(0.05f, 10800f)); // +5% настроения на 3 часа
            }
            else if (selectedEvent.Item3 == NewsImpact.Negative)
            {
                news.effects.Add(CreateWeatherEffect(-0.08f, 7200f)); // -8% настроения на 2 часа
            }

            return news;
        }

        /// <summary>
        /// Создать спортивные новости
        /// </summary>
        private NewsItem CreateSportsNews()
        {
            var sportsEvents = new[]
            {
                ("Местная команда выиграла чемпионат", "Город празднует спортивную победу", NewsImpact.Positive),
                ("Олимпийский чемпион посетил школы", "Дети вдохновлены встречей со спортсменом", NewsImpact.Positive),
                ("Построен новый спортивный комплекс", "Жители получили доступ к современным тренажерам", NewsImpact.Positive),
                ("Отменен городской марафон", "Организаторы ссылаются на финансовые трудности", NewsImpact.Negative)
            };

            var selectedEvent = sportsEvents[UnityEngine.Random.Range(0, sportsEvents.Length)];
            
            return new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Sports,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Спортивные новости"
            };
        }

        /// <summary>
        /// Создать технологические новости
        /// </summary>
        private NewsItem CreateTechnologyNews()
        {
            var techEvents = new[]
            {
                ("Запущена новая IT-платформа", "Цифровизация городских услуг продолжается", NewsImpact.Positive),
                ("Кибератака нарушила работу сервисов", "Восстановление займет несколько дней", NewsImpact.Negative),
                ("Бесплатный Wi-Fi появился в парках", "Город расширяет цифровую инфраструктуру", NewsImpact.Positive),
                ("Сбой в банковской системе", "Временно недоступны электронные платежи", NewsImpact.Negative)
            };

            var selectedEvent = techEvents[UnityEngine.Random.Range(0, techEvents.Length)];
            
            return new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Technology,
                title = selectedEvent.Item1,
                content = selectedEvent.Item2,
                impact = selectedEvent.Item3,
                publishTime = Time.time,
                source = "Технологические новости"
            };
        }

        /// <summary>
        /// Создать приветственные новости
        /// </summary>
        private NewsItem CreateWelcomeNews()
        {
            return new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Social,
                title = "Добро пожаловать в город!",
                content = "Следите за новостями, чтобы быть в курсе важных событий, влияющих на вашу жизнь.",
                impact = NewsImpact.Neutral,
                publishTime = Time.time,
                source = "Городские новости"
            };
        }

        #endregion

        #region News Management

        /// <summary>
        /// Опубликовать новость
        /// </summary>
        private void PublishNews(NewsItem news)
        {
            activeNews.Add(news);
            newsHistory.Add(news);

            // Применить эффекты
            foreach (var effect in news.effects)
            {
                StartGlobalEffect(effect);
                activeEffects[news.type].Add(effect);
            }

            OnNewsPublished?.Invoke(news);

            // Показать уведомление если новость важная
            if (news.impact != NewsImpact.Neutral && notificationSystem != null)
            {
                NotificationType notificationType = news.impact == NewsImpact.Positive ? 
                    NotificationType.Info : NotificationType.Warning;
                
                notificationSystem.ShowNotification("Новости", news.title, notificationType);
            }

            if (showDebugInfo)
            {
                Debug.Log($"Опубликована новость: {news.title} ({news.type}, {news.impact})");
            }
        }

        /// <summary>
        /// Истечь новость
        /// </summary>
        private void ExpireNews(NewsItem news)
        {
            activeNews.Remove(news);
            
            // Удалить эффекты
            if (activeEffects.ContainsKey(news.type))
            {
                foreach (var effect in news.effects)
                {
                    if (activeEffects[news.type].Contains(effect))
                    {
                        EndGlobalEffect(effect);
                        activeEffects[news.type].Remove(effect);
                    }
                }
            }

            OnNewsExpired?.Invoke(news);

            if (showDebugInfo)
            {
                Debug.Log($"Новость истекла: {news.title}");
            }
        }

        #endregion

        #region Effect Management

        /// <summary>
        /// Создать экономический эффект
        /// </summary>
        private GameEffect CreateEconomicEffect(float modifier, float duration)
        {
            return new GameEffect
            {
                type = EffectType.Economic,
                modifier = modifier,
                duration = duration,
                startTime = Time.time,
                description = modifier > 0 ? "Экономический подъем" : "Экономический спад"
            };
        }

        /// <summary>
        /// Создать политический эффект
        /// </summary>
        private GameEffect CreatePoliticalEffect(float modifier, float duration)
        {
            return new GameEffect
            {
                type = EffectType.Political,
                modifier = modifier,
                duration = duration,
                startTime = Time.time,
                description = modifier > 0 ? "Политическая стабильность" : "Политическая нестабильность"
            };
        }

        /// <summary>
        /// Создать социальный эффект
        /// </summary>
        private GameEffect CreateSocialEffect(float modifier, float duration)
        {
            return new GameEffect
            {
                type = EffectType.Social,
                modifier = modifier,
                duration = duration,
                startTime = Time.time,
                description = modifier > 0 ? "Социальный подъем" : "Социальная напряженность"
            };
        }

        /// <summary>
        /// Создать криминальный эффект
        /// </summary>
        private GameEffect CreateCrimeEffect(float modifier, float duration)
        {
            return new GameEffect
            {
                type = EffectType.Crime,
                modifier = modifier,
                duration = duration,
                startTime = Time.time,
                description = modifier > 0 ? "Рост преступности" : "Снижение преступности"
            };
        }

        /// <summary>
        /// Создать эффект погоды
        /// </summary>
        private GameEffect CreateWeatherEffect(float modifier, float duration)
        {
            return new GameEffect
            {
                type = EffectType.Weather,
                modifier = modifier,
                duration = duration,
                startTime = Time.time,
                description = modifier > 0 ? "Хорошая погода" : "Плохая погода"
            };
        }

        /// <summary>
        /// Запустить глобальный эффект
        /// </summary>
        private void StartGlobalEffect(GameEffect effect)
        {
            ApplyEffect(effect);
            OnGlobalEffectStarted?.Invoke(effect);

            if (showDebugInfo)
            {
                Debug.Log($"Запущен эффект: {effect.description} ({effect.modifier:+0.0%})");
            }
        }

        /// <summary>
        /// Завершить глобальный эффект
        /// </summary>
        private void EndGlobalEffect(GameEffect effect)
        {
            RemoveEffect(effect);
            OnGlobalEffectEnded?.Invoke(effect);

            if (showDebugInfo)
            {
                Debug.Log($"Завершен эффект: {effect.description}");
            }
        }

        /// <summary>
        /// Применить эффект
        /// </summary>
        private void ApplyEffect(GameEffect effect)
        {
            if (playerStats == null)
                return;

            string sourceName = $"Новости: {effect.description}";

            switch (effect.type)
            {
                case EffectType.Economic:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.EarningsMultiplier, 1f + effect.modifier, ModifierOperation.Multiply,
                        sourceName, effect.duration));
                    break;

                case EffectType.Social:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.MoodRegen, effect.modifier * 10f, ModifierOperation.Add,
                        sourceName, effect.duration));
                    break;

                case EffectType.Weather:
                    playerStats.AddStatModifier(new StatModifier(
                        StatType.MoodModifier, effect.modifier * 20f, ModifierOperation.Add,
                        sourceName, effect.duration));
                    break;

                case EffectType.Political:
                    // TODO: Влияние на системы штрафов, полиции и т.д.
                    break;

                case EffectType.Crime:
                    // TODO: Влияние на безопасность и черный рынок
                    break;
            }
        }

        /// <summary>
        /// Удалить эффект
        /// </summary>
        private void RemoveEffect(GameEffect effect)
        {
            if (playerStats == null)
                return;

            string sourceName = $"Новости: {effect.description}";
            playerStats.RemoveStatModifiersBySource(sourceName);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Получить последние новости
        /// </summary>
        public List<NewsItem> GetLatestNews(int count = 5)
        {
            var latest = new List<NewsItem>(activeNews);
            latest.Reverse(); // Показать сначала самые новые
            
            if (latest.Count > count)
            {
                latest = latest.GetRange(0, count);
            }

            return latest;
        }

        /// <summary>
        /// Получить новости по типу
        /// </summary>
        public List<NewsItem> GetNewsByType(NewsType newsType)
        {
            return activeNews.FindAll(news => news.type == newsType);
        }

        /// <summary>
        /// Получить активные эффекты
        /// </summary>
        public List<GameEffect> GetActiveEffects()
        {
            var allEffects = new List<GameEffect>();
            
            foreach (var effectList in activeEffects.Values)
            {
                allEffects.AddRange(effectList);
            }

            return allEffects;
        }

        /// <summary>
        /// Есть ли активный эффект типа
        /// </summary>
        public bool HasActiveEffect(EffectType effectType)
        {
            foreach (var effectList in activeEffects.Values)
            {
                if (effectList.Exists(effect => effect.type == effectType))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public NewsSystemSaveData GetSaveData()
        {
            return new NewsSystemSaveData
            {
                activeNews = new List<NewsItem>(activeNews),
                newsHistory = new List<NewsItem>(newsHistory),
                lastNewsTime = new Dictionary<NewsType, float>(lastNewsTime),
                lastNewsUpdate = lastNewsUpdate
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(NewsSystemSaveData data)
        {
            activeNews = data.activeNews ?? new List<NewsItem>();
            newsHistory = data.newsHistory ?? new List<NewsItem>();
            lastNewsTime = data.lastNewsTime ?? new Dictionary<NewsType, float>();
            lastNewsUpdate = data.lastNewsUpdate;

            // Восстановить активные эффекты
            foreach (var news in activeNews)
            {
                foreach (var effect in news.effects)
                {
                    float remainingTime = effect.duration - (Time.time - effect.startTime);
                    if (remainingTime > 0)
                    {
                        effect.duration = remainingTime;
                        StartGlobalEffect(effect);
                        activeEffects[news.type].Add(effect);
                    }
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"NewsSystem загружен. Активных новостей: {activeNews.Count}, " +
                         $"Всего новостей: {newsHistory.Count}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик случайного события
        /// </summary>
        private void OnRandomEventTriggered(RandomEvent randomEvent)
        {
            // Создать новость на основе случайного события
            if (ShouldCreateNewsForEvent(randomEvent))
            {
                var news = CreateNewsFromEvent(randomEvent);
                if (news != null)
                {
                    PublishNews(news);
                }
            }
        }

        /// <summary>
        /// Обработчик глобального события
        /// </summary>
        private void OnGlobalEventStarted(RandomEventType eventType)
        {
            // Крупные события всегда попадают в новости
            var news = CreateNewsFromGlobalEvent(eventType);
            if (news != null)
            {
                PublishNews(news);
            }
        }

        /// <summary>
        /// Обработчик изменения социального статуса
        /// </summary>
        private void OnSocialStatusChanged(SocialStatus newStatus)
        {
            // Значительные изменения статуса могут попасть в новости
            if (newStatus == SocialStatus.Elite || newStatus == SocialStatus.UpperClass)
            {
                var news = new NewsItem
                {
                    id = System.Guid.NewGuid().ToString(),
                    type = NewsType.Social,
                    title = "Местный житель добился успеха",
                    content = "Один из горожан сумел значительно улучшить свое положение в обществе",
                    impact = NewsImpact.Positive,
                    publishTime = Time.time,
                    source = "Социальная хроника"
                };

                PublishNews(news);
            }
        }

        /// <summary>
        /// Обработчик завершения сделки на черном рынке
        /// </summary>
        private void OnBlackMarketDealCompleted(BlackMarketDeal deal)
        {
            // Крупные криминальные сделки могут попасть в новости
            if (deal.potentialProfit > 5000 && UnityEngine.Random.value < 0.3f)
            {
                var news = new NewsItem
                {
                    id = System.Guid.NewGuid().ToString(),
                    type = NewsType.Crime,
                    title = "Полиция расследует подозрительную активность",
                    content = "Правоохранительные органы получили информацию о крупных незаконных сделках",
                    impact = NewsImpact.Negative,
                    publishTime = Time.time,
                    source = "Криминальная хроника"
                };

                news.effects.Add(CreateCrimeEffect(0.2f, 14400f)); // Повышенное внимание полиции
                PublishNews(news);
            }
        }

        /// <summary>
        /// Следует ли создавать новость для события
        /// </summary>
        private bool ShouldCreateNewsForEvent(RandomEvent randomEvent)
        {
            // Только значимые события попадают в новости
            return randomEvent.isGlobal || UnityEngine.Random.value < 0.2f;
        }

        /// <summary>
        /// Создать новость из события
        /// </summary>
        private NewsItem CreateNewsFromEvent(RandomEvent randomEvent)
        {
            var news = new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = NewsType.Social,
                title = randomEvent.title,
                content = randomEvent.description,
                impact = DetermineImpactFromEvent(randomEvent),
                publishTime = Time.time,
                source = "Городские события"
            };

            return news;
        }

        /// <summary>
        /// Создать новость из глобального события
        /// </summary>
        private NewsItem CreateNewsFromGlobalEvent(RandomEventType eventType)
        {
            NewsType newsType = NewsType.Social;
            NewsImpact impact = NewsImpact.Neutral;
            string title = "";
            string content = "";

            switch (eventType)
            {
                case RandomEventType.EconomicCrisis:
                    newsType = NewsType.Economic;
                    impact = NewsImpact.Negative;
                    title = "Экономический кризис затронул город";
                    content = "Эксперты прогнозируют снижение доходов и рост безработицы";
                    break;

                case RandomEventType.EconomicBoom:
                    newsType = NewsType.Economic;
                    impact = NewsImpact.Positive;
                    title = "Экономический рост радует горожан";
                    content = "Увеличивается количество рабочих мест и доходы населения";
                    break;

                case RandomEventType.WeatherDisaster:
                    newsType = NewsType.Weather;
                    impact = NewsImpact.Negative;
                    title = "Непогода нарушила привычный ритм города";
                    content = "Жители столкнулись с трудностями из-за погодных условий";
                    break;

                default:
                    return null;
            }

            return new NewsItem
            {
                id = System.Guid.NewGuid().ToString(),
                type = newsType,
                title = title,
                content = content,
                impact = impact,
                publishTime = Time.time,
                source = "Городские новости"
            };
        }

        /// <summary>
        /// Определить влияние из события
        /// </summary>
        private NewsImpact DetermineImpactFromEvent(RandomEvent randomEvent)
        {
            // Анализировать награды и штрафы события
            if (randomEvent.rewards.Count > randomEvent.penalties.Count)
                return NewsImpact.Positive;
            else if (randomEvent.penalties.Count > randomEvent.rewards.Count)
                return NewsImpact.Negative;
            else
                return NewsImpact.Neutral;
        }

        #endregion

        #region Context Menu Debug

        [ContextMenu("Generate Random News")]
        private void DebugGenerateRandomNews()
        {
            GenerateRandomNews();
        }

        [ContextMenu("Generate Economic News")]
        private void DebugGenerateEconomicNews()
        {
            PublishNews(CreateEconomicNews());
        }

        [ContextMenu("Generate Crime News")]
        private void DebugGenerateCrimeNews()
        {
            PublishNews(CreateCrimeNews());
        }

        [ContextMenu("Show News Stats")]
        private void DebugShowNewsStats()
        {
            Debug.Log($"Статистика новостей:");
            Debug.Log($"Активных новостей: {activeNews.Count}");
            Debug.Log($"Всего новостей: {newsHistory.Count}");
            Debug.Log($"Активных эффектов: {GetActiveEffects().Count}");
            
            var latest = GetLatestNews(3);
            Debug.Log("Последние новости:");
            foreach (var news in latest)
            {
                Debug.Log($"- {news.title} ({news.type})");
            }
        }

        [ContextMenu("Clear All News")]
        private void DebugClearAllNews()
        {
            for (int i = activeNews.Count - 1; i >= 0; i--)
            {
                ExpireNews(activeNews[i]);
            }
        }

        #endregion
    }

    /// <summary>
    /// Новостная запись
    /// </summary>
    [System.Serializable]
    public class NewsItem
    {
        public string id;                       // Уникальный ID
        public NewsType type;                   // Тип новости
        public string title;                    // Заголовок
        public string content;                  // Содержание
        public NewsImpact impact;               // Влияние
        public float publishTime;               // Время публикации
        public string source;                   // Источник
        public List<GameEffect> effects = new List<GameEffect>(); // Эффекты
    }

    /// <summary>
    /// Игровой эффект
    /// </summary>
    [System.Serializable]
    public class GameEffect
    {
        public EffectType type;                 // Тип эффекта
        public float modifier;                  // Модификатор
        public float duration;                  // Длительность
        public float startTime;                 // Время начала
        public string description;              // Описание
    }

    /// <summary>
    /// Типы эффектов
    /// </summary>
    public enum EffectType
    {
        Economic,           // Экономический
        Political,          // Политический
        Social,             // Социальный
        Crime,              // Криминальный
        Weather             // Погодный
    }



    /// <summary>
    /// Данные NewsSystem для сохранения
    /// </summary>
    [System.Serializable]
    public class NewsSystemSaveData
    {
        public List<NewsItem> activeNews;
        public List<NewsItem> newsHistory;
        public Dictionary<NewsType, float> lastNewsTime;
        public float lastNewsUpdate;
    }
}
