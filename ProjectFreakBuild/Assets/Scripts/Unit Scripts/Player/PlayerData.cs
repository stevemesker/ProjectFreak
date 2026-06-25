using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    //script that governs the active data and stats of the player
    [Header("Base Stat Data")]
    [SerializeField, Tooltip("Core stats of the player")]
    public PlayerStats pStats;

    [Header("Inventory Data")]
    [SerializeField, Tooltip("Active inventory of the player")]
    public Inventory pInventory;

    private void Awake()
    {
        pInventory._BackpackInventory = new Dictionary<ItemSO, int>();
    }
}
