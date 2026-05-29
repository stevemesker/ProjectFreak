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
    [Tooltip("Maximum health points can be")]
    public int _HP;
    [Tooltip("Current health. Cannot go above HP and being brought to 0 defeats the unit")]
    public int _Health;
    [Tooltip("Physical strength and effectiveness of physical attacks/weapons")]
    public int _STR;
    [Tooltip("Physical resilience and lowers incoming physical damage")]
    public int _DEF;
    [Tooltip("How fast a character moves and how quickly they can use physical attacks/weapons")]
    public int _AGI;
    
    [Header("Mental Stats")]
    [Tooltip("Mental skill and effectiveness with magic attacks/weapons")]
    public int _INT;
    [Tooltip("Mental resilience and lowers incoming magic damage")]
    public int _SPR;
    [Tooltip("How fast a character can use magic abilities/weapons and how quickly/effectively they progress in levels")]
    public int _WIS;

    [Header("Other Stats")]
    [Tooltip("How strong overall a creature is. For enemies it is completely arbitrary but for Hazen and his shades it effects various stat point distribution")]
    public int _LVL;
    [Tooltip("Name of the Unit")]
    public string _Name;
}

[Serializable]
public class PartyStats : CoreStats
{
    [Header("Party Stats")]
    [Tooltip("How much current xp the unit has. Needed level is calculated elsewhere")]
    public int _XP;
}

[Serializable]
public class ShadeStats : PartyStats
{
    [Header("Shade Stats")]
    [Tooltip("Discepline. How disciplined the shade is. The higher the discipline the more interactions Hazen will have with this shade")]
    public int _DIS;
    [Tooltip("Wildness. Determines how aggressive the shade can be and how willing to listen to orders it is. Higher wild means a stronger monster but much less controllable")]
    public int _WILD;
}

[Serializable]
public class PlayerStats : PartyStats
{
    [Header("Tamer Stats")]
    [Tooltip("Energy used to allow shades to do special actions like using abilities and even existing on its own")]
    public float _SOUL;
    [Tooltip("Number of shades Hazen can have")]
    public int _SHA; //number of allowed shades
    [Tooltip("Index of the current equipped shade")]
    public int _CurrentShade; //index of the current equipped shade
    [Tooltip("List of all shades")]
    public List<ShadeStats> _Shades; //list of all shades
}

[Serializable]
public class EnemyStats : CoreStats
{
    [Header("Loot Drops")]
    [Tooltip("")]
    public int _DropGold;
    [Tooltip("")]
    public List<ItemSO> DropItems;
}
