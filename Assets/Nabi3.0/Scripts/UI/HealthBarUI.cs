using NABHI.Character;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta el sistema de salud del player (PlayerHealth) con un Slider de UI.
/// Suscribirse al evento OnHealthChanged garantiza que la barra se actualice
/// exactamente cuando el jugador recibe daño o se cura.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Slider que representa la barra de vida")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Script PlayerHealth del jugador")]
    [SerializeField] private PlayerHealth playerHealth;

    void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[HealthBarUI] PlayerHealth no asignado. Buscando en escena...");
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogError("[HealthBarUI] No se encontró PlayerHealth en la escena.");
            return;
        }

        // Inicializar barra con valores actuales
        healthSlider.maxValue = playerHealth.MaxHealth;
        healthSlider.value = playerHealth.CurrentHealth;

        // Suscribirse al evento para recibir actualizaciones
        playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
    }

    private void OnHealthChanged(int newHealth)
    {
        healthSlider.value = newHealth;
    }
}
