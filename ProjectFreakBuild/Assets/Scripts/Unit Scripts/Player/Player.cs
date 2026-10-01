using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    //Core
    public static Player player;
    public PlayerData pData;

    //State Data/Pointers
    [FoldoutGroup("Current State Data")]
    public GameObject camTarget;
    [FoldoutGroup("Current State Data")]
    [Tooltip("Points to the hand bone so weapon swapping knows where to instantiate the weapon to. IMPORTANT: hand bone must be the lowest level child as swapping checks for children and will delete it when swapping. Can easily break parent chains")]
    public GameObject handPointer;

    //Combat Variables
    [FoldoutGroup("Combat")][Header("Weapons")][SerializeField] 
    bool isCharging;
    [FoldoutGroup("Combat")][SerializeField] 
    float chargeAmount;
    [FoldoutGroup("Combat")][Tooltip("Current selection number"), SerializeField]
    public int weaponSelection;

    [FoldoutGroup("Script Pointers")]
    public CharacterMovement _movement;


    //Private/Unserialized Variables
    private ITriggerable weaponTrigger;
    private Coroutine chargeTime;
    private Coroutine cycleTimer;
    private float chargeTimeInitiated;

    //#region Initialize
    private void Awake()
    {
        if (Player.player != null) { Destroy(gameObject); return; }
        Player.player = this;
        DontDestroyOnLoad(gameObject);
        UpdateEquippedWeaponSlotSize();
        UpdateCurrentWeapon();
        CameraManager._CamManager.SetCamTargetToPlayer();
    }
    
    #region Equipment

    public void UpdateCurrentWeapon()
    {
        if (handPointer == null) { Debug.LogError("Error! Hand bone has not been selected to allow weapon swapping"); return; }

        if (pData.pInventory._EquippedWeapons[weaponSelection] == null || pData.pInventory._EquippedWeapons[weaponSelection].weaponPrefab == null)
        {
            //empty selection or no weapon prefab, hold nothing
            if (handPointer.transform.childCount != 0)
            {
                Destroy(handPointer.transform.GetChild(0).gameObject);
            }
            return;
        }

        if (handPointer.transform.childCount != 0) Destroy(handPointer.transform.GetChild(0).gameObject);
        //Spawn Current Weapon
        GameObject wpn = Instantiate(pData.pInventory._EquippedWeapons[weaponSelection].weaponPrefab, handPointer.transform.position, handPointer.transform.transform.rotation, handPointer.transform);
        wpn.name = pData.pInventory._EquippedWeapons[weaponSelection].ItemName;

        wpn.GetComponent<ITriggerable>().SetUpWeapon(pData.pInventory._EquippedWeapons[weaponSelection], gameObject, pData.pStats);
    }
    public void UpdateEquippedWeaponSlotSize()
    {
        //function ensures there are a correct number of equipped weapon slots available
        if (pData.pInventory._EquippedWeapons.Count < pData.pInventory._EquipmentSize)
        {
            for (int i = pData.pInventory._EquippedWeapons.Count; i < pData.pInventory._EquipmentSize; i++)
            {
                pData.pInventory._EquippedWeapons.Add(null);
            }
            return;
        }
        if (pData.pInventory._EquippedWeapons.Count > pData.pInventory._EquipmentSize)
        {
            //removes them if the size shrank. WARNING! Currently don't have a way to put the equipment back into an inventory cuz I dunno where it needs to go yet or if that's even a problem I'll run into
            for (int i = pData.pInventory._EquippedWeapons.Count; i > pData.pInventory._EquipmentSize; i--)
            {
                pData.pInventory._EquippedWeapons.RemoveAt(pData.pInventory._EquippedWeapons.Count - 1);
            }
            return;
        }
    }

    public int GetActiveWeaponIndex()
    {
        return weaponSelection;
    }
    #endregion

    #region Use Weapon
    public void UseCurrentWeapon()
    {
        if (handPointer.transform.childCount == 0) return; //todo: add unarmed strike
        if (handPointer.GetComponent<ITriggerable>() != null) return; //held item does not have the ITriggerable interface (see Known Issues)

        //that 0 should be that proper stats the player uses to effect the weapon type. Figure that out later
        handPointer.transform.GetChild(0).GetComponent<ITriggerable>().TriggerAttack();
    }
    public void ReleaseCurrentWeapon()
    {
        if (handPointer.transform.childCount == 0) return; //todo: add unarmed strike
        if (handPointer.GetComponent<ITriggerable>() != null) return; //held item does not have the ITriggerable interface (see Known Issues)

        handPointer.transform.GetChild(0).GetComponent<ITriggerable>().ReleaseAttack();
    }

    #endregion

    #region Disabling Player Character
    public void EnablePlayerControl()
    {
        _movement.EnableMovement();
    }

    public void DisablePlayerControl()
    {
        _movement.DisableMovement();
    }

    public void SetPlayerTurning(bool Active)
    {
        _movement.SetTurning(Active);
    }

    #endregion
}
