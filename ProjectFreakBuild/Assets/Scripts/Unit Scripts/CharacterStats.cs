using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

/// <summary>
/// 
/// Base stats that all units that can be effected by combat will reference
/// 
/// </summary>

[Serializable]
public class CoreStats
{
    [Header("Physical Stats")]
    public int _HP;
    public int _Health;
    public int _STR;
    public int _DEF;
    public int _AGI;
    
    [Header("Mental Stats")]
    public int _INT;
    public int _SPR;
    public int _WIS;

    [Header("Other Stats")]
    public int _LVL;
    public int _Name;
}

[Serializable]
public class PartyStats : CoreStats
{
    [Header("Party Stats")]
    public int _XP;
}

[Serializable]
public class ShadeStats : PartyStats
{
    public int _DIS;
    public int _WILD;
}

[Serializable]
public class PlayerStats : PartyStats
{
    [Header("Tamer Stats")]
    public float _SOUL;
    public int _SHA; //number of allowed shades
    public int _CurrentShade; //index of the current equipped shade
    public List<ShadeStats> _Shades; //list of all shades
}

[Serializable]
public class EnemyStats : CoreStats
{
    [Header("Loot Drops")]
    public int _DropGold;
    public List<ItemSO> DropItems;
}
