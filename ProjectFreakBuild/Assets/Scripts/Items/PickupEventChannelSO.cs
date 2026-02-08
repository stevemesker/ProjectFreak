using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Item Pickup Event Channel")]
public class PickupEventChannelSO : ScriptableObject
{
    public event Action<ItemSO, int> OnPickup;

    public void Raise(ItemSO item, int amount)
    {
        OnPickup?.Invoke(item, amount);
    }
}
