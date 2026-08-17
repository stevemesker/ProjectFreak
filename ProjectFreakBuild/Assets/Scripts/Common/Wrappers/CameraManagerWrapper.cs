using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManagerWrapper : MonoBehaviour
{
    //Script that accesses global camera setting from the game manager > camera manager
    public void setCamTargetToPlayer()
    {
        CameraManager._CamManager.setCamTargetToPlayer();
    }

    public void setGameplayCameraPriority(int priority)
    {
        CameraManager._CamManager.setGameplayCameraPriority(priority);
    }
}
