using UnityEngine;

namespace NABHI.Weapons
{
    /// <summary>
    /// Sistema de aim en 8 direcciones para Metroidvania shooter
    /// Maneja la dirección de disparo usando teclado/mouse o gamepad
    /// </summary>
    public class AimController : MonoBehaviour
    {
        #region ENUMS

        public enum AimMode
        {
            KeyboardMouse,  // Aim con mouse o teclas direccionales
            Gamepad         // Aim con stick derecho del gamepad
        }

        public enum AimStyle
        {
            FreeAim,        // Aim libre en cualquier dirección
            DirectionalAim, // Aim en 8 direcciones fijas (mejor para pixel art)
            FrontOnly       // Aim solo hacia adelante (5 direcciones: frente, diagonal-arriba, arriba, diagonal-abajo, abajo)
        }

        #endregion

        #region CONFIGURACIÓN

        [Header("Configuración de Aim")]
        [Tooltip("Modo de aim (teclado/mouse o gamepad)")]
        [SerializeField] private AimMode aimMode = AimMode.KeyboardMouse;

        [Tooltip("Estilo de aim (libre o 8 direcciones)")]
        [SerializeField] private AimStyle aimStyle = AimStyle.DirectionalAim;

        [Tooltip("Auto-detectar modo según input")]
        [SerializeField] private bool autoDetectInputMode = true;

        [Header("Configuración Visual")]
        [Tooltip("Transform del arma (para rotarla hacia el aim)")]
        [SerializeField] private Transform weaponTransform;

        [Tooltip("Offset del arma respecto al personaje (ignorado si el arma está en un bone)")]
        [SerializeField] private Vector2 weaponOffset = new Vector2(0.3f, 0.1f);

        [Tooltip("Si el arma está dentro de un bone (ej: RightHand), no modificar su posición local")]
        [SerializeField] private bool weaponIsInBone = false;

        [Tooltip("Rotar arma hacia dirección de aim")]
        [SerializeField] private bool rotateWeapon = true;

        [Tooltip("Flip del arma cuando miras a la izquierda")]
        [SerializeField] private bool flipWeaponOnLeft = true;

        [Header("8 Direcciones")]
        [Tooltip("Zona muerta para detectar input (evita drift del stick)")]
        [SerializeField] private float deadzone = 0.3f;

        #endregion

        #region REFERENCIAS

        private NABHI.Character.CharacterController2D characterController;
        private Vector2 aimDirection = Vector2.right;
        private Vector2 lastAimDirection = Vector2.right;

        #endregion

        #region PROPIEDADES PÚBLICAS

        public Vector2 AimDirection => aimDirection;
        public AimMode CurrentAimMode => aimMode;

        #endregion

        #region DIRECCIONES PREDEFINIDAS

        // 8 direcciones completas
        private static readonly Vector2[] EightDirections = new Vector2[]
        {
            new Vector2(1, 0),      // Derecha
            new Vector2(1, 1).normalized,   // Derecha-Arriba
            new Vector2(0, 1),      // Arriba
            new Vector2(-1, 1).normalized,  // Izquierda-Arriba
            new Vector2(-1, 0),     // Izquierda
            new Vector2(-1, -1).normalized, // Izquierda-Abajo
            new Vector2(0, -1),     // Abajo
            new Vector2(1, -1).normalized   // Derecha-Abajo
        };

        // 5 direcciones frontales (relativas a mirar hacia la derecha)
        // Se multiplicará X por facingDirection para voltear cuando mira izquierda
        private static readonly Vector2[] FrontDirections = new Vector2[]
        {
            new Vector2(1, 0),                  // Adelante (horizontal)
            new Vector2(1, 1).normalized,       // Diagonal arriba
            new Vector2(0, 1),                  // Arriba
            new Vector2(1, -1).normalized,      // Diagonal abajo
            new Vector2(0, -1)                  // Abajo
        };

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            characterController = GetComponent<NABHI.Character.CharacterController2D>();

            if (characterController == null)
            {
                Debug.LogWarning("[AimController] No se encontró CharacterController2D. El aim no podrá usar la dirección del personaje.");
            }

