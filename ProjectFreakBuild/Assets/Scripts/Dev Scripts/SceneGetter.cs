using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

public class SceneGetter : MonoBehaviour
{
    //private UnityEngine.Object _sceneAsset;
    public SceneLocationSO fileToWrite;

    [Button("Get Scene Name")]
    public void GetSceneName(UnityEngine.Object _sceneAsset)
    {
        if (_sceneAsset == null)
            return;

        string path = _sceneAsset.name;

        fileToWrite._Scene = path;
    }
}
