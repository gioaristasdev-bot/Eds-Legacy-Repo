using UnityEngine;

[DefaultExecutionOrder(-50)]
public class DemoAimBlendController : MonoBehaviour
{
    [Header("References")]
    public Transform aimTarget;
    public Transform visualRoot;
    public Transform headBone;
    public Transform weaponPivot;
    public Transform movementSource;
    public Animator targetAnimator;

    [Header("Blend Parameters")]
    public string speedParameter = "Speed";
    public string moveXParameter = "MoveX";
    public string moveYParameter = "MoveY";
    public float movementDamping = 8f;

    [Header("Facing")]
    public float flipDeadZone = 0.1f;
    public int Facing { get; private set; } = 1;

    [Header("Head Aim")]
    [Range(0f, 89f)]
    public float maxUpAngle = 50f;
    [Range(0f, 89f)]
    public float maxDownAngle = 50f;
    public float headSmoothSpeed = 12f;

    [Header("Weapon Aim")]
    [Range(0f, 89f)]
    public float maxWeaponUpAngle = 85f;
    [Range(0f, 89f)]
    public float maxWeaponDownAngle = 85f;
    public float rightWeaponOffset = 0f;
    public float leftWeaponOffset = 180f;
    public bool mirrorWeaponOnLeft = true;

    [Header("Deprecated Overrides")]
    public bool disableDeprecatedComponents = true;
    public SpriteFlip deprecatedSpriteFlip;
    public HeadAimController deprecatedHeadAim;
    public WeaponPivot deprecatedWeaponPivot;

    private Quaternion headBaseLocalRotation;
    private float currentHeadAngle;
    private Vector3 lastMovementPosition;
    private Vector2 smoothedLocalVelocity;

    private float defaultHeadUpAngle;
    private float defaultHeadDownAngle;
    private float defaultWeaponUpAngle;
    private float defaultWeaponDownAngle;

    private void Awake()
    {
        defaultHeadUpAngle = maxUpAngle;
        defaultHeadDownAngle = maxDownAngle;
        defaultWeaponUpAngle = maxWeaponUpAngle;
        defaultWeaponDownAngle = maxWeaponDownAngle;

        if (headBone != null)
        {
            headBaseLocalRotation = headBone.localRotation;
            currentHeadAngle = NormalizeAngle(headBone.localEulerAngles.z);
        }

        if (movementSource != null)
        {
            lastMovementPosition = movementSource.position;
        }

        if (disableDeprecatedComponents)
        {
            DisableDeprecatedComponents();
        }
    }

    private void LateUpdate()
    {
        if (aimTarget == null)
        {
            return;
        }

        UpdateFacing();
        UpdateHeadAim();
        UpdateWeaponAim();
        UpdateMovementBlend();
    }

