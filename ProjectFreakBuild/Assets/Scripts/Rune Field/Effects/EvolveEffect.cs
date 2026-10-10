using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//rune field effect that evolves the shade into another form. A node with one of these is an evolution gate
//the shade evolves when a gate in its open zone turns on (ShadeManager.TryEvolve). The effect also compiles like any other, but nothing reads it there
[System.Serializable]
public class EvolveEffect : RuneEffect
{
    [Tooltip("The form the shade evolves into")]
    public ShadeEvolutionSO _Evolution;

    public override string GetSetupProblem()
    {
        if (_Evolution == null) return "Evolve has no evolution assigned";
        return "";
    }
}
