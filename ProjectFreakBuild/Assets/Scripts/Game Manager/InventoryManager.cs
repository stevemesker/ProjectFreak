using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

public class InventoryManager : MonoBehaviour
{
    //Static Controller//
    public static InventoryManager _PlayerInventory;

    //Global Variables
    [OdinSerialize] 
    //[DictionaryDrawerSettings(KeyLabel = "Ingredient", ValueLabel = "Amount")] 
    public Dictionary<IngredientItem, int> playerIngredients = new Dictionary<IngredientItem, int>();
    public int ItemStackSizeMax; //handles the maximum stack size any item can be

    private void Awake()
    {
        if (_PlayerInventory != null)
        {
            Debug.LogError("Warning! duplicate game managers have been detected in script " + this + " in game manager " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        _PlayerInventory = this;
    }

    [Button]
    public bool addIngredient (IngredientItem item, int amountAdded)
    {
        if (item == null || amountAdded < 1) return false; //checks to make sure inputs are valid (should probably do this earlier up the chain but works fine for now
        if (playerIngredients.TryGetValue(item, out int current))
        {
            print("Inventory slot exists, current count " + current);
            if ((current + amountAdded) > ItemStackSizeMax)
            {
                Debug.LogWarning("Warning! Maximum item amount has been surpassed for item " + item.ItemName);
                playerIngredients[item] = ItemStackSizeMax;
                return false;
            }
            print(" new inventory amount should be " + (current+amountAdded));
            playerIngredients[item] = current + amountAdded;
            print("Current count is " + playerIngredients[item]);
            return true;
        }
        else
        {
            if (amountAdded < 1)
            {
                Debug.LogError("Cannot add item " + item.ItemName + " due to inventory amount issues attempting to add " + amountAdded + " to a new list item.");
                return false;
            }
            if (amountAdded > ItemStackSizeMax) amountAdded = ItemStackSizeMax;
            print("New Inventory adding: " + item.ItemName + " X " + amountAdded);
            
            playerIngredients.Add(item, amountAdded);
            return true;
        }
    }

    [Button]
    public bool removeIngredient(IngredientItem item, int amountRemoved)
    {
        if (item == null || amountRemoved < 1) return false; //checks to make sure inputs are valid (should probably do this earlier up the chain but works fine for now
        if (playerIngredients.TryGetValue(item, out int current))
        {
            print("Inventory slot exists, current count " + current);
            if ((current - amountRemoved) <= 0)
            {
                print("Emptying out ingredient inventory");
                playerIngredients.Remove(item);
                return true;
            }
            playerIngredients[item] = playerIngredients[item] - amountRemoved;
            print("Removal Complete! Current amount: " + playerIngredients[item]);
            return true;
        }
        else
        {
            Debug.LogWarning("Warning! Attempting to remove " + item.ItemName + "But it was not found in player inventory.");
            return false;
        }
    }

    [Button]
    public int checkIngredient(IngredientItem item)
    {
        if (item == null) return 0;
        if (playerIngredients.TryGetValue(item, out int current))
        {
            return (current);
        }
            return 0;
    }
}


