using UnityEngine;
using NABHI.Chakras.Abilities;

namespace NABHI.Character
{
    /// <summary>
    /// Character Controller 2D profesional para NABHI
    /// Sistema de física y movimiento sin dependencias externas
    /// Autor: Aristas Studios
    /// Fecha: 2025-11-09
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class CharacterController2D : MonoBehaviour
    {
        #region REFERENCIAS

        [Header("Referencias")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Transform wallCheck;
        [SerializeField] private Transform ledgeCheck;

        private Rigidbody2D rb;
        private CapsuleCollider2D capsuleCollider;
        private CharacterState characterState;
        private PlayerHealth playerHealth; // Referencia al sistema de salud
        private ChakraFloat chakraFloat; // Referencia al chakra Float para deshabilitar salto
        private ChakraTremor chakraTremor; // Referencia al chakra Tremor para bloquear input

        public static bool inputBlocked = false;

        #endregion

        #region PARÁMETROS DE MOVIMIENTO

        [Header("Movimiento Horizontal")]
        [Tooltip("Velocidad máxima de movimiento en unidades/segundo")]
        [SerializeField] private float moveSpeed = 8f;

        [Tooltip("Velocidad de correr (multiplicador)")]
        [SerializeField] private float runSpeedMultiplier = 1.5f;

        [Tooltip("Tiempo para alcanzar velocidad máxima (segundos)")]
        [SerializeField] private float accelerationTime = 0.1f;

        [Tooltip("Tiempo para detenerse completamente (segundos)")]
        [SerializeField] private float decelerationTime = 0.1f;

        [Header("Control Aéreo")]
        [Tooltip("Multiplicador de control en el aire (0-1)")]
        [SerializeField] private float airControlMultiplier = 0.8f;

        [Tooltip("Velocidad máxima de caída")]
        [SerializeField] private float maxFallSpeed = 20f;

        #endregion

        #region PARÁMETROS DE SALTO

        [Header("Salto")]
        [Tooltip("Fuerza del salto")]
        [SerializeField] private float jumpForce = 12f;

        [Tooltip("Multiplicador de gravedad al soltar botón de salto")]
        [SerializeField] private float jumpCutMultiplier = 0.5f;

        [Tooltip("Multiplicador de gravedad al caer")]
        [SerializeField] private float fallGravityMultiplier = 1.5f;

        [Tooltip("Tiempo de Coyote Time (gracia para saltar después de caer)")]
        [SerializeField] private float coyoteTime = 0.15f;

        [Tooltip("Tiempo de Jump Buffer (recordar input de salto)")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Saltos Múltiples")]
        [SerializeField] private bool allowDoubleJump = true;
        [SerializeField] private int maxAirJumps = 1;

        #endregion

        #region PARÁMETROS DE DASH

        [Header("Dash")]
        [SerializeField] private bool dashEnabled = true;
        [SerializeField] private float dashSpeed = 20f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 1f;

        #endregion

        #region PARÁMETROS DE PARED

        [Header("Mecánicas de Pared")]
        [SerializeField] private bool wallSlideEnabled = true;
        [SerializeField] private float wallSlideSpeed = 4f;
        [SerializeField] private bool wallJumpEnabled = true;
        [SerializeField] private Vector2 wallJumpForce = new Vector2(12f, 14f);

        [Tooltip("Tiempo sin control horizontal después de WallJump")]
        [SerializeField] private float wallJumpControlLockTime = 0.15f;

        [Tooltip("Fuerza que empuja al personaje hacia la pared durante WallSlide (0 = sin empuje)")]
        [SerializeField] private float wallStickForce = 0f;

        #endregion

        #region PARÁMETROS DE LEDGE

        [Header("Ledge Grab")]
        [SerializeField] private bool ledgeGrabEnabled = false;
        [SerializeField] private float ledgeCheckDistance = 0.5f;

        #endregion

        #region DETECCIÓN DE COLISIONES

        [Header("Detección")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private float wallCheckDistance = 0.5f;

        #endregion

        #region VARIABLES DE ESTADO

        // Input
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool jumpHeld;
        private bool dashPressed;
        private bool sprintHeld;

        // Estado de movimiento
        private float currentSpeed;
        private float velocityXSmoothing;
        private int facingDirection = 1; // 1 = derecha, -1 = izquierda

        // Estado de salto
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private int airJumpsRemaining;
        private bool isJumping;

        // Estado de dash
        private bool isDashing;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private Vector2 dashDirection;

        // Estado de pared
        private bool isTouchingWall;
        private bool isWallSliding;
        private int wallDirection; // 1 = derecha, -1 = izquierda
        private float wallJumpControlLockCounter; // Tiempo restante sin control después de WallJump

        // Estado de suelo
        private bool isGrounded;
        private bool wasGrounded;

        #endregion

        #region PROPIEDADES PÚBLICAS

        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;
        public bool IsWallSliding => isWallSliding;
        public int FacingDirection => facingDirection;
        public int WallDirection => wallDirection;
        public Vector2 Velocity => rb.velocity;
        public int AirJumpsRemaining => airJumpsRemaining;
        public int MaxAirJumps => maxAirJumps;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            // Obtener componentes
            rb = GetComponent<Rigidbody2D>();
            capsuleCollider = GetComponent<CapsuleCollider2D>();
            playerHealth = GetComponent<PlayerHealth>();
            chakraFloat = GetComponentInChildren<ChakraFloat>();
            chakraTremor = GetComponentInChildren<ChakraTremor>();

            // Configurar Rigidbody2D
            rb.gravityScale = 2.5f; // Ajustar al gusto
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Crear componentes de detección si no existen
            CreateCheckPoints();
        }

        private void Update()
        {
            // Procesar input
            ProcessInput();

            // Actualizar contadores
            UpdateTimers();

            // Detectar colisiones
            CheckCollisions();

            // Procesar lógica de estado
            UpdateState();
        }

        private void FixedUpdate()
        {
            // Aplicar física
            if (isDashing)
            {
                ApplyDash();
            }
            else if (isWallSliding)
            {
                ApplyWallSlide();
            }
            else
            {
                ApplyMovement();
                ApplyGravity();
            }

            // Limitar velocidad de caída
            ClampFallSpeed();
        }

        #endregion

        #region INPUT

        private void ProcessInput()
        {
            // No procesar input si está en knockback (recibió daño)
            if (playerHealth != null && !playerHealth.CanReceiveInput())
            {
                moveInput = Vector2.zero;
                jumpPressed = false;
                jumpHeld = false;
                dashPressed = false;
                sprintHeld = false;
                return;
            }

            // No procesar input si está ejecutando Tremor (chakra)
            if (chakraTremor != null && chakraTremor.IsExecutingTremor)
            {
                moveInput = Vector2.zero;
                jumpPressed = false;
                jumpHeld = false;
                dashPressed = false;
                sprintHeld = false;
                return;
            }

            // Lectura de input (Old Input Manager)
            // TODO: Cambiar a New Input System si se desea
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");

            jumpPressed = Input.GetButtonDown("Jump");
            jumpHeld = Input.GetButton("Jump");
            dashPressed = Input.GetButtonDown("Dash");
            sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Normalizar input diagonal
            if (moveInput.magnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        #endregion

        #region DETECCIÓN DE COLISIONES

        private void CheckCollisions()
        {
            // Guardar estado anterior
            wasGrounded = isGrounded;

            // Detectar suelo
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );

            // Detectar pared - verificar AMBOS lados
            RaycastHit2D wallHitRight = Physics2D.Raycast(
                wallCheck.position,
                Vector2.right,
                wallCheckDistance,
                wallLayer
            );

            RaycastHit2D wallHitLeft = Physics2D.Raycast(
                wallCheck.position,
                Vector2.left,
                wallCheckDistance,
                wallLayer
            );

            // Determinar si toca pared y en qué dirección
            if (wallHitRight.collider != null)
            {
                isTouchingWall = true;
                wallDirection = 1; // Pared a la derecha
            }
            else if (wallHitLeft.collider != null)
            {
                isTouchingWall = true;
                wallDirection = -1; // Pared a la izquierda
            }
            else
            {
                isTouchingWall = false;
            }
        }

        private void CreateCheckPoints()
        {
            // Crear puntos de verificación si no existen
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheck = groundCheckObj.transform;
                groundCheck.parent = transform;
                groundCheck.localPosition = new Vector3(0, -capsuleCollider.size.y / 40, 0);
            }

            if (wallCheck == null)
            {
                GameObject wallCheckObj = new GameObject("WallCheck");
                wallCheck = wallCheckObj.transform;
                wallCheck.parent = transform;
                wallCheck.localPosition = new Vector3(0, 0, 0);
            }
        }

        #endregion

        #region MOVIMIENTO

        private void ApplyMovement()
        {
            // No aplicar movimiento durante knockback (permite que el empuje funcione)
            if (playerHealth != null && playerHealth.IsInKnockback)
            {
                return;
            }

            // Durante el control lock del WallJump, no modificar velocidad horizontal
            if (wallJumpControlLockCounter > 0)
            {
                // Solo mantener la velocidad Y actual (gravedad), pero preservar el impulso X del WallJump
                return;
            }

            // Calcular velocidad objetivo con multiplicador de sprint
            float currentMoveSpeed = sprintHeld ? moveSpeed * runSpeedMultiplier : moveSpeed;
            float targetSpeed = moveInput.x * currentMoveSpeed;

            // Si está tocando pared y presionando hacia ella, no aplicar movimiento horizontal hacia la pared
            bool isPushingTowardsWall = isTouchingWall && Mathf.Sign(moveInput.x) == Mathf.Sign(wallDirection) && Mathf.Abs(moveInput.x) > 0.1f;
            if (isPushingTowardsWall && !isGrounded)
            {
                targetSpeed = 0f; // No empujar hacia la pared
                currentSpeed = 0f; // Forzar velocidad a 0 inmediatamente
                // Debug.Log($"[ApplyMovement] Presionando hacia pared - Velocity: {rb.velocity}, Gravity: {rb.gravityScale}");
            }

            // Aplicar multiplicador de aire si no está en suelo
            float acceleration = isGrounded ? accelerationTime : accelerationTime / airControlMultiplier;

            // Suavizar velocidad con SmoothDamp
            currentSpeed = Mathf.SmoothDamp(
                currentSpeed,
                targetSpeed,
                ref velocityXSmoothing,
                acceleration
            );

            // Aplicar velocidad
            rb.velocity = new Vector2(currentSpeed, rb.velocity.y);

            // Actualizar dirección
            if (moveInput.x > 0.1f)
            {
                facingDirection = 1;
            }
            else if (moveInput.x < -0.1f)
            {
                facingDirection = -1;
            }
        }

        #endregion

        #region SALTO

        private void UpdateTimers()
        {
            // Coyote Time
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
                airJumpsRemaining = maxAirJumps;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            // Jump Buffer
            if (jumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            // Dash Cooldown
            if (dashCooldownRemaining > 0)
            {
                dashCooldownRemaining -= Time.deltaTime;
            }

            // WallJump Control Lock
            if (wallJumpControlLockCounter > 0)
            {
                wallJumpControlLockCounter -= Time.deltaTime;
            }
        }

        private void UpdateState()
        {
            // Aterrizaje
            if (isGrounded && !wasGrounded)
            {
                OnLanding();
            }

            // Salto (deshabilitado cuando Float chakra está activo)
            bool floatModeActive = chakraFloat != null && chakraFloat.IsFloatModeActive;

            if (jumpBufferCounter > 0 && !floatModeActive)
            {
                if (coyoteTimeCounter > 0)
                {
                    // Salto normal desde el suelo
                    Jump();
                    jumpBufferCounter = 0;
                }
                else if (wallJumpEnabled && isTouchingWall && !isGrounded)
                {
                    // Wall jump tiene PRIORIDAD cuando está tocando la pared
                    WallJump();
                    jumpBufferCounter = 0;
                    // Restaurar saltos aéreos después del WallJump
                    airJumpsRemaining = maxAirJumps;
                }
                else if (allowDoubleJump && airJumpsRemaining > 0 && !isGrounded)
                {
                    // Doble salto solo si NO está tocando la pared
                    Jump();
                    airJumpsRemaining--;
                    jumpBufferCounter = 0;
                }
            }

            // Dash
            if (dashPressed && dashEnabled && dashCooldownRemaining <= 0 && !isDashing)
            {
                StartDash();
            }

            // Wall Slide (estilo Hollow Knight/Megaman)
            // Solo activar WallSlide si ESTÁ presionando hacia la pared
            bool isPushingTowardsWall = Mathf.Sign(moveInput.x) == Mathf.Sign(wallDirection) && Mathf.Abs(moveInput.x) > 0.1f;

            // Usar tolerancia en velocidad Y para evitar flickering (0.1f en lugar de 0)
            if (wallSlideEnabled && isTouchingWall && !isGrounded && rb.velocity.y <= 0.1f && isPushingTowardsWall)
            {
                isWallSliding = true;
            }
            else
            {
                isWallSliding = false;
            }
        }

        private void Jump()
        {
            // No permitir salto durante knockback
            if (playerHealth != null && playerHealth.IsInKnockback)
            {
                return;
            }

            // Resetear velocidad vertical
            rb.velocity = new Vector2(rb.velocity.x, 0);

            // Aplicar fuerza de salto
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            isJumping = true;
            coyoteTimeCounter = 0;

            // Callback para eventos/sonido
            OnJump();
        }

        private void WallJump()
        {
            // No permitir wall jump durante knockback
            if (playerHealth != null && playerHealth.IsInKnockback)
            {
                return;
            }

            // Saltar en dirección opuesta a la pared (diagonal)
            Vector2 jumpDir = new Vector2(-wallDirection * wallJumpForce.x, wallJumpForce.y);
            rb.velocity = jumpDir;

            // Activar lock de control horizontal para que el impulso no se sobreescriba
            wallJumpControlLockCounter = wallJumpControlLockTime;

            // Cambiar dirección
            facingDirection = -wallDirection;

            isJumping = true;
            isWallSliding = false;

            OnWallJump();
        }

        #endregion

        #region GRAVEDAD

        private void ApplyGravity()
        {
            // No modificar gravedad si Float chakra está activo
            if (chakraFloat != null && chakraFloat.IsFloatModeActive)
            {
                return;
            }

            // Gravedad variable para mejor feel de salto
            if (rb.velocity.y < 0)
            {
                // Cayendo - gravedad aumentada
                rb.gravityScale = 2.5f * fallGravityMultiplier;
            }
            else if (rb.velocity.y > 0 && !jumpHeld)
            {
                // Saltando pero soltó botón - cortar salto
                rb.gravityScale = 2.5f * jumpCutMultiplier;
            }
            else
            {
                // Gravedad normal
                rb.gravityScale = 2.5f;
            }
        }

        private void ClampFallSpeed()
        {
            // No limitar velocidad si Float chakra está activo
            if (chakraFloat != null && chakraFloat.IsFloatModeActive)
            {
                return;
            }

            if (rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
            }
        }

        #endregion

        #region DASH

        private void StartDash()
        {
            // No permitir dash durante knockback
            if (playerHealth != null && playerHealth.IsInKnockback)
            {
                return;
            }

            isDashing = true;
            dashTimeRemaining = dashDuration;
            dashCooldownRemaining = dashCooldown;

            // Determinar dirección del dash (SOLO HORIZONTAL)
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                // Dash en dirección horizontal del input (ignora input vertical)
                dashDirection = new Vector2(Mathf.Sign(moveInput.x), 0);
            }
            else
            {
                // Dash hacia donde mira el personaje
                dashDirection = new Vector2(facingDirection, 0);
            }

            // Resetear gravedad durante dash
            rb.gravityScale = 0;

            OnDashStart();
        }

        private void ApplyDash()
        {
            dashTimeRemaining -= Time.fixedDeltaTime;

            if (dashTimeRemaining <= 0)
            {
                EndDash();
            }
            else
            {
                // Aplicar velocidad de dash
                rb.velocity = dashDirection * dashSpeed;
            }
        }

        private void EndDash()
        {
            isDashing = false;
            rb.gravityScale = 2.5f;

            // Reducir velocidad después del dash
            rb.velocity = rb.velocity * 0.5f;

            OnDashEnd();
        }

        /// <summary>
        /// Cancela el dash forzadamente (útil para knockback o daño)
        /// </summary>
        public void CancelDash()
        {
            if (isDashing)
            {
                isDashing = false;
                dashTimeRemaining = 0f;
                rb.gravityScale = 2.5f;
                Debug.Log("[CharacterController2D] Dash cancelado");
            }
        }

        #endregion

        #region WALL MECHANICS

        private void ApplyWallSlide()
        {
            // Velocidad horizontal mínima hacia la pared
            float horizontalVelocity = wallDirection * wallStickForce;

            // Aplicar velocidad de caída constante (wallSlideSpeed)
            // Forzar la velocidad vertical directamente para asegurar el deslizamiento
            float verticalVelocity = -wallSlideSpeed;

            // Aplicar velocidades directamente
            rb.velocity = new Vector2(horizontalVelocity, verticalVelocity);

            // Desactivar gravedad durante wall slide (ya controlamos la caída manualmente)
            rb.gravityScale = 0f;
        }

        #endregion

        #region EVENTOS (para extender funcionalidad)

        protected virtual void OnJump()
        {
            // Override en clases derivadas para sonido/VFX
            Debug.Log("Jump!");
        }

        protected virtual void OnWallJump()
        {
            Debug.Log("Wall Jump!");
        }

        protected virtual void OnLanding()
        {
            Debug.Log("Landed!");
            isJumping = false;
        }

        protected virtual void OnDashStart()
        {
            Debug.Log("Dash Start!");
        }

        protected virtual void OnDashEnd()
        {
            Debug.Log("Dash End!");
        }

        #endregion

        #region GIZMOS (Debug Visual)

        private void OnDrawGizmosSelected()
        {
            if (groundCheck != null)
            {
                // Dibujar detector de suelo
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            if (wallCheck != null)
            {
                // Dibujar detector de pared
                Gizmos.color = isTouchingWall ? Color.blue : Color.yellow;
                Vector3 wallRayEnd = wallCheck.position + Vector3.right * facingDirection * wallCheckDistance;
                Gizmos.DrawLine(wallCheck.position, wallRayEnd);
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS (API)

        /// <summary>
        /// Forzar salto desde código externo
        /// </summary>
        public void ForceJump()
        {
            if (isGrounded || coyoteTimeCounter > 0)
            {
                Jump();
            }
        }

        /// <summary>
        /// Forzar dash desde código externo
        /// </summary>
        public void ForceDash(Vector2 direction)
        {
            if (dashCooldownRemaining <= 0)
            {
                dashDirection = direction.normalized;
                StartDash();
            }
        }

        /// <summary>
        /// Aplicar knockback externo
        /// </summary>
        public void ApplyKnockback(Vector2 force)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        /// <summary>
        /// Establecer velocidad directamente
        /// </summary>
        public void SetVelocity(Vector2 velocity)
        {
            rb.velocity = velocity;
        }

        #endregion
    }
}
