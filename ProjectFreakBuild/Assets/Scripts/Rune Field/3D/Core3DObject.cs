using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//the core on the 3D rune field. It's a view: RuneField3DObject moves it to the field's center and tells it how much power is left
//its power label (left / total, above the orb) is drawn by RuneFieldLabelsObject
//it also keeps the state the core shader will read (Steve's shader): spare power, total power, and whether a rune is waiting for power
//(bright / normal / dull purple). The shader's property names become inspector fields here once it exists //todo
public class Core3DObject : MonoBehaviour
{
    [Header("Runtime Data")]
    [Tooltip("Total power the core has, from the shade's level and fragment (read only)")]
    [SerializeField, ReadOnly] int _MaxPower;

    [Tooltip("Power no rune is using yet (read only)")]
    [SerializeField, ReadOnly] int _PowerLeft;

    [Tooltip("True while a rune that could have power doesn't (the core can't afford it, or it isn't bridged to the core). Planning runes past the open zone don't count (read only)")]
    [SerializeField, ReadOnly] bool _RunesWaiting;

    #region Display
    public void ShowPower(int powerLeft, int maxPower, bool runesWaiting)
    {
        //function the field calls after every change
        _PowerLeft = powerLeft;
        _MaxPower = maxPower;
        _RunesWaiting = runesWaiting;
    }

    public int GetPowerLeft() { return _PowerLeft; }
    public int GetMaxPower() { return _MaxPower; }
    public bool HasRunesWaiting() { return _RunesWaiting; }
    #endregion
}
