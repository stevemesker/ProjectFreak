using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;

public class Player : MonoBehaviour
{
    public static Player player;
    public PlayerData pData;
    public GameObject camTarget;

    [Header("Current State Data")]

    [Tooltip("Current selection number"), SerializeField]
    private int weaponSelection;
    [Tooltip("Points to the hand bone so weapon swapping knows where to instantiate the weapon to. IMPORTANT: hand bone must be the lowest level child as swapping checks for children and will delete it when swapping. Can easily break parent chains")]
    public GameObject handPointer;

    private ITriggerable weaponTrigger;
    //event Variable//
    [Header("Events")]
    [SerializeField] private PickupEventChannelSO pickupChannel;

    private void Start()
    {
        if (Player.player != null) { Destroy(gameObject); return; }
        Player.player = this;
        UpdateEquippedWeaponSlotSize();
    }

    private void OnEnable()
    {
        if (pickupChannel != null)
            pickupChannel.OnPickup += HandlePickup;
    }

    private void OnDisable()
    {
        if (pickupChannel != null)
            pickupChannel.OnPickup -= HandlePickup;
    }

    #region Item Pickup
    private void HandlePickup(ItemSO item, int amount, GameObject origin)
    {
        print("Picking up " + item.name);

        switch(item)
        {
            case WeaponItem weapon:
                if (pickUpWeapon(item as WeaponItem)) origin.GetComponent<ItemDrop>().removeItemInventory(amount);
                else Debug.LogWarning("Warning! Cannot fit weapon " + item.name);
                break;
            default:
                if (pickUpItem(item, amount)) origin.GetComponent<ItemDrop>().removeItemInventory(amount);
                else Debug.LogWarning("Warning! Cannot fit item " + item.name);
                break;
        }
    }

    private bool pickUpWeapon(WeaponItem wpn)
    {
        if (pData.pInventory.checkEquippedWeaponFits(wpn) == true)
        {
            pData.pInventory.addEquipmentInventory(wpn);
            updateCurrentWeapon();
            return true;
        }
        else if (pData.pInventory.checkInventoryFits(wpn, 1))
        {
            pData.pInventory.addBackpackInventory(wpn, 1);
            return true;
        }

        return false;
    }
    
    private bool pickUpItem(ItemSO item, int amount)
    {
        if (pData.pInventory.checkInventoryFits(item, amount))
        {
            pData.pInventory.addBackpackInventory(item, 1);
            return true;
        }
        return false;
    }
    #endregion

    #region Equipment
    public void setActiveWeapon (int index)
    {
        //function that hanles switching weapon selection
        int wpn = index;
        if (index < 0)
        {
            wpn = pData.pInventory._EquipmentSize - Mathf.Abs(index % pData.pInventory._EquipmentSize);
        }
        weaponSelection = wpn % pData.pInventory._EquipmentSize;
        updateCurrentWeapon();
    }

    void updateCurrentWeapon()
    {
        if (handPointer == null) { Debug.LogError("Error! Hand bone has not been selected to allow weapon swapping"); return; }
        /*
        if (weaponSelection+1 > pData.pInventory._EquippedWeapons.Count)
        {
            if (handPointer.transform.childCount != 0)
            {
                Destroy(handPointer.transform.GetChild(0).gameObject);
            }
            return;
        }
        */
        
        if (pData.pInventory._EquippedWeapons[weaponSelection] == null || pData.pInventory._EquippedWeapons[weaponSelection].weaponPrefab == null)
        {
            //empty selection or no weapon prefab, hold nothing
            if (handPointer.transform.childCount != 0)
            {
                Destroy(handPointer.transform.GetChild(0).gameObject);
            }
            return;
        }

        if (handPointer.transform.childCount != 0) Destroy(handPointer.transform.GetChild(0).gameObject);
        //Spawn Current Weapon
        GameObject wpn = Instantiate(pData.pInventory._EquippedWeapons[weaponSelection].weaponPrefab, handPointer.transform.position, handPointer.transform.transform.rotation, handPointer.transform);
        wpn.name = pData.pInventory._EquippedWeapons[weaponSelection].ItemName;

        wpn.GetComponent<ITriggerable>().SetUpWeapon(pData.pInventory._EquippedWeapons[weaponSelection], gameObject);

    }
    public void UpdateEquippedWeaponSlotSize()
    {
        //function ensures there are a correct number of equipped weapon slots available
        if (pData.pInventory._EquippedWeapons.Count < pData.pInventory._EquipmentSize)
        {
            for (int i = pData.pInventory._EquippedWeapons.Count; i < pData.pInventory._EquipmentSize; i++)
            {
                pData.pInventory._EquippedWeapons.Add(null);
            }
            return;
        }
        if (pData.pInventory._EquippedWeapons.Count > pData.pInventory._EquipmentSize)
        {
            //removes them if the size shrank. WARNING! Currently don't have a way to put the equipment back into an inventory cuz I dunno where it needs to go yet or if that's even a problem I'll run into
            for (int i = pData.pInventory._EquippedWeapons.Count; i > pData.pInventory._EquipmentSize; i--)
            {
                pData.pInventory._EquippedWeapons.RemoveAt(pData.pInventory._EquippedWeapons.Count - 1);
            }
            return;
        }
    }

    public int getActiveWeaponIndex()
    {
        return weaponSelection;
    }
    #endregion

    //[Button("Test equipment Index")]
    public ItemSO testInventoryIndex(int index)
    {
        if (pData.pInventory._BackpackInventory.Count < index + 1) return null;
        ItemSO keyAtIndex = pData.pInventory._BackpackInventory.Keys.ElementAt(index);
        return keyAtIndex;
    }
}
