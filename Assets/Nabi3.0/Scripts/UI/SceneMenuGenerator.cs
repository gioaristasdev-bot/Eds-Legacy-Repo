using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneMenuGenerator : MonoBehaviour
{
    [System.Serializable]
    public class SceneData
    {
        public string displayName;
        public string sceneName;
    }

    public List<SceneData> scenes = new List<SceneData>();

    public GameObject buttonPrefab;
    public Transform container;

    private void Start()
    {
        // Limpia
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        // Genera solo las escenas que tú pongas
        foreach (var scene in scenes)
        {
            GameObject btn = Instantiate(buttonPrefab, container);

            btn.GetComponentInChildren<TMP_Text>().text = scene.displayName;

            string sceneNameCopy = scene.sceneName;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                SceneTransitionManager.Instance.LoadScene(sceneNameCopy);
            });
        }
    }
}