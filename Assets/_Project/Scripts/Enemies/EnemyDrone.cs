using UnityEngine;
using NABHI.Chakras.Abilities;

namespace NABHI.Enemies
{
    /// <summary>
    /// DRON DE ATAQUE — Enemigo aéreo rápido.
    ///
    /// Estados (Documento Técnico): Patrol Air → Chase → Attack → Retreat → Hit → Death
    /// Movimiento sinusoidal en patrulla. Persecución directa al jugador.
    /// Daño por contacto. Tras impactar: Retreat (retroceso táctico) antes de reiniciar.
    ///
    /// Chakras:
    ///   IHackable  → pausa temporal (base EnemyBase)
    ///   IEMPTarget → lo desactiva (implementado en este script)
    ///   IStunnable → interrumpe (base EnemyBase)
    ///   Invisibilidad → reduce tracking (base EnemyBase, layer InvisiblePlayer)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyDrone : EnemyBase, IEMPTarget
    {
        #region CONFIGURACIÓN DRONE

        [Header("Dron - Movimiento")]
        [SerializeField] private float floatAmplitude = 0.5f;
        [SerializeField] private float floatFrequency = 2f;

        [Header("Dron - Daño por Contacto")]
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float contactCooldown = 1f;

        [Header("Dron - Retreat (retroceso táctico)")]
        [Tooltip("Duración del retroceso tras impactar al jugador")]
        [SerializeField] private float retreatDuration = 1.2f;
        [Tooltip("Velocidad durante el retroceso")]
        [SerializeField] private float retreatSpeed = 5f;

        [Header("Dron - EMP")]
        [SerializeField] private Color empDisabledColor = new Color(0.3f, 0.3f, 0.3f);

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Vector2 patrolCenter;
        private float sinOffset;
        private float lastContactDamageTime;

        // Retreat
        private float retreatTimer;
        private Vector2 retreatDirection;

        // EMP
        private bool isDisabledByEMP;
        private float empTimer;

        #endregion

        #region PROPIEDADES

        public bool IsDisabledByEMP => isDisabledByEMP;

        #endregion

        #region UNITY CALLBACKS

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        protected override void Start()
        {
            // Detección 360° sin línea de visión (drone ve en todas direcciones)
            use360Detection    = true;
            requireLineOfSight = false;
            patrolCenter       = transform.position;
            sinOffset          = Random.Range(0f, Mathf.PI * 2f);
            base.Start();
        }

        protected override void Update()
        {
            // Manejar EMP primero (anula todo)
            if (isDisabledByEMP)
            {
                empTimer -= Time.deltaTime;
                rb.velocity = Vector2.zero;
                if (empTimer <= 0f)
                    EndEMPEffect();
                return;
            }

            base.Update();
            CheckContactDamage();
        }

        private void CheckContactDamage()
        {
            if (isDead || isStunned || isHacked || isDisabledByEMP) return;
            if (Time.time - lastContactDamageTime < contactCooldown) return;
            // No verificar daño mientras hace Retreat
            if (currentState == EnemyState.Retreat) return;

            float contactRadius = 0.6f;
            Collider2D hit = Physics2D.OverlapCircle(transform.position, contactRadius, playerLayer);
            if (hit == null) return;

            var damageable = hit.GetComponent<NABHI.Character.IDamageable>();
            if (damageable != null && damageable.IsAlive())
            {
                damageable.TakeDamage(contactDamage);
                lastContactDamageTime = Time.time;
                Debug.Log($"[EnemyDrone] Daño por contacto: {contactDamage}");

                // Iniciar Retreat tras impactar
                ChangeState(EnemyState.Retreat);
            }
        }

        #endregion

        #region COMPORTAMIENTO

        protected override void OnPatrol()
        {
            // Movimiento sinusoidal flotante omnidireccional
            float xOffset = Mathf.Sin(Time.time * floatFrequency + sinOffset) * patrolDistance;
            float yOffset = Mathf.Cos(Time.time * floatFrequency * 0.7f + sinOffset) * floatAmplitude;

            Vector2 targetPos = patrolCenter + new Vector2(xOffset, yOffset);
            Vector2 moveDir   = targetPos - (Vector2)transform.position;

            rb.velocity = moveDir.normalized * patrolSpeed;
            FlipSprite(rb.velocity.x);
        }

        protected override void OnChase()
        {
            if (playerTarget == null) return;

            Vector2 dir = DirectionToPlayer();
            rb.velocity = dir * chaseSpeed;
            FlipSprite(dir.x);
        }

        protected override void OnAttack()
        {
            // El drone ataca por contacto (CheckContactDamage en Update)
            // Este estado es transitorio; volver a Chase si hay jugador
            ChangeState(EnemyState.Chase);
        }

        protected override void OnRetreat()
        {
            // Volar en la dirección opuesta al jugador
            rb.velocity = retreatDirection * retreatSpeed;

            retreatTimer -= Time.deltaTime;
            if (retreatTimer <= 0)
            {
                // Volver a Chase si el jugador sigue visible, o a Patrol
                if (playerTarget != null && IsPlayerStillDetected())
                    ChangeState(EnemyState.Chase);
                else
                    ChangeState(EnemyState.Patrol);
            }
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        protected override void OnAnimStateChanged(EnemyState newState)
        {
            // Inicializar Retreat al entrar en ese estado
            if (newState == EnemyState.Retreat)
            {
                retreatTimer = retreatDuration;
                // Dirección de huida: opuesta al jugador (o hacia atrás si no hay target)
                retreatDirection = playerTarget != null
                    ? -DirectionToPlayer()
                    : new Vector2(-facingDirection, 0.3f).normalized;
            }

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);

            bool moving = newState == EnemyState.Patrol
                       || newState == EnemyState.Chase
                       || newState == EnemyState.Retreat;
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

        #region IEMPTarget

        public void ApplyEMPEffect(float duration)
        {
            if (isDead) return;

            isDisabledByEMP = true;
            empTimer        = duration;
            rb.velocity     = Vector2.zero;

            if (spriteRenderer != null)
                spriteRenderer.color = empDisabledColor;

            Debug.Log($"[EnemyDrone] Desactivado por EMP por {duration}s");
        }

        private void EndEMPEffect()
        {
            isDisabledByEMP = false;
            empTimer        = 0f;
            UpdateVisualColor();
            Debug.Log($"[EnemyDrone] EMP terminado, reactivado");
        }

        #endregion

        #region GIZMOS

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Rango de contacto/daño
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 0.6f);

            // Dirección de Retreat (en Play)
            if (Application.isPlaying && currentState == EnemyState.Retreat)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)retreatDirection * 2f);
            }
        }

        #endregion
    }
}
