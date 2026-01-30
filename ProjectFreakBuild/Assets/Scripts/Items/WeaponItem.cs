using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewWeapon", menuName = "ScriptableObjects/Weapons/Weapon", order = 0)]

public class WeaponItem : ItemSO
{
    [TitleGroup("---Weapon Data---")]
    public List<GameObject> ProjectilePrefab;
    public int baseDamage;
}
