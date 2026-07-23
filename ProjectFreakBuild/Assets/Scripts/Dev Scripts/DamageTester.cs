using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class DamageTester : MonoBehaviour
{
    [Button("TestDamage")]
    public void testDamage()
    {
        if (ScreenDamageUIManager._UIdamage == null) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.displayDamage(transform.position, 100, false);
    }

    [Button("TestHealing")]
    public void testHealing()
    {
        if (ScreenDamageUIManager._UIdamage == null) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.displayDamage(transform.position, -100, false);
    }

    [Button("TestCrit")]
    public void testCritDamage(int amount)
    {
        if (ScreenDamageUIManager._UIdamage == null) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.displayDamage(transform.position, amount, true);
    }
}
