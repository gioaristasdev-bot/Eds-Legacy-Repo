using UnityEngine;

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

        [Tooltip("Referencia al AimController (asignado automáticamente)")]
        private AimController aimController;

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

        [Tooltip("Retroceso del arma al disparar (screenshake, knockback)")]
        [SerializeField] private float recoilForce = 0.5f;

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

            if (aimController == null)
            {
                Debug.LogWarning("[WeaponController] No se encontró AimController en el Player. El arma no podrá apuntar.");
            }

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

        private void HandleInput()
        {
            // Input de disparo (Mouse Left Click o botón de gamepad)
            bool fireInput = Input.GetButton("Fire1") || Input.GetMouseButton(0);

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

            // Registrar tiempo del disparo
            lastFireTime = Time.time;

            // Callback para eventos (sonido, screenshake, etc.)
            OnFire();
        }

        private Vector2 GetFireDirection()
        {
            // Si hay AimController, usar su dirección
            if (aimController != null)
            {
                return aimController.AimDirection;
            }

            // Fallback: disparar hacia donde mira el personaje
            return Vector2.right * Mathf.Sign(transform.lossyScale.x);
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
            // Override en clases derivadas para sonido/VFX/screenshake
            Debug.Log("Fire!");

            // TODO: Agregar sonido de disparo
            // AudioManager.Instance.PlaySound("GunShot");

            // TODO: Agregar screenshake
            // CameraShake.Instance.Shake(0.1f, 0.1f);
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
