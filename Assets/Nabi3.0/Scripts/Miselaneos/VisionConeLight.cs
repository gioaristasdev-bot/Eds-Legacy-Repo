using UnityEngine;

public class VisionConeLight : MonoBehaviour
{
    public Transform player;

    [Header("Vision")]
    public float viewDistance = 10f;
    public float viewAngle = 45f;

    [Header("Layers")]
    public LayerMask obstacleMask;
    public LayerMask playerMask;

    [Header("Debug")]
    public bool detected;

    void Update()
    {
        DetectPlayer();
    }

    void DetectPlayer()
    {
        detected = false;

        if (player == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. Distancia
        if (distanceToPlayer < viewDistance)
        {
            // 2. Ángulo
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle < viewAngle / 2f)
            {
                // 3. Raycast (línea de visión)
                if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, obstacleMask))
                {
                    detected = true;

                    Debug.Log("👁️ Player detectado");

                    // 👉 AQUÍ puedes hacer cosas:
                    // seguir jugador
                    // activar alarma
                    // matar jugador
                }
            }
        }
    }
}