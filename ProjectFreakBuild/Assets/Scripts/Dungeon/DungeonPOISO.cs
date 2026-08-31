using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_POI_DungeonName_Size_Type", menuName = "Dungeon/POI", order = 0)]
public class DungeonPOISO : ScriptableObject
{
    public GameObject _POI_Prefab;
    public POIType.Size _POI_Size;
    public List<POIType.Tag> _POI_Tags;

    //public Vector2Int _POI_Dimension;
    //public POIShape.Shape _POI_Shape;
    //public DungeonLootTableSO _POI_LootTable;
    //public DungeonEnemyTableSO _POI_EnemyTable;
}
