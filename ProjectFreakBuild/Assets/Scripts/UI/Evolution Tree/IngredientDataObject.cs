using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

public class IngredientDataObject : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public TextMeshProUGUI IngredientNamePointer;
    public Image IngredientIconPointer;
    public IngredientItem Item;
    public int itemAmount;

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
        throw new System.NotImplementedException();
    }

    public void OnDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}
