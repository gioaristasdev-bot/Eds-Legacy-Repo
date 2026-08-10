using System.Collections;
using UnityEngine;
using NABHI.Character;

namespace NABHI.Enemies
{
    /// <summary>
    /// Boss Reina — Jefe aéreo, inmortal en la fase actual.
    ///
    /// Secuencia guionizada:
    ///   Intro → aparición con fade-in
    ///   Loop  → Attack1 (posición fija) → Cooldown → Attack2 (posición fija) → Cooldown → repite
    ///   Final → cuando el player baja del umbral de vida, interrumpe el loop y ejecuta Attack3
    ///           luego queda en FinalIdle (solo hover, sin más ataques)
    ///
    /// Animator — Triggers requeridos: "Attack1"  "Attack2"  "Attack3"
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossReina : MonoBehaviour, IDamageable
    {
        // ════════════════════════════════════════════════════════════════════
        // ENUMS
        // ════════════════════════════════════════════════════════════════════

        public enum BossState { Intro, Idle, Attack1, Attack2, Attack3, Cooldown, FinalIdle }

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
        [Tooltip("Velocidad con la que el boss sigue al jugador en X durante Idle")]
        [SerializeField] private float trackPlayerSpeed = 3f;

        #endregion

        #region Arena — Límites de la zona de pelea

        [Header("Arena — Límites de la Zona de Pelea")]
        [Tooltip("Límite izquierdo en X de la arena. El boss y las posiciones de ataque se clamparán a este valor.")]
        [SerializeField] private float arenaMinX = -10f;
        [Tooltip("Límite derecho en X de la arena.")]
        [SerializeField] private float arenaMaxX = 10f;
        [Tooltip("(Opcional) Pared izquierda — GameObject con Collider2D. Se activa al inicio de la pelea.")]
        [SerializeField] private GameObject leftWall;
        [Tooltip("(Opcional) Pared derecha — GameObject con Collider2D. Se activa al inicio de la pelea.")]
        [SerializeField] private GameObject rightWall;

        #endregion

        #region Intro

        [Header("Intro — Aparición")]
        [Tooltip("Duración del fade-in desde transparente")]
        [SerializeField] private float introDuration = 2.5f;
        [Tooltip("Segundos en Idle antes de empezar el primer ataque")]
        [SerializeField] private float idleWarmup = 1.5f;

        #endregion

        #region Secuencia guionizada

        [Header("Secuencia Guionizada")]
        [Tooltip("Segundos de cooldown/hover entre cada ataque")]
        [SerializeField] private float cooldownDuration = 2f;

        [Tooltip("Posiciones fijas en la zona de pelea donde la Reina ejecuta Attack1 (Slam). " +
                 "Se ciclan en orden: primero pos[0], luego pos[1], etc.")]
        [SerializeField] private Transform[] attack1Positions;

        [Tooltip("Posiciones fijas en la zona de pelea donde la Reina ejecuta Attack2 (Doble Agarre). " +
                 "Se ciclan en orden.")]
        [SerializeField] private Transform[] attack2Positions;

        #endregion

        #region Attack3 — Trigger por vida del player

        [Header("Attack3 — Ataque Final")]
        [Tooltip("Porcentaje de vida del player (0-1) por debajo del cual se activa el Attack3. " +
                 "Ej: 0.25 = cuando le queda 25% de vida o menos.")]
        [SerializeField] [Range(0f, 1f)] private float attack3HealthThreshold = 0.25f;

        #endregion

        #region Attack1 — Slam

        [Header("Attack1 — Slam al Suelo")]
        [Tooltip("Velocidad de desplazamiento hacia la posición del ataque")]
        [SerializeField] private float slamPositionSpeed = 6f;
        [Tooltip("Velocidad de descenso vertical")]
        [SerializeField] private float slamDescentSpeed = 18f;
        [Tooltip("Velocidad de ascenso al volver a hover")]
        [SerializeField] private float slamReturnSpeed = 9f;
        [Tooltip("Y objetivo al impactar (suelo)")]
        [SerializeField] private float slamGroundY = 0f;
        [Tooltip("Offset XY del punto de impacto relativo al pivot del boss")]
        [SerializeField] private Vector2 slamImpactOffset = Vector2.zero;
        [Tooltip("Pausa de telegrafeo antes de bajar")]
        [SerializeField] private float slamTelegraphPause = 0.35f;
        [Tooltip("Segundos desde el inicio del ataque hasta que se activa el daño corporal (para sincronizar con la animación)")]
        [SerializeField] private float slamContactDamageDelay = 2f;
        [SerializeField] private float slamDamageRadius = 2.5f;
        [SerializeField] private float slamDamage = 20f;
        [SerializeField] private GameObject slamVFXPrefab;
        [SerializeField] private LayerMask playerLayer;
        [Tooltip("(Opcional) Collider2D hijo que define el área de daño del Slam. " +
                 "Si se asigna, se usa en lugar del radio. Desactivado por defecto en el prefab.")]
        [SerializeField] private Collider2D slamHitbox;

        #endregion

        #region Attack2 — Doble Agarre

        [Header("Attack2 — Doble Agarre")]
        [Tooltip("Velocidad de desplazamiento hacia la posición del ataque")]
        [SerializeField] private float grabMoveSpeed = 12f;
        [Tooltip("Velocidad de descenso del agarre")]
        [SerializeField] private float grabDescentSpeed = 20f;
        [Tooltip("Pausa de telegrafeo antes de bajar")]
        [SerializeField] private float grabTelegraphPause = 0.4f;
        [Tooltip("Offset XY del punto de impacto relativo al pivot del boss")]
        [SerializeField] private Vector2 grabImpactOffset = Vector2.zero;
        [SerializeField] private float grabDamageRadius = 3.5f;
        [SerializeField] private float grabDamage = 25f;
        [SerializeField] private GameObject grabVFXPrefab;
        [Tooltip("(Opcional) Collider2D hijo que define el área de daño del Doble Agarre. " +
                 "Si se asigna, se usa en lugar del radio. Desactivado por defecto en el prefab.")]
        [SerializeField] private Collider2D grabHitbox;

        #endregion

        #region Attack3 — Rayos

        [Header("Attack3 — Rayos / Activación Final")]
        [SerializeField] private GameObject lightningPrefab;
        [Tooltip("Pausa de telegrafeo antes de que caigan los rayos")]
        [SerializeField] private float lightningTelegraphPause = 0.7f;
        [SerializeField] private float lightningDamage = 15f;
        [SerializeField] private float lightningDamageRadius = 1.2f;
        [Tooltip("Delay entre cada rayo consecutivo")]
        [SerializeField] private float delayBetweenStrikes = 0.3f;

        [Space]
        [Tooltip("TRUE → usa puntos fijos. FALSE → posiciones aleatorias dentro del área.")]
        [SerializeField] private bool useLightningFixedPoints = false;

        [Header("Attack3 — Puntos Fijos")]
        [SerializeField] private Transform[] lightningFixedPoints;

        [Header("Attack3 — Área Aleatoria")]
        [SerializeField] private Transform lightningAreaCenter;
        [SerializeField] private float lightningAreaWidth = 12f;
        [SerializeField] private int lightningRandomCount = 5;

        #endregion

        #region Daño Corporal

        [Header("Daño Corporal — siempre activo")]
        [Tooltip("Puntos de contacto corporal de la Reina (cabeza, cuerpo, alas…). " +
                 "Cada uno es un hijo vacío con BossContactDamage e Is Body Hitbox marcado. " +
                 "Se activan al inicio de la pelea y permanecen activos todo el tiempo.")]
        [SerializeField] private BossContactDamage[] bodyContactDamagers;

        #endregion

        #region Visual

        [Header("Visual")]
        [SerializeField] private SpriteRenderer[] spriteRenderers;
        [Tooltip("Hijo visual raíz para el flip (rotation Y)")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Animator animator;
        [Tooltip("Activar si el sprite mira a la izquierda por defecto")]
        [SerializeField] private bool spriteDefaultFacesLeft = false;
        [Tooltip("Dirección fija de la Reina durante toda la pelea. True = mira a la derecha.")]
        [SerializeField] private bool faceRight = true;

        #endregion

        #region Secuencia Muerte Ed

        [Header("Secuencia Muerte Ed")]
        [Tooltip("Script que maneja los videos de muerte. Se activa automáticamente al terminar el Attack3.")]
        [SerializeField] private DeathVideoSequence deathVideoSequence;
        [Tooltip("Segundos de pausa dramática entre el fin del Attack3 y el inicio de la secuencia de video.")]
        [SerializeField] private float deathSequenceDelay = 0.8f;

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
        private PlayerHealth playerHealth;
        private BossContactDamage[] contactDamagers;
        private int facingDirection = 1;
        private int attack1Index = 0;
        private int attack2Index = 0;
        private bool attack3Triggered  = false;
        private bool stopAttackLoop    = false;

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

            if (animator != null)
                animator.applyRootMotion = false;

            if (spriteRenderers == null || spriteRenderers.Length == 0)
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

            contactDamagers = GetComponentsInChildren<BossContactDamage>();
        }

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerTarget = playerGO.transform;
                playerHealth = playerGO.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                    playerHealth = playerGO.GetComponentInChildren<PlayerHealth>();

                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged.AddListener(OnPlayerHealthChanged);
                    Debug.Log($"[BossReina] PlayerHealth encontrado. MaxHealth={playerHealth.MaxHealth}, umbral={attack3HealthThreshold*100f}%");
                }
                else
                {
                    Debug.LogWarning("[BossReina] PlayerHealth NO encontrado — Attack3 no se activará por umbral de vida.");
                }
            }
            else
            {
                Debug.LogWarning("[BossReina] No se encontró GameObject con tag 'Player'.");
            }

            // Hitboxes corporales siempre activos desde que comienza la pelea
            if (bodyContactDamagers != null)
                foreach (var bd in bodyContactDamagers)
                    if (bd != null) bd.Activate();

            ApplyFixedFacing();
            SetAlpha(0f);
            StartCoroutine(IntroRoutine());
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged.RemoveListener(OnPlayerHealthChanged);
        }

        private void OnPlayerHealthChanged(int newHealth)
        {
            if (attack3Triggered) return;
            if (currentState == BossState.Intro) return;

            float ratio = (float)newHealth / playerHealth.MaxHealth;
            Debug.Log($"[BossReina] HP player={newHealth}/{playerHealth.MaxHealth} ({ratio*100f:F0}%) umbral={attack3HealthThreshold*100f:F0}%");
            if (ratio <= attack3HealthThreshold)
                TriggerFinalAttack();
        }

        private void TriggerFinalAttack()
        {
            if (attack3Triggered) return;
            Debug.Log("[BossReina] >>> TriggerFinalAttack — iniciando Attack3");
            attack3Triggered = true;
            stopAttackLoop   = true;
            StopAllCoroutines();
            SetContactDamage(false);
            StartCoroutine(FinalAttackRoutine());
        }

        private IEnumerator FinalAttackRoutine()
        {
            yield return StartCoroutine(Attack3Routine());

            EnterFinalIdle();

            if (deathVideoSequence != null)
            {
                yield return new WaitForSeconds(deathSequenceDelay);
                deathVideoSequence.TriggerSequence();
            }
        }

        private void Update()
        {
            if (attack3Triggered) return;
            if (currentState == BossState.Intro || currentState == BossState.FinalIdle) return;
            if (playerHealth == null) return;

            float ratio = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth;
            if (ratio <= attack3HealthThreshold)
                TriggerFinalAttack();
        }

        private void FixedUpdate()
        {
            // Clamp de posición X — corre SIEMPRE independientemente del estado
            float posX = rb.position.x;
            if (posX < arenaMinX || posX > arenaMaxX)
            {
                rb.position = new Vector2(Mathf.Clamp(posX, arenaMinX, arenaMaxX), rb.position.y);
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            if (currentState != BossState.Idle    &&
                currentState != BossState.Cooldown &&
                currentState != BossState.FinalIdle)
                return;

            float targetY = hoverHeight + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            float velY    = (targetY - rb.position.y) * 6f;

            float velX = 0f;
            if (currentState == BossState.Idle && playerTarget != null)
            {
                float dirX = playerTarget.position.x - rb.position.x;
                velX = Mathf.Clamp(dirX * 10f, -trackPlayerSpeed, trackPlayerSpeed);
                if ((rb.position.x <= arenaMinX && velX < 0f) || (rb.position.x >= arenaMaxX && velX > 0f))
                    velX = 0f;
            }

            rb.linearVelocity = new Vector2(velX, velY);
        }

        // ════════════════════════════════════════════════════════════════════
        // INTRO
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator IntroRoutine()
        {
            currentState = BossState.Intro;

            // Activar paredes de la arena al comenzar la pelea
            if (leftWall  != null) leftWall.SetActive(true);
            if (rightWall != null) rightWall.SetActive(true);

            Vector2 startPos = rb.position;
            startPos.y = hoverHeight;
            rb.position = startPos;

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

            StartCoroutine(AttackSequenceRoutine());
        }

        // ════════════════════════════════════════════════════════════════════
        // SECUENCIA GUIONIZADA
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator AttackSequenceRoutine()
        {
            while (!stopAttackLoop)
            {
                // ── Attack 1 en posición fija ─────────────────────────────
                Vector2 pos1 = GetAttack1Position();
                yield return StartCoroutine(Attack1Routine(pos1));
                if (stopAttackLoop) break;

                currentState = BossState.Cooldown;
                rb.linearVelocity  = Vector2.zero;
                yield return new WaitForSeconds(cooldownDuration);
                if (stopAttackLoop) break;
                currentState = BossState.Idle;

                // ── Attack 2 en posición fija ─────────────────────────────
                Vector2 pos2 = GetAttack2Position();
                yield return StartCoroutine(Attack2Routine(pos2));
                if (stopAttackLoop) break;

                currentState = BossState.Cooldown;
                rb.linearVelocity  = Vector2.zero;
                yield return new WaitForSeconds(cooldownDuration);
                if (stopAttackLoop) break;
                currentState = BossState.Idle;
            }
        }

        private void EnterFinalIdle()
        {
            attack3Triggered = true;
            currentState     = BossState.FinalIdle;
            rb.linearVelocity      = Vector2.zero;
        }

        // ════════════════════════════════════════════════════════════════════
        // POSICIONES DE ATAQUE — se ciclan en orden
        // ════════════════════════════════════════════════════════════════════

        private Vector2 GetAttack1Position()
        {
            if (attack1Positions != null && attack1Positions.Length > 0)
            {
                var t = attack1Positions[attack1Index % attack1Positions.Length];
                attack1Index++;
                if (t != null) return t.position;
            }
            // Fallback: posición actual del player
            return playerTarget != null
                ? new Vector2(playerTarget.position.x, hoverHeight)
                : new Vector2(rb.position.x, hoverHeight);
        }

        private Vector2 GetAttack2Position()
        {
            if (attack2Positions != null && attack2Positions.Length > 0)
            {
                var t = attack2Positions[attack2Index % attack2Positions.Length];
                attack2Index++;
                if (t != null) return t.position;
            }
            // Fallback: X=0 (centro del arena)
            return new Vector2(0f, hoverHeight);
        }

        // ════════════════════════════════════════════════════════════════════
        // ATTACK1 — SLAM AL SUELO
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator Attack1Routine(Vector2 attackPos)
        {
            currentState = BossState.Attack1;
            rb.linearVelocity = Vector2.zero;

            // 1. Moverse a la posición X fija del ataque (sin animación todavía)
            yield return StartCoroutine(MoveToX(attackPos.x, slamPositionSpeed));

            // 2. Telegrafeo
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(slamTelegraphPause);

            // 3. Trigger de animación aquí: el boss ya está en posición, empieza la animación del slam
            //    slamContactDamageDelay debe coincidir con los frames hasta el impacto visual en la animación
            animator?.ResetTrigger(AnimParam.Attack1);
            animator?.SetTrigger(AnimParam.Attack1);

            float lockedX = rb.position.x;
            if (slamContactDamageDelay > 0f)
                yield return new WaitForSeconds(slamContactDamageDelay);
            SetContactDamage(true);

            // 4. Descenso al suelo
            yield return StartCoroutine(DescendToY(slamGroundY, slamDescentSpeed, lockedX));

            // 5. Impacto
            rb.linearVelocity = Vector2.zero;
            ApplyImpact((Vector2)transform.position + slamImpactOffset, slamDamageRadius, slamDamage, slamVFXPrefab, slamHitbox);
            yield return new WaitForSeconds(0.4f);

            // 6. Retorno a hover — desactiva daño corporal
            SetContactDamage(false);
            yield return StartCoroutine(AscendToY(hoverHeight, slamReturnSpeed, lockedX));
        }

        // ════════════════════════════════════════════════════════════════════
        // ATTACK2 — DOBLE AGARRE
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator Attack2Routine(Vector2 attackPos)
        {
            currentState = BossState.Attack2;
            animator?.SetTrigger(AnimParam.Attack2);
            rb.linearVelocity = Vector2.zero;

            // 1. Moverse a la posición X fija del ataque
            yield return StartCoroutine(MoveToX(attackPos.x, grabMoveSpeed));

            // 2. Telegrafeo
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(grabTelegraphPause);

            // 3. Descenso al suelo — activa daño corporal
            float lockedX = rb.position.x;
            SetContactDamage(true);
            yield return StartCoroutine(DescendToY(slamGroundY, grabDescentSpeed, lockedX));

            // 4. Impacto
            rb.linearVelocity = Vector2.zero;
            ApplyImpact((Vector2)transform.position + grabImpactOffset, grabDamageRadius, grabDamage, grabVFXPrefab, grabHitbox);
            yield return new WaitForSeconds(0.5f);

            // 5. Retorno a hover — desactiva daño corporal
            SetContactDamage(false);
            yield return StartCoroutine(AscendToY(hoverHeight, slamReturnSpeed, lockedX));
        }

        // ════════════════════════════════════════════════════════════════════
        // ATTACK3 — RAYOS / ATAQUE FINAL
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator Attack3Routine()
        {
            currentState = BossState.Attack3;
            animator?.SetTrigger(AnimParam.Attack3);
            rb.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(lightningTelegraphPause);

            if (useLightningFixedPoints && lightningFixedPoints != null && lightningFixedPoints.Length > 0)
            {
                foreach (Transform point in lightningFixedPoints)
                {
                    if (point != null) SpawnLightningAt(point.position);
                    yield return new WaitForSeconds(delayBetweenStrikes);
                }
            }
            else
            {
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

            if (playerTarget == null) return;
            if (Vector2.Distance(playerTarget.position, position) > lightningDamageRadius) return;

            var damageable = playerTarget.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
                damageable.TakeDamage(lightningDamage);
        }

        // ════════════════════════════════════════════════════════════════════
        // UTILIDADES DE MOVIMIENTO
        // ════════════════════════════════════════════════════════════════════

        private IEnumerator MoveToX(float targetX, float speed)
        {
            targetX = Mathf.Clamp(targetX, arenaMinX, arenaMaxX);
            while (Mathf.Abs(rb.position.x - targetX) > 0.15f)
            {
                float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
                newX = Mathf.Clamp(newX, arenaMinX, arenaMaxX);
                rb.MovePosition(new Vector2(newX, rb.position.y));
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(new Vector2(targetX, rb.position.y));
        }

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

        private void ApplyImpact(Vector2 position, float radius, float damage, GameObject vfxPrefab,
                                  Collider2D hitbox = null)
        {
            if (vfxPrefab != null)
                Instantiate(vfxPrefab, position, Quaternion.identity);

            if (playerTarget == null) return;

            bool inRange;
            if (hitbox != null)
            {
                // Usar los bounds del hitbox configurado en el editor
                Bounds b = hitbox.bounds;
                inRange  = b.Contains(playerTarget.position);
            }
            else
            {
                inRange = Vector2.Distance(playerTarget.position, position) <= radius;
            }

            if (!inRange) return;

            var damageable = playerTarget.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive())
                damageable.TakeDamage(damage);
        }

        // ════════════════════════════════════════════════════════════════════
        // VISUAL
        // ════════════════════════════════════════════════════════════════════

        private void SetContactDamage(bool active)
        {
            if (contactDamagers == null) return;
            foreach (var cd in contactDamagers)
            {
                if (cd.IsBodyHitbox) continue; // los corporales siempre activos, nunca se tocan
                if (active) cd.Activate();
                else        cd.Deactivate();
            }
        }

        private void ApplyFixedFacing()
        {
            if (visualRoot == null) return;
            int dir       = faceRight ? 1 : -1;
            int visualDir = spriteDefaultFacesLeft ? -dir : dir;
            Vector3 s = visualRoot.localScale;
            s.x = Mathf.Abs(s.x) * visualDir;
            visualRoot.localScale = s;
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
        // IDAMAGEABLE — INMORTAL
        // ════════════════════════════════════════════════════════════════════

        public void TakeDamage(float damage) { }
        public bool IsAlive() => true;
        public void Heal(float amount) { }

        // ════════════════════════════════════════════════════════════════════
        // PROPIEDADES
        // ════════════════════════════════════════════════════════════════════

        public BossState CurrentState => currentState;

        // ════════════════════════════════════════════════════════════════════
        // GIZMOS
        // ════════════════════════════════════════════════════════════════════

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // Límites de la arena
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
            Gizmos.DrawLine(new Vector3(arenaMinX, -2f), new Vector3(arenaMinX, hoverHeight + 2f));
            Gizmos.DrawLine(new Vector3(arenaMaxX, -2f), new Vector3(arenaMaxX, hoverHeight + 2f));

            // Línea de hover height
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            Gizmos.DrawLine(
                new Vector3(arenaMinX, hoverHeight),
                new Vector3(arenaMaxX, hoverHeight));

            // Posiciones de Attack1
            if (attack1Positions != null)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
                foreach (var p in attack1Positions)
                {
                    if (p == null) continue;
                    Gizmos.DrawWireSphere(new Vector3(p.position.x, slamGroundY + slamImpactOffset.y), slamDamageRadius);
                    Gizmos.DrawLine(new Vector3(p.position.x, hoverHeight), new Vector3(p.position.x, slamGroundY));
                }
            }

            // Posiciones de Attack2
            if (attack2Positions != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
                foreach (var p in attack2Positions)
                {
                    if (p == null) continue;
                    Gizmos.DrawWireSphere(new Vector3(p.position.x, slamGroundY + grabImpactOffset.y), grabDamageRadius);
                    Gizmos.DrawLine(new Vector3(p.position.x, hoverHeight), new Vector3(p.position.x, slamGroundY));
                }
            }

            // Puntos de rayos Attack3
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
                Vector3 lc = lightningAreaCenter != null
                    ? lightningAreaCenter.position
                    : new Vector3(transform.position.x, slamGroundY);
                Gizmos.color = new Color(1f, 1f, 0.2f, 0.3f);
                Gizmos.DrawWireCube(lc, new Vector3(lightningAreaWidth, 0.5f, 0));
            }
        }
    }
}
