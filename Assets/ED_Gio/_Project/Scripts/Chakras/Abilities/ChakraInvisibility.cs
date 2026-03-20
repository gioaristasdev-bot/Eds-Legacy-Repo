using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 2: Invisibilidad
    /// Color: Azul - Tercer Ojo (Conciencia)
    ///
    /// Vuelve invisible al personaje mientras esta activo.
    /// Los enemigos no pueden detectarlo visualmente.
    /// Consume energia a tasa 3N (3 veces mas que Float).
    /// </summary>
    public class ChakraInvisibility : ChakraBase
    {
        [Header("Invisibility Settings")]
        [Tooltip("Alpha del sprite mientras esta invisible (0 = completamente invisible)")]
        [SerializeField] private float invisibleAlpha = 0.2f;

        [Tooltip("Velocidad de transicion de visibilidad")]
        [SerializeField] private float fadeSpeed = 5f;

        [Tooltip("Se cancela al atacar?")]
        [SerializeField] private bool cancelOnAttack = true;

        [Tooltip("Se cancela al recibir dano?")]
        [SerializeField] private bool cancelOnDamage = true;

        [Header("Layer de deteccion enemiga")]
        [Tooltip("Layer al que cambia mientras esta invisible (para que enemigos no lo detecten)")]
        [SerializeField] private string invisibleLayerName = "InvisiblePlayer";

        [Header("Visual")]
        [SerializeField] private ParticleSystem invisibilityParticles;

        // Referencias
        private SpriteRenderer[] spriteRenderers;
        private int originalLayer;
        private Color[] originalColors;
        private float currentAlpha = 1f;
        private bool isTransitioning;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.Invisibility;
            chakraName = "Invisibilidad";
            chakraColor = new Color(0.2f, 0.4f, 1f); // Azul
            activationMode = ChakraActivationMode.Continuous;

            // Tasa de consumo 3N
            energyCostPerSecond = 30f;

            // Obtener sprites
            spriteRenderers = GetComponentsInParent<SpriteRenderer>();
            CacheOriginalColors();
        }

        private void CacheOriginalColors()
        {
            if (spriteRenderers != null)
            {
                originalColors = new Color[spriteRenderers.Length];
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    originalColors[i] = spriteRenderers[i].color;
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            // Transicion suave de alpha
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            float targetAlpha = isActive ? invisibleAlpha : 1f;

            if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
                ApplyAlpha(currentAlpha);
            }
        }

        private void ApplyAlpha(float alpha)
        {
            if (spriteRenderers == null) return;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null && originalColors != null && i < originalColors.Length)
                {
                    Color newColor = originalColors[i];
                    newColor.a = alpha;
                    spriteRenderers[i].color = newColor;
                }
            }
        }

        protected override void OnActivate()
        {
            // Guardar layer original
            originalLayer = transform.parent != null ? transform.parent.gameObject.layer : gameObject.layer;

            // Cambiar a layer invisible (si existe)
            int invisibleLayer = LayerMask.NameToLayer(invisibleLayerName);
            if (invisibleLayer != -1)
            {
                SetLayerRecursively(transform.parent != null ? transform.parent.gameObject : gameObject, invisibleLayer);
            }

            // Activar particulas
            if (invisibilityParticles != null)
            {
                invisibilityParticles.Play();
            }

            Debug.Log("[ChakraInvisibility] Personaje invisible");
        }

        protected override void OnDeactivate()
        {
            // Restaurar layer original
            SetLayerRecursively(transform.parent != null ? transform.parent.gameObject : gameObject, originalLayer);

            // Detener particulas
            if (invisibilityParticles != null)
            {
                invisibilityParticles.Stop();
            }

            Debug.Log("[ChakraInvisibility] Personaje visible");
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Llamar cuando el personaje ataca
        /// </summary>
        public void OnPlayerAttack()
        {
            if (isActive && cancelOnAttack)
            {
                Debug.Log("[ChakraInvisibility] Cancelado por ataque");
                Deactivate();
            }
        }

        /// <summary>
        /// Llamar cuando el personaje recibe dano
        /// </summary>
        public void OnPlayerDamaged()
        {
            if (isActive && cancelOnDamage)
            {
                Debug.Log("[ChakraInvisibility] Cancelado por dano");
                Deactivate();
            }
        }

        protected override string GetChakraDescription()
        {
            return "Vuelve invisible al personaje. Los enemigos no pueden detectarlo. Consume energia rapidamente.";
        }

        /// <summary>
        /// Indica si el personaje esta invisible
        /// </summary>
        public bool IsInvisible => isActive;
    }
}
