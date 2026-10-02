using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class POISpawnerObject : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] POIType.Size _SpawnerSize;

    [Header("Debug")]
    [SerializeField] DungeonPOISO _CurrentPOI;
    [SerializeField] GameObject _SpawnedPOI;

    void Start()
    {
        SetVolumeActive(false);

        //if this floor has a DungeonFloorObject, register with it and let it call SpawnPOI() at the right time
        //otherwise spawn right away like before, so scenes without a floor object still work
        //(Start is used instead of Awake/OnEnable because every Awake in the scene has run by now, so the floor singleton is already set)
        if (DungeonFloorObject._Floor != null && DungeonFloorObject._Floor.gameObject.scene == gameObject.scene) //same scene check so we never register with a floor left over from the last scene
        {
            DungeonFloorObject._Floor.RegisterPOISpawner(this);
            return;
        }
        SpawnPOI();
    }

    #region Spawning
    public void SpawnPOI()
    {
        //function that asks the Dungeon Manager for a POI that fits this spot and spawns it here
        //will probably want a catch here for loading POIs on floors the player has already visited...
        if (_SpawnedPOI != null) return; //already spawned, don't double up
        if (DungeonManager._DM == null) { Debug.LogWarning($"Warning! No Dungeon Manager found for {gameObject.name}, spawning nothing...", this); return; }
        if (DungeonManager._DM._CurrentDungeon == null) { Debug.LogWarning($"Warning! Not inside a dungeon so {gameObject.name} has no POIs to pick from, spawning nothing...", this); return; } //happens when playing a floor scene directly

        _CurrentPOI = DungeonManager._DM.GetPOIFromCurrentRoom(_SpawnerSize);

        if (_CurrentPOI == null)
        {
            Debug.LogWarning($"Warning! No POI found for {gameObject.name}, spawning nothing...", this);
            return;
        }

        _SpawnedPOI = Instantiate(_CurrentPOI._POI_Prefab, transform.position, Quaternion.identity);
    }
    #endregion

    #region Tools

    [FoldoutGroup("Settings")]
    [SerializeField] GameObject _SizeVolume;
    [FoldoutGroup("Settings")]
    [SerializeField, Tooltip("Standard size (in meters) of a cell in a poi")] int _CellSizePerMeter = 4;
    [FoldoutGroup("Settings")]
    [SerializeField, Tooltip("How many cells wide the POI is for each size category, starting with Small at index 0")] List<int> _ScaleFactorByIndex;

    [Button("UpdateSizeVolume")]
    [GUIColor(0f, 1f, 0f)]
    public void UpdateSizeVolume()
    {
        if (_SizeVolume == null) { Debug.LogError("Error! No size volume has been assigned to POI Spawner"); return; }
        
        int sizeIndex = (int)_SpawnerSize;

        if (sizeIndex < 0 || sizeIndex >= _ScaleFactorByIndex.Count)
        {
            Debug.LogError($"No scale factor exists for size category: {_SpawnerSize}");
            _SizeVolume.transform.localScale =
            Vector3.one;
            return;
        }

        int scaleFactor = _ScaleFactorByIndex[sizeIndex];
        Vector3 finalscale = new Vector3(_CellSizePerMeter * scaleFactor, 4, _CellSizePerMeter * scaleFactor);

        _SizeVolume.transform.localScale = finalscale;
            //Vector3.one * _CellSizePerMeter * scaleFactor;
    }

    [Button("ToggleVolume")]
    [GUIColor("#ff9000")]
    public void ToggleVolume()
    {
        if (_SizeVolume == null) { Debug.LogError("Error! No size volume has been assigned to POI Spawner"); return; }
        _SizeVolume.SetActive(!_SizeVolume.activeSelf);
    }

    public void SetVolumeActive(bool state)
    {
        if (_SizeVolume == null) return;
        _SizeVolume.SetActive(state);
    }
    #endregion
}
