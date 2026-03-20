using UnityEngine;
using TMPro;
using System.Collections;

public class LoadingTextAnimation : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private float speed = 0.4f;

    private void Start()
    {
        StartCoroutine(AnimateLoading());
    }

    IEnumerator AnimateLoading()
    {
        int dotCount = 0;

        while (true)
        {
            dotCount++;

            if (dotCount > 3)
                dotCount = 1;

            loadingText.text = "Loading" + new string('.', dotCount);

            yield return new WaitForSeconds(speed);
        }
    }
}

