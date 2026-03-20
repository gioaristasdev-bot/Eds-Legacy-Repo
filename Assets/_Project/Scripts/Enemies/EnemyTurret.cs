using UnityEngine;

namespace NABHI.Enemies
{
    /// <summary>
    /// TORRETA — Enemigo estático con rotación limitada.
    ///
    /// Estados (Documento Técnico): Idle → Attack → Cooldown → Hit → Destroyed
    /// No se mueve. Detecta con radio + FOV, rota pivote hacia el jugador.
    /// Ciclo: dispara X tiros → Cooldown → vuelve a disparar.
    ///
    /// Chakras:
    ///   IHackable  → desactiva la torreta (base EnemyBase)
    ///   IStunnable → interrumpe el disparo (base EnemyBase, NO inmune per doc)
    ///   Invisibilidad → ignora al jugador (base EnemyBase, layer InvisiblePlayer)
    ///
    /// NOTA: La inmunidad a stun anterior fue ELIMINADA según el Documento Técnico.
    /// El Temblor ahora interrumpe el disparo de la torreta.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyTurret : EnemyBase
    {
        #region CONFIGURACIÓN TURRET

        [Header("Turret - Disparo")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.8f;
        [SerializeField] private float projectileDamage = 12f;
        [SerializeField] private float projectileSpeed = 14f;

        [Header("Turret - Ráfaga y Cooldown")]
        [Tooltip("Disparos consecutivos antes de entrar en Cooldown")]
        [SerializeField] private int shotsPerBurst = 3;
        [Tooltip("Duración del Cooldown entre ráfagas")]
        [SerializeField] private float cooldownDuration = 2f;

        [Header("Turret - Rotación")]
        [SerializeField] private Transform turretPivot;
        [SerializeField] private float rotationSpeed = 180f;
        [SerializeField] private float maxRotationAngle = 90f;

        [Header("Turret - Comportamiento")]
        [SerializeField] private float returnToIdleDelay = 2f;

        #endregion

        #region ESTADO

        private float lastFireTime;
        private float baseRotation;
        private int shotsFired;
        private float cooldownTimer;

        #endregion

        #region UNITY CALLBACKS

        protected override void Awake()
        {
            base.Awake();
            if (turretPivot == null)
                turretPivot = transform;
        }

        protected override void Start()
        {
            baseRotation = turretPivot.eulerAngles.z;
            base.Start();
            // La torreta empieza en Idle, no patrulla
            ChangeState(EnemyState.Idle);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        // ApplyStun NO se override aquí → la torreta ahora SÍ puede
        // ser stunneada (según Documento Técnico: "Temblor interrumpe disparo")
        // ─────────────────────────────────────────────────────────────

        #region MÁQUINA DE ESTADOS (override)

        protected override void UpdateState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    ScanForPlayer();
                    if (playerTarget != null)
                        ChangeState(EnemyState.Attack);
                    else
                        ReturnToBaseRotation();
                    break;

                case EnemyState.Attack:
                    OnAttack();
                    break;

                case EnemyState.Cooldown:
                    OnCooldown();
                    break;

                default:
                    // Stunned/Hacked/Hit son manejados en EnemyBase.Update antes
                    // de llamar a UpdateState, así que nunca llegaremos aquí con
                    // esos estados. Cualquier otro estado inesperado va a Idle.
                    ChangeState(EnemyState.Idle);
                    break;
            }
        }

        #endregion

        #region COMPORTAMIENTO

        protected override void OnPatrol()
        {
            // La torreta no patrulla → Idle
            ChangeState(EnemyState.Idle);
        }

        protected override void OnChase()
        {
            // La torreta no persigue → Attack directo
            if (playerTarget != null)
                ChangeState(EnemyState.Attack);
            else
                ChangeState(EnemyState.Idle);
        }

        protected override void OnAttack()
        {
            if (playerTarget == null || !IsPlayerStillDetected())
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            RotateTowardsPlayer();

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
            // Mantener rotación, esperar a disparar de nuevo
            if (playerTarget != null && IsPlayerStillDetected())
                RotateTowardsPlayer();
            else
                ReturnToBaseRotation();

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                if (playerTarget != null && IsPlayerStillDetected())
                    ChangeState(EnemyState.Attack);
                else
                    ChangeState(EnemyState.Idle);
            }
        }

        #endregion

        #region HOOKS DE ANIMACIÓN

        protected override void OnAnimStateChanged(EnemyState newState)
        {
            // Inicializar timer de Cooldown al entrar en ese estado
            if (newState == EnemyState.Cooldown)
                cooldownTimer = cooldownDuration;

            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            if (animator == null) return;

            animator.SetInteger(AnimParam.State, (int)newState);
            // La torreta no se mueve, IsMoving siempre false
            animator.SetBool(AnimParam.IsMoving, false);

            if (newState == EnemyState.Hit)  animator.SetTrigger(AnimParam.Hit);
            if (newState == EnemyState.Dead) animator.SetBool(AnimParam.IsDead, true);

            // Para el pivote/cañón: usar el Animator del child turretPivot si tiene uno propio.
            // Ej: animator del pivote para animación de disparo (flash de boca de cañón).
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
            /* ANIMACIÓN (descomentar cuando el artista entregue sprites):
            animator?.SetBool(AnimParam.IsDead, true);
            // Instantiate(deathVFX, transform.position, Quaternion.identity);
            */
        }

        #endregion

        #region ROTACIÓN

        private void RotateTowardsPlayer()
        {
            if (playerTarget == null) return;

            Vector2 dir = DirectionToPlayer();
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            float angleDiff = Mathf.DeltaAngle(baseRotation, targetAngle);
            angleDiff   = Mathf.Clamp(angleDiff, -maxRotationAngle, maxRotationAngle);
            targetAngle = baseRotation + angleDiff;

            float currentAngle = turretPivot.eulerAngles.z;
            float newAngle     = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            turretPivot.rotation = Quaternion.Euler(0, 0, newAngle);

            FlipSprite(dir.x);
        }

        private void ReturnToBaseRotation()
        {
            float currentAngle = turretPivot.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, baseRotation, rotationSpeed * 0.5f * Time.deltaTime);
            turretPivot.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        #endregion

        #region DISPARO

        private void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[EnemyTurret] {gameObject.name}: No tiene projectilePrefab asignado");
                return;
            }

            Vector2 spawnPos = firePoint != null ? firePoint.position : (Vector2)turretPivot.position;
            Vector2 dir      = turretPivot.right;

            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            EnemyProjectile proj = projObj.GetComponent<EnemyProjectile>();
            if (proj != null)
            {
                proj.SetDamage(projectileDamage);
                proj.Initialize(dir, projectileSpeed);
            }

            Debug.Log($"[EnemyTurret] Disparo proyectil ({shotsFired + 1}/{shotsPerBurst})");
        }

        #endregion

        #region GIZMOS

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            Transform fp = firePoint != null ? firePoint : (turretPivot != null ? turretPivot : transform);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(fp.position, 0.15f);

            if (turretPivot != null)
            {
                float baseRot = Application.isPlaying ? baseRotation : turretPivot.eulerAngles.z;
                Vector3 leftLimit  = Quaternion.Euler(0, 0, baseRot + maxRotationAngle) * Vector3.right * 3f;
                Vector3 rightLimit = Quaternion.Euler(0, 0, baseRot - maxRotationAngle) * Vector3.right * 3f;

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawLine(turretPivot.position, turretPivot.position + leftLimit);
                Gizmos.DrawLine(turretPivot.position, turretPivot.position + rightLimit);
            }
        }

        #endregion
    }
}
