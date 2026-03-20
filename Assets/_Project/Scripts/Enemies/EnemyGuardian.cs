using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// GUARDIÁN — Enemigo aéreo de distancia media.
    ///
    /// Estados (Documento Técnico): Hover → Track Player (Chase) → Shoot → Cooldown → Hit → Death
    /// Flotación lateral suave mientras patrulla. Persigue al jugador volando.
    /// Disparo energético rápido en ráfagas. Cooldown entre ráfagas.
    ///
    /// Chakras:
    ///   IHackable  → congela al guardián (base EnemyBase lo desactiva por hackDisableDuration)
    ///   IStunnable → desestabiliza el hover (base EnemyBase)
    ///   Invisibilidad → pierde al jugador como objetivo (base EnemyBase, layer InvisiblePlayer)
    ///
    /// Nota: No vulnerable a EMP (sin IEMPTarget).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyGuardian : EnemyBase
    {
        #region CONFIGURACIÓN GUARDIÁN

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
        [Tooltip("Número de disparos consecutivos por ráfaga")]
        [SerializeField] private int shotsPerBurst = 4;
        [Tooltip("Pausa entre ráfagas")]
        [SerializeField] private float cooldownDuration = 2.5f;

        [Header("Guardián - Rango de Ataque")]
        [Tooltip("Distancia para iniciar ataque (porcentaje del detectionRadius)")]
        [SerializeField] private float attackRangeMultiplier = 0.75f;

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Vector2 hoverCenter;
        private float lastFireTime;
        private int shotsFired;
        private float cooldownTimer;

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
            // Guardián usa FOV (no 360°) y requiere línea de visión
            use360Detection    = false;
            requireLineOfSight = true;
            hoverCenter        = transform.position;
            base.Start();
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
            // Hover lateral suave: movimiento sinusoidal en X + oscilación en Y
            float t       = Time.time;
            float xOffset = Mathf.Sin(t * patrolSpeed * 0.4f) * lateralRange;
            float yOffset = Mathf.Sin(t * hoverFrequency) * hoverAmplitude;

            Vector2 targetPos = hoverCenter + new Vector2(xOffset, yOffset);
            Vector2 moveDir   = targetPos - (Vector2)transform.position;

            rb.velocity = moveDir.normalized * patrolSpeed;
            FlipSprite(rb.velocity.x);
        }

        protected override void OnChase()
        {
            if (playerTarget == null) return;

            float dist         = DistanceToPlayer();
            float attackRange  = detectionRadius * attackRangeMultiplier;

            // Si está en rango de ataque, comenzar ráfaga
            if (dist <= attackRange)
            {
                ChangeState(EnemyState.Attack);
                return;
            }

            // Volar hacia el jugador con hover vertical
            Vector2 dir    = DirectionToPlayer();
            float   yHover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            rb.velocity    = new Vector2(dir.x * chaseSpeed, dir.y * chaseSpeed + yHover);
            FlipSprite(dir.x);
        }

        protected override void OnAttack()
        {
            if (playerTarget == null || !IsPlayerStillDetected())
            {
                ChangeState(playerTarget != null ? EnemyState.Chase : EnemyState.Patrol);
                return;
            }

            // Flotar ligeramente en posición mientras dispara
            float yHover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude * 0.5f;
            rb.velocity  = new Vector2(0, yHover);

            // Mirar al jugador
            FlipSprite(playerTarget.position.x - transform.position.x);

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
            // Hover suave durante el cooldown (no persigue)
            float yHover = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            rb.velocity  = new Vector2(0, yHover);

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (playerTarget != null && IsPlayerStillDetected())
                    ChangeState(EnemyState.Attack);
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
                Debug.LogWarning($"[EnemyGuardian] {gameObject.name}: Sin projectilePrefab asignado");
                return;
            }

            Vector2 spawnPos = firePoint != null
                ? (Vector2)firePoint.position
                : (Vector2)transform.position + new Vector2(facingDirection * 0.5f, 0f);

            // Apuntar directamente al jugador (proyectiles rápidos y precisos)
            Vector2 dir = DirectionToPlayer();

            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.SetDamage(projectileDamage);
                proj.Initialize(dir, projectileSpeed);
            }

            Debug.Log($"[EnemyGuardian] Disparo {shotsFired + 1}/{shotsPerBurst}");
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        protected override void OnAnimStateChanged(EnemyState newState)
        {
            // Inicializar timer de Cooldown
            if (newState == EnemyState.Cooldown)
                cooldownTimer = cooldownDuration;

            // Resetear contador de disparos al salir de Attack
            if (newState != EnemyState.Attack && newState != EnemyState.Cooldown)
                shotsFired = 0;

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);

            bool moving = newState == EnemyState.Patrol
                       || newState == EnemyState.Chase;
            animator.SetBool(AnimParam.IsMoving, moving);

            if (newState == EnemyState.Hit)  animator.SetTrigger(AnimParam.Hit);
            if (newState == EnemyState.Dead) animator.SetBool(AnimParam.IsDead, true);

            // ESTADOS GUARDIÁN (para el artista):
            // Hover/Patrol = 0-1 → animación de flotación tranquila
            // Chase = 2       → inclinación hacia adelante / propulsores activos
            // Attack = 3      → posición de disparo, cañones desplegados
            // Cooldown = 4    → volver a pose neutral, vent de calor
            // Hit = 5         → sacudida / destello
            // Dead = 10       → caída con humo
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
            rb.gravityScale = 1f; // El guardián cae al morir
            rb.velocity     = Vector2.zero;

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            animator?.SetBool(AnimParam.IsDead, true);
            // Instantiate(deathVFX, transform.position, Quaternion.identity);
            */
        }

        #endregion

        #region GIZMOS

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

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
