using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

//one rune in the rune field's inventory list. Shows the rune, how many the player has, and how many the draft field is using
//it greys out (and can't be dragged) when none are left to place, but stays in the list so it can still be looked at
public class ElementDataObject : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("<====Pointer Variables=====>")]
    public TextMeshProUGUI ElementNamePointer;
    public Image ElementIconPointer;
    [SerializeField, Tooltip("Old: not used anymore, the rune field spawns runes itself")] private GameObject NodePrefabToSpawn;

    [Tooltip("Text showing how many of this rune the inventory has. Press Build Count Labels (in prefab mode) to make one")]
    [SerializeField] TextMeshProUGUI _CountText;

    [Tooltip("Text showing how many the draft field is using on top of the saved field. Hidden when it's using none")]
    [SerializeField] TextMeshProUGUI _PendingText;

    [Tooltip("The image tinted while this button is dragged over a spot on the field where the rune can't go. Grabbed from this object if left empty")]
    [SerializeField] Graphic _TintTarget;

    [Header("Settings")]
    [Tooltip("Color of the inventory count")]
    [SerializeField] Color _CountColor = Color.black;

    [Tooltip("Color of the draft's count, brighter so it stands out")]
    [SerializeField] Color _PendingColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Tooltip("How the draft's count is written. {0} is replaced with the number")]
    [SerializeField] string _PendingFormat = "-{0}";

    [Tooltip("How see-through the button is when none are left to place (0 = invisible, 1 = solid)")]
    [SerializeField, Range(0f, 1f)] float _UnavailableAlpha = 0.4f;

    [Header("<====Current Data=====>")]
    public ElementItemSO Item;
    public int itemAmount;
    [Tooltip("How many the draft field is using on top of the saved field (read only)")]
    [SerializeField, ReadOnly] int _PendingAmount;
    [Tooltip("How many can still be placed (read only)")]
    [SerializeField, ReadOnly] int _AvailableAmount;

    //hidden private variables
    private Vector2 _startPosition;
    CanvasGroup _canvasGroup; //fades the whole button. Added automatically if the prefab doesn't have one
    bool _dragging; //true only for a drag that was allowed to start
    Color _normalTint = Color.white; //the tint target's color from the prefab, put back after a drag

    private void Awake()
    {
        if (TryGetComponent(out _canvasGroup) == false) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (_TintTarget == null) _TintTarget = GetComponent<Graphic>(); //Graphic is the parent of Image, RawImage and text, so any of them works
        if (_TintTarget != null) _normalTint = _TintTarget.color;
    }

    [Button("Activate Item Test")]
    public void FillData(ElementItemSO item, int amount, int pending, int available)
    {
        //function that fills out the button: the rune, the inventory count, the draft's count, and whether any are left to place
        Item = item;
        itemAmount = amount;
        _PendingAmount = pending;
        _AvailableAmount = available;
        if (item == null) return;

        if (ElementNamePointer != null) ElementNamePointer.text = item.ItemName;
        if (ElementIconPointer != null) ElementIconPointer.sprite = item.itemSprite;

        if (_CountText != null)
        {
            _CountText.text = amount.ToString();
            _CountText.color = _CountColor;
        }

        if (_PendingText != null)
        {
            _PendingText.gameObject.SetActive(pending > 0); //only shown while the draft is using some
            _PendingText.text = string.Format(_PendingFormat, pending); //puts the number where {0} is
            _PendingText.color = _PendingColor;
        }

        if (_canvasGroup != null) _canvasGroup.alpha = available > 0 ? 1f : _UnavailableAlpha;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //only lets the drag start if there's one left to place
        _dragging = _AvailableAmount > 0;
        _startPosition = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //follows the pointer, and tints red over a spot on the field where the rune can't go
        if (_dragging == false) return;
        transform.position = eventData.position;

        RuneFieldManager runeField = FindRuneFieldUnderPointer(eventData);
        bool blocked = runeField != null && runeField.CanPlaceFromInventory(Item, eventData.position) == false;
        if (_TintTarget != null) _TintTarget.color = blocked ? runeField.GetInvalidColor() : _normalTint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //function that drops the element onto the rune field if the pointer is over it, then snaps the button back to the list
        if (_dragging == false) return;
        _dragging = false;
        if (_TintTarget != null) _TintTarget.color = _normalTint;

        RuneFieldManager runeField = FindRuneFieldUnderPointer(eventData);
        if (runeField != null) runeField.PlaceRuneFromInventory(Item, eventData.position); //the rune field makes the rune object itself, with this element's data

        transform.position = _startPosition;
    }

    RuneFieldManager FindRuneFieldUnderPointer(PointerEventData eventData)
    {
        //returns the rune field under the pointer (the object tagged UI Drag Field), or null
        if (EventSystem.current == null) return null;
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, hits);

        var hit = hits.FirstOrDefault(t => t.gameObject.CompareTag("UI Drag Field")); //the first hit with that tag, or an empty result if none
        if (hit.isValid && hit.gameObject.TryGetComponent(out RuneFieldManager runeField)) return runeField;
        return null;
    }

    #region Tools
    [Button("Build Count Labels"), GUIColor(0.4f, 1f, 0.4f)]
    void BuildCountLabels()
    {
        //editor button that adds the count and draft-count texts on the right side of the button. Open the prefab (prefab mode) first, then press this
        //move and restyle them however you like afterwards
        if (_CountText == null) _CountText = MakeLabel("Count", new Vector2(-72f, 0f), 28f);
        if (_PendingText == null) _PendingText = MakeLabel("Pending", new Vector2(-8f, 0f), 22f);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); //tells the editor this changed, so it gets saved
#endif
    }

    TextMeshProUGUI MakeLabel(string labelName, Vector2 offsetFromRight, float fontSize)
    {
        //makes a small text anchored to the right edge of this button
        GameObject labelObject = new GameObject(labelName, typeof(RectTransform));
        labelObject.transform.SetParent(transform, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f); //anchored to the middle of the right edge
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = offsetFromRight;
        rect.sizeDelta = new Vector2(60f, 40f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "0";
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Right;
        label.raycastTarget = false; //so the text doesn't block dragging the button

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(labelObject, "Build Count Labels"); //Ctrl+Z removes it again
#endif
        return label;
    }
    #endregion
}
