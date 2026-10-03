using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Dev tool. Put it on an empty object standing on the floor to see a melee weapon's swing shapes in the Scene view,
/// without pressing play. The object's position is the wielder's feet and its blue arrow (forward) is the aim.
///
////////////////////////////////////////////////
public class SwingShapePreviewObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The weapon to preview. Its reach multiplier is applied to the shape")]
    [SerializeField] MeleeWeaponItem _Weapon;

    [Tooltip("Which hit of the combo to show, counting from 0")]
    [SerializeField, Min(0)] int _ComboStep;

    [Tooltip("Optional: preview a swing shape on its own instead of the weapon's combo (uses 1× reach)")]
    [SerializeField] SwingShapeSO _ShapeOverride;

    [Header("Settings")]
    [Tooltip("How far through the active time to show the sweep, 0 = start, 1 = end. Drag it to watch the swing grow")]
    [SerializeField, Range(0f, 1f)] float _SweepTime = 1f;

    [Tooltip("Show every hit of the combo at once, in a faint color, around the selected one")]
    [SerializeField] bool _ShowWholeCombo;

    //local variables
    static readonly Color FullColor = new Color(1f, 1f, 1f, 0.3f);
    static readonly Color SweptColor = new Color(1f, 0.3f, 0.2f, 1f);
    static readonly Color OtherStepColor = new Color(0.3f, 0.7f, 1f, 0.35f);

    #region Debugging
    private void OnDrawGizmos()
    {
        //draws the chosen swing shape from this object's position and facing
        if (_ShapeOverride != null)
        {
            _ShapeOverride.DrawGizmo(transform.position, transform.forward, 1f, _ShapeOverride.GetSweepAmount(_SweepTime), FullColor, SweptColor);
            return;
        }

        if (_Weapon == null || _Weapon._Combo == null || _Weapon._Combo.Count == 0) return;

        if (_ShowWholeCombo)
        {
            for (int i = 0; i < _Weapon._Combo.Count; i++)
            {
                if (i == _ComboStep || _Weapon._Combo[i] == null || _Weapon._Combo[i]._Swing == null) continue;
                _Weapon._Combo[i]._Swing.DrawGizmo(transform.position, transform.forward, _Weapon._Reach, 1f, OtherStepColor, OtherStepColor);
            }
        }

        if (_ComboStep >= _Weapon._Combo.Count) return; //picked a hit the combo doesn't have
        ComboStepEntry step = _Weapon._Combo[_ComboStep];
        if (step == null || step._Swing == null) return;
        step._Swing.DrawGizmo(transform.position, transform.forward, _Weapon._Reach, step._Swing.GetSweepAmount(_SweepTime), FullColor, SweptColor);
    }
    #endregion
}
