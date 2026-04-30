using UnityEngine;

public class PulseText : MonoBehaviour
{
    public float speed = 2f;      // velocidad del efecto
    public float scaleAmount = 0.1f; // cuánto crece

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * scaleAmount;

        transform.localScale = originalScale * scale;
    }
}