    private void UpdateFacing()
    {
        if (visualRoot == null)
        {
            return;
        }

        float dx = aimTarget.position.x - visualRoot.position.x;
        if (dx > flipDeadZone)
        {
            Facing = 1;
        }
        else if (dx < -flipDeadZone)
        {
            Facing = -1;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * Facing;
        visualRoot.localScale = scale;
    }

    private void UpdateHeadAim()
    {
        if (headBone == null || headBone.parent == null)
        {
            return;
        }

        // Hard lock: if limits are zero, keep head at base pose.
        if (maxUpAngle <= 0f && maxDownAngle <= 0f)
        {
            currentHeadAngle = 0f;
            headBone.localRotation = headBaseLocalRotation;
            return;
        }

        Vector3 dir = aimTarget.position - headBone.position;
        Vector3 axisRight = headBone.parent.right.normalized;
        Vector3 axisUp = headBone.parent.up.normalized;

        float localX = Vector3.Dot(dir, axisRight);
        float localY = Vector3.Dot(dir, axisUp);

        if (Facing == -1)
        {
            localX *= -1f;
        }

        float rawAngle = Vector2.SignedAngle(Vector2.down, new Vector2(localX, localY));
        float clamped = Mathf.Clamp(rawAngle, -maxDownAngle, maxUpAngle);
        currentHeadAngle = Mathf.LerpAngle(currentHeadAngle, clamped, headSmoothSpeed * Time.deltaTime);

        headBone.localRotation = headBaseLocalRotation * Quaternion.Euler(0f, 0f, currentHeadAngle);
    }

    private void UpdateWeaponAim()
    {
        if (weaponPivot == null || weaponPivot.parent == null)
        {
            return;
        }

        // Hard lock: if limits are zero, freeze weapon at side offset.
        if (maxWeaponUpAngle <= 0f && maxWeaponDownAngle <= 0f)
        {
            float lockOffset = Facing == 1 ? rightWeaponOffset : leftWeaponOffset;
            weaponPivot.localRotation = Quaternion.Euler(0f, 0f, lockOffset);

            if (mirrorWeaponOnLeft)
            {
                Vector3 lockScale = weaponPivot.localScale;
                lockScale.y = Mathf.Abs(lockScale.y) * Facing;
                weaponPivot.localScale = lockScale;
            }

            return;
        }

        Vector3 dir = aimTarget.position - weaponPivot.position;
        Vector3 axisRight = weaponPivot.parent.right.normalized;
        Vector3 axisUp = weaponPivot.parent.up.normalized;

        float localX = Vector3.Dot(dir, axisRight);
        float localY = Vector3.Dot(dir, axisUp);

        if (Facing == -1)
        {
            localX *= -1f;
        }

        float angle = Mathf.Atan2(localY, localX) * Mathf.Rad2Deg;
        float offset = Facing == 1 ? rightWeaponOffset : leftWeaponOffset;
        float targetFinalAngle = offset + angle;
        float clampedFinalAngle = ClampWeaponFinalAngle(targetFinalAngle);
        weaponPivot.localRotation = Quaternion.Euler(0f, 0f, clampedFinalAngle);

        if (mirrorWeaponOnLeft)
        {
            Vector3 localScale = weaponPivot.localScale;
            localScale.y = Mathf.Abs(localScale.y) * Facing;
            weaponPivot.localScale = localScale;
        }
    }

    private void UpdateMovementBlend()
    {
        if (targetAnimator == null || movementSource == null)
        {
            return;
        }

        float dt = Mathf.Max(0.0001f, Time.deltaTime);
        Vector3 worldVelocity = (movementSource.position - lastMovementPosition) / dt;
        lastMovementPosition = movementSource.position;

        Vector2 localVelocity;
        if (visualRoot != null)
        {
            Vector3 right = visualRoot.right;
            Vector3 up = visualRoot.up;
            localVelocity = new Vector2(Vector3.Dot(worldVelocity, right), Vector3.Dot(worldVelocity, up));
        }
        else
        {
            localVelocity = new Vector2(worldVelocity.x, worldVelocity.y);
        }

        smoothedLocalVelocity = Vector2.Lerp(smoothedLocalVelocity, localVelocity, movementDamping * Time.deltaTime);

        targetAnimator.SetFloat(speedParameter, smoothedLocalVelocity.magnitude);
        targetAnimator.SetFloat(moveXParameter, smoothedLocalVelocity.x);
        targetAnimator.SetFloat(moveYParameter, smoothedLocalVelocity.y);
    }

    private void DisableDeprecatedComponents()
    {
        if (deprecatedSpriteFlip == null)
        {
            deprecatedSpriteFlip = GetComponent<SpriteFlip>();
        }

        if (deprecatedHeadAim == null)
        {
            deprecatedHeadAim = GetComponent<HeadAimController>();
        }

        if (deprecatedWeaponPivot == null)
        {
            deprecatedWeaponPivot = GetComponent<WeaponPivot>();
        }

        if (deprecatedSpriteFlip != null)
        {
            deprecatedSpriteFlip.enabled = false;
        }

        if (deprecatedHeadAim != null)
        {
            deprecatedHeadAim.enabled = false;
        }

        if (deprecatedWeaponPivot != null)
        {
            deprecatedWeaponPivot.enabled = false;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    public void SetAimEnvelope(float headUp, float headDown, float weaponUp, float weaponDown)
    {
        maxUpAngle = Mathf.Clamp(headUp, 0f, 89f);
        maxDownAngle = Mathf.Clamp(headDown, 0f, 89f);
        maxWeaponUpAngle = Mathf.Clamp(weaponUp, 0f, 89f);
        maxWeaponDownAngle = Mathf.Clamp(weaponDown, 0f, 89f);
    }

    public void ResetAimEnvelopeToDefault()
    {
        SetAimEnvelope(defaultHeadUpAngle, defaultHeadDownAngle, defaultWeaponUpAngle, defaultWeaponDownAngle);
    }

    public float ClampWeaponFinalAngle(float finalAngle)
    {
        float offset = Facing == 1 ? rightWeaponOffset : leftWeaponOffset;
        float relative = Mathf.DeltaAngle(offset, finalAngle);
        float clampedRelative = Mathf.Clamp(relative, -maxWeaponDownAngle, maxWeaponUpAngle);
        return offset + clampedRelative;
    }
}
