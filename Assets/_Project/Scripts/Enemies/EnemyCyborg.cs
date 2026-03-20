using UnityEngine;
using NABHI.Character;

namespace NABHI.Enemies
{
    /// <summary>
    /// CYBORG — Enemigo melee terrestre.
    ///
    /// Estados (Documento Técnico): Idle → Chase → Attack → Recover → Hit → Death
    /// Patrulla horizontal con ground check, persecución al jugador.
    /// Doble sistema de daño: contacto continuo + golpe melee fuerte.
    /// Estado Recover: pausa post-ataque antes de retomar Chase.
    ///
    /// Chakras:
    ///   IHackable  → lo controla brevemente (base EnemyBase desactiva por hackDisableDuration)
    ///   IStunnable → interrumpe Chase/Attack (base EnemyBase)
    ///   Invisibilidad → deja de detectar al jugador (base EnemyBase, layer InvisiblePlayer)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyCyborg : EnemyBase
    {
        #region CONFIGURACIÓN CYBORG

        [Header("Cyborg - Daño por Contacto")]
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float contactCooldown = 0.8f;

        [Header("Cyborg - Golpe Melee (fuerte)")]
        [SerializeField] private float meleeDamage = 20f;
        [SerializeField] private float meleeRange = 1.5f;
        [SerializeField] private float meleeCooldown = 1.5f;
        [SerializeField] private Vector2 meleeKnockback = new Vector2(8f, 4f);

        [Header("Cyborg - Ground Check")]
        [SerializeField] private float groundCheckDistance = 1.5f;
        [SerializeField] private float edgeCheckOffset = 0.8f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Cyborg - Ataque Visual")]
        [SerializeField] private float attackWindup = 0.3f;

        [Header("Cyborg - Recover (post-ataque)")]
        [Tooltip("Duración de la pausa de recuperación tras el golpe melee antes de retomar Chase")]
        [SerializeField] private float recoverDuration = 0.6f;

        [Header("Cyborg - Ground Check (Self)")]
        [SerializeField] private float feetCheckDistance = 0.3f;

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Collider2D col;
        private float lastAttackTime;
        private float lastContactTime;
        private float attackWindupTimer;
        private bool isAttacking;

        // Recover
        private float recoverTimer;

        #endregion

        #region UNITY CALLBACKS

        protected override void Awake()
        {
            base.Awake();
            rb  = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
            CheckContactDamage();
        }

        /// <summary>
        /// Chequeo de daño por contacto usando OverlapCircle
        /// (funciona con Physics2D.IgnoreLayerCollision entre Player y Enemies).
        /// </summary>
        private void CheckContactDamage()
        {
            if (isDead || isStunned || isHacked) return;
            if (Time.time - lastContactTime < contactCooldown) return;

            float contactRadius = col != null ? col.bounds.extents.x + 0.2f : 0.6f;
            Collider2D hit = Physics2D.OverlapCircle(transform.position, contactRadius, playerLayer);

            if (hit == null) return;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(contactDamage);
                lastContactTime = Time.time;
                Debug.Log($"[EnemyCyborg] Daño por contacto: {contactDamage}");
            }
        }

        #endregion

        #region COMPORTAMIENTO

        protected override void OnPatrol()
        {
            if (!IsGrounded())
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                return;
            }

            int moveDir = movingRight ? 1 : -1;
            if (!IsGroundAhead(moveDir))
            {
                movingRight = !movingRight;
                FlipSprite(movingRight ? 1f : -1f);
            }

            float distFromOrigin = transform.position.x - patrolOrigin.x;
            if (distFromOrigin > patrolDistance)
            {
                movingRight = false;
                FlipSprite(-1f);
            }
            else if (distFromOrigin < -patrolDistance)
            {
                movingRight = true;
                FlipSprite(1f);
            }

