using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class SceneManagerWrapper : MonoBehaviour
{
    //Script that accesses changing scenes from the game manager singleton
    public void changeLocation(SceneLocationSO data)
    {
        //change scenes based on scenelocationSO data
        //must have that scriptable object to work
        if (data == null)
        {
            Debug.LogError("Error! No scene data was used. Use alternative scene changing, this one ain't it good sir");
            return;
        }
        SceneManagerObject._SceneManager.sceneLocationDataChange(data);
    }

    public void changeScene(string sceneName)
    {
        //changes scene based on input string
        SceneManagerObject._SceneManager.changeScene(sceneName);
    }

    public void HudFadeOnOpen(float speed)
    {
        //ensures the hud will fade in when scene is loaded
        SceneManagerObject._SceneManager.HudFadeOnOpen(speed);
    }
    public void ActiveOpeningScene()
    {
        SceneManagerObject._SceneManager.activateOpeningScene();
    }

    public void LoadOpeningScene()
    {
        SceneManagerObject._SceneManager.loadStartScene();
    }

    [Button("Test")]
    public void testSingleton()
    {
        if (SceneManagerObject._SceneManager != null)
        {
            print("Pow");
        }
        else
        {
            print("Hmmmmm");
        }
    }
}
