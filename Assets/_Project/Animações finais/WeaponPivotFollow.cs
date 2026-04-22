using UnityEngine;

public class WeaponPivotBobFollow : MonoBehaviour
{
    public Transform sourceBone;

    [Header("Follow")]
    public bool useWorldSpace = false;
    public float yInfluence = 1f;

    [Header("Drag / Weight")]
    public float followSpeed = 10f;
    public float maxYOffset = 0.25f;

    private Vector3 baseLocalPosition;
    private Vector3 baseWorldPosition;
    private float sourceStartY;
    private float currentYOffset;

    void Start()
    {
        baseLocalPosition = transform.localPosition;
        baseWorldPosition = transform.position;

        if (sourceBone != null)
        {
            sourceStartY = useWorldSpace
                ? sourceBone.position.y
                : sourceBone.localPosition.y;
        }
    }

    void LateUpdate()
    {
        if (sourceBone == null)
            return;

        float sourceY = useWorldSpace
            ? sourceBone.position.y
            : sourceBone.localPosition.y;

        float deltaY = (sourceY - sourceStartY) * yInfluence;
        deltaY = Mathf.Clamp(deltaY, -maxYOffset, maxYOffset);

        currentYOffset = Mathf.Lerp(currentYOffset, deltaY, followSpeed * Time.deltaTime);

        if (useWorldSpace)
        {
            Vector3 p = baseWorldPosition;
            p.y += currentYOffset;
            transform.position = p;
        }
        else
        {
            Vector3 p = baseLocalPosition;
            p.y += currentYOffset;
            transform.localPosition = p;
        }
    }
}