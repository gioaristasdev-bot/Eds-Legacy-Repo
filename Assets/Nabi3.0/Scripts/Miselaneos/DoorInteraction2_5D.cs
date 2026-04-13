using UnityEngine;

public class DoorInteraction2_5D : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject messageUI;
    [Tooltip("Mensaje alternativo cuando la puerta requiere chakra de hacking (ej: 'Hackear con chakra')")]
    [SerializeField] private GameObject hackMessageUI;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "OpenDoor";

    [Header("Audio")]
    [SerializeField] private AudioClip doorAudio;

    [Header("Chakra")]
    [Tooltip("Si está activo, el botón Y no funciona: la puerta solo se abre mediante el chakra de hacking")]
    [SerializeField] private bool requiresHackingChakra = false;

    private bool playerInside;
    private bool isOpen;

    private void Awake()
    {
        if (messageUI != null)
            messageUI.SetActive(false);
        if (hackMessageUI != null)
            hackMessageUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside || isOpen)
            return;

        if (requiresHackingChakra)
            return;

        // Botón Y del joystick
        if (Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            OpenDoor();
        }
    }

    public void OpenDoor()
    {
        isOpen = true;
        playerInside = false;

        if (messageUI != null)
            messageUI.SetActive(false);
        if (hackMessageUI != null)
            hackMessageUI.SetActive(false);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openTriggerName);

        if (doorAudio != null)
            AudioSource.PlayClipAtPoint(doorAudio, transform.position);
    }

    // ZONA DE ENTRADA
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isOpen)
            return;

        playerInside = true;

        if (requiresHackingChakra)
        {
            if (hackMessageUI != null)
                hackMessageUI.SetActive(true);
        }
        else
        {
            if (messageUI != null)
                messageUI.SetActive(true);
        }
    }

    // ZONA DE SALIDA
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (!isOpen)
        {
            if (messageUI != null)
                messageUI.SetActive(false);
            if (hackMessageUI != null)
                hackMessageUI.SetActive(false);
        }
    }
}


