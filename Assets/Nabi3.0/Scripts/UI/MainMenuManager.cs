using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Navigation")]
    [SerializeField] private GameObject firstSelectedButton;
    [SerializeField] private GameObject firstOptionsSelected;

    [Header("Scene")]
    [SerializeField] private string firstLevelSceneName = "Level1";

    private bool isStartingGame;

    private void Start()
    {
        Time.timeScale = 1f;

        optionsPanel.SetActive(false);

        SetupVideoBackground();

        SelectButton(firstSelectedButton);
    }

    private void SetupVideoBackground()
    {
        GameObject videoObj = GameObject.Find("BackgroundVideoRawImage");
        if (videoObj == null) return;

        VideoPlayer videoPlayer = videoObj.GetComponent<VideoPlayer>();
        RawImage rawImage = videoObj.GetComponent<RawImage>();
        if (videoPlayer == null || rawImage == null) return;

        RenderTexture rt = new RenderTexture(1920, 1080, 0);
        videoPlayer.targetTexture = rt;
        rawImage.texture = rt;
        videoPlayer.Play();
    }

    public void PlayGame()
    {
        if (isStartingGame) return;

        isStartingGame = true;

        SceneTransitionManager.Instance.LoadScene(firstLevelSceneName);
    }

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);

        SelectButton(firstOptionsSelected);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        SelectButton(firstSelectedButton);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game (Editor)");
    }

    private void SelectButton(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }
}
