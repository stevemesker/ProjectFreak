using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

[Serializable]
public class FreakData 
{
    public int _Id;
    public string _Name;
    public int _level;
    public int _currentHealth;
    public BaseStats _Stats;
    public UnitBaseClass.Player.UnitBaseClass _FreakClass;
    //public FreakInventory _Inventory;
}

[Serializable]
public class BaseStats
{
    public int _MaxHP;
    public int _Strength;
    public int _Agility;
    public int _Inteligence;

    [FoldoutGroup("Elemental Effectiveness")] public List<ElementType.Element> _ElementResistance;
    [FoldoutGroup("Elemental Effectiveness")] public List<ElementType.Element> _ElementWeakness;
    [FoldoutGroup("Elemental Effectiveness")] public float _ElementResitanceMultiplier = 1;
    [FoldoutGroup("Elemental Effectiveness")] public float _ElementWeaknessMultiplier = 1;

    [FoldoutGroup("Damage Type Effectiveness")] public List<DamageType.Type> _TypeResistance;
    [FoldoutGroup("Damage Type Effectiveness")] public List<DamageType.Type> _TypeWeakness;
    [FoldoutGroup("Damage Type Effectiveness")] public float _TypeResitanceMultiplier = 1;
    [FoldoutGroup("Damage Type Effectiveness")] public float _TypeWeaknessMultiplier = 1;
    
}

[Serializable]
public class FreakInventory
{
    [Tooltip("Number of equipment the freak can switch between")] public int _FreakEquipmentSize;
    [Tooltip("Inventory size of the specific freak")] public int _FreakInventorySize;
    public List<WeaponItem> _FreakEquippedWeapons;
    public Dictionary<ItemSO, int> _FreakBackpackInventory;
}
