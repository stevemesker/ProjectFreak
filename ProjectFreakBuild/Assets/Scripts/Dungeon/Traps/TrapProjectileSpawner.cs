using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrapProjectileSpawner : MonoBehaviour
{
    [Header("Damage Data")]
    public float _fireSpeed;
    public float _projectileSpeed = 1;
    public float _CritMultiplier = 1;
    public float _ImpactStrength = .25f;
    public List<DamageEntry> _DamageStats;

    [Header("Settings")]
    
    public GameObject _projectilePrefab;
    [SerializeField] GameObject _projectileSpawnLocator;

    [Header("Private Variables")]
    Coroutine _fireCountdown;
    [SerializeField] float _minimumFireSpeed = .25f;

    private void OnEnable()
    {
        if (_fireSpeed < _minimumFireSpeed) _fireSpeed = _minimumFireSpeed;
        _fireCountdown = StartCoroutine(fireCountdown());
    }
    private void OnDisable()
    {
        StopCoroutine(_fireCountdown);
        _fireCountdown = null;
    }

    IEnumerator fireCountdown()
    {
        yield return new WaitForSeconds(_fireSpeed);
        GameObject temp = Instantiate(_projectilePrefab, _projectileSpawnLocator.transform.position, _projectileSpawnLocator.transform.rotation);
        temp.GetComponent<ProjectileObject>()._Speed = _projectileSpeed;
        temp.GetComponent<ProjectileObject>()._Damage = FillProjectileDamage();
        _fireCountdown = StartCoroutine(fireCountdown());
    }

    DamagePackage FillProjectileDamage ()
    {
        DamagePackage temp = new DamagePackage();
        temp._Source = gameObject;
        temp._CritMultiplier = 1;
        temp._DamageImpactStrength = _ImpactStrength;
        temp._Entries = _DamageStats;
        return temp;
    }
}
