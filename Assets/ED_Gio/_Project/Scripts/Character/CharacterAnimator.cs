using UnityEngine;

namespace NABHI.Character
{
    /// <summary>
    /// Conecta el CharacterState con el Animator
    /// Actualiza los parámetros del animator basándose en el estado actual del personaje
    /// NOTA: Este componente debe estar en el Player. El Animator debe estar en el hijo "Visual"
    /// </summary>
    [RequireComponent(typeof(CharacterState))]
    [RequireComponent(typeof(CharacterController2D))]
    public class CharacterAnimator : MonoBehaviour
    {
        #region REFERENCIAS

        private Animator animator;
        private CharacterState characterState;
        private CharacterController2D controller;

        #endregion

        #region PARÁMETROS DEL ANIMATOR

        // Hash IDs para mejor performance (en lugar de strings)
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
        private static readonly int IsDashingHash = Animator.StringToHash("isDashing");
        private static readonly int IsWallSlidingHash = Animator.StringToHash("isWallSliding");
        private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
        private static readonly int IsFallingHash = Animator.StringToHash("isFalling");
        private static readonly int IsSprintingHash = Animator.StringToHash("isSprinting");
        private static readonly int VelocityYHash = Animator.StringToHash("velocityY");
        private static readonly int VelocityXHash = Animator.StringToHash("velocityX");
        private static readonly int HitHash = Animator.StringToHash("Hit");

        #endregion

        #region CONFIGURACIÓN

        [Header("Configuración")]
        [Tooltip("Activar/desactivar flip automático del sprite")]
        [SerializeField] private bool autoFlipSprite = true;

        [Tooltip("GameObject que contiene los sprites (usualmente 'Visual')")]
        [SerializeField] private Transform visualTransform;

        [Tooltip("Velocidad mínima para considerar que está 'moviéndose'")]
        [SerializeField] private float movementThreshold = 0.1f;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            // Obtener componentes del Player
            characterState = GetComponent<CharacterState>();
            controller = GetComponent<CharacterController2D>();

            // Si no se asignó visualTransform, buscar hijo llamado "Visual"
            if (visualTransform == null)
            {
                visualTransform = transform.Find("Visual");

                if (visualTransform == null)
                {
                    Debug.LogWarning($"[CharacterAnimator] No se encontró GameObject 'Visual'. " +
                        "El flip de sprite no funcionará. Asigna manualmente en Inspector.");
                }
            }

            // Buscar el Animator en el Visual (hijo)
            if (visualTransform != null)
            {
                animator = visualTransform.GetComponent<Animator>();

                if (animator == null)
                {
                    Debug.LogError($"[CharacterAnimator] No se encontró componente Animator en '{visualTransform.name}'. " +
                        "Asegúrate de que el Animator esté en el GameObject Visual.");
                }
            }
            else
            {
                Debug.LogError($"[CharacterAnimator] No se puede obtener el Animator porque no se encontró Visual.");
            }
        }

        private void Update()
        {
            UpdateAnimatorParameters();
            UpdateSpriteFlip();
        }

        #endregion

        #region ACTUALIZAR ANIMATOR

        private void UpdateAnimatorParameters()
        {
            if (animator == null || characterState == null || controller == null)
                return;

            // Obtener datos del controller
            bool isGrounded = controller.IsGrounded;
            bool isDashing = controller.IsDashing;
            bool isWallSliding = controller.IsWallSliding;
            Vector2 velocity = controller.Velocity;

            // Calcular si está moviéndose
            bool isMoving = Mathf.Abs(velocity.x) > movementThreshold && isGrounded;

            // Calcular si está saltando o cayendo
            bool isJumping = !isGrounded && velocity.y > 0.1f;
            bool isFalling = !isGrounded && velocity.y < -0.1f;

            // Detectar sprint (velocidad X mayor a moveSpeed base)
            bool isSprinting = isMoving && Mathf.Abs(velocity.x) > 8.5f; // 8 * 1.5 = 12, entonces > 8.5 indica sprint

            // Actualizar parámetros del Animator
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetBool(IsMovingHash, isMoving);
            animator.SetBool(IsDashingHash, isDashing);
            animator.SetBool(IsWallSlidingHash, isWallSliding);
            animator.SetBool(IsJumpingHash, isJumping);
            animator.SetBool(IsFallingHash, isFalling);
            animator.SetBool(IsSprintingHash, isSprinting);
            animator.SetFloat(VelocityYHash, velocity.y);
            animator.SetFloat(VelocityXHash, velocity.x);
        }

        #endregion

        #region FLIP SPRITE

        private void UpdateSpriteFlip()
        {
            if (!autoFlipSprite || visualTransform == null)
                return;

            int facingDirection = controller.FacingDirection;

            // Si está en WallSlide, mirar hacia la pared (invertir dirección)
            if (controller.IsWallSliding)
            {
                // WallDirection indica de qué lado está la pared
                // Queremos que el personaje mire hacia la pared
                facingDirection = controller.WallDirection;
            }

            // Flip usando scale
            Vector3 scale = visualTransform.localScale;

            if (facingDirection == 1) // Mirando a la derecha
            {
                scale.x = Mathf.Abs(scale.x); // Positivo
            }
            else if (facingDirection == -1) // Mirando a la izquierda
            {
                scale.x = -Mathf.Abs(scale.x); // Negativo
            }

            visualTransform.localScale = scale;
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Forzar reproducción de una animación específica
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (animator != null)
            {
                animator.Play(animationName);
            }
        }

        /// <summary>
        /// Activar un trigger del Animator
        /// </summary>
        public void SetTrigger(string triggerName)
        {
            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }
        }

        /// <summary>
        /// Activar animación de recibir daño
        /// </summary>
        public void PlayHitAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger(HitHash);
            }
        }

        /// <summary>
        /// Establecer valor de un parámetro booleano
        /// </summary>
        public void SetBool(string paramName, bool value)
        {
            if (animator != null)
            {
                animator.SetBool(paramName, value);
            }
        }

        /// <summary>
        /// Establecer valor de un parámetro float
        /// </summary>
        public void SetFloat(string paramName, float value)
        {
            if (animator != null)
            {
                animator.SetFloat(paramName, value);
            }
        }

        #endregion

        #region DEBUG

        private void OnValidate()
        {
            // Auto-asignar visualTransform si existe
            if (visualTransform == null)
            {
                Transform visual = transform.Find("Visual");
                if (visual != null)
                {
                    visualTransform = visual;
                }
            }
        }

        #endregion
    }
}
