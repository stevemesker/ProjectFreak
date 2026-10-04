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
    [Tooltip("Mental fortitude and lowers incoming magic damage")]
    public int _SPR;

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
    [Tooltip("Stat used for magical melee attacks. Intellect is the default")]
    public DamageType.StatType MagicalPrimaryStat = DamageType.StatType.Intellect;
    [Tooltip("Stat used for magical ranged attacks. Intellect is the default")]
    public DamageType.StatType MagicalSecondaryStat = DamageType.StatType.Intellect;

    [Header("Modifier Pointers")]
    [Tooltip("What stat is used for physical defense. Defense is the default, AGI is usually the other but there is no hard limit")]
    public DamageType.StatType PhysicalDefMod = DamageType.StatType.Defense;
    [Tooltip("What stat is used for magical defense. Spirit is the default")]
    public DamageType.StatType MagicalDefMod = DamageType.StatType.Spirit;

    [Header("Resistances: Half Damage")]
    [Tooltip("Attack types this unit takes half damage from")]
    public List<DamageType.AttackType> AttackTypeResistance;
    [Tooltip("Elements this unit takes half damage from")]
    public List<DamageType.ElementType> ElementTypeResistance;

    [Header("Immunity: No Damage")]
    [Tooltip("Attack types this unit takes no damage from")]
    public List<DamageType.AttackType> AttackTypeImmunity;
    [Tooltip("Elements this unit takes no damage from. Lava is its own element, so Fire immunity doesn't cover it")]
    public List<DamageType.ElementType> ElementTypeImmunity;

    //local variables
    const float ResistanceMultiplier = 0.5f; //how much damage a resistance lets through. todo: how resistances stack is still undecided in the GDD (Damage Balance)
    const int StatFloor = 1; //stats used in damage math never go below this, however many negative runes are stacked

    public int TypeToStatFinder(DamageType.StatType type)
    {
        //function that turns a stat type into this unit's value for it
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
            case DamageType.StatType.Intellect:
                return _INT;
            case DamageType.StatType.Spirit:
                return _SPR;
            default:
                return 0;
        }
    }

    public int GetCombatStat(DamageType.StatType type)
    {
        //function for a stat used in damage math. Same as TypeToStatFinder, but never below the stat floor (1),
        //so negative runes can't make damage 0 or flip it negative. Stat type None (like explosions) has no stat and stays 0
        if (type == DamageType.StatType.None) return 0;
        return Mathf.Max(StatFloor, TypeToStatFinder(type));
    }

    public DamageType.StatType GetDefensiveStatType(DamageType.StatType type)
    {
        //function that picks which of this unit's stats defends against an attack made with a given stat
        if (type == DamageType.StatType.Strength || type == DamageType.StatType.Agility || type == DamageType.StatType.Defense)
            return PhysicalDefMod;
        if (type == DamageType.StatType.Intellect || type == DamageType.StatType.Spirit)
            return MagicalDefMod;
        return DamageType.StatType.None;
    }

    public DamageType.StatType GetAttackStatType(bool isRanged, DamageType.AttackType type)
    {
        //function that picks which of this unit's stats powers an attack
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
        //function for how much of a hit gets through this unit's immunities and resistances. 0 = immune, 0.5 = resisted, 1 = full
        //Normal element and attack type None can still be listed by hand, but nothing has them by default
        if (AttackTypeImmunity != null && AttackTypeImmunity.Contains(atk)) return 0f;
        if (ElementTypeImmunity != null && ElementTypeImmunity.Contains(ele)) return 0f;

        if (AttackTypeResistance != null && AttackTypeResistance.Contains(atk)) return ResistanceMultiplier;
        if (ElementTypeResistance != null && ElementTypeResistance.Contains(ele)) return ResistanceMultiplier;

        return 1f;
    }
}

[Serializable]
public class PartyStats : CoreStats
{
    [Header("Party Stats")]
    [Tooltip("How much current xp the unit has. Needed level is calculated elsewhere")]
    public int _XP;
    public DangerLevel _Danger;
    public DangerType _CurrentDangerType;

    public void SetCurrentDangerType()
    {
        _CurrentDangerType = _Danger.GetCurrentDangerType(_Health, _HP);
    }
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
    [SerializeField]public List<WeaponItem> _EquippedWeapons;
    public Dictionary<ItemSO, int> _BackpackInventory;

    public bool CheckInventoryFits(ItemSO item, int amount)
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

    public bool CheckEquippedWeaponFits(WeaponItem x)
    {
        for (int i = 0; i < _EquippedWeapons.Count; i++)
            if (_EquippedWeapons[i] == null) return true;
        return false;
    }

    public void AddBackpackInventory (ItemSO x, int y)
    {
        Debug.LogWarning($"Adding inventory: {x.name} x {y}");
        _BackpackInventory.Add(x, y);
    }

    public void AddEquipmentInventory(WeaponItem x)
    {
        Debug.LogWarning($"Adding equipped inventory {x.name}");
        for (int i = 0; i < _EquippedWeapons.Count; i++)
            if (_EquippedWeapons[i] == null)
            {
                _EquippedWeapons[i] = x;
                return;
            }
    }
}
