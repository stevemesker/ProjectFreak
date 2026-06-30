using ElementType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class EnemyDamagable : MonoBehaviour, IDamagable
{
    [System.Serializable]
    public class DamageEvent : UnityEngine.Events.UnityEvent<DamagePackage> { }

    [SerializeField]
    private DamageEvent onDamage;

    [SerializeField]
    private UnityEvent onHit;

    //depreciated
    //public CoreStats baseStats;
    //[Header ("Current Stats")]
    //public int currentHealth;

    public bool TakeDamage(DamagePackage dmgPackage)
    {
        print(dmgPackage._Source.name);
        //used when something should happen when hit but does not need damage packages
        onHit?.Invoke();

        //special note, functions called here have to recieve a damage package on the first variable of the function. If not use the on hit event instead
        onDamage?.Invoke(dmgPackage);
        return true;
    }
}
