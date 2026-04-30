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

    void Update()
    {
        if (sequenceStarted) return;

        // 👇 Detecta muerte SIN usar PlayerHealth
        if (player == null || !player.activeInHierarchy)
        {
            StartCoroutine(PlaySequence());
        }
    }

    IEnumerator PlaySequence()
    {
        sequenceStarted = true;

        // 🎥 VIDEO 1
        videoPlayer.clip = deathVideo;
        videoPlayer.Play();

        yield return new WaitUntil(() => !videoPlayer.isPlaying);

        // ⏱️ pausa
        yield return new WaitForSeconds(delayBetweenVideos);

        // 🎥 VIDEO 2
        videoPlayer.clip = secondVideo;
        videoPlayer.Play();

        yield return new WaitUntil(() => !videoPlayer.isPlaying);

        // ➡️ créditos
        SceneManager.LoadScene(creditsSceneName);
    }
}