            // Buscar arma automáticamente si no está asignada
            if (weaponTransform == null)
            {
                Transform weaponObj = transform.Find("Weapon");
                if (weaponObj != null)
                {
                    weaponTransform = weaponObj;
                }
            }
        }

        private void Update()
        {
            UpdateAimDirection();
            UpdateWeaponVisuals();
        }

        #endregion

        #region AIM DIRECTION

        private void UpdateAimDirection()
        {
            Vector2 rawInput = GetRawAimInput();

            // Auto-detectar modo de input
            if (autoDetectInputMode)
            {
                DetectInputMode(rawInput);
            }

            // Obtener dirección del personaje
            int facingDirection = 1;
            if (characterController != null)
            {
                facingDirection = characterController.FacingDirection;
            }

            // Si hay input, actualizar dirección
            if (rawInput.magnitude > deadzone)
            {
                if (aimStyle == AimStyle.DirectionalAim)
                {
                    aimDirection = GetNearestEightDirection(rawInput);
                }
                else if (aimStyle == AimStyle.FrontOnly)
                {
                    aimDirection = GetNearestFrontDirection(rawInput, facingDirection);
                }
                else
                {
                    aimDirection = rawInput.normalized;
                }

                lastAimDirection = aimDirection;
            }
            else
            {
                // Sin input, usar dirección horizontal del personaje
                if (characterController != null)
                {
                    aimDirection = new Vector2(facingDirection, 0);
                }
                else
                {
                    aimDirection = lastAimDirection;
                }
            }
        }

        private Vector2 GetRawAimInput()
        {
            Vector2 input = Vector2.zero;

            if (aimMode == AimMode.KeyboardMouse)
            {
                // Opción 1: Mouse (prioridad)
                Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 directionToMouse = (mousePos - transform.position).normalized;

                // Opción 2: Teclas direccionales (Up, Down, etc.)
                Vector2 keyboardInput = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );

                // Si hay input de teclado, usarlo (útil para gamepad o solo teclado)
                // Si no, usar mouse
                if (keyboardInput.magnitude > 0.1f)
                {
                    input = keyboardInput;
                }
                else if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
                {
                    // Solo usar mouse si está presionando algún botón
                    input = directionToMouse;
                }
                else
                {
                    // Sin input, retornar zero
                    input = Vector2.zero;
                }
            }
            else // Gamepad
            {
                // Right Stick para aim
                float horizontal = Input.GetAxisRaw("RightStickHorizontal");
                float vertical = Input.GetAxisRaw("RightStickVertical");

                input = new Vector2(horizontal, vertical);

                // Fallback: Si no hay right stick configurado, usar left stick
                if (input.magnitude < 0.1f)
                {
                    input = new Vector2(
                        Input.GetAxisRaw("Horizontal"),
                        Input.GetAxisRaw("Vertical")
                    );
                }
            }

