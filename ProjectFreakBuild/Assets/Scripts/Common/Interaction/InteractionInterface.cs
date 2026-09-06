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
