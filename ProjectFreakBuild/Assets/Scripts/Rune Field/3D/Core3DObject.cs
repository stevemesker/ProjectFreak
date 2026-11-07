using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX; //VisualEffect, the component that plays a VFX Graph
using Sirenix.OdinInspector;

//the core on the 3D rune field. It's a view: RuneField3DObject moves it to the field's center and tells it how much power is left
//its power label (left / total, above the orb) is drawn by RuneFieldLabelsObject
//while a rune is waiting for power (too many runes for the core's power), the core ramps its depleted value up from 0 to 1
//and sends it to the orb shader and the orb's VFX Graph, which do the dulling and blinking. It ramps back down once there's enough power
//spare power (the glow) isn't sent, it's on hold until playtesting shows it's needed
public class Core3DObject : MonoBehaviour
{
    [Header("Settings")]
    [FoldoutGroup("Depleted")]
    [Tooltip("How long the depleted value takes to go from 0 to 1 once a rune is waiting for power, in seconds")]
    [SerializeField, Min(0f)] float _DepletedRampUpTime = 0.6f;

    [FoldoutGroup("Depleted")]
    [Tooltip("How long the depleted value takes to go from 1 back to 0 once every rune has power, in seconds")]
    [SerializeField, Min(0f)] float _DepletedRampDownTime = 0.4f;

    [FoldoutGroup("Depleted")]
    [Tooltip("The shape of the ramp (0 = fine, 1 = fully depleted). The same curve is used going up and coming back down")]
    [SerializeField] AnimationCurve _DepletedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The shader number the range highlight is sent to (0 = not lit, 1 = a rune being placed would bridge to the core). Sent to the core's renderers and the Range Effect's. Shaders without it ignore it")]
    [SerializeField] string _RangeHighlightProperty = "_RangeHighlight";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The orb shader's depleted number (its Reference name in Shader Graph, 0 = fine, 1 = depleted). Sent to the core's renderers. Shaders without it ignore it")]
    [SerializeField] string _DepletedProperty = "_DepletedValue";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The orb VFX Graph's exposed depleted number (the name in its Blackboard, 0 = fine, 1 = depleted)")]
    [SerializeField] string _VFXDepletedProperty = "DepletedValue";

    [Header("References")]
    [Tooltip("Optional. The magic effect shown while a rune being placed is close to the core (turned on while the highlight is above 0). Left empty, a child named RangeEffect is used if there is one")]
    [SerializeField] GameObject _RangeEffect;

    [Tooltip("Optional. The orb's VFX Graph that gets the depleted value. Left empty, the first VisualEffect in the core's children is used")]
    [SerializeField] VisualEffect _OrbVFX;

    [Header("Runtime Data")]
    [Tooltip("How lit up the core is by the range overlay right now, 0 to 1 (read only)")]
    [SerializeField, ReadOnly] float _RangeHighlight;

    [Tooltip("Total power the core has, from the shade's level and fragment (read only)")]
    [SerializeField, ReadOnly] int _MaxPower;

    [Tooltip("Power no rune is using yet (read only)")]
    [SerializeField, ReadOnly] int _PowerLeft;

    [Tooltip("True while the core can't afford a rune that's bridged to it (too many runes for its power). Runes that aren't bridged to the core and planning runes past the open zone don't count (read only)")]
    [SerializeField, ReadOnly] bool _RunesWaiting;

    [Tooltip("The depleted value being sent right now, after the curve, 0 to 1 (read only)")]
    [SerializeField, ReadOnly] float _DepletedValue;

    //local variables
    const string RangeEffectName = "RangeEffect"; //the child used for Range Effect when nothing is assigned
    RuneFieldTint _tint; //the core's renderers (not the Range Effect's), used to send the highlight and depleted numbers
    RuneFieldTint _effectTint;
    int _rangeHighlightID;
    int _depletedID;
    int _vfxDepletedID;
    bool _vfxHasDepleted; //false if the VFX Graph has no exposed depleted number, so it's skipped instead of erroring every frame
    float _depletedProgress; //how far along the ramp the core is: 0 fine, 1 depleted. The curve turns this into _DepletedValue
    Coroutine _depletedRamp;

    private void Awake()
    {
        if (_RangeEffect == null) _RangeEffect = RuneFieldTint.FindChild(transform, RangeEffectName);
        Transform effect = _RangeEffect != null ? _RangeEffect.transform : null;

        _tint = new RuneFieldTint(RuneFieldTint.FindRenderers(transform, effect), "_BaseColor"); //the color isn't used here, only SetFloat
        _rangeHighlightID = Shader.PropertyToID(string.IsNullOrEmpty(_RangeHighlightProperty) ? "_RangeHighlight" : _RangeHighlightProperty);

        if (_RangeEffect != null)
        {
            _effectTint = new RuneFieldTint(RuneFieldTint.FindRenderers(effect, null), "_BaseColor");
            _RangeEffect.SetActive(false);
        }

        SetupDepleted();
    }

    private void OnDisable()
    {
        //stops the ramp part way and jumps to where it was heading, so a core switched off mid-ramp isn't left half depleted
        StopDepletedRamp();
        PlaceDepleted();
    }

