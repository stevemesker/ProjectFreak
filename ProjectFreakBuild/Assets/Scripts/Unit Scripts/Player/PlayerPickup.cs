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

    #region Range Detection
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
    #endregion

    private void PickupInput(InputAction.CallbackContext context)
    {
        if (InRangePickup.Count < 1) return;
        
        //find closest item to pick up
        int index = 0;

        for (int i = 1; i < InRangePickup.Count; i++)
        {
            if (Vector3.Distance(InRangePickup[i].transform.position, transform.position) < Vector3.Distance(InRangePickup[index].transform.position, transform.position))
            {
                index = i;
            }
        }

        //GameObject tempObj = InRangePickup[index];
        IPickup temp = InRangePickup[index].GetComponent<IPickup>();
        Debug.Log($"Picking up item | {temp.GetObject().ItemName}");
        if (!TryGetComponent<IInventory>(out IInventory inv))
        {
            Debug.LogError(
                $"Error! Unit {gameObject.name} is trying to pick up item | {temp.GetObject().ItemName} | but this unit has no inventory interface..."
            );
            return;
        }

        handlePickupToInventory(temp, inv);
        InRangePickup.RemoveAt(index);
    }

    void handlePickupToInventory(IPickup pickup, IInventory inv)
    {
        ItemSO itm = pickup.GetObject();
        int amount = pickup.GetAmount();

        int remainder = inv.AddItem(itm, amount);

        //figure out how many we're taking from the stack
        amount -= remainder;

        pickup.TakeObjectAmount(amount);
    }
}
