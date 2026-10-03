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

    [Tooltip("How likely this hit is to stagger the target, 0 to 1. 0 = can't stagger. The target's size class shrinks it, and bosses ignore it (see EnemyStagger)")]
    [Range(0f, 1f)] public float _StaggerPower = 0f;

    public List<DamageEntry> _Entries;
}
