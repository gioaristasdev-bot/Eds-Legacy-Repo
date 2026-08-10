using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// GUARDIÁN — Enemigo aéreo de distancia media.
    ///
    /// Estados: Hover (Patrol) → Chase → Attack (ráfagas) → Cooldown → Hit → Death
    /// Flotación sinusoidal mientras patrulla. Persigue volando. Dispara apuntando al jugador.
    /// gravityScale=0 en vida → gravityScale=1 al morir (cae).
    ///
    /// Chakras:
    ///   IHackable  → congela (base EnemyBase)
    ///   IStunnable → desestabiliza el hover (base EnemyBase)
    ///   Invisibilidad → pierde objetivo (base EnemyBase, layer InvisiblePlayer)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyGuardian : EnemyBase
    {
        #region CONFIGURACIÓN

        [Header("Guardián - Hover/Patrulla")]
        [Tooltip("Amplitud de la oscilación vertical durante el hover")]
        [SerializeField] private float hoverAmplitude = 0.35f;
        [Tooltip("Frecuencia de la oscilación vertical")]
        [SerializeField] private float hoverFrequency = 1.8f;
        [Tooltip("Alcance horizontal de la patrulla aérea")]
        [SerializeField] private float lateralRange = 4f;

        [Header("Guardián - Disparo")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [Tooltip("Tiempo entre tiros dentro de una ráfaga")]
        [SerializeField] private float fireRate = 0.4f;
        [SerializeField] private float projectileDamage = 8f;
        [SerializeField] private float projectileSpeed = 18f;

        [Header("Guardián - Ráfaga y Cooldown")]
        [Tooltip("Disparos rápidos por sub-ráfaga")]
        [SerializeField] private int shotsPerBurst = 4;
        [Tooltip("Número de sub-ráfagas antes del Cooldown")]
        [SerializeField] private int burstCount = 2;
        [Tooltip("Pausa entre sub-ráfagas")]
        [SerializeField] private float burstPause = 0.5f;
        [Tooltip("Pausa larga después de completar todas las ráfagas")]
        [SerializeField] private float cooldownDuration = 2.5f;

        [Header("Guardián - Rango de Ataque")]
        [Tooltip("Distancia para iniciar ataque (porcentaje del detectionRadius)")]
        [SerializeField] private float attackRangeMultiplier = 0.75f;

        [Header("Guardián - Daño por Contacto")]
        [SerializeField] private float contactDamage = 8f;
        [SerializeField] private float contactCooldown = 1f;
        [Tooltip("Offset del centro del área de contacto relativo al pivot del prefab")]
        [SerializeField] private Vector2 contactOffset = new Vector2(0f, 0.5f);

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Vector2 hoverCenter;
        private float lastFireTime;
        private int currentShot;
        private int currentBurst;
        private bool inBurstPause;
        private float burstPauseTimer;
        private float cooldownTimer;
        private float lastContactTime;

        #endregion

        #region UNITY CALLBACKS

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        }

        protected override void Start()
        {
            use360Detection    = false;
            requireLineOfSight = true;
            hoverCenter        = transform.position;
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

            Collider2D hit = Physics2D.OverlapCircle((Vector2)transform.position + contactOffset, 0.7f, playerLayer);
            if (hit == null) return;

            var damageable = hit.GetComponent<NABHI.Character.IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(contactDamage);
                lastContactTime = Time.time;
            }
        }

        #endregion

        #region MÁQUINA DE ESTADOS (override)

        protected override void UpdateState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
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
            }
        }

        #endregion

        #region COMPORTAMIENTO

        protected override void OnPatrol()
        {
            float t       = Time.time;
            float xOffset = Mathf.Sin(t * patrolSpeed * 0.4f) * lateralRange;
            float yOffset = Mathf.Sin(t * hoverFrequency) * hoverAmplitude;

            Vector2 targetPos = hoverCenter + new Vector2(xOffset, yOffset);
            Vector2 moveDir   = targetPos - (Vector2)transform.position;

            rb.linearVelocity = moveDir.normalized * patrolSpeed;
            // Derivada del seno (coseno) para detectar dirección sin oscilación en cruce de cero
            FlipSprite(Mathf.Cos(t * patrolSpeed * 0.4f));
        }

        protected override void OnChase()
        {
            if (playerTarget == null) return;

            float dist        = DistanceToPlayer();
            float attackRange = detectionRadius * attackRangeMultiplier;

            if (dist <= attackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            Vector2 dir    = DirectionToPlayer();
            float   yHover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            rb.linearVelocity    = new Vector2(dir.x * chaseSpeed, dir.y * chaseSpeed + yHover);
            FlipSprite(dir.x);
        }

        protected override void OnAttack()
        {
            if (playerTarget == null || !IsPlayerStillDetected())
            {
                ResetBurstState();
                ChangeState(playerTarget != null ? EnemyState.Chase : EnemyState.Patrol);
                return;
            }

            // Hover suave en posición mientras dispara
            float yHover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude * 0.5f;
            rb.linearVelocity  = new Vector2(0, yHover);

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
            // Hover suave durante el cooldown (no persigue)
            float yHover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            rb.linearVelocity  = new Vector2(0, yHover);

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (playerTarget != null && IsPlayerStillDetected())
                    ChangeState(EnemyState.Attack);
                else
                    ChangeState(EnemyState.Patrol);
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

        #region DISPARO

        private void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[EnemyGuardian] {gameObject.name}: Sin projectilePrefab asignado");
                return;
            }

            OnShoot();

            Vector2 spawnPos = firePoint != null
                ? (Vector2)firePoint.position
                : (Vector2)transform.position + new Vector2(facingDirection * 0.5f, 0f);

            // Apuntar directamente al jugador (proyectil rápido y preciso)
            Vector2 dir = DirectionToPlayer();

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
            rb.gravityScale = 1f; // Cae al morir
            rb.linearVelocity     = Vector2.zero;
            animator?.SetBool(AnimParam.IsDead, true);
        }

        #endregion

        #region GIZMOS

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Área de contacto
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.35f);
            Gizmos.DrawWireSphere((Vector3)((Vector2)transform.position + contactOffset), 0.7f);

            // Rango de ataque
            float attackRange = detectionRadius * attackRangeMultiplier;
            Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Fire point
            int     dir = Application.isPlaying ? facingDirection : 1;
            Vector3 fp  = firePoint != null
                ? firePoint.position
                : transform.position + new Vector3(dir * 0.5f, 0f, 0);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(fp, 0.1f);

            // Centro de hover (en Play)
            if (Application.isPlaying)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.4f);
                Gizmos.DrawWireSphere(hoverCenter, 0.25f);
                Vector3 hoverCenter3 = hoverCenter;
                Gizmos.DrawLine(hoverCenter3 + Vector3.left  * lateralRange,
                                hoverCenter3 + Vector3.right * lateralRange);
            }
        }

        #endregion
    }
}
