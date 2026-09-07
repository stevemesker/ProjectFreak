using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    bool CanInteract();
    void Interact(GameObject Origin);
}

public interface IPickup
{
    ItemSO GetObject();
    int GetAmount();
    void TakeObjectAmount(int amount);
}

public interface IInventory
{
    //adds to the inventory and returns how much is left over from the stack
    int AddItem(ItemSO item, int amount);

    int GetItemAmount(ItemSO item);

    void RemoveItem(ItemSO item);
}
