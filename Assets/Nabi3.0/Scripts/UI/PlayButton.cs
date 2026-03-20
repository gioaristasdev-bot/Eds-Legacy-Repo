using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public void PlayGame(string sceneName)
    {
        SceneTransitionManager.Instance.LoadScene(sceneName);
    }
}