using UnityEngine;

public class Recoil : MonoBehaviour
{
    public Transform aimTarget;
    public float angleOffset = 0f;
    public DemoAimBlendController demoAimBlend;

    [Header("Rotation Recoil")]
    public float recoilAngle = 8f;
    public float recoilReturnSpeed = 18f;
    public float recoilKickSpeed = 28f;

    [Header("Position Recoil")]
    public float recoilDistance = 0.12f;
    public float positionReturnSpeed = 18f;
    public float positionKickSpeed = 28f;

    private float recoilCurrent = 0f;
    private float recoilVelocity = 0f;

    private Vector3 localStartPos;
    private Vector3 positionCurrent;
    private Vector3 positionVelocity;

    void Awake()
    {
        localStartPos = transform.localPosition;
        positionCurrent = localStartPos;
        positionVelocity = localStartPos;
    }

    void LateUpdate()
    {
        if (aimTarget == null) return;

        if (demoAimBlend == null)
        {
            demoAimBlend = GetComponentInParent<DemoAimBlendController>();
        }

        float baseAngle = transform.localEulerAngles.z;
        if (baseAngle > 180f) baseAngle -= 360f;

        if (Input.GetMouseButtonDown(0))
        {
            recoilVelocity += recoilAngle;

            Vector3 dir = (aimTarget.position - transform.position).normalized;
            Vector3 localBack = transform.parent != null
                ? transform.parent.InverseTransformDirection(-dir) * recoilDistance
                : -dir * recoilDistance;

            positionVelocity += localBack;
        }

        recoilVelocity = Mathf.Lerp(recoilVelocity, 0f, recoilReturnSpeed * Time.deltaTime);
        recoilCurrent = Mathf.Lerp(recoilCurrent, recoilVelocity, recoilKickSpeed * Time.deltaTime);

        positionVelocity = Vector3.Lerp(positionVelocity, localStartPos, positionReturnSpeed * Time.deltaTime);
        positionCurrent = Vector3.Lerp(positionCurrent, positionVelocity, positionKickSpeed * Time.deltaTime);

        transform.localPosition = positionCurrent;

        float finalAngle = baseAngle + angleOffset - recoilCurrent;
        if (demoAimBlend != null)
        {
            finalAngle = demoAimBlend.ClampWeaponFinalAngle(finalAngle);
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, finalAngle);
    }
}
