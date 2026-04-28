using UnityEngine;

public class AreaDialogueTrigger2D : MonoBehaviour
{
    [SerializeField] private AreaMessageTrigger2D dialogueManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            dialogueManager.enabled = true;
        }
    }
}
