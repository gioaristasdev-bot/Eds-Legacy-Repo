using UnityEngine;

public class PortalFastTravel : MonoBehaviour
{
    [Header("Portal")]
    public string portalID;
    public string portalName;

    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("UI")]
    public GameObject interactMessage;

    [Header("Audio")]
    public AudioClip activateSound;
    public AudioSource audioSource;

    [Header("Particles")]
    [Tooltip("Partículas que se activan al usar el portal")]
    public ParticleSystem[] portalParticles;

    private bool playerInside;
    private bool unlocked;
    private Transform currentPlayer;

    private void Start()
    {
        if (interactMessage != null)
            interactMessage.SetActive(false);

        // Asegurar partículas apagadas al inicio
        StopParticles();
    }

    private void Update()
    {
        if (!playerInside)
            return;

        // Botón Y
        if (Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            OpenPortal();
        }
    }

    private void OpenPortal()
    {
        // Primera vez = desbloquear
        if (!unlocked)
        {
            unlocked = true;

            FastTravelManager.Instance.UnlockPortal(this);

            Debug.Log("Portal desbloqueado: " + portalName);
        }

        // Sonido
        if (audioSource != null && activateSound != null)
        {
            audioSource.PlayOneShot(activateSound);
        }

        // Activar partículas
        PlayParticles();

        // AQUÍ después abriremos el menú de Fast Travel
        Debug.Log("Abrir menú fast travel");
    }

    /// <summary>
    /// Llamar cuando el jugador se transporte
    /// </summary>
    public void OnTeleportFinished()
    {
        StopParticles();
    }

    private void PlayParticles()
    {
        foreach (var p in portalParticles)
        {
            if (p != null)
                p.Play();
        }
    }

    private void StopParticles()
    {
        foreach (var p in portalParticles)
        {
            if (p != null)
                p.Stop();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        currentPlayer = other.transform;

        if (interactMessage != null)
            interactMessage.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (interactMessage != null)
            interactMessage.SetActive(false);
    }
}