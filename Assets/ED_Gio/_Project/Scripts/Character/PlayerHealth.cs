using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NABHI.Character
{
    /// <summary>
    /// Sistema de salud del jugador con knockback e invencibilidad temporal (i-frames)
    /// Estilo Metroidvania: empuje hacia atrás, parpadeo visual, control temporal deshabilitado
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        #region CONFIGURACIÓN

        [Header("Salud")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Knockback")]
        [Tooltip("Fuerza del empuje al recibir daño")]
        [SerializeField] private Vector2 knockbackForce = new Vector2(10f, 8f);

        [Tooltip("Duración del knockback (pérdida de control)")]
        [SerializeField] private float knockbackDuration = 0.3f;

        [Header("Invencibilidad")]
        [Tooltip("Duración de la invencibilidad después de recibir daño (i-frames)")]
        [SerializeField] private float invincibilityDuration = 1.5f;

        [Tooltip("Velocidad de parpadeo durante invencibilidad")]
        [SerializeField] private float flickerSpeed = 0.1f;

        [Header("Referencias")]
        [SerializeField] private CharacterController2D controller;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private HybridAnimationController hybridAnimationController;
        [SerializeField] private CharacterAnimator characterAnimator; // Fallback si no usa sistema híbrido
        [SerializeField] private Slider healthSlider;

        [Header("Feedback Visual")]
        [Tooltip("GameObject que contiene los sprites (para hacer flicker)")]
        [SerializeField] private GameObject visualRoot;

        [SerializeField] private SpriteRenderer[] spriteRenderers; // Múltiples sprites (frame-by-frame + rigging)

        [Header("Eventos")]
        public UnityEvent<int> OnHealthChanged;
        public UnityEvent OnDeath;
        public UnityEvent OnHit;

        #endregion

        #region ESTADO

        private bool isInvincible = false;
        private bool isInKnockback = false;
        private float knockbackTimer = 0f;
        private float invincibilityTimer = 0f;
        private bool isDead = false;

        #endregion

        #region PROPIEDADES PÚBLICAS

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => isInvincible;
        public bool IsInKnockback => isInKnockback;
        public bool IsDead => isDead;

        #endregion

        #region UNITY CALLBACKS

        void Start()
        {
            // Auto-find components si no están asignados
            if (controller == null)
                controller = GetComponent<CharacterController2D>();

            if (rb == null)
                rb = GetComponent<Rigidbody2D>();

            // Priorizar HybridAnimationController (sistema moderno)
            if (hybridAnimationController == null)
                hybridAnimationController = GetComponent<HybridAnimationController>();

            // Fallback a CharacterAnimator si no hay sistema híbrido
            if (hybridAnimationController == null && characterAnimator == null)
                characterAnimator = GetComponent<CharacterAnimator>();

            // Auto-find sprites para flicker
            if (spriteRenderers == null || spriteRenderers.Length == 0)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            }

            // Inicializar salud
            currentHealth = maxHealth;

            // Inicializar Slider
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        void Update()
        {
            // Actualizar timers
            if (isInKnockback)
            {
                knockbackTimer -= Time.deltaTime;
                if (knockbackTimer <= 0)
                {
                    EndKnockback();
                }
            }

            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;

                // Parpadeo visual durante invencibilidad
                FlickerVisual();

                if (invincibilityTimer <= 0)
                {
                    EndInvincibility();
                }
            }
        }

        #endregion

        #region IDAMAGEABLE IMPLEMENTATION

        public void TakeDamage(float damage)
        {
            // No recibir daño si está invencible o muerto
            if (isInvincible || isDead)
                return;

            // Aplicar daño
            int damageInt = Mathf.RoundToInt(damage);
            currentHealth -= damageInt;
            currentHealth = Mathf.Max(currentHealth, 0);

            Debug.Log($"[PlayerHealth] Daño recibido: {damageInt}. Salud restante: {currentHealth}/{maxHealth}");

            // Eventos
            OnHealthChanged?.Invoke(currentHealth);
            OnHit?.Invoke();

            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            // Cancelar dash si está activo (el knockback reemplaza al dash)
            if (controller != null && controller.IsDashing)
            {
                controller.CancelDash();
            }

            // Activar animación de hit (priorizar sistema híbrido)
            if (hybridAnimationController != null)
            {
                hybridAnimationController.PlayHitAnimation();
            }
            else if (characterAnimator != null)
            {
                characterAnimator.PlayHitAnimation();
            }

            // Verificar muerte
            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            // Activar knockback e invencibilidad
            StartKnockback();
            StartInvincibility();
        }

        public bool IsAlive()
        {
            return !isDead;
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            int healInt = Mathf.RoundToInt(amount);
            currentHealth += healInt;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            Debug.Log($"[PlayerHealth] Curado: {healInt}. Salud actual: {currentHealth}/{maxHealth}");

            OnHealthChanged?.Invoke(currentHealth);

            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }
        }

        #endregion

        #region KNOCKBACK

        private void StartKnockback()
        {
            isInKnockback = true;
            knockbackTimer = knockbackDuration;

            // Determinar dirección del knockback (opuesta a la que mira el personaje)
            // Asumimos que el daño viene de adelante
            int knockbackDirection = -controller.FacingDirection;

            // Aplicar knockback SOLO horizontal (mantiene movimiento vertical)
            // Esto evita que el personaje "salte" al recibir daño
            float currentVelocityY = rb.velocity.y; // Guardar velocidad vertical actual

            // Establecer velocidad directamente (más confiable que AddForce)
            // Knockback horizontal + velocidad vertical actual
            Vector2 knockbackVelocity = new Vector2(knockbackDirection * knockbackForce.x, currentVelocityY);
            rb.velocity = knockbackVelocity;

            Debug.Log($"[PlayerHealth] Knockback aplicado: dirección = {knockbackDirection}, velocidad = {knockbackVelocity}");
        }

        private void EndKnockback()
        {
            isInKnockback = false;
            knockbackTimer = 0f;

            Debug.Log("[PlayerHealth] Knockback finalizado");
        }

        /// <summary>
        /// Aplica knockback en una dirección específica (útil si sabes de dónde vino el daño)
        /// </summary>
        public void ApplyKnockback(Vector2 direction)
        {
            if (isDead || isInKnockback) return;

            isInKnockback = true;
            knockbackTimer = knockbackDuration;

            Vector2 knockback = direction.normalized * knockbackForce.magnitude;
            rb.velocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }

        #endregion

        #region INVENCIBILIDAD

        private void StartInvincibility()
        {
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;

            Debug.Log($"[PlayerHealth] Invencibilidad activada por {invincibilityDuration}s");
        }

        private void EndInvincibility()
        {
            isInvincible = false;
            invincibilityTimer = 0f;

            // Asegurar que todos los sprites sean visibles
            SetSpritesVisible(true);

            Debug.Log("[PlayerHealth] Invencibilidad finalizada");
        }

        private void FlickerVisual()
        {
            // Parpadeo tipo Metroidvania (alternar visibilidad)
            float flickerValue = Mathf.Sin(invincibilityTimer / flickerSpeed);
            bool visible = flickerValue > 0;

            SetSpritesVisible(visible);
        }

        private void SetSpritesVisible(bool visible)
        {
            // Usar alpha en lugar de enabled (compatible con sistema híbrido)
            // Cambiar el alpha funciona incluso cuando el GameObject está desactivado
            if (spriteRenderers == null) return;

            float alpha = visible ? 1f : 0f;

            foreach (SpriteRenderer sr in spriteRenderers)
            {
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a = alpha;
                    sr.color = color;
                }
            }
        }

        #endregion

        #region MUERTE

        private void Die()
        {
            if (isDead) return;

            isDead = true;
            Debug.Log("[PlayerHealth] Player ha muerto");

            // Evento de muerte
            OnDeath?.Invoke();

            // Desactivar control del personaje
            if (controller != null)
                controller.enabled = false;

            // Aquí puedes agregar:
            // - Animación de muerte
            // - Pantalla de Game Over
            // - Respawn en checkpoint
            // - Etc.
        }

        /// <summary>
        /// Revivir al jugador (útil para checkpoints/respawn)
        /// </summary>
        public void Revive()
        {
            isDead = false;
            currentHealth = maxHealth;
            isInvincible = false;
            isInKnockback = false;

            SetSpritesVisible(true);

            if (controller != null)
                controller.enabled = true;

            OnHealthChanged?.Invoke(currentHealth);

            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            Debug.Log("[PlayerHealth] Player revivido");
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Verificar si el personaje puede recibir input (no está en knockback)
        /// </summary>
        public bool CanReceiveInput()
        {
            return !isInKnockback && !isDead;
        }

        /// <summary>
        /// Forzar invencibilidad (útil para power-ups o eventos especiales)
        /// </summary>
        public void ForceInvincibility(float duration)
        {
            isInvincible = true;
            invincibilityTimer = duration;
        }

        #endregion

        #region DEBUG

        void OnGUI()
        {
            if (!Debug.isDebugBuild) return;

            GUILayout.BeginArea(new Rect(10, 150, 300, 150));
            GUILayout.Box("=== PLAYER HEALTH DEBUG ===");
            GUILayout.Label($"Health: {currentHealth}/{maxHealth}");
            GUILayout.Label($"Invincible: {isInvincible} ({invincibilityTimer:F2}s)");
            GUILayout.Label($"Knockback: {isInKnockback} ({knockbackTimer:F2}s)");
            GUILayout.Label($"Dead: {isDead}");
            GUILayout.EndArea();
        }

        #endregion
    }
}
