using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_TagLookUpTable", menuName = "Dungeon/Dungeon Type Tag LUT")]

public class TypeTagLookUpSO : ScriptableObject
{
    public int _EntryPoolSize = 36;
    public List<POITypeEntry> _POIType;

    [Button("Clear All Pools")]
    public void ClearAllPools()
    {
        for (int i = 0; i < _POIType.Count; i++)
        {
            _POIType[i].ClearPool();
        }
    }
    public POIType.Tag GetFloorTag(POIType.Type inputType)
    {
        POITypeEntry temp = GetTagList(inputType);
        return GetTag(temp);
    }
    POITypeEntry GetTagList(POIType.Type floorType)
    {
        for (int i = 0; i < _POIType.Count; i++)
        {
            if (floorType == _POIType[i]._Type) return _POIType[i];
        }
        Debug.LogError($"Error! Can not find floor type {floorType} in file {this.name}. Defaulting to first entry...");
        return _POIType[0];
    }

    POIType.Tag GetTag (POITypeEntry Entry)
    {
        return Entry.GetPoolTag(_EntryPoolSize);
    }
}

[System.Serializable]
public class POITypeEntry
{
    public POIType.Type _Type;
    public List<TypeEntry> _Entry;

    [SerializeField]List<POIType.Tag> _EntryPool;

    public POIType.Tag GetPoolTag(int PoolSize)
    {
        if (_EntryPool.Count == 0) fillpool(PoolSize);
        int tempInt = Random.Range(0, _EntryPool.Count);
        POIType.Tag temp = _EntryPool[tempInt];
        _EntryPool.RemoveAt(tempInt);

        return temp;
    }

    public void fillpool(int PoolSize)
    {
        _EntryPool = new List<POIType.Tag>();
        int poolMin = PoolSize;
        float temp;
        int tempInt;

        for (int i = 0; i < _Entry.Count; i++)
        {
            temp = poolMin * (_Entry[i]._EntryChance / 100);
            tempInt = (int)temp;
            if (tempInt <= 0) tempInt = 1;

            //Debug.Log($"Now adding {(int)temp} notes of { _Entry[i]._Tag } to the pool");
            for (int j = 0; j < tempInt; j++)
            {
                _EntryPool.Add(_Entry[i]._Tag);
            }
        }
    }

    public void ClearPool()
    {
        if (_EntryPool != null)
            _EntryPool.Clear();
    }
}

[System.Serializable]
public class TypeEntry
{
    public POIType.Tag _Tag;
    public float _EntryChance;
}