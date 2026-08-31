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
        [Tooltip("Gatillo derecho (RT / R2). Eje analogico del Input Manager.")]
        [SerializeField] private string fireTriggerAxis = "FireTrigger";

        [Tooltip("A partir de que recorrido del gatillo se considera disparo.")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float fireTriggerThreshold = 0.3f;

        [Tooltip("Boton extra de disparo en mando. None = solo gatillo.")]
        [SerializeField] private KeyCode fireKeyGamepad = KeyCode.None;

        [Header("Input - Equipar / Guardar")]
        [Tooltip("Eje del mando para equipar y guardar. Arriba en la cruceta.")]
        [SerializeField] private string equipToggleAxis = "DPadVertical";

        [Tooltip("Valor del eje a partir del cual se considera pulsado.")]
        [SerializeField] private float equipToggleThreshold = 0.5f;

        [Tooltip("Tecla equivalente en teclado, para probar sin mando.")]
        [SerializeField] private KeyCode equipToggleKey = KeyCode.Q;

        #endregion

        #region ESTADO

        private bool isWeaponEquipped = false;
        private float lastShootTime = 0f;
        private bool hasShot = false;

        // true una vez que el jugador recoge el arma del mundo — impide holster automático
        private bool hasPickedUpWeapon = false;

        // Flanco del input de equipar, para que un mantenido no alterne cada frame
        private bool equipTogglePrevio = false;
        private bool ejeToggleDisponible = true;

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

        private bool avisoReferenciaAsset = false;

        /// <summary>
        /// Disparo mantenido: teclado/raton via Fire1, o gatillo derecho analogico.
        /// El gatillo se lee como eje porque RT tiene recorrido, no es un boton.
        /// </summary>
        private bool IsFireHeld()
        {
            if (Input.GetButton("Fire1")) return true;
            if (fireKeyGamepad != KeyCode.None && Input.GetKey(fireKeyGamepad)) return true;

            if (!string.IsNullOrEmpty(fireTriggerAxis))
            {
                try { if (Input.GetAxisRaw(fireTriggerAxis) >= fireTriggerThreshold) return true; }
                catch (System.ArgumentException) { /* eje no definido en el Input Manager */ }
            }
            return false;
        }

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
            // Con el arma ya recogida, equipar y guardar pasa a ser decisión del jugador:
            // se alterna con arriba en la cruceta. Sin pickup sigue mandando el modo automático.
            if (hasPickedUpWeapon)
            {
                if (playerHealth != null && !playerHealth.CanReceiveInput())
                {
                    IsShooting = false;
                    return;
                }

                ProcesarToggleArma();

                // Solo se dispara con el arma en la mano.
                bool shootInput = IsFireHeld();
                IsShooting = isWeaponEquipped && shootInput;
                return;
            }

            // No permitir disparar si está en knockback (recibiendo daño)
            if (playerHealth != null && !playerHealth.CanReceiveInput())
            {
                IsShooting = false;
                return;
            }

            // Detectar si está disparando (teclado/mouse o boton X del mando)
            bool fireInput = IsFireHeld();

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

        private void LateUpdate()
        {
            if (weaponGameObject == null) return;

            // Un GameObject que vive en un prefab de disco no pertenece a ninguna
            // escena. Si la referencia apunta a un asset en vez de al arma de la
            // escena, SetActive reescribe el fichero del prefab en cada equipar y
            // desequipar: el asset aparece modificado en git sin que nadie lo haya
            // tocado, y la visibilidad real del arma no cambia. Se avisa una sola vez
            // y se ignora, en vez de seguir escribiendo en disco.
            if (!weaponGameObject.scene.IsValid())
            {
                if (!avisoReferenciaAsset)
                {
                    avisoReferenciaAsset = true;
                    Debug.LogError("[WeaponStateManager] weaponGameObject apunta a un prefab de disco " +
                                   $"('{weaponGameObject.name}'), no al arma de la escena. Se ignora para no " +
                                   "modificar el asset. Asigna el EdsGun que cuelga de WeaponPivot en el Player.");
                }
                return;
            }
            // Fuente única de verdad para la visibilidad: visible mientras el arma esté
            // equipada. Debe coincidir con el parámetro isArmed del Animator, porque las
            // animaciones armadas de Ed sujetan el arma con la mano.
            weaponGameObject.SetActive(IsWeaponEquipped);
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Alterna equipar/guardar con un flanco de subida: arriba en la cruceta o la
        /// tecla equivalente. Mantener pulsado no alterna en bucle.
        /// </summary>
        private void ProcesarToggleArma()
        {
            float eje = 0f;

            if (ejeToggleDisponible && !string.IsNullOrEmpty(equipToggleAxis))
            {
                try
                {
                    eje = Input.GetAxisRaw(equipToggleAxis);
                }
                catch (System.ArgumentException)
                {
                    // El eje no está definido en el Input Manager: seguimos con teclado.
                    ejeToggleDisponible = false;
                    Debug.LogWarning($"[WeaponStateManager] El eje '{equipToggleAxis}' no existe en el " +
                                     "Input Manager. Se usará solo la tecla de teclado.", this);
                }
            }

            bool pulsado = eje > equipToggleThreshold || Input.GetKey(equipToggleKey);

            if (pulsado && !equipTogglePrevio)
                ToggleWeapon();

            equipTogglePrevio = pulsado;
        }

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
        }

        /// <summary>
        /// Guardar arma manualmente (oculta el arma)
        /// </summary>
        public void HolsterWeapon()
        {
            isWeaponEquipped = false;
            hasShot = false;
            IsShooting = false;
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
