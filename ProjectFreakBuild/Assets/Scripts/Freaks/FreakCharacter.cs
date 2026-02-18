using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreakCharacter : MonoBehaviour
{
    [Tooltip("Base stats for the indivicual freak loaded in from the game manager")]public FreakData freakData;
    [Tooltip("Current held inventory of the freak")] public FreakInventory _Inventory;

    //event Variable//
    [Header("Events")]
    [SerializeField] private PickupEventChannelSO pickupChannel;

    private void Awake()
    {
        _Inventory._FreakBackpackInventory = new Dictionary<ItemSO, int>();
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

    private void HandlePickup(ItemSO item, int amount, GameObject origin)
    {
        int slotsAvailable = _Inventory._FreakInventorySize - _Inventory._FreakBackpackInventory.Count;
        ItemDrop drop = new ItemDrop();

        if (origin != null) drop = origin.GetComponent<ItemDrop>();

        if (item is WeaponItem && slotsAvailable > 0)
        {
            //print("adding weapon " + item.ItemName + " to the lil freak's inventory");
            if (drop != null) drop.removeItemInventory(amount);
        }
        /*
        int remainder;
        int invCount;
        int amountTaken;
        ItemDrop pickupContainer = origin.GetComponent<ItemDrop>();


        if (_Inventory._FreakBackpackInventory.TryGetValue(item, out invCount))
        {
            remainder = InventoryManager._PlayerInventory.ItemStackSizeMax - invCount; 
        }
        */
    }
}
