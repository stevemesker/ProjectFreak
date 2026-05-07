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

    [Tooltip("Will fire multiple shots between firing rounds. Each firing round only ends when burst completes. Must be at least 1")]
    public int shotsPerBurst = 1;

    [ShowIf(nameof(IsBurst))]
    [Tooltip("How fast the burst will wait to fire the next")]
    public float BurstFireRate;

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

    [Tooltip("number of projectiles are fired simultaneously in a firing round")]
    public int shotNumber = 1;

    [FoldoutGroup("Fire Spread Data")]
    [ShowIf(nameof(IsSpread))]
    [Tooltip("When fired, projectiles/hitscan will point in a arc within this range in front of the player")]
    public float fireSpread;

    [FoldoutGroup("Fire Spread Data")]
    [ShowIf(nameof(IsSpread))]
    [Tooltip("When fired, origin of the shot will be within a range from the weapon/player origin")]
    public float originSpread;

    [FoldoutGroup("Fire Spread Data")]
    [ShowIf(nameof(IsSpread))]
    [Tooltip("If true, shots will randomly point within the spread ranges")]
    public bool isRandomSpread;

    private bool IsBurst() => shotsPerBurst > 1;
    
    private bool IsSpread() => shotNumber > 1;


    ////////////////////////////////////

    //PROJECTILE VARIABLES

    [FoldoutGroup("---Projectile Data---")]
    [HideIf(nameof(isHitScan))]
    [Tooltip("projectiles that are spawned. Can be multiple projectile types which will spawn in different patterns based on the Projectile Pattern selected")]
    public List<GameObject> ProjectilePrefab;

    

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
