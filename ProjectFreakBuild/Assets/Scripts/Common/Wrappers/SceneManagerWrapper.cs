using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        GameManager._GameManager.GetComponent<SceneManagerObject>().sceneLocationDataChange(data);
    }

    public void changeScene(string sceneName)
    {
        //changes scene based on input string
        GameManager._GameManager.GetComponent<SceneManagerObject>().changeScene(sceneName);
    }

    public void HudFadeOnOpen(float speed)
    {
        //ensures the hud will fade in when scene is loaded
        GameManager._GameManager.GetComponent<SceneManagerObject>().HudFadeOnOpen(speed);
    }
    public void ActiveOpeningScene()
    {
        GameManager._GameManager.GetComponent<SceneManagerObject>().activateOpeningScene();
    }

    public void LoadOpeningScene()
    {
        GameManager._GameManager.GetComponent<SceneManagerObject>().loadStartScene();
    }
}
