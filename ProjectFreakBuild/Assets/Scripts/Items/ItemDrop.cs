using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider))]
public class ItemDrop : MonoBehaviour
{
    //Script for objects that drop after an enemy is killed that handles treasure containment
    [Tooltip("Item Scriptable Object data being passed along. AKA the item that was dropped")] public ItemSO ItemLootDrop;
    [Tooltip("How many copies of the item being dropped. Debug being 1")] public int ItemLootAmount;
    [SerializeField, Tooltip("Points to the child gameobject that the art gets instantiated under")] private GameObject _ArtParent;

    [Header("Event Channel")]
    [SerializeField, Tooltip("Pickup event pointer")] private PickupEventChannelSO pickupEvent;

    #region Initializing
    public void OnEnable()
    {
        if (ItemLootDrop == null) { Debug.LogError("null item spawn"); return; }
        fillDrop(ItemLootDrop);
    }

    public void fillDrop(ItemSO itm)
    {
        ItemLootDrop = itm;
        GameObject art = Instantiate(ItemLootDrop.dropArt, _ArtParent.transform.position, _ArtParent.transform.rotation, _ArtParent.transform);
        if (checkForWeapon())
        {
            WeaponObject wpn = art.GetComponent<WeaponObject>(); //quick grab the instanced weapon prefab

            //offsetting position
            wpn.OffsetObject(wpn._PlacementBoneOffsetObject);
            //spinning effect
            _ArtParent.GetComponent<ItemFloatAndSpin>().enabled = true;
            //set up rarity effects
        }
    }

    [Button("Test for Waapon")]private bool checkForWeapon()
    {
        //fucntion that tests if item is a weapon to handle turning on the spin and offset stuff. Could possibly make this an interface later but probably won't need to
        if (ItemLootDrop is WeaponItem weapon) return true;
        return false;
    }

    #endregion

    #region Interaction
    public void pickupItem()
    {
        if (ItemLootDrop == null)
        {
            Debug.LogWarning("Warning! Item pickup wwas attempted however no item was found.");
            return;
        }
        //print("Sending signal");
        pickupEvent.Raise(ItemLootDrop, ItemLootAmount, gameObject);

        //add pickup effects here
    }
    
    public void removeItemInventory(int amount)
    {
        //print("Removing " + amount + " " + ItemLootDrop.ItemName);
        ItemLootAmount -= amount;
        if (ItemLootAmount == 0)
        {
            //print("empty");
            //empty out everything
            Destroy(gameObject);
        }
    }
    #endregion

    #region Spawning
    public void MoveArc(Vector3 location, float arcHeight, float speed)
    {
        _ArtParent.GetComponent<ItemFloatAndSpin>().enabled = false;
        GetComponent<ArcMover>().LaunchTo(location, arcHeight, speed);
    }

    #endregion
}
