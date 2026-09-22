using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlShade : AbilityFunction
{
    public override void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
    {
        interpreter.AbilIntLog($"{source.name} successfully used the ability ControlShade!");
        GameManager._GameManager.GetComponent<ShadeManager>().ShadeControlAbility();
    }
}
