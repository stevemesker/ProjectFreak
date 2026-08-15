using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [SerializeField] int _ActiveSaveSlotIndex;
    public List<SaveSlotSO> _saveSlotList;

    public static SaveManager _save;

    private void Awake()
    {
        if (_save == null) _save = this;
    }

    #region Set Data
    public SaveSlotSO getSaveSlotData()
    {
        return _saveSlotList[_ActiveSaveSlotIndex];
    }
    public void clearSave(int index)
    {
        _saveSlotList[index].ResetData();
        SaveDataAll(index);
    }

    #endregion

    #region Load Data
    public void LoadSaveDataToSlot()
    {
        Debug.Log("Load save file data here...");
    }

    #endregion

    #region Save Slot Tools
    public int getCurrentActiveSaveSlot ()
    {
        return _ActiveSaveSlotIndex;
    }

    public void setCurrentActiveSaveSlot(int SlotIndex)
    {
        SlotIndex = Mathf.Abs(SlotIndex);
        if (_saveSlotList.Count - 1 > SlotIndex)
        {
            Debug.LogError("Error: Save SLot Index outside of number available save slots. Changing selected index to last save slot");
            SlotIndex = _saveSlotList.Count - 1;
        }
        _ActiveSaveSlotIndex = SlotIndex;
    }
    #endregion

    #region Save Data
    //All the functionality for writting slot data to the save file
    public void SaveDataAll(int index)
    {
        //Runs a save for every category
        SaveDataGame(index);
        SaveDataPlayer(index);
        SaveDataShades(index);
        SaveDataScene(index);
    }

    public void SaveDataGame(int index)
    {
        //Runs a save for specifically Game Data
    }

    public void SaveDataPlayer(int index)
    {
        //Runs a save for specifically Player Data
    }

    public void SaveDataShades(int index)
    {
        //Runs a save for specifically Shade Data
    }

    public void SaveDataScene(int index)
    {
        //Runs a save for specifically Scene Data
    }
    #endregion
}
