using System;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Типы характеристик игрока
    /// </summary>
    public enum StatType
    {
        // Базовые характеристики
        Health,     // Здоровье
        Hunger,     // Голод
        Mood,       // Настроение
        Fatigue,
        Money,      // Деньги
        Level,      // Уровень
        Experience, // Опыт

        // Модификаторы скорости изменения
        HealthRegen,            // Скорость восстановления здоровья
        HungerRate,             // Скорость роста голода
        MoodRegen,              // Скорость восстановления настроения

        // Модификаторы эффективности
        EarningsMultiplier,     // Множитель заработка
        ExperienceMultiplier,   // Множитель получения опыта
        LearningSpeedMultiplier,// Множитель скорости обучения
        LearningMultiplier = LearningSpeedMultiplier,
        WorkEfficiencyMultiplier,// Множитель эффективности работы

        // Модификаторы удачи и шансов
        LuckModifier,           // Модификатор удачи
        CriticalChance,         // Шанс критического успеха
        FailureReduction,       // Снижение шанса неудачи

        // Социальные модификаторы
        CharismaBonus,          // Бонус харизмы
        BusinessSkillBonus,     // Бонус деловых навыков
        NegotiationBonus,       // Бонус переговоров

        // Дополнительные модификаторы для новых систем
        MoodModifier,           // Модификатор настроения
        TransportSpeedBonus,    // Бонус скорости транспорта
        SocialInfluenceBonus,   // Бонус социального влияния
        CrimeDetectionReduction,// Снижение обнаружения преступлений
        FriendshipBonus,        // Бонус к дружбе
        ReputationGainBonus     // Бонус к получению репутации
    }

    /// <summary>
    /// Состояния игры
    /// </summary>
    public enum GameState
    {
        MainMenu,   // Главное меню
        Playing,    // Игра идет
        Paused,     // Пауза
        GameOver    // Конец игры
    }

    /// <summary>
    /// Типы игровых событий
    /// </summary>
    public enum GameEventType
    {
        StatChanged,        // Изменение характеристики
        LevelUp,           // Повышение уровня
        LowHealth,         // Низкое здоровье
        HighHunger,        // Высокий голод
        PlayerDeath,       // Смерть игрока
        MoneyEarned,       // Заработок денег
        FoodConsumed,      // Потребление еды
        RestTaken          // Отдых
    }

    /// <summary>
    /// Типы прохожих
    /// </summary>
    public enum NPCType
    {
        Poor,       // Бедный - дает мало денег
        Normal,     // Обычный - средняя сумма
        Rich,       // Богатый - большая сумма
        Tourist,    // Турист - случайная сумма
        Businessman // Бизнесмен - много денег, но редкий
    }

    /// <summary>
    /// Время дня (влияет на появление NPC)
    /// </summary>
    public enum TimeOfDay
    {
        Morning,    // Утро - мало людей
        Day,        // День - много людей
        Evening,    // Вечер - средне
        Night       // Ночь - мало людей
    }

    /// <summary>
    /// Локации в игре
    /// </summary>
    public enum Location
    {
        Street,         // Улица - стартовая локация
        Park,           // Парк - улучшает настроение
        BusinessDistrict,   // Бизнес-район - богатые люди
        ShoppingMall,   // Торговый центр - туристы и покупки
        Slums,          // Трущобы - опасно, нелегальный заработок
        TrainStation,   // Вокзал - туристы, есть полиция
        NightClub,      // Ночной клуб - только ночью, высокий доход
        
        // Дополнительные локации
        Shop,           // Магазин
        University,     // Университет
        Hospital,       // Больница
        Library,        // Библиотека
        JobCenter,      // Центр занятости
        Apartment,      // Квартира
        House,          // Дом
        Hotel,          // Отель
        Hostel,         // Хостел
        Shelter,        // Приют
        Industrial,     // Промышленная зона
        Abandoned       // Заброшенное место
    }

    /// <summary>
    /// Состояния UI системы
    /// </summary>
    public enum UIState
    {
        Game,           // Основной игровой интерфейс
        Pause,          // Меню паузы
        Settings,       // Настройки
        MainMenu,       // Главное меню
        GameOver        // Экран окончания игры
    }

    /// <summary>
    /// Типы уведомлений
    /// </summary>
    public enum NotificationType
    {
        Info,           // Информационное
        Success,        // Успех
        Warning,        // Предупреждение
        Danger,         // Опасность
        Achievement,    // Достижение
        Money,          // Изменение денег
        Critical,       // Критическое уведомление
        Important       // Важное уведомление
    }

    /// <summary>
    /// Приоритеты уведомлений
    /// </summary>
    public enum NotificationPriority
    {
        Low,            // Низкий приоритет
        Normal,         // Обычный приоритет
        High,           // Высокий приоритет
        Critical        // Критический приоритет
    }

    /// <summary>
    /// Типы локаций
    /// </summary>
    public enum LocationType
    {
        Street,         // Улица - стартовая локация
        Park,           // Парк - улучшает настроение
        BusinessDistrict,   // Бизнес-район - богатые люди
        ShoppingMall,   // Торговый центр - туристы и покупки
        Slums,          // Трущобы - опасно, нелегальный заработок
        TrainStation,   // Вокзал - туристы, есть полиция
        NightClub       // Ночной клуб - только ночью, высокий доход
    }

    /// <summary>
    /// Состояния доступности локации
    /// </summary>
    public enum LocationAccessibility
    {
        Available,      // Доступна
        Locked,         // Заблокирована (нужен уровень/деньги)
        TimeRestricted, // Недоступна в текущее время
        Closed          // Закрыта навсегда
    }

    /// <summary>
    /// Типы эффектов локаций
    /// </summary>
    public enum LocationEffectType
    {
        LocationStatModifier,   // Модификатор характеристик локации
        MoneyModifier,          // Модификатор заработка
        NPCModifier,            // Модификатор появления NPC
        DegradationModifier,    // Модификатор деградации
        SpecialEvent            // Специальное событие
    }

    /// <summary>
    /// Типы навыков персонажа
    /// </summary>
    public enum SkillType
    {
        None,
        Charisma,
        Education,
        Fitness,
        Luck,
        Business,
        Computer,
        Stealth,
        Streetwise,
        Physical_Fitness,
        Intelligence,
        Business_Skills
    }

    /// <summary>
    /// Типы работы
    /// </summary>
    public enum JobType
    {
        None,           // Без работы
        Cleaner,        // Уборщик
        Security,       // Охранник
        Seller,         // Продавец
        Cashier,
        Salesman,
        Manager,        // Менеджер
        Businessman,    // Бизнесмен
        Entrepreneur,
        CEO             // Директор
    }

    /// <summary>
    /// Категории товаров в магазине
    /// </summary>
    public enum ShopCategory
    {
        Food,           // Еда
        Clothing,       // Одежда
        Housing,        // Жилье
        Transport,      // Транспорт
        Education,      // Образование
        Entertainment  // Развлечения
    }

    /// <summary>
    /// Качество товаров
    /// </summary>
    public enum ItemQuality
    {
        Poor,           // Плохое качество
        Common,         // Обычное качество
        Good,           // Хорошее качество
        Excellent,      // Отличное качество
        Premium,        // Премиум качество
        Luxury          // Люкс качество
    }

    /// <summary>
    /// Типы образовательных курсов
    /// </summary>
    public enum EducationType
    {
        BasicSkills,    // Базовые навыки
        ComputerCourse, // Компьютерные курсы
        LanguageCourse, // Языковые курсы
        BusinessSchool, // Бизнес-школа
        University,     // Университет
        MBA             // MBA программа
    }

    /// <summary>
    /// Рабочие смены
    /// </summary>
    public enum WorkShift
    {
        Morning,        // Утренняя смена
        Day,            // Дневная смена
        Evening,        // Вечерняя смена
        Night           // Ночная смена
    }

    /// <summary>
    /// Типы достижений
    /// </summary>
    public enum AchievementType
    {
        Level,          // Достижение уровня
        Money,          // Накопление денег
        Skill,          // Развитие навыков
        Job,            // Трудовые достижения
        Purchase,       // Покупки
        Survival,       // Выживание
        Social,         // Социальные достижения
        Education,      // Образование
        Special         // Специальные достижения
    }

    /// <summary>
    /// Категории достижений
    /// </summary>
    public enum AchievementCategory
    {
        Progress,       // Прогресс
        Milestone,      // Веха
        Challenge,      // Вызов
        Secret          // Секретное
    }

    /// <summary>
    /// Типы наград
    /// </summary>
    public enum RewardType
    {
        Money,          // Деньги
        Experience,     // Опыт
        SkillPoints,    // Очки навыков
        Item,           // Предмет
        Unlock          // Разблокировка
    }

    /// <summary>
    /// Типы эффектов предметов
    /// </summary>
    public enum ItemEffectType
    {
        ItemStatModifier,   // Модификатор характеристик предмета
        SkillBonus,         // Бонус к навыкам
        IncomeModifier,     // Модификатор дохода
        DegradationModifier,// Модификатор деградации
        Unlock,             // Разблокировка
        Temporary,          // Временный эффект
        Permanent           // Постоянный эффект
    }

    /// <summary>
    /// Периоды времени в течение дня
    /// </summary>
    public enum TimePeriod
    {
        EarlyMorning,   // Раннее утро (5-8)
        Morning,        // Утро (8-12)
        Day,
        Afternoon,
        Evening,        // Вечер (17-21)
        Night,          // Ночь (21-1)
        LateNight       // Поздняя ночь (1-5)
    }

    /// <summary>
    /// Скорость течения времени
    /// </summary>
    public enum TimeSpeed
    {
        Paused,         // Время остановлено
        Normal,         // Обычная скорость (1x)
        Fast,           // Быстро (2x)
        VeryFast,       // Очень быстро (4x)
        UltraFast       // Сверхбыстро (8x)
    }

    /// <summary>
    /// Типы погоды
    /// </summary>
    public enum WeatherType
    {
        Any,
        Clear,          // Ясно
        Cloudy,         // Облачно
        Rainy,          // Дождь
        Heavy_Rain,     // Сильный дождь
        Snow,           // Снег
        Fog,            // Туман
        Storm           // Гроза
    }

    /// <summary>
    /// Сезоны года
    /// </summary>
    public enum Season
    {
        Spring,         // Весна
        Summer,         // Лето
        Autumn,         // Осень
        Winter          // Зима
    }

    /// <summary>
    /// Типы мест для сна
    /// </summary>
    public enum SleepLocation
    {
        Street,         // Улица (бесплатно, плохо восстанавливает)
        Park,           // Парк (бесплатно, небезопасно)
        Shelter,        // Приют (дешево, безопасно)
        Hostel,         // Хостел (дешево, нормально)
        Hotel,          // Отель (дорого, хорошо)
        Apartment,      // Своя квартира (дорого купить, отлично восстанавливает)
        House           // Свой дом (очень дорого, максимальное восстановление)
    }

    /// <summary>
    /// Уровни усталости
    /// </summary>
    public enum FatigueLevel
    {
        Rested,         // Отдохнувший (0-20%)
        Slightly_Tired, // Слегка устал (20-40%)
        Tired,          // Устал (40-60%)
        Very_Tired,     // Очень устал (60-80%)
        Exhausted       // Изнеможение (80-100%)
    }

    /// <summary>
    /// Типы временных событий
    /// </summary>
    public enum TimeBasedEventType
    {
        Morning_Jog,        // Утренняя пробежка
        Rush_Hour,          // Час пик
        Lunch_Break,        // Обеденный перерыв
        Happy_Hour,         // Счастливый час
        Night_Market,       // Ночной рынок
        Late_Night_Deals,   // Ночные сделки
        Early_Bird_Special, // Ранние скидки
        Weekend_Event,      // Выходное событие
        Holiday_Event,      // Праздничное событие
        Emergency_Event,    // Экстренное событие
        Danger,
        Opportunity,
        Work,
        Positive,
        Choice,
        Skill_Challenge
    }

    /// <summary>
    /// Категории временных событий
    /// </summary>
    public enum EventCategory
    {
        Work,           // Рабочие события
        Education,      // Образовательные события
        Social,         // Социальные события
        Economic,       // Экономические события
        Health,         // События здоровья
        Entertainment,  // Развлечения
        Random,         // Случайные события
        Weather,        // Погодные события
        Holiday,        // Праздничные события
        Moral,
        Performance,
        Career,
        Business,
        Survival
    }

    /// <summary>
    /// Статусы временных событий
    /// </summary>
    public enum EventStatus
    {
        Pending,        // Ожидает
        Active,         // Активно
        Completed,      // Завершено
        Expired,        // Истекло
        Cancelled,      // Отменено
        Failed          // Провалено
    }

    /// <summary>
    /// Типы визуальных эффектов времени
    /// </summary>
    public enum VisualTimeEffect
    {
        SkyGradient,    // Градиент неба
        SunPosition,    // Позиция солнца
        MoonPosition,   // Позиция луны
        Lighting,       // Освещение
        Shadows,        // Тени
        Fog,            // Туман
        Rain,           // Дождь
        Snow,           // Снег
        Wind,           // Ветер
        StreetLights    // Уличные фонари
    }

    /// <summary>
    /// Уровни интенсивности погодных эффектов
    /// </summary>
    public enum WeatherIntensity
    {
        None,           // Отсутствует
        Light,          // Легкая
        Moderate,       // Умеренная
        Heavy,          // Сильная
        Extreme         // Экстремальная
    }

    // ========== БОЕВАЯ СИСТЕМА ==========

    /// <summary>
    /// Типы оружия
    /// </summary>
    public enum WeaponType
    {
        None,           // Без оружия (кулаки)
        Knife,          // Нож
        Bat,            // Бита
        Club,           // Дубинка
        Pistol,         // Пистолет
        BrokenBottle,   // Разбитая бутылка
        Hammer,         // Молоток
        Chain,          // Цепь
        Knuckles,       // Кастет
        Crowbar,        // Лом
        Machete,        // Мачете
        Sword,          // Меч
        Axe,            // Топор
        Taser,          // Электрошокер
        PepperSpray     // Газовый баллончик
    }

    /// <summary>
    /// Типы врагов
    /// </summary>
    public enum EnemyType
    {
        Thug,           // Хулиган
        Robber,         // Грабитель
        Bandit,         // Бандит
        DrunkPerson,    // Пьяный
        StreetFighter,  // Уличный боец
        Police,         // Полицейский
        SecurityGuard,  // Охранник
        Gang_Member,    // Член банды
        Dealer,         // Торговец наркотиками
        Bully          // Задира
    }

    /// <summary>
    /// Действия в бою
    /// </summary>
    public enum CombatAction
    {
        Attack,         // Атака
        Defend,         // Защита
        Dodge,          // Уклонение
        UseItem,        // Использовать предмет
        Flee,           // Побег
        Negotiate,      // Переговоры
        Intimidate,     // Запугивание
        SurpriseAttack  // Внезапная атака
    }

    /// <summary>
    /// Результат боевого действия
    /// </summary>
    public enum CombatResult
    {
        Hit,            // Попадание
        Miss,           // Промах
        Critical,       // Критический удар
        Blocked,        // Заблокировано
        Dodged,         // Уклонение
        Stunned,        // Оглушение
        Disarmed,       // Разоружение
        Fled            // Побег
    }

    /// <summary>
    /// Состояние боя
    /// </summary>
    public enum CombatState
    {
        NotInCombat,    // Не в бою
        PreCombat,      // Подготовка к бою
        InCombat,       // В бою
        PlayerTurn,     // Ход игрока
        EnemyTurn,      // Ход врага
        CombatEnded,    // Бой окончен
        PlayerWon,      // Игрок победил
        PlayerLost,     // Игрок проиграл
        PlayerFled      // Игрок сбежал
    }

    /// <summary>
    /// Тип боевого AI
    /// </summary>
    public enum CombatAI
    {
        Aggressive,     // Агрессивный (всегда атакует)
        Defensive,      // Оборонительный (часто защищается)
        Balanced,       // Сбалансированный
        Cowardly,       // Трусливый (может сбежать)
        Tactical,       // Тактический (использует предметы)
        Berserker,      // Берсерк (рискованные атаки)
        Police          // Полицейский (особое поведение)
    }

    /// <summary>
    /// Уровень розыска полиции
    /// </summary>
    public enum WantedLevel
    {
        None,           // Нет розыска
        Suspicious,     // Подозрения
        Minor,          // Незначительный (штраф)
        Moderate,       // Умеренный (арест возможен)
        High,           // Высокий (активный поиск)
        Serious,
        Dangerous,
        MostWanted,
        Maximum = MostWanted
    }

    /// <summary>
    /// Типы боевых событий
    /// </summary>
    public enum CombatEventType
    {
        RandomEncounter,    // Случайная встреча
        Ambush,            // Засада
        Robbery,           // Ограбление
        PoliceRaid,        // Полицейский рейд
        GangFight,         // Драка с бандой
        SelfDefense,       // Самооборона
        BarFight,          // Драка в баре
        StreetFight,       // Уличная драка
        Mugging,           // Нападение
        Protection,        // Защита кого-то
        TerritoryDispute,
        Gang_Conflict,
        Revenge
    }

    /// <summary>
    /// Последствия боя
    /// </summary>
    public enum CombatConsequence
    {
        None,              // Нет последствий
        MoneyLoss,         // Потеря денег
        Injury,            // Травма
        Arrest,            // Арест
        Reputation,        // Изменение репутации
        ItemLoss,          // Потеря предметов
        Hospital,          // Больница
        Fine,              // Штраф
        Witness,           // Свидетели
        Revenge,
        ReputationGain,
        ReputationLoss,
        ItemFound,
        Hospitalization,
        Pursuit,
        WitnessReport,
        RandomHelp,
        Retaliation,
        JobOffer,
        MedicalAssistance
    }

    /// <summary>
    /// Расположение оружия
    /// </summary>
    public enum WeaponPlacement
    {
        Hidden,            // Скрыто
        Visible,           // Видимо
        Holstered,         // В кобуре
        InHand,            // В руке
        Concealed          // Замаскировано
    }

    /// <summary>
    /// Состояние оружия
    /// </summary>
    public enum WeaponCondition
    {
        Broken,            // Сломано
        Poor,              // Плохое состояние
        Fair,              // Удовлетворительное
        Good,              // Хорошее
        Excellent,         // Отличное
        Perfect            // Идеальное
    }

    /// <summary>
    /// Типы боевого опыта
    /// </summary>
    public enum CombatExperience
    {
        Victory,           // Победа
        Defeat,            // Поражение
        Survival,          // Выживание
        FirstStrike,       // Первый удар
        CriticalHit,       // Критический удар
        PerfectDefense,    // Идеальная защита
        SuccessfulFlee,    // Успешный побег
        Disarm,            // Разоружение
        Intimidation,      // Запугивание
        Negotiation        // Переговоры
    }

    /// <summary>
    /// Тип урона
    /// </summary>
    public enum DamageType
    {
        Blunt,             // Тупой (кулаки, дубинка)
        Cutting,           // Режущий (нож, стекло)
        Slashing = Cutting,
        Piercing,          // Колющий (пистолет)
        Ballistic = Piercing,
        Electric,          // Электрический (шокер)
        Chemical,          // Химический (газ)
        Psychological      // Психологический (запугивание)
    }

    // =================== ДОПОЛНИТЕЛЬНЫЕ СИСТЕМЫ ===================

    /// <summary>
    /// Типы транспорта
    /// </summary>
    public enum TransportType
    {
        None,               // Пешком
        Bicycle,            // Велосипед
        Motorcycle,         // Мотоцикл
        Car,                // Машина
        PublicTransport,    // Общественный транспорт
        Taxi,               // Такси
        Train,              // Поезд
        Bus                 // Автобус
    }

    /// <summary>
    /// Состояние транспорта
    /// </summary>
    public enum TransportCondition
    {
        Broken,             // Сломан
        Poor,               // Плохое
        Fair,               // Удовлетворительное
        Good,               // Хорошее
        Excellent,          // Отличное
        Perfect             // Идеальное
    }

    /// <summary>
    /// Способ получения транспорта
    /// </summary>
    public enum TransportAcquisitionType
    {
        Purchase,           // Покупка
        Rent,               // Аренда
        Gift,               // Подарок
        Theft,              // Кража
        Found,              // Найден
        Won                 // Выигран
    }

    /// <summary>
    /// Типы друзей/союзников
    /// </summary>
    public enum FriendType
    {
        Helper,             // Помощник
        Teacher,            // Учитель
        BusinessPartner,    // Партнер по бизнесу
        Protector,          // Защитник
        Informant,          // Информатор
        Mentor,             // Наставник
        Rival,              // Соперник
        Criminal,           // Криминальный элемент
        Official,           // Чиновник
        Investor            // Инвестор
    }

    /// <summary>
    /// Уровень отношений
    /// </summary>
    public enum RelationshipLevel
    {
        Enemy,              // Враг
        Hostile,            // Враждебный
        Unfriendly,         // Недружелюбный
        Neutral,            // Нейтральный
        Friendly,           // Дружелюбный
        Good,               // Хороший друг
        Best,               // Лучший друг
        Trusted,            // Доверенное лицо
        Family              // Как семья
    }

    /// <summary>
    /// Типы случайных событий
    /// </summary>
    public enum RandomEventType
    {
        // Глобальные события
        EconomicCrisis,     // Экономический кризис
        EconomicBoom,       // Экономический бум
        WeatherDisaster,    // Природная катастрофа
        PoliticalChange,    // Политические изменения
        
        // Личные события
        LuckyFind,          // Счастливая находка
        UnexpectedMeeting,  // Неожиданная встреча
        JobOffer,           // Предложение работы
        Investment,         // Инвестиционная возможность
        Inheritance,        // Наследство
        Accident,           // Несчастный случай
        
        // Локационные события
        NewBusiness,        // Новый бизнес в районе
        AreaUpgrade,        // Улучшение района
        AreaDecline,        // Упадок района
        SpecialSale,        // Специальная распродажа
        
        // Социальные события
        CommunityEvent,     // Общественное мероприятие
        Celebration,        // Празднование
        Protest,            // Протест
        Festival            // Фестиваль
    }

    /// <summary>
    /// Типы штрафов и нарушений
    /// </summary>
    public enum ViolationType
    {
        Littering,          // Мусор
        Loitering,          // Бродяжничество
        PublicUrination,    // Справление нужды в общественном месте
        Begging,            // Попрошайничество
        Trespassing,        // Проникновение на частную территорию
        Vagrancy,           // Бродяжничество
        DrunkAndDisorderly, // Пьянство и нарушение порядка
        Harassment,         // Домогательство
        Shoplifting,        // Мелкая кража
        Disturbance,        // Нарушение общественного порядка
        MinorViolation,
        PublicDisorder,
        Theft,
        Aggression,
        Vandalism,
        IllegalBusiness,
        BribeAttempt
    }

    /// <summary>
    /// Типы преступлений для полицейской системы
    /// </summary>
    public enum CrimeType
    {
        Fighting,
        IllegalWeapon,
        Theft,
        Assault,
        Vandalism,
        ResistingArrest,
        BriberyAttempt
    }

    /// <summary>
    /// Социальные группы
    /// </summary>
    public enum SocialGroup
    {
        Homeless,           // Бездомные
        Workers,            // Рабочие
        Students,           // Студенты
        Business,           // Бизнес-класс
        Government,         // Правительство
        HighSociety,        // Высшее общество
        Community,          // Сообщество
        Religious,          // Религиозные группы
        Academic,           // Академическое сообщество
        Media,              // СМИ
        Criminal,           // Преступники
        Officials,          // Чиновники
        Artists,            // Творческие люди
        Elderly,            // Пожилые люди
        Youth               // Молодежь
    }

    /// <summary>
    /// Типы репутации
    /// </summary>
    public enum ReputationType
    {
        General,            // Общая репутация
        Business,           // Деловая репутация
        Criminal,           // Криминальная репутация
        Academic,           // Академическая репутация
        Social,             // Социальная репутация
        Street              // Уличная репутация
    }

    /// <summary>
    /// Товары черного рынка
    /// </summary>
    public enum BlackMarketItem
    {
        StolenGoods,        // Краденые товары
        Drugs,              // Наркотики
        IllegalWeapons,     // Нелегальное оружие
        FakeDocuments,      // Поддельные документы
        Contraband,         // Контрабанда
        Information,        // Информация
        Services,           // Нелегальные услуги
        Electronics,        // Электроника
        Luxury,             // Роскошные товары
        Medical             // Медицинские препараты
    }

    /// <summary>
    /// Типы новостей
    /// </summary>
    public enum NewsType
    {
        Economic,           // Экономические
        Political,          // Политические
        Social,             // Социальные
        Criminal,           // Криминальные
        Crime = Criminal,
        Weather,            // Погодные
        Sports,             // Спортивные
        Entertainment,      // Развлечения
        Technology,         // Технологические
        Health,             // Здравоохранение
        Education           // Образование
    }

    /// <summary>
    /// Влияние новостей
    /// </summary>
    public enum NewsImpact
    {
        None,               // Без влияния
        Positive,
        Negative,
        Neutral,
        Minor,              // Незначительное
        Moderate,           // Умеренное
        Major,              // Значительное
        Critical            // Критическое
    }


    /// <summary>
    /// Социальные статусы
    /// </summary>
    public enum SocialStatus
    {
        Homeless,           // Бездомный
        Poor,               // Бедняк
        WorkingClass,       // Рабочий класс
        LowerMiddleClass,   // Низший средний класс
        MiddleClass,        // Средний класс
        UpperMiddleClass,   // Высший средний класс
        UpperClass,         // Высший класс
        Elite               // Элита
    }

    /// <summary>
    /// Криминальные группировки
    /// </summary>
    public enum CriminalGroup
    {
        StreetThugs,        // Уличные бандиты
        SmugglingRing,      // Контрабандисты
        OrganizedCrime,     // Организованная преступность
        Cartel,             // Картель
        CyberCriminals      // Кибер-преступники
    }

    /// <summary>
    /// Типы сделок черного рынка
    /// </summary>
    public enum BlackMarketDealType
    {
        StolenGoods,        // Краденые товары
        Contraband,         // Контрабанда
        DrugDealing,        // Торговля наркотиками
        WeaponTrafficking,  // Торговля оружием
        MoneyLaundering,    // Отмывание денег
        CyberCrime,         // Кибер-преступления
        Protection,         // Рэкет
        CorruptionScheme,   // Коррупционная схема
        HumanTrafficking,   // Торговля людьми
        IdentityTheft       // Кража личности
    }
}