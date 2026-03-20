using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    private string sceneToLoad;
    private bool isLoading;

    private void Awake()
    {
        Debug.Log("SceneTransitionManager Awake ejecutado");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading) return;

        sceneToLoad = sceneName;
        StartCoroutine(LoadWithFade());
    }

    private IEnumerator LoadWithFade()
    {
        isLoading = true;

        if (FadeManager.Instance != null)
            yield return StartCoroutine(FadeManager.Instance.FadeOut());

        LoadingManager.SceneToLoad = sceneToLoad;
        SceneManager.LoadScene("LoadingScene");
    }

}