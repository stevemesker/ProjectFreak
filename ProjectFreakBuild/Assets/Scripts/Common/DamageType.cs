using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Every enum value has an explicit number (= 3). Unity saves enums on assets as numbers, so if a value is ever removed
//or added in the middle, the numbers of everything else stay the same and saved assets don't shift.
//When adding a value, give it the next unused number. When removing one, don't reuse its number.
namespace DamageType
{
    public enum AttackType
    {
        None = 0,
        Physical = 1,
        Explosion = 2,
        Magical = 3,
        TrueDamage = 4
    }

    public enum StatType
    {
        None = 0,
        Health = 1,
        Strength = 2,
        Defense = 3,
        Agility = 4,
        Intellect = 5,
        Spirit = 6,
        //7 was Wisdom (removed in the damage overhaul, Oct 2026). Don't reuse 7
        Discipline = 8,
        Wild = 9
    }

    public enum ElementType
    {
        //Normal is non-elemental and the default. Nothing resists it or is weak to it. See Elemental Affinity in the GDD
        Normal = 0,

        //primary
        Fire = 1,
        Water = 2,
        Air = 3,
        Earth = 4,

        //secondary (made from two primaries, but its own element in combat)
        Ice = 5,
        Lava = 6,
        Lightning = 7,
        Plant = 8,

        //end game
        Void = 9,
        Light = 10
    }
}

[System.Serializable]
public struct DamageEntry
{
    //one chunk of damage in an attack (like a sword's physical hit, or its bonus fire). See Damage Package in the GDD

    [Tooltip("Raw damage before the target's defense: attack stat × weapon power × multipliers. Kept as a decimal, the target rounds up at the very end")]
    public float _Damage;

    [Tooltip("The attacker's attack stat when the attack was made (STR, AGI or INT). The target compares its defense against this. Traps fill it with a faux stat")]
    public float _AttackStat;

    [Tooltip("Physical, Magical, Explosion or TrueDamage. True damage skips defense, but resistances and immunities still apply")]
    public DamageType.AttackType _atkType;

    [Tooltip("Which stat the attack used. Decides which defense stat blocks it (DEF for physical stats, SPR for magical ones)")]
    public DamageType.StatType _statType;

    [Tooltip("Element of this damage. Normal = non-elemental")]
    public DamageType.ElementType _elementType;
}
