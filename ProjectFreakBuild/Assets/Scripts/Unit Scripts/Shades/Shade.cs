using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Shade : MonoBehaviour
{
    //the current shade on the field
    public static Shade shade;

    [Header("Data")]
    public ShadeSO _shadeSlotData;
    public ShadeEvolutionSO _shadeEvoData;

    [FoldoutGroup("Pointers")]
    [Header("Script pointers")]
    public CharacterMovement _movement;

    private void OnEnable()
    {
        if (Shade.shade == null) Shade.shade = this;
    }

    #region Control Shade

    public void EnablePlayerControl()
    {
        _movement.EnableMovement();
    }

    public void EnableShadeControl()
    {
        _movement.DisableMovement();
    }
    #endregion
}
