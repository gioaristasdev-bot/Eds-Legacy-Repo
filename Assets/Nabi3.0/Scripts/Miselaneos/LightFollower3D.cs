using UnityEngine;

public class LightFollower3D : MonoBehaviour
{
    public Transform player;
    public float speed = 5f;

    private bool detected = false;

    void Update()
    {
        if (detected && player != null)
        {
            Vector3 targetPosition = new Vector3(
                player.position.x,
                player.position.y,
                transform.position.z
            );

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            detected = true;
            Debug.Log("Entró al rango");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            detected = false;
            Debug.Log("Salió del rango"); // 👈 importante para debug
        }
    }
}
