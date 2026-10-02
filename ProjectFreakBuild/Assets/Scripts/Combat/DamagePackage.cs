using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamagePackage 
{
    public GameObject _Source;

    public float _CritMultiplier;

    public float _DamageImpactStrength = .25f;

    [Tooltip("How far this hit pushes the target back, in meters. 0 = no knockback. The target's size class can shrink this (see SizeClassRulesSO)")]
    public float _KnockbackDistance = 0f;

    public List<DamageEntry> _Entries;
}
