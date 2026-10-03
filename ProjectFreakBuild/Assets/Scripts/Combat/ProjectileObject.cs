using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    //a simple projectile: flies forward and delivers its damage package to the first thing it hits

    [Header("Settings")]
    [Tooltip("Seconds before the projectile removes itself if whatever spawned it didn't give it a lifetime (like traps)")]
    [SerializeField, Min(0.1f)] float _DefaultLifeTime = 10f;

    [Header("Runtime Data")]
    [Tooltip("How fast it flies along its forward, in meters per second. Set by whatever launched it")]
    public float _Speed;

    [Tooltip("The damage package it carries")]
    public DamagePackage _Damage;

    [Tooltip("Extra launch info from the weapon (charge %, finisher). Not used by this simple projectile yet")]
    public LaunchPackage _Launch;

    //local variables
    bool _isLaunched; //true once LaunchProjectile has set the lifetime
    UnitTeam _sourceTeam; //the shooter's side, so the projectile flies through its allies. Null = hits every team (like traps)

    private void Start()
    {
        //anything spawned without LaunchProjectile (like trap projectiles) still cleans itself up eventually
        if (_isLaunched == false) Destroy(gameObject, _DefaultLifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * _Speed * Time.deltaTime;
    }

    public void LaunchProjectile(DamagePackage damage, LaunchPackage launch, float speed, float lifeTime)
    {
        //function weapons use to send the projectile off with everything it needs
        _Damage = damage;
        _Launch = launch;
        _Speed = speed;
        _isLaunched = true;

        //remember the shooter's team now, so we don't look it up on every hit
        _sourceTeam = null;
        if (damage != null && damage._Source != null) _sourceTeam = damage._Source.GetComponent<UnitTeam>();
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime)); //Destroy with a second number waits that many seconds first
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (_Damage == null) { Destroy(gameObject); return; } //never got a package, nothing to deliver
        if (_Damage._Source != null && other.transform.IsChildOf(_Damage._Source.transform)) return; //the shooter, or any part of it (IsChildOf also counts the object itself)

        //allies of the shooter: fly straight through them (no friendly fire)
        if (_sourceTeam != null)
        {
            UnitTeam hitTeam = other.GetComponentInParent<UnitTeam>();
            if (hitTeam != null && _sourceTeam.IsHostileTo(hitTeam) == false) return;
        }

        IDamagable damagable = other.GetComponentInParent<IDamagable>(); //InParent so hitting a child collider (like a big enemy's arm) still finds its damage script
        if (damagable == null)
        {
            Destroy(gameObject);
            return;
        }

        //spawn hit effects here
        damagable.TakeDamage(_Damage);
        Destroy(gameObject);
    }
}
