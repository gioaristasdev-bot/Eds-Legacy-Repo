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

    [Tooltip("Opcional. Si esta, el flip sigue su VisualFacing en vez de la " +
             "direccion de movimiento, para poder apuntar hacia atras.")]
    [SerializeField] private AimController aimController;

    [Header("Flip Settings")]
    [SerializeField] private bool autoFlipSprite = true;

    [Header("Cadencia de la caminata")]
    [Tooltip("Velocidad horizontal (u/s) a la que el ciclo de andar se reproduce a velocidad 1. " +
             "Debe coincidir con el moveSpeed del CharacterController2D.")]
    [SerializeField] private float walkSpeedReference = 8f;

    [Tooltip("Velocidad horizontal (u/s) del sprint, a la que el blend tree usa el ciclo de " +
             "correr al 100%. Debe coincidir con moveSpeed * runSpeedMultiplier.")]
    [SerializeField] private float sprintSpeedReference = 12f;

    [Tooltip("Límites del multiplicador de velocidad de la animación. El techo es 1: por " +
             "encima de walkSpeedReference quien debe tomar el relevo es el clip de correr " +
             "vía locomotionBlend, no el ciclo de andar reproducido más rápido. Subirlo de 1 " +
             "hace que la velocidad horizontal, que oscila por física alrededor del valor de " +
             "referencia, acelere el ciclo de forma irregular.")]
    [SerializeField] private float minWalkPlaybackSpeed = 0.5f;
    [SerializeField] private float maxWalkPlaybackSpeed = 1f;

    [Tooltip("Al desplazarse en sentido contrario al lado que mira el personaje, el " +
             "ciclo de piernas se reproduce al reves y camina hacia atras de verdad. " +
             "Sin esto, apuntar a un lado y avanzar al otro produce el efecto moonwalk.")]
    [SerializeField] private bool reverseCycleWhenRetreating = true;

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
    private static readonly int DeathHash          = Animator.StringToHash("Death");
    private static readonly int MoveSpeedHash      = Animator.StringToHash("moveSpeed");
    private static readonly int LocomotionBlendHash = Animator.StringToHash("locomotionBlend");

    // Amortiguación de la mezcla andar->correr, en segundos.
    private const float LocomotionBlendDamp = 0.12f;

    // Capa de override del tren superior: mantiene los brazos en pose de disparo
    // mientras las piernas siguen con el ciclo de carrera. Peso 0 = balanceo normal.
    private const string UpperBodyLayerName  = "Upper Body Shoot";
    private const float  UpperBodyFadeSpeed  = 6f;   // unidades de peso por segundo
    private int   upperBodyLayer  = -1;
    private float upperBodyWeight = 0f;

    // === DIAGNOSTICO TEMPORAL — quitar cuando se cierre el bug del salto a Idle ===
    [Header("Diagnostico (temporal)")]
    [SerializeField] private bool logCambiosDeEstado = true;
    private int   diagEstadoPrevio = 0;
    private float diagMaxMoveSpeed = 0f;

    // --- Estado interno ---
    private bool isFacingRight = true;

    // Estado de animaciones especiales (set desde chakras)
    private bool isLevitating = false;
    private bool isHacking    = false;

    // -------------------------------------------------------------------------
    #region UNITY CALLBACKS

    void Start()
    {
        if (controller == null)
            controller = GetComponent<CharacterController2D>();

        if (weaponStateManager == null)
            weaponStateManager = GetComponent<WeaponStateManager>();

        if (aimController == null)
            aimController = GetComponent<AimController>();

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

        if (riggedAnimator != null)
        {
            for (int i = 0; i < riggedAnimator.layerCount; i++)
            {
                if (riggedAnimator.GetLayerName(i) != UpperBodyLayerName) continue;
                upperBodyLayer = i;
                break;
            }

            if (upperBodyLayer < 0)
                Debug.LogWarning($"[HybridAnimationController] No se encontró la capa '{UpperBodyLayerName}' " +
                                 "en el Animator Controller. Los brazos seguirán balanceándose al disparar en carrera.");
            else
                riggedAnimator.SetLayerWeight(upperBodyLayer, 0f);
        }
    }

    void Update()
    {
        if (controller == null || riggedAnimator == null) return;

        UpdateAnimatorParameters();

        if (logCambiosDeEstado)
            DiagnosticarEstado();
    }

    // Registra cada cambio de estado del Animator junto al contexto que lo provoco.
    // Sirve para localizar el salto a Idle mientras se camina.
    private void DiagnosticarEstado()
    {
        AnimatorStateInfo info = riggedAnimator.GetCurrentAnimatorStateInfo(0);
        float ms    = riggedAnimator.GetFloat(MoveSpeedHash);
        float blend = riggedAnimator.GetFloat(LocomotionBlendHash);

        if (ms > diagMaxMoveSpeed)
        {
            diagMaxMoveSpeed = ms;
            if (ms > 1.001f)
                Debug.LogError($"[DIAG] moveSpeed SUPERO 1: {ms:F3}  (max permitido {maxWalkPlaybackSpeed:F2}) " +
                               $"velX={controller.Velocity.x:F2}");
        }

        if (info.fullPathHash == diagEstadoPrevio) return;
        diagEstadoPrevio = info.fullPathHash;

        Debug.Log($"[DIAG] estado -> hash={info.fullPathHash} | velX={controller.Velocity.x:F2} " +
                  $"| isMoving={Mathf.Abs(controller.Velocity.x) > 0.1f} | grounded={controller.IsGrounded} " +
                  $"| moveSpeed={ms:F3} | blend={blend:F3} | pesoCapa1={(upperBodyLayer >= 0 ? riggedAnimator.GetLayerWeight(upperBodyLayer) : -1f):F2}");
    }

    // El flip se aplica en LateUpdate, NO en Update: los clips del rig tienen curvas
    // de transform de raíz, así que el Animator reescribe el TRS completo del objeto
    // que lo contiene (incluida la rotación, que vuelve a identidad) en su fase de
    // evaluación, posterior a Update. Escribiendo aquí, después de esa fase, la
    // rotación del flip sobrevive hasta el render.
    void LateUpdate()
    {
        if (controller == null || riggedVisual == null) return;

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

        float speedX = Mathf.Abs(velocity.x);

        // Mezcla andar -> correr. Los estados ED_Walk/ED_Walk 0 son blend trees 1D
        // sobre este parámetro: 0 = ciclo de andar, 1 = ciclo de correr.
        // Se amortigua para que el cambio de ciclo no salte de golpe.
        float blend = sprintSpeedReference > walkSpeedReference
            ? Mathf.InverseLerp(walkSpeedReference, sprintSpeedReference, speedX)
            : 0f;
        riggedAnimator.SetFloat(LocomotionBlendHash, blend, LocomotionBlendDamp, Time.deltaTime);

        // Cadencia dentro del ciclo, proporcional a la velocidad real, para que los
        // pasos no patinen a velocidades intermedias. Los estados lo usan como
        // Speed Multiplier en el Animator Controller.
        float playbackSpeed = walkSpeedReference > 0.01f
            ? speedX / walkSpeedReference
            : 1f;
        // Un multiplicador negativo hace que Unity reproduzca el estado hacia atras.
        // Se usa cuando la velocidad va contra el lado que mira el personaje, que es
        // lo que ocurre al avanzar en una direccion mientras se apunta a la contraria.
        // Solo afecta a ED_Walk y ED_Walk 0, los unicos estados con Speed Multiplier.
        int ladoVisual = aimController != null ? aimController.VisualFacing : controller.FacingDirection;
        bool retrocediendo = reverseCycleWhenRetreating && isMoving && (velocity.x * ladoVisual) < 0f;

        riggedAnimator.SetFloat(MoveSpeedHash,
            (retrocediendo ? -1f : 1f) *
            Mathf.Clamp(playbackSpeed, minWalkPlaybackSpeed, maxWalkPlaybackSpeed));

        // La capa de override pone los brazos en pose de disparo mientras las piernas
        // siguen con su animación en la capa base. Se activa en los dos casos que el
        // estado ED_Shoot no cubre: disparar en movimiento y disparar en el aire
        // (saltando o cayendo). Quieto y en suelo, ED_Shoot ya anima el cuerpo entero.
        if (upperBodyLayer >= 0)
        {
            bool disparoParcial = isShooting && (isMoving || !isGrounded);
            float targetWeight = disparoParcial ? 1f : 0f;
            upperBodyWeight = Mathf.MoveTowards(upperBodyWeight, targetWeight,
                                                UpperBodyFadeSpeed * Time.deltaTime);
            riggedAnimator.SetLayerWeight(upperBodyLayer, upperBodyWeight);
        }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region FLIP

    private void HandleFlip()
    {
        // El lado visual lo decide AimController cuando se esta apuntando, para que
        // apuntar hacia atras gire al personaje en vez de retorcerle los brazos.
        int facing = aimController != null ? aimController.VisualFacing : controller.FacingDirection;
        isFacingRight = facing > 0;

        // Se compara contra el transform real y no contra un flag cacheado: si algo
        // externo resetea la rotación (cambio de estado, respawn, swap de visual), el
        // flip se vuelve a aplicar solo en lugar de quedarse desincronizado para siempre.
        Quaternion target = Quaternion.Euler(0f, isFacingRight ? 0f : 180f, 0f);
        if (riggedVisual.transform.localRotation != target)
            riggedVisual.transform.localRotation = target;
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
    /// Activa el trigger "Death". El estado Death no tiene salida: Ed se queda
    /// en el último frame hasta que se recarga la escena o se hace Revive().
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (riggedAnimator == null || riggedAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[HybridAnimationController] No se pudo activar 'Death'. Animator no asignado.");
            return;
        }
        riggedAnimator.SetTrigger(DeathHash);
    }

    /// <summary>
    /// Saca a Ed del estado Death y lo devuelve al idle correspondiente.
    /// Necesario porque Death es un estado terminal (sin transiciones de salida).
    /// </summary>
    public void ResetDeathAnimation()
    {
        if (riggedAnimator == null || riggedAnimator.runtimeAnimatorController == null)
            return;

        riggedAnimator.ResetTrigger(DeathHash);

        bool isArmed = weaponStateManager != null && weaponStateManager.IsWeaponEquipped;
        riggedAnimator.Play(isArmed ? "ED_Idle" : "ED_Idle 0", 0, 0f);
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

        // Un frame de margen para que los bools de movimiento del Update ya estén
        // asentados antes de disparar el trigger.
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
