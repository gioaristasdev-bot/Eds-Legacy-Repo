using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoDialogueSequence : MonoBehaviour
{
    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip[] videos;

    [Header("UI Dialogos")]
    [SerializeField] private GameObject[] dialogueUI;

    [Header("Configuración")]
    [SerializeField] private string nextSceneName;

    private int currentIndex = 0;
    private bool isPlaying = false;

    void Start()
    {
        // Apagar todos los diálogos
        foreach (GameObject ui in dialogueUI)
        {
            if (ui != null)
                ui.SetActive(false);
        }

        StartSequence();
    }

    void Update()
    {
        if (isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetButtonDown("Submit"))
            {
                Next();
            }
        }
    }

    void StartSequence()
    {
        currentIndex = 0;
        isPlaying = true;

        PlayCurrent();
    }

    void PlayCurrent()
    {
        // 🔴 Seguridad
        if (currentIndex >= videos.Length)
        {
            EndSequence();
            return;
        }

        // Reproducir video
        videoPlayer.clip = videos[currentIndex];
        videoPlayer.Play();

        // Mostrar UI correspondiente
        ShowDialogue(currentIndex);
    }

    void ShowDialogue(int index)
    {
        // Apagar todos
        foreach (GameObject ui in dialogueUI)
        {
            if (ui != null)
                ui.SetActive(false);
        }

        // Encender el actual
        if (dialogueUI[index] != null)
            dialogueUI[index].SetActive(true);
    }

    void Next()
    {
        currentIndex++;

        if (currentIndex < videos.Length)
        {
            PlayCurrent();
        }
        else
        {
            EndSequence();
        }
    }

    void EndSequence()
    {
        isPlaying = false;

        // Apagar todo
        foreach (GameObject ui in dialogueUI)
        {
            if (ui != null)
                ui.SetActive(false);
        }

        videoPlayer.Stop();

        // 🔥 Cargar escena del juego
        SceneManager.LoadScene(nextSceneName);
    }
}
