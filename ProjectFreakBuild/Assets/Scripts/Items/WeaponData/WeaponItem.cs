using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// What every weapon shares, ranged or melee (see Weapons > Shared Traits in the GDD).
/// It's abstract, so assets are always made from a weapon type like WeaponRangedItem.
///
////////////////////////////////////////////////
public abstract class WeaponItem : ItemSO
{
    [Title("Weapon Data")]
    [Tooltip("Physical or magical. Decides which of the wielder's stats powers the attack (see CoreStats)")]
    public DamageType.AttackType _AttackType = DamageType.AttackType.Physical;

    [Tooltip("The held weapon prefab. Needs a weapon script on it, like WeaponAttackRanged")]
    [Required] //Odin draws a red error box when this is empty
    public GameObject _WeaponPrefab;

    [Tooltip("Attacks per second. 2 = twice a second, 0.2 = once every 5 seconds. This is the number players see, so keep it the real rate")]
    [Min(0.01f)] public float _AttackSpeed = 1f;

    [Tooltip("Camera shake each time the weapon fires. 0 = none. (Separate from the shake when a hit lands)")]
    [Min(0f)] public float _ActivationShake = 0f;

    [Title("Damage Data")]
    [Tooltip("Damage per hit before the wielder's attack stat and other modifiers are added")]
    [Min(0)] public int _BaseDamage = 5;

    [Tooltip("Element of the damage")]
    public DamageType.ElementType _Element = DamageType.ElementType.Normal;

    [Tooltip("How far a hit pushes the target back, in meters. 0 = none. The target's size class can shrink it")]
    [Min(0f)] public float _Knockback = 0f;

    #region Tools
    public float GetAttackTime()
    {
        //function that turns attack speed into seconds per attack (2 per second = 0.5 seconds)
        return 1f / Mathf.Max(_AttackSpeed, 0.01f); //Max stops a divide by 0 if the value is ever 0
    }
    #endregion
}
