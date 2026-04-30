using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class DeathVideoSequence : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject player;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip deathVideo;
    [SerializeField] private VideoClip secondVideo;

    [Header("Config")]
    [SerializeField] private float delayBetweenVideos = 1.5f;
    [SerializeField] private string creditsSceneName;

    private bool sequenceStarted = false;

    // Llamar desde BossReina al terminar el Attack3
    public void TriggerSequence()
    {
        if (sequenceStarted) return;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        sequenceStarted = true;

        GameAudioManager.Instance?.StopMusic();

        yield return PlayVideo(deathVideo);

        yield return new WaitForSeconds(delayBetweenVideos);

        yield return PlayVideo(secondVideo);

        SceneManager.LoadScene(creditsSceneName);
    }

    IEnumerator PlayVideo(VideoClip clip)
    {
        videoPlayer.clip = clip;
        videoPlayer.Play();

        // El VideoPlayer tarda un frame en arrancar; esperar a que isPlaying sea true
        // antes de esperar a que sea false evita saltar el video inmediatamente.
        yield return new WaitUntil(() => videoPlayer.isPlaying);
        yield return new WaitUntil(() => !videoPlayer.isPlaying);
    }
}