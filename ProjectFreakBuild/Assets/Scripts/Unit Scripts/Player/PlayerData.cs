using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour, IUnitData, IInventory
{
    //script that governs the active data and stats of the player
    [Header("Base Stat Data")]
    [SerializeField, Tooltip("Core stats of the player")]
    public PlayerStats pStats;

    [Header("Inventory Data")]
    [SerializeField, Tooltip("Active inventory of the player")]
    public Inventory pInventory;

    [Header("Ability Data")]
    public List<AbilitySO> _TamerAbilities;
    public List<AbilitySO> _StandardAbilities;

    private void Awake()
    {
        pInventory._BackpackInventory = new Dictionary<ItemSO, int>();
    }


    #region Unit Data Interface
    public DangerLevel GetDangerLevelSettings()
    {
        return pStats._Danger;
    }

    #endregion

    #region Inventory Interface
    public int AddItem(ItemSO item, int amount)
    {
        switch(item)
        {
            case WeaponItem weapon:
                if (pInventory.CheckEquippedWeaponFits(item as WeaponItem))
                {
                    pInventory.AddEquipmentInventory(item as WeaponItem);
                    Player.player.UpdateCurrentWeapon();
                    //need to figure out if weapons can stack...
                    return amount - 1;
                }
                else
                {
                    //todo: add inventory functionality here
                }
                break;
            default:
                Debug.LogWarning($"Warning! Item {item.ItemName} is a type PlayerData doesn't handle yet, ignoring it...");
                break;
        }

        return 0;
    }

    public int GetItemAmount(ItemSO item)
    {
        throw new System.NotImplementedException();
    }

    public void RemoveItem(ItemSO item)
    {
        throw new System.NotImplementedException();
    }
    #endregion
}
