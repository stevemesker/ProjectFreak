using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_Dungeon_Name", menuName = "Dungeon/Dungeon", order = 0)]
public class DungeonSO : ScriptableObject
{
    [Header ("Dungeon Data")]
    public string _DungeonName;
    public int _DungeonColumnCount = 3;
    public int _DungeonRowCount = 10;
    public float _DungeonMapNodeWiggle = 20.5f;

    [Header ("Scene Lists")]
    public string _DungeonEntranceSceneName = "SCN_DungeonEntrance_0";
    public string _DungeonBossSceneName = "SCN_DungeonBossRoom_0";
    public List<string> _DungeonFloorList;
    public List<DungeonPOISO> POIList;

    [Header("Floor Types")]
    public int _DungeonFloorPoolSize = 36;
    public List<floorPoolColumn> _DungeonFloorPoolTypes;

    [Header("Tables")]
    public DungeonLootTableSO LootTable;
    public DungeonEnemyTableSO EnemyTable;

    #region Data Fill
    [Header("Temp")]
    [Tooltip("Used for batch loading scenes into _Dungeon Floor List. Be sure to clear out...")]
    public List<Object> _SceneAdd;

    [Button("Fill Floor List")]
    public void FillScenes()
    {
        _DungeonFloorList.Clear();
        for (int i = 0; i < _SceneAdd.Count; i++)
        {
            _DungeonFloorList.Add(_SceneAdd[i].name);
        }
        _SceneAdd.Clear();
    }

    [Button("Clear All Lists")]
    public void EmptyScenes()
    {
        _DungeonFloorList.Clear();
        POIList.Clear();
    }
    #endregion
}

[System.Serializable]
public class floorPoolColumn
{
    public List<floorPoolEntry> Entry;
}

[System.Serializable]
public class floorPoolEntry
{
    public POIType.Type _EntryType;
    public float _EntryChace;
}
