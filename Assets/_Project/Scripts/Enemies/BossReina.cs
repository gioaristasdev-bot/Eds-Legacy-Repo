using System.Collections;
using UnityEngine;
using NABHI.Character;

namespace NABHI.Enemies
{
    /// <summary>
    /// Boss Reina — Jefe aéreo, inmortal en la fase actual.
    ///
    /// Estados:
    ///   Intro    → aparición progresiva desde negro
    ///   Idle     → hover sinusoidal siguiendo al jugador en X
    ///   Attack1  → Slam: desciende sobre el jugador y golpea el suelo
    ///   Attack2  → Doble agarre: va al centro y golpea con ambos brazos
    ///   Attack3  → Activación: invoca rayos en puntos fijos o aleatorios
    ///   Cooldown → recuperación entre ataques (hover pero sin tracking)
    ///
    /// Animator — crear estos Triggers en el Animator Controller:
    ///   "Attack1"  "Attack2"  "Attack3"
    ///
    /// Nota: IDamageable está implementado como no-op (inmortal).
    /// Para activar daño real en el futuro, eliminar los cuerpos vacíos.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossReina : MonoBehaviour, IDamageable
    {
        // ════════════════════════════════════════════════════════════════════
        // ENUMS Y CONSTANTES
        // ════════════════════════════════════════════════════════════════════

        public enum BossState { Intro, Idle, Attack1, Attack2, Attack3, Cooldown }

        public static class AnimParam
        {
            public const string Attack1 = "Attack1";
            public const string Attack2 = "Attack2";
            public const string Attack3 = "Attack3";
        }

        // ════════════════════════════════════════════════════════════════════
        // CONFIGURACIÓN
        // ════════════════════════════════════════════════════════════════════

        #region Hover

        [Header("Hover")]
        [Tooltip("Altura Y base del boss mientras flota")]
        [SerializeField] private float hoverHeight = 5f;
        [Tooltip("Amplitud de la oscilación vertical")]
        [SerializeField] private float hoverAmplitude = 0.4f;
        [Tooltip("Frecuencia de la oscilación vertical")]
        [SerializeField] private float hoverFrequency = 1.2f;
        [Tooltip("Velocidad máxima con la que sigue al jugador en X durante Idle")]
        [SerializeField] private float trackPlayerSpeed = 3f;

        #endregion

        #region Intro

        [Header("Intro — Aparición")]
        [Tooltip("Duración del fade-in desde transparente")]
        [SerializeField] private float introDuration = 2.5f;
        [Tooltip("Segundos en Idle antes de empezar el primer ataque")]
        [SerializeField] private float idleWarmup = 1.5f;

        #endregion

        #region Ciclo de ataques

        [Header("Ciclo de Ataques")]
        [Tooltip("Si true, elige el orden de ataque al azar. Si false, cicla 1→2→3→1…")]
        [SerializeField] private bool randomAttackOrder = false;
        [Tooltip("Segundos de cooldown/hover entre cada ataque")]
        [SerializeField] private float cooldownDuration = 2f;

        #endregion

        #region Attack1 — Slam

        [Header("Attack1 — Slam al Suelo")]
        [Tooltip("Velocidad para posicionarse sobre el jugador en X")]
        [SerializeField] private float slamPositionSpeed = 6f;
        [Tooltip("Velocidad de descenso vertical")]
        [SerializeField] private float slamDescentSpeed = 18f;
        [Tooltip("Velocidad de ascenso al volver a hover")]
        [SerializeField] private float slamReturnSpeed = 9f;
        [Tooltip("Y objetivo al impactar (suelo)")]
        [SerializeField] private float slamGroundY = 0f;
        [Tooltip("Offset XY del punto de impacto/daño relativo al pivot del boss. Útil si el sprite no está centrado en el pivot.")]
        [SerializeField] private Vector2 slamImpactOffset = Vector2.zero;
        [Tooltip("Pausa de telegrafeo antes de bajar")]
        [SerializeField] private float slamTelegraphPause = 0.35f;
        [Tooltip("Radio del daño de impacto")]
        [SerializeField] private float slamDamageRadius = 2.5f;
        [SerializeField] private float slamDamage = 20f;
        [SerializeField] private GameObject slamVFXPrefab;
        [SerializeField] private LayerMask playerLayer;

        #endregion

        #region Attack2 — Doble Agarre

        [Header("Attack2 — Doble Agarre al Centro")]
        [Tooltip("Transform en el centro del arena. Si es null, usa X=0")]
        [SerializeField] private Transform arenaCenter;
        [Tooltip("Velocidad de desplazamiento horizontal al centro")]
        [SerializeField] private float grabMoveSpeed = 12f;
        [Tooltip("Velocidad de descenso del agarre")]
        [SerializeField] private float grabDescentSpeed = 20f;
        [Tooltip("Pausa de telegrafeo antes de bajar")]
        [SerializeField] private float grabTelegraphPause = 0.4f;
        [Tooltip("Offset XY del punto de impacto/daño relativo al pivot del boss.")]
        [SerializeField] private Vector2 grabImpactOffset = Vector2.zero;
        [Tooltip("Radio del daño del doble agarre (mayor que el slam)")]
        [SerializeField] private float grabDamageRadius = 3.5f;
        [SerializeField] private float grabDamage = 25f;
        [SerializeField] private GameObject grabVFXPrefab;

        #endregion

        #region Attack3 — Rayos

        [Header("Attack3 — Rayos / Activación")]
        [SerializeField] private GameObject lightningPrefab;
        [Tooltip("Pausa de activación antes de que caigan los rayos")]
        [SerializeField] private float lightningTelegraphPause = 0.7f;
        [SerializeField] private float lightningDamage = 15f;
        [SerializeField] private float lightningDamageRadius = 1.2f;
        [Tooltip("Delay entre cada rayo consecutivo")]
        [SerializeField] private float delayBetweenStrikes = 0.3f;

        [Space]
        [Tooltip("TRUE → usa puntos fijos (lightningFixedPoints). FALSE → posiciones aleatorias dentro del área.")]
        [SerializeField] private bool useLightningFixedPoints = false;

        [Header("Attack3 — Puntos Fijos")]
        [Tooltip("Transforms de la escena donde caen los rayos en modo fijo")]
        [SerializeField] private Transform[] lightningFixedPoints;

        [Header("Attack3 — Área Aleatoria")]
        [Tooltip("Centro del área de rayos aleatorios. Si null usa posición del boss")]
        [SerializeField] private Transform lightningAreaCenter;
        [Tooltip("Ancho total del área aleatoria")]
        [SerializeField] private float lightningAreaWidth = 12f;
        [Tooltip("Número de rayos aleatorios")]
        [SerializeField] private int lightningRandomCount = 5;

        #endregion

        #region Visual

        [Header("Visual")]
        [Tooltip("Todos los SpriteRenderers del boss (para el fade-in)")]
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [Tooltip("Hijo visual raíz para el flip (rotation Y)")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [Tooltip("Activar si el sprite mira a la izquierda por defecto")]
        [SerializeField] private bool spriteDefaultFacesLeft = false;

        #endregion

        #region Debug

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        #endregion

        // ════════════════════════════════════════════════════════════════════
        // ESTADO INTERNO
        // ════════════════════════════════════════════════════════════════════

        private BossState currentState;
        private Rigidbody2D rb;
        private Transform playerTarget;
        private int facingDirection = 1;
        private int nextAttackIndex = 0;

        // ════════════════════════════════════════════════════════════════════
        // UNITY
        // ════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            // Desactivar root motion para que las animaciones no muevan el transform
            if (animator != null)
                animator.applyRootMotion = false;

            if (spriteRenderers == null || spriteRenderers.Length == 0)
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                playerTarget = playerGO.transform;

            SetAlpha(0f);
            StartCoroutine(IntroRoutine());
        }

        private void Update()
        {
            if (currentState == BossState.Idle || currentState == BossState.Cooldown)
                FlipTowardPlayer();
        }

        private void FixedUpdate()
        {
            if (currentState != BossState.Idle && currentState != BossState.Cooldown)
                return;

            float targetY = hoverHeight + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            float velY    = (targetY - rb.position.y) * 6f;

            float velX = 0f;
            if (currentState == BossState.Idle && playerTarget != null)
            {
                float dirX = playerTarget.position.x - rb.position.x;
                // Gain alto: cierra distancia rápido, se frena al acercarse
                velX = Mathf.Clamp(dirX * 10f, -trackPlayerSpeed, trackPlayerSpeed);
            }

            rb.velocity = new Vector2(velX, velY);
        }

        // ════════════════════════════════════════════════════════════════════
        // INTRO — APARICIÓN PROGRESIVA
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator IntroRoutine()
        {
            currentState = BossState.Intro;

            // Fijar solo la Y a hover height; X viene de la posición en escena
            Vector2 startPos = rb.position;
            startPos.y = hoverHeight;
            rb.position = startPos;

            // Fade-in
            float elapsed = 0f;
            while (elapsed < introDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Clamp01(elapsed / introDuration));
                yield return null;
            }
            SetAlpha(1f);

            currentState = BossState.Idle;
            yield return new WaitForSeconds(idleWarmup);

            StartCoroutine(AttackCycleRoutine());
        }

