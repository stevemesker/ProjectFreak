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
    [Tooltip("Gameobject using the weapon. Used so that spawned bullets do not run into the unit that shot it")]public GameObject _Wielder;
    [SerializeField] public WeaponRangedItem _WeaponObject;
    //public WeaponItem weaponData;
    //[SerializeField, Tooltip("Amount of current charge for the weapon")] private float chargeAmount;
    [SerializeField] private bool canFire = true; //used for global pausing
    [SerializeField] private bool isReleased = true; //used for non-automatic attack gating
    private int projectileIndex = 0;

    private Coroutine currentTimer;

    private int currentPower;
    List<ElementType.Element> currentElement;

    private int projectileIndexer; //used tyo scycke through projectiles in case there are multiple types

    public void SetUpWeapon(ItemSO item, GameObject Wielder)
    {
        _Wielder = Wielder;
        _WeaponObject = item as WeaponRangedItem;
    }

    #region WeaponUse
    public void updateStats(int power, List<ElementType.Element> element)
    {

    }
    public void TriggerAttack(int power, List<Element> element)
    {
        //input that comes from the freak character. Does not fire the round but tells the gun to continue firing if it can
        updateStats(power, element);
        if (canFire == false) return;
        isReleased = false;
        FireGun();
    }

    public void ReleaseAttack()
    {
        isReleased = true;
    }

    void FireGun()
    {
        if (isReleased || currentTimer != null) return;
        print("Firing Gun");

        if (_WeaponObject.shotNumber > 1) multishot();
        else SingleShot();
        //cooldown
        if (_WeaponObject.isAutomatic)currentTimer = StartCoroutine(CoolDownTimer(_WeaponObject.weaponFireRate));
    }

    void SingleShot()
    {
        GameObject spawnedProjectile;
        if (projectileIndex >= _WeaponObject.ProjectilePrefab.Count) projectileIndex = 0;

        spawnedProjectile = spawnProjectile(projectileIndex, transform.position, transform.rotation);
        FillProjectileStats(spawnedProjectile.GetComponent<ProjectileObject>());

        projectileIndex ++;
        
    }

    void multishot()
    {
        GameObject spawnedProjectile;
        //int projectileIndex = 0;
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
    #endregion

    #region spawning damage dealers

    GameObject spawnProjectile(int projectileIndex, Vector3 position, Quaternion rotation)
    {
        GameObject spawnedProjectile;
        spawnedProjectile = Instantiate(_WeaponObject.ProjectilePrefab[projectileIndex], position, rotation);
        return spawnedProjectile;
    }

    void FillProjectileStats(ProjectileObject projectile)
    {
        projectile.instigator = _Wielder;
        projectile.speed = _WeaponObject.projectileSpeed;
        
    }


    void fireHitScan()
    {
        //function used to fire hitscan attacks
    }

    IEnumerator CoolDownTimer(float time)
    {
        yield return new WaitForSeconds(time);
        currentTimer = null;
        if (_WeaponObject.isAutomatic && isReleased == false) FireGun();
    }
    #endregion

    #region Spawning Location Tools

    public Vector3 SpawnOrigin(int index, int maxCount, float distance)
    {
        //max count:    number of total attacks being spawned
        //index:        which bullet are we dealing with of the maxcount being spawned
        //distance:     How far from the origin the spacing will go
        if (maxCount <= 1) return transform.position;
        float xPosition = ((distance / (maxCount-1))*index - (distance/2));
        return transform.position + (transform.right*xPosition);
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
