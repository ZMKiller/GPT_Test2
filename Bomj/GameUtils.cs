using UnityEngine;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Утилитарные функции и константы для игры
    /// </summary>
    public static class GameUtils
    {
        // Константы игры
        public const float MIN_STAT_VALUE = 0f;
        public const float MAX_STAT_VALUE = 100f;
        public const int MIN_LEVEL = 1;
        public const int MAX_LEVEL = 999;
        
        // Константы времени
        public const float SECONDS_IN_MINUTE = 60f;
        public const float SECONDS_IN_HOUR = 3600f;
        public const float SECONDS_IN_DAY = 86400f;
        
        // Пороги для состояний
        public const float CRITICAL_HEALTH_THRESHOLD = 10f;
        public const float LOW_HEALTH_THRESHOLD = 20f;
        public const float HIGH_HUNGER_THRESHOLD = 80f;
        public const float CRITICAL_HUNGER_THRESHOLD = 95f;
        public const float LOW_MOOD_THRESHOLD = 25f;
        
        /// <summary>
        /// Преобразовать игровое время в читаемый формат
        /// </summary>
        /// <param name="seconds">Время в секундах</param>
        /// <returns>Форматированная строка времени</returns>
        public static string FormatGameTime(float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int days = totalSeconds / (int)SECONDS_IN_DAY;
            int hours = (totalSeconds % (int)SECONDS_IN_DAY) / (int)SECONDS_IN_HOUR;
            int minutes = (totalSeconds % (int)SECONDS_IN_HOUR) / (int)SECONDS_IN_MINUTE;
            int secs = totalSeconds % (int)SECONDS_IN_MINUTE;
            
            if (days > 0)
                return $"{days}д {hours:D2}:{minutes:D2}:{secs:D2}";
            else
                return $"{hours:D2}:{minutes:D2}:{secs:D2}";
        }

        /// <summary>
        /// Форматировать деньги с разделителями тысяч
        /// </summary>
        /// <param name="amount">Сумма денег</param>
        /// <returns>Форматированная строка</returns>
        public static string FormatMoney(float amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000:F1}М";
            else if (amount >= 1000)
                return $"{amount / 1000:F1}К";
            else
                return $"{amount:F0}";
        }

        /// <summary>
        /// Получить цвет для отображения характеристики на основе её значения
        /// </summary>
        /// <param name="statType">Тип характеристики</param>
        /// <param name="value">Текущее значение</param>
        /// <param name="maxValue">Максимальное значение</param>
        /// <returns>Цвет для отображения</returns>
        public static Color GetStatColor(StatType statType, float value, float maxValue)
        {
            float percentage = value / maxValue;
            
            switch (statType)
            {
                case StatType.Health:
                    if (percentage <= CRITICAL_HEALTH_THRESHOLD / 100f)
                        return Color.red;
                    else if (percentage <= LOW_HEALTH_THRESHOLD / 100f)
                        return Color.yellow;
                    else
                        return Color.green;
                
                case StatType.Hunger:
                    if (percentage >= CRITICAL_HUNGER_THRESHOLD / 100f)
                        return Color.red;
                    else if (percentage >= HIGH_HUNGER_THRESHOLD / 100f)
                        return Color.yellow;
                    else
                        return Color.green;
                
                case StatType.Mood:
                    if (percentage <= LOW_MOOD_THRESHOLD / 100f)
                        return Color.red;
                    else if (percentage <= 0.5f)
                        return Color.yellow;
                    else
                        return Color.green;
                
                case StatType.Money:
                    return Color.yellow;
                
                case StatType.Experience:
                    return Color.cyan;
                
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Получить описание состояния характеристики
        /// </summary>
        /// <param name="statType">Тип характеристики</param>
        /// <param name="value">Текущее значение</param>
        /// <param name="maxValue">Максимальное значение</param>
        /// <returns>Текстовое описание состояния</returns>
        public static string GetStatDescription(StatType statType, float value, float maxValue)
        {
            float percentage = value / maxValue;
            
            switch (statType)
            {
                case StatType.Health:
                    if (percentage <= CRITICAL_HEALTH_THRESHOLD / 100f)
                        return "Критическое состояние";
                    else if (percentage <= LOW_HEALTH_THRESHOLD / 100f)
                        return "Плохое самочувствие";
                    else if (percentage <= 0.5f)
                        return "Удовлетворительно";
                    else if (percentage <= 0.8f)
                        return "Хорошее состояние";
                    else
                        return "Отличное здоровье";
                
                case StatType.Hunger:
                    if (percentage >= CRITICAL_HUNGER_THRESHOLD / 100f)
                        return "Умираю от голода";
                    else if (percentage >= HIGH_HUNGER_THRESHOLD / 100f)
                        return "Очень голоден";
                    else if (percentage >= 0.5f)
                        return "Проголодался";
                    else if (percentage >= 0.2f)
                        return "Немного голоден";
                    else
                        return "Сыт";
                
                case StatType.Mood:
                    if (percentage <= LOW_MOOD_THRESHOLD / 100f)
                        return "Депрессия";
                    else if (percentage <= 0.4f)
                        return "Плохое настроение";
                    else if (percentage <= 0.6f)
                        return "Нейтральное";
                    else if (percentage <= 0.8f)
                        return "Хорошее настроение";
                    else
                        return "Отличное настроение";
                
                default:
                    return "Норма";
            }
        }

        /// <summary>
        /// Рассчитать опыт, необходимый для достижения определенного уровня
        /// </summary>
        /// <param name="level">Целевой уровень</param>
        /// <param name="baseExperience">Базовое количество опыта для 2 уровня</param>
        /// <param name="multiplier">Множитель роста опыта</param>
        /// <returns>Общее количество опыта</returns>
        public static float CalculateExperienceForLevel(int level, float baseExperience = 100f, float multiplier = 1.5f)
        {
            if (level <= 1) return 0f;
            
            float totalExperience = 0f;
            float currentLevelExp = baseExperience;
            
            for (int i = 2; i <= level; i++)
            {
                totalExperience += currentLevelExp;
                currentLevelExp *= multiplier;
            }
            
            return totalExperience;
        }

        /// <summary>
        /// Рассчитать уровень по общему количеству опыта
        /// </summary>
        /// <param name="totalExperience">Общее количество опыта</param>
        /// <param name="baseExperience">Базовое количество опыта для 2 уровня</param>
        /// <param name="multiplier">Множитель роста опыта</param>
        /// <returns>Уровень игрока</returns>
        public static int CalculateLevelFromExperience(float totalExperience, float baseExperience = 100f, float multiplier = 1.5f)
        {
            if (totalExperience <= 0f) return 1;
            
            int level = 1;
            float experienceUsed = 0f;
            float currentLevelExp = baseExperience;
            
            while (experienceUsed + currentLevelExp <= totalExperience)
            {
                experienceUsed += currentLevelExp;
                level++;
                currentLevelExp *= multiplier;
                
                if (level >= MAX_LEVEL) break;
            }
            
            return level;
        }

        /// <summary>
        /// Интерполировать между двумя значениями с учетом времени
        /// </summary>
        /// <param name="from">Начальное значение</param>
        /// <param name="to">Конечное значение</param>
        /// <param name="time">Текущее время (0-1)</param>
        /// <param name="curve">Кривая интерполяции</param>
        /// <returns>Интерполированное значение</returns>
        public static float SmoothInterpolate(float from, float to, float time, AnimationCurve curve = null)
        {
            if (curve != null)
            {
                time = curve.Evaluate(time);
            }
            else
            {
                // Плавная кривая по умолчанию
                time = Mathf.SmoothStep(0f, 1f, time);
            }
            
            return Mathf.Lerp(from, to, time);
        }

        /// <summary>
        /// Проверить является ли значение в критическом диапазоне
        /// </summary>
        /// <param name="statType">Тип характеристики</param>
        /// <param name="value">Значение</param>
        /// <param name="maxValue">Максимальное значение</param>
        /// <returns>True если значение критическое</returns>
        public static bool IsCriticalValue(StatType statType, float value, float maxValue)
        {
            float percentage = value / maxValue;
            
            switch (statType)
            {
                case StatType.Health:
                    return percentage <= CRITICAL_HEALTH_THRESHOLD / 100f;
                case StatType.Hunger:
                    return percentage >= CRITICAL_HUNGER_THRESHOLD / 100f;
                case StatType.Mood:
                    return percentage <= LOW_MOOD_THRESHOLD / 100f;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Генерировать случайное значение с нормальным распределением
        /// </summary>
        /// <param name="mean">Среднее значение</param>
        /// <param name="standardDeviation">Стандартное отклонение</param>
        /// <returns>Случайное значение</returns>
        public static float RandomNormal(float mean, float standardDeviation)
        {
            // Использование метода Бокса-Мюллера
            float u1 = Random.Range(0f, 1f);
            float u2 = Random.Range(0f, 1f);
            
            float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
            
            return z0 * standardDeviation + mean;
        }

        /// <summary>
        /// Ограничить значение в пределах минимума и максимума
        /// </summary>
        /// <param name="value">Значение для ограничения</param>
        /// <param name="min">Минимальное значение</param>
        /// <param name="max">Максимальное значение</param>
        /// <returns>Ограниченное значение</returns>
        public static float ClampStat(float value, float min = MIN_STAT_VALUE, float max = MAX_STAT_VALUE)
        {
            return Mathf.Clamp(value, min, max);
        }
    }

    /// <summary>
    /// Расширения для удобства работы с компонентами Unity
    /// </summary>
    public static class UnityExtensions
    {
        /// <summary>
        /// Безопасно получить компонент или добавить его, если он отсутствует
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <param name="gameObject">GameObject для поиска компонента</param>
        /// <returns>Найденный или добавленный компонент</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Безопасно найти компонент в дочерних объектах
        /// </summary>
        /// <typeparam name="T">Тип компонента</typeparam>
        /// <param name="transform">Transform для поиска</param>
        /// <param name="includeInactive">Включать неактивные объекты</param>
        /// <returns>Найденный компонент или null</returns>
        public static T SafeGetComponentInChildren<T>(this Transform transform, bool includeInactive = false) where T : Component
        {
            try
            {
                return transform.GetComponentInChildren<T>(includeInactive);
            }
            catch
            {
                return null;
            }
        }
    }
}