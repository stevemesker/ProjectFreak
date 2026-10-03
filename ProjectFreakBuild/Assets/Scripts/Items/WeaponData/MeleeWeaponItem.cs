using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Data for a melee weapon: its reach and its combo (a chain of swings that plays in order while attacking).
/// WeaponAttackMelee reads it and does the swinging. See Melee Weapon System in the GDD.
///
////////////////////////////////////////////////
[CreateAssetMenu(fileName = "SO_Weapon_Melee_Name_0", menuName = "Combat/Melee Weapon", order = 0)]
public class MeleeWeaponItem : WeaponItem
{
    [Title("Melee")]
    [Tooltip("Multiplier on every swing shape's reach. A dagger might be 0.7, a greatsword 1.5")]
    [Min(0.1f)] public float _Reach = 1f;

    [Tooltip("Seconds without attacking before the combo goes back to the first hit")]
    [Min(0f)] public float _ComboResetTime = 0.8f;

    [Title("Combo", "Plays in order while attacking. The last hit is the finisher")]
    [InfoBox("$" + nameof(GetComboWarning), InfoMessageType.Warning, nameof(HasComboWarning))] //the $ tells Odin to call the function for the message text
    [ListDrawerSettings(ShowIndexLabels = true)] //numbers each hit in the inspector
    public List<ComboStepEntry> _Combo = new List<ComboStepEntry>();

    [ShowInInspector, ReadOnly] //ShowInInspector shows a value that isn't saved, here the result of a calculation
    [PropertyTooltip("How long one full combo takes, in seconds: hits ÷ attack speed. The step timings get stretched or squeezed to fit")] //Odin's tooltip, since Unity's only works on fields
    float FullComboSeconds { get { return GetComboSeconds(); } }

    //local variables
    [System.NonSerialized] bool _warningsLogged; //never saved to the asset, so safe to change at runtime

    #region Timing
    public float GetComboSeconds()
    {
        //function for how long a full combo takes: attack speed is the average attacks per second
        if (_Combo == null || _Combo.Count == 0) return 0f;
        return _Combo.Count / Mathf.Max(_AttackSpeed, 0.01f);
    }

    public float GetTimeScale()
    {
        //function that turns the steps' relative timings into real seconds, so the whole combo takes GetComboSeconds()
        //e.g. steps adding up to 1.5 with a 3 second combo means every timing gets doubled
        float totalRelativeTime = 0f;
        if (_Combo != null)
        {
            for (int i = 0; i < _Combo.Count; i++)
            {
                if (_Combo[i] != null) totalRelativeTime += _Combo[i].GetTotalTime();
            }
        }
        if (totalRelativeTime <= 0f) return 1f; //nothing to scale, use the timings as they are
        return GetComboSeconds() / totalRelativeTime;
    }
    #endregion

    #region Tools
    public void LogSetupWarnings(Object context)
    {
        //logs the same problems the inspector warns about, once per play session
        if (_warningsLogged) return;
        _warningsLogged = true;
        if (HasComboWarning()) Debug.LogWarning($"Warning! {name}: {GetComboWarning()}", context);
    }

    string GetComboWarning()
    {
        //function that collects every combo setup problem into one message. Empty = all good
        if (_Combo == null || _Combo.Count == 0) return "The combo is empty, this weapon can't attack.";

        string warning = "";
        for (int i = 0; i < _Combo.Count; i++)
        {
            if (_Combo[i] == null) { warning += $"Hit {i} is empty. "; continue; }
            if (_Combo[i]._Swing == null) warning += $"Hit {i} has no swing shape, it won't hit anything. ";
        }
        return warning;
    }

    bool HasComboWarning() { return GetComboWarning() != ""; }
    #endregion
}

[System.Serializable]
public class ComboStepEntry
{
    //one hit in a melee combo

    [Tooltip("The swing shape this hit uses (Slash, Thrust, Spin...)")]
    public SwingShapeSO _Swing;

    [Title("Timing", "Relative: the combo is stretched or squeezed to match attack speed")]
    [Tooltip("Wind up before the swing can hit")]
    [Min(0f)] public float _Windup = 0.15f;

    [Tooltip("How long the swing sweeps and can hit")]
    [Min(0.01f)] public float _ActiveTime = 0.15f;

    [Tooltip("Recovery after the swing before the next hit can start")]
    [Min(0f)] public float _Recovery = 0.2f;

    [Title("Hit")]
    [Tooltip("× the weapon's damage")]
    [Min(0f)] public float _DamageMultiplier = 1f;

    [Tooltip("× the weapon's knockback")]
    [Min(0f)] public float _KnockbackMultiplier = 1f;

    [Tooltip("How likely this hit is to stagger, 0 to 1. The enemy's size class shrinks it (Small ×1, Medium ×0.4, Large ×0.15, Huge never). Bosses ignore it. Finishers usually get the most")]
    [Range(0f, 1f)] public float _StaggerPower = 0.5f;

    [Tooltip("The wielder's move speed while this hit plays (wind up to the end of recovery). 1 = normal, 0.5 = half speed")]
    [Range(0f, 1f)] public float _MoveMultiplier = 1f;

    public float GetTotalTime()
    {
        //function for this step's relative length before scaling
        return _Windup + _ActiveTime + _Recovery;
    }
}
