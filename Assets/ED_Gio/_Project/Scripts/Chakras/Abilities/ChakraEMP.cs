using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 6: Pulso Electromagnetico (PEM)
    /// Color: Azul Claro - Garganta (Comunicacion)
    ///
    /// Emite un pulso que incapacita a los enemigos electronicos/mecanicos.
    /// La duracion e intensidad pueden mejorarse con amuletos.
    /// </summary>
    public class ChakraEMP : ChakraBase
    {
        [Header("EMP Settings")]
        [Tooltip("Radio del pulso EMP")]
        [SerializeField] private float empRadius = 6f;

        [Tooltip("Duracion del efecto de incapacitacion")]
        [SerializeField] private float disableDuration = 3f;

        [Tooltip("Dano a enemigos electronicos")]
        [SerializeField] private int empDamage = 15;

        [Header("Mejoras (modificadas por amuletos)")]
        [SerializeField] private float durationMultiplier = 1f;
        [SerializeField] private float radiusMultiplier = 1f;

        [Header("Layers")]
        [SerializeField] private LayerMask electronicEnemyLayer;
        [SerializeField] private LayerMask mechanismLayer;

        [Header("Visual")]
        [SerializeField] private ParticleSystem empPulseParticles;
        [SerializeField] private float pulseExpandSpeed = 20f;
        [SerializeField] private LineRenderer pulseWaveRenderer;

        [Header("Audio")]
        [SerializeField] private AudioClip empSound;

        private AudioSource audioSource;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.EMP;
            chakraName = "Pulso Electromagnetico";
            chakraColor = new Color(0.4f, 0.8f, 1f); // Azul claro
            activationMode = ChakraActivationMode.Instant;
            cooldown = 5f;
            energyCostPerUse = 35f;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        protected override void OnActivate()
        {
            // Ejecutar pulso EMP
            ExecuteEMP();

            // Desactivar inmediatamente
            StartCoroutine(InstantDeactivate());
        }

        private IEnumerator InstantDeactivate()
        {
            yield return null;
            isActive = false;
        }

        private void ExecuteEMP()
        {
            float effectiveRadius = empRadius * radiusMultiplier;
            float effectiveDuration = disableDuration * durationMultiplier;

            Vector2 origin = transform.position;

            // Detectar enemigos electronicos
            Collider2D[] enemies = Physics2D.OverlapCircleAll(origin, effectiveRadius, electronicEnemyLayer);
            foreach (var enemyCollider in enemies)
            {
                ApplyEMPToEnemy(enemyCollider, origin, effectiveDuration);
            }

            // Detectar mecanismos
            Collider2D[] mechanisms = Physics2D.OverlapCircleAll(origin, effectiveRadius, mechanismLayer);
            foreach (var mechanism in mechanisms)
            {
                ApplyEMPToMechanism(mechanism, effectiveDuration);
            }

            // Efectos visuales
            StartCoroutine(AnimatePulseWave(effectiveRadius));

            if (empPulseParticles != null)
            {
                var main = empPulseParticles.main;
                main.startSize = effectiveRadius * 2;
                empPulseParticles.Play();
            }

            // Sonido
            if (empSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(empSound);
            }

            Debug.Log($"[ChakraEMP] PEM ejecutado! Radio: {effectiveRadius}, Duracion: {effectiveDuration}s, Afectados: {enemies.Length + mechanisms.Length}");
        }

        private void ApplyEMPToEnemy(Collider2D enemyCollider, Vector2 origin, float duration)
        {
            // Aplicar dano
            var damageable = enemyCollider.GetComponent<Character.IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(empDamage);
            }

            // Aplicar efecto EMP (deshabilitar)
            var empTarget = enemyCollider.GetComponent<IEMPTarget>();
            if (empTarget != null)
            {
                empTarget.ApplyEMPEffect(duration);
            }

            // Tambien puede actuar como stun
            var stunnable = enemyCollider.GetComponent<IStunnable>();
            if (stunnable != null)
            {
                stunnable.ApplyStun(duration);
            }
        }

        private void ApplyEMPToMechanism(Collider2D mechanismCollider, float duration)
        {
            var empTarget = mechanismCollider.GetComponent<IEMPTarget>();
            if (empTarget != null)
            {
                empTarget.ApplyEMPEffect(duration);
            }
        }

        private IEnumerator AnimatePulseWave(float maxRadius)
        {
            if (pulseWaveRenderer == null) yield break;

            pulseWaveRenderer.enabled = true;
            float currentRadius = 0f;

            // Crear circulo
            int segments = 36;
            pulseWaveRenderer.positionCount = segments + 1;

            while (currentRadius < maxRadius)
            {
                currentRadius += pulseExpandSpeed * Time.deltaTime;

                // Actualizar posiciones del circulo
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * currentRadius;
                    float y = Mathf.Sin(angle) * currentRadius;
                    pulseWaveRenderer.SetPosition(i, transform.position + new Vector3(x, y, 0));
                }

                // Fade out
                float alpha = 1f - (currentRadius / maxRadius);
                Color color = chakraColor;
                color.a = alpha;
                pulseWaveRenderer.startColor = color;
                pulseWaveRenderer.endColor = color;

                yield return null;
            }

            pulseWaveRenderer.enabled = false;
        }

        protected override void OnDeactivate()
        {
            // No hace nada especial
        }

        /// <summary>
        /// Aplicar mejora de amuleto a la duracion
        /// </summary>
        public void ApplyDurationBonus(float multiplier)
        {
            durationMultiplier = multiplier;
        }

        /// <summary>
        /// Aplicar mejora de amuleto al radio
        /// </summary>
        public void ApplyRadiusBonus(float multiplier)
        {
            radiusMultiplier = multiplier;
        }

        protected override string GetChakraDescription()
        {
            return $"Emite un pulso que incapacita enemigos electronicos en un radio de {empRadius * radiusMultiplier} unidades durante {disableDuration * durationMultiplier} segundos.";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = chakraColor;
            Gizmos.DrawWireSphere(transform.position, empRadius * radiusMultiplier);
        }
    }

    /// <summary>
    /// Interface para objetivos afectados por EMP
    /// </summary>
    public interface IEMPTarget
    {
        void ApplyEMPEffect(float duration);
        bool IsDisabledByEMP { get; }
    }
}
