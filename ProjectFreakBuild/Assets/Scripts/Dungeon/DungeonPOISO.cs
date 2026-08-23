using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_POI_WxH_Shape_type", menuName = "Dungeon/POI", order = 0)]
public class DungeonPOISO : ScriptableObject
{
    public GameObject _POI_Prefab;
    public POIType.Type _POI_Type;
    public Vector2Int _POI_Dimension;
    public POIShape.Shape _POI_Shape;
    public DungeonLootTableSO _POI_LootTable;
    public DungeonEnemyTableSO _POI_EnemyTable;
}
