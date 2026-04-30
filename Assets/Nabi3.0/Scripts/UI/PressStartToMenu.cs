using UnityEngine;
using UnityEngine.SceneManagement;

public class PressStartToMenu : MonoBehaviour
{
    [SerializeField] private string menuSceneName;

    void Update()
    {
        if (
            Input.GetKeyDown(KeyCode.Return) ||     // Enter
            Input.GetKeyDown(KeyCode.Space) ||      // Space
            Input.GetButtonDown("Submit")        // A / X
            //Input.GetButtonDown("Start")            // Start (si está configurado)
            
        )
        {
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
