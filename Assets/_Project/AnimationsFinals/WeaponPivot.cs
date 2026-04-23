using UnityEngine;

public class WeaponPivot : MonoBehaviour
{
    public Transform aimTarget;
    public Transform weaponVisual;

    [Header("Angle Offsets")]
    public float rightAngleOffset = 0f;
    public float leftAngleOffset = 0f;

    [Header("Flip Right")]
    public bool flipXRight = false;
    public bool flipYRight = false;

    [Header("Flip Left")]
    public bool flipXLeft = false;
    public bool flipYLeft = false;

    void Update()
    {
        if (aimTarget == null || weaponVisual == null)
            return;

        Vector3 dir = aimTarget.position - transform.position;

        Vector3 axisRight = transform.parent != null ? transform.parent.right : Vector3.right;
        Vector3 axisUp = transform.parent != null ? transform.parent.up : Vector3.up;

        float localX = Vector3.Dot(dir, axisRight);
        float localY = Vector3.Dot(dir, axisUp);

        if (transform.lossyScale.x < 0f)
            localX *= -1f;

        float angle = Mathf.Atan2(localY, localX) * Mathf.Rad2Deg;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
