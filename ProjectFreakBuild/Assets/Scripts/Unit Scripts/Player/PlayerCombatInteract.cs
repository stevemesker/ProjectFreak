using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class PlayerCombatInteract : MonoBehaviour
{
    [Header("Weapon Switching")]
    [SerializeField] float cycleTime;
    [SerializeField] float cycleScale;
    [SerializeField] bool isCycling;
    Coroutine cycleTimer;

    [Header("Pointers")]
    [SerializeField]PlayerData pData;

    private PlayerInput pInput;

    private void Awake()
    {
        pInput = new PlayerInput();
    }

    private void OnEnable()
    {
        pInput.Enable();
        pInput.Player.WeaponSelect.performed += switchSelection;
        pInput.Player.WeaponSelect.canceled += endSelection;
        pInput.Player.Trigger.performed += useWeapon;
        pInput.Player.Trigger.canceled += releaseWeapon;
    }

    private void OnDisable()
    {
        pInput.Player.WeaponSelect.performed -= switchSelection;
        pInput.Player.WeaponSelect.canceled -= endSelection;
        pInput.Player.Trigger.performed -= useWeapon;
        pInput.Player.Trigger.canceled -= releaseWeapon;
        pInput.Disable();
    }

    #region Weapon Selecting
    void switchSelection(InputAction.CallbackContext context)
    {
        if (cycleTimer != null)
        {
            StopCoroutine(cycleTimer);
            cycleTimer = null;
            isCycling = false;
        }
        
        setActiveWeapon(Player.player.getActiveWeaponIndex() + (int)Mathf.Sign(context.ReadValue<float>()));
        cycleTimer = StartCoroutine(selectionCycle((int)Mathf.Sign(context.ReadValue<float>())));
        
    }

    void endSelection(InputAction.CallbackContext context)
    {
        
        StopCoroutine(cycleTimer);
        cycleTimer = null;
        isCycling = false;
        
    }

    IEnumerator selectionCycle(int direction)
    {
        float scale = new float();
        if (isCycling) scale = cycleScale;
        else scale = 1;

        yield return new WaitForSeconds(cycleTime / scale);
        setActiveWeapon(Player.player.getActiveWeaponIndex() + direction);
        isCycling = true;
        cycleTimer = StartCoroutine(selectionCycle(direction));
    }

    public void setActiveWeapon(int index)
    {
        //function that handles switching weapon selection
        int wpn = index;
        if (index < 0)
        {
            wpn = pData.pInventory._EquipmentSize - Mathf.Abs(index % pData.pInventory._EquipmentSize);
        }
        Player.player.weaponSelection = wpn % pData.pInventory._EquipmentSize;
        Player.player.updateCurrentWeapon();
    }

    #endregion

    #region useWeapon
    private void useWeapon(InputAction.CallbackContext context)
    {
        //Player.player.UseCurrentWeapon();
        print("Using weapon");
        if (Player.player.handPointer.transform.childCount == 0) { print("Need to add unarmed strike"); return; }
        if (Player.player.handPointer.GetComponent<ITriggerable>() != null) { print("Held item does not have itriggerable interface"); return; }

        //that 0 should be that proper stats the player uses to effect the weapon type. Figure that out later
        Player.player.handPointer.transform.GetChild(0).GetComponent<ITriggerable>().TriggerAttack();
    }
    private void releaseWeapon(InputAction.CallbackContext context)
    {
        //Player.player.releaseCurrentWeapon();
        print("Releasing weapon");
        if (Player.player.handPointer.transform.childCount == 0) { print("Need to add unarmed strike"); return; }
        if (Player.player.handPointer.GetComponent<ITriggerable>() != null) { print("Held item does not have itriggerable interface"); return; }

        Player.player.handPointer.transform.GetChild(0).GetComponent<ITriggerable>().ReleaseAttack();
    }

    #endregion
}
