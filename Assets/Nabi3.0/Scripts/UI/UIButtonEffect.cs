using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonEffect : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);
    public float speed = 10f;

    private Vector3 targetScale;

    void Start()
    {
        targetScale = normalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);
    }

    public void OnSelect(BaseEventData eventData)
    {
        targetScale = selectedScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        targetScale = normalScale;
    }
}
