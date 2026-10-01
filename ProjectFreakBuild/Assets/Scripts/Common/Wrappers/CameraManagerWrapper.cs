using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManagerWrapper : MonoBehaviour
{
    //Script that accesses global camera setting from the game manager > camera manager
    public void SetCamTargetToPlayer()
    {
        //points the gameplay camera back at the player
        if (ManagerTester("SetCamTargetToPlayer") == false) return;
        CameraManager._CamManager.SetCamTargetToPlayer();
    }

    public void SetGameplayCameraPriority(int priority)
    {
        //changes the gameplay camera's cinemachine priority
        if (ManagerTester("SetGameplayCameraPriority") == false) return;
        CameraManager._CamManager.SetGameplayCameraPriority(priority);
    }

    #region Tools
    bool ManagerTester(string type)
    {
        //checks the camera manager exists before forwarding a call to it
        if (CameraManager._CamManager == null) { Debug.LogError($"Error! {type} wrapper on {gameObject.name} couldn't find the Camera Manager", this); return false; }
        return true;
    }
    #endregion
}
