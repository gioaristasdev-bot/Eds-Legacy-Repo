using UnityEngine;
using NABHI.Character;

namespace NABHI.Enemies
{
    /// <summary>
    /// CYBORG — Enemigo terrestre de rango medio.
    ///
    /// Estados: Patrol → Chase → Attack (disparo en ráfaga) → Cooldown → Hit → Death
    /// Patrulla horizontal con ground check. Al detectar al jugador lo persigue.
    /// Al entrar en rango de ataque se detiene y dispara en ráfagas.
    ///
    /// Chakras:
    ///   IHackable  → desactiva temporalmente (base EnemyBase)
    ///   IStunnable → interrumpe Chase/Attack (base EnemyBase)
    ///   Invisibilidad → deja de detectar al jugador (base EnemyBase)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyCyborg : EnemyBase
    {
        #region CONFIGURACIÓN

        [Header("Cyborg - Daño por Contacto")]
        [SerializeField] private float contactDamage = 5f;
        [SerializeField] private float contactCooldown = 1f;

        [Header("Cyborg - Disparo")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.6f;
        [SerializeField] private float projectileDamage = 12f;
        [SerializeField] private float projectileSpeed = 14f;

        [Header("Cyborg - Ráfaga y Cooldown")]
        [Tooltip("Disparos rápidos por sub-ráfaga")]
        [SerializeField] private int shotsPerBurst = 3;
        [Tooltip("Número de sub-ráfagas antes del Cooldown")]
        [SerializeField] private int burstCount = 2;
        [Tooltip("Pausa entre sub-ráfagas (ej: entre los 3 primeros y los 3 siguientes)")]
        [SerializeField] private float burstPause = 0.5f;
        [Tooltip("Pausa larga después de completar todas las ráfagas")]
        [SerializeField] private float cooldownDuration = 2.5f;
        [Tooltip("Distancia a la que deja de perseguir y empieza a disparar")]
        [SerializeField] private float attackRange = 6f;

        [Header("Cyborg - Ground Check")]
        [SerializeField] private float groundCheckDistance = 1.5f;
        [SerializeField] private float edgeCheckOffset = 0.8f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float feetCheckDistance = 0.3f;
        [Tooltip("Offset del origen del ground check relativo al pivot. Ajustar si el gizmo no coincide con los pies.")]
        [SerializeField] private Vector2 groundCheckOriginOffset = Vector2.zero;
        [Tooltip("Desactiva el ground check temporalmente para probar movimiento")]
        [SerializeField] private bool ignoreGroundCheck = false;

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Collider2D col;
        private float lastContactTime;
        private float lastFireTime;
        private int currentShot;
        private int currentBurst;
        private bool inBurstPause;
        private float burstPauseTimer;
        private float cooldownTimer;

        #endregion

        #region UNITY CALLBACKS

        protected override void Awake()
        {
            base.Awake();
            rb  = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        protected override void Update()
        {
            base.Update();
            CheckContactDamage();
        }

private void CheckContactDamage()
        {
            if (isDead || isStunned || isHacked) return;
            if (Time.time - lastContactTime < contactCooldown) return;

            float radius = col != null ? col.bounds.extents.x + 0.2f : 0.6f;
            Collider2D hit = Physics2D.OverlapCircle(transform.position, radius, playerLayer);
            if (hit == null) return;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(contactDamage);
                lastContactTime = Time.time;
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
                movingRight = !movingRight;

            float distFromOrigin = transform.position.x - patrolOrigin.x;
            if (distFromOrigin > patrolDistance)       movingRight = false;
            else if (distFromOrigin < -patrolDistance) movingRight = true;

            float moveX = movingRight ? patrolSpeed : -patrolSpeed;
            rb.velocity = new Vector2(moveX, rb.velocity.y);
            FlipSprite(moveX);
        }

        protected override void OnChase()
        {
            if (playerTarget == null) return;

            if (DistanceToPlayer() <= attackRange)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                ChangeState(EnemyState.Attack);
                return;
            }

            if (!IsGrounded())
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                return;
            }

            float dirX    = playerTarget.position.x - transform.position.x;
            int chaseDir  = dirX > 0 ? 1 : -1;
            FlipSprite(dirX);

            if (!IsGroundAhead(chaseDir))
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
                return;
            }

            rb.velocity = new Vector2(chaseDir * chaseSpeed, rb.velocity.y);
        }

