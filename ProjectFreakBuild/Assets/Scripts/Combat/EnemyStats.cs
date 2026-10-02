using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : MonoBehaviour, IUnitHealth
{
    public EnemyCoreStats eStats;

    [SerializeField]
    private UnityEvent onDeath;


    public void TakeDamage(DamagePackage dmg)
    {
        int damageTakenTotal = 0;
        for (int i = 0; i < dmg._Entries.Count; i++)
        {
            if (ScreenDamageUIManager._UIdamage != null) ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(transform.position, (int)(DamageCalculation(dmg._Entries[i]) * dmg._CritMultiplier), false);
            damageTakenTotal += (int)(DamageCalculation(dmg._Entries[i])*dmg._CritMultiplier);
        }
        eStats._Health -= damageTakenTotal;
        

        if (eStats._Health <= 0) onDeath?.Invoke();
        if (eStats._Health > eStats._HP) eStats._Health = eStats._HP;
    }

    public float GetHealthPercent()
    {
        //function that gives how healthy this enemy is, 0 to 1. Used by the AI brain (like deciding when to flee)
        if (eStats == null || eStats._HP <= 0) return 1f; //no max health set, treat it as healthy instead of dividing by 0
        return Mathf.Clamp01((float)eStats._Health / eStats._HP); //(float) so the division keeps decimals instead of rounding to 0
    }

    public int DamageCalculation(DamageEntry entry)
    {
        float defenseStat = -eStats.TypeToStatFinder(eStats.GetDefensiveStatType(entry._statType));
        float dmg = ((entry._Damage)-defenseStat)/eStats.GetAttackResistanceModifier(entry._atkType, entry._elementType);
        return (int)dmg;
    }
}
