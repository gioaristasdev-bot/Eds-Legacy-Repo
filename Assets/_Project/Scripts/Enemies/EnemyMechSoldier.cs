using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// ACORAZADO — Unidad pesada terrestre con disparo frontal.
    ///
    /// Estados: Patrol → Attack (disparo frontal en ráfagas) → Cooldown → Hit → Death
    /// Patrulla lenta. Al detectar al jugador SE DETIENE y dispara hacia adelante.
    /// NO persigue (sin estado Chase). Alta HP, knockback reducido.
    /// Ciclo: sub-ráfagas con pausa entre ellas → Cooldown largo → repite.
    ///
    /// Chakras:
    ///   IHackable  → desactiva temporalmente (base EnemyBase)
    ///   IStunnable → interrumpe disparo (base EnemyBase)
    ///   Invisibilidad → reduce detección (base EnemyBase, layer InvisiblePlayer)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyMechSoldier : EnemyBase
    {
        #region CONFIGURACIÓN

        [Header("Acorazado - Daño por Contacto")]
        [SerializeField] private float contactDamage = 12f;
        [SerializeField] private float contactCooldown = 1f;
        [SerializeField] private Vector2 contactOffset = new Vector2(0f, 0.5f);

        [Header("Acorazado - Disparo Frontal")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 1.2f;
        [SerializeField] private float projectileDamage = 18f;
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Acorazado - Ráfaga y Cooldown")]
        [Tooltip("Disparos rápidos por sub-ráfaga")]
        [SerializeField] private int shotsPerBurst = 2;
        [Tooltip("Número de sub-ráfagas antes del Cooldown")]
        [SerializeField] private int burstCount = 2;
        [Tooltip("Pausa entre sub-ráfagas")]
        [SerializeField] private float burstPause = 0.6f;
        [Tooltip("Pausa larga después de completar todas las ráfagas")]
        [SerializeField] private float cooldownDuration = 3f;
        [Tooltip("Distancia máxima para iniciar ataque al detectar al jugador")]
        [SerializeField] private float maxShootDistance = 12f;

        [Header("Acorazado - Ground Check")]
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

        protected override void Start()
        {
            if (maxHealth <= 50f)  maxHealth      = 120f;
            if (detectionRadius < 10f) detectionRadius = 12f;
            base.Start();
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

            float radius = col != null ? col.bounds.extents.x + 0.2f : 0.7f;
            Collider2D hit = Physics2D.OverlapCircle((Vector2)transform.position + contactOffset, radius, playerLayer);
            if (hit == null) return;

            var damageable = hit.GetComponent<NABHI.Character.IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(contactDamage);
                lastContactTime = Time.time;
            }
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
            // El Acorazado no persigue — va directo a Attack o Patrol
            if (playerTarget != null && IsPlayerStillDetected())
                ChangeState(EnemyState.Attack);
            else
                ChangeState(EnemyState.Patrol);
        }

        protected override void OnAttack()
        {
            // Quieto durante toda la ráfaga — sin movimiento
            rb.velocity = new Vector2(0, rb.velocity.y);

            // Solo voltear para mirar al jugador
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

        protected override void OnCooldown()
        {
            rb.velocity = new Vector2(0, rb.velocity.y);

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (playerTarget != null && IsPlayerStillDetected() && DistanceToPlayer() <= maxShootDistance)
                    ChangeState(EnemyState.Attack);
                else
                {
                    playerTarget = null;
                    ChangeState(EnemyState.Patrol);
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

        #endregion

        #region DISPARO FRONTAL

        private void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[EnemyMechSoldier] {gameObject.name}: projectilePrefab no asignado");
                return;
            }

            OnShoot();

            Vector2 spawnPos = firePoint != null
                ? (Vector2)firePoint.position
                : (Vector2)transform.position + new Vector2(facingDirection * 0.6f, 0.1f);

            // Disparo FRONTAL — en la dirección que mira, no apunta al jugador
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

            // Bloquear X durante el disparo para evitar deslizamiento
            rb.constraints = newState == EnemyState.Attack
                ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation
                : RigidbodyConstraints2D.FreezeRotation;

            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);

            bool moving   = newState == EnemyState.Patrol;
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

            // Rango de disparo
            Gizmos.color = new Color(1f, 0.2f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxShootDistance);

            // Área de contacto
            float contactRadius = col != null ? col.bounds.extents.x + 0.2f : 0.7f;
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
            Gizmos.DrawWireSphere((Vector3)((Vector2)transform.position + contactOffset), contactRadius);

            // Fire point
            int dir = Application.isPlaying ? facingDirection : 1;
            Vector3 fp = firePoint != null
                ? firePoint.position
                : transform.position + new Vector3(dir * 0.6f, 0.1f, 0);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fp, 0.12f);
            Gizmos.color = new Color(1f, 0.4f, 0f);
            Gizmos.DrawLine(fp, fp + new Vector3(dir * 2f, 0, 0));

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
