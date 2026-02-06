using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewWeapon", menuName = "ScriptableObjects/Weapons/Weapon", order = 0)]

public class WeaponItem : ItemSO
{
    [TitleGroup("---Weapon Data---")]
    public WeaponTypeBaseClass.WeaponType weaponType;
    public GameObject weaponPrefab;

    [TitleGroup("---Damage Data---")]
    public ElementType.Element element;
    public int baseDamage;
    public float fireKnockbackAmount;
    public float fireRate;

    [TitleGroup("---Projectile Data---")]
    public List<GameObject> ProjectilePrefab;
    public int projectileCount;
    public float KnockbackDamage;

}
