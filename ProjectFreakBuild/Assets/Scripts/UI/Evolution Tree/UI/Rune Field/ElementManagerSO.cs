using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SO_ElementManager", menuName = "ScriptableObjects/Shades/ShadeManagerSO", order = 0)]
public class ElementManagerSO : ScriptableObject
{
    public ShadeManager manager;

    #region runefield Functions
    public void BoostStats(ElementItemSO source)
    {
        //function that sends an element's stat boosts to the shade manager when its rune gets power
        if (manager == null) { Debug.LogError($"Error! No Shade Manager registered on {name}, can't boost stats from {source.name}", this); return; }
        List<statBoostPackage> temp = source.GetStatBoostPackage();
        manager.ReceiveStatBoostPackage(temp);
    }

    public void ReduceStats(ElementItemSO source)
    {
        //function that takes an element's stat boosts back off when its rune loses power
        if (manager == null) { Debug.LogError($"Error! No Shade Manager registered on {name}, can't reduce stats from {source.name}", this); return; }
        List<statBoostPackage> temp = source.GetStatBoostPackage();
        manager.RemoveStatBoostPackage(temp);
    }
    #endregion
}

[Serializable]
public class statBoostPackage
{
    public ElementItemSO _linkedElement;
    public DamageType.StatType _statToChange;
    public int _ChangeAmount;
}