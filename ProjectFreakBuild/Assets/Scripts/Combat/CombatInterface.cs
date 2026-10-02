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

public interface IUnitData
{
    DangerLevel GetDangerLevelSettings();
}