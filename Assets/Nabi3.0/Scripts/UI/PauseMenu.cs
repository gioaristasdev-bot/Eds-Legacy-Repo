using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using NABHI.UI;
using NABHI.Character;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmExitPanel;

    [SerializeField] private GameObject firstPauseSelected;
    [SerializeField] private GameObject firstConfirmSelected;

    private bool isPaused = false;
    private CharacterController2D characterController;

    private void Awake()
    {
        characterController = FindObjectOfType<CharacterController2D>();
    }

    void Update()
    {
        // 🚫 Si estamos en GameOver, no permitir pausa
        if (GameOverMenu.IsGameOver)
            return;

        // ESC o botón Start
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else
            {
                if (confirmExitPanel.activeSelf)
                {
                    CancelReturn();
                }
                else
                {
                    ResumeGame();
                }
            }
        }

        // Botón B del mando
        if (isPaused && Input.GetKeyDown(KeyCode.JoystickButton1))
        {
            if (confirmExitPanel.activeSelf)
            {
                CancelReturn();
            }
            else
            {
                ResumeGame();
            }
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);
        confirmExitPanel.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;

        if (characterController != null)
            characterController.SetInputBlocked(true);

        SelectButton(firstPauseSelected);
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        confirmExitPanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        if (characterController != null)
        {
            characterController.SetInputBlocked(false);
            characterController.SkipInputForFrames(1);
        }

        EventSystem.current.SetSelectedGameObject(null);
    }

    public void AskReturnToMenu()
    {
        pausePanel.SetActive(false);
        confirmExitPanel.SetActive(true);

        // ⚠ NO tocamos Time.timeScale aquí
        SelectButton(firstConfirmSelected);
    }

    public void CancelReturn()
    {
        confirmExitPanel.SetActive(false);
        pausePanel.SetActive(true);

        // ⚠ Seguimos en pausa
        SelectButton(firstPauseSelected);
    }

    public void ConfirmReturn()
    {
        Time.timeScale = 1f;
        isPaused = false;

        SceneTransitionManager.Instance.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SelectButton(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }
}