using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CombatTools
{
    //Small helpers every weapon (ranged and melee) shares, so the rules for allies, walls and damage only live in one place.
    //"static" means you call them straight from the class (CombatTools.IsAlly(...)) without putting this on an object. Same idea as NavMeshTools

    #region Hit Rules
    public static bool IsPartOf(Collider hitCollider, GameObject owner)
    {
        //function for checking if a collider belongs to an object or any of its children (IsChildOf also counts the object itself)
        if (hitCollider == null || owner == null) return false;
        return hitCollider.transform.IsChildOf(owner.transform);
    }

    public static bool IsAlly(UnitTeam myTeam, UnitTeam other)
    {
        //function for checking if another unit is on my side. No team = no allies, so everything can be hit
        if (myTeam == null || other == null) return false;
        if (other == myTeam) return true;
        return myTeam.IsHostileTo(other) == false;
    }

    public static bool IsLevelGeometry(Collider hitCollider)
    {
        //function for checking if something stops attacks: anything that isn't a unit and can't be damaged (walls, floors, props)
        if (hitCollider.GetComponentInParent<UnitTeam>() != null) return false;
        if (hitCollider.GetComponentInParent<IDamagable>() != null) return false;
        return true;
    }

    public static Collider GetBodyCollider(Component unit, List<Collider> buffer)
    {
        //function that finds a unit's solid collider, skipping trigger colliders like pickup or detection ranges
        //fills the buffer list you pass in instead of making a new list every time (less garbage)
        if (unit == null) return null;
        unit.GetComponentsInChildren(false, buffer);
        for (int i = 0; i < buffer.Count; i++)
        {
            if (buffer[i].isTrigger == false) return buffer[i];
        }
        return null;
    }
    #endregion

    #region Damage
    public static DamagePackage BuildWeaponDamagePackage(GameObject source, CoreStats stats, WeaponItem weapon, bool isRanged, float damageMultiplier, float knockbackMultiplier)
    {
        //function that builds a weapon's damage snapshot using the wielder's stats right now
        //damage = (base damage + attack stat) × multipliers, following the formula in the Damage note
        DamagePackage package = new DamagePackage();
        package._Source = source;
        package._CritMultiplier = 1f; //todo: crits aren't built yet (damage overhaul)
        package._KnockbackDistance = weapon._Knockback * knockbackMultiplier;
        package._Entries = new List<DamageEntry>();

        DamageType.StatType attackStat = DamageType.StatType.None;
        int attackStatValue = 0;
        if (stats != null)
        {
            attackStat = stats.GetAttackStatType(isRanged, weapon._AttackType); //STR for physical melee, AGI for physical ranged, INT for magic
            attackStatValue = stats.TypeToStatFinder(attackStat);
        }

        DamageEntry entry = new DamageEntry();
        entry._Damage = Mathf.RoundToInt((weapon._BaseDamage + attackStatValue) * damageMultiplier);
        entry._atkType = weapon._AttackType;
        entry._statType = attackStat;
        entry._elementType = weapon._Element;
        package._Entries.Add(entry);

        return package;
    }
    #endregion

    #region Effects
    public static void ShakeCameraForWielder(GameObject wielder, float force)
    {
        //function for a weapon's activation shake. Only shakes when the camera is following the wielder, so a shade attacking off screen doesn't shake it
        if (force <= 0f) return;
        if (CameraManager._CamManager == null) return;
        if (CameraManager._CamManager._currentFollowTarget != wielder) return;

        CameraManager._CamManager.CameraShake(force, null);
    }
    #endregion
}
