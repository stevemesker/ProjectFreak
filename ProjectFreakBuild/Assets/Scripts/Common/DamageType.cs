using ElementType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DamageType
{
    public enum AttackType
    {
        None,
        Physical,
        Explosion,
        Magical,
        TrueDamage
    }
    public enum StatType
    {
        None,
        Health,
        Strength,
        Defense,
        Agility,
        Intelect,
        Spirit,
        Wisdom,
        Discipline,
        Wild
    }
    public enum ElementType
    {
        None,
        Normal,
        Fire,
        Water,
        Ice,
        Electric,
        Earth,
        Poison,
        Dark,
        Light,
        Healing
    }
}

[SerializeField]
public struct DamageEntry
{
    public int _Damage;
    public DamageType.AttackType _atkType;
    public DamageType.StatType _statType;
    public DamageType.ElementType _elementType;
}
