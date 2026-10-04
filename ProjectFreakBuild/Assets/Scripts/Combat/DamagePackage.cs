using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DamagePackage
{
    //a snapshot of an attack at the moment it was made. Built once, then only read. See Damage Package in the GDD

    [Tooltip("Who made the attack. Projectiles and swings never hit their own source")]
    public GameObject _Source;

    [Tooltip("Chance for this hit to crit, 0 to 1. Comes from the weapon or ability (plus runes later). The target rolls it when the hit lands. Staggered targets always take a crit")]
    [Range(0f, 1f)] public float _CritChance = 0f;

    [Tooltip("Damage multiplier on a crit. Only the first (main) entry is multiplied")]
    [Min(1f)] public float _CritMultiplier = 1.5f;

    [Tooltip("How hard the hit shakes the camera")]
    public float _DamageImpactStrength = .25f;

    [Tooltip("How far this hit pushes the target back, in meters. 0 = no knockback. The target's size class can shrink this (see SizeClassRulesSO)")]
    public float _KnockbackDistance = 0f;

    [Tooltip("How likely this hit is to stagger the target, 0 to 1. 0 = can't stagger. The target's size class shrinks it, and bosses ignore it (see EnemyStagger)")]
    [Range(0f, 1f)] public float _StaggerPower = 0f;

    [Tooltip("Friendly fire. Off (default) = the attack skips everyone on the attacker's team. On = it can hit allies too")]
    public bool _HitsAllies = false;

    [Tooltip("Each separate chunk of damage in the attack. The first entry is the main one (the attack itself), the rest are bonus effects")]
    public List<DamageEntry> _Entries;
}
