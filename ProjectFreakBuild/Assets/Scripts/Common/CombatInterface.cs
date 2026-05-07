using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    bool TakeDamage(int damage, DamageType.Type type, GameObject agressor, ElementType.Element element);
}

public interface ITriggerable
{
    void SetUpWeapon(ItemSO item, GameObject Wielder);
    void updateStats(int power, List<ElementType.Element> element);
    void TriggerAttack(int power, List<ElementType.Element> element);
    void ReleaseAttack();
}
