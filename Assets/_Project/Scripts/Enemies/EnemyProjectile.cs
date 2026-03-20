using UnityEngine;
using NABHI.Character;

namespace NABHI.Enemies
{
    /// <summary>
    /// Proyectil disparado por enemigos (MechSoldier, Turret).
    /// Daña al jugador via IDamageable, se destruye al impactar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        #region CONFIGURACIÓN

        [Header("Proyectil")]
        [SerializeField] private float damage = 15f;
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 5f;

        [Header("Visual")]
        [SerializeField] private TrailRenderer trail;

        #endregion

        #region ESTADO

        private Rigidbody2D rb;
        private Vector2 direction;
        private float spawnTime;
        private bool hasHit;

        #endregion

        #region UNITY CALLBACKS

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - spawnTime >= lifetime)
            {
                DestroyProjectile();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasHit) return;

            // Ignorar otros enemigos y proyectiles enemigos
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemies"))
                return;
            if (collision.GetComponent<EnemyProjectile>() != null)
                return;

            // Dañar al jugador
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player") ||
                collision.gameObject.layer == LayerMask.NameToLayer("InvisiblePlayer"))
            {
                IDamageable damageable = collision.GetComponent<IDamageable>();
                if (damageable != null && damageable.IsAlive())
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"[EnemyProjectile] Impactó al jugador por {damage} daño");
                }
                hasHit = true;
                DestroyProjectile();
                return;
            }

            // Impactar suelo/paredes
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                hasHit = true;
                DestroyProjectile();
                return;
            }
        }

        #endregion

        #region MÉTODOS PÚBLICOS

        /// <summary>
        /// Inicializar el proyectil con dirección y velocidad opcional
        /// </summary>
        public void Initialize(Vector2 direction, float? customSpeed = null)
        {
            this.direction = direction.normalized;
            float projectileSpeed = customSpeed ?? speed;
            rb.velocity = this.direction * projectileSpeed;

            // Rotar sprite para apuntar en la dirección
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        /// <summary>
        /// Establecer daño personalizado
        /// </summary>
        public void SetDamage(float newDamage)
        {
            damage = newDamage;
        }

        #endregion

        #region PRIVADOS

        private void DestroyProjectile()
        {
            if (trail != null)
            {
                trail.transform.SetParent(null);
                trail.autodestruct = true;
            }

            Destroy(gameObject);
        }

        #endregion
    }
}
