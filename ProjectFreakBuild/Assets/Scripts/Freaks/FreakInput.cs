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
        pInput.Freak.FreakWeaponSelect.performed += SwitchSelection;
        pInput.Freak.FreakWeaponScroll.performed += ScrollSelection;
        pInput.Freak.FreakWeaponActivation.performed += UseWeapon;
        pInput.Freak.FreakWeaponActivation.canceled += ReleaseWeapon;
    }

    private void OnDisable()
    {
        pInput.Freak.FreakWeaponSelect.performed -= SwitchSelection;
        pInput.Freak.FreakWeaponScroll.performed -= ScrollSelection;
        pInput.Freak.FreakWeaponActivation.performed -= UseWeapon;
        pInput.Freak.FreakWeaponActivation.canceled -= ReleaseWeapon;
        pInput.Disable();
    }

    #region WeaponSelecting

    private void SwitchSelection(InputAction.CallbackContext context)
    {
        print(context.ReadValue<float>());
        SelectWeaponSlot((int)Mathf.Sign(context.ReadValue<float>()));
        //SelectWeaponSlot(context.ReadValue<int>());
    }

    private void ScrollSelection(InputAction.CallbackContext context)
    {
        print("Still need to add mouse scrolling");
        print(context);
    }

    private void SelectWeaponSlot(int amount)
    {
        characterData.EquippedWeaponScrollSelection(amount);
    }
    #endregion

    #region Weapon Usage
    private void UseWeapon(InputAction.CallbackContext context)
    {
        characterData.UseCurrentWeapon();
    }
    private void ReleaseWeapon(InputAction.CallbackContext context)
    {
        characterData.ReleaseCurrentWeapon();
    }
    #endregion
}
