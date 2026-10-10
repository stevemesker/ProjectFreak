using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//the core on the 3D rune field. It's a view: RuneField3DObject moves it to the field's center and tells it how much power is left
//the power label (left / total, above the orb) comes in step 6 with the other labels
public class Core3DObject : MonoBehaviour
{
    [Header("Runtime Data")]
    [Tooltip("Total power the core has, from the shade's level and fragment (read only)")]
    [SerializeField, ReadOnly] int _MaxPower;

    [Tooltip("Power no rune is using yet (read only)")]
    [SerializeField, ReadOnly] int _PowerLeft;

    #region Display
    public void ShowPower(int powerLeft, int maxPower)
    {
        //function the field calls after every change
        _PowerLeft = powerLeft;
        _MaxPower = maxPower;
    }

    public int GetPowerLeft() { return _PowerLeft; }
    public int GetMaxPower() { return _MaxPower; }
    #endregion
}
