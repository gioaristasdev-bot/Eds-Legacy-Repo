using UnityEngine;

public class ElevatorController2D : MonoBehaviour
{
    [Header("Elevator")]
    public Transform elevatorPlatform;
    public Transform targetPointUp;
    public float speed = 2f;

    [Header("UI")]
    public GameObject pressButtonMessage;

    [Header("Player")]
    public MonoBehaviour playerMovementScript;

    [Header("Effects")]
    public ParticleSystem[] particles;
    public AudioSource elevatorAudio;

    private bool playerInside;
    private bool isMoving;
    private bool hasBeenUsed;

    void Update()
    {
        if (playerInside && !isMoving && !hasBeenUsed)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton3)) // Botón Y
            {
                StartElevator();
            }
        }

        if (isMoving)
        {
            MoveElevator();
        }
    }

    void StartElevator()
    {
        isMoving = true;
        hasBeenUsed = true;

        pressButtonMessage.SetActive(false);

        // 🔒 Bloquear input del player
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        // 🔥 Activar partículas
        foreach (var p in particles)
        {
            if (p != null)
                p.Play();
        }

        // 🔊 Activar audio
        if (elevatorAudio != null)
            elevatorAudio.Play();
    }

    void MoveElevator()
    {
        elevatorPlatform.position = Vector3.MoveTowards(
            elevatorPlatform.position,
            targetPointUp.position,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(elevatorPlatform.position, targetPointUp.position) < 0.01f)
        {
            FinishElevator();
        }
    }

    void FinishElevator()
    {
        isMoving = false;

        // 🔓 Reactivar input del player
        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        // ❄️ Detener partículas
        foreach (var p in particles)
        {
            if (p != null)
                p.Stop();
        }

        // 🔇 Detener audio
        if (elevatorAudio != null)
            elevatorAudio.Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasBeenUsed && other.CompareTag("Player"))
        {
            playerInside = true;
            pressButtonMessage.SetActive(true);

            // Player se mueve con el elevador
            other.transform.SetParent(elevatorPlatform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            pressButtonMessage.SetActive(false);

            other.transform.SetParent(null);
        }
    }
}



