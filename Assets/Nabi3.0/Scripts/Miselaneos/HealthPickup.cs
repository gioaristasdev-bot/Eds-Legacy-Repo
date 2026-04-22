using UnityEngine;

namespace NABHI.Character
{
    /// <summary>
    /// Objeto de recuperación de salud.
    /// Requiere un Collider2D en modo Trigger.
    /// Al contacto con el jugador cura la cantidad indicada y se destruye.
    /// </summary>
    public class HealthPickup : MonoBehaviour
    {
        [Tooltip("Cantidad de salud que recupera el jugador")]
        [SerializeField] private int healAmount = 25;

        [Tooltip("Efecto visual al recoger (opcional)")]
        [SerializeField] private GameObject pickupVFX;

        [Tooltip("Sonido al recoger el objeto")]
        [SerializeField] private AudioClip pickupSound;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            if (playerHealth == null || !playerHealth.IsAlive())
                return;

            // No curar si ya tiene la vida al máximo
            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
                return;

            playerHealth.Heal(healAmount);

            if (pickupVFX != null)
                Instantiate(pickupVFX, transform.position, Quaternion.identity);

            // Reproducir sonido
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            Destroy(gameObject);
        }
    }
}

