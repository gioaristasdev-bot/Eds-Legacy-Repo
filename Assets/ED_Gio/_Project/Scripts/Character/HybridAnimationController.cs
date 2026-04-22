using UnityEngine;
using NABHI.Character;
using NABHI.Weapons;

/// <summary>
/// Controlador de animaciones de Ed basado en rig 2D.
/// Maneja todos los estados de movimiento (con arma y sin arma) a través de
/// parámetros del Animator. El Animator Controller decide qué clip reproducir
/// según isArmed, isLevitating, isHacking, etc.
/// </summary>
public class HybridAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController2D controller;
    [SerializeField] private WeaponStateManager weaponStateManager;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Rigging 2D")]
    [SerializeField] private GameObject riggedVisual;
    [SerializeField] private Animator riggedAnimator;

    [Header("Flip Settings")]
    [SerializeField] private bool autoFlipSprite = true;

    // --- Hashes de parámetros (performance) ---
    private static readonly int IsMovingHash       = Animator.StringToHash("isMoving");
    private static readonly int IsGroundedHash     = Animator.StringToHash("isGrounded");
    private static readonly int IsJumpingHash      = Animator.StringToHash("isJumping");
    private static readonly int IsFallingHash      = Animator.StringToHash("isFalling");
    private static readonly int IsDashingHash      = Animator.StringToHash("isDashing");
    private static readonly int IsWallSlidingHash  = Animator.StringToHash("isWallSliding");
    private static readonly int IsArmedHash        = Animator.StringToHash("isArmed");
    private static readonly int IsShootingHash     = Animator.StringToHash("isShooting");
    private static readonly int IsLevitatingHash   = Animator.StringToHash("isLevitating");
    private static readonly int IsHackingHash      = Animator.StringToHash("isHacking");
    private static readonly int JumpCountHash      = Animator.StringToHash("JumpCount");
    private static readonly int HitHash            = Animator.StringToHash("Hit");
    private static readonly int GroundPoundHash    = Animator.StringToHash("GroundPound");

    // --- Estado interno ---
    private bool isFacingRight = true;
    private Vector3 riggedBaseScale;
    private CharacterState characterState;

    // Bloqueo para animaciones de chakra
    private bool isPlayingChakraAnimation = false;
    private float chakraAnimationEndTime  = 0f;

    // Estado de animaciones especiales (set desde chakras)
    private bool isLevitating = false;
    private bool isHacking    = false;

    // -------------------------------------------------------------------------
    #region UNITY CALLBACKS

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController2D>();

        if (characterState == null)
            characterState = GetComponent<CharacterState>();

        if (weaponStateManager == null)
            weaponStateManager = GetComponent<WeaponStateManager>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (riggedVisual == null)
        {
            // Buscar primer hijo con Animator como fallback
            Animator found = GetComponentInChildren<Animator>();
            if (found != null)
            {
                riggedVisual = found.gameObject;
                if (riggedAnimator == null)
                    riggedAnimator = found;
                Debug.LogWarning($"[HybridAnimationController] riggedVisual auto-asignado a '{riggedVisual.name}'. Asígnalo en Inspector.");
            }
            else
            {
                Debug.LogError("[HybridAnimationController] riggedVisual no asignado y no se encontró Animator en hijos.");
                enabled = false;
                return;
            }
        }

        riggedVisual.SetActive(true);
    }

    void Update()
    {
        if (controller == null || riggedAnimator == null) return;

        UpdateAnimatorParameters();

        if (autoFlipSprite)
            HandleFlip();
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ANIMATOR PARAMETERS

    private void UpdateAnimatorParameters()
    {
        Vector2 velocity   = controller.Velocity;
        bool isGrounded    = controller.IsGrounded;
        bool isDashing     = controller.IsDashing;
        bool isWallSliding = controller.IsWallSliding;
        bool isMoving      = Mathf.Abs(velocity.x) > 0.1f;
        bool isJumping     = !isGrounded && velocity.y > 0.1f;
        bool isFalling     = !isGrounded && velocity.y < -0.1f;

        bool isArmed    = weaponStateManager != null && weaponStateManager.IsWeaponEquipped;
        bool isShooting = weaponStateManager != null && weaponStateManager.IsShooting;

        int jumpCount = 0;
        if (!isGrounded)
            jumpCount = controller.MaxAirJumps - controller.AirJumpsRemaining + 1;

        riggedAnimator.SetBool(IsMovingHash,      isMoving);
        riggedAnimator.SetBool(IsGroundedHash,    isGrounded);
        riggedAnimator.SetBool(IsJumpingHash,     isJumping);
        riggedAnimator.SetBool(IsFallingHash,     isFalling);
        riggedAnimator.SetBool(IsDashingHash,     isDashing);
        riggedAnimator.SetBool(IsWallSlidingHash, isWallSliding);
        riggedAnimator.SetBool(IsArmedHash,       isArmed);
        riggedAnimator.SetBool(IsShootingHash,    isShooting);
        riggedAnimator.SetBool(IsLevitatingHash,  isLevitating);
        riggedAnimator.SetBool(IsHackingHash,     isHacking);
        riggedAnimator.SetInteger(JumpCountHash,  jumpCount);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region FLIP

    private void HandleFlip()
    {
        bool shouldFaceRight = controller.FacingDirection > 0;
        if (shouldFaceRight == isFacingRight) return;

        isFacingRight = shouldFaceRight;

        riggedVisual.transform.localRotation = Quaternion.Euler(0f, isFacingRight ? 0f : 180f, 0f);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region MÉTODOS PÚBLICOS — Animaciones especiales

    /// <summary>
    /// Activa/desactiva el estado de levitación (ChakraFloat).
    /// El Animator Controller maneja el Start → Loop → End según cambios de este bool.
    /// </summary>
    public void SetLevitating(bool value)
    {
        isLevitating = value;
    }

    /// <summary>
    /// Activa/desactiva el estado de hackeo (ChakraRemoteHack).
    /// El Animator Controller maneja Hack Start → Hacking End.
    /// </summary>
    public void SetHacking(bool value)
    {
        isHacking = value;
    }

    /// <summary>
    /// Activa el trigger "Hit" para la animación de recibir daño.
    /// </summary>
    public void PlayHitAnimation()
    {
        if (riggedAnimator == null || riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar 'Hit'. Animator no asignado.");
            return;
        }
        riggedAnimator.SetTrigger(HitHash);
    }

    /// <summary>
    /// Activa el trigger "GroundPound" para la animación de Tremor (ChakraTremor).
    /// </summary>
    public void PlayGroundPoundAnimation()
    {
        StartCoroutine(PlayGroundPoundCoroutine());
    }

    private System.Collections.IEnumerator PlayGroundPoundCoroutine()
    {
        if (riggedAnimator == null || riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar 'GroundPound'. Animator no asignado.");
            yield break;
        }

        isPlayingChakraAnimation = true;
        chakraAnimationEndTime = Time.time + 1.0f;

        yield return null;

        riggedAnimator.ResetTrigger(GroundPoundHash);
        riggedAnimator.SetTrigger(GroundPoundHash);
        Debug.Log("[HybridAnimationController] Trigger 'GroundPound' activado.");
    }

    /// <summary>
    /// Activa un trigger arbitrario en el Animator (uso general para chakras/habilidades).
    /// </summary>
    public void PlayAnimationTrigger(string triggerName)
    {
        if (riggedAnimator == null || riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"[HybridAnimationController] No se pudo activar trigger '{triggerName}'.");
            return;
        }
        riggedAnimator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Fuerza un estado específico del Animator por nombre (debug / casos especiales).
    /// </summary>
    public void ForceAnimationSystem(bool useFrameByFrame)
    {
        // Mantenido por compatibilidad — ya solo existe el sistema rigged.
        // El parámetro no tiene efecto.
        Debug.LogWarning("[HybridAnimationController] ForceAnimationSystem() ya no tiene efecto. Solo existe sistema rigged.");
    }

    /// <summary>
    /// Siempre retorna false (ya no hay sistema Frame-by-Frame).
    /// Mantenido por compatibilidad con código existente.
    /// </summary>
    public bool IsUsingFrameByFrame() => false;

    #endregion
}
