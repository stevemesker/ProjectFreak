using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_NewItem", menuName = "ScriptableObjects/Items/Ingredient", order = 0)]

public class IngredientItem : ItemSO
{
    [TitleGroup("===Ingredient Base Data===")]
    public Sprite itemSprite;
}
