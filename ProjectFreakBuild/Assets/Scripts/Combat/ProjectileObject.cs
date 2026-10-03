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
        Destroy(gameObject, Mathf.Max(0.1f, lifeTime)); //Destroy with a second number waits that many seconds first
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (_Damage == null) { Destroy(gameObject); return; } //never got a package, nothing to deliver
        if (other.gameObject == _Damage._Source) return;

        IDamagable damagable = other.GetComponent<IDamagable>();
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
