using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class PlayerCombatInteract : MonoBehaviour
{
    [SerializeField] float cycleTime;
    [SerializeField] float cycleScale;
    [SerializeField] bool isCycling;
    Coroutine cycleTimer;

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
        Player.player.setActiveWeapon(Player.player.getActiveWeaponIndex() + (int)Mathf.Sign(context.ReadValue<float>()));
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
        Player.player.setActiveWeapon(Player.player.getActiveWeaponIndex() + direction);
        isCycling = true;
        cycleTimer = StartCoroutine(selectionCycle(direction));
    }

    #endregion

    #region useWeapon
    private void useWeapon(InputAction.CallbackContext context)
    {
        Player.player.UseCurrentWeapon();
    }
    private void releaseWeapon(InputAction.CallbackContext context)
    {
        Player.player.releaseCurrentWeapon();
    }
    #endregion
}
