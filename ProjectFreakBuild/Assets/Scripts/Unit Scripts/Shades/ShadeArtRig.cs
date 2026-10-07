using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ShadeArtRig : MonoBehaviour
{
    //Goes on the root of every shade art prefab. Marks the points the shade scripts need (tail, casting)
    //and lists which parts to hide in each form, so one FBX can be used for both tethered and released

    [Header("References")]
    [Tooltip("Where the tether tail meets the body. Falls back to this object's position if left empty")]
    [SerializeField] Transform _TailConnectionPoint;

    [Tooltip("Where abilities come out of the shade. Falls back to this object's position if left empty")]
    [SerializeField] Transform _CastPoint;

    [Header("Settings")]
    [Tooltip("Child objects turned off while the shade is tethered (like the legs on a shade that uses one FBX for both forms)")]
    [SerializeField] List<GameObject> _HideWhenTethered = new List<GameObject>();

    [Tooltip("Child objects turned off while the shade is released")]
    [SerializeField] List<GameObject> _HideWhenReleased = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Gizmo size for the tail and cast points in the scene view, in meters")]
    [SerializeField, Min(0.01f)] float _GizmoSize = 0.15f;

    private void OnValidate()
    {
        //editor-only warnings for a rig that isn't filled out yet
        if (_TailConnectionPoint == null) Debug.LogWarning($"Warning! No Tail Connection Point on the ShadeArtRig of {gameObject.name}, the tail will connect to the art's root", this);
        if (_CastPoint == null) Debug.LogWarning($"Warning! No Cast Point on the ShadeArtRig of {gameObject.name}, abilities will come from the art's root", this);
    }

    #region Form
    public void ApplyForm(ShadeFormType.Form form)
    {
        //function that shows every part, then hides the parts this form doesn't use
        SetPartsActive(_HideWhenTethered, true);
        SetPartsActive(_HideWhenReleased, true);

        if (form == ShadeFormType.Form.Tethered) SetPartsActive(_HideWhenTethered, false);
        else if (form == ShadeFormType.Form.Released) SetPartsActive(_HideWhenReleased, false);
    }

    void SetPartsActive(List<GameObject> parts, bool active)
    {
        //turns a list of parts on or off, skipping empty entries
        foreach (GameObject part in parts)
        {
            if (part != null) part.SetActive(active);
        }
    }
    #endregion

    #region Tools
    public Transform GetTailConnectionPoint()
    {
        //function the tail uses to find where to attach. Falls back to the art's root
        if (_TailConnectionPoint == null) return transform;
        return _TailConnectionPoint;
    }

    public Transform GetCastPoint()
    {
        //function for where abilities come from. Falls back to the art's root
        if (_CastPoint == null) return transform;
        return _CastPoint;
    }
    #endregion

    #region Debugging
    private void OnDrawGizmosSelected()
    {
        //shows the tail point (cyan) and cast point (red) when the art is selected
        Gizmos.color = Color.cyan;
        if (_TailConnectionPoint != null) Gizmos.DrawWireSphere(_TailConnectionPoint.position, _GizmoSize);
        Gizmos.color = Color.red;
        if (_CastPoint != null) Gizmos.DrawWireSphere(_CastPoint.position, _GizmoSize);
    }
    #endregion
}
