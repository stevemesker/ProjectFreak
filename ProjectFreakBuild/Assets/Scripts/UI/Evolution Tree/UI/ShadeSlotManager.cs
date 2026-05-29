using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ShadeSlotManager : MonoBehaviour
{
    [SerializeField] List<GameObject> slotList;

    private void OnEnable()
    {
        if (GameManager._GameManager == null) return;

        checkList();
    }
    public void checkList()
    {
        //function that makes sure the list of enabled slots matches the player's shade slot number

        List<ShadeSO> temp = GameManager._GameManager.getShadeList();
        for (int i = 0; i < slotList.Count; i++)
        {
            //check player level to make sure you can click this slot

            //else
            slotList[i].SetActive(true);
        }
    }

    public void loadShadeSlectionIndex(GameObject origin)
    {
        if (slotList.Contains(origin) == false) { Debug.LogError("Error loading shade! Button " + origin.name + " is trying to load a shade but is not referenced in slotList"); return; }
        print("Loading shade in slot " + slotList.IndexOf(origin));
    }
}
