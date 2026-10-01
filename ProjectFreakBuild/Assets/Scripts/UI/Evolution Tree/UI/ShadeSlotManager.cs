using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShadeSlotManager : MonoBehaviour
{
    [SerializeField] List<GameObject> slotList;
    [SerializeField] RuneFieldManager _runeField;

    private void OnEnable()
    {
        if (GameManager._GameManager == null) return;

        CheckList();
    }
    public void CheckList()
    {
        //function that makes sure the list of enabled slots matches the player's shade slot number

        List<ShadeSO> temp = GameManager._GameManager.GetShadeList();
        for (int i = 0; i < slotList.Count; i++)
        {
            //check player level to make sure you can click this slot
            if (i >= GameManager._GameManager.GetPlayerCurrentLevel())
                slotList[i].SetActive(false);
            else
                slotList[i].SetActive(true);
        }
    }

    public void LoadShadeSelectionIndex(GameObject origin)
    {
        if (GameManager._GameManager == null) return;
        if (slotList.Contains(origin) == false) { Debug.LogError("Error loading shade! Button " + origin.name + " is trying to load a shade but is not referenced in slotList"); return; }
        int index = slotList.IndexOf(origin);
        
        //may need to add some catch for loading a rune field that's already selected

        _runeField.LoadRuneField(index);
        /*
        if (GameManager._GameManager.GetComponent<ShadeManager>().GetShadeSelectionIndex() == index)
        {
            //print("already selected this shade...");
            return;
        }

        _runeField.ClearRuneField();
        
        SelectShadeSlot(index);
        _runeField.LoadRuneFieldFromPackage(GameManager._GameManager.GetComponent<ShadeManager>()._ShadeSlots[index]);*/
    }

    public void SelectShadeSlot(int index)
    {
        if (ShadeManager._ShadeManager == null) return;
        ShadeManager._ShadeManager.SetShadeSelection(index);
    }
}
