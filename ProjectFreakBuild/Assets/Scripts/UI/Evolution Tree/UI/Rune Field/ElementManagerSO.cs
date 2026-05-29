using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SO_ElementManager", menuName = "ScriptableObjects/Shades/ShadeManagerSO", order = 0)]
public class ElementManagerSO : ScriptableObject
{
    public ShadeManager manager;

    #region runefield Functions
    public void boostStats(ElementItemSO source)
    {
        List<statBoostPackage> temp = source.getStatBoostPackage();
        Debug.Log(source.name + " is boosting " + temp.Count + " different stats. It's source comes from " + temp[0]._ElementConnect);
        manager.receiveStatBoostPackage(temp);
    }

    public void reduceStats(ElementItemSO source)
    {
        List<statBoostPackage> temp = source.getStatBoostPackage();
        Debug.Log(source.name + " is reducing " + temp.Count + " different stats. It's source comes from " + temp[0]._ElementConnect);
        manager.removeStatBoostPackage(temp);
    }
    #endregion
}

[Serializable]
public class statBoostPackage
{
    public ElementItemSO _linkedElement;
    public GameObject _ElementConnect;
    public StatNameType.Stat _statToChange;
    public int _ChangeAmount;
}