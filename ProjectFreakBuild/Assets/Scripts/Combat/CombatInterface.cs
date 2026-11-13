using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    bool TakeDamage(DamagePackage dmgPackage);
}

public interface ITriggerable
{
    //anything a unit can hold and attack with (see Weapon usage in the GDD)
    void SetUpWeapon(ItemSO item, GameObject Wielder, CoreStats stats);
    void TriggerAttack();
    void ReleaseAttack();
    bool IsRange();
    bool IsBusy(); //true from the moment the weapon attacks until its attack time is over. Weapons can't be switched while busy
}

public interface IMeleeComboSource
{
    //anything that can hand a MeleeComboRunner a combo to play: a melee weapon now, the Melee Combo ability function later
    List<ComboStepEntry> GetCombo();
    float GetReach(); //multiplier on every swing shape's reach
    float GetComboResetTime(); //seconds without attacking before the combo goes back to the first hit
    float GetTimeScale(); //turns the steps' relative timings into real seconds
    float GetActivationShake(); //camera shake when a swing starts
    bool HitsAllies(); //friendly fire
    string GetSourceName(); //for error messages
    DamagePackage BuildHitPackage(GameObject wielder, CoreStats stats, ComboStepEntry step); //the damage one hit deals, built the moment it lands
}

public interface IRangedFireSource
{
    //anything that can hand a RangedPatternFirer its shot settings: a ranged weapon now, the Fire Projectile ability function later
    bool IsHitScan(); //instant rays instead of projectiles
    float GetFireDistance(); //hit scan range in meters, 0 = infinite
    int GetHitPierce(); //how many units one hit scan ray can hit
    float GetVerticalAimAngle(); //how far up or down hit scan tilts to reach a target, in degrees
    float GetAimWidth(); //how wide the hit scan aim line is, in meters
    float GetRayRadius(); //thickness of a hit scan ray, in meters
    GameObject GetTracerEffect(); //hit scan line effect, can be null
    GameObject GetImpactEffect(); //effect where a shot hits, can be null
    float GetImpactLifeTime(); //seconds before an impact effect is removed
    float GetActivationShake(); //camera shake each volley
    string GetSourceName(); //for error messages
}

public interface IKnockbackable
{
    //anything that can be pushed back by a hit. The damage package's _KnockbackDistance says how far
    void TakeKnockback(DamagePackage dmgPackage);
}

public interface IStaggerable
{
    //anything that can be staggered (interrupted and stunned briefly) by a hit. Gets every hit, even ones with no
    //stagger power, so bosses can check their health thresholds. The damage package's _StaggerPower says how likely it is
    void TakeStagger(DamagePackage dmgPackage);
    bool IsStaggered(); //true while staggered. Staggered units always take a crit (see CombatTools.ResolveHit)
}

public interface IAggroReceiver
{
    //anything that wants to know who hit it, so its AI can go after the attacker (see UnitTargeting)
    void AddAggro(DamagePackage dmgPackage);
}

public interface IKillable
{
    //anything that can die (see EnemyDeath). EnemyStats calls Kill at 0 health; self-destructs can call it directly
    void Kill();
    bool IsDead();
}

public interface IUnitHealth
{
    //anything that can report how healthy it is, so AI can react to being hurt (like fleeing)
    float GetHealthPercent(); //0 = dead, 1 = full health
}

public interface IUnitData
{
    DangerLevel GetDangerLevelSettings();
}