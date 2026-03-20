using UnityEngine;

public class Sprite2_5DLock : MonoBehaviour
{
    private Quaternion initialRotation;

    void Awake()
    {
        initialRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        transform.localRotation = initialRotation;
    }
}

