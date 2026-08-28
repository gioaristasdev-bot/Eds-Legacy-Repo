using UnityEngine;
using NABHI.Character;

namespace NABHI.Weapons
{
    /// <summary>
    /// Controlador de arma del jugador
    /// Maneja disparo, cadencia, munición, y efectos visuales
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        #region REFERENCIAS

        [Header("Referencias")]
        [Tooltip("Prefab del proyectil a disparar")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("Punto desde donde salen las balas (Fire Point)")]
        [SerializeField] private Transform firePoint;

        [Tooltip("Gatillo derecho (RT / R2). Debe coincidir con el del WeaponStateManager.")]
        [SerializeField] private string fireTriggerAxis = "FireTrigger";

        [Range(0.05f, 0.95f)]
        [SerializeField] private float fireTriggerThreshold = 0.3f;

        [Tooltip("Referencia al AimController (asignado automáticamente)")]
        private AimController aimController;
        private NABHI.Character.CharacterController2D characterController;

        [Tooltip("WeaponStateManager del Player (asignado automáticamente)")]
        private WeaponStateManager weaponStateManager;

        private PlayerSFX sfx;

        #endregion

        #region PARÁMETROS DE DISPARO

        [Header("Configuración de Disparo")]
        [Tooltip("Cadencia de disparo (disparos por segundo)")]
        [SerializeField] private float fireRate = 5f;

        [Tooltip("Velocidad de los proyectiles")]
        [SerializeField] private float projectileSpeed = 20f;

        [Tooltip("Daño por proyectil")]
        [SerializeField] private float projectileDamage = 10f;

        [Tooltip("Los proyectiles atraviesan enemigos")]
        [SerializeField] private bool piercingShots = false;

        [Tooltip("Empuje vertical, en unidades/s, que gana el personaje al disparar " +
                 "hacia abajo estando en el aire.")]
        [SerializeField] private float recoilForce = 7f;

        [Tooltip("Techo de velocidad de ascenso por retroceso. Impide quedarse " +
                 "flotando disparando al suelo. jumpForce vale 12, asi que por debajo " +
                 "de eso el retroceso nunca supera a un salto normal.")]
        [SerializeField] private float maxRecoilRiseSpeed = 8f;

        [Tooltip("Cuanto hay que apuntar hacia abajo para que haya retroceso. " +
                 "-1 es abajo del todo; -0.7 deja fuera las diagonales suaves.")]
        [Range(-1f, 0f)]
        [SerializeField] private float recoilAimThreshold = -0.7f;

        [SerializeField] private bool enableDownwardRecoil = true;

        [Tooltip("Desactiva la colision fisica entre el proyectil recien creado y los " +
                 "colliders del propio jugador, para que la bala no rebote ni empuje " +
                 "al dispararse pegada al cuerpo.")]
        [SerializeField] private bool ignoreShooterCollision = true;

        #endregion

        #region MUNICIÓN

        [Header("Sistema de Munición")]
        [Tooltip("¿Usar sistema de munición limitada?")]
        [SerializeField] private bool useAmmo = false;

        [Tooltip("Munición actual")]
        [SerializeField] private int currentAmmo = 30;

        [Tooltip("Munición máxima del cargador")]
        [SerializeField] private int maxAmmo = 30;

        [Tooltip("Munición de reserva")]
        [SerializeField] private int reserveAmmo = 120;

        [Tooltip("Tiempo de recarga (segundos)")]
        [SerializeField] private float reloadTime = 1.5f;

        #endregion

        #region EFECTOS VISUALES

        [Header("Efectos Visuales")]
        [Tooltip("Prefab de muzzle flash (fogonazo al disparar)")]
        [SerializeField] private GameObject muzzleFlashPrefab;

        [Tooltip("Duración del muzzle flash")]
        [SerializeField] private float muzzleFlashDuration = 0.05f;

        [Tooltip("SpriteRenderer del arma (para animación de disparo)")]
        [SerializeField] private SpriteRenderer weaponSprite;

        #endregion

        #region ESTADO

        private float lastFireTime;
        private bool isReloading = false;
        private GameObject currentMuzzleFlash;

        #endregion

        #region PROPIEDADES PÚBLICAS

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;
        public int ReserveAmmo => reserveAmmo;
        public bool IsReloading => isReloading;
        public float FireCooldown => 1f / fireRate;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            // Obtener AimController del padre (Player)
            aimController = GetComponentInParent<AimController>();
            characterController = GetComponentInParent<NABHI.Character.CharacterController2D>();

