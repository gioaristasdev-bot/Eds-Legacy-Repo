using UnityEngine;
using NABHI.Character;

namespace NABHI.Weapons
{
    /// <summary>
    /// Gestiona el estado del arma (equipada/guardada) de forma automática
    /// - Equipa el arma SOLO cuando está disparando activamente
    /// - Guarda el arma inmediatamente al dejar de disparar
    /// - El arma solo es visible durante la acción de disparo
    /// </summary>
    public class WeaponStateManager : MonoBehaviour
    {
        #region CONFIGURACIÓN

        [Header("Configuración de Estado")]
        [Tooltip("Modo de equipamiento del arma")]
        [SerializeField] private WeaponEquipMode equipMode = WeaponEquipMode.OnlyWhileShooting;

        public enum WeaponEquipMode
        {
            OnlyWhileShooting,  // Arma visible solo al disparar (recomendado)
            WithTimer           // Arma visible mientras dispara + timer de holster
        }

        [Tooltip("Tiempo sin disparar antes de guardar el arma (solo si mode = WithTimer)")]
        [SerializeField] private float timeToHolster = 2f;

        [Header("Referencias")]
        [Tooltip("GameObject del arma (para activar/desactivar)")]
        [SerializeField] private GameObject weaponGameObject;

        [Tooltip("WeaponController (detecta cuándo se dispara)")]
        [SerializeField] private WeaponController weaponController;

        [Tooltip("PlayerHealth (para verificar si puede disparar durante knockback)")]
        [SerializeField] private PlayerHealth playerHealth;

        [Header("Input - Mando")]
        [Tooltip("Boton de disparo en mando (X = JoystickButton2)")]
        [SerializeField] private KeyCode fireKeyGamepad = KeyCode.JoystickButton2;

        #endregion

        #region ESTADO

        private bool isWeaponEquipped = false;
        private float lastShootTime = 0f;
        private bool hasShot = false;

        // true una vez que el jugador recoge el arma del mundo — impide holster automático
        private bool hasPickedUpWeapon = false;

        #endregion

        #region PROPIEDADES PÚBLICAS

        /// <summary>
        /// ¿El arma está equipada actualmente?
        /// </summary>
        public bool IsWeaponEquipped => hasPickedUpWeapon && isWeaponEquipped;

        /// <summary>
        /// ¿El jugador ya recogió el arma del mundo?
        /// </summary>
        public bool HasPickedUpWeapon => hasPickedUpWeapon;

        /// <summary>
        /// ¿Está disparando activamente? (para animación de Shoot)
        /// </summary>
        public bool IsShooting { get; private set; }

        #endregion

        #region UNITY CALLBACKS

        private void Start()
        {
            // Auto-find WeaponController si no está asignado
            if (weaponController == null)
            {
                weaponController = GetComponentInChildren<WeaponController>();
            }

            // Auto-find PlayerHealth si no está asignado
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            // Estado inicial: arma guardada (solo se equipa al disparar)
            HolsterWeapon();
        }

        private void Update()
        {
            // Si el jugador ya recogió el arma, siempre está equipada — no holstear
            if (hasPickedUpWeapon)
            {
                isWeaponEquipped = true;

                if (playerHealth != null && !playerHealth.CanReceiveInput())
                {
                    IsShooting = false;
                    if (weaponGameObject != null) weaponGameObject.SetActive(false);
                    return;
                }

                bool shootInput = Input.GetButton("Fire1") || Input.GetKey(fireKeyGamepad);
                IsShooting = shootInput;

                // El visual del arma solo aparece al disparar
                if (weaponGameObject != null)
                    weaponGameObject.SetActive(IsShooting);

                return;
            }

            // No permitir disparar si está en knockback (recibiendo daño)
            if (playerHealth != null && !playerHealth.CanReceiveInput())
            {
                IsShooting = false;
                return;
            }

            // Detectar si está disparando (teclado/mouse o boton X del mando)
            bool fireInput = Input.GetButton("Fire1") || Input.GetKey(fireKeyGamepad);

            if (fireInput)
            {
                // Equipar arma al disparar
                if (!isWeaponEquipped)
                {
                    EquipWeapon();
                }

                // Actualizar tiempo de último disparo
                lastShootTime = Time.time;
                hasShot = true;
                IsShooting = false; // Sin pickup no se activa isShooting
            }
            else
            {
                IsShooting = false;

                // Guardar arma según el modo
                if (equipMode == WeaponEquipMode.OnlyWhileShooting)
                {
                    // Modo simple: guardar inmediatamente al soltar Fire1
                    if (isWeaponEquipped)
                    {
                        HolsterWeapon();
                    }
                }
            }

            // Modo con timer: guardar después de tiempo sin disparar
            if (equipMode == WeaponEquipMode.WithTimer && isWeaponEquipped && hasShot)
            {
                float timeSinceLastShot = Time.time - lastShootTime;

                if (timeSinceLastShot >= timeToHolster)
                {
                    HolsterWeapon();
                }
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Llamar cuando el jugador recoge el arma del mundo.
        /// A partir de este momento el arma permanece equipada indefinidamente.
        /// </summary>
        public void PickupWeapon()
        {
            hasPickedUpWeapon = true;
            EquipWeapon();
        }

        /// <summary>
        /// Equipar arma manualmente (muestra el arma)
        /// </summary>
        public void EquipWeapon()
        {
            isWeaponEquipped = true;

            if (weaponGameObject != null)
            {
                weaponGameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Guardar arma manualmente (oculta el arma)
        /// </summary>
        public void HolsterWeapon()
        {
            isWeaponEquipped = false;
            hasShot = false;
            IsShooting = false;

            if (weaponGameObject != null)
            {
                weaponGameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Toggle manual del arma (equipar/guardar)
        /// </summary>
        public void ToggleWeapon()
        {
            if (isWeaponEquipped)
            {
                HolsterWeapon();
            }
            else
            {
                EquipWeapon();
            }
        }

        /// <summary>
        /// Resetear timer de holster (útil para mantener arma equipada)
        /// </summary>
        public void ResetHolsterTimer()
        {
            lastShootTime = Time.time;
        }

        #endregion

        #region DEBUG

        private void OnGUI()
        {
            if (false)
            {
                GUILayout.BeginArea(new Rect(10, 150, 300, 100));
                GUILayout.Label($"<b>Weapon State:</b>");
                GUILayout.Label($"Equipped: {isWeaponEquipped}");
                GUILayout.Label($"Shooting: {IsShooting}");

                if (isWeaponEquipped && hasShot)
                {
                    float timeLeft = timeToHolster - (Time.time - lastShootTime);
                    GUILayout.Label($"Holster in: {timeLeft:F1}s");
                }

                GUILayout.EndArea();
            }
        }

        #endregion
    }
}
