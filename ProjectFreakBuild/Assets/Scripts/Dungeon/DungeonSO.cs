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
    
    [Header("Floor Types")]
    public int _DungeonFloorPoolSize = 36;
    public List<floorPoolColumn> _DungeonFloorPoolTypes;

    [Header("POI Data")]
    public List<DungeonPOISO> POIList;
    public TypeTagLookUpSO POILUT;

    [Header("Tables")]
    public DungeonLootTableSO LootTable;
    public DungeonEnemyTableSO EnemyTable;

    //local/unserialized
    public Dictionary<POIType.Size, List<DungeonPOISO>> _POISizeDictionary;
    public Dictionary<POIType.Tag, List<DungeonPOISO>> _POITagDictionary;

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

    public void BuildPOIDictionaries()
    {
        _POISizeDictionary = new Dictionary<POIType.Size, List<DungeonPOISO>>();
        _POITagDictionary = new Dictionary<POIType.Tag, List<DungeonPOISO>>();

        for (int i = 0; i < POIList.Count; i++)
        {
            //adding poi to dictionary by size
            Debug.Log($"Now adding {POIList[i].name} to dictionaries. It has {POIList[i]._POI_Tags.Count} tags associated with it...");
            if (!_POISizeDictionary.TryGetValue(POIList[i]._POI_Size, out List<DungeonPOISO> sizeList))
            {
                sizeList = new List<DungeonPOISO>();
                _POISizeDictionary.Add(POIList[i]._POI_Size, sizeList);
            }

            sizeList.Add(POIList[i]);

            //adding poi to dictionary by tags
            for (int j = 0; j < POIList[i]._POI_Tags.Count; j++)
            {
                if (!_POITagDictionary.TryGetValue(POIList[i]._POI_Tags[j], out List<DungeonPOISO> tagList))
                {
                    tagList = new List<DungeonPOISO>();
                    _POITagDictionary.Add(POIList[i]._POI_Tags[j], tagList);
                }
                if (!tagList.Contains(POIList[i]))
                {
                    tagList.Add(POIList[i]);
                }
            }
        }
    }

    public DungeonPOISO GetPOI(POIType.Type inputType, POIType.Size inputSize)
    {
        _POISizeDictionary.TryGetValue(inputSize, out List<DungeonPOISO> tempSizeList);
        for (int i = 0; i < POILUT._EntryPoolSize; i++)
        {
            POIType.Tag tempTag = POILUT.GetFloorTag(inputType);
            DungeonPOISO finalPOI = SearchDictionaries(tempSizeList, tempTag);
            if (finalPOI != null) return finalPOI;
        }

        Debug.LogError($"Error! Could not find POI that matches size: {inputSize} and any associated tags for type: {inputType} in look up table {POILUT.name}");
        return null;
    }

    DungeonPOISO SearchDictionaries (List<DungeonPOISO> listedPOIBySize, POIType.Tag tempTag)
    {
        //List <DungeonPOISO> listedPOISByTag = 
        _POITagDictionary.TryGetValue(tempTag, out List<DungeonPOISO> listedPOIByTag);
        List<DungeonPOISO> finalList = new List<DungeonPOISO>();

        for (int i = 0; i < listedPOIBySize.Count; i++)
        {
            for (int j = 0; j < listedPOIByTag.Count; j++)
            {
                if (listedPOIByTag[j] == listedPOIBySize[i]) finalList.Add(listedPOIByTag[j]);
            }
        }
        if (finalList.Count == 0) return null;

        return finalList[Random.Range(0, finalList.Count)];
    }
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

