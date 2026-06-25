using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class PlayerCombatInteract : MonoBehaviour
{
    private PlayerInput pInput;

    private void Awake()
    {
        pInput = new PlayerInput();
    }

    private void OnEnable()
    {
        pInput.Enable();
        pInput.Player.WeaponSelect.performed += switchSelection;
    }

    #region Weapon Selecting
    [Button("Mod test")]
    public int modulusTester(int input)
    {
        return input % Player.player.pData.pInventory._EquipmentSize;
    }
    void switchSelection(InputAction.CallbackContext context)
    {
        //print((int)Mathf.Sign(context.ReadValue<float>()));
        Player.player.setActiveWeapon(Player.player.getActiveWeaponIndex() + (int)Mathf.Sign(context.ReadValue<float>()));
    }

    #endregion
}
