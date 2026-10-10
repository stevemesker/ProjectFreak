using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

//one rune on the 2D rune field. It's a view now: it shows the rune and passes the player's drags to the RuneFieldManager,
//which runs them through the RuneField rules
public class ElementItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [Tooltip("How see-through the rune is while it has no power (0 = invisible, 1 = solid)")]
    [SerializeField, Range(0f, 1f)] float _UnpoweredAlpha = 0.45f;

    [Header("References")]
    [Tooltip("The rune field this rune sits on. Grabbed from the parent if left empty")]
    public RectTransform RuneFieldTransform;
    [SerializeField, Tooltip("Bridge prefab the rune field spawns for bridges")]
    private GameObject BridgePrefabRef;
    [Tooltip("The image tinted when the rune is dragged over a spot it can't go. Grabbed from this object if left empty")]
    [SerializeField] Graphic _TintTarget;

    [Header("Runtime Data")]
    [Tooltip("This rune's element (read only)")]
    [SerializeField, ReadOnly] ElementItemSO _ElementAttached;
    [Tooltip("This rune's ID on the field (read only)")]
    [SerializeField, ReadOnly] int _RuneID = -2;
    [Tooltip("Power this rune needs (read only)")]
    [ReadOnly] public int RequiredPower;
    [Tooltip("Power this rune has right now. It's all or nothing, so either 0 or Required Power (read only)")]
    [ReadOnly] public int CurrentPower;

    //local variables
    RuneFieldManager _runeField;
    CanvasGroup _canvasGroup; //fades the rune while it has no power. Added automatically if the prefab doesn't have one
    Color _normalTint = Color.white; //the tint target's color from the prefab, put back when the rune isn't over a bad spot

    void Awake()
    {
        if (RuneFieldTransform == null) RuneFieldTransform = gameObject.transform.parent.GetComponent<RectTransform>();

        if (TryGetComponent(out _canvasGroup) == false) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (_TintTarget == null) _TintTarget = GetComponent<Graphic>(); //Graphic is the parent of Image, RawImage and text, so any of them works
        if (_TintTarget != null) _normalTint = _TintTarget.color;
    }

    #region Initialize
    public void Setup(RuneFieldManager runeField, int runeID, ElementItemSO element)
    {
        //function the rune field calls right after spawning this rune
        _runeField = runeField;
        _RuneID = runeID;
        _ElementAttached = element;
    }
    #endregion

    #region Getters
    public int GetRuneID()
    {
        return _RuneID;
    }

    public ElementItemSO GetElementSOAttachment()
    {
        return _ElementAttached;
    }

    public GameObject GetBridgePrefab()
    {
        return BridgePrefabRef;
    }
    #endregion

    #region Display
    public void ShowPower(bool powered, int powerNeeded)
    {
        //shows whether the rune has power (faded when it doesn't)
        RequiredPower = powerNeeded;
        CurrentPower = powered ? powerNeeded : 0;
        if (_canvasGroup != null) _canvasGroup.alpha = powered ? 1f : _UnpoweredAlpha;
    }

    public void ShowInvalid(bool invalid, Color invalidColor)
    {
        //tints the rune while it's dragged over a spot it can't go, and puts its normal color back otherwise
        if (_TintTarget == null) return;
        _TintTarget.color = invalid ? invalidColor : _normalTint;
    }
    #endregion

    #region Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_runeField == null) return; //placed by hand, not part of a field
        _runeField.BeginRuneDrag(_RuneID);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_runeField == null) return;
        _runeField.DragRune(_RuneID, eventData.position); //eventData.position works for the mouse and any pointer the event system supports
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_runeField == null) return;
        _runeField.EndRuneDrag(_RuneID, eventData); //eventData lets the field check if the rune was dropped on the inventory list
    }
    #endregion
}
