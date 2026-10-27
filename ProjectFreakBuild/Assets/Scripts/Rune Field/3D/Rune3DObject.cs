using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//one rune on the 3D rune field. It's a view: RuneField3DObject spawns it, places it and tells it how to look
//it's colored by its element, dimmed without power, brighter under the pointer, tinted while frozen and while dragged over a spot it can't go
public class Rune3DObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The material's color name: _BaseColor for HDRP Lit, _UnlitColor for HDRP Unlit")]
    [SerializeField] string _ColorProperty = "_BaseColor";

    [Tooltip("How bright the rune is while it has no power (0 = black, 1 = full color)")]
    [SerializeField, Range(0f, 1f)] float _UnpoweredBrightness = 0.35f;

    [Tooltip("Color mixed in while the rune is frozen (sitting in a zone the shade evolved out of)")]
    [SerializeField] Color _FrozenColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Tooltip("How much of Frozen Color is mixed in (0 = none, 1 = all)")]
    [SerializeField, Range(0f, 1f)] float _FrozenBlend = 0.35f;

    [Tooltip("How much brighter the rune gets while the pointer is on it (1 = no change)")]
    [SerializeField, Min(1f)] float _HoverBrightness = 1.35f;

    [Tooltip("The shader number the range highlight is sent to (0 = not lit, 1 = a rune being placed would bridge to this one). Sent to the rune's renderers and the Range Effect's. Shaders without it ignore it")]
    [SerializeField] string _RangeHighlightProperty = "_RangeHighlight";

    [Header("References")]
    [Tooltip("The renderers that get colored. Left empty, every renderer on the rune is used (except the Range Effect's)")]
    [SerializeField] List<Renderer> _TintRenderers = new List<Renderer>();

    [Tooltip("Optional. The big magic effect shown while a rune being placed is close (turned on while the highlight is above 0). Left empty, a child named RangeEffect is used if there is one")]
    [SerializeField] GameObject _RangeEffect;

    [Header("Runtime Data")]
    [Tooltip("This rune's ID on the field (read only)")]
    [SerializeField, ReadOnly] int _RuneID = -2;

    [Tooltip("This rune's element (read only)")]
    [SerializeField, ReadOnly] ElementItemSO _Element;

    [Tooltip("Power this rune needs (read only)")]
    [SerializeField, ReadOnly] int _RequiredPower;

    [Tooltip("Does it have power right now (read only)")]
    [SerializeField, ReadOnly] bool _Powered;

    [Tooltip("How lit up it is by the range overlay right now, 0 to 1 (read only)")]
    [SerializeField, ReadOnly] float _RangeHighlight;

    //local variables
    const string RangeEffectName = "RangeEffect"; //the child used for Range Effect when nothing is assigned
    RuneFieldTint _tint;
    RuneFieldTint _effectTint; //the Range Effect's renderers, so they get the highlight number without the rune's color
    int _rangeHighlightID;
    Color _elementColor = Color.white;
    bool _frozen;
    bool _invalid;
    bool _hovered;
    Color _invalidColor = Color.red;

    private void Awake()
    {
        if (_RangeEffect == null) _RangeEffect = RuneFieldTint.FindChild(transform, RangeEffectName);
        Transform effect = _RangeEffect != null ? _RangeEffect.transform : null; //the effect keeps its own look, it isn't colored like the rune

        List<Renderer> renderers = _TintRenderers.Count > 0 ? _TintRenderers : RuneFieldTint.FindRenderers(transform, effect);
        _tint = new RuneFieldTint(renderers, _ColorProperty);
        _rangeHighlightID = Shader.PropertyToID(string.IsNullOrEmpty(_RangeHighlightProperty) ? "_RangeHighlight" : _RangeHighlightProperty);

        if (_RangeEffect != null)
        {
            _effectTint = new RuneFieldTint(RuneFieldTint.FindRenderers(effect, null), _ColorProperty);
            _RangeEffect.SetActive(false);
        }
    }

    #region Initialize
    public void Setup(int runeID, ElementItemSO element, Color elementColor)
    {
        //function the field calls right after spawning this rune
        _RuneID = runeID;
        _Element = element;
        _elementColor = elementColor;
        ApplyColor();
    }
    #endregion

    #region Getters
    public int GetRuneID() { return _RuneID; }
    public ElementItemSO GetElement() { return _Element; }
    public int GetRequiredPower() { return _RequiredPower; }
    public bool IsPowered() { return _Powered; }
    #endregion

    #region Display
    public void ShowPower(bool powered, int powerNeeded)
    {
        //shows whether the rune has power (dimmed when it doesn't)
        _RequiredPower = powerNeeded;
        _Powered = powered;
        ApplyColor();
    }

    public void ShowFrozen(bool frozen)
    {
        //shows the rune can't be moved anymore (its zone is frozen)
        _frozen = frozen;
        ApplyColor();
    }

    public void ShowHover(bool hovered)
    {
        //brightens the rune while the pointer is on it, so it's clear what a click will grab
        _hovered = hovered;
        ApplyColor();
    }

    public void ShowInvalid(bool invalid, Color invalidColor)
    {
        //tints the rune while it's dragged over a spot it can't go
        _invalid = invalid;
        _invalidColor = invalidColor;
        ApplyColor();
    }

    public void ShowRangeHighlight(float intensity)
    {
        //function the range overlay calls while a rune is being placed nearby: 0 = not lit, 1 = it would bridge to this rune
        //sends the number to the rune's shader and the Range Effect's, and only shows the effect while it's above 0
        _RangeHighlight = intensity;
        if (_tint != null) _tint.SetFloat(_rangeHighlightID, intensity);

        if (_RangeEffect == null) return;
        bool show = intensity > 0f;
        if (_RangeEffect.activeSelf != show) _RangeEffect.SetActive(show);
        if (_effectTint != null) _effectTint.SetFloat(_rangeHighlightID, intensity);
    }

    void ApplyColor()
    {
        //works the final color out from every look at once, so they never undo each other
        if (_tint == null) return;
        if (_invalid) { _tint.SetColor(_invalidColor); return; }

        Color color = _elementColor;
        if (_frozen) color = Color.Lerp(color, _FrozenColor, _FrozenBlend);
        float brightness = _Powered ? 1f : _UnpoweredBrightness;
        if (_hovered) brightness *= _HoverBrightness;
        color = new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a);
        _tint.SetColor(color);
    }
    #endregion
}
