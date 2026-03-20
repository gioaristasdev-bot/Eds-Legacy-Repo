using UnityEngine;
using NABHI.Character;
using Cinemachine;

/// <summary>
/// Sistema de Camera Shake compatible con Cinemachine
/// Usa Cinemachine Impulse para generar vibraciones que no son sobreescritas por la Virtual Camera
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Configuración de Shake")]
    [Tooltip("Fuerza del shake (recomendado: 0.5 - 2.0)")]
    [SerializeField] private float shakeForce = 1.0f;

    void Start()
    {
        // Auto-find impulse source si no está asignado
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        // Crear impulse source si no existe
        if (impulseSource == null)
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            Debug.Log("[CameraShake] CinemachineImpulseSource creado automáticamente. " +
                "Configúralo en el Inspector para mejor control.");
        }

        // Suscribirse al evento de daño
        if (playerHealth != null)
            playerHealth.OnHit.AddListener(Shake);
        else
            Debug.LogWarning("[CameraShake] PlayerHealth no está asignado. No se activará el shake al recibir daño.");
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHit.RemoveListener(Shake);
    }

    /// <summary>
    /// Genera un impulso de cámara (shake)
    /// </summary>
    private void Shake()
    {
        if (impulseSource != null)
        {
            // Generar impulso con la fuerza configurada
            impulseSource.GenerateImpulse(shakeForce);
            Debug.Log($"[CameraShake] Impulso generado con fuerza: {shakeForce}");
        }
        else
        {
            Debug.LogWarning("[CameraShake] No se encontró CinemachineImpulseSource. El shake no funcionará.");
        }
    }

    /// <summary>
    /// Método público para activar shake manualmente desde código
    /// </summary>
    public void TriggerShake()
    {
        Shake();
    }

    /// <summary>
    /// Activar shake con fuerza customizada
    /// </summary>
    public void TriggerShake(float customForce)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(customForce);
        }
    }
}