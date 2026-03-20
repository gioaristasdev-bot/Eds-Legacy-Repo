using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private GameObject messageUI;

    private bool playerInside;

    private void Start()
    {
        if (messageUI != null)
            messageUI.SetActive(false);
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            SceneTransitionManager.Instance.LoadScene(sceneToLoad);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        if (messageUI != null)
            messageUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        if (messageUI != null)
            messageUI.SetActive(false);
    }
}

