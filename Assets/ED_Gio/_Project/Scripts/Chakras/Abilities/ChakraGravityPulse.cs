using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace NABHI.Chakras.Abilities
{
    /// <summary>
    /// Chakra 7 (Opcion 2): Pulso Gravitacional
    /// Color: Rojo - Raiz (Confianza)
    ///
    /// Ralentiza el tiempo en un area alrededor del personaje.
    /// Afecta a enemigos, plataformas, objetos, proyectiles y elementos etereos.
    /// El jugador NO es afectado.
    /// </summary>
    public class ChakraGravityPulse : ChakraBase
    {
        [Header("Gravity Pulse Settings")]
        [Tooltip("Radio del efecto de ralentizacion")]
        [SerializeField] private float pulseRadius = 8f;

        [Tooltip("Duracion del efecto")]
        [SerializeField] private float pulseDuration = 3f;

        [Tooltip("Factor de ralentizacion (0.1 = 10% velocidad)")]
        [SerializeField] private float slowFactor = 0.2f;

        [Header("Layers afectados")]
        [SerializeField] private LayerMask affectedLayers;

        [Header("Visual")]
        [SerializeField] private ParticleSystem pulseParticles;
        [SerializeField] private GameObject slowFieldVisual;
        [SerializeField] private Material slowFieldMaterial;
        [SerializeField] private Color slowFieldColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);

        // Estado
        private bool isPulseActive;
        private float pulseTimer;
        private List<SlowedObject> slowedObjects = new List<SlowedObject>();
        private SpriteRenderer fieldRenderer;

        // Propiedades
        public bool IsPulseActive => isPulseActive;
        public float PulseTimeRemaining => pulseTimer;

        protected override void Awake()
        {
            base.Awake();

            // Configurar tipo
            chakraType = ChakraType.GravityPulse;
            chakraName = "Pulso Gravitacional";
            chakraColor = new Color(0.9f, 0.2f, 0.2f); // Rojo
            activationMode = ChakraActivationMode.Instant;
            cooldown = 8f;
            energyCostPerUse = 40f;

            // Crear visual del campo si no existe
            if (slowFieldVisual == null)
            {
                CreateSlowFieldVisual();
            }
        }

        private void CreateSlowFieldVisual()
        {
            slowFieldVisual = new GameObject("SlowFieldVisual");
            slowFieldVisual.transform.SetParent(transform);
            slowFieldVisual.transform.localPosition = Vector3.zero;

            fieldRenderer = slowFieldVisual.AddComponent<SpriteRenderer>();
            fieldRenderer.sprite = CreateCircleSprite();
            fieldRenderer.color = slowFieldColor;
            fieldRenderer.sortingOrder = -1;

            slowFieldVisual.transform.localScale = Vector3.one * pulseRadius * 2;
            slowFieldVisual.SetActive(false);
        }

        private Sprite CreateCircleSprite()
        {
            // Crear textura circular simple
            int size = 128;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist < radius)
                    {
                        float alpha = 1f - (dist / radius);
                        pixels[y * size + x] = new Color(1, 1, 1, alpha * 0.5f);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        protected override void Update()
        {
            base.Update();

            if (isPulseActive)
            {
                pulseTimer -= Time.deltaTime;

                // Actualizar objetos afectados
                UpdateSlowedObjects();

                // Actualizar visual
                UpdateFieldVisual();

                if (pulseTimer <= 0)
                {
                    EndPulse();
                }
            }
        }

        protected override void OnActivate()
        {
            StartPulse();

            // Los chakras instantaneos con duracion manejan su propio ciclo
            StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            yield return new WaitForSeconds(pulseDuration);

            if (isPulseActive)
            {
                EndPulse();
            }
        }

        private void StartPulse()
        {
            isPulseActive = true;
            pulseTimer = pulseDuration;

            // Detectar y ralentizar objetos
            DetectAndSlowObjects();

            // Activar visual
            if (slowFieldVisual != null)
            {
                slowFieldVisual.SetActive(true);
            }

            // Particulas
            if (pulseParticles != null)
            {
                pulseParticles.Play();
            }

            Debug.Log($"[ChakraGravityPulse] Pulso activado! Objetos afectados: {slowedObjects.Count}");
        }

        private void EndPulse()
        {
            isPulseActive = false;
            pulseTimer = 0;

            // Restaurar velocidades
            foreach (var slowed in slowedObjects)
            {
                slowed.Restore();
            }
            slowedObjects.Clear();

            // Desactivar visual
            if (slowFieldVisual != null)
            {
                slowFieldVisual.SetActive(false);
            }

            // Detener particulas
            if (pulseParticles != null)
            {
                pulseParticles.Stop();
            }

            // Desactivar chakra
            isActive = false;

            Debug.Log("[ChakraGravityPulse] Pulso terminado");
        }

        private void DetectAndSlowObjects()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pulseRadius, affectedLayers);

            foreach (var hit in hits)
            {
                // No afectar al jugador
                if (hit.transform.IsChildOf(transform.root) || hit.CompareTag("Player"))
                    continue;

                SlowedObject slowed = new SlowedObject(hit.gameObject, slowFactor);
                slowedObjects.Add(slowed);
            }
        }

        private void UpdateSlowedObjects()
        {
            // Verificar nuevos objetos que entran al area
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pulseRadius, affectedLayers);

            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(transform.root) || hit.CompareTag("Player"))
                    continue;

                // Verificar si ya esta en la lista
                bool alreadySlowed = false;
                foreach (var slowed in slowedObjects)
                {
                    if (slowed.Target == hit.gameObject)
                    {
                        alreadySlowed = true;
                        break;
                    }
                }

                if (!alreadySlowed)
                {
                    SlowedObject newSlowed = new SlowedObject(hit.gameObject, slowFactor);
                    slowedObjects.Add(newSlowed);
                }
            }

            // Restaurar objetos que salen del area
            for (int i = slowedObjects.Count - 1; i >= 0; i--)
            {
                var slowed = slowedObjects[i];
                if (slowed.Target == null)
                {
                    slowedObjects.RemoveAt(i);
                    continue;
                }

                float dist = Vector2.Distance(transform.position, slowed.Target.transform.position);
                if (dist > pulseRadius * 1.1f) // Un poco de tolerancia
                {
                    slowed.Restore();
                    slowedObjects.RemoveAt(i);
                }
            }
        }

        private void UpdateFieldVisual()
        {
            if (fieldRenderer == null) return;

            // Pulsar el alpha basado en el tiempo restante
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            Color color = slowFieldColor;
            color.a = slowFieldColor.a * (0.5f + pulse * 0.5f);
            fieldRenderer.color = color;
        }

        protected override void OnDeactivate()
        {
            if (isPulseActive)
            {
                EndPulse();
            }
        }

        protected override string GetChakraDescription()
        {
            return $"Ralentiza el tiempo en un area de {pulseRadius} unidades durante {pulseDuration} segundos. El jugador no es afectado.";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = chakraColor;
            Gizmos.DrawWireSphere(transform.position, pulseRadius);
        }
    }

    /// <summary>
    /// Clase helper para manejar objetos ralentizados
    /// </summary>
    public class SlowedObject
    {
        public GameObject Target { get; private set; }

        private Rigidbody2D rb;
        private Animator animator;
        private float originalAnimatorSpeed;
        private Vector2 originalVelocity;
        private float slowFactor;
        private bool isSlowed;

        // Para proyectiles
        private Rigidbody2D projectileRb;
        private Vector2 originalProjectileVelocity;

        public SlowedObject(GameObject target, float factor)
        {
            Target = target;
            slowFactor = factor;

            rb = target.GetComponent<Rigidbody2D>();
            animator = target.GetComponent<Animator>();

            ApplySlow();
        }

        public void ApplySlow()
        {
            if (isSlowed) return;
            isSlowed = true;

            // Ralentizar Rigidbody
            if (rb != null)
            {
                originalVelocity = rb.velocity;
                rb.velocity *= slowFactor;
                rb.gravityScale *= slowFactor;
            }

            // Ralentizar Animator
            if (animator != null)
            {
                originalAnimatorSpeed = animator.speed;
                animator.speed *= slowFactor;
            }

            // Efecto visual
            ApplyVisualEffect();
        }

        public void Restore()
        {
            if (!isSlowed) return;
            isSlowed = false;

            // Restaurar Rigidbody
            if (rb != null)
            {
                rb.gravityScale /= slowFactor;
                // No restaurar velocidad exacta, dejar que continue naturalmente
            }

            // Restaurar Animator
            if (animator != null)
            {
                animator.speed = originalAnimatorSpeed;
            }

            // Quitar efecto visual
            RemoveVisualEffect();
        }

        private void ApplyVisualEffect()
        {
            // Tint azulado/rojizo para indicar ralentizacion
            var renderers = Target.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                Color original = renderer.color;
                renderer.color = new Color(
                    original.r * 0.7f,
                    original.g * 0.7f,
                    original.b + 0.3f,
                    original.a
                );
            }
        }

        private void RemoveVisualEffect()
        {
            // Restaurar colores (simplificado - en produccion guardar colores originales)
            var renderers = Target.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                Color c = renderer.color;
                renderer.color = new Color(
                    Mathf.Min(1, c.r / 0.7f),
                    Mathf.Min(1, c.g / 0.7f),
                    Mathf.Max(0, c.b - 0.3f),
                    c.a
                );
            }
        }
    }
}
