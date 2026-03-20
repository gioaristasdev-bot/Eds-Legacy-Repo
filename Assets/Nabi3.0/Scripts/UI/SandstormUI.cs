using UnityEngine;
using UnityEngine.UI;

public class SandstormUI : MonoBehaviour
{
    public float scrollSpeedX = 0.2f;
    public float scrollSpeedY = 0.05f;

    public float noiseScale = 1.5f;
    public float distortionStrength = 0.05f;

    private Material mat;
    private Vector2 offset;

    void Start()
    {
        Image img = GetComponent<Image>();

        // Muy importante: Instanciar material
        mat = Instantiate(img.material);
        img.material = mat;
    }

    void Update()
    {
        offset.x += scrollSpeedX * Time.deltaTime;
        offset.y += scrollSpeedY * Time.deltaTime;

        float noiseX = Mathf.PerlinNoise(Time.time * noiseScale, 0f);
        float noiseY = Mathf.PerlinNoise(0f, Time.time * noiseScale);

        Vector2 noiseOffset = new Vector2(noiseX, noiseY) * distortionStrength;

        mat.mainTextureOffset = offset + noiseOffset;
    }
}
