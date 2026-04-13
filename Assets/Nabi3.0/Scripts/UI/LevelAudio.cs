using UnityEngine;

public class LevelAudio : MonoBehaviour
{
    [SerializeField] private AudioClip levelMusic;

    private void Start()
    {
        if (GameAudioManager.Instance != null && levelMusic != null)
        {
            GameAudioManager.Instance.PlayMusic(levelMusic);
        }
    }
}
