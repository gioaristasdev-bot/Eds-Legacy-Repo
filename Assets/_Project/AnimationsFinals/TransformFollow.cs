using UnityEngine;

public class TransformFollow : MonoBehaviour
{
    public Transform followTarget;
    public bool followPosition = true;
    public bool followRotation = true;
    public Vector3 positionOffset;
    public Vector3 eulerOffset;
    public Transform weaponPivot;
    public float rotationOffset = 0f;
    public float rotationSmoothSpeed = 20f;
    public bool useLocalRotation = true;
    public bool useLocalPosition = true;
    public float positionSmoothSpeed = 20f;

    private void LateUpdate()
    {
        if (followTarget == null) return;

        if (followPosition)
        {
            Vector3 targetPos = followTarget.position + positionOffset;
            if (useLocalPosition)
                transform.localPosition = Vector3.Lerp(transform.localPosition,
                    transform.parent != null
                        ? transform.parent.InverseTransformPoint(targetPos)
                        : targetPos,
                    positionSmoothSpeed * Time.deltaTime);
            else
                transform.position = Vector3.Lerp(transform.position, targetPos,
                    positionSmoothSpeed * Time.deltaTime);
        }

        if (followRotation)
        {
            Quaternion targetRot = followTarget.rotation * Quaternion.Euler(eulerOffset);
            if (useLocalRotation)
                transform.localRotation = Quaternion.Slerp(transform.localRotation,
                    Quaternion.Euler(eulerOffset) * Quaternion.Euler(0, 0, rotationOffset),
                    rotationSmoothSpeed * Time.deltaTime);
            else
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                    rotationSmoothSpeed * Time.deltaTime);
        }
    }
}
