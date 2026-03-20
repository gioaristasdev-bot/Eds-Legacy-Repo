using UnityEngine;

public class PlayerMovement2_5D : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody rb;
    private SpriteFacing2_5D facing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        facing = GetComponent<SpriteFacing2_5D>();
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");

        rb.velocity = new Vector3(h * speed, rb.velocity.y, 0f);

        facing.UpdateFacing(h);
    }
}
