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
        pInput.Player.WeaponSelect.performed += SwitchSelection;
        pInput.Player.WeaponSelect.canceled += EndSelection;
        pInput.Player.Trigger.performed += UseWeapon;
        pInput.Player.Trigger.canceled += ReleaseWeapon;
    }

    private void OnDisable()
    {
        pInput.Player.WeaponSelect.performed -= SwitchSelection;
        pInput.Player.WeaponSelect.canceled -= EndSelection;
        pInput.Player.Trigger.performed -= UseWeapon;
        pInput.Player.Trigger.canceled -= ReleaseWeapon;
        pInput.Disable();
    }

    #region Weapon Selecting
    void SwitchSelection(InputAction.CallbackContext context)
    {
        if (cycleTimer != null)
        {
            StopCoroutine(cycleTimer);
            cycleTimer = null;
            isCycling = false;
        }
        
        SetActiveWeapon(Player.player.GetActiveWeaponIndex() + (int)Mathf.Sign(context.ReadValue<float>()));
        cycleTimer = StartCoroutine(SelectionCycle((int)Mathf.Sign(context.ReadValue<float>())));
        
    }

    void EndSelection(InputAction.CallbackContext context)
    {
        
        StopCoroutine(cycleTimer);
        cycleTimer = null;
        isCycling = false;
        
    }

    IEnumerator SelectionCycle(int direction)
    {
        float scale = new float();
        if (isCycling) scale = cycleScale;
        else scale = 1;

        yield return new WaitForSeconds(cycleTime / scale);
        SetActiveWeapon(Player.player.GetActiveWeaponIndex() + direction);
        isCycling = true;
        cycleTimer = StartCoroutine(SelectionCycle(direction));
    }

    public void SetActiveWeapon(int index)
    {
        //function that handles switching weapon selection
        int wpn = index;
        if (index < 0)
        {
            wpn = pData.pInventory._EquipmentSize - Mathf.Abs(index % pData.pInventory._EquipmentSize);
        }
        Player.player.weaponSelection = wpn % pData.pInventory._EquipmentSize;
        Player.player.UpdateCurrentWeapon();
    }

    #endregion

    #region UseWeapon
    private void UseWeapon(InputAction.CallbackContext context)
    {
        //Player.player.UseCurrentWeapon();
        if (Player.player.handPointer.transform.childCount == 0) return; //todo: add unarmed strike
        if (Player.player.handPointer.GetComponent<ITriggerable>() != null) return; //held item does not have the ITriggerable interface (see Known Issues)

        //that 0 should be that proper stats the player uses to effect the weapon type. Figure that out later
        Player.player.handPointer.transform.GetChild(0).GetComponent<ITriggerable>().TriggerAttack();
    }
    private void ReleaseWeapon(InputAction.CallbackContext context)
    {
        //Player.player.ReleaseCurrentWeapon();
        if (Player.player.handPointer.transform.childCount == 0) return; //todo: add unarmed strike
        if (Player.player.handPointer.GetComponent<ITriggerable>() != null) return; //held item does not have the ITriggerable interface (see Known Issues)

        Player.player.handPointer.transform.GetChild(0).GetComponent<ITriggerable>().ReleaseAttack();
    }

    #endregion
}
