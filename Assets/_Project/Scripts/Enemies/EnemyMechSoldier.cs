using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// ACORAZADO — Unidad pesada terrestre con disparo frontal.
    ///
    /// Estados (Documento Técnico): Idle → Patrol → Shoot → Cooldown → Hit → Death
    /// Patrulla lenta. Al detectar al jugador SE DETIENE y dispara hacia adelante.
    /// NO persigue (sin estado Chase). Alta HP, knockback reducido.
    /// Ciclo de disparo: ráfaga de X tiros → Cooldown → repite si jugador visible.
    ///
    /// Chakras:
    ///   IHackable  → lo desactiva temporalmente (base EnemyBase)
    ///   IStunnable → interrumpe disparo (base EnemyBase)
    ///   Invisibilidad → reduce detección (base EnemyBase, layer InvisiblePlayer)
    ///
    /// Nota de implementación:
    ///   - Disparo FRONTAL: el proyectil va en la dirección que mira (facingDirection, 0),
    ///     NO apunta al jugador. El Acorazado no trackea, solo dispara hacia adelante.
    ///   - Sin IEMPTarget (el doc no lo menciona para este enemigo).
    ///   - Ground check usa col.bounds.min.y para precisión (fix de sesión 23).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyMechSoldier : EnemyBase
    {
        #region CONFIGURACIÓN ACORAZADO

        [Header("Acorazado - Disparo Frontal")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [Tooltip("Tiempo entre tiros dentro de una ráfaga")]
        [SerializeField] private float fireRate = 1.2f;
        [SerializeField] private float projectileDamage = 18f;
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Acorazado - Ráfaga y Cooldown")]
        [Tooltip("Disparos consecutivos antes del Cooldown")]
        [SerializeField] private int shotsPerBurst = 2;
        [Tooltip("Duración del Cooldown entre ráfagas")]
        [SerializeField] private float cooldownDuration = 3f;

        [Header("Acorazado - Rango de Ataque")]
        [Tooltip("Distancia máxima para iniciar ataque al detectar al jugador")]
        [SerializeField] private float maxShootDistance = 12f;

        [Header("Acorazado - Ground Check")]
        [SerializeField] private float groundCheckDistance = 1.5f;
        [SerializeField] private float edgeCheckOffset = 0.8f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float feetCheckDistance = 0.3f;

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Collider2D col;
        private float lastFireTime;
        private int shotsFired;
        private float cooldownTimer;

        #endregion

        #region UNITY CALLBACKS

        protected override void Awake()
        {
            base.Awake();
            rb  = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // El Acorazado es resistente al knockback
            // (también configurable en el Inspector vía herencia de EnemyBase)
        }

        protected override void Start()
        {
            // Alta HP para el Acorazado (override si no se ajustó en Inspector)
            if (maxHealth <= 50f)
                maxHealth = 120f;

            // Knockback reducido (si no se ajustó en Inspector)
            // knockbackResistance se serializa en EnemyBase; valor recomendado: 0.7

            // Radio de detección ajustado para detectar en FOV frontal
            if (detectionRadius < 10f)
                detectionRadius = 12f;

            base.Start();
        }

        #endregion

        #region MÁQUINA DE ESTADOS (override — sin Chase)

        protected override void UpdateState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                case EnemyState.Patrol:
                    OnPatrol();
                    ScanForPlayer();
                    // Al detectar al jugador: DETENERSE y pasar a Attack (sin Chase)
                    if (playerTarget != null && DistanceToPlayer() <= maxShootDistance)
                        ChangeState(EnemyState.Attack);
                    break;

                case EnemyState.Attack:
                    OnAttack();
                    break;

                case EnemyState.Cooldown:
                    OnCooldown();
                    break;
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
            // El Acorazado NO persigue. Si llegara aquí, pasa a Attack o Patrol.
            if (playerTarget != null && IsPlayerStillDetected())
                ChangeState(EnemyState.Attack);
            else
                ChangeState(EnemyState.Patrol);
        }

        protected override void OnAttack()
        {
            // Detener movimiento mientras dispara
            rb.velocity = new Vector2(0, rb.velocity.y);

            // Si el jugador se aleja o pierde visión, retomar patrulla
            if (playerTarget == null || !IsPlayerStillDetected() || DistanceToPlayer() > maxShootDistance * 1.3f)
            {
                playerTarget = null;
                ChangeState(EnemyState.Patrol);
                return;
            }

            // Voltear para mirar al jugador, pero disparar recto (horizontal)
            float dirToPlayer = playerTarget.position.x - transform.position.x;
            FlipSprite(dirToPlayer);

            if (Time.time - lastFireTime >= fireRate)
            {
                FireProjectile();
                lastFireTime = Time.time;
                shotsFired++;

                if (shotsFired >= shotsPerBurst)
                {
                    shotsFired = 0;
                    ChangeState(EnemyState.Cooldown);
                }
            }
        }

        protected override void OnCooldown()
        {
            // Detenido durante el Cooldown
            rb.velocity = new Vector2(0, rb.velocity.y);

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (playerTarget != null && IsPlayerStillDetected())
                    ChangeState(EnemyState.Attack);
                else
                {
                    playerTarget = null;
                    ChangeState(EnemyState.Patrol);
                }
            }
        }

        #endregion

        #region DISPARO FRONTAL

        /// <summary>
        /// Dispara en dirección FRONTAL (facingDirection, 0) — no apunta al jugador.
        /// Esto distingue al Acorazado: dispara donde mira, no donde está el jugador.
        /// </summary>
        private void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[EnemyMechSoldier/Acorazado] {gameObject.name}: Sin projectilePrefab");
                return;
            }

            Vector2 spawnPos = firePoint != null
                ? (Vector2)firePoint.position
                : (Vector2)transform.position + new Vector2(facingDirection * 0.6f, 0.1f);

            // Disparo HORIZONTAL en la dirección que mira
            Vector2 dir = new Vector2(facingDirection, 0);

            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.SetDamage(projectileDamage);
                proj.Initialize(dir, projectileSpeed);
            }

            Debug.Log($"[EnemyMechSoldier/Acorazado] Disparo frontal ({shotsFired + 1}/{shotsPerBurst})");
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        protected override void OnAnimStateChanged(EnemyState newState)
        {
            // Inicializar timer de Cooldown
            if (newState == EnemyState.Cooldown)
                cooldownTimer = cooldownDuration;

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);

            bool moving = newState == EnemyState.Patrol;
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
            // Instantiate(deathVFX, transform.position, Quaternion.identity);
            */
        }

        #endregion

        #region GROUND CHECK (con fix de col.bounds.min.y)

        private bool IsGrounded()
        {
            float originY = col != null ? col.bounds.min.y : transform.position.y;
            Vector2 origin = new Vector2(transform.position.x, originY);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, feetCheckDistance, groundLayer);
            return hit.collider != null;
        }

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

            // Rango de disparo
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxShootDistance);

            // Fire point
            int dir = Application.isPlaying ? facingDirection : 1;
            Vector3 fp = firePoint != null
                ? firePoint.position
                : transform.position + new Vector3(dir * 0.6f, 0.1f, 0);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fp, 0.12f);

            // Dirección de disparo (línea horizontal)
            Gizmos.color = new Color(1f, 0.4f, 0f);
            Gizmos.DrawLine(fp, fp + new Vector3(dir * 2f, 0, 0));
        }

        #endregion
    }
}
