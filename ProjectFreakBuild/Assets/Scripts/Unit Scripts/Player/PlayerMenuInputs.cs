using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMenuInputs : MonoBehaviour
{
    private PlayerInput pInput;

    private void Awake()
    {
        pInput = new PlayerInput();
    }

    private void OnEnable()
    {
        pInput.Enable();
        pInput.Player.RadialMenu.performed += ToggleRadialMenu;
        pInput.Player.RadialMenu.canceled += ToggleRadialMenu;
    }

    private void OnDisable()
    {
        pInput.Player.RadialMenu.performed -= ToggleRadialMenu;
        pInput.Player.RadialMenu.canceled -= ToggleRadialMenu;
        pInput.Disable();
    }

    void ToggleRadialMenu(InputAction.CallbackContext context)
    {
        HUDManager._HUD.ToggleRadialMenu();
    }
}
