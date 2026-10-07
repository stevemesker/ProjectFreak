using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReturnShade : AbilityFunction
{
    //puts the shade away, whatever form it's in
    public override void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
    {
        interpreter.AbilIntLog($"{source.name} successfully used Return Shade!");
        if (ShadeManager._ShadeManager == null) { Debug.LogError("Error! Shade Manager not found, can't use Return Shade", source); return; }
        ShadeManager._ShadeManager.ReturnShade();
    }
}
