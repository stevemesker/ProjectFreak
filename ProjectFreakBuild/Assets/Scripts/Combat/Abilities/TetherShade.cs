using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TetherShade : AbilityFunction
{
    //brings the shade out tethered. Using it while tethered puts the shade away, using it while released swaps to tethered
    public override void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
    {
        interpreter.AbilIntLog($"{source.name} successfully used Tether Shade!");
        if (ShadeManager._ShadeManager == null) { Debug.LogError("Error! Shade Manager not found, can't use Tether Shade", source); return; }
        ShadeManager._ShadeManager.TetherShade();
    }
}
