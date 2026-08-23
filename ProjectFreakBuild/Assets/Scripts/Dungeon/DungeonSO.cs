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
    public List<string> _DungeonFloorList;
    public List<DungeonPOISO> POIList;

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
