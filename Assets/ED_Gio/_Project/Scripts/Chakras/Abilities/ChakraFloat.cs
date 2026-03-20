using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 1: Float (Levitacion)
    /// Color: Rosa - Corona (Espiritualidad)
    ///
    /// Cuando el chakra esta activo:
    /// - Mantener Salto (Espacio) = Levitar hacia arriba (consume energia)
    /// - Soltar Salto = Descender lentamente (gravedad reducida)
    ///
    /// Activar con E, desactivar con E.
    /// </summary>
    public class ChakraFloat : ChakraBase
    {
        [Header("Float Settings")]
        [Tooltip("Fuerza de levitacion hacia arriba al mantener salto")]
        [SerializeField] private float floatForce = 12f;

        [Tooltip("Velocidad maxima de ascenso")]
        [SerializeField] private float maxUpwardSpeed = 5f;

        [Tooltip("Gravedad reducida mientras el chakra esta activo")]
        [SerializeField] private float floatGravityScale = 0.3f;

        [Tooltip("Velocidad de descenso maxima (caida lenta)")]
        [SerializeField] private float maxDescentSpeed = 1.5f;

        [Header("Input")]
        [Tooltip("Tecla para levitar (mientras el chakra esta activo)")]
        [SerializeField] private KeyCode floatKey = KeyCode.Space;

        [Header("Energy")]
        [Tooltip("Solo consume energia mientras levita activamente (mantiene salto)")]
        [SerializeField] private bool onlyConsumeWhileAscending = true;

        [Header("Visual")]
        [SerializeField] private ParticleSystem floatParticles;
        [SerializeField] private ParticleSystem ascendParticles;

        // Referencias
        private Rigidbody2D rb;
        private float originalGravityScale;
        private bool isAscending;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.Float;
            chakraName = "Levitacion";
            chakraColor = new Color(1f, 0.4f, 0.7f); // Rosa
            activationMode = ChakraActivationMode.Continuous;

            // Este chakra maneja su propia energia (solo consume al mantener Espacio)
            manualEnergyManagement = true;

            // Obtener referencias
            rb = GetComponentInParent<Rigidbody2D>();
        }

        private void Start()
        {
            if (rb != null)
            {
                originalGravityScale = rb.gravityScale;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (!isActive) return;

            // Detectar si esta manteniendo el boton de salto
            bool holdingFloat = Input.GetKey(floatKey);

            if (holdingFloat)
            {
                // Ascender
                ApplyAscendPhysics();

                if (!isAscending)
                {
                    isAscending = true;
                    OnStartAscending();
                }

                // Consumir energia solo mientras asciende
                if (onlyConsumeWhileAscending && energySystem != null)
                {
                    if (!energySystem.ConsumeEnergyPerSecond(energyCostPerSecond))
                    {
                        // Sin energia, desactivar chakra
                        Deactivate();
                        return;
                    }
                }
            }
            else
            {
                // Descender lentamente
                ApplyDescentPhysics();

                if (isAscending)
                {
                    isAscending = false;
                    OnStopAscending();
                }

                // Si no consume energia solo al ascender, detener el consumo
                if (onlyConsumeWhileAscending && energySystem != null)
                {
                    energySystem.StopUsingEnergy();
                }
            }
        }

        private void ApplyAscendPhysics()
        {
            if (rb == null) return;

            // Desactivar gravedad completamente mientras asciende
            rb.gravityScale = 0f;

            // Si está cayendo, cancelar la velocidad de caída primero
            if (rb.velocity.y < 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            // Aplicar velocidad de ascenso directamente (más responsivo que AddForce)
            float targetVelocityY = Mathf.MoveTowards(rb.velocity.y, maxUpwardSpeed, floatForce * Time.deltaTime);
            rb.velocity = new Vector2(rb.velocity.x, targetVelocityY);
        }

        private void ApplyDescentPhysics()
        {
            if (rb == null) return;

            // Gravedad reducida para caer lentamente
            rb.gravityScale = floatGravityScale;

            // Limitar velocidad de descenso (caida suave)
            if (rb.velocity.y < -maxDescentSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -maxDescentSpeed);
            }
        }

        private void OnStartAscending()
        {
            // Particulas de ascenso
            if (ascendParticles != null)
            {
                ascendParticles.Play();
            }
        }

        private void OnStopAscending()
        {
            // Detener particulas de ascenso
            if (ascendParticles != null)
            {
                ascendParticles.Stop();
            }
        }

        protected override void OnActivate()
        {
            if (rb != null)
            {
                originalGravityScale = rb.gravityScale;
                rb.gravityScale = floatGravityScale;
            }

            // Activar particulas base (aura de levitacion)
            if (floatParticles != null)
            {
                floatParticles.Play();
            }

            isAscending = false;
            Debug.Log("[ChakraFloat] Modo levitacion activado. Mantén Espacio para ascender.");
        }

        protected override void OnDeactivate()
        {
            // Restaurar gravedad normal
            if (rb != null)
            {
                rb.gravityScale = originalGravityScale;
            }

            // Detener todas las particulas
            if (floatParticles != null)
            {
                floatParticles.Stop();
            }

            if (ascendParticles != null)
            {
                ascendParticles.Stop();
            }

            // Detener consumo de energia
            if (energySystem != null)
            {
                energySystem.StopUsingEnergy();
            }

            isAscending = false;
            Debug.Log("[ChakraFloat] Modo levitacion desactivado.");
        }

        protected override string GetChakraDescription()
        {
            return "Activa el modo levitacion. Mantén Espacio para ascender, suelta para descender suavemente.";
        }

        /// <summary>
        /// Indica si el chakra Float esta activo
        /// </summary>
        public bool IsFloatModeActive => isActive;

        /// <summary>
        /// Indica si esta ascendiendo activamente (manteniendo salto)
        /// </summary>
        public bool IsAscending => isAscending;
    }
}