            float moveX = movingRight ? patrolSpeed : -patrolSpeed;
            rb.velocity = new Vector2(moveX, rb.velocity.y);
        }

        protected override void OnChase()
        {
            if (playerTarget == null) return;

            float distToPlayer = DistanceToPlayer();

            if (distToPlayer <= meleeRange && Time.time - lastAttackTime >= meleeCooldown)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            if (!IsGrounded())
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                return;
            }

            float dirX = playerTarget.position.x - transform.position.x;
            int chaseDir = dirX > 0 ? 1 : -1;
            FlipSprite(dirX);

            if (!IsGroundAhead(chaseDir))
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                return;
            }

            float moveX = chaseDir * chaseSpeed;
            rb.velocity = new Vector2(moveX, rb.velocity.y);
        }

        protected override void OnAttack()
        {
            rb.velocity = new Vector2(0, rb.velocity.y);

            if (!isAttacking)
            {
                isAttacking = true;
                attackWindupTimer = attackWindup;
                return;
            }

            attackWindupTimer -= Time.deltaTime;
            if (attackWindupTimer <= 0)
            {
                ExecuteMeleeAttack();
                isAttacking = false;
                lastAttackTime = Time.time;
                // Transición a Recover en vez de Chase directamente
                ChangeState(EnemyState.Recover);
            }
        }

        protected override void OnRecover()
        {
            // Quedarse quieto durante la recuperación
            rb.velocity = new Vector2(0, rb.velocity.y);

            recoverTimer -= Time.deltaTime;
            if (recoverTimer <= 0)
                ChangeState(EnemyState.Chase);
        }

        #endregion

        #region ATAQUE MELEE

        private void ExecuteMeleeAttack()
        {
            if (playerTarget == null) return;

            Collider2D hit = Physics2D.OverlapCircle(
                (Vector2)transform.position + new Vector2(facingDirection * meleeRange * 0.5f, 0),
                meleeRange,
                playerLayer
            );

            if (hit == null)
            {
                Debug.Log($"[EnemyCyborg] Golpe falló - jugador fuera de rango");
                return;
            }

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(meleeDamage);
                Debug.Log($"[EnemyCyborg] Golpe melee: {meleeDamage} daño");

                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Vector2 knockDir = new Vector2(facingDirection, 0.5f).normalized;
                    playerHealth.ApplyKnockback(knockDir * meleeKnockback.magnitude);
                }
            }
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        protected override void OnAnimStateChanged(EnemyState newState)
        {
            // Inicializar timer de Recover al entrar en ese estado
            if (newState == EnemyState.Recover)
                recoverTimer = recoverDuration;

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);

            bool moving = newState == EnemyState.Chase
                       || newState == EnemyState.Patrol;
            animator.SetBool(AnimParam.IsMoving, moving);

            if (newState == EnemyState.Hit)  animator.SetTrigger(AnimParam.Hit);
            if (newState == EnemyState.Dead) animator.SetBool(AnimParam.IsDead, true);
            */
        }

        protected override void OnHitReceived()
        {
            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            animator?.SetTrigger(AnimParam.Hit);
            */
        }

        protected override void OnDeath()
        {
            rb.velocity = Vector2.zero;

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            animator?.SetBool(AnimParam.IsDead, true);
            // Instanciar VFX de muerte:
            // Instantiate(deathVFX, transform.position, Quaternion.identity);
            */
        }

        #endregion

        #region GROUND CHECK

        /// <summary>
        /// Verifica si hay suelo debajo. Raycast desde la base del collider (col.bounds.min.y).
        /// </summary>
        private bool IsGrounded()
        {
            float originY = col != null ? col.bounds.min.y : transform.position.y;
            Vector2 origin = new Vector2(transform.position.x, originY);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, feetCheckDistance, groundLayer);
            return hit.collider != null;
        }

        /// <summary>
        /// Verifica si hay suelo adelante (detección de bordes de plataforma).
        /// Raycast desde la base del collider + offset horizontal.
        /// </summary>
        private bool IsGroundAhead(int direction)
        {
            float originY = col != null ? col.bounds.min.y : transform.position.y;
            Vector2 checkPos = new Vector2(transform.position.x + direction * edgeCheckOffset, originY);
            RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, groundCheckDistance, groundLayer);
            return hit.collider != null;
        }

        #endregion

        #region GIZMOS

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Área de golpe melee (frente)
            int meleeDir = Application.isPlaying ? facingDirection : 1;
            Vector3 meleeCenter = transform.position + new Vector3(meleeDir * meleeRange * 0.5f, 0, 0);
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawWireSphere(meleeCenter, meleeRange);

            // Ground check (pies)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * feetCheckDistance);

            // Edge check (adelante)
            int dir = Application.isPlaying ? facingDirection : 1;
            Vector3 checkPos = transform.position + new Vector3(dir * edgeCheckOffset, 0, 0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(checkPos, checkPos + Vector3.down * groundCheckDistance);
        }

        #endregion
    }
}
