using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDWrapper : MonoBehaviour
{
    //function that calls fade outs
    public void FadeHudIn(float speed)
    {
        if (HUDManager._HUD == null) return;
        HUDManager._HUD.FadeIn(speed);
    }
    public void FadeHudOut(float speed)
    {
        if (HUDManager._HUD == null) return;
        HUDManager._HUD.FadeOut(speed);
    }
}