        private void SetAlpha(float alpha)
        {
            foreach (var sr in spriteRenderers)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // HOVER Y TRACKING
        // ════════════════════════════════════════════════════════════════════

        private void FlipTowardPlayer()
        {
            if (playerTarget == null) return;
            float dirX = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dirX) < 0.1f) return;

            facingDirection = dirX > 0f ? 1 : -1;
            int visualDir   = spriteDefaultFacesLeft ? -facingDirection : facingDirection;

            if (visualRoot != null)
            {
                Vector3 s = visualRoot.localScale;
                s.x = Mathf.Abs(s.x) * visualDir;
                visualRoot.localScale = s;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // CICLO DE ATAQUES
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator AttackCycleRoutine()
        {
            int[] order = { 1, 2, 3 };

            while (true)
            {
                // Respiro mínimo en Idle antes de atacar
                yield return new WaitForSeconds(0.5f);

                int attack = randomAttackOrder
                    ? Random.Range(1, 4)
                    : order[nextAttackIndex % 3];

                nextAttackIndex++;

                switch (attack)
                {
                    case 1: yield return StartCoroutine(Attack1Routine()); break;
                    case 2: yield return StartCoroutine(Attack2Routine()); break;
                    case 3: yield return StartCoroutine(Attack3Routine()); break;
                }

                // Cooldown: hover pero sin tracking
                currentState = BossState.Cooldown;
                rb.velocity  = Vector2.zero;
                yield return new WaitForSeconds(cooldownDuration);
                currentState = BossState.Idle;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // ATTACK1 — SLAM AL SUELO
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator Attack1Routine()
        {
            currentState = BossState.Attack1;
            animator?.SetTrigger(AnimParam.Attack1);

            rb.velocity = Vector2.zero;

            // 1. Seguir al player en X en tiempo real hasta centrarse sobre él
            yield return StartCoroutine(MoveToPlayerX(slamPositionSpeed));

            // 2. Telegrafeo — pausa breve antes de bajar
            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(slamTelegraphPause);

            // 3. Descenso rápido al suelo (X bloqueada)
            float lockedX = rb.position.x;
            yield return StartCoroutine(DescendToY(slamGroundY, slamDescentSpeed, lockedX));

            // 4. Impacto
            rb.velocity = Vector2.zero;
            ApplyImpact((Vector2)transform.position + slamImpactOffset, slamDamageRadius, slamDamage, slamVFXPrefab);
            yield return new WaitForSeconds(0.4f);

            // 5. Retorno a hover
            yield return StartCoroutine(AscendToY(hoverHeight, slamReturnSpeed, lockedX));
        }

        // ════════════════════════════════════════════════════════════════════
        // ATTACK2 — DOBLE AGARRE AL CENTRO
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator Attack2Routine()
        {
            currentState = BossState.Attack2;
            animator?.SetTrigger(AnimParam.Attack2);

            rb.velocity = Vector2.zero;

            // 1. Desplazarse al centro del arena
            float centerX = arenaCenter != null ? arenaCenter.position.x : 0f;
            yield return StartCoroutine(MoveToX(centerX, grabMoveSpeed));

            // 2. Telegrafeo
            rb.velocity = Vector2.zero;
            yield return new WaitForSeconds(grabTelegraphPause);

            // 3. Descenso al suelo desde el centro
            yield return StartCoroutine(DescendToY(slamGroundY, grabDescentSpeed, centerX));

            // 4. Impacto de doble agarre (radio mayor)
            rb.velocity = Vector2.zero;
            ApplyImpact((Vector2)transform.position + grabImpactOffset, grabDamageRadius, grabDamage, grabVFXPrefab);
            yield return new WaitForSeconds(0.5f);

            // 5. Retorno a hover
            yield return StartCoroutine(AscendToY(hoverHeight, slamReturnSpeed, centerX));
        }

        // ════════════════════════════════════════════════════════════════════
        // ATTACK3 — RAYOS / ACTIVACIÓN
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator Attack3Routine()
        {
            currentState = BossState.Attack3;
            animator?.SetTrigger(AnimParam.Attack3);
            rb.velocity = Vector2.zero;

            // Esperar la animación de activación (telegrafeo)
            yield return new WaitForSeconds(lightningTelegraphPause);

            if (useLightningFixedPoints
                && lightningFixedPoints != null
                && lightningFixedPoints.Length > 0)
            {
                // Modo fijo: rayos en los puntos asignados en el Inspector
                foreach (Transform point in lightningFixedPoints)
                {
                    if (point != null)
                        SpawnLightningAt(point.position);
                    yield return new WaitForSeconds(delayBetweenStrikes);
                }
            }
            else
            {
                // Modo aleatorio: rayos distribuidos dentro del área configurada
                Vector2 areaCenter = lightningAreaCenter != null
                    ? (Vector2)lightningAreaCenter.position
                    : new Vector2(transform.position.x, slamGroundY);

                for (int i = 0; i < lightningRandomCount; i++)
                {
                    float rx = areaCenter.x + Random.Range(-lightningAreaWidth / 2f, lightningAreaWidth / 2f);
                    SpawnLightningAt(new Vector2(rx, areaCenter.y));
                    yield return new WaitForSeconds(delayBetweenStrikes);
                }
            }

            yield return new WaitForSeconds(0.3f);
        }

        private void SpawnLightningAt(Vector2 position)
        {
            if (lightningPrefab != null)
                Instantiate(lightningPrefab, position, Quaternion.identity);

            Collider2D hit = Physics2D.OverlapCircle(position, lightningDamageRadius, playerLayer);
            if (hit != null)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                    damageable.TakeDamage(lightningDamage);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // UTILIDADES DE MOVIMIENTO
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Sigue al player en X en tiempo real hasta quedar centrado sobre él.</summary>
        private IEnumerator MoveToPlayerX(float speed)
        {
            if (playerTarget == null) yield break;

            while (Mathf.Abs(rb.position.x - playerTarget.position.x) > 0.15f)
            {
                float targetX = playerTarget.position.x;
                float newX    = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(newX, rb.position.y));
                FlipTowardPlayer();
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(new Vector2(playerTarget.position.x, rb.position.y));
        }

        /// <summary>Mueve el boss al targetX manteniendo Y actual. Solo eje X.</summary>
        private IEnumerator MoveToX(float targetX, float speed)
        {
            while (Mathf.Abs(rb.position.x - targetX) > 0.15f)
            {
                float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(newX, rb.position.y));
                FlipTowardPlayer();
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(new Vector2(targetX, rb.position.y));
        }

        /// <summary>Desciende verticalmente a targetY manteniendo X bloqueada.</summary>
        private IEnumerator DescendToY(float targetY, float speed, float lockedX)
        {
            while (rb.position.y > targetY + 0.05f)
            {
                float newY = Mathf.MoveTowards(rb.position.y, targetY, speed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(lockedX, newY));
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(new Vector2(lockedX, targetY));
        }

        /// <summary>Asciende verticalmente a targetY manteniendo X bloqueada.</summary>
        private IEnumerator AscendToY(float targetY, float speed, float lockedX)
        {
            while (rb.position.y < targetY - 0.1f)
            {
                float newY = Mathf.MoveTowards(rb.position.y, targetY, speed * Time.fixedDeltaTime);
                rb.MovePosition(new Vector2(lockedX, newY));
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(new Vector2(lockedX, targetY));
        }

        /// <summary>Instancia VFX y aplica daño en área.</summary>
        private void ApplyImpact(Vector2 position, float radius, float damage, GameObject vfxPrefab)
        {
            if (vfxPrefab != null)
                Instantiate(vfxPrefab, position, Quaternion.identity);

            Collider2D hit = Physics2D.OverlapCircle(position, radius, playerLayer);
            if (hit != null)
            {
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                    damageable.TakeDamage(damage);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // IDAMAGEABLE — INMORTAL
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Inmortal: no recibe daño. Quitar los cuerpos vacíos cuando se active el daño real.</summary>
        public void TakeDamage(float damage) { }
        public bool IsAlive() => true;
        public void Heal(float amount) { }

        // ════════════════════════════════════════════════════════════════════
        // PROPIEDADES PÚBLICAS
        // ════════════════════════════════════════════════════════════════════

        public BossState CurrentState => currentState;

        // ════════════════════════════════════════════════════════════════════
        // GIZMOS
        // ════════════════════════════════════════════════════════════════════

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // Línea de hover height
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            Gizmos.DrawLine(
                new Vector3(transform.position.x - 8f, hoverHeight),
                new Vector3(transform.position.x + 8f, hoverHeight)
            );

            // Attack1 — radio de slam (con offset aplicado)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(
                new Vector3(transform.position.x + slamImpactOffset.x, slamGroundY + slamImpactOffset.y),
                slamDamageRadius);

            // Attack2 — radio de agarre en el centro (con offset aplicado)
            float cx = arenaCenter != null ? arenaCenter.position.x : 0f;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(
                new Vector3(cx + grabImpactOffset.x, slamGroundY + grabImpactOffset.y),
                grabDamageRadius);

            // Attack3 — puntos fijos de rayos
            if (useLightningFixedPoints && lightningFixedPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var p in lightningFixedPoints)
                {
                    if (p != null)
                        Gizmos.DrawWireSphere(p.position, lightningDamageRadius);
                }
            }
            else
            {
                // Attack3 — área aleatoria de rayos
                Vector3 lc = lightningAreaCenter != null
                    ? lightningAreaCenter.position
                    : new Vector3(transform.position.x, slamGroundY);
                Gizmos.color = new Color(1f, 1f, 0.2f, 0.3f);
                Gizmos.DrawWireCube(lc, new Vector3(lightningAreaWidth, 0.5f, 0));
            }
        }
    }
}
