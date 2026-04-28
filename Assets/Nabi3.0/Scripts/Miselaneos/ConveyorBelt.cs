using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    public float speed = 2f;
    public Vector2 direction = Vector2.left;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Mover directamente el jugador
            other.transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }
    }
}
