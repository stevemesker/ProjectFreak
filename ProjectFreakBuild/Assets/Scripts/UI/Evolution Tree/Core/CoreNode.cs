using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using TMPro;

//the core on the 2D rune field. It's a view now: the RuneFieldManager uses it as the center of the field and tells it how much power is left
public class CoreNode : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional text that shows the core's power as left / total. Leave empty if the core has no label")]
    [SerializeField] TextMeshProUGUI _PowerLabel;

    [Header("Runtime Data")]
    [Tooltip("Total power the core has, from the shade's level (read only)")]
    [ReadOnly] public int CoreNodeMaxPower;
    [Tooltip("Power no rune is using yet (read only)")]
    [ReadOnly] public int CoreNodeCurrentPower;

    #region Display
    public void ShowPower(int powerLeft, int maxPower)
    {
        //function the rune field calls after every change
        CoreNodeCurrentPower = powerLeft;
        CoreNodeMaxPower = maxPower;
        if (_PowerLabel != null) _PowerLabel.text = $"{powerLeft} / {maxPower}";
    }
    #endregion
}
