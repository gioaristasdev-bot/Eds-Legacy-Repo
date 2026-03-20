using UnityEngine;
using NABHI.Character;

namespace NABHI.Weapons
{
    /// <summary>
    /// Proyectil genérico para armas
    /// Puede ser bala, láser, flecha, etc.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        #region PARÁMETROS

        [Header("Configuración de Proyectil")]
        [Tooltip("Daño que hace el proyectil")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Velocidad del proyectil")]
        [SerializeField] private float speed = 20f;

        [Tooltip("Tiempo de vida antes de autodestruirse (segundos)")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("¿El proyectil atraviesa enemigos?")]
        [SerializeField] private bool piercing = false;

        [Tooltip("¿El proyectil es afectado por gravedad?")]
        [SerializeField] private bool affectedByGravity = false;

        [Header("Efectos Visuales")]
        [Tooltip("Prefab de efecto al impactar (opcional)")]
        [SerializeField] private GameObject hitEffectPrefab;

        [Tooltip("Trail Renderer (opcional)")]
        [SerializeField] private TrailRenderer trail;

        #endregion

        #region REFERENCIAS

        private Rigidbody2D rb;
        private Vector2 direction;
        private float spawnTime;
        private bool hasHit = false;

        #endregion

        #region PROPIEDADES PÚBLICAS

        public float Damage => damage;
        public Vector2 Direction => direction;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // Configurar Rigidbody
            rb.gravityScale = affectedByGravity ? 1f : 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            spawnTime = Time.time;
            // Debug.Log($"[Projectile] Spawned! Velocity: {rb.velocity}, Direction: {direction}, Speed: {speed}");
        }

        private void Update()
        {
            // Autodestruirse después del lifetime
            if (Time.time - spawnTime >= lifetime)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Debug.Log($"[Projectile] Triggered with: {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)})");

            // Ignorar si ya impactó (para piercing)
            if (hasHit && !piercing)
                return;

            // Detectar qué golpeó
            bool shouldDestroy = HandleCollision(collision);

            if (shouldDestroy)
            {
                // Debug.Log($"[Projectile] Destroying after hitting {collision.gameObject.name}");
                hasHit = true;
                DestroyProjectile();
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Inicializar proyectil con dirección y velocidad
        /// </summary>
        public void Initialize(Vector2 direction, float? customSpeed = null)
        {
            this.direction = direction.normalized;
            float projectileSpeed = customSpeed ?? speed;

            // Aplicar velocidad
            rb.velocity = this.direction * projectileSpeed;

            // Rotar proyectil para que apunte en la dirección correcta
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// Establecer daño del proyectil
        /// </summary>
        public void SetDamage(float newDamage)
        {
            damage = newDamage;
        }

        /// <summary>
        /// Establecer si el proyectil atraviesa enemigos
        /// </summary>
        public void SetPiercing(bool isPiercing)
        {
            piercing = isPiercing;
        }

        #endregion

        #region MÉTODOS PRIVADOS

        private bool HandleCollision(Collider2D collision)
        {
            // TODO: Implementar sistema de daño a enemigos
            // Por ahora solo detectamos contra qué chocó

            // Colisión con paredes/suelo
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                SpawnHitEffect(collision.ClosestPoint(transform.position));
                return true; // Destruir proyectil
            }

            // Colisión con enemigos (cuerpo principal o hurtbox child con layer "Enemies")
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemies"))
            {
                // GetComponentInParent funciona tanto si golpeamos el collider principal
                // del enemigo como si golpeamos una hurtbox en un child GameObject.
                IDamageable damageable = collision.GetComponentInParent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    damageable.TakeDamage(damage);
                }

                SpawnHitEffect(collision.ClosestPoint(transform.position));

                // Si es piercing, no destruir
                return !piercing;
            }

            // Ignorar colisión con Player (no queremos que nos disparemos a nosotros mismos)
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                return false; // No destruir
            }

            // Por defecto, destruir al impactar cualquier otra cosa
            return true;
        }

        private void SpawnHitEffect(Vector2 position)
        {
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
                Destroy(effect, 1f); // Destruir efecto después de 1 segundo
            }
        }

        private void DestroyProjectile()
        {
            // Desactivar trail antes de destruir (para que no desaparezca bruscamente)
            if (trail != null)
            {
                trail.transform.SetParent(null);
                trail.autodestruct = true;
            }

            Destroy(gameObject);
        }

        #endregion

        #region DEBUG

        private void OnDrawGizmos()
        {
            // Dibujar dirección del proyectil
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)direction * 0.5f);
        }

        #endregion
    }
}
