using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewRangedWeapon", menuName = "ScriptableObjects/Weapons/RangedWeapon", order = 0)]
public class WeaponRangedItem : WeaponItem
{
    //SHARED VARIABLES

    [TitleGroup("---Projectile Fire Data---")]
    [Tooltip("")]
    public bool isHitScan;

    [Tooltip("Requires a held buildup time before attacking begins")]
    public bool isChargedShot;

    [ShowIf(nameof(isChargedShot))]
    [Tooltip("If projectile grows in scale based on charging. 0 means no scaling")]
    public float chargeScaling;

    [Tooltip("If ticked, weapon will not quit firing after first shot and will instead wait until fire rate timer ends")]
    public bool isAutomatic;

    [Tooltip("Will fire multiple shots between firing rounds. Each firing round only ends when burst completes. Must be at least 1")]
    public int shotsPerBurst = 1;

    [Tooltip("If true, automatically targets closest enemies without requiring the player to face their direction")]
    public bool isLockOn;

    [FoldoutGroup("Lockon Data")]
    [ShowIf(nameof(isLockOn))]
    [Tooltip("Distance weapon will lock on to targets from player origin")]
    public float lockOnDistance;

    [FoldoutGroup("Lockon Data")]
    [ShowIf(nameof(isLockOn))]
    [Tooltip("Maximum number of targets the weapon will lock on to")]
    public int lockonTargetCount;

    [ShowIf(nameof(IsBurst))]
    [Tooltip("How fast the burst will wait to fire the next")]
    public float BurstFireRate;

    [FoldoutGroup("Fire Spread Data")]
    [Tooltip("When fired, projectiles/hitscan will point in a arc within this range in front of the player")]
    public float fireSpread;

    [FoldoutGroup("Fire Spread Data")]
    [Tooltip("When fired, origin of the shot will be within a range from the weapon/player origin")]
    public float originSpread;

    [FoldoutGroup("Fire Spread Data")]
    [ShowIf(nameof(IsSpread))]
    [Tooltip("If true, shots will randomly point within the spread ranges")]
    public bool isRandomSpread;

    private bool IsBurst() => shotsPerBurst > 1;
    private bool IsSpread() => fireSpread > 0 || originSpread > 0;
    ////////////////////////////////////

    //PROJECTILE VARIABLES

    [FoldoutGroup("---Projectile Data---")]
    [HideIf(nameof(isHitScan))]
    [Tooltip("projectiles that are spawned. Can be multiple projectile types which will spawn in different patterns based on the Projectile Pattern selected")]
    public List<GameObject> ProjectilePrefab;

    [FoldoutGroup("---Projectile Data---")]
    [HideIf(nameof(isHitScan))]
    [Tooltip("How projectiles are spawned when fired (single, line, cone, ring, charge)")]
    public ProjectileDataTypes.ProjectilePattern projectilePattern;

    //[FoldoutGroup("---Projectile Data---")]
    //[HideIf(nameof(isHitScan))]
    //[Tooltip("how many projectiles are spawned at once when firing")]
    //public int projectileCount;

    [FoldoutGroup("---Projectile Data---")]
    [HideIf(nameof(isHitScan))]
    [Tooltip("how fast a projectile moves across the battlefield (ignored if hitscan is selected)")]
    public int projectileSpeed;

    [FoldoutGroup("---Projectile Data---")]
    [HideIf(nameof(isHitScan))]
    [Tooltip("how long a projectile will last before it automatically completes itself. Use 0 if it stays infinitely")]
    public int projectileLifeTime;
    ////////////////////////////////////

    //HITSCAN VARIABLES

    [FoldoutGroup("---Hitscan Projectile Data---")]
    [ShowIf(nameof(isHitScan))]
    [Tooltip("Max Distance the raycast will fire fot hitscna. 0 means infinite")]
    public float fireDistance;
    ////////////////////////////////////
}
