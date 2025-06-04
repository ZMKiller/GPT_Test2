using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Система управления валютой игры
    /// </summary>
    public class MoneySystem : MonoBehaviour
    {
        [Header("Ссылки на UI")]
        [SerializeField] private Text moneyText;                    // Текст отображения денег
        [SerializeField] private Text moneyChangeText;              // Текст изменения денег (+/-)
        [SerializeField] private Animator moneyUIAnimator;          // Аниматор UI денег
        
        [Header("Анимация монет")]
        [SerializeField] private GameObject coinEffectPrefab;       // Префаб эффекта монеты
        [SerializeField] private Transform coinEffectParent;        // Родитель для эффектов
        [SerializeField] private int maxCoinEffects = 20;           // Максимум эффектов на экране
        
        [Header("Настройки анимации")]
        [SerializeField] private float coinAnimationDuration = 1f;  // Длительность анимации монеты
        [SerializeField] private AnimationCurve coinMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float coinRandomSpread = 1f;       // Разброс появления монет
        
        [Header("Звуковые эффекты")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip coinSound;               // Звук получения монеты
        [SerializeField] private AudioClip spendSound;              // Звук траты денег
        [SerializeField] private AudioClip levelUpBonusSound;       // Звук бонуса за уровень
        
        [Header("Настройки отображения")]
        [SerializeField] private bool useShortFormat = true;        // Использовать короткий формат (1K, 1M)
        [SerializeField] private bool showChangeAnimation = true;   // Показывать анимацию изменения
        [SerializeField] private float changeTextDuration = 2f;     // Длительность показа изменения
        
        // Компоненты и ссылки
        private PlayerStats playerStats;
        private Camera mainCamera;
        private Queue<GameObject> coinEffectPool = new Queue<GameObject>();
        private List<GameObject> activeCoinEffects = new List<GameObject>();
        
        // Состояние системы
        private float currentMoney = 0f;
        private float displayedMoney = 0f;
        private bool isUpdatingDisplay = false;
        
        // События
        public Action<float> OnMoneyAdded;
        public Action<float> OnMoneySpent;
        public Action<double, double, string> OnMoneyChanged; // newAmount, change, source

        #region Unity Callbacks

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeSystem();
            SubscribeToEvents();
        }

        private void Update()
        {
            UpdateMoneyDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать компоненты
        /// </summary>
        private void InitializeComponents()
        {
            // Найти главную камеру
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }

            // Создать AudioSource если не назначен
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Создать родительский объект для эффектов
            if (coinEffectParent == null)
            {
                GameObject parent = new GameObject("CoinEffects");
                coinEffectParent = parent.transform;
            }
        }

        /// <summary>
        /// Инициализировать систему
        /// </summary>
        private void InitializeSystem()
        {
            // Найти PlayerStats
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null && GameManager.Instance != null)
            {
                playerStats = GameManager.Instance.PlayerStats;
            }

            if (playerStats == null)
            {
                Debug.LogError("MoneySystem: PlayerStats не найден!");
                return;
            }

            // Синхронизировать деньги
            currentMoney = playerStats.Money;
            displayedMoney = currentMoney;
            UpdateMoneyText();

            Debug.Log("MoneySystem инициализирована");
        }

        /// <summary>
        /// Подписаться на события
        /// </summary>
        private void SubscribeToEvents()
        {
            GameEvents.OnStatChanged += OnStatChanged;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnMoneyEarned += OnMoneyEarnedEvent;
        }

        /// <summary>
        /// Отписаться от событий
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEvents.OnStatChanged -= OnStatChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnMoneyEarned -= OnMoneyEarnedEvent;
        }

        #endregion

        #region Управление деньгами

        /// <summary>
        /// Добавить деньги игроку
        /// </summary>
        /// <param name="amount">Сумма денег</param>
        /// <param name="worldPosition">Позиция в мире для эффекта</param>
        public void AddMoney(float amount, Vector3 worldPosition)
        {
            AddMoney(amount, "Unknown", worldPosition);
        }

        /// <summary>
        /// Добавить деньги игроку с указанием источника
        /// </summary>
        /// <param name="amount">Сумма денег</param>
        /// <param name="source">Источник дохода</param>
        /// <param name="worldPosition">Позиция в мире для эффекта</param>
        public void AddMoney(double amount, string source, Vector3 worldPosition = default)
        {
            if (amount <= 0)
                return;

            // Применить модификаторы заработка
            double modifiedAmount = ApplyEarningModifiers(amount, source);

            // Добавить деньги через PlayerStats
            if (playerStats != null)
            {
                playerStats.ChangeMoney((float)modifiedAmount);
            }

            // Воспроизвести эффекты
            PlayCoinEffect(worldPosition, (float)modifiedAmount);
            PlayCoinSound();
            ShowMoneyChangeText($"+{FormatMoney((float)modifiedAmount)}", Color.green);

            // Создать событие заработка денег
            var moneyEvent = new MoneyEventData(modifiedAmount, source);
            GameEvents.TriggerMoneyEarned(moneyEvent);

            OnMoneyAdded?.Invoke((float)modifiedAmount);
            
            Debug.Log($"Добавлено {modifiedAmount:F1} денег из источника '{source}' в позиции {worldPosition}");
        }

        /// <summary>
        /// Применить модификаторы заработка
        /// </summary>
        private double ApplyEarningModifiers(double baseAmount, string source)
        {
            if (playerStats == null) return baseAmount;

            double modifiedAmount = baseAmount;

            // Базовый модификатор настроения (существующий)
            float moodModifier = playerStats.GetEarningModifier();
            modifiedAmount *= moodModifier;

            // Модификатор заработка от навыков и предметов
            float earningsMultiplier = playerStats.EarningsMultiplier;
            modifiedAmount *= (1.0 + earningsMultiplier);

            // Специальные модификаторы в зависимости от источника
            switch (source.ToLower())
            {
                case "npc":
                case "interaction":
                    // Модификатор харизмы для взаимодействий с NPC
                    float charismaBonus = playerStats.GetTotalModifierForStat(StatType.CharismaBonus);
                    modifiedAmount *= (1.0 + charismaBonus);
                    break;

                case "job":
                case "work":
                    // Модификатор эффективности работы
                    float workEfficiency = playerStats.GetTotalModifierForStat(StatType.WorkEfficiencyMultiplier);
                    modifiedAmount *= (1.0 + workEfficiency);
                    break;

                case "business":
                case "negotiation":
                    // Модификатор деловых навыков
                    float businessBonus = playerStats.GetTotalModifierForStat(StatType.BusinessSkillBonus);
                    float negotiationBonus = playerStats.GetTotalModifierForStat(StatType.NegotiationBonus);
                    modifiedAmount *= (1.0 + businessBonus + negotiationBonus);
                    break;
            }

            // Модификатор удачи (случайный бонус)
            float luckModifier = playerStats.GetTotalModifierForStat(StatType.LuckModifier);
            if (luckModifier > 0)
            {
                float criticalChance = playerStats.GetTotalModifierForStat(StatType.CriticalChance);
                if (UnityEngine.Random.value < criticalChance)
                {
                    modifiedAmount *= (1.5 + luckModifier * 0.1); // Критический заработок
                    ShowMoneyChangeText("КРИТИЧЕСКИЙ ЗАРАБОТОК!", Color.yellow);
                }
            }

            return modifiedAmount;
        }

        /// <summary>
        /// Потратить деньги
        /// </summary>
        /// <param name="amount">Сумма для траты</param>
        /// <param name="worldPosition">Позиция в мире для эффекта</param>
        /// <returns>True если деньги потрачены успешно</returns>
        public bool SpendMoney(float amount, Vector3 worldPosition = default)
        {
            if (amount <= 0f)
                return false;

            if (playerStats == null || !playerStats.CanAfford(amount))
                return false;

            // Потратить деньги через PlayerStats
            playerStats.ChangeMoney(-amount);

            // Воспроизвести эффекты
            PlaySpendSound();
            ShowMoneyChangeText($"-{FormatMoney(amount)}", Color.red);

            OnMoneySpent?.Invoke(amount);
            
            Debug.Log($"Потрачено {amount:F1} денег");
            return true;
        }

        /// <summary>
        /// Проверить может ли игрок потратить указанную сумму
        /// </summary>
        /// <param name="amount">Сумма для проверки</param>
        /// <returns>True если денег достаточно</returns>
        public bool CanAfford(float amount)
        {
            return playerStats != null && playerStats.CanAfford(amount);
        }

        /// <summary>
        /// Проверить может ли игрок потратить указанную сумму (алиас для CanAfford)
        /// </summary>
        /// <param name="amount">Сумма для проверки</param>
        /// <returns>True если денег достаточно</returns>
        public bool CanSpend(float amount)
        {
            return CanAfford(amount);
        }

        /// <summary>
        /// Получить текущую сумму денег
        /// </summary>
        /// <returns>Текущие деньги</returns>
        public float GetMoney()
        {
            return playerStats != null ? playerStats.Money : 0f;
        }

        /// <summary>
        /// Получить текущую сумму денег как double
        /// </summary>
        /// <returns>Текущие деньги</returns>
        public double GetMoneyDouble()
        {
            return playerStats != null ? playerStats.Money : 0.0;
        }

        /// <summary>
        /// Проверить может ли игрок потратить указанную сумму (double)
        /// </summary>
        /// <param name="amount">Сумма для проверки</param>
        /// <returns>True если денег достаточно</returns>
        public bool CanAfford(double amount)
        {
            return GetMoneyDouble() >= amount;
        }

        /// <summary>
        /// Потратить деньги (double версия)
        /// </summary>
        /// <param name="amount">Сумма для траты</param>
        /// <param name="source">Источник траты</param>
        /// <returns>True если деньги потрачены успешно</returns>
        public bool SpendMoney(double amount, string source = "Purchase")
        {
            if (amount <= 0)
                return false;

            if (!CanAfford(amount))
                return false;

            // Потратить деньги через PlayerStats
            if (playerStats != null)
            {
                playerStats.ChangeMoney(-(float)amount);
            }

            // Воспроизвести эффекты
            PlaySpendSound();
            ShowMoneyChangeText($"-{FormatMoney((float)amount)}", Color.red);

            // Вызвать событие изменения денег
            OnMoneyChanged?.Invoke(GetMoneyDouble(), -amount, source);
            OnMoneySpent?.Invoke((float)amount);
            
            Debug.Log($"Потрачено {amount:F1} денег на '{source}'");
            return true;
        }

        #endregion

        #region Отображение

        /// <summary>
        /// Обновить отображение денег
        /// </summary>
        private void UpdateMoneyDisplay()
        {
            if (playerStats == null)
                return;

            float targetMoney = playerStats.Money;
            
            if (Mathf.Abs(displayedMoney - targetMoney) > 0.01f)
            {
                // Плавное изменение отображаемых денег
                displayedMoney = Mathf.Lerp(displayedMoney, targetMoney, Time.deltaTime * 5f);
                UpdateMoneyText();
                
                // Если достигли цели, установить точное значение
                if (Mathf.Abs(displayedMoney - targetMoney) < 0.1f)
                {
                    displayedMoney = targetMoney;
                    UpdateMoneyText();
                }
            }
        }

        /// <summary>
        /// Обновить текст денег
        /// </summary>
        private void UpdateMoneyText()
        {
            if (moneyText != null)
            {
                string formattedMoney = FormatMoney(displayedMoney);
                moneyText.text = $"💰 {formattedMoney}";
                
                // Обновить цвет в зависимости от суммы
                moneyText.color = GetMoneyColor(displayedMoney);
            }
        }

        /// <summary>
        /// Форматировать деньги для отображения
        /// </summary>
        /// <param name="amount">Сумма денег</param>
        /// <returns>Форматированная строка</returns>
        private string FormatMoney(float amount)
        {
            if (useShortFormat)
            {
                return GameUtils.FormatMoney(amount);
            }
            else
            {
                return amount.ToString("F0");
            }
        }

        /// <summary>
        /// Получить цвет для отображения денег
        /// </summary>
        /// <param name="amount">Сумма денег</param>
        /// <returns>Цвет текста</returns>
        private Color GetMoneyColor(float amount)
        {
            if (amount >= 1000000f)      // Миллион+
                return Color.yellow;
            else if (amount >= 100000f)  // 100K+
                return Color.cyan;
            else if (amount >= 10000f)   // 10K+
                return Color.green;
            else if (amount >= 1000f)    // 1K+
                return Color.white;
            else if (amount >= 100f)     // 100+
                return new Color(0.8f, 0.8f, 0.8f);
            else                         // Меньше 100
                return new Color(0.6f, 0.6f, 0.6f);
        }

        /// <summary>
        /// Показать текст изменения денег
        /// </summary>
        /// <param name="text">Текст для отображения</param>
        /// <param name="color">Цвет текста</param>
        private void ShowMoneyChangeText(string text, Color color)
        {
            if (!showChangeAnimation || moneyChangeText == null)
                return;

            StartCoroutine(AnimateMoneyChangeText(text, color));
        }

        /// <summary>
        /// Анимация текста изменения денег
        /// </summary>
        private IEnumerator AnimateMoneyChangeText(string text, Color color)
        {
            moneyChangeText.text = text;
            moneyChangeText.color = color;
            moneyChangeText.gameObject.SetActive(true);

            Vector3 originalScale = moneyChangeText.transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;

            // Анимация появления
            float elapsed = 0f;
            float fadeInTime = 0.2f;
            
            while (elapsed < fadeInTime)
            {
                float t = elapsed / fadeInTime;
                moneyChangeText.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Удержание
            yield return new WaitForSeconds(changeTextDuration * 0.6f);

            // Анимация исчезновения
            elapsed = 0f;
            float fadeOutTime = changeTextDuration * 0.4f;
            Color originalColor = color;
            
            while (elapsed < fadeOutTime)
            {
                float t = elapsed / fadeOutTime;
                moneyChangeText.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                
                Color fadeColor = originalColor;
                fadeColor.a = Mathf.Lerp(1f, 0f, t);
                moneyChangeText.color = fadeColor;
                
                elapsed += Time.deltaTime;
                yield return null;
            }

            moneyChangeText.gameObject.SetActive(false);
            moneyChangeText.transform.localScale = originalScale;
        }

        #endregion

        #region Эффекты

        /// <summary>
        /// Воспроизвести эффект монеты
        /// </summary>
        /// <param name="worldPosition">Позиция в мире</param>
        /// <param name="amount">Сумма денег</param>
        private void PlayCoinEffect(Vector3 worldPosition, float amount)
        {
            if (coinEffectPrefab == null || mainCamera == null)
                return;

            // Ограничить количество эффектов
            if (activeCoinEffects.Count >= maxCoinEffects)
            {
                var oldestEffect = activeCoinEffects[0];
                if (oldestEffect != null)
                {
                    ReturnCoinEffectToPool(oldestEffect);
                }
                activeCoinEffects.RemoveAt(0);
            }

            // Конвертировать в экранные координаты
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            
            // Создать эффект
            GameObject coinEffect = GetCoinEffectFromPool();
            if (coinEffect != null)
            {
                StartCoroutine(AnimateCoinEffect(coinEffect, screenPosition, amount));
            }
        }

        /// <summary>
        /// Получить эффект монеты из пула
        /// </summary>
        private GameObject GetCoinEffectFromPool()
        {
            GameObject coinEffect;
            
            if (coinEffectPool.Count > 0)
            {
                coinEffect = coinEffectPool.Dequeue();
            }
            else
            {
                coinEffect = Instantiate(coinEffectPrefab, coinEffectParent);
            }
            
            coinEffect.SetActive(true);
            activeCoinEffects.Add(coinEffect);
            
            return coinEffect;
        }

        /// <summary>
        /// Вернуть эффект монеты в пул
        /// </summary>
        private void ReturnCoinEffectToPool(GameObject coinEffect)
        {
            if (coinEffect != null)
            {
                coinEffect.SetActive(false);
                coinEffectPool.Enqueue(coinEffect);
                activeCoinEffects.Remove(coinEffect);
            }
        }

        /// <summary>
        /// Анимация эффекта монеты
        /// </summary>
        private IEnumerator AnimateCoinEffect(GameObject coinEffect, Vector3 startScreenPosition, float amount)
        {
            RectTransform coinRect = coinEffect.GetComponent<RectTransform>();
            if (coinRect == null)
            {
                ReturnCoinEffectToPool(coinEffect);
                yield break;
            }

            // Начальная и конечная позиции
            Vector3 startPos = startScreenPosition + Random.insideUnitSphere * coinRandomSpread * 50f;
            Vector3 endPos = moneyText != null ? moneyText.transform.position : startPos + Vector3.up * 100f;
            
            // Добавить случайную дугу
            Vector3 midPos = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * Random.Range(50f, 150f);
            
            coinRect.position = startPos;

            float elapsed = 0f;
            Vector3 originalScale = coinRect.localScale;

            while (elapsed < coinAnimationDuration)
            {
                float t = elapsed / coinAnimationDuration;
                float curveT = coinMoveCurve.Evaluate(t);

                // Движение по кривой Безье
                Vector3 currentPos = CalculateBezierPoint(startPos, midPos, endPos, curveT);
                coinRect.position = currentPos;

                // Масштабирование
                float scale = Mathf.Lerp(1f, 0.3f, t);
                coinRect.localScale = originalScale * scale;

                // Вращение
                coinRect.Rotate(0, 0, 360f * Time.deltaTime * 2f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Эффект исчезновения в UI денег
            if (moneyUIAnimator != null)
            {
                moneyUIAnimator.SetTrigger("MoneyReceived");
            }

            ReturnCoinEffectToPool(coinEffect);
        }

        /// <summary>
        /// Рассчитать точку на кривой Безье
        /// </summary>
        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        #endregion

        #region Звуковые эффекты

        /// <summary>
        /// Воспроизвести звук получения монеты
        /// </summary>
        private void PlayCoinSound()
        {
            if (audioSource != null && coinSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f); // Небольшая вариация высоты тона
                audioSource.PlayOneShot(coinSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук траты денег
        /// </summary>
        private void PlaySpendSound()
        {
            if (audioSource != null && spendSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(spendSound);
            }
        }

        /// <summary>
        /// Воспроизвести звук бонуса за уровень
        /// </summary>
        private void PlayLevelUpBonusSound()
        {
            if (audioSource != null && levelUpBonusSound != null)
            {
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(levelUpBonusSound);
            }
        }

        #endregion

        #region Обработчики событий

        /// <summary>
        /// Обработчик изменения характеристик
        /// </summary>
        private void OnStatChanged(StatChangedEventData data)
        {
            if (data.statType == StatType.Money)
            {
                double oldMoney = currentMoney;
                currentMoney = data.newValue;
                double change = currentMoney - oldMoney;
                OnMoneyChanged?.Invoke(currentMoney, change, "StatChange");
            }
        }

        /// <summary>
        /// Обработчик повышения уровня (новая версия)
        /// </summary>
        private void OnLevelUp(LevelUpData data)
        {
            // Бонус за повышение уровня
            double levelBonus = data.moneyBonus > 0 ? data.moneyBonus : data.newLevel * 10.0;
            AddMoney(levelBonus, "Level Up Bonus", playerStats.transform.position);
            PlayLevelUpBonusSound();
            
            Debug.Log($"Бонус за достижение {data.newLevel} уровня: {levelBonus} денег");
        }

        /// <summary>
        /// Обработчик события заработка денег (новая версия)
        /// </summary>
        private void OnMoneyEarnedEvent(MoneyEventData data)
        {
            // Дополнительные эффекты при заработке
            if (moneyUIAnimator != null)
            {
                moneyUIAnimator.SetTrigger("MoneyEarned");
            }

            // Показать источник дохода в UI (если есть)
            if (data.amount >= 100) // Показывать только для крупных сумм
            {
                ShowMoneyChangeText($"+{FormatMoney((float)data.amount)} ({data.source})", Color.green);
            }
        }

        #endregion

        #region Публичные методы

        /// <summary>
        /// Установить UI элементы
        /// </summary>
        public void SetUIElements(Text moneyDisplay, Text changeDisplay, Animator uiAnimator)
        {
            moneyText = moneyDisplay;
            moneyChangeText = changeDisplay;
            moneyUIAnimator = uiAnimator;
            
            UpdateMoneyText();
        }

        /// <summary>
        /// Включить/выключить короткий формат отображения
        /// </summary>
        public void SetShortFormat(bool useShort)
        {
            useShortFormat = useShort;
            UpdateMoneyText();
        }

        /// <summary>
        /// Включить/выключить анимацию изменения денег
        /// </summary>
        public void SetChangeAnimation(bool enabled)
        {
            showChangeAnimation = enabled;
        }

        /// <summary>
        /// Установить максимальное количество эффектов монет
        /// </summary>
        public void SetMaxCoinEffects(int max)
        {
            maxCoinEffects = Mathf.Max(1, max);
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Добавить тестовые деньги
        /// </summary>
        [ContextMenu("Add Test Money")]
        private void AddTestMoney()
        {
            if (Application.isPlaying)
            {
                Vector3 testPosition = playerStats != null ? playerStats.transform.position : Vector3.zero;
                AddMoney(100f, testPosition);
            }
        }

        /// <summary>
        /// Потратить тестовые деньги
        /// </summary>
        [ContextMenu("Spend Test Money")]
        private void SpendTestMoney()
        {
            if (Application.isPlaying)
            {
                SpendMoney(50f);
            }
        }

        #endregion
    }
}