using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//rune field effect that gives the shade an ability while it's active
//nothing uses granted abilities yet, the Shade Manager reads them once the runtime entries are built (Rune Field Overhaul Plan, step 3)
[System.Serializable]
public class GrantAbilityEffect : RuneEffect
{
    [Tooltip("The ability the shade gets")]
    public AbilitySO _Ability;

    public override string GetSetupProblem()
    {
        if (_Ability == null) return "Grant Ability has no ability assigned";
        return "";
    }
}