    #region Display
    public void ShowPower(int powerLeft, int maxPower, bool runesWaiting, bool animate)
    {
        //function the field calls after every change
        //animate is false when a saved field first opens, so the core snaps to its look instead of ramping
        bool waitingChanged = runesWaiting != _RunesWaiting;
        _PowerLeft = powerLeft;
        _MaxPower = maxPower;
        _RunesWaiting = runesWaiting;

        if (animate == false) { StopDepletedRamp(); PlaceDepleted(); return; }
        if (waitingChanged) RampDepleted(); //called on every refresh (even mid-drag), so the ramp only restarts when waiting actually changes
    }

    public void ShowRangeHighlight(float intensity)
    {
        //function the range overlay calls while a rune is being placed nearby: 0 = not lit, 1 = it would bridge to the core
        _RangeHighlight = intensity;
        if (_tint != null) _tint.SetFloat(_rangeHighlightID, intensity);

        if (_RangeEffect == null) return;
        bool show = intensity > 0f;
        if (_RangeEffect.activeSelf != show) _RangeEffect.SetActive(show);
        if (_effectTint != null) _effectTint.SetFloat(_rangeHighlightID, intensity);
    }

    public int GetPowerLeft() { return _PowerLeft; }
    public int GetMaxPower() { return _MaxPower; }
    public bool HasRunesWaiting() { return _RunesWaiting; }
    #endregion

    #region Depleted
    void SetupDepleted()
    {
        //finds the orb's VFX Graph (if not assigned), turns the property names into IDs and sends the starting value
        if (_OrbVFX == null) _OrbVFX = GetComponentInChildren<VisualEffect>(true); //true includes switched off children
        _depletedID = Shader.PropertyToID(string.IsNullOrEmpty(_DepletedProperty) ? "_DepletedValue" : _DepletedProperty);
        _vfxDepletedID = Shader.PropertyToID(string.IsNullOrEmpty(_VFXDepletedProperty) ? "DepletedValue" : _VFXDepletedProperty); //VFX Graph uses the same kind of ID as shaders

        if (_OrbVFX == null) Debug.LogWarning($"Warning! No VisualEffect found on {gameObject.name}, the depleted value only goes to the orb shader...", this);
        else
        {
            _vfxHasDepleted = _OrbVFX.HasFloat(_vfxDepletedID); //checks the VFX Graph actually has an exposed float with that name
            if (_vfxHasDepleted == false) Debug.LogWarning($"Warning! {_OrbVFX.gameObject.name}'s VFX Graph has no exposed float named {_VFXDepletedProperty}, the depleted value only goes to the orb shader...", this);
        }

        PlaceDepleted();
    }

    void RampDepleted()
    {
        //starts the depleted value moving toward its target from wherever it is now, so a change mid-ramp turns around smoothly
        StopDepletedRamp();
        float rampTime = _RunesWaiting ? _DepletedRampUpTime : _DepletedRampDownTime;
        if (isActiveAndEnabled == false || rampTime <= 0f) { PlaceDepleted(); return; } //coroutines can't run on a switched off object
        _depletedRamp = StartCoroutine(DepletedRamp(rampTime));
    }

    IEnumerator DepletedRamp(float rampTime)
    {
        //moves the ramp's progress toward its target at a steady speed, sending the curved value every frame
        //unscaledDeltaTime keeps it moving while the game is paused under the rune field (deltaTime would be 0)
        //MoveTowards steps by a fixed amount without passing the target, so turning around half way takes half the time
        float target = _RunesWaiting ? 1f : 0f;
        while (_depletedProgress != target)
        {
            _depletedProgress = Mathf.MoveTowards(_depletedProgress, target, Time.unscaledDeltaTime / rampTime);
            SendDepleted();
            yield return null;
        }
        _depletedRamp = null;
    }

    void PlaceDepleted()
    {
        //puts the depleted value straight at its target, no ramp
        _depletedProgress = _RunesWaiting ? 1f : 0f;
        SendDepleted();
    }

    void SendDepleted()
    {
        //turns the ramp's progress into the value through the curve and sends it to the orb shader and the VFX Graph
        _DepletedValue = _DepletedCurve.Evaluate(_depletedProgress);
        if (_tint != null) _tint.SetFloat(_depletedID, _DepletedValue);
        if (_OrbVFX != null && _vfxHasDepleted) _OrbVFX.SetFloat(_vfxDepletedID, _DepletedValue);
    }

    void StopDepletedRamp()
    {
        //stops the ramp if one is running
        if (_depletedRamp != null) StopCoroutine(_depletedRamp);
        _depletedRamp = null;
    }
    #endregion

    #region Test Tools
    [FoldoutGroup("Depleted"), Button("Test Depleted (Play Mode)")]
    void TestDepleted()
    {
        //play mode button: flips depleted on / off with its ramp, for tuning the times and curve. The next field refresh puts it back
        if (Application.isPlaying == false) { Debug.LogWarning("Warning! Test Depleted only works in play mode, nothing changed...", this); return; }
        ShowPower(_PowerLeft, _MaxPower, _RunesWaiting == false, true);
    }
    #endregion
}
