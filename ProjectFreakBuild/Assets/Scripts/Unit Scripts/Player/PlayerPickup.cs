using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPickup : MonoBehaviour
{
    private PlayerInput pInput;
    [SerializeField]private List<GameObject> InRangePickup;

    private void Awake()
    {
        pInput = new PlayerInput();
    }
    private void OnEnable()
    {
        pInput.Enable();

        pInput.Player.Pickup.performed += PickupInput;
    }
    private void OnDisable()
    {
        pInput.Player.Pickup.performed -= PickupInput;
        pInput.Disable();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Item") return;

        ItemSO itm = other.gameObject.GetComponent<ItemDrop>().ItemLootDrop;

        if (itm is WeaponItem) { InRangePickup.Add(other.gameObject); /*print("Adding weapon to gatherables");*/ return; }
        print("Adding ingredient to inventory automatically");

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "Item") return;

        ItemSO itm = other.gameObject.GetComponent<ItemDrop>().ItemLootDrop;

        if (itm is WeaponItem) { InRangePickup.Remove(other.gameObject); /*print("Removing weapon to gatherables");*/ return; }
        
    }
    private void PickupInput(InputAction.CallbackContext context)
    {
        if (InRangePickup.Count < 1) return;
        if (InRangePickup.Count == 1) 
        {
            //print("Now picking up " + InRangePickup[0].name);
            InRangePickup[0].GetComponent<ItemDrop>().pickupItem();
            InRangePickup.Clear();
            return; 
        }
        int index = 0;
        for (int i = 1; i < InRangePickup.Count; i++)
        {
            if (Vector3.Distance(InRangePickup[i].transform.position, transform.position) < Vector3.Distance(InRangePickup[index].transform.position, transform.position))
            {
                index = i;
            }
        }
        InRangePickup[index].GetComponent<ItemDrop>().pickupItem();
        InRangePickup.RemoveAt(index);
    }
}
