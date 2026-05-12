using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class IngredientListWindow : MonoBehaviour
{
    public GameObject ListHolderPointer;
    public GameObject ButtonListPrefab;

    [SerializeField, Tooltip("Current List of button prefabs. Used to access their data to compare to the master list in inventory manager")]
    private List<GameObject> currentListAssets;

    private void OnEnable()
    {
        if (InventoryManager._PlayerInventory == null)
        {
            Debug.LogWarning("Warning! No Item List Update Event was found! Maybe the game manager doesn't exist or this is firing off before the game manager declairs the event");
            return;
        }
            
        InventoryManager._PlayerInventory.OnInventoryChanged += UpdateList;
    }

    private void OnDisable()
    {
        InventoryManager._PlayerInventory.OnInventoryChanged -= UpdateList;
    }

    [Button("Update List")]
    public void UpdateList()
    {
        //pull in the manager's source of truth for ingredients
        var ingredients = InventoryManager._PlayerInventory.Ingredients;

        if (ingredients.Count < currentListAssets.Count) removeUnusedBoxes(currentListAssets.Count - ingredients.Count);
        if (ingredients.Count > currentListAssets.Count) spawnMoreButtons(ingredients.Count - currentListAssets.Count);

        int index = 0;
        foreach (var entry in InventoryManager._PlayerInventory.Ingredients)
        {
            //if (currentListAssets.Count < i) currentListAssets.Add(Instantiate(ButtonListPrefab, ListHolderPointer.transform));
            //currentListAssets[i].GetComponent<IngredientDataObject>().FillData(ingredients[i])
            currentListAssets[index].SetActive(true);
            currentListAssets[index].GetComponent<IngredientDataObject>().FillData(entry.Key, entry.Value);
            index++;
        }
    }

    private void spawnMoreButtons(int amount)
    {
        print("Make " + amount + "more boxes");
        for (int i = 0; i<amount; i++)
        {
            currentListAssets.Add(Instantiate(ButtonListPrefab, ListHolderPointer.transform));
        }
    }

    private void removeUnusedBoxes(int amount)
    {
        print("Too many boxes, deleting " + amount);
        for (int i = 0; i < amount; i++)
        {
            currentListAssets[currentListAssets.Count -1 - i].SetActive(false);
        }
    }







    //var ingredients = InventoryManager._PlayerInventory.Ingredients;
    [Button("Test")]
    public void test()
    {
        var ingredients = InventoryManager._PlayerInventory.Ingredients;
        print("Found ingredient data count" + ingredients.Count);
    }
}
