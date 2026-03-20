using UnityEngine;

public class DoorInteraction2_5D : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject messageUI;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "OpenDoor";

    [Header("Audio")]
    [SerializeField] private AudioSource doorAudio;

    private bool playerInside;
    private bool isOpen;

    private void Awake()
    {
        if (messageUI != null)
            messageUI.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside || isOpen)
            return;

        // Botón Y del joystick (como el elevador)
        if (Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        playerInside = false;

        if (messageUI != null)
            messageUI.SetActive(false);

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openTriggerName);

        if (doorAudio != null)
            doorAudio.Play();
    }

    // ZONA DE ENTRADA
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isOpen)
            return;

        playerInside = true;

        if (messageUI != null)
            messageUI.SetActive(true);
    }

    // ZONA DE SALIDA
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (!isOpen && messageUI != null)
            messageUI.SetActive(false);
    }
}


