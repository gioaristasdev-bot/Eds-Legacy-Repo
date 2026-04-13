using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private float volume = 1f;

    public void OnSelect(BaseEventData eventData)
    {
        PlaySound();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound();
    }

    private void PlaySound()
    {
        if (hoverSound != null)
        {
            AudioSource.PlayClipAtPoint(hoverSound, Vector3.zero, volume);
        }
    }
}
