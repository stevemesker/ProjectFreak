using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Sirenix.OdinInspector;

//the spotlight that follows the rune field camera's focus (the Diablo-style pool of light)
//goes on the spot light, which sits under the RuneFieldCameraObject so it moves with the focus
//widens the light's cone as the camera zooms out, so the lit pool covers more of the field
//the pool is set by its size on the floor (meters), and the cone angle is worked out from how high the light is
[DefaultExecutionOrder(120)] //after the camera rig places itself and updates its zoom, so the light uses this frame's zoom
[RequireComponent(typeof(Light))]
public class RuneFieldFocusLightObject : MonoBehaviour
{
    const float MinSpotAngle = 1f; //HDRP spot lights can't go below 1 degree
    const float MaxSpotAngle = 179f; //or past 179 degrees
    const float MinHeight = 0.01f; //a light this close to (or below) the field can't make a pool

    [Header("Settings")]
    [Tooltip("How wide the lit pool on the floor is when zoomed all the way in, in meters (across the whole circle)")]
    [SerializeField, Min(0.1f)] float _PoolSizeZoomedIn = 17f;

    [Tooltip("How wide the lit pool on the floor is when zoomed all the way out, in meters (across the whole circle)")]
    [SerializeField, Min(0.1f)] float _PoolSizeZoomedOut = 40f;

    [Tooltip("How the pool grows between zoomed in (left, 0) and zoomed out (right, 1). Up the left side keeps it small longer, up the right side grows it sooner")]
    [SerializeField] AnimationCurve _PoolGrowth = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Extra range past the floor, in meters. The light's range is set so it always reaches the edge of the pool, plus this")]
    [SerializeField, Min(0f)] float _ExtraRange = 5f;

    [Header("References")]
    [Tooltip("The field camera, for its zoom and focus point. Left empty, the one this light sits under is used")]
    [SerializeField] RuneFieldCameraObject _CameraRig;

    [Header("Runtime Data")]
    [Tooltip("How high the light is above the camera's focus right now, in meters")]
    [SerializeField, ReadOnly] float _CurrentHeight;

    [Tooltip("The light's cone angle right now, in degrees")]
    [SerializeField, ReadOnly] float _CurrentAngle;

    [FoldoutGroup("Tools")]
    [Tooltip("Edit mode: sets the light as if the camera were at this zoom (0 = zoomed in, 1 = zoomed out), to tune the pool sizes without pressing play")]
    [SerializeField, Range(0f, 1f), OnValueChanged("PreviewZoom")] float _PreviewZoom = 0.8f;

    //local variables
    Light _light;
    HDAdditionalLightData _lightData;

    private void Start()
    {
        //finds what it needs once
        if (FindReferences() == false) enabled = false;
    }

    private void LateUpdate()
    {
        //resizes the pool to match the camera's zoom every frame
        ApplyZoom(_CameraRig.GetZoom());
    }

    #region Initialize
    bool FindReferences()
    {
        //grabs the light, its HDRP data and the camera rig. Returns false if something required is missing
        if (_light == null) _light = GetComponent<Light>();
        if (_lightData == null) _lightData = GetComponent<HDAdditionalLightData>();
        if (_CameraRig == null) _CameraRig = GetComponentInParent<RuneFieldCameraObject>(); //looks up through the parents, so placing the light under the rig is all the setup

        if (_light.type != LightType.Spot) { Debug.LogError($"Error! {gameObject.name} needs to be a spot light for the focus light", this); return false; }
        if (_lightData == null) { Debug.LogError($"Error! No HDRP light data on {gameObject.name}", this); return false; }
        if (_CameraRig == null) { Debug.LogError($"Error! {gameObject.name} isn't under a RuneFieldCameraObject and none is assigned", this); return false; }
        return true;
    }
    #endregion

    #region Pool Size
    void ApplyZoom(float zoom)
    {
        //sets the cone angle and range so the pool on the floor is the right size for this zoom
        _CurrentHeight = GetHeightAboveFocus();
        if (_CurrentHeight < MinHeight) return; //the light is at or below the field, there's no pool to size

        float growth = _PoolGrowth.Evaluate(Mathf.Clamp01(zoom));
        float poolSize = Mathf.LerpUnclamped(_PoolSizeZoomedIn, _PoolSizeZoomedOut, growth); //unclamped so the curve can overshoot on purpose
        _CurrentAngle = GetConeAngle(poolSize * 0.5f, _CurrentHeight);

        //SetSpotAngle is HDRP's way of changing the cone. The second value keeps the soft edge (Inner Angle %) as it was set in the inspector
        _lightData.SetSpotAngle(_CurrentAngle, _lightData.innerSpotPercent);

        //distance from the light to the edge of the pool, so the light's range never cuts the pool short
        float edgeDistance = _CurrentHeight / Mathf.Cos(_CurrentAngle * 0.5f * Mathf.Deg2Rad);
        _light.range = edgeDistance + _ExtraRange;
    }

    float GetHeightAboveFocus()
    {
        //how far above the camera's focus point the light is, measured along the rig's up direction
        Vector3 offset = transform.position - _CameraRig.transform.position;
        return Vector3.Dot(offset, _CameraRig.transform.up); //Dot gives how much of the offset points "up", ignoring any sideways part
    }

    float GetConeAngle(float poolRadius, float height)
    {
        //the full cone angle (in degrees) that lights a circle this wide on the floor from this height
        //Atan2 gives the angle from straight down out to the pool's edge. The cone is twice that, one side each way
        float halfAngle = Mathf.Atan2(poolRadius, height) * Mathf.Rad2Deg;
        return Mathf.Clamp(halfAngle * 2f, MinSpotAngle, MaxSpotAngle);
    }
    #endregion

    #region Tools
    void PreviewZoom()
    {
        //edit mode only: sizes the light for Preview Zoom, to check the pool without playing
        if (Application.isPlaying) return;
        if (FindReferences() == false) return;

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(_light, "Preview Focus Light Zoom"); //so Ctrl+Z puts the light back
#endif
        ApplyZoom(_PreviewZoom);
    }

    private void OnValidate()
    {
        //warns in the editor if the light can't make a pool from where it is
        if (_CameraRig == null) return;
        if (GetHeightAboveFocus() < MinHeight) Debug.LogWarning($"Warning! {gameObject.name} is at or below the camera's focus, so the focus light can't size its pool. Move it up", this);
    }
    #endregion
}
