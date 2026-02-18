using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Item Pickup Event Channel")]
public class PickupEventChannelSO : ScriptableObject
{
    public event Action<ItemSO, int, GameObject> OnPickup;

    public void Raise(ItemSO item, int amount, GameObject source)
    {
        OnPickup?.Invoke(item, amount, source);
    }
}
