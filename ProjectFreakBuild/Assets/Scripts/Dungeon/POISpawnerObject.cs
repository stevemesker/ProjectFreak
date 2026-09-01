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
        //will probably want a catch here for loading POIs on floors the player has already visited...

        _CurrentPOI = DungeonManager._DM.getPOIFromCurrentRoom();

        if (_CurrentPOI == null)
        {
            Debug.LogWarning($"No POI object found for {gameObject.name}...");
            return;
        }

        _SpawnedPOI = Instantiate(_CurrentPOI._POI_Prefab, transform.position, Quaternion.identity);
    }

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
