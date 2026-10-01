using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlShade : AbilityFunction
{
    public override void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
    {
        interpreter.AbilIntLog($"{source.name} successfully used the ability ControlShade!");
        if (ShadeManager._ShadeManager == null) { Debug.LogError("Error! Shade Manager not found, can't use ControlShade", source); return; }
        ShadeManager._ShadeManager.ShadeControlAbility();
    }
}
