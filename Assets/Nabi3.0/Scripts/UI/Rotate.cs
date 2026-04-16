using UnityEngine;
using System.Collections;

public class Rotate : MonoBehaviour {

    public float x;
    public float y;
    public float z;

    public bool si;
    void Update()
    {
        if (si)
        {
            // Rotate the object around its local X axis at 1 degree per second
            transform.Rotate(x * Time.deltaTime, y * Time.deltaTime, z * Time.deltaTime);
        }
        else
        {
            // ...also rotate around the World's Y axis
            transform.Rotate(x * Time.deltaTime, y * Time.deltaTime, z * Time.deltaTime, Space.World);
        }

    }
}
