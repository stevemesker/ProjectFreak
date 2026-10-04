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

    public static bool CanHitTeam(UnitTeam myTeam, UnitTeam other, bool hitsAllies)
    {
        //function for the friendly fire rule: allies are skipped unless the attack is flagged to hit them (DamagePackage._HitsAllies)
        if (hitsAllies) return true;
        return IsAlly(myTeam, other) == false;
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
    //tuning for the damage formula. todo: move these to an inspector asset if they need tuning often (see Damage Balance in the GDD)
    const float DefenseWeight = 2f; //defense counts double: damage that gets through = ATK ÷ (ATK + 2 × DEF)
    const int MinimumAttackStat = 1; //the stat floor, so a hit with a missing attack stat still does something
    const float RoundingTolerance = 0.0001f;

    public static DamagePackage BuildWeaponDamagePackage(GameObject source, CoreStats stats, WeaponItem weapon, bool isRanged, float damageMultiplier, float knockbackMultiplier)
    {
        //function that builds a weapon's damage snapshot using the wielder's stats right now
        //raw damage = attack stat × weapon power × multipliers (combo step, charge). The target applies its defense later (see ResolveHit)
        DamagePackage package = new DamagePackage();
        package._Source = source;
        package._CritChance = weapon._CritChance;
        package._CritMultiplier = weapon._CritMultiplier;
        package._HitsAllies = weapon._HitsAllies;
        package._KnockbackDistance = weapon._Knockback * knockbackMultiplier;
        package._Entries = new List<DamageEntry>();

        DamageType.StatType attackStat = DamageType.StatType.None;
        int attackStatValue = MinimumAttackStat;
        if (stats != null)
        {
            attackStat = stats.GetAttackStatType(isRanged, weapon._AttackType); //STR for physical melee, AGI for physical ranged, INT for magic
            attackStatValue = Mathf.Max(MinimumAttackStat, stats.GetCombatStat(attackStat));
        }

        DamageEntry entry = new DamageEntry();
        entry._AttackStat = attackStatValue; //snapshotted now, the target needs it for the defense math even if the wielder changes or dies
        entry._Damage = attackStatValue * weapon._Power * damageMultiplier; //stays a decimal, the target rounds once at the end
        entry._atkType = weapon._AttackType;
        entry._statType = attackStat;
        entry._elementType = weapon._Element;
        package._Entries.Add(entry); //the first entry is always the main one (the only one that can crit)

        return package;
    }

    public static HitResult ResolveHit(DamagePackage package, CoreStats defender, bool isStaggered)
    {
        //THE damage calculation. Every unit with health calls this when a hit lands, so the rules only live here.
        //For each entry: raw × crit (main entry only) × effectiveness × resistance × ATK ÷ (ATK + 2 × DEF), rounded up at the end
        HitResult result = new HitResult();
        if (package == null || package._Entries == null || defender == null) return result;

        //crit: staggered targets always take one, otherwise roll the attack's crit chance. Rolled here, on the target, so every projectile rolls on its own
        result._IsCrit = isStaggered || Random.value < package._CritChance; //Random.value is a random number from 0 to 1
        float critMultiplier = Mathf.Max(1f, package._CritMultiplier); //never below 1, so a package left at 0 can't wipe out the hit

        for (int i = 0; i < package._Entries.Count; i++)
        {
            DamageEntry entry = package._Entries[i];
            float damage = Mathf.Max(0f, entry._Damage);

            if (i == 0 && result._IsCrit) damage *= critMultiplier; //only the main entry crits
            damage *= GetEffectiveness(entry, defender);
            damage *= defender.GetAttackResistanceModifier(entry._atkType, entry._elementType); //0 when immune, 0.5 when resisted

            //true damage skips defense, but resistances and immunities above still apply
            if (entry._atkType != DamageType.AttackType.TrueDamage) damage *= GetDefenseMultiplier(entry, defender);

            //round up once at the very end, so any hit that isn't fully blocked does at least 1. An immune hit is exactly 0 and stays 0
            int finalDamage = Mathf.CeilToInt(damage - RoundingTolerance); //the tiny tolerance stops float error (like 4.0000001) rounding a whole number up
            result._EntryDamage.Add(finalDamage);
            result._TotalDamage += finalDamage;
        }
        return result;
    }

    static float GetDefenseMultiplier(DamageEntry entry, CoreStats defender)
    {
        //function for how much of a hit gets through the defender's defense: ATK ÷ (ATK + 2 × DEF). Only the ratio matters,
        //so 10 vs 10 and 300 vs 300 feel the same. Defense always helps but never blocks 100%
        int defense = defender.GetCombatStat(defender.GetDefensiveStatType(entry._statType));
        if (defense <= 0) return 1f; //no defense stat for this kind of attack (like explosions), the full hit gets through

        float attack = Mathf.Max(MinimumAttackStat, entry._AttackStat);
        return attack / (attack + DefenseWeight * defense);
    }

    static float GetEffectiveness(DamageEntry entry, CoreStats defender)
    {
        //function for the weakness multiplier (like water against fire)
        return 1f; //todo: the element effectiveness chart isn't decided yet (see Elemental Affinity in the GDD)
    }
    #endregion

    #region Effects
    public static void ShowHitPopups(Vector3 position, HitResult result)
    {
        //function that shows one damage number per entry. Only the main (first) entry shows as a crit
        if (ScreenDamageUIManager._UIdamage == null || ScreenDamageUIManager._UIdamage._damageCanvas == null || result == null) return;
        for (int i = 0; i < result._EntryDamage.Count; i++)
        {
            bool showAsCrit = i == 0 && result._IsCrit;
            ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(position, result._EntryDamage[i], showAsCrit);
        }
    }

    public static void ShowHealPopup(Vector3 position, int amount)
    {
        //function that shows a heal number. The popup draws negative numbers in the healing color
        if (ScreenDamageUIManager._UIdamage == null || ScreenDamageUIManager._UIdamage._damageCanvas == null || amount <= 0) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(position, -amount, false);
    }

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

public class HitResult
{
    //what one hit actually did to a unit, worked out by CombatTools.ResolveHit

    public int _TotalDamage; //all entries added up, what comes off health
    public bool _IsCrit; //true if the main entry crit (for the popup)
    public List<int> _EntryDamage = new List<int>(); //final damage of each entry, in the same order as the package's entries (for popups)
}
