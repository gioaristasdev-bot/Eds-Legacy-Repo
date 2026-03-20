using UnityEngine;
using NABHI.Character;
using NABHI.Chakras.Abilities;

namespace NABHI.Enemies
{
    /// <summary>
    /// Clase base abstracta para todos los enemigos de NABHI.
    /// Implementa IDamageable, IStunnable, IHackable.
    ///
    /// ESTADOS DISPONIBLES (mapeados del Documento Técnico de Enemigos):
    ///   Idle     → sin objetivo, estático o en espera
    ///   Patrol   → patrulla (terrestre o aérea)
    ///   Chase    → persecución activa del jugador
    ///   Attack   → acción de ataque (disparo, melee, embestida)
    ///   Cooldown → espera post-ataque antes de repetir ciclo
    ///   Hit      → ventana de animación al recibir golpe (pausa breve de IA)
    ///   Recover  → recuperación post-ataque (exclusivo Cyborg)
    ///   Retreat  → retroceso táctico (exclusivo Dron de Ataque)
    ///   Stunned  → aturdido por ChakraTremor
    ///   Hacked   → desactivado por ChakraRemoteHack
    ///   Dead     → muerto
    /// </summary>
    public abstract class EnemyBase : MonoBehaviour, IDamageable, IStunnable, IHackable
    {
        #region ENUMS

        public enum EnemyState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Cooldown,
            Hit,
            Recover,
            Retreat,
            Stunned,
            Hacked,
            Dead
        }

        #endregion

        // ═══════════════════════════════════════════════════════════════════
        // SISTEMA DE ANIMACIÓN
        // ═══════════════════════════════════════════════════════════════════
        //
        // Cuando el artista entregue las animaciones, seguir estos pasos:
        //
        //  1. Asignar el Animator Controller en el campo "Animator" del Inspector.
        //
        //  2. Crear en el Animator Controller los parámetros exactos:
        //       "State"    → Integer  (valor = (int)EnemyState)
        //       "Hit"      → Trigger  (animación breve de recibir golpe)
        //       "IsDead"   → Bool     (activa y mantiene animación de muerte)
        //       "IsMoving" → Bool     (activo en Patrol/Chase/Retreat)
        //
        //  3. Mapeo de EnemyState → valor Integer "State":
        //       Idle     = 0   Patrol   = 1   Chase    = 2
        //       Attack   = 3   Cooldown = 4   Hit      = 5
        //       Recover  = 6   Retreat  = 7   Stunned  = 8
        //       Hacked   = 9   Dead     = 10
        //
        //  4. En cada subclase, dentro de OnAnimStateChanged(), descomentar el
        //     bloque del Animator correspondiente.
        //
        // ═══════════════════════════════════════════════════════════════════

        #region ANIMATION PARAMS

        /// <summary>
        /// Nombres de parámetros del Animator. Usar como:
        ///   animator.SetInteger(AnimParam.State, (int)EnemyState.Chase);
        ///   animator.SetTrigger(AnimParam.Hit);
        /// </summary>
        public static class AnimParam
        {
            public const string State    = "State";    // Integer - estado actual
            public const string Hit      = "Hit";      // Trigger - golpe recibido
            public const string IsDead   = "IsDead";  // Bool    - muerte persistente
            public const string IsMoving = "IsMoving"; // Bool    - en movimiento
        }

        #endregion

        #region CONFIGURACIÓN

        [Header("Salud")]
        [SerializeField] protected float maxHealth = 50f;
        [SerializeField] protected float currentHealth;

        [Header("Detección")]
        [SerializeField] protected float detectionRadius = 8f;
        [SerializeField] protected float fieldOfView = 120f;
        [SerializeField] protected bool use360Detection = false;
        [SerializeField] protected LayerMask playerLayer;
        [SerializeField] protected LayerMask obstacleLayer;
        [SerializeField] protected bool requireLineOfSight = true;
        [Tooltip("Offset vertical para el raycast de línea de visión (altura de los ojos)")]
        [SerializeField] protected float eyeHeight = 0.4f;

