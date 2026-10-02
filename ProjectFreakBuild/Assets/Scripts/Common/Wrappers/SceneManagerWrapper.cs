using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SceneManagerWrapper : MonoBehaviour
{
    //Script that accesses changing scenes from the game manager singleton
    public void ChangeLocation(SceneLocationSO data)
    {
        //change scenes based on scenelocationSO data
        //must have that scriptable object to work
        if (ManagerTester("ChangeLocation") == false) return;
        if (data == null)
        {
            Debug.LogError($"Error! No scene data was given to ChangeLocation on {gameObject.name}. Use alternative scene changing, this one ain't it good sir", this);
            return;
        }
        SceneManagerObject._SceneManager.SceneLocationDataChange(data);
    }

    public void ChangeScene(string sceneName)
    {
        //changes scene based on input string
        if (ManagerTester("ChangeScene") == false) return;
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogError($"Error! No scene name was given to ChangeScene on {gameObject.name}", this); return; } //stops a blank inspector field from trying to load a scene called ""
        SceneManagerObject._SceneManager.ChangeScene(sceneName);
    }

    public void HudFadeOnOpen(float speed)
    {
        //ensures the hud will fade in when scene is loaded
        if (ManagerTester("HudFadeOnOpen") == false) return;
        SceneManagerObject._SceneManager.HudFadeOnOpen(speed);
    }

    public void ActiveOpeningScene()
    {
        //turns on the opening scene that was loaded in the background
        if (ManagerTester("ActiveOpeningScene") == false) return;
        SceneManagerObject._SceneManager.ActivateOpeningScene();
    }

    public void LoadOpeningScene()
    {
        //starts loading the opening scene
        if (ManagerTester("LoadOpeningScene") == false) return;
        SceneManagerObject._SceneManager.LoadStartScene();
    }

    #region Tools
    bool ManagerTester(string type)
    {
        //checks the scene manager exists before forwarding a call to it
        if (SceneManagerObject._SceneManager == null) { Debug.LogError($"Error! {type} wrapper on {gameObject.name} couldn't find the Scene Manager", this); return false; }
        return true;
    }
    #endregion

    #region Test Tools
    [Button("Test")]
    public void TestSingleton()
    {
        //editor button that checks if the scene manager can be found
        if (ManagerTester("TestSingleton")) Debug.Log("Scene Manager found");
    }
    #endregion
}
