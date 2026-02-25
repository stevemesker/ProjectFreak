using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewWeapon", menuName = "ScriptableObjects/Weapons/Weapon", order = 0)]

public class WeaponItem : ItemSO
{
    [TitleGroup("---Weapon Data---")]
    [Tooltip("Class of weapon: Melee, Ranged, Summon")]
    public WeaponTypeBaseClass.WeaponType weaponType;
    [Tooltip("The art associated with the data which includes the model and its muzzle location")] 
    public GameObject weaponPrefab;
    [Tooltip("how quickly the weapon can fire another projectile right after the previous")] 
    public float weaponFireRate;
    [Tooltip("time it takes to fire a projectile once the fire button is pressed that slows the player down by half (not to be confused with fire rate)")] 
    public float weaponWarmUpTime;
    [Tooltip("how strong the kickback of the gun is to the player")] 
    public float weaponKnockback;

    [TitleGroup("---Damage Data---")]
    [Tooltip("Raw damage per projectile")] 
    public int baseDamage;
    [Tooltip("elemental damage type hitting the opponent (see element types for list)")] 
    public ElementType.Element element;
    [Tooltip("knockback strength hitting the enemy")] 
    public float knockbackHitAmount;

    
}
