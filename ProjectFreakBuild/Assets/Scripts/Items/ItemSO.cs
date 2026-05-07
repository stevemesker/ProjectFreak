using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;



public abstract class ItemSO : ScriptableObject
{
    [TitleGroup("===Item Base Data===")]
    [Tooltip("Specific item ID, unique to the item type. Meaning id's for weapons and ingredients can overlap")]
    public int ItemID;
    public string ItemName;
    public string ItemDescription;
    [Tooltip("What art asset gameobject is spawned when the drop is created")]
    public GameObject dropArt;
    [Tooltip("Rarity type of the item: Normal, Common, Rare, Epic, Legendary, God")]public Rarity.Type ItemRarity;
}
