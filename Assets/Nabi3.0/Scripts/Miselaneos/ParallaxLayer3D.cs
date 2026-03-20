using UnityEngine;

public class ParallaxLayer3D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Parallax Settings")]
    [Range(0f, 2f)]
    [SerializeField] private float parallaxMultiplier = 0.5f;

    private Vector3 lastCameraPosition;

    void Start()
    {
        if (!cameraTransform)
            cameraTransform = Camera.main.transform;

        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Solo parallax en X (y opcional en Y)
        Vector3 parallaxOffset = new Vector3(
            deltaMovement.x * parallaxMultiplier,
            deltaMovement.y * parallaxMultiplier,
            0f
        );

        transform.position += parallaxOffset;

        lastCameraPosition = cameraTransform.position;
    }
}

