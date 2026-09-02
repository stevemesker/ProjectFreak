using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_POI_DungeonName_Size_Type", menuName = "Dungeon/POI", order = 0)]
public class DungeonPOISO : ScriptableObject
{
    public GameObject _POI_Prefab;
    public POIType.Size _POI_Size;
    public List<POIType.Tag> _POI_Tags;

    [Button("Fill tags with all")]
    void fillList()
    {
        //function that fills the poi with all of the tags because sometimes adding them takes longer than deleteing them...
        _POI_Tags.Clear();
        _POI_Tags.AddRange((POIType.Tag[])Enum.GetValues(typeof(POIType.Tag)));
    }
    [Button("Clear all tags")]
    void clearList()
    {
        _POI_Tags.Clear();
    }
}
