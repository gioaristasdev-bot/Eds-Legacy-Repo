using UnityEngine;
using System.Collections;
using NABHI.Character;

/// <summary>
/// Camera Shake simple que funciona sin Cinemachine
/// Mueve la Main Camera directamente (no compatible con Cinemachine)
/// </summary>
public class SimpleCameraShake : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform cameraTransform;

    [Header("Configuración")]
    [Tooltip("Duración del shake en segundos")]
    [SerializeField] private float shakeDuration = 0.15f;

    [Tooltip("Magnitud del shake (qué tan fuerte)")]
    [SerializeField] private float shakeMagnitude = 0.3f;

    private Vector3 originalPosition;
    private bool isShaking = false;

    void Start()
    {
        // Auto-find PlayerHealth
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        // Auto-find Main Camera si no está asignado
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
                Debug.Log("[SimpleCameraShake] Main Camera encontrada automáticamente");
            }
            else
            {
                Debug.LogError("[SimpleCameraShake] No se encontró Main Camera. El shake no funcionará.");
            }
        }

        // Suscribirse al evento de daño
        if (playerHealth != null)
        {
            playerHealth.OnHit.AddListener(TriggerShake);
            Debug.Log("[SimpleCameraShake] Suscrito al evento OnHit de PlayerHealth");
        }
        else
        {
            Debug.LogWarning("[SimpleCameraShake] PlayerHealth no está asignado. El shake no funcionará.");
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHit.RemoveListener(TriggerShake);
    }

    /// <summary>
    /// Activa el shake de cámara
    /// </summary>
    public void TriggerShake()
    {
        if (cameraTransform != null && !isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    /// <summary>
    /// Activa shake con parámetros custom
    /// </summary>
    public void TriggerShake(float duration, float magnitude)
    {
        if (cameraTransform != null && !isShaking)
        {
            StartCoroutine(ShakeCoroutine(duration, magnitude));
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        yield return ShakeCoroutine(shakeDuration, shakeMagnitude);
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        originalPosition = cameraTransform.localPosition;
        float elapsed = 0f;

        Debug.Log($"[SimpleCameraShake] Shake iniciado - Duración: {duration}s, Magnitud: {magnitude}");

        while (elapsed < duration)
        {
            // Generar offset aleatorio
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restaurar posición original
        cameraTransform.localPosition = originalPosition;
        isShaking = false;

        Debug.Log("[SimpleCameraShake] Shake finalizado");
    }
}