        protected override void OnAttack()
        {
            // Quieto durante toda la ráfaga — sin persecución
            rb.velocity = new Vector2(0, rb.velocity.y);

            // Solo voltear para mirar al jugador, sin moverse
            if (playerTarget != null)
                FlipSprite(playerTarget.position.x - transform.position.x);

            // Pausa entre sub-ráfagas
            if (inBurstPause)
            {
                burstPauseTimer -= Time.deltaTime;
                if (burstPauseTimer <= 0f)
                    inBurstPause = false;
                return;
            }

            if (Time.time - lastFireTime >= fireRate)
            {
                FireProjectile();
                lastFireTime = Time.time;
                currentShot++;

                if (currentShot >= shotsPerBurst)
                {
                    currentShot = 0;
                    currentBurst++;

                    if (currentBurst >= burstCount)
                    {
                        ResetBurstState();
                        ChangeState(EnemyState.Cooldown);
                    }
                    else
                    {
                        inBurstPause    = true;
                        burstPauseTimer = burstPause;
                    }
                }
            }
        }

        private void ResetBurstState()
        {
            currentShot     = 0;
            currentBurst    = 0;
            inBurstPause    = false;
            burstPauseTimer = 0f;
        }

        protected override void OnCooldown()
        {
            rb.velocity = new Vector2(0, rb.velocity.y);

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (playerTarget != null && IsPlayerStillDetected())
                    ChangeState(EnemyState.Chase);
                else
                    ChangeState(EnemyState.Patrol);
            }
        }

        #endregion

        #region DISPARO

        private void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[EnemyCyborg] {gameObject.name}: projectilePrefab no asignado");
                return;
            }

            OnShoot();

            Vector2 spawnPos = firePoint != null
                ? (Vector2)firePoint.position
                : (Vector2)transform.position + new Vector2(facingDirection * 0.5f, 0.2f);

            // Disparo frontal: siempre en la dirección que mira, sin apuntar al jugador
            Vector2 dir = new Vector2(facingDirection, 0);

            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.SetDamage(projectileDamage);
                proj.Initialize(dir, projectileSpeed);
            }
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        protected override void OnAnimStateChanged(EnemyState newState)
        {
            if (newState == EnemyState.Cooldown)
                cooldownTimer = cooldownDuration;

            // Bloquear X durante el disparo para evitar deslizamiento por inercia
            rb.constraints = newState == EnemyState.Attack
                ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                : RigidbodyConstraints2D.FreezeRotation;

            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);

            bool moving   = newState == EnemyState.Patrol || newState == EnemyState.Chase;
            bool shooting = newState == EnemyState.Attack;
            animator.SetBool(AnimParam.IsMoving, moving);
            animator.SetBool("IsShooting", shooting);

            if (newState == EnemyState.Hit)  animator.SetTrigger(AnimParam.Hit);
            if (newState == EnemyState.Dead) animator.SetBool(AnimParam.IsDead, true);
        }

        protected override void OnHitReceived()
        {
            base.OnHitReceived();
            animator?.SetTrigger(AnimParam.Hit);
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            rb.velocity = Vector2.zero;
            animator?.SetBool(AnimParam.IsDead, true);
        }

        #endregion

        #region GROUND CHECK

        private Vector2 GetGroundCheckOrigin()
        {
            float baseY = col != null ? col.bounds.min.y : transform.position.y;
            return new Vector2(transform.position.x, baseY) + groundCheckOriginOffset;
        }

        private bool IsGrounded()
        {
            if (ignoreGroundCheck) return true;
            RaycastHit2D hit = Physics2D.Raycast(GetGroundCheckOrigin(), Vector2.down, feetCheckDistance, groundLayer);
            return hit.collider != null;
        }

        private bool IsGroundAhead(int direction)
        {
            if (ignoreGroundCheck) return true;
            Vector2 origin = GetGroundCheckOrigin() + new Vector2(direction * edgeCheckOffset, 0);
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
            return hit.collider != null;
        }

        #endregion

        #region GIZMOS

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Rango de ataque
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Fire point
            int dir = Application.isPlaying ? facingDirection : 1;
            Vector3 fp = firePoint != null
                ? firePoint.position
                : transform.position + new Vector3(dir * 0.5f, 0.2f, 0);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fp, 0.1f);

            // Ground check origin
            float baseY = col != null ? col.bounds.min.y : transform.position.y;
            Vector3 feetOrigin = new Vector3(transform.position.x, baseY) + (Vector3)groundCheckOriginOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(feetOrigin, 0.05f);
            Gizmos.DrawLine(feetOrigin, feetOrigin + Vector3.down * feetCheckDistance);

            // Edge check
            Vector3 edgeOrigin = feetOrigin + new Vector3(dir * edgeCheckOffset, 0, 0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(edgeOrigin, 0.05f);
            Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector3.down * groundCheckDistance);
        }

        #endregion
    }
}
