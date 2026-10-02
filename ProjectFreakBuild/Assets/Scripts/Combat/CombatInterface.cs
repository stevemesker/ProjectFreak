using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    bool TakeDamage(DamagePackage dmgPackage);
}

public interface ITriggerable
{
    void SetUpWeapon(ItemSO item, GameObject Wielder, CoreStats stats);
    void TriggerAttack();
    void ReleaseAttack();
    //DamageType.StatType GetStatType();
    bool IsRange();
}

public interface IKnockbackable
{
    //anything that can be pushed back by a hit. The damage package's _KnockbackDistance says how far
    void TakeKnockback(DamagePackage dmgPackage);
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