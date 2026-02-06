using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;



public abstract class ItemSO : ScriptableObject
{
    [TitleGroup("===Item Base Data===")]
    public int ItemID;
    public string ItemName;
    public string ItemDescription;
}
