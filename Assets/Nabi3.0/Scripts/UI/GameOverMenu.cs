using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NABHI.UI
{
    public class GameOverMenu : MonoBehaviour
    {
        public static bool IsGameOver = false;
        [Header("Referencias")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private NABHI.Character.PlayerHealth playerHealth;
        [SerializeField] private Button continueButton;

        [Header("Configuración")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Start()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            // Suscribirse al evento de muerte
            if (playerHealth != null)
                playerHealth.OnDeath.AddListener(ShowGameOverMenu);
        }

        private void ShowGameOverMenu()
        {
            IsGameOver = true;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            Time.timeScale = 0f;

            StartCoroutine(SelectContinueButton());
        }

        // Botón CONTINUAR
        public void ContinueGame()
        {
            IsGameOver = false;
            Time.timeScale = 1f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (playerHealth != null)
                playerHealth.Revive();
        }

        // Botón VOLVER AL MENÚ
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private System.Collections.IEnumerator SelectContinueButton()
        {
            yield return null; // Esperar un frame

            EventSystem.current.SetSelectedGameObject(null);

            if (continueButton != null)
                continueButton.Select();
        }
    }


}