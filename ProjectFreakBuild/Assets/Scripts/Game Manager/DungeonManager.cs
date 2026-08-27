using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager _DM;

    [Header("Current Data")]
    public DungeonSO _CurrentDungeon;
    public int _CurrentRoomID;
    public GameObject _CurrentDungeonMap;

    [Header("Dungeon Chapter Settings")]
    public List<DungeonChapterData> _DungeonChapterData;

    [Header("Dungeon Settings")]
    public GameObject _MapPrefab;
    public GameObject _NodePrefab;
    public GameObject _BridgePrefab;

    //local variables
    DungeonMapManager _map;

    void Start()
    {
        if (_DM == null) _DM = this;
    }

    #region Dungeon Floor Changing
    public void EnterDungeon(int dungeonID)
    {
        if (_MapPrefab == null) Debug.LogError("Error! Map prefab not found!");
        _CurrentDungeon = _DungeonChapterData[dungeonID]._DungeonData;

        _CurrentRoomID = _CurrentDungeon._DungeonColumnCount * _CurrentDungeon._DungeonRowCount;

        _CurrentDungeonMap = Instantiate(_MapPrefab);
        _map = _CurrentDungeonMap.GetComponent<DungeonMapManager>();
        _map.StartNewMap(_CurrentDungeon);

        SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        SceneManagerObject._SceneManager.changeScene(_DungeonChapterData[dungeonID]._DungeonData._DungeonEntranceSceneName);
    }

    public void MoveToFloor(int floorID)
    {
        DungeonMapNode temp = getMapNode(floorID);
        string floorToEnter;
        if (temp._FloorSceneName == "")
        {
            print("Need to generate new floor...");
            floorToEnter = _CurrentDungeon._DungeonFloorList[Random.Range(0, _CurrentDungeon._DungeonFloorList.Count)];
        }
        else floorToEnter = temp._FloorSceneName;
        _CurrentRoomID = floorID;

        SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        SceneManagerObject._SceneManager.changeScene(floorToEnter);
    }

    #endregion

    #region Tools
    public DungeonMapNode getMapNode(int ID)
    {
        if (ID >= _map._FloorNodes.Count) return _map._FloorNodes[_map._FloorNodes.Count - 1].GetComponent<DungeonMapNode>();
        return _map._FloorNodes[ID].GetComponent<DungeonMapNode>();
    }

    public int getCurrentDungeonFloorID()
    {
        return _CurrentRoomID;
    }

    #endregion
}

[System.Serializable]
public class DungeonChapterData
{
    public int DungeonChapter;
    public DungeonSO _DungeonData;
}