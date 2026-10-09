using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

public class ElementDataObject : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("<====Pointer Variables=====>")]
    public TextMeshProUGUI ElementNamePointer;
    public Image ElementIconPointer;
    [SerializeField, Tooltip("Old: not used anymore, the rune field spawns runes itself")] private GameObject NodePrefabToSpawn;

    [Header("<====Current Data=====>")]
    public ElementItemSO Item;
    public int itemAmount;

    //hidden private variables
    private Vector2 _startPosition;

    [Button("Activate Item Test")]
    public void FillData(ElementItemSO item, int amount)
    {
        //function that fills out the data within the button object
        Item = item;
        ElementNamePointer.text = item.ItemName;
        ElementIconPointer.sprite = item.itemSprite;
        itemAmount = amount;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _startPosition = transform.position;

    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //function that drops the element onto the rune field if the pointer is over it, then snaps the button back to the list
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, hits);

        var hit = hits.FirstOrDefault(t => t.gameObject.CompareTag("UI Drag Field"));
        if (hit.isValid && hit.gameObject.TryGetComponent(out RuneFieldManager runeField))
            runeField.PlaceRuneFromInventory(Item, eventData.position); //the rune field makes the rune object itself, with this element's data

        transform.position = _startPosition;

    }
}
