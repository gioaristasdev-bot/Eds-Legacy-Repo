using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerName = "Open";

    private bool playerInside = false;
    private bool isOpen = false;

    void Update()
    {
        if (!playerInside || isOpen)
            return;

        if (Input.GetButtonDown("Y")) // Botón Y del Gamepad
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        isOpen = true;
        doorAnimator.SetTrigger(openTriggerName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }
}

