using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//rune field effect that adds a flat amount to one of the shade's stats (negative amounts take it away)
//used by ability nodes and by every element rune's stat boosts
[System.Serializable]
public class StatChangeEffect : RuneEffect
{
    [Tooltip("Which stat this changes")]
    [FormerlySerializedAs("_statToChange")] //element assets saved this under the old statBoostPackage name, this keeps their values
    public DamageType.StatType _Stat = DamageType.StatType.None;

    [Tooltip("How much to add to the stat. Negative takes it away (a stat never goes below 1 from runes)")]
    [FormerlySerializedAs("_ChangeAmount")] //same as above
    public int _Amount = 1;

    public override string GetSetupProblem()
    {
        //a stat change needs a stat, and an amount of 0 does nothing
        if (_Stat == DamageType.StatType.None) return "Stat Change has no stat picked";
        if (_Amount == 0) return "Stat Change amount is 0";
        return "";
    }
}
