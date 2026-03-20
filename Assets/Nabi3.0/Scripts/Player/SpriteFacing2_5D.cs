using UnityEngine;

public class SpriteFacing2_5D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void UpdateFacing(float horizontal)
    {
        if (Mathf.Abs(horizontal) < 0.01f) return;

        spriteRenderer.flipX = horizontal < 0f;
    }
}
