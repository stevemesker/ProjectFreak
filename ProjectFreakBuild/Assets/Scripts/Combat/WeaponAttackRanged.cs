using ElementType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
/// 
/// Script interperets the data from a ranged weapon item and handles firing appropriate projectiles
/// 
/// ////////////////////////////////////////////////
public class WeaponAttackRanged : MonoBehaviour, ITriggerable
{
    [Tooltip("Gameobject using the weapon. Used so that spawned bullets do not run into the unit that shot it")]
    public GameObject _Wielder;
    [SerializeField] 
    public WeaponRangedItem _WeaponObject;
    [SerializeField]
    //private DamageType.StatType _WeaponAttackStat;
    public int _AttackBonus;

    DamagePackage dmgPackage;
    public DamageType.ElementType elementType;
    
    [SerializeField] private bool canFire = true; //used for global pausing
    [SerializeField] private bool isReleased = true; //used for non-automatic attack gating
    
    [SerializeField]
    bool isCharging;

    private int projectileIndex = 0;

    private Coroutine currentTimer;

    private int currentPower;
    List<ElementType.Element> currentElement;

    private int projectileIndexer; //used tyo scycke through projectiles in case there are multiple types

    //
    //Private/Unserialized Variables
    private ITriggerable weaponTrigger;
    private Coroutine chargeTime;
    private Coroutine cycleTimer;
    private float chargeTimeInitiated;

    #region Initialization
    public void SetUpWeapon(ItemSO item, GameObject Wielder, CoreStats stats)
    {
        _Wielder = Wielder;
        _WeaponObject = item as WeaponRangedItem;
        
        dmgPackage = new DamagePackage();

        dmgPackage._Source = Wielder;
        dmgPackage._CritMultiplier = 1; //figure this out later, it'll probably come from the weapon data? but maybe not it might be a stat thing I dunno man I just work here
        int dmg = stats.TypeToStatFinder(stats.GetAttackStatType(isRange(), _WeaponObject.weaponAttackType));

        addDamageEntryToPackage(CreateDamageEntry(dmg, _WeaponObject.weaponAttackType, stats.GetAttackStatType(isRange(), _WeaponObject.weaponAttackType), elementType));
    }
    
    public void addDamageEntryToPackage(DamageEntry entry)
    {
        dmgPackage._Entries.Add(entry);
    }

    public DamageEntry CreateDamageEntry(int dmg, DamageType.AttackType atk, DamageType.StatType stat, DamageType.ElementType element)
    {
        DamageEntry entry = new DamageEntry();
        entry._Damage = dmg;
        entry._atkType = atk;
        entry._statType = stat;
        entry._elementType = element;
        return entry;
    }

    void FillProjectileStats(ProjectileObject projectile)
    {
        projectile.instigator = _Wielder;
        projectile.speed = _WeaponObject.projectileSpeed;
        projectile._Damage = dmgPackage;
    }

    public bool isRange()
    {
        return true;
    }
    #endregion

    #region Weapon Activation Trigger
    public void TriggerAttack()
    {
        if (_WeaponObject.isChargedShot == true)
        {
            print("Charging has begun...");

            chargeTimeInitiated = Time.time;
            chargeTime = StartCoroutine(ChargeTimer(_WeaponObject.chargeMaxAmount));
            return;
        }
        fireWeapon(1);
    }

    public void ReleaseAttack()
    {
        if (isCharging)
        {
            float timeRemaining = Time.time - chargeTimeInitiated;
            if (timeRemaining > _WeaponObject.chargeMaxAmount) timeRemaining = _WeaponObject.chargeMaxAmount;
            StopCoroutine(chargeTime);
            chargeTime = null;
            isCharging = false;
            //return;
        }
        if (cycleTimer != null)
        {
            StopCoroutine(cycleTimer);
            cycleTimer = null;
        }
    }
    #endregion

    #region Weapon Firing
    void fireWeapon(float multiplier)
    {
        print("Bang! X " + multiplier);

        if (_WeaponObject.shotNumber > 1) multishot();
        else SingleShot();

        if (_WeaponObject.isAutomatic)
        {
            cycleTimer = StartCoroutine(CycleTimer(_WeaponObject.weaponFireRate, multiplier));
        }
    }

    void SingleShot()
    {
        GameObject spawnedProjectile;
        if (projectileIndex >= _WeaponObject.ProjectilePrefab.Count) projectileIndex = 0;

        spawnedProjectile = spawnProjectile(projectileIndex, transform.position, transform.rotation);
        FillProjectileStats(spawnedProjectile.GetComponent<ProjectileObject>());

        projectileIndex++;

    }

    void multishot()
    {
        GameObject spawnedProjectile;
        for (int i = 0; i < _WeaponObject.shotNumber; i++)
        {
            print("bang!");
            if (projectileIndex >= _WeaponObject.ProjectilePrefab.Count) projectileIndex = 0;

            if (_WeaponObject.isRandomSpread) spawnedProjectile = spawnProjectile(projectileIndex, SpawnOrigin(i, _WeaponObject.shotNumber, _WeaponObject.originSpread), SpawnRandomRotation(_WeaponObject.fireSpread));
            else spawnedProjectile = spawnProjectile(projectileIndex, SpawnOrigin(i, _WeaponObject.shotNumber, _WeaponObject.originSpread), SpawnRotation(i, _WeaponObject.shotNumber, _WeaponObject.fireSpread));

            FillProjectileStats(spawnedProjectile.GetComponent<ProjectileObject>());

            projectileIndex++;
        }
    }

    GameObject spawnProjectile(int projectileIndex, Vector3 position, Quaternion rotation)
    {
        GameObject spawnedProjectile;
        spawnedProjectile = Instantiate(_WeaponObject.ProjectilePrefab[projectileIndex], position, rotation);
        return spawnedProjectile;
    }
    #endregion

    #region Timers
    IEnumerator ChargeTimer(float amount)
    {
        isCharging = true;
        yield return new WaitForSeconds(amount);
        if (_WeaponObject.isAutomatic)
        {
            fireWeapon(amount);
        }
    }
    IEnumerator CycleTimer(float cycleTime, float bonus)
    {
        yield return new WaitForSeconds(cycleTime);
        fireWeapon(bonus);
    }
    #endregion

    #region Spawning Location Tools

    public Vector3 SpawnOrigin(int index, int maxCount, float distance)
    {
        //max count:    number of total attacks being spawned
        //index:        which bullet are we dealing with of the maxcount being spawned
        //distance:     How far from the origin the spacing will go
        if (maxCount <= 1) return transform.position;
        float xPosition = ((distance / (maxCount - 1)) * index - (distance / 2));
        return transform.position + (transform.right * xPosition);
    }
    public Quaternion SpawnRotation(int index, int maxCount, float spreadAngle)
    {
        //if (maxCount <= 1) return transform.rotation;
        float angleOffset = ((spreadAngle / (maxCount - 1)) * index) - (spreadAngle / 2f);
        return Quaternion.AngleAxis(angleOffset, transform.up) * transform.rotation;
    }

    public Quaternion SpawnRandomRotation(float spreadAngle)
    {
        float angleOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
        return Quaternion.AngleAxis(angleOffset, transform.up) * transform.rotation;
        //return Quaternion.identity;
    }
    #endregion

}
