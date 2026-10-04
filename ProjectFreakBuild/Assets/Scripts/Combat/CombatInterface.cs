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

public interface IUnitHealth
{
    //anything that can report how healthy it is, so AI can react to being hurt (like fleeing)
    float GetHealthPercent(); //0 = dead, 1 = full health
}

public interface IUnitData
{
    DangerLevel GetDangerLevelSettings();
}