using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class IngredientListWindow : MonoBehaviour
{
    public GameObject ListHolderPointer;
    public GameObject RuneFieldPointer;
    public GameObject ButtonListPrefab;

    [SerializeField, Tooltip("Current List of button prefabs. Used to access their data to compare to the master list in inventory manager")]
    private List<GameObject> currentListAssets;

    //local variables
    RuneFieldManager _runeField; //read from RuneFieldPointer. Gives each button how many of its rune the draft is using

    private void OnEnable()
    {
        //listens for inventory changes and draft changes, then fills the list straight away
        if (RuneFieldPointer != null) _runeField = RuneFieldPointer.GetComponent<RuneFieldManager>();
        if (_runeField != null) _runeField.OnDraftChanged += UpdateList;
        else Debug.LogWarning($"Warning! No RuneFieldManager on Rune Field Pointer for {gameObject.name}, the list won't show how many runes the field is using...", this);

        if (InventoryManager._PlayerInventory == null)
        {
            Debug.LogWarning("Warning! No Item List Update Event was found! Maybe the game manager doesn't exist or this is firing off before the game manager declairs the event");
            return;
        }
            
        InventoryManager._PlayerInventory.OnInventoryChanged += UpdateList;
        UpdateList();
    }

    private void OnDisable()
    {
        //stops listening. Both are checked first, since either can be missing when testing a scene on its own
        if (_runeField != null) _runeField.OnDraftChanged -= UpdateList;
        if (InventoryManager._PlayerInventory != null) InventoryManager._PlayerInventory.OnInventoryChanged -= UpdateList;
    }

    [Button("Update List")]
    public void UpdateList()
    {
        //function that rebuilds the rune list: one button per rune in the inventory, with its count, how many the draft is using, and how many are left to place
        if (InventoryManager._PlayerInventory == null) return;

        //pull in the manager's source of truth for elements
        var elements = InventoryManager._PlayerInventory.Elements;

        if (elements.Count < currentListAssets.Count) RemoveUnusedBoxes(currentListAssets.Count - elements.Count);
        if (elements.Count > currentListAssets.Count) SpawnMoreButtons(elements.Count - currentListAssets.Count);

        int index = 0;
        foreach (var entry in InventoryManager._PlayerInventory.Elements)
        {
            //if (currentListAssets.Count < i) currentListAssets.Add(Instantiate(ButtonListPrefab, ListHolderPointer.transform));
            //currentListAssets[i].GetComponent<IngredientDataObject>().FillData(ingredients[i])
            currentListAssets[index].SetActive(true);
            if (currentListAssets[index].TryGetComponent(out ElementDataObject button))
            {
                int pending = _runeField != null ? _runeField.GetPendingUse(entry.Key) : 0; //"? :" picks the left value if there's a rune field, 0 if not
                int available = _runeField != null ? _runeField.GetAvailable(entry.Key) : entry.Value;
                button.FillData(entry.Key, entry.Value, pending, available);
            }
            index++;
        }

        /*
        //pull in the manager's source of truth for ingredients
        var ingredients = InventoryManager._PlayerInventory.Ingredients;

        if (ingredients.Count < currentListAssets.Count) RemoveUnusedBoxes(currentListAssets.Count - ingredients.Count);
        if (ingredients.Count > currentListAssets.Count) SpawnMoreButtons(ingredients.Count - currentListAssets.Count);

        int index = 0;
        foreach (var entry in InventoryManager._PlayerInventory.Ingredients)
        {
            //if (currentListAssets.Count < i) currentListAssets.Add(Instantiate(ButtonListPrefab, ListHolderPointer.transform));
            //currentListAssets[i].GetComponent<IngredientDataObject>().FillData(ingredients[i])
            currentListAssets[index].SetActive(true);
            currentListAssets[index].GetComponent<IngredientDataObject>().FillData(entry.Key, entry.Value);
            index++;
        }*/
    }

    private void SpawnMoreButtons(int amount)
    {
        for (int i = 0; i<amount; i++)
        {
            currentListAssets.Add(Instantiate(ButtonListPrefab, ListHolderPointer.transform));
        }
    }

    private void RemoveUnusedBoxes(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            currentListAssets[currentListAssets.Count -1 - i].SetActive(false);
        }
    }







    //var ingredients = InventoryManager._PlayerInventory.Ingredients;
    [Button("Test")]
    public void Test()
    {
        var ingredients = InventoryManager._PlayerInventory.Ingredients;
        print("Found ingredient data count" + ingredients.Count);
    }
}
