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

    // 🔊 NUEVO: AUDIO
    [Header("Menu Audio")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private float musicVolume = 0.5f;

    private AudioSource musicSource;

    private bool isStartingGame;

    private void Start()
    {
        Time.timeScale = 1f;

        optionsPanel.SetActive(false);

        SetupVideoBackground();

        // 🔊 INICIAR MUSICA
        PlayMenuMusic();

        SelectButton(firstSelectedButton);
    }

    // 🔊 NUEVO MÉTODO
    private void PlayMenuMusic()
    {
        if (menuMusic == null) return;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = menuMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D
        musicSource.Play();
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

        // 🔊 OPCIONAL: parar música al iniciar juego
        if (musicSource != null)
            musicSource.Stop();

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
