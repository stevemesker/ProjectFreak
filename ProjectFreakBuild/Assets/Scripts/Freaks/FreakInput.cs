using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreakInput : MonoBehaviour
{
    [Tooltip("References the freak character class to access inventory")]
    public FreakCharacter characterData;
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
    }

    private void OnDisable()
    {
        pInput.Freak.FreakWeaponSelect.performed -= switchSelection;
        pInput.Freak.FreakWeaponScroll.performed -= scrollSelection;
        pInput.Disable();
    }

    private void switchSelection(InputAction.CallbackContext context)
    {
        print(context.ReadValue<float>());
        selectWeaponSlot((int)Mathf.Sign(context.ReadValue<float>()));
        //selectWeaponSlot(context.ReadValue<int>());
    }

    private void scrollSelection(InputAction.CallbackContext context)
    {
        print(context);
    }

    private void selectWeaponSlot(int amount)
    {
        characterData.EquippedWeaponScrollSelection(amount);
    }
}
