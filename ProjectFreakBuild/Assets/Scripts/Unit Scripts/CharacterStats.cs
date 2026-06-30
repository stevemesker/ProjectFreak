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

    [Header("Combat Stats")]
    [Tooltip("Stat used for physical melee attacks. Strength is the default")]
    public DamageType.StatType PhysicalPrimaryStat = DamageType.StatType.Strength;
    [Tooltip("Stat used for physical ranged attacks. Agility is the default")]
    public DamageType.StatType PhysicalSecondaryStat = DamageType.StatType.Agility;
    [Tooltip("Stat used for magical melee attacks. Intelect is the default")]
    public DamageType.StatType MagicalPrimaryStat = DamageType.StatType.Intelect;
    [Tooltip("Stat used for magical ranged attacks. Intelect is the default")]
    public DamageType.StatType MagicalSecondaryStat = DamageType.StatType.Intelect;

    [Header("Modifier Pointers")]
    [Tooltip("What stat is used for physical defense. Defense is the default, AGI is usually the other but there is no hard limit")]
    public DamageType.StatType PhysicalDefMod = DamageType.StatType.Defense;
    [Tooltip("What stat is used for magical defense. Spirit is the default")]
    public DamageType.StatType MagicalDefMod = DamageType.StatType.Spirit;

    [Header("Resistances: Half Damage")]
    public List<DamageType.AttackType> AttackTypeResistance;
    public List<DamageType.ElementType> ElementTypeResistance;

    [Header("Immunity: No Damage")]
    public List<DamageType.AttackType> AttackTypeImmunity;
    public List<DamageType.ElementType> ElementTypeImmunity;

    public int TypeToStatFinder(DamageType.StatType type)
    {
        switch(type)
        {
            case DamageType.StatType.Health:
                return _HP;
            case DamageType.StatType.Strength:
                return _STR;
            case DamageType.StatType.Defense:
                return _DEF;
            case DamageType.StatType.Agility:
                return _AGI;
            case DamageType.StatType.Intelect:
                return _INT;
            case DamageType.StatType.Spirit:
                return _SPR;
            case DamageType.StatType.Wisdom:
                return _WIS;
            default:
                return 0;
        }
    }

    public DamageType.StatType GetDefensiveStatType(DamageType.StatType type)
    {
        if (type == DamageType.StatType.Strength || type == DamageType.StatType.Agility || type == DamageType.StatType.Defense)
            return PhysicalDefMod;
        if (type == DamageType.StatType.Intelect || type == DamageType.StatType.Spirit || type == DamageType.StatType.Wisdom)
            return MagicalDefMod;
        return DamageType.StatType.None;
    }

    public DamageType.StatType GetAttackStatType(bool isRanged, DamageType.AttackType type)
    {
        switch(type)
        {
            case DamageType.AttackType.Physical:
                if (isRanged) return PhysicalSecondaryStat;
                else return PhysicalPrimaryStat;
            case DamageType.AttackType.Magical:
                if (isRanged) return MagicalSecondaryStat;
                else return MagicalPrimaryStat;
            default: return DamageType.StatType.None;
        }
    }
    public float GetAttackResistanceModifier(DamageType.AttackType atk, DamageType.ElementType ele)
    {
        if (atk == DamageType.AttackType.None && ele == DamageType.ElementType.None) return 1;

        for (int i = 0; i < AttackTypeResistance.Count; i++)
        {
            if (atk == AttackTypeResistance[i]) return .5f;
        }
        for (int i = 0; i < ElementTypeResistance.Count; i++)
        {
            if (ele == ElementTypeResistance[i]) return .5f;
        }

        return 1;
    }
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
public class EnemyCoreStats : CoreStats
{
    [Header("Loot Drops")]
    [Tooltip("")]
    public int _DropGold;
    [Tooltip("")]
    public List<ItemSO> DropItems;
}

[Serializable]
public class Inventory
{
    [Tooltip("Number of equipment the character can switch between")] public int _EquipmentSize;
    [Tooltip("Inventory size of the specific character")] public int _InventorySize;
    public List<WeaponItem> _EquippedWeapons;
    public Dictionary<ItemSO, int> _BackpackInventory;

    public bool checkInventoryFits(ItemSO item, int amount)
    {
        if (_BackpackInventory.ContainsKey(item))
        {
            if (_BackpackInventory[item] + amount < item.itemStackSizeMax) return true;
            else return false;
        }
        else if (_BackpackInventory.Count < _InventorySize)
        {
            return true;
        }
        return false;
    }

    public bool checkEquippedWeaponFits(WeaponItem x)
    {
        for (int i = 0; i < _EquippedWeapons.Count; i++)
            if (_EquippedWeapons[i] == null) return true;
        return false;
    }

    public void addBackpackInventory (ItemSO x, int y)
    {
        _BackpackInventory.Add(x, y);
    }

    public void addEquipmentInventory(WeaponItem x)
    {
        for (int i = 0; i < _EquippedWeapons.Count; i++)
            if (_EquippedWeapons[i] == null)
            {
                _EquippedWeapons[i] = x;
                return;
            }
    }
}
