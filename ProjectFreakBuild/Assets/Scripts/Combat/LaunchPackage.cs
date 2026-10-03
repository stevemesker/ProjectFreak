using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LaunchPackage
{
    //information a weapon hands each projectile it fires that isn't damage (damage goes in the DamagePackage)
    //the projectile decides what to do with it, like growing on a full charge. See Ranged Weapon System in the GDD

    [Tooltip("Who fired it")]
    public GameObject _Source;

    [Tooltip("How charged the weapon was when it fired, 0 to 1. Weapons that don't charge send 1 (full power)")]
    [Range(0f, 1f)] public float _ChargePercent = 1f;

    [Tooltip("True if this came from the weapon's finisher cycle")]
    public bool _IsFinisher;
}
