using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneReference
{
#if UNITY_EDITOR
    [SerializeField]
    private SceneAsset _sceneAsset;
#endif

    [SerializeField, HideInInspector]
    private string _scenePath;

    public string ScenePath => _scenePath;

#if UNITY_EDITOR
    private void OnValidate()
    {
        _scenePath = _sceneAsset != null
            ? AssetDatabase.GetAssetPath(_sceneAsset)
            : "";
    }
#endif
}