using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewWeapon", menuName = "ScriptableObjects/Weapons/Weapon", order = 0)]

public class WeaponItem : ItemSO
{
    [TitleGroup("---Weapon Data---")]
    [Tooltip("Class of weapon: Melee, Ranged, Summon")]
    public DamageType.AttackType weaponAttackType;

    [Tooltip("The art associated with the data which includes the model and its muzzle location")] 
    public GameObject weaponPrefab;

    [Tooltip("how quickly the weapon can attack right after the previous")] 
    public float weaponFireRate;

    [Tooltip("time it takes to fire a projectile once the fire button is pressed that slows the player down by half (not to be confused with fire rate)")] 
    public float weaponWarmUpTime;

    [Tooltip("how strong the kickback of the gun is to the player")] 
    public float weaponKnockback;

    [Tooltip("If ticked, weapon will not quit firing after first shot and will instead wait until fire rate timer ends")]
    public bool isAutomatic;

    [Tooltip("Requires a held buildup time before attacking begins")]
    public bool isChargedShot;
    
    [ShowIf(nameof(isChargedShot))]
    [FoldoutGroup("Charge Settings")]
    [Tooltip("How many seconds it takes to hit max charge")]
    public float chargeMaxAmount;

    [ShowIf(nameof(isChargedShot))]
    [FoldoutGroup("Charge Settings")]
    [Tooltip("If projectile grows in scale based on charging. 0 means no scaling")]
    public float chargeScaling;

    [ShowIf(nameof(isChargedShot))]
    [FoldoutGroup("Charge Settings")]
    [Tooltip("probably should get rid of this")]
    public bool ChargeAutoAttack;

    [TitleGroup("---Damage Data---")]
    [Tooltip("Raw damage per projectile")] 
    public int baseDamage;
    [Tooltip("elemental damage type hitting the opponent (see element types for list)")] 
    public ElementType.Element element;
    [Tooltip("knockback strength hitting the enemy")] 
    public float knockbackHitAmount;

}
