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

    [Header("References")]
    [Tooltip("The renderers that get colored. Left empty, every renderer on the rune is used")]
    [SerializeField] List<Renderer> _TintRenderers = new List<Renderer>();

    [Header("Runtime Data")]
    [Tooltip("This rune's ID on the field (read only)")]
    [SerializeField, ReadOnly] int _RuneID = -2;

    [Tooltip("This rune's element (read only)")]
    [SerializeField, ReadOnly] ElementItemSO _Element;

    [Tooltip("Power this rune needs (read only)")]
    [SerializeField, ReadOnly] int _RequiredPower;

    [Tooltip("Does it have power right now (read only)")]
    [SerializeField, ReadOnly] bool _Powered;

    //local variables
    RuneFieldTint _tint;
    Color _elementColor = Color.white;
    bool _frozen;
    bool _invalid;
    bool _hovered;
    Color _invalidColor = Color.red;

    private void Awake()
    {
        List<Renderer> renderers = _TintRenderers.Count > 0 ? _TintRenderers : RuneFieldTint.FindRenderers(transform, null);
        _tint = new RuneFieldTint(renderers, _ColorProperty);
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