            return input;
        }

        private void DetectInputMode(Vector2 rawInput)
        {
            // Detectar si está usando gamepad o teclado/mouse
            // Esto es básico, puede mejorarse con Input System nuevo

            // Si hay input de right stick, es gamepad
            float rightStickMagnitude = new Vector2(
                Input.GetAxisRaw("RightStickHorizontal"),
                Input.GetAxisRaw("RightStickVertical")
            ).magnitude;

            if (rightStickMagnitude > 0.1f)
            {
                aimMode = AimMode.Gamepad;
            }
            // Si hay movimiento de mouse, es teclado/mouse
            else if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            {
                aimMode = AimMode.KeyboardMouse;
            }
        }

        private Vector2 GetNearestEightDirection(Vector2 input)
        {
            float minAngle = float.MaxValue;
            Vector2 nearest = Vector2.right;

            foreach (Vector2 dir in EightDirections)
            {
                float angle = Vector2.Angle(input, dir);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    nearest = dir;
                }
            }

            return nearest;
        }

        private Vector2 GetNearestFrontDirection(Vector2 input, int facingDirection)
        {
            float minAngle = float.MaxValue;
            Vector2 nearest = new Vector2(facingDirection, 0); // Default: adelante

            foreach (Vector2 dir in FrontDirections)
            {
                // Ajustar dirección según hacia dónde mira el personaje
                Vector2 adjustedDir = new Vector2(dir.x * facingDirection, dir.y);

                float angle = Vector2.Angle(input, adjustedDir);
                if (angle < minAngle)
                {
                    minAngle = angle;
                    nearest = adjustedDir;
                }
            }

            return nearest;
        }

        #endregion

        #region WEAPON VISUALS

        private void UpdateWeaponVisuals()
        {
            if (weaponTransform == null)
                return;

            // Obtener la dirección que mira el personaje
            int facingDirection = 1;
            if (characterController != null)
            {
                facingDirection = characterController.FacingDirection;
            }

            // Solo posicionar el arma si NO está dentro de un bone
            // Si está en un bone (ej: RightHand), el bone controla la posición
            if (!weaponIsInBone)
            {
                Vector3 adjustedOffset = weaponOffset;
                adjustedOffset.x = weaponOffset.x * facingDirection;
                weaponTransform.localPosition = adjustedOffset;
            }

            // Rotar arma hacia dirección de aim
            if (rotateWeapon)
            {
                float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

                // Si el arma está en un bone y el personaje está mirando a la izquierda,
                // el Visual ya está flippeado, así que ajustamos el ángulo
                if (weaponIsInBone && facingDirection < 0)
                {
                    // Cuando Visual tiene scale.x = -1, la rotación se invierte
                    // Necesitamos compensar: invertir el ángulo respecto al eje Y
                    angle = 180f - angle;
                }

                weaponTransform.localRotation = Quaternion.Euler(0, 0, angle);
            }

            // Flip del arma cuando miras a la izquierda
            // Si el arma está en un bone, el flip del Visual ya lo maneja
            if (flipWeaponOnLeft && !weaponIsInBone)
            {
                Vector3 scale = weaponTransform.localScale;

                // Determinar si el arma debe estar flippeada
                bool shouldFlip;

                Vector2 rawInput = GetRawAimInput();
                if (rawInput.magnitude > deadzone)
                {
                    shouldFlip = aimDirection.x < 0;
                }
                else
                {
                    shouldFlip = facingDirection < 0;
                }

                if (shouldFlip)
                {
                    scale.y = -Mathf.Abs(scale.y);
                }
                else
                {
                    scale.y = Mathf.Abs(scale.y);
                }

                weaponTransform.localScale = scale;
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Forzar dirección de aim
        /// </summary>
        public void SetAimDirection(Vector2 direction)
        {
            aimDirection = direction.normalized;
            lastAimDirection = aimDirection;
        }

        /// <summary>
        /// Cambiar modo de aim
        /// </summary>
        public void SetAimMode(AimMode mode)
        {
            aimMode = mode;
        }

        /// <summary>
        /// Cambiar estilo de aim
        /// </summary>
        public void SetAimStyle(AimStyle style)
        {
            aimStyle = style;
        }

        #endregion

        #region DEBUG

        private void OnDrawGizmos()
        {
            // Dibujar dirección de aim
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)aimDirection * 2f);

            // Dibujar las direcciones posibles según el modo
            if (aimStyle == AimStyle.DirectionalAim)
            {
                Gizmos.color = Color.yellow;
                foreach (Vector2 dir in EightDirections)
                {
                    Gizmos.DrawLine(transform.position, transform.position + (Vector3)dir * 0.5f);
                }
            }
            else if (aimStyle == AimStyle.FrontOnly)
            {
                // Obtener dirección del personaje
                int facingDir = 1;
                if (characterController != null)
                {
                    facingDir = characterController.FacingDirection;
                }

                Gizmos.color = Color.green;
                foreach (Vector2 dir in FrontDirections)
                {
                    Vector2 adjustedDir = new Vector2(dir.x * facingDir, dir.y);
                    Gizmos.DrawLine(transform.position, transform.position + (Vector3)adjustedDir * 0.5f);
                }
            }
        }

        #endregion
    }
}
