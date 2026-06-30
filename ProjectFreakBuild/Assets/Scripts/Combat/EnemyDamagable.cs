using ElementType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EnemyDamagable : MonoBehaviour, IDamagable
{
    public CoreStats baseStats;


    [Header ("Current Stats")]
    public int currentHealth;

    public bool TakeDamage(DamagePackage dmgPackage)
    {
        //primary function from the IDamagable class
        int finalDamage = 0;
        for(int i = 0; i < dmgPackage._Entries.Count; i++)
        {
            if (dmgPackage._Entries[i]._atkType == DamageType.AttackType.TrueDamage)
            {
                finalDamage += (int)(dmgPackage._Entries[i]._Damage * dmgPackage._CritMultiplier);
            }
            else
            {
                finalDamage += takeNormalDamage(dmgPackage._Entries[i], dmgPackage._CritMultiplier);
            }
        }
        if (finalDamage == 0) return false;
        if (finalDamage < 0 && currentHealth - finalDamage > baseStats._HP) finalDamage = currentHealth - baseStats._HP; //this is for somehow healing
        currentHealth -= finalDamage;
        if (currentHealth <= 0) killUnit();
        return true;
    }

    int takeNormalDamage(DamageEntry atk, float crit)
    {
        switch(atk._statType)
        {
            case DamageType.StatType.Strength:
                return (int)((atk._Damage - baseStats.TypeToStatFinder(baseStats.PhysicalDefMod)) * crit);
            case DamageType.StatType.Defense:
                return (int)((atk._Damage - baseStats.TypeToStatFinder(baseStats.PhysicalDefMod)) * crit);
            case DamageType.StatType.Agility:
                return (int)((atk._Damage - baseStats.TypeToStatFinder(baseStats.PhysicalDefMod)) * crit);
            case DamageType.StatType.Intelect:
                return (int)((atk._Damage - baseStats._SPR) * crit);
            case DamageType.StatType.Spirit:
                return (int)((atk._Damage - baseStats._SPR) * crit);
            case DamageType.StatType.Wisdom:
                return (int)((atk._Damage - baseStats._SPR) * crit);
            default: return atk._Damage;
        }
        
        //return 0;
    }

    private void killUnit()
    {
        print("I dead");
        Destroy(gameObject);
    }
}
