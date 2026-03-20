using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 4: Eco Sensitivo
    /// Color: Naranja - Sacro (Sexualidad y Creatividad)
    ///
    /// Desde puntos especificos del mapa (orbes etereos), el personaje puede
    /// activar este chakra para revelar zonas y caminos ocultos.
    /// </summary>
    public class ChakraEchoSense : ChakraBase
    {
        [Header("Echo Sense Settings")]
        [Tooltip("Radio de deteccion de puntos de eco")]
        [SerializeField] private float detectionRadius = 3f;

        [Tooltip("Duracion de la revelacion")]
        [SerializeField] private float revealDuration = 10f;

        [Tooltip("Radio de revelacion de zonas ocultas")]
        [SerializeField] private float revealRadius = 15f;

        [Header("Layers")]
        [SerializeField] private LayerMask echoPointLayer;
        [SerializeField] private LayerMask hiddenZoneLayer;

        [Header("Visual")]
        [SerializeField] private ParticleSystem echoWaveParticles;
        [SerializeField] private Color revealedColor = new Color(1f, 0.6f, 0.2f, 0.5f);

        // Estado
        private EchoPoint currentEchoPoint;
        private List<HiddenZone> revealedZones = new List<HiddenZone>();
        private Coroutine revealCoroutine;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.EchoSense;
            chakraName = "Eco Sensitivo";
            chakraColor = new Color(1f, 0.6f, 0.2f); // Naranja
            activationMode = ChakraActivationMode.Contextual;
            cooldown = 1f;
        }

        protected override void Update()
        {
            base.Update();

            // Buscar puntos de eco cercanos
            DetectEchoPoints();
        }

        private void DetectEchoPoints()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, echoPointLayer);

            if (hits.Length > 0)
            {
                // Encontrar el mas cercano
                float closestDist = float.MaxValue;
                EchoPoint closest = null;

                foreach (var hit in hits)
                {
                    var echoPoint = hit.GetComponent<EchoPoint>();
                    if (echoPoint != null && echoPoint.IsActive)
                    {
                        float dist = Vector2.Distance(transform.position, hit.transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = echoPoint;
                        }
                    }
                }

                currentEchoPoint = closest;
            }
            else
            {
                currentEchoPoint = null;
            }
        }

        public override bool HasValidTarget()
        {
            return currentEchoPoint != null;
        }

        public override bool CanActivate()
        {
            if (!base.CanActivate())
                return false;

            return HasValidTarget();
        }

        protected override void OnActivate()
        {
            if (currentEchoPoint == null)
            {
                Debug.LogWarning("[ChakraEchoSense] No hay punto de eco valido!");
                return;
            }

            // Revelar zonas ocultas
            RevealHiddenZones(currentEchoPoint.transform.position);

            // Marcar el punto de eco como usado (si es de un solo uso)
            currentEchoPoint.OnUsed();

            // Efecto visual
            if (echoWaveParticles != null)
            {
                echoWaveParticles.transform.position = currentEchoPoint.transform.position;
                echoWaveParticles.Play();
            }

            // Desactivar inmediatamente (es instantaneo)
            StartCoroutine(InstantDeactivate());
        }

        private IEnumerator InstantDeactivate()
        {
            yield return null;
            isActive = false;
        }

        private void RevealHiddenZones(Vector2 origin)
        {
            // Encontrar zonas ocultas en el radio
            Collider2D[] hiddenColliders = Physics2D.OverlapCircleAll(origin, revealRadius, hiddenZoneLayer);

            foreach (var collider in hiddenColliders)
            {
                var hiddenZone = collider.GetComponent<HiddenZone>();
                if (hiddenZone != null && !hiddenZone.IsRevealed)
                {
                    hiddenZone.Reveal(revealDuration);
                    revealedZones.Add(hiddenZone);
                }
            }

            Debug.Log($"[ChakraEchoSense] Reveladas {hiddenColliders.Length} zonas ocultas");

            // Iniciar temporizador para ocultar
            if (revealCoroutine != null)
                StopCoroutine(revealCoroutine);

            revealCoroutine = StartCoroutine(HideZonesAfterDuration());
        }

        private IEnumerator HideZonesAfterDuration()
        {
            yield return new WaitForSeconds(revealDuration);

            foreach (var zone in revealedZones)
            {
                if (zone != null)
                {
                    zone.Hide();
                }
            }

            revealedZones.Clear();
        }

        protected override void OnDeactivate()
        {
            // No hace nada especial
        }

        protected override string GetChakraDescription()
        {
            return "Revela zonas y caminos ocultos desde puntos de eco especiales en el mapa.";
        }

        /// <summary>
        /// Indica si hay un punto de eco cercano
        /// </summary>
        public bool HasNearbyEchoPoint => currentEchoPoint != null;
        public EchoPoint NearbyEchoPoint => currentEchoPoint;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = chakraColor;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            if (currentEchoPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, currentEchoPoint.transform.position);
            }
        }
    }

    /// <summary>
    /// Componente para puntos de eco en el mapa
    /// </summary>
    public class EchoPoint : MonoBehaviour
    {
        [SerializeField] private bool singleUse = false;
        [SerializeField] private ParticleSystem idleParticles;

        private bool isUsed = false;

        public bool IsActive => !isUsed || !singleUse;

        public void OnUsed()
        {
            if (singleUse)
            {
                isUsed = true;
                if (idleParticles != null)
                    idleParticles.Stop();
            }
        }
    }

    /// <summary>
    /// Componente para zonas ocultas que pueden ser reveladas
    /// </summary>
    public class HiddenZone : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] hiddenSprites;
        [SerializeField] private Collider2D[] hiddenColliders;
        [SerializeField] private GameObject visualIndicator;

        private bool isRevealed = false;

        public bool IsRevealed => isRevealed;

        public void Reveal(float duration)
        {
            isRevealed = true;

            // Mostrar sprites
            foreach (var sprite in hiddenSprites)
            {
                if (sprite != null)
                {
                    sprite.enabled = true;
                    // Efecto de fade in
                    StartCoroutine(FadeIn(sprite, 0.5f));
                }
            }

            // Activar colliders
            foreach (var col in hiddenColliders)
            {
                if (col != null)
                    col.enabled = true;
            }

            // Ocultar indicador
            if (visualIndicator != null)
                visualIndicator.SetActive(false);
        }

        public void Hide()
        {
            isRevealed = false;

            // Ocultar sprites
            foreach (var sprite in hiddenSprites)
            {
                if (sprite != null)
                {
                    StartCoroutine(FadeOut(sprite, 0.5f));
                }
            }

            // Desactivar colliders
            foreach (var col in hiddenColliders)
            {
                if (col != null)
                    col.enabled = false;
            }

            // Mostrar indicador
            if (visualIndicator != null)
                visualIndicator.SetActive(true);
        }

        private IEnumerator FadeIn(SpriteRenderer sprite, float duration)
        {
            Color color = sprite.color;
            color.a = 0;
            sprite.color = color;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0, 1, elapsed / duration);
                sprite.color = color;
                yield return null;
            }
        }

        private IEnumerator FadeOut(SpriteRenderer sprite, float duration)
        {
            Color color = sprite.color;
            float startAlpha = color.a;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, 0, elapsed / duration);
                sprite.color = color;
                yield return null;
            }

            sprite.enabled = false;
        }
    }
}
