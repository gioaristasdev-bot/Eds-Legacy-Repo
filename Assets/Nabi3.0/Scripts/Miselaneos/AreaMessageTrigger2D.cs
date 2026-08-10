using UnityEngine;

public class AreaMessageTrigger2D : MonoBehaviour
{
    [SerializeField] private GameObject[] messagesUI; // 👈 varios mensajes ya hechos
    [SerializeField] private MonoBehaviour playerMovement;

    private int currentIndex = 0;
    private bool playerInside = false;
    private bool isShowing = false;
    private Rigidbody2D playerRb;
    private bool alreadyTriggered = false;

    private void Start()
    {
        // Apagar todos al inicio
        foreach (GameObject msg in messagesUI)
        {
            if (msg != null)
                msg.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInside && isShowing)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Submit"))
            {
                NextMessage();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !alreadyTriggered)
        {
            alreadyTriggered = true; // 👈 se marca como usado

            playerInside = true;
            currentIndex = 0;
            isShowing = true;

            ShowMessage(currentIndex);

            if (playerMovement != null)
                playerMovement.enabled = false;
        }

        playerRb = other.GetComponent<Rigidbody2D>();

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EndMessages();
        }
    }

    void ShowMessage(int index)
    {
        // Apagar todos
        foreach (GameObject msg in messagesUI)
        {
            if (msg != null)
                msg.SetActive(false);
        }

        // Encender el actual
        if (messagesUI[index] != null)
            messagesUI[index].SetActive(true);
    }

    void NextMessage()
    {
        currentIndex++;

        if (currentIndex < messagesUI.Length)
        {
            ShowMessage(currentIndex);
        }
        else
        {
            EndMessages();
        }
    }

    void EndMessages()
    {
        isShowing = false;

        // Apagar todos
        foreach (GameObject msg in messagesUI)
        {
            if (msg != null)
                msg.SetActive(false);
        }

        // ✅ Devolver control
        if (playerMovement != null)
            playerMovement.enabled = true;
    }
}



