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
        List<statBoostPackage> temp = source.GetStatBoostPackage();
        //Debug.Log(source.name + " is boosting " + temp.Count + " different stats. It's source comes from " + temp[0]._ElementConnect);
        manager.ReceiveStatBoostPackage(temp);
    }

    public void ReduceStats(ElementItemSO source)
    {
        List<statBoostPackage> temp = source.GetStatBoostPackage();
        //Debug.Log(source.name + " is reducing " + temp.Count + " different stats. It's source comes from " + temp[0]._ElementConnect);
        manager.RemoveStatBoostPackage(temp);
    }
    #endregion
}

[Serializable]
public class statBoostPackage
{
    public ElementItemSO _linkedElement;
    public GameObject _ElementConnect;
    public DamageType.StatType _statToChange;
    public int _ChangeAmount;
}