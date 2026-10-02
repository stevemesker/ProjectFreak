using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroy : MonoBehaviour
{
    //local variables
    bool _isQuitting; //true once the game (or play mode) is shutting down

    private void OnEnable()
    {
        //keeps this object alive between scene loads
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        //Unity calls this before everything gets turned off when the game closes or play mode stops
        _isQuitting = true;
    }

    private void OnDisable()
    {
        //when this object is turned off, move it back into the active scene so it gets cleaned up with that scene
        //skip it while shutting down: the active scene is being unloaded then, and moving into it throws an error
        if (_isQuitting) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() == false || activeScene.isLoaded == false) return; //no loaded scene to move into
        SceneManager.MoveGameObjectToScene(gameObject, activeScene);
    }
}
