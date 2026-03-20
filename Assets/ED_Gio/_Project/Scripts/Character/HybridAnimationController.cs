using UnityEngine;
using NABHI.Character;
using NABHI.Weapons;

/// <summary>
/// Sistema híbrido de animación que combina:
/// - Frame-by-frame (sprite sequence) para movimiento sin arma (Idle, Run, Jump, WallSlide)
/// - Rigging 2D (bones) para movimiento con arma (Idle_Armed, Walk_Armed, Shoot, etc.)
/// </summary>
public class HybridAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController2D controller;
    [SerializeField] private WeaponStateManager weaponStateManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Rigging 2D System (Bones)")]
    [SerializeField] private GameObject riggedVisual;      // GameObject con el rig (bones)
    [SerializeField] private Animator riggedAnimator;      // Animator del rig

    [Header("Frame-by-Frame System (Sprites)")]
    [SerializeField] private GameObject spriteVisual;      // GameObject con SpriteRenderer
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator spriteAnimator;      // Animator para animaciones sprite

    [Header("Flip Settings")]
    [SerializeField] private bool autoFlipSprite = true;
    [Tooltip("Si los sprites frame-by-frame están dibujados mirando a la izquierda originalmente")]
    [SerializeField] private bool spritesFaceLeft = true;

    // Estados que usan frame-by-frame
    private enum AnimationSystem
    {
        Rigging2D,
        FrameByFrame
    }

    private AnimationSystem currentSystem = AnimationSystem.Rigging2D;
    private bool isFacingRight = true;
    private CharacterState characterState;

    // Hash para triggers (mejor performance y más confiable)
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int GroundPoundHash = Animator.StringToHash("GroundPound");

    // Guardar scales base originales para evitar acumulación
    private Vector3 spriteBaseScale;
    private Vector3 riggedBaseScale;

    // Bloqueo para animaciones de chakra (evita que Update cambie el sistema)
    private bool isPlayingChakraAnimation = false;
    private float chakraAnimationEndTime = 0f;

    void Start()
    {
        // Auto-find components si no están asignados
        if (controller == null)
            controller = GetComponent<CharacterController2D>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();

        if (weaponStateManager == null)
            weaponStateManager = GetComponent<WeaponStateManager>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        // Validar referencias
        if (riggedVisual == null || spriteVisual == null)
        {
            Debug.LogError("HybridAnimationController: Faltan referencias a Visual GameObjects!");
            enabled = false;
            return;
        }

        // Guardar scales base originales (valores absolutos)
        spriteBaseScale = spriteVisual.transform.localScale;
        spriteBaseScale.x = Mathf.Abs(spriteBaseScale.x);
        riggedBaseScale = riggedVisual.transform.localScale;
        riggedBaseScale.x = Mathf.Abs(riggedBaseScale.x);

        // Empezar con frame-by-frame activo (Idle)
        SetAnimationSystem(AnimationSystem.FrameByFrame);
    }

    void Update()
    {
        if (controller == null) return;

        // Determinar qué sistema usar según el estado
        AnimationSystem targetSystem = DetermineAnimationSystem();

        // Cambiar sistema si es necesario
        if (targetSystem != currentSystem)
        {
            SetAnimationSystem(targetSystem);
        }

        // Actualizar parámetros del animator activo
        UpdateAnimatorParameters();

        // Manejar flip
        if (autoFlipSprite)
        {
            HandleFlip();
        }
    }

    /// <summary>
    /// Determina qué sistema de animación usar según el estado del personaje y del arma
    /// </summary>
    private AnimationSystem DetermineAnimationSystem()
    {
        if (characterState == null) return AnimationSystem.FrameByFrame;

        // PRIORIDAD 0: Si está reproduciendo animación de chakra, mantener Rigging2D
        if (isPlayingChakraAnimation)
        {
            // Verificar si ya terminó el tiempo de bloqueo
            if (Time.time >= chakraAnimationEndTime)
            {
                isPlayingChakraAnimation = false;
            }
            else
            {
                return AnimationSystem.Rigging2D;
            }
        }

        // PRIORIDAD 1: Si está en knockback (recibiendo daño), SIEMPRE usar Rigging2D
        // Esto permite que la animación Hit se reproduzca sin interrupciones
        if (playerHealth != null && playerHealth.IsInKnockback)
        {
            return AnimationSystem.Rigging2D;
        }

        MovementState state = characterState.CurrentState;

        // PRIORIDAD 2: Si el arma está equipada Y está disparando
        // SIEMPRE usar Rigging2D para animación Shoot (quieto o moviéndose)
        if (weaponStateManager != null && weaponStateManager.IsWeaponEquipped && weaponStateManager.IsShooting)
        {
            // Usar Rigging2D con animación Shoot
            // El Animator manejará si se mueve (isMoving) o está quieto
            return AnimationSystem.Rigging2D;
        }

        // Sin arma: usar frame-by-frame para movimiento básico
        // Frame-by-frame para Idle, Run y WallSlide
        if (state == MovementState.Idle)
        {
            return AnimationSystem.FrameByFrame;
        }

        if (state == MovementState.Walking || state == MovementState.Running)
        {
            return AnimationSystem.FrameByFrame;
        }

        if (state == MovementState.WallSliding)
        {
            return AnimationSystem.FrameByFrame;
        }

        // Rigging 2D para todo lo demás (Jump, Dash, Fall, etc.)
        return AnimationSystem.Rigging2D;
    }

    /// <summary>
    /// Cambia entre sistema de rigging 2D y frame-by-frame
    /// </summary>
    private void SetAnimationSystem(AnimationSystem system)
    {
        currentSystem = system;

        if (system == AnimationSystem.FrameByFrame)
        {
            // Sincronizar scale del sprite con dirección actual ANTES de activar
            SyncSpriteScale();

            // Activar frame-by-frame, desactivar rigging
            spriteVisual.SetActive(true);
            riggedVisual.SetActive(false);
        }
        else
        {
            // Sincronizar scale del rig con dirección actual ANTES de activar
            SyncRiggedScale();

            // Activar rigging, desactivar frame-by-frame
            spriteVisual.SetActive(false);
            riggedVisual.SetActive(true);
        }
    }

    /// <summary>
    /// Actualiza parámetros del animator activo
    /// </summary>
    private void UpdateAnimatorParameters()
    {
        Animator activeAnimator = (currentSystem == AnimationSystem.FrameByFrame)
            ? spriteAnimator
            : riggedAnimator;

        if (activeAnimator == null) return;

        // Parámetros comunes
        float velocityX = Mathf.Abs(controller.Velocity.x);
        bool isGrounded = controller.IsGrounded;
        bool isMoving = velocityX > 0.1f;

        // Calcular JumpCount (común para ambos sistemas)
        int jumpCount = 0;
        if (!isGrounded)
        {
            jumpCount = controller.MaxAirJumps - controller.AirJumpsRemaining + 1;
        }

        // Frame-by-frame parameters
        if (currentSystem == AnimationSystem.FrameByFrame)
        {
            activeAnimator.SetBool("isMoving", isMoving);
            activeAnimator.SetBool("isGrounded", isGrounded);
            activeAnimator.SetFloat("velocityY", controller.Velocity.y);
            activeAnimator.SetInteger("JumpCount", jumpCount);

            // WallSliding state
            bool isWallSliding = characterState != null && characterState.CurrentState == MovementState.WallSliding;
            activeAnimator.SetBool("isWallSliding", isWallSliding);
        }
        // Rigging 2D parameters
        else
        {
            // Parámetros estándar
            activeAnimator.SetBool("isMoving", isMoving);
            activeAnimator.SetBool("isGrounded", isGrounded);
            activeAnimator.SetInteger("JumpCount", jumpCount);

            // Parámetros del arma
            if (weaponStateManager != null)
            {
                activeAnimator.SetBool("isArmed", weaponStateManager.IsWeaponEquipped);
                activeAnimator.SetBool("isShooting", weaponStateManager.IsShooting);
            }

            // CharacterAnimator.cs se encarga del resto (isJumping, isFalling, etc.)
        }
    }

    /// <summary>
    /// Maneja el flip del sprite según la dirección de movimiento
    /// </summary>
    private void HandleFlip()
    {
        // Usar FacingDirection del controller en lugar de input directo
        int facingDir = controller.FacingDirection;

        // Sincronizar con la dirección del controller
        bool shouldFaceRight = facingDir > 0;

        if (shouldFaceRight != isFacingRight)
        {
            Flip();
        }
    }

    /// <summary>
    /// Voltea el visual activo
    /// </summary>
    private void Flip()
    {
        isFacingRight = !isFacingRight;

        if (currentSystem == AnimationSystem.FrameByFrame)
        {
            SyncSpriteScale();
        }
        else
        {
            SyncRiggedScale();
        }
    }

    /// <summary>
    /// Sincroniza el scale del sprite con la dirección actual
    /// </summary>
    private void SyncSpriteScale()
    {
        Vector3 scale = spriteBaseScale;

        // Si sprites miran a izquierda: derecha = negativo, izquierda = positivo
        // Si sprites miran a derecha: derecha = positivo, izquierda = negativo
        if (spritesFaceLeft)
        {
            scale.x = isFacingRight ? -spriteBaseScale.x : spriteBaseScale.x;
        }
        else
        {
            scale.x = isFacingRight ? spriteBaseScale.x : -spriteBaseScale.x;
        }

        spriteVisual.transform.localScale = scale;
    }

    /// <summary>
    /// Sincroniza el scale del rig con la dirección actual
    /// </summary>
    private void SyncRiggedScale()
    {
        Vector3 scale = riggedBaseScale;
        scale.x = isFacingRight ? riggedBaseScale.x : -riggedBaseScale.x;
        riggedVisual.transform.localScale = scale;
    }

    /// <summary>
    /// Fuerza el uso de un sistema específico (útil para debugging)
    /// </summary>
    public void ForceAnimationSystem(bool useFrameByFrame)
    {
        SetAnimationSystem(useFrameByFrame ? AnimationSystem.FrameByFrame : AnimationSystem.Rigging2D);
    }

    /// <summary>
    /// Obtiene el sistema activo actual
    /// </summary>
    public bool IsUsingFrameByFrame()
    {
        return currentSystem == AnimationSystem.FrameByFrame;
    }

    /// <summary>
    /// Activa el trigger "Hit" en el Rigged Animator
    /// IMPORTANTE: Fuerza el cambio a Rigging2D ANTES de activar el trigger
    /// </summary>
    public void PlayHitAnimation()
    {
        // Verificar que el Rigged Animator existe y tiene controller
        if (riggedAnimator == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar animación Hit. Rigged Animator no está asignado.");
            return;
        }

        if (riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar animación Hit. Rigged Animator no tiene controller asignado.");
            return;
        }

        // PASO 1: Forzar cambio a Rigging2D ANTES de activar el trigger
        // Esto asegura que riggedVisual esté activo cuando se active el trigger
        if (currentSystem != AnimationSystem.Rigging2D)
        {
            Debug.Log("[HybridAnimationController] Forzando cambio a Rigging2D para animación Hit");
            SetAnimationSystem(AnimationSystem.Rigging2D);
        }

        // DEBUG: Verificar estado del animator antes de activar trigger
        if (riggedAnimator != null)
        {
            Debug.Log($"[HybridAnimationController] Estado del Animator antes de Hit:");
            Debug.Log($"  - isGrounded: {riggedAnimator.GetBool("isGrounded")}");
            Debug.Log($"  - JumpCount: {riggedAnimator.GetInteger("JumpCount")}");
            Debug.Log($"  - CurrentState: {riggedAnimator.GetCurrentAnimatorStateInfo(0).IsName("ED_Hit")}");
        }

        // PASO 2: Activar trigger Hit en el Rigged Animator (que ahora está activo)
        riggedAnimator.SetTrigger(HitHash);
        Debug.Log("[HybridAnimationController] Trigger 'Hit' activado en Rigged Animator");
    }

    /// <summary>
    /// Activa el trigger "GroundPound" para la animacion de golpe al suelo (Chakra Tremor)
    /// </summary>
    public void PlayGroundPoundAnimation()
    {
        StartCoroutine(PlayGroundPoundCoroutine());
    }

    private System.Collections.IEnumerator PlayGroundPoundCoroutine()
    {
        // Verificar que el Rigged Animator existe
        if (riggedAnimator == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar animación GroundPound. Rigged Animator no está asignado.");
            yield break;
        }

        if (riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar animación GroundPound. Rigged Animator no tiene controller asignado.");
            yield break;
        }

        // BLOQUEAR el cambio de sistema durante la animación (1 segundo debería ser suficiente)
        isPlayingChakraAnimation = true;
        chakraAnimationEndTime = Time.time + 1.0f;

        // Forzar cambio a Rigging2D para la animacion
        if (currentSystem != AnimationSystem.Rigging2D)
        {
            Debug.Log("[HybridAnimationController] Forzando cambio a Rigging2D para animación GroundPound");
            SetAnimationSystem(AnimationSystem.Rigging2D);
        }

        // IMPORTANTE: Esperar un frame para que el Animator se inicialice
        yield return null;

        // Limpiar cualquier trigger pendiente y activar el nuevo
        riggedAnimator.ResetTrigger(GroundPoundHash);
        riggedAnimator.SetTrigger(GroundPoundHash);
        Debug.Log("[HybridAnimationController] Trigger 'GroundPound' activado en Rigged Animator");
    }

    /// <summary>
    /// Activa un trigger de animacion por nombre
    /// </summary>
    public void PlayAnimationTrigger(string triggerName)
    {
        if (riggedAnimator == null || riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[HybridAnimationController] No se pudo activar trigger '{triggerName}'");
            return;
        }

        // Forzar Rigging2D
        if (currentSystem != AnimationSystem.Rigging2D)
        {
            SetAnimationSystem(AnimationSystem.Rigging2D);
        }

        riggedAnimator.SetTrigger(triggerName);
        Debug.Log($"[HybridAnimationController] Trigger '{triggerName}' activado");
    }
}
