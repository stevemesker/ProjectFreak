using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreakInput : MonoBehaviour
{
    [Tooltip("References the freak character class to access inventory")]
    [SerializeField] private FreakCharacter characterData;
    private PlayerInput pInput;

    // Start is called before the first frame update
    void Awake()
    {
        pInput = new PlayerInput();
    }

    private void OnEnable()
    {
        pInput.Enable();
        pInput.Freak.FreakWeaponSelect.performed += switchSelection;
        pInput.Freak.FreakWeaponScroll.performed += scrollSelection;
        pInput.Freak.FreakWeaponActivation.performed += useWeapon;
        pInput.Freak.FreakWeaponActivation.canceled += releaseWeapon;
    }

    private void OnDisable()
    {
        pInput.Freak.FreakWeaponSelect.performed -= switchSelection;
        pInput.Freak.FreakWeaponScroll.performed -= scrollSelection;
        pInput.Freak.FreakWeaponActivation.performed -= useWeapon;
        pInput.Freak.FreakWeaponActivation.canceled -= releaseWeapon;
        pInput.Disable();
    }

    #region WeaponSelecting

    private void switchSelection(InputAction.CallbackContext context)
    {
        print(context.ReadValue<float>());
        selectWeaponSlot((int)Mathf.Sign(context.ReadValue<float>()));
        //selectWeaponSlot(context.ReadValue<int>());
    }

    private void scrollSelection(InputAction.CallbackContext context)
    {
        print("Still need to add mouse scrolling");
        print(context);
    }

    private void selectWeaponSlot(int amount)
    {
        characterData.EquippedWeaponScrollSelection(amount);
    }
    #endregion

    #region Weapon Usage
    private void useWeapon(InputAction.CallbackContext context)
    {
        characterData.UseCurrentWeapon();
    }
    private void releaseWeapon(InputAction.CallbackContext context)
    {
        characterData.releaseCurrentWeapon();
    }
    #endregion
}
