using UnityEngine;

public class AreaMessageTrigger2D : MonoBehaviour
{
    [SerializeField] private GameObject messageUI;

    private void Start()
    {
        if (messageUI != null)
            messageUI.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            messageUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            messageUI.SetActive(false);
        }
    }
}



