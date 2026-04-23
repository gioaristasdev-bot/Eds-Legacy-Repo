using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    public Camera cam;
    public Transform aimTarget;

    void Update()
    {
        if (cam == null || aimTarget == null) return;

        Vector3 mouseScreen = Input.mousePosition;
        mouseScreen.z = -cam.transform.position.z;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0f;

        aimTarget.position = mouseWorld;
    }
}