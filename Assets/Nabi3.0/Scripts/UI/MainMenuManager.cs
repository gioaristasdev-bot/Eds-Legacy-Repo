using UnityEngine;
using UnityEngine.EventSystems;

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

        SelectButton(firstSelectedButton);
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
