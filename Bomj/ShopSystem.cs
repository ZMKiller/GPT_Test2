using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система магазина и покупок
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        [Header("Настройки магазина")]
        [SerializeField] private bool enableShop = true;
        [SerializeField] private float priceInflationRate = 0.02f; // 2% в день
        [SerializeField] private int itemsPerCategory = 10;

        [Header("Звуки")]
        [SerializeField] private AudioClip purchaseSuccessSound;
        [SerializeField] private AudioClip purchaseFailSound;

        // Компоненты
        private PlayerStats playerStats;
        private MoneySystem moneySystem;
        private SkillSystem skillSystem;
        private AudioSource audioSource;

        private float timePriceMultiplier = 1f;

        // Товары
        private Dictionary<ShopCategory, List<ShopItem>> shopItems = new Dictionary<ShopCategory, List<ShopItem>>();
        private List<ShopItem> purchasedItems = new List<ShopItem>();

        // События
        public event Action<ShopItem> OnItemPurchased;
        public event Action<ShopCategory> OnCategoryUnlocked;
        public event Action<List<ShopItem>> OnShopUpdated;

        #region Unity Methods

        private void Awake()
        {
            // Получение компонентов
            playerStats = GetComponent<PlayerStats>();
            moneySystem = FindObjectOfType<MoneySystem>();
            skillSystem = GetComponent<SkillSystem>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Инициализация магазина
            InitializeShop();
        }

        private void Start()
        {
            if (!enableShop)
            {
                enabled = false;
                return;
            }

            if (playerStats == null || moneySystem == null)
            {
                Debug.LogError("ShopSystem: Не найдены необходимые компоненты!");
                enabled = false;
                return;
            }

            // Подписка на события
            SubscribeToEvents();

            Debug.Log("ShopSystem инициализирован");
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация магазина
        /// </summary>
        private void InitializeShop()
        {
            // Инициализация категорий
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
            {
                shopItems[category] = new List<ShopItem>();
            }

            // Создание товаров
            CreateShopItems();
        }

        /// <summary>
        /// Создание товаров в магазине
        /// </summary>
        private void CreateShopItems()
        {
            CreateFoodItems();
            CreateClothingItems();
            CreateHousingItems();
            CreateTransportItems();
            CreateEducationItems();
            CreateEntertainmentItems();
        }

        /// <summary>
        /// Создание товаров категории "Еда"
        /// </summary>
        private void CreateFoodItems()
        {
            var foodItems = new List<ShopItem>
            {
                new ShopItem("Хлеб", ShopCategory.Food, ItemQuality.Poor, 5, 1, 
                    "Простой хлеб для утоления голода", new Dictionary<StatType, float> { { StatType.Hunger, -10 } }),
                
                new ShopItem("Быстрая еда", ShopCategory.Food, ItemQuality.Common, 25, 1,
                    "Быстрая и сытная еда", new Dictionary<StatType, float> { { StatType.Hunger, -25 } }),
                
                new ShopItem("Домашняя еда", ShopCategory.Food, ItemQuality.Good, 50, 3,
                    "Полезная домашняя еда", new Dictionary<StatType, float> { { StatType.Hunger, -40 }, { StatType.Health, 5 } }),
                
                new ShopItem("Ресторанная еда", ShopCategory.Food, ItemQuality.Premium, 150, 5,
                    "Изысканная еда из ресторана", new Dictionary<StatType, float> { { StatType.Hunger, -50 }, { StatType.Health, 10 }, { StatType.Mood, 10 } }),
                
                new ShopItem("Деликатесы", ShopCategory.Food, ItemQuality.Luxury, 500, 10,
                    "Эксклюзивные деликатесы", new Dictionary<StatType, float> { { StatType.Hunger, -60 }, { StatType.Health, 15 }, { StatType.Mood, 20 } })
            };

            shopItems[ShopCategory.Food].AddRange(foodItems);
        }

        /// <summary>
        /// Создание товаров категории "Одежда"
        /// </summary>
        private void CreateClothingItems()
        {
            var clothingItems = new List<ShopItem>
            {
                new ShopItem("Подержанная одежда", ShopCategory.Clothing, ItemQuality.Poor, 20, 5,
                    "Дешевая подержанная одежда", new Dictionary<StatType, float> { { StatType.Mood, 5 } }),
                
                new ShopItem("Обычная одежда", ShopCategory.Clothing, ItemQuality.Common, 100, 5,
                    "Простая но чистая одежда", new Dictionary<StatType, float> { { StatType.Mood, 15 } }),
                
                new ShopItem("Модная одежда", ShopCategory.Clothing, ItemQuality.Good, 300, 10,
                    "Стильная современная одежда", new Dictionary<StatType, float> { { StatType.Mood, 25 } },
                    permanentEffects: new Dictionary<string, float> { { "CharismaBonus", 0.1f } }),
                
                new ShopItem("Брендовая одежда", ShopCategory.Clothing, ItemQuality.Premium, 1000, 15,
                    "Дорогая брендовая одежда", new Dictionary<StatType, float> { { StatType.Mood, 40 } },
                    permanentEffects: new Dictionary<string, float> { { "CharismaBonus", 0.2f } }),
                
                new ShopItem("Дизайнерский костюм", ShopCategory.Clothing, ItemQuality.Luxury, 5000, 25,
                    "Эксклюзивный дизайнерский костюм", new Dictionary<StatType, float> { { StatType.Mood, 60 } },
                    permanentEffects: new Dictionary<string, float> { { "CharismaBonus", 0.35f }, { "BusinessBonus", 0.15f } })
            };

            shopItems[ShopCategory.Clothing].AddRange(clothingItems);
        }

        /// <summary>
        /// Создание товаров категории "Жилье"
        /// </summary>
        private void CreateHousingItems()
        {
            var housingItems = new List<ShopItem>
            {
                new ShopItem("Койка в общежитии", ShopCategory.Housing, ItemQuality.Poor, 100, 20,
                    "Простая койка в общем помещении", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "HealthRegen", 0.5f } }),
                
                new ShopItem("Комната в коммуналке", ShopCategory.Housing, ItemQuality.Common, 500, 20,
                    "Собственная комната в коммунальной квартире", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "HealthRegen", 1f }, { "MoodRegen", 0.5f } }),
                
                new ShopItem("Однокомнатная квартира", ShopCategory.Housing, ItemQuality.Good, 2000, 25,
                    "Собственная однокомнатная квартира", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "HealthRegen", 2f }, { "MoodRegen", 1f } }),
                
                new ShopItem("Двухкомнатная квартира", ShopCategory.Housing, ItemQuality.Premium, 10000, 30,
                    "Просторная двухкомнатная квартира", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "HealthRegen", 3f }, { "MoodRegen", 2f } }),
                
                new ShopItem("Элитная квартира", ShopCategory.Housing, ItemQuality.Luxury, 50000, 40,
                    "Роскошная квартира в элитном районе", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "HealthRegen", 5f }, { "MoodRegen", 3f }, { "CharismaBonus", 0.25f } })
            };

            shopItems[ShopCategory.Housing].AddRange(housingItems);
        }

        /// <summary>
        /// Создание товаров категории "Транспорт"
        /// </summary>
        private void CreateTransportItems()
        {
            var transportItems = new List<ShopItem>
            {
                new ShopItem("Велосипед", ShopCategory.Transport, ItemQuality.Common, 200, 25,
                    "Простой велосипед для передвижения", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "MobilityBonus", 0.1f } }),
                
                new ShopItem("Мотоцикл", ShopCategory.Transport, ItemQuality.Good, 2000, 25,
                    "Быстрый мотоцикл", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "MobilityBonus", 0.25f } }),
                
                new ShopItem("Подержанный автомобиль", ShopCategory.Transport, ItemQuality.Good, 8000, 30,
                    "Старый но надежный автомобиль", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "MobilityBonus", 0.4f }, { "CharismaBonus", 0.1f } }),
                
                new ShopItem("Новый автомобиль", ShopCategory.Transport, ItemQuality.Premium, 25000, 35,
                    "Современный автомобиль", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "MobilityBonus", 0.6f }, { "CharismaBonus", 0.2f } }),
                
                new ShopItem("Люксовый автомобиль", ShopCategory.Transport, ItemQuality.Luxury, 100000, 45,
                    "Роскошный премиум автомобиль", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "MobilityBonus", 0.8f }, { "CharismaBonus", 0.4f }, { "BusinessBonus", 0.2f } })
            };

            shopItems[ShopCategory.Transport].AddRange(transportItems);
        }

        /// <summary>
        /// Создание товаров категории "Образование"
        /// </summary>
        private void CreateEducationItems()
        {
            var educationItems = new List<ShopItem>
            {
                new ShopItem("Книги по саморазвитию", ShopCategory.Education, ItemQuality.Common, 50, 1,
                    "Полезные книги для самообразования", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "EducationBonus", 0.05f } }),
                
                new ShopItem("Онлайн курсы", ShopCategory.Education, ItemQuality.Good, 200, 5,
                    "Профессиональные онлайн курсы", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "EducationBonus", 0.15f } }),
                
                new ShopItem("Языковые курсы", ShopCategory.Education, ItemQuality.Good, 500, 10,
                    "Изучение иностранного языка", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "EducationBonus", 0.2f }, { "CharismaBonus", 0.1f } }),
                
                new ShopItem("Профессиональные курсы", ShopCategory.Education, ItemQuality.Premium, 2000, 15,
                    "Курсы повышения квалификации", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "EducationBonus", 0.3f }, { "BusinessBonus", 0.1f } }),
                
                new ShopItem("MBA программа", ShopCategory.Education, ItemQuality.Luxury, 20000, 35,
                    "Престижная MBA программа", new Dictionary<StatType, float>(),
                    permanentEffects: new Dictionary<string, float> { { "EducationBonus", 0.5f }, { "BusinessBonus", 0.3f }, { "CharismaBonus", 0.2f } })
            };

            shopItems[ShopCategory.Education].AddRange(educationItems);
        }

        /// <summary>
        /// Создание товаров категории "Развлечения"
        /// </summary>
        private void CreateEntertainmentItems()
        {
            var entertainmentItems = new List<ShopItem>
            {
                new ShopItem("Билет в кино", ShopCategory.Entertainment, ItemQuality.Common, 15, 1,
                    "Поход в кинотеатр", new Dictionary<StatType, float> { { StatType.Mood, 15 } }),
                
                new ShopItem("Концерт", ShopCategory.Entertainment, ItemQuality.Good, 100, 5,
                    "Билет на музыкальный концерт", new Dictionary<StatType, float> { { StatType.Mood, 30 } }),
                
                new ShopItem("Спортзал (месяц)", ShopCategory.Entertainment, ItemQuality.Good, 50, 10,
                    "Месячный абонемент в спортзал", new Dictionary<StatType, float> { { StatType.Health, 20 } },
                    permanentEffects: new Dictionary<string, float> { { "FitnessBonus", 0.1f } }),
                
                new ShopItem("Отпуск", ShopCategory.Entertainment, ItemQuality.Premium, 1000, 15,
                    "Недельный отпуск", new Dictionary<StatType, float> { { StatType.Health, 40 }, { StatType.Mood, 50 } }),
                
                new ShopItem("Круиз", ShopCategory.Entertainment, ItemQuality.Luxury, 10000, 25,
                    "Роскошный морской круиз", new Dictionary<StatType, float> { { StatType.Health, 60 }, { StatType.Mood, 80 } },
                    permanentEffects: new Dictionary<string, float> { { "CharismaBonus", 0.15f } })
            };

            shopItems[ShopCategory.Entertainment].AddRange(entertainmentItems);
        }

        /// <summary>
        /// Подписка на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnLevelUp += OnLevelUp;
        }

        /// <summary>
        /// Отписка от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnLevelUp -= OnLevelUp;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Получить товары категории
        /// </summary>
        public List<ShopItem> GetCategoryItems(ShopCategory category)
        {
            if (!shopItems.ContainsKey(category))
                return new List<ShopItem>();

            // Фильтровать по доступности
            return shopItems[category].Where(item => IsItemAvailable(item)).ToList();
        }

        /// <summary>
        /// Получить все доступные категории
        /// </summary>
        public List<ShopCategory> GetAvailableCategories()
        {
            var availableCategories = new List<ShopCategory>();
            
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
            {
                if (IsCategoryUnlocked(category))
                {
                    availableCategories.Add(category);
                }
            }

            return availableCategories;
        }

        /// <summary>
        /// Проверить разблокировку категории
        /// </summary>
        public bool IsCategoryUnlocked(ShopCategory category)
        {
            int playerLevel = playerStats?.Level ?? 1;

            switch (category)
            {
                case ShopCategory.Food:
                    return true; // Всегда доступна
                
                case ShopCategory.Clothing:
                    return playerLevel >= 5;
                
                case ShopCategory.Housing:
                    return playerLevel >= 20;
                
                case ShopCategory.Transport:
                    return playerLevel >= 25;
                
                case ShopCategory.Education:
                    return playerLevel >= 15;
                
                case ShopCategory.Entertainment:
                    return playerLevel >= 10;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Проверить доступность товара
        /// </summary>
        public bool IsItemAvailable(ShopItem item)
        {
            if (!IsCategoryUnlocked(item.category))
                return false;

            int playerLevel = playerStats?.Level ?? 1;
            return playerLevel >= item.levelRequirement;
        }

        /// <summary>
        /// Купить товар
        /// </summary>
        public bool PurchaseItem(ShopItem item, int quantity = 1)
        {
            if (!IsItemAvailable(item))
            {
                Debug.Log($"Товар {item.name} недоступен");
                PlayPurchaseFailSound();
                return false;
            }

            double totalCost = item.price * quantity * timePriceMultiplier;
            
            if (!moneySystem.CanAfford(totalCost))
            {
                Debug.Log($"Недостаточно денег для покупки {item.name}");
                PlayPurchaseFailSound();
                GameEvents.TriggerNotification("Недостаточно денег", NotificationType.Warning);
                return false;
            }

            // Проверка навыков (если есть требования)
            if (item.skillRequirements != null && skillSystem != null)
            {
                if (!skillSystem.CheckSkillRequirements(item.skillRequirements))
                {
                    Debug.Log($"Недостаточно навыков для покупки {item.name}");
                    PlayPurchaseFailSound();
                    GameEvents.TriggerNotification("Недостаточно навыков", NotificationType.Warning);
                    return false;
                }
            }

            // Покупка
            if (moneySystem.SpendMoney(totalCost, $"Purchase: {item.name} x{quantity}"))
            {
                // Применение эффектов
                ApplyItemEffects(item, quantity);
                
                // Добавление в купленные товары
                for (int i = 0; i < quantity; i++)
                {
                    purchasedItems.Add(item);
                }

                // Эффекты и события
                PlayPurchaseSuccessSound();
                OnItemPurchased?.Invoke(item);
                
                GameEvents.TriggerItemPurchased(new ShopEventData
                {
                    category = item.category,
                    itemName = item.name,
                    totalCost = totalCost,
                    quality = item.quality
                });

                GameEvents.TriggerNotification(
                    $"Куплено: {item.name} x{quantity}",
                    NotificationType.Success
                );

                Debug.Log($"Куплено: {item.name} x{quantity} за {GameUtils.FormatMoney(totalCost)}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Получить купленные товары
        /// </summary>
        public List<ShopItem> GetPurchasedItems()
        {
            return new List<ShopItem>(purchasedItems);
        }

        /// <summary>
        /// Получить купленные товары по категории
        /// </summary>
        public List<ShopItem> GetPurchasedItems(ShopCategory category)
        {
            return purchasedItems.Where(item => item.category == category).ToList();
        }

        /// <summary>
        /// Проверить, куплен ли товар
        /// </summary>
        public bool IsItemPurchased(ShopItem item)
        {
            return purchasedItems.Any(purchasedItem => 
                purchasedItem.name == item.name && 
                purchasedItem.category == item.category);
        }

        /// <summary>
        /// Получить товар по ID
        /// </summary>
        /// <param name="itemId">Уникальный идентификатор товара</param>
        /// <returns>Товар или null, если не найден</returns>
        public ShopItem GetItemById(string itemId)
        {
            foreach (var category in shopItems.Values)
            {
                var item = category.FirstOrDefault(i => i.id == itemId);
                if (item != null)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Получить название категории
        /// </summary>
        public string GetCategoryName(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Food: return "Еда";
                case ShopCategory.Clothing: return "Одежда";
                case ShopCategory.Housing: return "Жилье";
                case ShopCategory.Transport: return "Транспорт";
                case ShopCategory.Education: return "Образование";
                case ShopCategory.Entertainment: return "Развлечения";
                default: return category.ToString();
            }
        }

        #endregion

        #region Item Effects

        /// <summary>
        /// Применить эффекты товара
        /// </summary>
        private void ApplyItemEffects(ShopItem item, int quantity)
        {
            // Временные эффекты на характеристики
            if (item.statEffects != null)
            {
                foreach (var effect in item.statEffects)
                {
                    float totalEffect = effect.Value * quantity;
                    ApplyStatEffect(effect.Key, totalEffect);
                }
            }

            // Постоянные эффекты
            if (item.permanentEffects != null)
            {
                foreach (var effect in item.permanentEffects)
                {
                    ApplyPermanentEffect(effect.Key, effect.Value);
                }
            }
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
                
                case StatType.Money:
                    if (value > 0)
                        moneySystem.AddMoney(value, "Item Effect");
                    else
                        moneySystem.SpendMoney(-value, "Item Effect");
                    break;
            }
        }

        /// <summary>
        /// Применить постоянный эффект
        /// </summary>
        private void ApplyPermanentEffect(string effectType, float value)
        {
            // Здесь можно добавить постоянные эффекты
            // Например, через PlayerModifiers или подобную систему
            Debug.Log($"Применен постоянный эффект: {effectType} +{value:P1}");
        }

        #endregion

        #region Audio

        /// <summary>
        /// Воспроизвести звук успешной покупки
        /// </summary>
        private void PlayPurchaseSuccessSound()
        {
            if (purchaseSuccessSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(purchaseSuccessSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук неудачной покупки
        /// </summary>
        private void PlayPurchaseFailSound()
        {
            if (purchaseFailSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(purchaseFailSound);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Обработчик повышения уровня
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            // Проверить разблокировку новых категорий
            foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
            {
                if (IsCategoryUnlocked(category) && data.oldLevel < GetCategoryUnlockLevel(category))
                {
                    OnCategoryUnlocked?.Invoke(category);
                    GameEvents.TriggerNotification(
                        $"Разблокирована категория: {GetCategoryName(category)}",
                        NotificationType.Achievement
                    );
                }
            }
        }

        /// <summary>
        /// Получить уровень разблокировки категории
        /// </summary>
        private int GetCategoryUnlockLevel(ShopCategory category)
        {
            switch (category)
            {
                case ShopCategory.Food: return 1;
                case ShopCategory.Clothing: return 5;
                case ShopCategory.Entertainment: return 10;
                case ShopCategory.Education: return 15;
                case ShopCategory.Housing: return 20;
                case ShopCategory.Transport: return 25;
                default: return 1;
            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public ShopSystemSaveData GetSaveData()
        {
            var purchasedItemsData = new List<PurchasedItemData>();
            
            foreach (var item in purchasedItems)
            {
                purchasedItemsData.Add(new PurchasedItemData
                {
                    name = item.name,
                    category = item.category.ToString(),
                    purchaseTime = DateTime.Now.ToBinary()
                });
            }

            return new ShopSystemSaveData
            {
                purchasedItems = purchasedItemsData
            };
        }

        /// <summary>
        /// Загрузить данные
        /// </summary>
        public void LoadData(ShopSystemSaveData data)
        {
            if (data?.purchasedItems != null)
            {
                purchasedItems.Clear();
                
                foreach (var itemData in data.purchasedItems)
                {
                    if (Enum.TryParse<ShopCategory>(itemData.category, out ShopCategory category))
                    {
                        var item = shopItems[category].FirstOrDefault(i => i.name == itemData.name);
                        if (item != null)
                        {
                            purchasedItems.Add(item);
                            
                            // Повторно применить постоянные эффекты
                            if (item.permanentEffects != null)
                            {
                                foreach (var effect in item.permanentEffects)
                                {
                                    ApplyPermanentEffect(effect.Key, effect.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Показать все товары")]
        private void DebugShowAllItems()
        {
            foreach (var category in shopItems)
            {
                Debug.Log($"=== {GetCategoryName(category.Key)} ===");
                foreach (var item in category.Value)
                {
                    Debug.Log($"{item.name} - {GameUtils.FormatMoney(item.price)} " +
                             $"(Уровень: {item.levelRequirement}, Качество: {item.quality})");
                }
            }
        }

        [ContextMenu("Купить случайный товар")]
        private void DebugBuyRandomItem()
        {
            var availableCategories = GetAvailableCategories();
            if (availableCategories.Count > 0)
            {
                var randomCategory = availableCategories[UnityEngine.Random.Range(0, availableCategories.Count)];
                var categoryItems = GetCategoryItems(randomCategory);
                if (categoryItems.Count > 0)
                {
                    var randomItem = categoryItems[UnityEngine.Random.Range(0, categoryItems.Count)];
                    PurchaseItem(randomItem);
                }
            }
        }

        /// <summary>
        /// Задать множитель цен на основе времени суток
        /// </summary>
        public void SetTimeBasedPriceMultiplier(float multiplier)
        {
            timePriceMultiplier = Mathf.Max(0.1f, multiplier);
        }

        #endregion
    }

    /// <summary>
    /// Товар в магазине
    /// </summary>
    [System.Serializable]
    public class ShopItem
    {
        public string id;           // Уникальный идентификатор
        public string name;
        public ShopCategory category;
        public ItemQuality quality;
        public double price;
        public int levelRequirement;
        public string description;
        
        // Эффекты
        public Dictionary<StatType, float> statEffects;
        public Dictionary<string, float> permanentEffects;
        public Dictionary<SkillType, int> skillRequirements;
        
        // Метаданные
        public bool isConsumable = true;
        public int maxStack = 1;

        public ShopItem(string id, string name, ShopCategory category, ItemQuality quality, double price,
                       int levelRequirement, string description, Dictionary<StatType, float> statEffects = null,
                       Dictionary<string, float> permanentEffects = null,
                       Dictionary<SkillType, int> skillRequirements = null,
                       bool isConsumable = true, int maxStack = 1)
        {
            this.id = id;
            this.name = name;
            this.category = category;
            this.quality = quality;
            this.price = price;
            this.levelRequirement = levelRequirement;
            this.description = description;
            this.statEffects = statEffects ?? new Dictionary<StatType, float>();
            this.permanentEffects = permanentEffects ?? new Dictionary<string, float>();
            this.skillRequirements = skillRequirements;
            this.isConsumable = isConsumable;
            this.maxStack = maxStack;
        }


        public ShopItem(string name, ShopCategory category, ItemQuality quality, double price,
                       int levelRequirement, string description, Dictionary<StatType, float> statEffects = null,
                       Dictionary<string, float> permanentEffects = null,
                       bool isConsumable = true, int maxStack = 1)
            : this(Guid.NewGuid().ToString(), name, category, quality, price, levelRequirement,
                   description, statEffects, permanentEffects, null, isConsumable, maxStack)
        {
        }
    }

    /// <summary>
    /// Данные системы магазина для сохранения
    /// </summary>
    [System.Serializable]

}