        [Header("Invisibilidad")]
        [SerializeField] protected string invisibleLayerName = "InvisiblePlayer";
        protected int invisibleLayerIndex;

        [Header("Patrulla")]
        [SerializeField] protected float patrolSpeed = 2f;
        [SerializeField] protected float patrolDistance = 4f;

        [Header("Persecución")]
        [SerializeField] protected float chaseSpeed = 4f;

        [Header("Knockback")]
        [Tooltip("Resistencia al knockback. 0 = recibe completo | 1 = inmune")]
        [SerializeField, Range(0f, 1f)] protected float knockbackResistance = 0f;

        [Header("Hurtbox (Sistema Unificado de Daño)")]
        [Tooltip("Collider hijo separado para recibir daño de proyectiles del jugador.\n" +
                 "Agregar child 'Hurtbox' con CircleCollider2D trigger + EnemyHurtbox script.\n" +
                 "Si es null, el daño se recibe en el collider principal.")]
        [SerializeField] protected Collider2D hurtboxCollider;

        [Header("Hackeable (ChakraRemoteHack)")]
        [SerializeField] protected bool canBeHacked = true;
        [SerializeField] protected float hackDisableDuration = 3f;

        [Header("Animación")]
        [Tooltip("Animator del sprite. Asignar cuando el artista entregue las animaciones.")]
        [SerializeField] protected Animator animator;
        [Tooltip("Duración del estado Hit (ventana de animación) antes de retomar el estado previo")]
        [SerializeField] protected float hitStateDuration = 0.25f;

        [Header("Visual Feedback (color, temporal hasta tener sprites)")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Color idleColor     = Color.white;
        [SerializeField] protected Color chaseColor    = Color.yellow;
        [SerializeField] protected Color attackColor   = Color.red;
        [SerializeField] protected Color cooldownColor = new Color(1f, 0.5f, 0f);  // naranja
        [SerializeField] protected Color stunColor     = new Color(0.5f, 0.5f, 1f); // azul
        [SerializeField] protected Color hackedColor   = new Color(0.2f, 0.8f, 0.2f); // verde
        [SerializeField] protected Color hurtColor     = Color.white; // flash al recibir golpe

        [Header("Debug")]
        [SerializeField] protected bool showDebugGizmos = true;

        #endregion

        #region ESTADO

        protected EnemyState currentState = EnemyState.Idle;
        protected Transform playerTarget;
        protected Vector2 lastKnownPlayerPos;
        protected int facingDirection = 1;
        protected Vector2 patrolOrigin;
        protected bool movingRight = true;

        // Stun
        protected bool isStunned;
        protected float stunTimer;

        // Hacked
        protected bool isHacked;
        protected float hackedTimer;
        protected EnemyState stateBeforeHack;

        // Hit state
        protected bool isInHitState;
        protected float hitStateTimer;
        protected EnemyState stateBeforeHit;

        // Flash visual
        protected float flashTimer;
        protected float flashDuration = 0.15f;
        protected bool isFlashing;

        // Muerte
        protected bool isDead;

        #endregion

        #region PROPIEDADES

        public EnemyState CurrentState      => currentState;
        public bool       IsStunned         => isStunned;
        public bool       IsHacked          => isHacked;
        public bool       IsDead            => isDead;
        public int        FacingDirection   => facingDirection;
        public float      KnockbackResist   => knockbackResistance;

        // IHackable
        public virtual bool CanBeHacked => canBeHacked && !isDead && !isHacked && !isStunned;

        #endregion

        #region UNITY CALLBACKS

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            invisibleLayerIndex = LayerMask.NameToLayer(invisibleLayerName);

            int playerLayerIndex  = LayerMask.NameToLayer("Player");
            int enemiesLayerIndex = LayerMask.NameToLayer("Enemies");
            if (playerLayerIndex != -1 && enemiesLayerIndex != -1)
                Physics2D.IgnoreLayerCollision(playerLayerIndex, enemiesLayerIndex, true);
        }

