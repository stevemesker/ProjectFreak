using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TestAbility : AbilityFunction
{
    public override void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
    {
        interpreter.AbilIntLog($"{source.name} successfully used an ability!");
    }
}
