using UnityEngine;

public class SpriteFlip : MonoBehaviour
{
    public Transform aimTarget;
    public Transform visualRoot;
    public Transform weaponPivot;
    public float deadZone = 0.15f;

    public int Facing { get; private set; } = 1;

    void Update()
    {
        if (aimTarget == null || visualRoot == null)
            return;

        float dx = aimTarget.position.x - visualRoot.position.x;

        if (dx > deadZone)
            Facing = 1;
        else if (dx < -deadZone)
            Facing = -1;

        Vector3 s = visualRoot.localScale;
        s.x = Mathf.Abs(s.x) * Facing;
        visualRoot.localScale = s;
    }
}
