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

    [SerializeField] private ItemSO item;
    [SerializeField] private int amount = 1;

    public void Awake()
    {
        if (ItemLootDrop == null) { Debug.LogError("Warning! Loot was dropped but " + gameObject.name + " does not contain loot data."); Destroy(gameObject);  return; }

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Player>() == false) return;

        print("Now adding " + ItemLootDrop.ItemName + " to inventory");
        pickupEvent.Raise(item, amount);

        Destroy(gameObject);
    }
}
