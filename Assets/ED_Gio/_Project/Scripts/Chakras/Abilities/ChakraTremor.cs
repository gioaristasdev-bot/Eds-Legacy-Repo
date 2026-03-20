using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;
using NABHI.Character;
using NABHI.Weapons;
using Cinemachine;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 3: Temblor
    /// Color: Verde - Corazon (Amor y Cura)
    ///
    /// Crea un fuerte temblor en un area corta alrededor del personaje.
    /// - Noquea temporalmente a los enemigos
    /// - Destruye estructuras fragiles en el entorno
    /// </summary>
    public class ChakraTremor : ChakraBase
    {
        [Header("Tremor Settings")]
        [Tooltip("Radio del efecto de temblor")]
        [SerializeField] private float tremblRadius = 5f;

        [Tooltip("Duracion del aturdimiento en enemigos")]
        [SerializeField] private float stunDuration = 2f;

        [Tooltip("Fuerza del knockback aplicado")]
        [SerializeField] private float knockbackForce = 8f;

        [Header("Damage")]
        [Tooltip("Dano aplicado a enemigos")]
        [SerializeField] private int damage = 10;

        [Tooltip("Dano a estructuras fragiles")]
        [SerializeField] private int structureDamage = 50;

        [Tooltip("Destruir estructuras instantaneamente (ignorar vida)")]
        [SerializeField] private bool instantDestroyStructures = true;

        [Header("Layers")]
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask destructibleLayer;

        [Header("Animation")]
        [Tooltip("Referencia al HybridAnimationController para la animacion de golpe")]
        [SerializeField] private HybridAnimationController animationController;
        [Tooltip("Nombre del trigger de animacion de golpe al suelo")]
        [SerializeField] private string groundPoundTrigger = "GroundPound";
        [Tooltip("Tiempo de espera para la animacion antes del impacto")]
        [SerializeField] private float animationWindupTime = 0.4f;

        [Header("Visual")]
        [SerializeField] private ParticleSystem tremblParticles;

        [Header("Camera Shake (Cinemachine)")]
        [Tooltip("ImpulseSource propio del Tremor. Si no se asigna, se crea automaticamente.")]
        [SerializeField] private CinemachineImpulseSource tremorImpulseSource;
        [Tooltip("Fuerza del shake para el Tremor (recomendado: 2.0 - 4.0, mas fuerte que un golpe normal)")]
        [SerializeField] private float tremorShakeForce = 3.0f;
        [Tooltip("Tiempo que el personaje queda bloqueado despues del impacto")]
        [SerializeField] private float postImpactLockDuration = 0.75f;

        [Header("Audio")]
        [SerializeField] private AudioClip tremblSound;
        [SerializeField] private AudioClip groundPoundSound;

        private AudioSource audioSource;
        private Animator playerAnimator;
        private static readonly int GroundPoundHash = Animator.StringToHash("GroundPound");

        // Referencias para verificar condiciones de activacion
        private CharacterController2D characterController;
        private WeaponStateManager weaponStateManager;

        // Estado de ejecución (para bloquear input del personaje)
        private bool isExecutingTremor = false;

        /// <summary>
        /// Indica si el Tremor está en ejecución (bloquea otras acciones)
        /// </summary>
        public bool IsExecutingTremor => isExecutingTremor;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.Tremor;
            chakraName = "Temblor";
            chakraColor = new Color(0.2f, 0.8f, 0.4f); // Verde
            activationMode = ChakraActivationMode.Instant;
            cooldown = 3f;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Buscar referencias de animacion
            if (animationController == null)
            {
                animationController = GetComponentInParent<HybridAnimationController>();
            }

            // Crear CinemachineImpulseSource propio si no esta asignado
            if (tremorImpulseSource == null)
            {
                tremorImpulseSource = GetComponent<CinemachineImpulseSource>();
            }
            if (tremorImpulseSource == null)
            {
                tremorImpulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
                // Configurar duracion mas larga que el hit normal (sustain + decay)
                tremorImpulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = 0.4f;
                tremorImpulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_DecayTime = 0.8f;
                Debug.Log("[ChakraTremor] CinemachineImpulseSource creado automaticamente. Ajusta la duracion en el Inspector.");
            }

            // Buscar animator directamente si no hay HybridAnimationController
            playerAnimator = GetComponentInParent<Animator>();

            // Buscar referencias para condiciones de activacion
            characterController = GetComponentInParent<CharacterController2D>();
            weaponStateManager = GetComponentInParent<WeaponStateManager>();
        }

        /// <summary>
        /// El Tremor solo puede activarse si:
        /// - Esta en el suelo
        /// - No esta haciendo dash
        /// - No esta disparando
        /// - No esta en wall slide
        /// </summary>
        public override bool CanActivate()
        {
            // Verificar condiciones base (energia, cooldown, desbloqueado)
            if (!base.CanActivate())
            {
                return false;
            }

            // DEBE estar en el suelo
            if (characterController != null && !characterController.IsGrounded)
            {
                Debug.Log("[ChakraTremor] No se puede activar: no esta en el suelo");
                return false;
            }

            // NO puede estar haciendo dash
            if (characterController != null && characterController.IsDashing)
            {
                Debug.Log("[ChakraTremor] No se puede activar: esta en dash");
                return false;
            }

            // NO puede estar en wall slide
            if (characterController != null && characterController.IsWallSliding)
            {
                Debug.Log("[ChakraTremor] No se puede activar: esta en wall slide");
                return false;
            }

            // NO puede estar disparando
            if (weaponStateManager != null && weaponStateManager.IsShooting)
            {
                Debug.Log("[ChakraTremor] No se puede activar: esta disparando");
                return false;
            }

            return true;
        }

        protected override void OnActivate()
        {
            // Iniciar secuencia: animacion -> impacto -> temblor
            StartCoroutine(TremorSequence());
        }

        private IEnumerator TremorSequence()
        {
            // BLOQUEAR input del personaje durante la ejecución
            isExecutingTremor = true;

            // Detener movimiento del personaje
            StopCharacterMovement();

            // PASO 1: Reproducir animacion de golpe al suelo
            PlayGroundPoundAnimation();

            // Sonido de anticipacion del golpe
            if (groundPoundSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(groundPoundSound);
            }

            // PASO 2: Esperar el tiempo de windup de la animacion
            yield return new WaitForSeconds(animationWindupTime);

            // PASO 3: Ejecutar el temblor en el momento del impacto
            ExecuteTrembl();

            // PASO 4: Esperar despues del impacto antes de desbloquear
            yield return new WaitForSeconds(postImpactLockDuration);

            // DESBLOQUEAR input del personaje
            isExecutingTremor = false;

            // Los chakras instantaneos se "desactivan" inmediatamente
            isActive = false;
        }

        private void StopCharacterMovement()
        {
            // Detener el rigidbody del personaje
            Rigidbody2D rb = GetComponentInParent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(0, rb.velocity.y); // Solo detener movimiento horizontal
            }
        }

        private void PlayGroundPoundAnimation()
        {
            // OPCION 1: Usar HybridAnimationController (recomendado)
            if (animationController != null)
            {
                animationController.PlayGroundPoundAnimation();
                Debug.Log("[ChakraTremor] Animacion GroundPound activada via HybridAnimationController");
                return;
            }

            // OPCION 2: Fallback - buscar animator directamente
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger(GroundPoundHash);
                Debug.Log("[ChakraTremor] Animacion GroundPound activada via Animator directo");
            }
            else
            {
                Debug.LogWarning("[ChakraTremor] No se encontro Animator ni HybridAnimationController para la animacion de golpe");
            }
        }

        private void ExecuteTrembl()
        {
            Vector2 origin = transform.position;

            // Detectar enemigos
            Collider2D[] enemies = Physics2D.OverlapCircleAll(origin, tremblRadius, enemyLayer);
            foreach (var enemyCollider in enemies)
            {
                ApplyTremblToEnemy(enemyCollider, origin);
            }

            // Detectar estructuras destruibles
            Collider2D[] destructibles = Physics2D.OverlapCircleAll(origin, tremblRadius, destructibleLayer);
            foreach (var destructible in destructibles)
            {
                ApplyTremblToStructure(destructible);
            }

            // Efectos visuales
            if (tremblParticles != null)
            {
                tremblParticles.Play();
            }

            // Screen shake
            TriggerScreenShake();

            // Sonido
            if (tremblSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(tremblSound);
            }

            Debug.Log($"[ChakraTremor] Temblor ejecutado! Enemigos afectados: {enemies.Length}, Estructuras: {destructibles.Length}");
        }

        private void ApplyTremblToEnemy(Collider2D enemyCollider, Vector2 origin)
        {
            // Aplicar dano si tiene interface de dano
            var damageable = enemyCollider.GetComponent<Character.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            // Aplicar knockback
            Rigidbody2D rb = enemyCollider.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = ((Vector2)enemyCollider.transform.position - origin).normalized;
                rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            }

            // Aplicar stun si tiene componente de enemigo
            var stunnable = enemyCollider.GetComponent<IStunnable>();
            if (stunnable != null)
            {
                stunnable.ApplyStun(stunDuration);
            }
        }

        private void ApplyTremblToStructure(Collider2D structureCollider)
        {
            // Buscar componente de estructura destruible
            var destructible = structureCollider.GetComponent<IDestructible>();
            if (destructible != null)
            {
                if (instantDestroyStructures)
                {
                    // Destruir instantaneamente aplicando dano masivo
                    destructible.TakeDamage(9999);
                }
                else
                {
                    destructible.TakeDamage(structureDamage);
                }
            }
        }

        private void TriggerScreenShake()
        {
            // Usar ImpulseSource propio del Tremor (duracion independiente del hit)
            if (tremorImpulseSource != null)
            {
                tremorImpulseSource.GenerateImpulse(tremorShakeForce);
                Debug.Log($"[ChakraTremor] Camera shake via Cinemachine Impulse con fuerza: {tremorShakeForce}");
            }
            else
            {
                Debug.LogWarning("[ChakraTremor] CinemachineImpulseSource no encontrado.");
            }
        }

        protected override void OnDeactivate()
        {
            // No hace nada especial al desactivar (es instantaneo)
        }

        protected override string GetChakraDescription()
        {
            return $"Crea un temblor que noquea enemigos y destruye estructuras fragiles en un radio de {tremblRadius} unidades.";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = chakraColor;
            Gizmos.DrawWireSphere(transform.position, tremblRadius);
        }
    }

    /// <summary>
    /// Interface para objetos que pueden ser aturdidos
    /// </summary>
    public interface IStunnable
    {
        void ApplyStun(float duration);
        bool IsStunned { get; }
    }

    /// <summary>
    /// Interface para objetos destruibles
    /// </summary>
    public interface IDestructible
    {
        void TakeDamage(int damage);
        bool IsDestroyed { get; }
    }
}
