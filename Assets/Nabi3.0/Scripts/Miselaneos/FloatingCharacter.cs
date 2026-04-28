using UnityEngine;

public class FloatingCharacter : MonoBehaviour
{
    public float floatSpeed = 1f;
    public float floatHeight = 0.5f;
    public float rotationSpeed = 30f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Movimiento vertical suave
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.position = new Vector3(
            transform.position.x,
            newY,
            transform.position.z
        );

        // Rotación lenta
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}