using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class DamageTester : MonoBehaviour
{
    [Button("TestDamage")]
    public void TestDamage()
    {
        if (ScreenDamageUIManager._UIdamage == null) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(transform.position, 100, false);
    }

    [Button("TestHealing")]
    public void TestHealing()
    {
        if (ScreenDamageUIManager._UIdamage == null) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(transform.position, -100, false);
    }

    [Button("TestCrit")]
    public void TestCritDamage(int amount)
    {
        if (ScreenDamageUIManager._UIdamage == null) return;
        ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(transform.position, amount, true);
    }
}
