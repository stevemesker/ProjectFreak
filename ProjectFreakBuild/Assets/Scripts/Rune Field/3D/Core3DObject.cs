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
    [FoldoutGroup("Shader Properties")]
    [Tooltip("The shader number the range highlight is sent to (0 = not lit, 1 = a rune being placed would bridge to the core). Sent to the core's renderers and the Range Effect's. Shaders without it ignore it")]
    [SerializeField] string _RangeHighlightProperty = "_RangeHighlight";

    [Header("References")]
    [Tooltip("Optional. The magic effect shown while a rune being placed is close to the core (turned on while the highlight is above 0). Left empty, a child named RangeEffect is used if there is one")]
    [SerializeField] GameObject _RangeEffect;

    [Header("Runtime Data")]
    [Tooltip("How lit up the core is by the range overlay right now, 0 to 1 (read only)")]
    [SerializeField, ReadOnly] float _RangeHighlight;

    [Tooltip("Total power the core has, from the shade's level and fragment (read only)")]
    [SerializeField, ReadOnly] int _MaxPower;

    [Tooltip("Power no rune is using yet (read only)")]
    [SerializeField, ReadOnly] int _PowerLeft;

    [Tooltip("True while a rune that could have power doesn't (the core can't afford it, or it isn't bridged to the core). Planning runes past the open zone don't count (read only)")]
    [SerializeField, ReadOnly] bool _RunesWaiting;

    //local variables
    const string RangeEffectName = "RangeEffect"; //the child used for Range Effect when nothing is assigned
    RuneFieldTint _tint; //the core's renderers (not the Range Effect's), only used to send the highlight number
    RuneFieldTint _effectTint;
    int _rangeHighlightID;

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
    }

    #region Display
    public void ShowPower(int powerLeft, int maxPower, bool runesWaiting)
    {
        //function the field calls after every change
        _PowerLeft = powerLeft;
        _MaxPower = maxPower;
        _RunesWaiting = runesWaiting;
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
}
