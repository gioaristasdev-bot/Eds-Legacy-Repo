using UnityEngine;

public class HeadAimController : MonoBehaviour
{
    public Transform aimTarget;
    public SpriteFlip spriteFlip;
    public Transform headBone;

    public float maxUpAngle = 50f;
    public float maxDownAngle = 50f;
    public float smoothSpeed = 10f;

    private float currentAngle;
    private Quaternion headBaseRotation;

    void Awake()
    {
        if (headBone == null)
            headBone = transform.parent;
    }

    void Start()
    {
        if (headBone == null)
            return;

        headBaseRotation = headBone.localRotation;

        currentAngle = headBone.localEulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
    }

    void LateUpdate()
    {
        if (aimTarget == null || spriteFlip == null || headBone == null || headBone.parent == null)
            return;

        bool facingLeft = spriteFlip.Facing == -1;

        Vector3 dir = aimTarget.position - headBone.position;
        Vector3 axisRight = headBone.parent.right.normalized;
        Vector3 axisUp = headBone.parent.up.normalized;

        float localX = Vector3.Dot(dir, axisRight);
        float localY = Vector3.Dot(dir, axisUp);

        if (facingLeft)
            localX *= -1f;

        Vector2 localTarget = new Vector2(localX, localY);
        float rawAngle = Vector2.SignedAngle(Vector2.down, localTarget);

        float desiredAngle = Mathf.Clamp(rawAngle, -maxDownAngle, maxUpAngle);

        currentAngle = Mathf.LerpAngle(currentAngle, desiredAngle, smoothSpeed * Time.deltaTime);
        headBone.localRotation = headBaseRotation * Quaternion.Euler(0f, 0f, currentAngle);
    }
}
