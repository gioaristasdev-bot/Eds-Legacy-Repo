using UnityEngine;
using NABHI.Character;

namespace NABHI.Enemies
{
    /// <summary>
    /// Daño por contacto con el cuerpo del boss.
    /// No usa Physics2D (evita el IgnoreLayerCollision Player-Enemies).
    /// Agregar al GameObject raíz del boss o a un hijo que actúe como hitbox corporal.
    /// </summary>
    public class BossContactDamage : MonoBehaviour
    {
        [Tooltip("Daño por toque")]
        [SerializeField] private float damage = 10f;
        [Tooltip("Radio de detección del contacto")]
        [SerializeField] private float contactRadius = 1.2f;
        [Tooltip("Segundos de invulnerabilidad entre golpes")]
        [SerializeField] private float damageCooldown = 0.8f;
        [Tooltip("Marcar si este componente es el hitbox corporal permanente. " +
                 "Los hitboxes marcados así nunca son desactivados por los ataques.")]
        [SerializeField] private bool isBodyHitbox = false;

        public bool IsBodyHitbox => isBodyHitbox;

        private Transform playerTarget;
        private IDamageable playerDamageable;
        private float cooldownTimer;

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO == null) return;
            playerTarget    = playerGO.transform;
            playerDamageable = playerGO.GetComponentInChildren<IDamageable>();
            if (playerDamageable == null)
                playerDamageable = playerGO.GetComponent<IDamageable>();
        }

        public bool IsActive { get; private set; }

        public void Activate()
        {
            IsActive      = true;
            cooldownTimer = 0f;
        }

        public void Deactivate() => IsActive = false;

        private void Update()
        {
            if (!IsActive) return;
            if (playerTarget == null || playerDamageable == null) return;

            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer > 0f) return;

            if (Vector2.Distance(transform.position, playerTarget.position) <= contactRadius)
            {
                if (playerDamageable.IsAlive())
                {
                    playerDamageable.TakeDamage(damage);
                    cooldownTimer = damageCooldown;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, contactRadius);
        }
    }
}