            if (aimController == null)
            {
                Debug.LogWarning("[WeaponController] No se encontró AimController en el Player. El arma no podrá apuntar.");
            }

            weaponStateManager = GetComponentInParent<WeaponStateManager>();
            sfx = GetComponentInParent<PlayerSFX>();

            // Crear FirePoint si no existe
            if (firePoint == null)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePoint = firePointObj.transform;
                firePoint.SetParent(transform);
                firePoint.localPosition = new Vector3(0.5f, 0, 0); // Ajustar según arma
            }
        }

        private void Update()
        {
            HandleInput();
        }

        #endregion

        #region INPUT

        private bool IsTriggerHeld()
        {
            if (string.IsNullOrEmpty(fireTriggerAxis)) return false;
            try { return Input.GetAxisRaw(fireTriggerAxis) >= fireTriggerThreshold; }
            catch (System.ArgumentException) { return false; }
        }

        private void HandleInput()
        {
            // Input de disparo (Mouse Left Click o botón de gamepad)
            bool fireInput = Input.GetButton("Fire1") || Input.GetMouseButton(0) || IsTriggerHeld();

            if (fireInput && CanFire())
            {
                Fire();
            }

            // Input de recarga (R o botón de gamepad)
            bool reloadInput = Input.GetKeyDown(KeyCode.R);

            if (reloadInput && CanReload())
            {
                StartReload();
            }
        }

        #endregion

        #region DISPARO

        private bool CanFire()
        {
            // Solo disparar si el arma fue recogida
            if (weaponStateManager != null && !weaponStateManager.HasPickedUpWeapon)
                return false;

            // No puede disparar si está recargando
            if (isReloading)
                return false;

            // Verificar cooldown de cadencia
            if (Time.time - lastFireTime < FireCooldown)
                return false;

            // Verificar munición (si el sistema está activado)
            if (useAmmo && currentAmmo <= 0)
                return false;

            // Verificar que exista prefab de proyectil
            if (projectilePrefab == null)
            {
                Debug.LogError("[WeaponController] No hay ProjectilePrefab asignado!");
                return false;
            }

            return true;
        }

        private void Fire()
        {
            // Obtener dirección de disparo
            Vector2 fireDirection = GetFireDirection();

            // Crear proyectil
            GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            IgnoreShooterCollision(projectileObj);

            Projectile projectile = projectileObj.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.Initialize(fireDirection, projectileSpeed);
                projectile.SetDamage(projectileDamage);
                projectile.SetPiercing(piercingShots);
            }
            else
            {
                Debug.LogError("[WeaponController] El ProjectilePrefab no tiene componente Projectile!");
            }

            // Consumir munición
            if (useAmmo)
            {
                currentAmmo--;
            }

            // Efectos visuales
            SpawnMuzzleFlash();

            ApplyDownwardRecoil();

            // Registrar tiempo del disparo
            lastFireTime = Time.time;

            // Callback para eventos (sonido, screenshake, etc.)
            OnFire();
        }

        /// <summary>
        /// Disparar hacia abajo en el aire empuja al personaje hacia arriba.
        ///
        /// No se usa ApplyKnockback del controller porque ese pone la velocidad a cero
        /// antes del impulso (esta pensado para el knockback de dano) y aqui eso
        /// cortaria en seco el movimiento horizontal en el aire.
        ///
        /// El techo evita quedarse flotando: con fireRate 5 se dispara 5 veces por
        /// segundo, asi que sin limite cada disparo sumaria altura indefinidamente.
        /// </summary>
        /// <summary>
        /// Anula la colision fisica entre el proyectil y el jugador que lo dispara.
        ///
        /// Projectile.HandleCollision ya ignora la capa Player a nivel logico (ni dana
        /// ni se destruye), pero eso no evita la respuesta fisica: el proyectil lleva
        /// un CircleCollider2D no-trigger y un Rigidbody2D, asi que el motor resuelve
        /// el choque igualmente y la bala rebota o empuja. Se nota sobre todo al
        /// disparar hacia abajo, cuando la boca del arma queda cerca del cuerpo.
        ///
        /// Se hace por pares de colliders y no en la matriz de capas porque el
        /// proyectil vive en la capa Default, y desactivar Default contra Player
        /// romperia la colision con cualquier otro objeto que este en Default.
        /// </summary>
        private void IgnoreShooterCollision(GameObject projectileObj)
        {
            if (!ignoreShooterCollision) return;
            if (characterController == null || projectileObj == null) return;

            Collider2D[] propios = characterController.GetComponentsInChildren<Collider2D>(true);
            Collider2D[] bala = projectileObj.GetComponentsInChildren<Collider2D>(true);

            for (int i = 0; i < bala.Length; i++)
            {
                if (bala[i] == null) continue;
                for (int k = 0; k < propios.Length; k++)
                {
                    if (propios[k] == null) continue;
                    Physics2D.IgnoreCollision(bala[i], propios[k], true);
                }
            }
        }

        private void ApplyDownwardRecoil()
        {
            if (!enableDownwardRecoil) return;
            if (characterController == null || aimController == null) return;

            // Solo en el aire: en tierra el suelo absorbe el retroceso.
            if (characterController.IsGrounded) return;

            Vector2 aim = aimController.AimDirection;
            if (aim.y > recoilAimThreshold) return;

            // El empuje va en direccion opuesta al disparo.
            Vector2 push = -aim.normalized * recoilForce;

            Vector2 v = characterController.Velocity;
            float desired = v.y + push.y;
            float capped  = Mathf.Min(desired, maxRecoilRiseSpeed);

            // Nunca frenar a quien ya sube mas rapido que el techo (por ejemplo
            // en pleno salto): el retroceso suma, no sustituye.
            float finalY = Mathf.Max(capped, v.y);

            characterController.SetVelocity(new Vector2(v.x + push.x, finalY));
        }

        private Vector2 GetFireDirection()
        {
            // Si hay AimController, usar su dirección
            if (aimController != null)
            {
                return aimController.AimDirection;
            }

            // Fallback: usar rotación Y del transform para detectar el flip (escala ya no aplica)
            bool facingRight = Mathf.Abs(transform.rotation.eulerAngles.y) < 90f;
            return facingRight ? Vector2.right : Vector2.left;
        }

        #endregion

        #region RECARGA

        private bool CanReload()
        {
            // No puede recargar si ya está recargando
            if (isReloading)
                return false;

            // No puede recargar si no usa munición
            if (!useAmmo)
                return false;

            // No puede recargar si el cargador está lleno
            if (currentAmmo >= maxAmmo)
                return false;

            // No puede recargar si no hay munición de reserva
            if (reserveAmmo <= 0)
                return false;

            return true;
        }

        private void StartReload()
        {
            isReloading = true;
            Invoke(nameof(FinishReload), reloadTime);
            OnReloadStart();
        }

        private void FinishReload()
        {
            // Calcular cuánta munición necesitamos
            int ammoNeeded = maxAmmo - currentAmmo;

            // Tomar munición de la reserva
            int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

            currentAmmo += ammoToReload;
            reserveAmmo -= ammoToReload;

            isReloading = false;
            OnReloadFinish();
        }

        #endregion

        #region EFECTOS VISUALES

        private void SpawnMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && firePoint != null)
            {
                // Crear muzzle flash
                currentMuzzleFlash = Instantiate(muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                currentMuzzleFlash.transform.SetParent(firePoint);

                // Destruir después de un tiempo
                Destroy(currentMuzzleFlash, muzzleFlashDuration);
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Agregar munición a la reserva
        /// </summary>
        public void AddAmmo(int amount)
        {
            reserveAmmo += amount;
        }

        /// <summary>
        /// Recargar completamente (power-up o checkpoint)
        /// </summary>
        public void RefillAmmo()
        {
            currentAmmo = maxAmmo;
            reserveAmmo = maxAmmo * 4; // O el valor que prefieras
        }

        /// <summary>
        /// Forzar disparo desde código externo
        /// </summary>
        public void ForceFire()
        {
            if (CanFire())
            {
                Fire();
            }
        }

        #endregion

        #region EVENTOS (para extender funcionalidad)

        protected virtual void OnFire()
        {
            sfx?.PlayShot();
        }

        protected virtual void OnReloadStart()
        {
            Debug.Log("Reload Start");

            // TODO: Agregar animación de recarga
            // TODO: Agregar sonido de recarga
        }

        protected virtual void OnReloadFinish()
        {
            Debug.Log("Reload Finish");

            // TODO: Agregar sonido de recarga terminada
        }

        #endregion

        #region DEBUG

        private void OnDrawGizmosSelected()
        {
            if (firePoint != null)
            {
                // Dibujar punto de disparo
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(firePoint.position, 0.1f);

                // Dibujar dirección de disparo
                if (Application.isPlaying && aimController != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(firePoint.position, firePoint.position + (Vector3)aimController.AimDirection * 2f);
                }
            }
        }

        #endregion
    }
}