        protected virtual void Start()
        {
            currentHealth = maxHealth;
            patrolOrigin  = transform.position;
            ChangeState(EnemyState.Patrol);
        }

        protected virtual void Update()
        {
            if (isDead) return;

            // 1. Hit state: pausa breve de IA + ventana para animación de recibir golpe
            if (isInHitState)
            {
                hitStateTimer -= Time.deltaTime;
                if (hitStateTimer <= 0f)
                    ExitHitState();
                return;
            }

            // 2. Hacked: desactivado por ChakraRemoteHack
            if (isHacked)
            {
                hackedTimer -= Time.deltaTime;
                if (hackedTimer <= 0f)
                    EndHackEffect();
                return;
            }

            // 3. Stunned: aturdido por ChakraTremor
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                    EndStun();
                return;
            }

            // Flash visual (no bloquea la IA)
            if (isFlashing)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    isFlashing = false;
                    UpdateVisualColor();
                }
            }

            UpdateState();
        }

        #endregion

        #region MÁQUINA DE ESTADOS

        protected virtual void UpdateState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    ScanForPlayer();
                    if (playerTarget != null)
                        ChangeState(EnemyState.Chase);
                    break;

                case EnemyState.Patrol:
                    OnPatrol();
                    ScanForPlayer();
                    if (playerTarget != null)
                        ChangeState(EnemyState.Chase);
                    break;

                case EnemyState.Chase:
                    if (!IsPlayerStillDetected())
                    {
                        playerTarget = null;
                        ChangeState(EnemyState.Patrol);
                        break;
                    }
                    lastKnownPlayerPos = playerTarget.position;
                    OnChase();
                    break;

                case EnemyState.Attack:
                    OnAttack();
                    break;

                case EnemyState.Cooldown:
                    OnCooldown();
                    break;

                case EnemyState.Recover:
                    OnRecover();
                    break;

                case EnemyState.Retreat:
                    OnRetreat();
                    break;
            }
        }

        protected void ChangeState(EnemyState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            OnAnimStateChanged(newState);
            UpdateVisualColor();
        }

        #endregion

        #region DETECCIÓN

        protected virtual void ScanForPlayer()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, playerLayer);
            foreach (var hit in hits)
            {
                if (invisibleLayerIndex != -1 && hit.gameObject.layer == invisibleLayerIndex)
                    continue;

                if (!use360Detection && !IsInFieldOfView(hit.transform.position))
                    continue;

                if (requireLineOfSight && !HasLineOfSight(hit.transform.position))
                    continue;

                playerTarget = hit.transform;
                lastKnownPlayerPos = playerTarget.position;
                return;
            }
        }

        protected bool IsPlayerStillDetected()
        {
            if (playerTarget == null) return false;

            if (invisibleLayerIndex != -1 && playerTarget.gameObject.layer == invisibleLayerIndex)
                return false;

            if (Vector2.Distance(transform.position, playerTarget.position) > detectionRadius * 1.2f)
                return false;

            if (requireLineOfSight && !HasLineOfSight(playerTarget.position))
                return false;

            return true;
        }

        protected bool IsInFieldOfView(Vector2 targetPosition)
        {
            Vector2 eyePos = (Vector2)transform.position + Vector2.up * eyeHeight;
            Vector2 dir    = (targetPosition - eyePos).normalized;
            float   angle  = Vector2.Angle(new Vector2(facingDirection, 0), dir);
            return angle <= fieldOfView / 2f;
        }

        protected bool HasLineOfSight(Vector2 targetPosition)
        {
            Vector2 eyePos      = (Vector2)transform.position + Vector2.up * eyeHeight;
            Vector2 targetCenter = targetPosition + Vector2.up * eyeHeight;
            Vector2 direction   = targetCenter - eyePos;
            RaycastHit2D hit    = Physics2D.Raycast(eyePos, direction.normalized, direction.magnitude, obstacleLayer);
            return hit.collider == null;
        }

        #endregion

        #region IDAMAGEABLE

        public virtual void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            EnterHitState();

            if (currentHealth <= 0)
                Die();
        }

        public bool IsAlive() => !isDead;

        public void Heal(float amount)
        {
            if (isDead) return;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        #endregion

        #region HIT STATE

        private void EnterHitState()
        {
            // No interrumpir estados ya "incapacitados"
            if (isStunned || isHacked || isDead)
            {
                StartDamageFlash();
                return;
            }

            if (!isInHitState)
                stateBeforeHit = currentState;

            isInHitState  = true;
            hitStateTimer = hitStateDuration;

            if (currentState != EnemyState.Hit)
            {
                currentState = EnemyState.Hit;
                OnAnimStateChanged(EnemyState.Hit);
            }

            StartDamageFlash();
            OnHitReceived();
        }

        private void ExitHitState()
        {
            isInHitState = false;
            ChangeState(stateBeforeHit);
        }

        #endregion

        #region ISTUNNABLE

        public virtual void ApplyStun(float duration)
        {
            if (isDead) return;
            isStunned = true;
            stunTimer = duration;
            ChangeState(EnemyState.Stunned);
        }

        protected virtual void EndStun()
        {
            isStunned = false;
            stunTimer = 0f;
            ChangeState(EnemyState.Patrol);
        }

        #endregion

        #region IHACKABLE

        public virtual void OnHackStart()
        {
            // El chakra muestra su propia barra de progreso.
            // Override si el enemigo necesita feedback visual propio al ser hackeado.
        }

        public virtual void OnHackComplete()
        {
            if (!CanBeHacked) return;
            ApplyHackEffect();
        }

        public virtual void OnHackInterrupted() { }

        protected virtual void ApplyHackEffect()
        {
            if (isDead) return;
            stateBeforeHack = currentState;
            isHacked        = true;
            hackedTimer     = hackDisableDuration;
            ChangeState(EnemyState.Hacked);
        }

        protected virtual void EndHackEffect()
        {
            isHacked    = false;
            hackedTimer = 0f;
            EnemyState returnState = (stateBeforeHack == EnemyState.Dead)
                ? EnemyState.Patrol
                : stateBeforeHack;
            ChangeState(returnState);
        }

        #endregion

        #region MUERTE

        protected virtual void Die()
        {
            if (isDead) return;

            isDead        = true;
            currentHealth = 0;
            ChangeState(EnemyState.Dead);
            OnDeath();

            foreach (var col in GetComponents<Collider2D>())
                col.enabled = false;

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 0.3f;
                spriteRenderer.color = c;
            }

            Destroy(gameObject, 1f);
        }

        #endregion

        #region VISUAL FEEDBACK

        protected virtual void UpdateVisualColor()
        {
            if (spriteRenderer == null || isFlashing) return;

            switch (currentState)
            {
                case EnemyState.Idle:
                case EnemyState.Patrol:
                case EnemyState.Hit:
                case EnemyState.Recover:
                case EnemyState.Retreat:
                    spriteRenderer.color = idleColor;    break;
                case EnemyState.Chase:
                    spriteRenderer.color = chaseColor;   break;
                case EnemyState.Attack:
                    spriteRenderer.color = attackColor;  break;
                case EnemyState.Cooldown:
                    spriteRenderer.color = cooldownColor; break;
                case EnemyState.Stunned:
                    spriteRenderer.color = stunColor;    break;
                case EnemyState.Hacked:
                    spriteRenderer.color = hackedColor;  break;
            }
        }

        protected void StartDamageFlash()
        {
            isFlashing = true;
            flashTimer = flashDuration;
            if (spriteRenderer != null)
                spriteRenderer.color = hurtColor;
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        /// <summary>
        /// Llamado en cada cambio de estado. Override en subclases para conectar el Animator.
        ///
        /// PLANTILLA (descomentar en cada subclase cuando el artista entregue animaciones):
        ///
        ///   protected override void OnAnimStateChanged(EnemyState newState)
        ///   {
        ///       if (animator == null) return;
        ///       animator.SetInteger(AnimParam.State, (int)newState);
        ///       bool moving = newState == EnemyState.Patrol
        ///                  || newState == EnemyState.Chase
        ///                  || newState == EnemyState.Retreat;
        ///       animator.SetBool(AnimParam.IsMoving, moving);
        ///       if (newState == EnemyState.Hit)  animator.SetTrigger(AnimParam.Hit);
        ///       if (newState == EnemyState.Dead) animator.SetBool(AnimParam.IsDead, true);
        ///   }
        /// </summary>
        protected virtual void OnAnimStateChanged(EnemyState newState)
        {
            // Base vacío. Override en subclases.
        }

        /// <summary>
        /// Llamado al recibir un golpe, antes de posible Die().
        /// Override para trigger de hit, sonido de impacto, partículas.
        ///
        /// Ejemplo:
        ///   protected override void OnHitReceived()
        ///   {
        ///       animator?.SetTrigger(AnimParam.Hit);
        ///       audioSource?.PlayOneShot(hitSound);
        ///   }
        /// </summary>
        protected virtual void OnHitReceived() { }

        /// <summary>
        /// Llamado al morir, antes de Destroy. Override para animación de muerte,
        /// efectos de partículas, drops de objetos.
        ///
        /// Ejemplo:
        ///   protected override void OnDeath()
        ///   {
        ///       animator?.SetBool(AnimParam.IsDead, true);
        ///       Instantiate(deathVFX, transform.position, Quaternion.identity);
        ///   }
        /// </summary>
        protected virtual void OnDeath() { }

        #endregion

        #region HOOKS DE COMPORTAMIENTO (implementar en subclases)

        protected abstract void OnPatrol();
        protected abstract void OnChase();
        protected abstract void OnAttack();

        /// <summary>Espera post-disparo. Override para timer de cooldown.</summary>
        protected virtual void OnCooldown() { }

        /// <summary>Recuperación post-ataque (Cyborg). Override para timer de recover.</summary>
        protected virtual void OnRecover() { }

        /// <summary>Retroceso táctico (Dron de Ataque). Override para lógica de retreat.</summary>
        protected virtual void OnRetreat() { }

        #endregion

        #region UTILIDADES

        protected void FlipSprite(float directionX)
        {
            if      (directionX >  0.01f) facingDirection =  1;
            else if (directionX < -0.01f) facingDirection = -1;

            if (spriteRenderer != null)
                spriteRenderer.flipX = facingDirection < 0;
        }

        protected float DistanceToPlayer()
        {
            if (playerTarget == null) return float.MaxValue;
            return Vector2.Distance(transform.position, playerTarget.position);
        }

        protected Vector2 DirectionToPlayer()
        {
            if (playerTarget == null) return Vector2.zero;
            return (playerTarget.position - transform.position).normalized;
        }

        #endregion

        #region GIZMOS

        protected virtual void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            Vector3 eyePos = transform.position + Vector3.up * eyeHeight;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(eyePos, 0.1f);

            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (!use360Detection)
            {
                int dir    = Application.isPlaying ? facingDirection : 1;
                float half = fieldOfView / 2f;
                Vector3 fwd   = new Vector3(dir, 0, 0);
                Vector3 left  = Quaternion.Euler(0, 0,  half) * fwd * detectionRadius;
                Vector3 right = Quaternion.Euler(0, 0, -half) * fwd * detectionRadius;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(eyePos, eyePos + left);
                Gizmos.DrawLine(eyePos, eyePos + right);
            }

            if (Application.isPlaying && playerTarget != null)
            {
                Vector3 targetCenter = playerTarget.position + Vector3.up * eyeHeight;
                Gizmos.color = HasLineOfSight(playerTarget.position) ? Color.green : Color.red;
                Gizmos.DrawLine(eyePos, targetCenter);
            }

            Vector3 origin = Application.isPlaying ? (Vector3)patrolOrigin : transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawLine(origin + Vector3.left * patrolDistance, origin + Vector3.right * patrolDistance);
        }

        #endregion
    }
}
