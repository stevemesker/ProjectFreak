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
    [Tooltip("Points to the hand bone so weapon swapping knows where to instantiate the weapon to. Only the weapon this script spawned gets removed when swapping, so other children of the hand are safe")]
    public GameObject handPointer;

    //Combat Variables
    [FoldoutGroup("Combat")][Tooltip("Current selection number"), SerializeField]
    public int weaponSelection;

    [FoldoutGroup("Combat")]
    [Tooltip("Weapon slot waiting to be switched to once the held weapon finishes its attack. -1 = nothing queued (read only)")]
    [SerializeField, ReadOnly] int _QueuedWeaponSelection = -1;

    [FoldoutGroup("Script Pointers")]
    public CharacterMovement _movement;
    [FoldoutGroup("Script Pointers")]
    [Tooltip("Reads the player's controls and steers _movement. Turned off while the player controls a shade. Grabbed from this object if left empty")]
    public PlayerInputDriver _InputDriver;

    //local variables
    GameObject _heldWeaponObject; //the weapon currently spawned in the hand
    ITriggerable _heldWeapon; //the weapon script on it
    WeaponItem _heldWeaponItem; //the item it was spawned from, so asking for the same item again doesn't respawn it
    Coroutine _switchBuffer; //waits for the held weapon to stop being busy, then switches to the queued slot

    //#region Initialize
    private void Awake()
    {
        if (Player.player != null) { Destroy(gameObject); return; }
        Player.player = this;
        DontDestroyOnLoad(gameObject);
        if (_InputDriver == null) _InputDriver = GetComponent<PlayerInputDriver>();
        UpdateEquippedWeaponSlotSize();
        UpdateCurrentWeapon();
        CameraManager._CamManager.SetCamTargetToPlayer();
    }

    #region Equipment
    public void SelectWeapon(int index)
    {
        //function for picking which equipped weapon slot to hold
        //attack speed can never be bypassed, so if the held weapon is mid attack the pick is queued until it's free
        int slotCount = pData.pInventory._EquippedWeapons.Count;
        if (index < 0 || index >= slotCount) { Debug.LogWarning($"Warning! Weapon slot {index} doesn't exist on {gameObject.name}, ignoring the switch...", this); return; }

        //picking the weapon we're already holding clears anything queued, so the player can change their mind
        if (index == weaponSelection)
        {
            ClearQueuedWeapon();
            return;
        }

        if (IsHeldWeaponBusy())
        {
            //remember the latest pick (no time limit) and switch the moment the weapon is free
            _QueuedWeaponSelection = index;
            if (_switchBuffer == null) _switchBuffer = StartCoroutine(SwitchWhenFree());
            return;
        }

        weaponSelection = index;
        UpdateCurrentWeapon();
    }

    public void UpdateCurrentWeapon()
    {
        //function that makes the hand hold whatever is in the selected slot: spawns the weapon and sets it up
        if (handPointer == null) { Debug.LogError($"Error! Hand bone has not been selected on {gameObject.name} to allow weapon swapping", this); return; }

        List<WeaponItem> equippedWeapons = pData.pInventory._EquippedWeapons;
        if (weaponSelection < 0 || weaponSelection >= equippedWeapons.Count)
        {
            Debug.LogWarning($"Warning! Weapon selection {weaponSelection} is outside the equipped slots on {gameObject.name}, holding nothing...", this);
            ClearHeldWeapon();
            return;
        }

        WeaponItem selectedItem = equippedWeapons[weaponSelection];

        //already holding this exact item, nothing to do. Respawning it would reset its attack timer (like when picking up a weapon into another slot)
        if (selectedItem != null && selectedItem == _heldWeaponItem && _heldWeaponObject != null) return;

        ClearHeldWeapon();
        if (selectedItem == null || selectedItem._WeaponPrefab == null) return; //empty slot or no weapon prefab, hold nothing

        //Spawn Current Weapon
        _heldWeaponObject = Instantiate(selectedItem._WeaponPrefab, handPointer.transform.position, handPointer.transform.rotation, handPointer.transform);
        _heldWeaponObject.name = selectedItem.ItemName;
        _heldWeaponItem = selectedItem;

        //TryGetComponent works with interfaces too, it finds whichever component implements ITriggerable
        if (_heldWeaponObject.TryGetComponent(out ITriggerable weapon) == false)
        {
            Debug.LogError($"Error! Weapon prefab {selectedItem._WeaponPrefab.name} has no ITriggerable script (like WeaponAttackRanged), it can't attack", this);
            return;
        }

        _heldWeapon = weapon;
        _heldWeapon.SetUpWeapon(selectedItem, gameObject, pData.pStats);
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
        //function that gives the slot currently held in the hand
        return weaponSelection;
    }

    public int GetSelectedWeaponIndex()
    {
        //function that gives the slot the player has picked: the queued one if a switch is waiting, otherwise the held one
        //used for scrolling, so scrolling past several weapons while busy lands on the last one picked
        if (_QueuedWeaponSelection >= 0) return _QueuedWeaponSelection;
        return weaponSelection;
    }

    IEnumerator SwitchWhenFree()
    {
        //waits until the held weapon finishes its attack, then switches to whatever was picked last
        while (IsHeldWeaponBusy()) yield return null;

        _switchBuffer = null;
        int queuedSelection = _QueuedWeaponSelection;
        _QueuedWeaponSelection = -1;
        if (queuedSelection < 0) yield break; //the queue was cleared while waiting

        weaponSelection = queuedSelection;
        UpdateCurrentWeapon();
    }

    void ClearQueuedWeapon()
    {
        //function that forgets a queued switch
        _QueuedWeaponSelection = -1;
        if (_switchBuffer != null) StopCoroutine(_switchBuffer);
        _switchBuffer = null;
    }

    void ClearHeldWeapon()
    {
        //function that removes the weapon this script spawned in the hand
        if (_heldWeaponObject != null) Destroy(_heldWeaponObject);
        _heldWeaponObject = null;
        _heldWeapon = null;
        _heldWeaponItem = null;
    }

    bool IsHeldWeaponBusy()
    {
        //function for checking if the held weapon is mid attack
        //checks the GameObject first: a destroyed weapon still looks "not null" through the interface, but Unity's GameObject check knows it's gone
        if (_heldWeaponObject == null || _heldWeapon == null) return false;
        return _heldWeapon.IsBusy();
    }
    #endregion

    #region Use Weapon
    public void UseCurrentWeapon()
    {
        //function for pressing the attack button with the held weapon
        if (_heldWeaponObject == null || _heldWeapon == null) return; //todo: add unarmed strike
        _heldWeapon.TriggerAttack();
    }

    public void ReleaseCurrentWeapon()
    {
        //function for letting go of the attack button
        if (_heldWeaponObject == null || _heldWeapon == null) return;
        _heldWeapon.ReleaseAttack();
    }
    #endregion

    #region Disabling Player Character
    public void EnablePlayerControl()
    {
        //gives the controls back to the player's body by turning its input driver on
        if (_InputDriver == null) { Debug.LogError($"Error! No PlayerInputDriver on {gameObject.name}, can't give it control", this); return; }
        _InputDriver.enabled = true;
    }

    public void DisablePlayerControl()
    {
        //takes the controls away from the player's body (it stops and keeps floating, see PlayerInputDriver.OnDisable)
        if (_InputDriver == null) { Debug.LogError($"Error! No PlayerInputDriver on {gameObject.name}, can't take its control away", this); return; }
        _InputDriver.enabled = false;
    }

    public void SetPlayerTurning(bool Active)
    {
        _movement.SetTurning(Active);
    }

    #endregion
}
