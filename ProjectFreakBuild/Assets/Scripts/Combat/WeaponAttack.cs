using ElementType;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponAttack : MonoBehaviour, ITriggerable
{
    [SerializeField] private WeaponObject _Weapon;

    public bool ReleaseAttack()
    {
        print("Click!");
        return true;
    }

    public void TriggerAttack(int power, List<Element> element)
    {
        print("Pow!");
    }
}
