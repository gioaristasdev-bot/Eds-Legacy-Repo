using UnityEngine;

[CreateAssetMenu(fileName = "NewSceneInfo", menuName = "Game/Scene Info")]
public class SceneInfo : ScriptableObject
{
    public string sceneName;
    public string displayName;
}