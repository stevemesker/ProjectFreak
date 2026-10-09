using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

public class IngredientDataObject : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("<====Pointer Variables=====>")]
    public TextMeshProUGUI IngredientNamePointer;
    public Image IngredientIconPointer;
    [SerializeField, Tooltip("Old: not used anymore, the rune field spawns runes itself")] private GameObject NodePrefabToSpawn;

    [Header("<====Current Data=====>")]
    public IngredientItem Item;
    public int itemAmount;

    //hidden private variables
    private Vector2 _startPosition;

    [Button("Activate Item Test")]
    public void FillData(IngredientItem item, int amount)
    {
        //function that fills out the data within the button object
        Item = item;
        IngredientNamePointer.text = item.ItemName;
        IngredientIconPointer.sprite = item.itemSprite;
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
        //ingredients aren't runes, so dropping one on the rune field does nothing now (it used to spawn an empty rune). Snaps back to the list
        transform.position = _startPosition;

    }
}
