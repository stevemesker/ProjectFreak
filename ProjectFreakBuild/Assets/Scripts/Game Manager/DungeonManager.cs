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

    //local variables
    DungeonMapManager _map;

    void Start()
    {
        if (_DM == null) _DM = this;
    }

    public void EnterDungeon(int dungeonID)
    {
        SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        SceneManagerObject._SceneManager.changeScene(_DungeonChapterData[dungeonID].DungeonEntranceSceneName);
        
        if (_MapPrefab == null) Debug.LogError("Error! Map prefab not found!");

        _CurrentDungeonMap = Instantiate(_MapPrefab);
        _map = _CurrentDungeonMap.GetComponent<DungeonMapManager>();
        _map.StartNewMap(_DungeonChapterData[dungeonID]._DungeonData);
    }
}

[System.Serializable]
public class DungeonChapterData
{
    public int DungeonChapter;
    public string DungeonEntranceSceneName = "SCN_DungeonEntrance_0";
    public DungeonSO _DungeonData;
}