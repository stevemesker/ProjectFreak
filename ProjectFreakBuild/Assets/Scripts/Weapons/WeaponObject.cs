using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponObject : MonoBehaviour
{
    [Header("Pointer Variables")]
    public GameObject _Art;
    public GameObject _HandleBoneObject;
    public GameObject _PlacementBoneOffsetObject;
    private ItemInstanceOffset instOffset; //finds out if the item has the offset script attached. Will be null if not so assume all pointers will be the object's origin
    //public WeaponItem _WeaponData;

    #region Initializing
    private void Awake()
    {
        instOffset = GetComponent<ItemInstanceOffset>();
    }
    #endregion

    public void OffsetObject(GameObject obj)
    {
        if (instOffset && obj != null) 
        {
            instOffset.OffsetTarget = obj;
            instOffset.OffsetObject(); 
        }
    }
}
