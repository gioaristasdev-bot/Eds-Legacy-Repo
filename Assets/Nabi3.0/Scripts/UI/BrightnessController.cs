using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BrightnessController : MonoBehaviour
{
    public static BrightnessController Instance;

    [SerializeField] private Image brightnessImage;
    [SerializeField] private Slider brightnessSlider;

    private float brightnessValue = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Cargar valor guardado
        brightnessValue = PlayerPrefs.GetFloat("Brightness", 0f);

        ApplyBrightness(brightnessValue);

        if (brightnessSlider != null)
        {
            brightnessSlider.value = brightnessValue;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }
    }

    public void SetBrightness(float value)
    {
        brightnessValue = value;
        ApplyBrightness(value);

        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness(float value)
    {
        if (brightnessImage != null)
        {
            Color color = brightnessImage.color;
            color.a = value;
            brightnessImage.color = color;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyBrightness(brightnessValue);
    }
}