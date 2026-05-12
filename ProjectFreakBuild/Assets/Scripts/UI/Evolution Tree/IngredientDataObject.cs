using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class IngredientDataObject : MonoBehaviour
{
    public TextMeshProUGUI IngredientNamePointer;
    public Image IngredientIconPointer;
    public IngredientItem Item;

    [Button("Activate Item Test")]
    public void FillData()
    {
        //function that fills out the data within the button object
        IngredientNamePointer.text = Item.ItemName;
        IngredientIconPointer.sprite = Item.itemSprite;
    }
}
