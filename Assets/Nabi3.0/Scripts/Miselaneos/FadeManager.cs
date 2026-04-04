using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeSpeed = 2f;

    private bool hasStarted;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (fadeImage != null)
            fadeImage.gameObject.SetActive(true);
        StartCoroutine(FadeIn());
        hasStarted = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!hasStarted) return;
        if (scene.name == "LoadingScene") return;

        GameObject fadePanel = GameObject.Find("FadePanel");
        if (fadePanel != null)
        {
            fadeImage = fadePanel.GetComponent<Image>();
            fadePanel.SetActive(true);
            StartCoroutine(FadeIn());
        }
    }

    public IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        float alpha = 1f;

        while (alpha > 0)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 0);
    }

    public IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        float alpha = 0f;

        while (alpha < 1)
        {
            alpha += Time.deltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, 1);
    }
}
