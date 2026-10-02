using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class PersonalitySetup
{
    //Picks a personality preset and optionally overrides single values for just this unit (or enemy type).
    //Used on the EnemySO (AI foldout) for enemies, and on the UnitBrain for units without an EnemySO (shades).
    //BuildPersonality() makes the brain's own runtime copy: preset values first, then any turned-on overrides on top.

    [Tooltip("The shared personality to start from. Leave empty to start from the default values (a normal enemy)")]
    public AIPersonalitySO _Preset;

    [Tooltip("Turn on to use a different aggression than the preset")]
    public bool _OverrideAggression;

    [ShowIf("_OverrideAggression")]
    [Tooltip("How much it wants to fight, 0 to 1")]
    [Range(0f, 1f)] public float _Aggression = 0.6f;

    [Tooltip("Turn on to use a different fear than the preset")]
    public bool _OverrideFear;

    [ShowIf("_OverrideFear")]
    [Tooltip("How easily it gets scared, 0 to 1")]
    [Range(0f, 1f)] public float _Fear = 0.3f;

    [Tooltip("Turn on to use a different roam than the preset")]
    public bool _OverrideRoam;

    [ShowIf("_OverrideRoam")]
    [Tooltip("How much it drifts and goes looking for trouble, 0 to 1")]
    [Range(0f, 1f)] public float _Roam = 0.3f;

    [Tooltip("Turn on to use a different loyalty than the preset")]
    public bool _OverrideLoyalty;

    [ShowIf("_OverrideLoyalty")]
    [Tooltip("Shades only. How strongly it sticks near the player, 0 to 1")]
    [Range(0f, 1f)] public float _Loyalty = 0.7f;

    [Tooltip("Turn on to use a different commit time than the preset")]
    public bool _OverrideCommitTime;

    [ShowIf("_OverrideCommitTime")]
    [Tooltip("How long it sticks with a choice before changing its mind easily, in seconds")]
    [Min(0f)] public float _CommitTime = 1f;

    public AIPersonality BuildPersonality()
    {
        //function that makes a new personality from the preset (or the defaults) with any overrides applied on top
        AIPersonality personality = new AIPersonality(); //starts as the default values
        if (_Preset != null && _Preset._Values != null) personality = _Preset._Values.Copy(); //a copy, so the preset asset never changes

        if (_OverrideAggression) personality._Aggression = _Aggression;
        if (_OverrideFear) personality._Fear = _Fear;
        if (_OverrideRoam) personality._Roam = _Roam;
        if (_OverrideLoyalty) personality._Loyalty = _Loyalty;
        if (_OverrideCommitTime) personality._CommitTime = _CommitTime;
        return personality;
    }
}
