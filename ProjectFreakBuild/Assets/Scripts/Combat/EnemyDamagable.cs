using ElementType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class EnemyDamagable : MonoBehaviour, IDamagable
{
    public BaseStats baseStats;

    [Header ("Current Stats")]
    public int currentHealth;
    
    public bool TakeDamage(int damage, DamageType.Type type, GameObject agressor, Element element)
    {
        if (type == DamageType.Type.TrueDamage) { takeTrueDamage(damage); return true; } //handles true damage
        currentHealth -= damage *  (int)(testResistanceDamageType(type) * testResistanceElement(element));

        return true;
    }

    private bool takeTrueDamage(int damage)
    {
        //for dealing damage that cannot be modified or resisted
        currentHealth -= damage;
        return true;
    }

    [Button("Test Elemental Resistance")]
    private float testResistanceElement(Element element)
    {
        float finalMultiplier = 1;
        for (int i = 0; i < baseStats._ElementResistance.Count; i++)
        {
            if (baseStats._ElementResistance[i] == element)
            {
                print("resistance found! Type: " + element);
                finalMultiplier = finalMultiplier / baseStats._ElementResitanceMultiplier;
            }
        }
        for (int i = 0; i < baseStats._ElementWeakness.Count; i++)
        {
            if (baseStats._ElementWeakness[i] == element)
            {
                print("weakness found! Type: " + element);
                finalMultiplier = finalMultiplier * baseStats._ElementWeaknessMultiplier;
            }
        }
        return finalMultiplier;
    }

    [Button("Test Damage Type Resistance")]
    private float testResistanceDamageType(DamageType.Type type)
    {
        float finalMultiplier = 1;
        for (int i = 0; i < baseStats._TypeResistance.Count; i++)
        {
            if (baseStats._TypeResistance[i] == type)
            {
                print("resistance found! Type: " + type);
                finalMultiplier = finalMultiplier / baseStats._TypeResitanceMultiplier;
            }
        }
        for (int i = 0; i < baseStats._TypeWeakness.Count; i++)
        {
            if (baseStats._TypeWeakness[i] == type)
            {
                print("weakness found! Type: " + type);
                finalMultiplier = finalMultiplier * baseStats._TypeWeaknessMultiplier;
            }
        }
        return finalMultiplier;
    }
}
