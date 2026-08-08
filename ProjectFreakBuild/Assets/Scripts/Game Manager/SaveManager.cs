using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] int _ActiveSaveSlotIndex;
    public List<SaveSlotSO> _saveSlotList;

    public void LoadSaveDataToSlot()
    {
        Debug.Log("Load save file data here...");
    }

    public int getCurrentActiveSaveSlot ()
    {
        return _ActiveSaveSlotIndex;
    }
}
