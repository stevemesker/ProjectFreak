using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapProjectileSpawner : MonoBehaviour
{
    [Header("Damage Data")]
    [Tooltip("Seconds between shots")]
    public float _fireSpeed;
    [Tooltip("How fast the projectiles fly, in meters per second")]
    public float _projectileSpeed = 1;
    [Tooltip("The trap's stand-in attack stat, since a preset trap has no stats of its own. The target's defense is compared against it (see Damage Balance in the GDD)")]
    [Min(1f)] public float _FauxAttackStat = 10f;
    [Tooltip("Chance for each projectile to crit, 0 to 1. Staggered targets always take a crit")]
    [Range(0f, 1f)] public float _CritChance = 0f;
    [Tooltip("Damage multiplier on a crit. Only the first (main) damage entry gets multiplied")]
    [Min(1f)] public float _CritMultiplier = 1.5f;
    [Tooltip("How hard a hit shakes the camera")]
    public float _ImpactStrength = .25f;
    [Tooltip("The damage each projectile carries. Damage here is the raw amount before the target's defense. The trap's faux attack stat is filled in automatically")]
    public List<DamageEntry> _DamageStats;

    [Header("Settings")]
    [Tooltip("The projectile it fires. Needs a ProjectileObject on it")]
    public GameObject _projectilePrefab;
    [Tooltip("Where projectiles spawn and which way they face")]
    [SerializeField] GameObject _projectileSpawnLocator;

    [Header("Private Variables")]
    Coroutine _fireCountdown;
    [Tooltip("The fastest the trap is allowed to fire, in seconds between shots")]
    [SerializeField] float _minimumFireSpeed = .25f;

    private void OnEnable()
    {
        //starts the firing loop once the trap is turned on
        if (_projectilePrefab == null) { Debug.LogError($"Error! Projectile prefab not assigned on {gameObject.name}", this); return; }
        if (_projectileSpawnLocator == null) { Debug.LogError($"Error! Projectile spawn locator not assigned on {gameObject.name}", this); return; }
        if (_fireSpeed < _minimumFireSpeed) _fireSpeed = _minimumFireSpeed;
        _fireCountdown = StartCoroutine(FireCountdown());
    }
    private void OnDisable()
    {
        //stops the firing loop so it doesn't keep running while disabled
        if (_fireCountdown != null) StopCoroutine(_fireCountdown);
        _fireCountdown = null;
    }

    IEnumerator FireCountdown()
    {
        //function that fires the trap on a loop
        while (true)
        {
            yield return new WaitForSeconds(_fireSpeed);
            GameObject temp = Instantiate(_projectilePrefab, _projectileSpawnLocator.transform.position, _projectileSpawnLocator.transform.rotation);
            ProjectileObject projectile = temp.GetComponent<ProjectileObject>();
            if (projectile == null) { Debug.LogError($"Error! {_projectilePrefab.name} has no ProjectileObject, trap on {gameObject.name} can't fire it", this); Destroy(temp); yield break; }
            projectile._Speed = _projectileSpeed;
            projectile._Damage = FillProjectileDamage();
        }
    }

    DamagePackage FillProjectileDamage ()
    {
        //function that builds a fresh damage package for one projectile. The entries are copied (not shared with the inspector list)
        //so changing the list later never changes projectiles already flying
        DamagePackage temp = new DamagePackage();
        temp._Source = gameObject;
        temp._CritChance = _CritChance;
        temp._CritMultiplier = _CritMultiplier;
        temp._DamageImpactStrength = _ImpactStrength;
        temp._HitsAllies = true; //preset traps have no team, so they hit everyone
        temp._Entries = new List<DamageEntry>();

        if (_DamageStats == null) return temp;
        for (int i = 0; i < _DamageStats.Count; i++)
        {
            DamageEntry entry = _DamageStats[i]; //DamageEntry is a struct, so this is already a copy
            entry._AttackStat = _FauxAttackStat;
            temp._Entries.Add(entry);
        }
        return temp;
    }
}
