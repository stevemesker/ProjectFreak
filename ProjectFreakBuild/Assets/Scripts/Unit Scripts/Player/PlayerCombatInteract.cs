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
        
        SetActiveWeapon(Player.player.GetSelectedWeaponIndex() + (int)Mathf.Sign(context.ReadValue<float>()));
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
        SetActiveWeapon(Player.player.GetSelectedWeaponIndex() + direction);
        isCycling = true;
        cycleTimer = StartCoroutine(SelectionCycle(direction));
    }

    public void SetActiveWeapon(int index)
    {
        //function that handles switching weapon selection. Wraps the index around the slots, then lets the Player decide if it can switch right now
        int slotCount = pData.pInventory._EquipmentSize;
        if (slotCount <= 0) return; //no weapon slots, nothing to switch to (and % 0 would crash)

        int wpn = index;
        if (index < 0)
        {
            wpn = slotCount - Mathf.Abs(index % slotCount);
        }
        Player.player.SelectWeapon(wpn % slotCount);
    }

    #endregion

    #region UseWeapon
    private void UseWeapon(InputAction.CallbackContext context)
    {
        //attack button pressed, the Player passes it to the held weapon
        if (Player.player == null) return;
        Player.player.UseCurrentWeapon();
    }
    private void ReleaseWeapon(InputAction.CallbackContext context)
    {
        //attack button let go
        if (Player.player == null) return;
        Player.player.ReleaseCurrentWeapon();
    }

    #endregion
}
