using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : MonoBehaviour
{
    public EnemyCoreStats eStats;

    [SerializeField]
    private UnityEvent onDeath;


    public void TakeDamage(DamagePackage dmg)
    {
        print(dmg._Source.name + " hit " + gameObject.name);
        int damageTakenTotal = 0;
        for (int i = 0; i < dmg._Entries.Count; i++)
        {
            damageTakenTotal += (int)(DamageCalculation(dmg._Entries[i])*dmg._CritMultiplier);
        }
        eStats._Health -= damageTakenTotal;
        if (eStats._Health <= 0) onDeath?.Invoke();
        if (eStats._Health > eStats._HP) eStats._Health = eStats._HP;
    }

    public int DamageCalculation(DamageEntry entry)
    {
        float defenseStat = -eStats.TypeToStatFinder(eStats.GetDefensiveStatType(entry._statType));
        float dmg = ((entry._Damage)-defenseStat)/eStats.GetAttackResistanceModifier(entry._atkType, entry._elementType);
        return (int)dmg;
    }
}
