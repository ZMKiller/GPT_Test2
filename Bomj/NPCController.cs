using System.Collections;
using UnityEngine;
using System;

namespace HomelessToMillionaire
{
    /// <summary>
    /// Контроллер поведения отдельного прохожего
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour
    {
        [Header("Ссылки на компоненты")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D npcCollider;
        [SerializeField] private Animator animator;
        
        [Header("Настройки движения")]
        [SerializeField] private Vector2 moveDirection = Vector2.right;
        [SerializeField] private float currentSpeed = 3f;
        [SerializeField] private bool flipSpriteBasedOnDirection = true;
        
        [Header("Настройки взаимодействия")]
        [SerializeField] private LayerMask playerLayerMask = 1;
        [SerializeField] private bool hasGivenMoney = false;
        
        // Данные NPC
        private NPCData npcData;
        private Transform playerTransform;
        private MoneySystem moneySystem;
        private PlayerStats playerStats;
        
        // Состояние
        private bool isInteractingWithPlayer = false;
        private float lastMoneyGiveTime = 0f;
        private float spawnTime;
        private bool isInitialized = false;
        
        // События
        public Action<NPCController> OnNPCDespawn;
        public Action<NPCController, float> OnMoneyGiven;

        #region Unity Callbacks

        private void Awake()
        {
            // Получить компоненты если они не назначены
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (npcCollider == null)
                npcCollider = GetComponent<Collider2D>();
            
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Start()
        {
            spawnTime = Time.time;
            
            // Найти необходимые компоненты
            if (GameObject.FindGameObjectWithTag("Player") != null)
            {
                playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            }
            
            moneySystem = FindObjectOfType<MoneySystem>();
            playerStats = FindObjectOfType<PlayerStats>();
            
            if (playerStats == null)
            {
                playerStats = GameManager.Instance?.PlayerStats;
            }
        }

        private void Update()
        {
            if (!isInitialized || npcData == null)
                return;

            // Движение
            MoveNPC();
            
            // Проверка взаимодействия с игроком
            CheckPlayerInteraction();
            
            // Проверка времени жизни
            CheckLifetime();
            
            // Обновление анимации
            UpdateAnimation();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isInteractingWithPlayer = true;
                OnPlayerEnterRange();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isInteractingWithPlayer = false;
                OnPlayerExitRange();
            }
        }

        #endregion

        #region Инициализация

        /// <summary>
        /// Инициализировать NPC с заданными данными
        /// </summary>
        /// <param name="data">Данные NPC</param>
        /// <param name="direction">Направление движения</param>
        public void Initialize(NPCData data, Vector2 direction)
        {
            npcData = data;
            moveDirection = direction.normalized;
            currentSpeed = data.GetRandomSpeed();
            
            // Установить спрайт
            if (spriteRenderer != null && data.Sprite != null)
            {
                spriteRenderer.sprite = data.Sprite;
            }
            
            // Настроить коллайдер для обнаружения игрока
            if (npcCollider != null && npcCollider is CircleCollider2D circleCollider)
            {
                circleCollider.radius = data.DetectionRadius;
                circleCollider.isTrigger = true;
            }
            
            // Повернуть спрайт в зависимости от направления
            UpdateSpriteDirection();
            
            isInitialized = true;
            hasGivenMoney = false;
            lastMoneyGiveTime = 0f;
            
            Debug.Log($"NPC {data.Name} инициализирован с направлением {direction} и скоростью {currentSpeed}");
        }

        #endregion

        #region Движение

        /// <summary>
        /// Двигать NPC
        /// </summary>
        private void MoveNPC()
        {
            // Базовое движение
            Vector3 movement = new Vector3(moveDirection.x, moveDirection.y, 0) * currentSpeed * Time.deltaTime;
            transform.position += movement;
        }

        /// <summary>
        /// Обновить направление спрайта
        /// </summary>
        private void UpdateSpriteDirection()
        {
            if (!flipSpriteBasedOnDirection || spriteRenderer == null)
                return;

            // Отразить спрайт горизонтально в зависимости от направления движения
            if (moveDirection.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else if (moveDirection.x > 0)
            {
                spriteRenderer.flipX = false;
            }
        }

        #endregion

        #region Взаимодействие с игроком

        /// <summary>
        /// Проверить взаимодействие с игроком
        /// </summary>
        private void CheckPlayerInteraction()
        {
            if (!isInteractingWithPlayer || playerTransform == null)
                return;

            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            if (distanceToPlayer <= npcData.DetectionRadius)
            {
                TryGiveMoney();
            }
        }

        /// <summary>
        /// Попытаться дать деньги игроку
        /// </summary>
        private void TryGiveMoney()
        {
            // Проверить можно ли дать деньги
            if (!CanGiveMoney())
                return;

            // Рассчитать сумму денег
            float moneyAmount = CalculateMoneyAmount();
            
            if (moneyAmount > 0)
            {
                GiveMoney(moneyAmount);
            }
        }

        /// <summary>
        /// Проверить можно ли дать деньги
        /// </summary>
        /// <returns>True если можно дать деньги</returns>
        private bool CanGiveMoney()
        {
            // Если NPC дает деньги только один раз и уже дал
            if (!npcData.GivesMoneyContinuously && hasGivenMoney)
                return false;

            // Проверить кулдаун
            if (Time.time - lastMoneyGiveTime < npcData.MoneyGivingCooldown)
                return false;

            return true;
        }

        /// <summary>
        /// Рассчитать сумму денег с учетом модификаторов игрока
        /// </summary>
        /// <returns>Сумма денег</returns>
        private float CalculateMoneyAmount()
        {
            float playerMoodModifier = 1f;
            float playerLevelModifier = 1f;

            if (playerStats != null)
            {
                // Модификатор настроения (от 0.5 до 1.5)
                playerMoodModifier = Mathf.Lerp(0.5f, 1.5f, playerStats.MoodPercentage);
                
                // Модификатор уровня (каждый уровень дает +5% к заработку)
                playerLevelModifier = 1f + (playerStats.Level - 1) * 0.05f;
            }

            return npcData.GetRandomMoneyAmount(playerMoodModifier, playerLevelModifier);
        }

        /// <summary>
        /// Дать деньги игроку
        /// </summary>
        /// <param name="amount">Сумма денег</param>
        private void GiveMoney(float amount)
        {
            // Добавить деньги через систему
            if (moneySystem != null)
            {
                moneySystem.AddMoney(amount, transform.position);
            }
            else if (playerStats != null)
            {
                playerStats.ChangeMoney(amount);
            }

            // Обновить состояние
            hasGivenMoney = true;
            lastMoneyGiveTime = Time.time;

            // Вызвать события
            OnMoneyGiven?.Invoke(this, amount);
            
            // Анимация выдачи денег
            StartCoroutine(PlayMoneyGiveAnimation());
            
            Debug.Log($"NPC {npcData.Name} дал {amount:F1} денег игроку");
        }

        /// <summary>
        /// Корутина анимации выдачи денег
        /// </summary>
        private IEnumerator PlayMoneyGiveAnimation()
        {
            // Простая анимация масштабирования
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;
            
            float animationTime = 0.2f;
            float elapsed = 0f;
            
            // Увеличение
            while (elapsed < animationTime)
            {
                float t = elapsed / animationTime;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            elapsed = 0f;
            
            // Уменьшение
            while (elapsed < animationTime)
            {
                float t = elapsed / animationTime;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            transform.localScale = originalScale;
        }

        #endregion

        #region События игрока

        /// <summary>
        /// Игрок вошел в зону обнаружения
        /// </summary>
        private void OnPlayerEnterRange()
        {
            Debug.Log($"Игрок приблизился к NPC {npcData.Name}");
            
            // Можно добавить эффекты или анимации
            if (animator != null)
            {
                animator.SetBool("PlayerNear", true);
            }
        }

        /// <summary>
        /// Игрок вышел из зоны обнаружения
        /// </summary>
        private void OnPlayerExitRange()
        {
            Debug.Log($"Игрок отошел от NPC {npcData.Name}");
            
            if (animator != null)
            {
                animator.SetBool("PlayerNear", false);
            }
        }

        #endregion

        #region Управление жизненным циклом

        /// <summary>
        /// Проверить время жизни NPC
        /// </summary>
        private void CheckLifetime()
        {
            float lifetime = Time.time - spawnTime;
            
            // Если NPC существует слишком долго без взаимодействия
            if (!isInteractingWithPlayer && lifetime > npcData.DespawnTime)
            {
                DespawnNPC();
            }
        }

        /// <summary>
        /// Удалить NPC
        /// </summary>
        public void DespawnNPC()
        {
            OnNPCDespawn?.Invoke(this);
            
            // Можно добавить эффект исчезновения
            StartCoroutine(DespawnAnimation());
        }

        /// <summary>
        /// Анимация исчезновения
        /// </summary>
        private IEnumerator DespawnAnimation()
        {
            float fadeTime = 0.5f;
            float elapsed = 0f;
            Color originalColor = spriteRenderer.color;
            
            while (elapsed < fadeTime)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Destroy(gameObject);
        }

        #endregion

        #region Анимация

        /// <summary>
        /// Обновить анимацию
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null)
                return;

            // Установить скорость движения для анимации
            animator.SetFloat("Speed", currentSpeed);
            animator.SetBool("Moving", currentSpeed > 0.1f);
            
            // Направление движения
            animator.SetFloat("MoveX", moveDirection.x);
            animator.SetFloat("MoveY", moveDirection.y);
        }

        #endregion

        #region Утилиты

        /// <summary>
        /// Проверить является ли коллайдер игроком
        /// </summary>
        /// <param name="other">Коллайдер для проверки</param>
        /// <returns>True если это игрок</returns>
        private bool IsPlayer(Collider2D other)
        {
            return other.CompareTag("Player") || ((1 << other.gameObject.layer) & playerLayerMask) != 0;
        }

        /// <summary>
        /// Получить данные NPC
        /// </summary>
        public NPCData GetNPCData()
        {
            return npcData;
        }

        /// <summary>
        /// Проверить дал ли NPC уже деньги
        /// </summary>
        public bool HasGivenMoney()
        {
            return hasGivenMoney;
        }

        /// <summary>
        /// Принудительно установить направление движения
        /// </summary>
        /// <param name="direction">Новое направление</param>
        public void SetMoveDirection(Vector2 direction)
        {
            moveDirection = direction.normalized;
            UpdateSpriteDirection();
        }

        /// <summary>
        /// Установить скорость движения
        /// </summary>
        /// <param name="speed">Новая скорость</param>
        public void SetSpeed(float speed)
        {
            currentSpeed = Mathf.Max(0f, speed);
        }

        #endregion

        #region Отладка

        /// <summary>
        /// Отрисовка гизмо для отладки
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (npcData != null)
            {
                // Радиус обнаружения игрока
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCircle(transform.position, npcData.DetectionRadius);
                
                // Направление движения
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, new Vector3(moveDirection.x, moveDirection.y, 0) * 2f);
            }
        }

        #endregion
    }
}