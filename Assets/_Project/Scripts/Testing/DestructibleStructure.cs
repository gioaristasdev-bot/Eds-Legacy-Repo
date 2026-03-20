using UnityEngine;
using NABHI.Chakras.Abilities;

namespace NABHI.Testing
{
    /// <summary>
    /// Estructura destructible para testear el chakra Tremor.
    /// Implementa IDestructible.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DestructibleStructure : MonoBehaviour, IDestructible
    {
        [Header("Stats")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Visual")]
        [SerializeField] private Sprite intactSprite;
        [SerializeField] private Sprite damagedSprite;
        [SerializeField] private Sprite destroyedSprite;
        [SerializeField] private Color intactColor = Color.white;
        [SerializeField] private Color damagedColor = new Color(0.8f, 0.6f, 0.4f);

        [Header("Destruction Effects")]
        [SerializeField] private ParticleSystem destructionParticles;
        [SerializeField] private GameObject[] debrisPrefabs;
        [SerializeField] private int debrisCount = 3;
        [SerializeField] private float debrisForce = 5f;

        [Header("Audio")]
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip destroySound;

        [Header("Behavior")]
        [SerializeField] private bool disableColliderOnDestroy = true;
        [SerializeField] private bool hideOnDestroy = true;
        [SerializeField] private bool fadeOutOnDestroy = true;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float destroyDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private SpriteRenderer spriteRenderer;
        private Collider2D structureCollider;
        private AudioSource audioSource;
        private bool isDestroyed;

        public bool IsDestroyed => isDestroyed;
        public float HealthPercent => (float)currentHealth / maxHealth;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            structureCollider = GetComponent<Collider2D>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null && (damageSound != null || destroySound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            currentHealth = maxHealth;
            UpdateVisual();
        }

        public void TakeDamage(int damage)
        {
            if (isDestroyed) return;

            currentHealth -= damage;

            if (showDebugInfo)
            {
                Debug.Log($"[DestructibleStructure] {gameObject.name} recibio {damage} de dano. Salud: {currentHealth}/{maxHealth}");
            }

            // Sonido de dano
            if (damageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(damageSound);
            }

            UpdateVisual();

            if (currentHealth <= 0)
            {
                Destroy();
            }
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null) return;

            float healthPercent = HealthPercent;

            if (healthPercent > 0.5f)
            {
                // Intacto
                if (intactSprite != null)
                {
                    spriteRenderer.sprite = intactSprite;
                }
                spriteRenderer.color = intactColor;
            }
            else if (healthPercent > 0)
            {
                // Danado
                if (damagedSprite != null)
                {
                    spriteRenderer.sprite = damagedSprite;
                }
                spriteRenderer.color = damagedColor;
            }
        }

        private void Destroy()
        {
            if (isDestroyed) return;

            isDestroyed = true;

            if (showDebugInfo)
            {
                Debug.Log($"[DestructibleStructure] {gameObject.name} ha sido destruido!");
            }

            // Sonido de destruccion
            if (destroySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(destroySound);
            }

            // Particulas
            if (destructionParticles != null)
            {
                destructionParticles.Play();
            }

            // Generar escombros
            SpawnDebris();

            // Cambiar sprite a destruido
            if (spriteRenderer != null && destroyedSprite != null)
            {
                spriteRenderer.sprite = destroyedSprite;
            }

            // Desactivar collider inmediatamente
            if (disableColliderOnDestroy && structureCollider != null)
            {
                structureCollider.enabled = false;
            }

            // Fade out visual o ocultar
            if (fadeOutOnDestroy && spriteRenderer != null)
            {
                StartCoroutine(FadeOutAndDestroy());
            }
            else if (hideOnDestroy && spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
                if (destroyDelay > 0)
                {
                    Destroy(gameObject, destroyDelay);
                }
            }
            else if (destroyDelay > 0)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            if (spriteRenderer == null) yield break;

            Color startColor = spriteRenderer.color;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            // Ocultar completamente
            spriteRenderer.enabled = false;

            // Destruir el objeto
            if (destroyDelay > 0)
            {
                Destroy(gameObject, destroyDelay);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SpawnDebris()
        {
            if (debrisPrefabs == null || debrisPrefabs.Length == 0) return;

            for (int i = 0; i < debrisCount; i++)
            {
                GameObject debrisPrefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
                if (debrisPrefab == null) continue;

                Vector3 spawnPos = transform.position + new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    Random.Range(-0.5f, 0.5f),
                    0
                );

                GameObject debris = Instantiate(debrisPrefab, spawnPos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));

                Rigidbody2D debrisRb = debris.GetComponent<Rigidbody2D>();
                if (debrisRb != null)
                {
                    Vector2 randomDir = Random.insideUnitCircle.normalized;
                    debrisRb.AddForce(randomDir * debrisForce, ForceMode2D.Impulse);
                    debrisRb.AddTorque(Random.Range(-10f, 10f), ForceMode2D.Impulse);
                }

                // Auto-destruir escombros
                Destroy(debris, 3f);
            }
        }

        /// <summary>
        /// Reparar la estructura (para testing)
        /// </summary>
        public void Repair()
        {
            currentHealth = maxHealth;
            isDestroyed = false;

            if (structureCollider != null)
            {
                structureCollider.enabled = true;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }

            UpdateVisual();

            if (showDebugInfo)
            {
                Debug.Log($"[DestructibleStructure] {gameObject.name} reparado.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isDestroyed ? Color.gray : (HealthPercent > 0.5f ? Color.green : Color.yellow);

            if (structureCollider != null)
            {
                Gizmos.DrawWireCube(structureCollider.bounds.center, structureCollider.bounds.size);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one);
            }
        }
    }
}
