using System;
using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Класс оружия
    /// </summary>
    [System.Serializable]
    public class Weapon
    {
        [Header("Основная информация")]
        public WeaponType weaponType;           // Тип оружия
        public string customName;               // Пользовательское имя
        public string description;              // Описание
        
        [Header("Характеристики урона")]
        public float minDamage;                 // Минимальный урон
        public float maxDamage;                 // Максимальный урон
        public float accuracy;                  // Точность (0-1)
        public float criticalChance;            // Шанс критического удара (0-1)
        public DamageType damageType;           // Тип урона
        
        [Header("Прочность и состояние")]
        public float durability;                // Текущая прочность (0-100)
        public float maxDurability;             // Максимальная прочность
        public WeaponCondition condition;       // Состояние оружия
        public bool canBeRepaired;              // Можно ли починить
        
        [Header("Физические свойства")]
        public float weight;                    // Вес в кг
        public float length;                    // Длина в см
        public bool isTwoHanded;                // Двуручное оружие
        public bool isThrowable;                // Можно ли метать
        
        [Header("Экономика")]
        public double purchasePrice;            // Цена покупки
        public double sellPrice;                // Цена продажи
        public bool isLegal;                    // Легальное оружие
        public bool requiresLicense;            // Требует лицензию
        
        [Header("Эффекты")]
        public float stunChance;                // Шанс оглушения
        public float bleedChance;               // Шанс кровотечения
        public float intimidationBonus;         // Бонус к запугиванию
        public float concealability;            // Возможность скрытого ношения (0-1)
        
        public Weapon()
        {
            weaponType = WeaponType.None;
            condition = WeaponCondition.Good;
            durability = 100f;
            maxDurability = 100f;
            canBeRepaired = true;
            isLegal = true;
        }
        
        /// <summary>
        /// Получить отображаемое имя
        /// </summary>
        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(customName))
                return customName;
                
            return GetWeaponTypeName(weaponType);
        }
        
        /// <summary>
        /// Получить название типа оружия
        /// </summary>
        private string GetWeaponTypeName(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.None: return "Кулаки";
                case WeaponType.Knife: return "Нож";
                case WeaponType.Bat: return "Бита";
                case WeaponType.Club: return "Дубинка";
                case WeaponType.Hammer: return "Молоток";
                case WeaponType.Chain: return "Цепь";
                case WeaponType.BrokenBottle: return "Разбитая бутылка";
                case WeaponType.Pistol: return "Пистолет";
                case WeaponType.PepperSpray: return "Перцовый баллончик";
                case WeaponType.Taser: return "Электрошокер";
                case WeaponType.Knuckles: return "Кастет";
                case WeaponType.Crowbar: return "Лом";
                case WeaponType.Machete: return "Мачете";
                case WeaponType.Sword: return "Меч";
                case WeaponType.Axe: return "Топор";
                default: return type.ToString();
            }
        }
        
        /// <summary>
        /// Получить полное описание оружия
        /// </summary>
        public string GetFullDescription()
        {
            string desc = $"{GetDisplayName()}\n";
            desc += $"Урон: {minDamage:F0}-{maxDamage:F0}\n";
            desc += $"Точность: {accuracy * 100:F0}%\n";
            desc += $"Критический удар: {criticalChance * 100:F0}%\n";
            desc += $"Состояние: {GetConditionName(condition)}\n";
            desc += $"Прочность: {durability:F0}/{maxDurability:F0}\n";
            desc += $"Вес: {weight:F1} кг\n";
            
            if (!string.IsNullOrEmpty(description))
            {
                desc += $"\n{description}";
            }
            
            return desc;
        }
        
        /// <summary>
        /// Получить название состояния
        /// </summary>
        private string GetConditionName(WeaponCondition cond)
        {
            switch (cond)
            {
                case WeaponCondition.Perfect: return "Идеальное";
                case WeaponCondition.Excellent: return "Отличное";
                case WeaponCondition.Good: return "Хорошее";
                case WeaponCondition.Fair: return "Удовлетворительное";
                case WeaponCondition.Poor: return "Плохое";
                case WeaponCondition.Broken: return "Сломано";
                default: return cond.ToString();
            }
        }
        
        /// <summary>
        /// Рассчитать случайный урон
        /// </summary>
        public float CalculateDamage()
        {
            float baseDamage = UnityEngine.Random.Range(minDamage, maxDamage);
            
            // Модификатор состояния
            float conditionModifier = GetConditionModifier();
            
            return baseDamage * conditionModifier;
        }
        
        /// <summary>
        /// Получить модификатор состояния
        /// </summary>
        public float GetConditionModifier()
        {
            switch (condition)
            {
                case WeaponCondition.Perfect: return 1.2f;
                case WeaponCondition.Excellent: return 1.1f;
                case WeaponCondition.Good: return 1.0f;
                case WeaponCondition.Fair: return 0.85f;
                case WeaponCondition.Poor: return 0.7f;
                case WeaponCondition.Broken: return 0.3f;
                default: return 1.0f;
            }
        }
        
        /// <summary>
        /// Получить эффективную точность с учетом состояния
        /// </summary>
        public float GetEffectiveAccuracy()
        {
            return accuracy * GetConditionModifier();
        }
        
        /// <summary>
        /// Получить эффективный шанс критического удара
        /// </summary>
        public float GetEffectiveCriticalChance()
        {
            return criticalChance * GetConditionModifier();
        }
        
        /// <summary>
        /// Обновить состояние на основе прочности
        /// </summary>
        public void UpdateConditionFromDurability()
        {
            float durabilityPercent = durability / maxDurability;
            
            if (durabilityPercent <= 0f)
                condition = WeaponCondition.Broken;
            else if (durabilityPercent <= 0.2f)
                condition = WeaponCondition.Poor;
            else if (durabilityPercent <= 0.4f)
                condition = WeaponCondition.Fair;
            else if (durabilityPercent <= 0.7f)
                condition = WeaponCondition.Good;
            else if (durabilityPercent <= 0.9f)
                condition = WeaponCondition.Excellent;
            else
                condition = WeaponCondition.Perfect;
        }
        
        /// <summary>
        /// Можно ли использовать оружие
        /// </summary>
        public bool CanUse()
        {
            return condition != WeaponCondition.Broken && durability > 0;
        }
        
        /// <summary>
        /// Получить стоимость ремонта
        /// </summary>
        public double GetRepairCost()
        {
            if (!canBeRepaired || condition != WeaponCondition.Broken)
                return 0;
                
            return purchasePrice * 0.3; // 30% от стоимости покупки
        }
        
        /// <summary>
        /// Создать оружие "Кулаки"
        /// </summary>
        public static Weapon CreateFists()
        {
            return new Weapon
            {
                weaponType = WeaponType.None,
                minDamage = 3f,
                maxDamage = 8f,
                accuracy = 0.8f,
                criticalChance = 0.05f,
                damageType = DamageType.Blunt,
                durability = 100f,
                maxDurability = 100f,
                condition = WeaponCondition.Perfect,
                weight = 0f,
                length = 0f,
                isTwoHanded = false,
                isThrowable = false,
                purchasePrice = 0,
                sellPrice = 0,
                isLegal = true,
                requiresLicense = false,
                stunChance = 0.1f,
                bleedChance = 0f,
                intimidationBonus = 0f,
                concealability = 1f,
                canBeRepaired = false,
                description = "Ваши собственные кулаки. Всегда при вас, но не самый эффективный способ решения проблем."
            };
        }
        
        /// <summary>
        /// Создать оружие определенного типа
        /// </summary>
        public static Weapon CreateWeapon(WeaponType type)
        {
            Weapon weapon = new Weapon();
            weapon.weaponType = type;
            
            switch (type)
            {
                case WeaponType.None:
                    return CreateFists();
                    
                case WeaponType.Knife:
                    weapon.minDamage = 8f;
                    weapon.maxDamage = 15f;
                    weapon.accuracy = 0.85f;
                    weapon.criticalChance = 0.15f;
                    weapon.damageType = DamageType.Piercing;
                    weapon.weight = 0.3f;
                    weapon.length = 15f;
                    weapon.purchasePrice = 25;
                    weapon.sellPrice = 10;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.05f;
                    weapon.bleedChance = 0.3f;
                    weapon.intimidationBonus = 0.2f;
                    weapon.concealability = 0.8f;
                    weapon.description = "Острый кухонный нож. Смертельное оружие в умелых руках.";
                    break;
                    
                case WeaponType.Bat:
                    weapon.minDamage = 12f;
                    weapon.maxDamage = 20f;
                    weapon.accuracy = 0.75f;
                    weapon.criticalChance = 0.1f;
                    weapon.damageType = DamageType.Blunt;
                    weapon.weight = 1.2f;
                    weapon.length = 80f;
                    weapon.isTwoHanded = true;
                    weapon.purchasePrice = 40;
                    weapon.sellPrice = 15;
                    weapon.isLegal = true;
                    weapon.stunChance = 0.25f;
                    weapon.bleedChance = 0.05f;
                    weapon.intimidationBonus = 0.3f;
                    weapon.concealability = 0.3f;
                    weapon.description = "Бейсбольная бита. Отличное оружие для ближнего боя.";
                    break;
                    
                case WeaponType.Club:
                    weapon.minDamage = 10f;
                    weapon.maxDamage = 18f;
                    weapon.accuracy = 0.8f;
                    weapon.criticalChance = 0.12f;
                    weapon.damageType = DamageType.Blunt;
                    weapon.weight = 0.8f;
                    weapon.length = 60f;
                    weapon.purchasePrice = 30;
                    weapon.sellPrice = 12;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.2f;
                    weapon.bleedChance = 0.02f;
                    weapon.intimidationBonus = 0.25f;
                    weapon.concealability = 0.4f;
                    weapon.description = "Полицейская дубинка. Эффективное оружие для самообороны.";
                    break;
                    
                case WeaponType.Hammer:
                    weapon.minDamage = 15f;
                    weapon.maxDamage = 25f;
                    weapon.accuracy = 0.7f;
                    weapon.criticalChance = 0.15f;
                    weapon.damageType = DamageType.Blunt;
                    weapon.weight = 1.5f;
                    weapon.length = 35f;
                    weapon.purchasePrice = 20;
                    weapon.sellPrice = 8;
                    weapon.isLegal = true;
                    weapon.stunChance = 0.3f;
                    weapon.bleedChance = 0.1f;
                    weapon.intimidationBonus = 0.2f;
                    weapon.concealability = 0.5f;
                    weapon.description = "Тяжелый молоток. Строительный инструмент или оружие.";
                    break;
                    
                case WeaponType.Chain:
                    weapon.minDamage = 6f;
                    weapon.maxDamage = 14f;
                    weapon.accuracy = 0.65f;
                    weapon.criticalChance = 0.08f;
                    weapon.damageType = DamageType.Blunt;
                    weapon.weight = 2.0f;
                    weapon.length = 100f;
                    weapon.purchasePrice = 15;
                    weapon.sellPrice = 5;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.15f;
                    weapon.bleedChance = 0.05f;
                    weapon.intimidationBonus = 0.35f;
                    weapon.concealability = 0.6f;
                    weapon.description = "Тяжелая металлическая цепь. Устрашающее оружие.";
                    break;
                    
                case WeaponType.BrokenBottle:
                    weapon.minDamage = 5f;
                    weapon.maxDamage = 12f;
                    weapon.accuracy = 0.6f;
                    weapon.criticalChance = 0.2f;
                    weapon.damageType = DamageType.Slashing;
                    weapon.weight = 0.4f;
                    weapon.length = 20f;
                    weapon.purchasePrice = 0;
                    weapon.sellPrice = 0;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.05f;
                    weapon.bleedChance = 0.4f;
                    weapon.intimidationBonus = 0.15f;
                    weapon.concealability = 0.7f;
                    weapon.maxDurability = 30f;
                    weapon.durability = 30f;
                    weapon.description = "Разбитая стеклянная бутылка. Опасное импровизированное оружие.";
                    break;
                    
                case WeaponType.Pistol:
                    weapon.minDamage = 25f;
                    weapon.maxDamage = 40f;
                    weapon.accuracy = 0.9f;
                    weapon.criticalChance = 0.25f;
                    weapon.damageType = DamageType.Ballistic;
                    weapon.weight = 0.8f;
                    weapon.length = 20f;
                    weapon.purchasePrice = 500;
                    weapon.sellPrice = 200;
                    weapon.isLegal = false;
                    weapon.requiresLicense = true;
                    weapon.stunChance = 0.1f;
                    weapon.bleedChance = 0.5f;
                    weapon.intimidationBonus = 0.8f;
                    weapon.concealability = 0.6f;
                    weapon.description = "Пистолет калибра 9мм. Смертельное огнестрельное оружие.";
                    break;
                    
                case WeaponType.PepperSpray:
                    weapon.minDamage = 2f;
                    weapon.maxDamage = 5f;
                    weapon.accuracy = 0.95f;
                    weapon.criticalChance = 0.05f;
                    weapon.damageType = DamageType.Chemical;
                    weapon.weight = 0.1f;
                    weapon.length = 10f;
                    weapon.purchasePrice = 15;
                    weapon.sellPrice = 5;
                    weapon.isLegal = true;
                    weapon.stunChance = 0.8f;
                    weapon.bleedChance = 0f;
                    weapon.intimidationBonus = 0.1f;
                    weapon.concealability = 0.95f;
                    weapon.maxDurability = 50f;
                    weapon.durability = 50f;
                    weapon.description = "Перцовый баллончик. Эффективное средство самообороны.";
                    break;
                    
                case WeaponType.Taser:
                    weapon.minDamage = 1f;
                    weapon.maxDamage = 3f;
                    weapon.accuracy = 0.85f;
                    weapon.criticalChance = 0.1f;
                    weapon.damageType = DamageType.Electric;
                    weapon.weight = 0.3f;
                    weapon.length = 15f;
                    weapon.purchasePrice = 80;
                    weapon.sellPrice = 30;
                    weapon.isLegal = true;
                    weapon.requiresLicense = true;
                    weapon.stunChance = 0.9f;
                    weapon.bleedChance = 0f;
                    weapon.intimidationBonus = 0.3f;
                    weapon.concealability = 0.8f;
                    weapon.maxDurability = 60f;
                    weapon.durability = 60f;
                    weapon.description = "Электрошокер. Несмертельное оружие для обезвреживания.";
                    break;
                    
                case WeaponType.Knuckles:
                    weapon.minDamage = 8f;
                    weapon.maxDamage = 15f;
                    weapon.accuracy = 0.9f;
                    weapon.criticalChance = 0.12f;
                    weapon.damageType = DamageType.Blunt;
                    weapon.weight = 0.5f;
                    weapon.length = 0f;
                    weapon.purchasePrice = 35;
                    weapon.sellPrice = 15;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.15f;
                    weapon.bleedChance = 0.1f;
                    weapon.intimidationBonus = 0.2f;
                    weapon.concealability = 0.9f;
                    weapon.description = "Металлические кастеты. Усиливают удар кулаком.";
                    break;
                    
                case WeaponType.Crowbar:
                    weapon.minDamage = 14f;
                    weapon.maxDamage = 22f;
                    weapon.accuracy = 0.75f;
                    weapon.criticalChance = 0.1f;
                    weapon.damageType = DamageType.Blunt;
                    weapon.weight = 1.8f;
                    weapon.length = 60f;
                    weapon.purchasePrice = 25;
                    weapon.sellPrice = 10;
                    weapon.isLegal = true;
                    weapon.stunChance = 0.2f;
                    weapon.bleedChance = 0.05f;
                    weapon.intimidationBonus = 0.25f;
                    weapon.concealability = 0.3f;
                    weapon.description = "Тяжелый лом. Универсальный инструмент и оружие.";
                    break;
                    
                case WeaponType.Machete:
                    weapon.minDamage = 18f;
                    weapon.maxDamage = 28f;
                    weapon.accuracy = 0.8f;
                    weapon.criticalChance = 0.18f;
                    weapon.damageType = DamageType.Slashing;
                    weapon.weight = 1.0f;
                    weapon.length = 50f;
                    weapon.purchasePrice = 60;
                    weapon.sellPrice = 25;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.05f;
                    weapon.bleedChance = 0.4f;
                    weapon.intimidationBonus = 0.5f;
                    weapon.concealability = 0.2f;
                    weapon.description = "Большой нож-мачете. Крайне опасное холодное оружие.";
                    break;
                    
                case WeaponType.Sword:
                    weapon.minDamage = 20f;
                    weapon.maxDamage = 35f;
                    weapon.accuracy = 0.85f;
                    weapon.criticalChance = 0.2f;
                    weapon.damageType = DamageType.Slashing;
                    weapon.weight = 1.5f;
                    weapon.length = 80f;
                    weapon.isTwoHanded = true;
                    weapon.purchasePrice = 200;
                    weapon.sellPrice = 80;
                    weapon.isLegal = false;
                    weapon.stunChance = 0.05f;
                    weapon.bleedChance = 0.5f;
                    weapon.intimidationBonus = 0.7f;
                    weapon.concealability = 0.1f;
                    weapon.description = "Настоящий меч. Редкое и чрезвычайно опасное оружие.";
                    break;
                    
                case WeaponType.Axe:
                    weapon.minDamage = 22f;
                    weapon.maxDamage = 35f;
                    weapon.accuracy = 0.7f;
                    weapon.criticalChance = 0.25f;
                    weapon.damageType = DamageType.Slashing;
                    weapon.weight = 2.5f;
                    weapon.length = 70f;
                    weapon.isTwoHanded = true;
                    weapon.purchasePrice = 45;
                    weapon.sellPrice = 20;
                    weapon.isLegal = true;
                    weapon.stunChance = 0.1f;
                    weapon.bleedChance = 0.6f;
                    weapon.intimidationBonus = 0.6f;
                    weapon.concealability = 0.1f;
                    weapon.description = "Тяжелый топор. Инструмент дровосека или варварское оружие.";
                    break;
            }
            
            return weapon;
        }
        
        /// <summary>
        /// Создать случайное оружие
        /// </summary>
        public static Weapon CreateRandomWeapon()
        {
            Array weaponTypes = Enum.GetValues(typeof(WeaponType));
            WeaponType randomType = (WeaponType)weaponTypes.GetValue(
                UnityEngine.Random.Range(1, weaponTypes.Length)); // Исключаем None
                
            Weapon weapon = CreateWeapon(randomType);
            
            // Случайное состояние
            weapon.condition = (WeaponCondition)UnityEngine.Random.Range(1, 6);
            weapon.durability = UnityEngine.Random.Range(20f, 100f);
            weapon.UpdateConditionFromDurability();
            
            return weapon;
        }
        
        /// <summary>
        /// Копировать оружие
        /// </summary>
        public Weapon Clone()
        {
            return new Weapon
            {
                weaponType = this.weaponType,
                customName = this.customName,
                description = this.description,
                minDamage = this.minDamage,
                maxDamage = this.maxDamage,
                accuracy = this.accuracy,
                criticalChance = this.criticalChance,
                damageType = this.damageType,
                durability = this.durability,
                maxDurability = this.maxDurability,
                condition = this.condition,
                canBeRepaired = this.canBeRepaired,
                weight = this.weight,
                length = this.length,
                isTwoHanded = this.isTwoHanded,
                isThrowable = this.isThrowable,
                purchasePrice = this.purchasePrice,
                sellPrice = this.sellPrice,
                isLegal = this.isLegal,
                requiresLicense = this.requiresLicense,
                stunChance = this.stunChance,
                bleedChance = this.bleedChance,
                intimidationBonus = this.intimidationBonus,
                concealability = this.concealability
            };
        }
    }
}