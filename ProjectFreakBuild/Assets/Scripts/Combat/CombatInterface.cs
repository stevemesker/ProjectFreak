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
    bool isRange();
}
