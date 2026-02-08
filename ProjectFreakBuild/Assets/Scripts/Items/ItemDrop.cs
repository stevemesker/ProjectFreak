using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemDrop : MonoBehaviour
{
    //Script for objects that drop after an enemy is killed that handles treasure containment
    public ItemSO ItemLootDrop;
    public int ItemLootAmount;

    [Header("Event Channel")]
    [SerializeField] private PickupEventChannelSO pickupEvent;


    public void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == false) return;

        if (ItemLootDrop == null)
        {
            Debug.LogWarning("Warning! Item pickup wwas attempted however no item was found.");
            return;
        }

        print("Now adding " + ItemLootDrop.ItemName + " to inventory");
        pickupEvent.Raise(ItemLootDrop, ItemLootAmount);

        Destroy(gameObject);
    }
}
