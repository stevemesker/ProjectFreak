using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager _DM;

    [Header("Current Data")]
    public DungeonSO _CurrentDungeon;
    public int _CurrentRoomID;
    public DungeonMapNode _PreviousRoomNode;
    public GameObject _CurrentDungeonMap;
    public GameObject _CurrentMapLocator;

    [Header("Dungeon Chapter Settings")]
    public List<DungeonChapterData> _DungeonChapterData;

    [Header("Dungeon Settings")]
    public GameObject _MapPrefab;
    public GameObject _NodePrefab;
    public GameObject _BridgePrefab;
    public GameObject _LocatorPrefab;
    public DungeonTypeTranslatorSO _DungeonTypeTranslator;

    //local variables
    DungeonMapManager _map;
    private PlayerInput pInput;

    void Start()
    {
        if (_DM == null) _DM = this;
        BuildDungeonDictionaries();
    }

    private void OnEnable()
    {
        pInput = new PlayerInput();
        pInput.Enable();

        pInput.Player.OptionsMenu.performed += ToggleMap;
    }

    private void OnDisable()
    {
        pInput.Player.OptionsMenu.performed -= ToggleMap;
        pInput.Disable();
    }

    #region Dungeon Floor Changing
    public void EnterDungeon(int dungeonID)
    {
        if (_MapPrefab == null) Debug.LogError("Error! Map prefab not found!");
        _CurrentDungeon = _DungeonChapterData[dungeonID]._DungeonData;

        _CurrentRoomID = _CurrentDungeon._DungeonColumnCount * _CurrentDungeon._DungeonRowCount;

        _CurrentDungeonMap = Instantiate(_MapPrefab);
        _map = _CurrentDungeonMap.GetComponent<DungeonMapManager>();
        _map.StartNewMap(_CurrentDungeon);

        //released shades don't come into dungeons (a tethered shade does, it travels with the player)
        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.ReturnReleased();

        //SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        //SceneManagerObject._SceneManager.ChangeScene(_DungeonChapterData[dungeonID]._DungeonData._DungeonEntranceSceneName);
    }

    public void CompleteDungeon(string ReturnMap)
    {
        //add reward stuff here

        _CurrentDungeon = null;
        _CurrentRoomID = 0;
        Destroy(_CurrentDungeonMap);
        _map = null;

        //migth need a better system for returning to previous areas
        SceneManagerObject._SceneManager.ChangeScene(ReturnMap);
    }

    public void MoveToFloor(int floorID)
    {
        DungeonMapNode temp = GetMapNode(floorID);
        string floorToEnter;
        if (temp._FloorSceneName == "")
        {
            floorToEnter = _CurrentDungeon._DungeonFloorList[Random.Range(0, _CurrentDungeon._DungeonFloorList.Count)];
        }
        else floorToEnter = temp._FloorSceneName;
        _PreviousRoomNode = _CurrentDungeonMap.GetComponent<DungeonMapManager>()._FloorNodes[_CurrentRoomID].GetComponent<DungeonMapNode>();
        _CurrentRoomID = floorID;
        _CurrentMapLocator.transform.position = temp.gameObject.transform.position;

        SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        SceneManagerObject._SceneManager.ChangeScene(floorToEnter);
    }

    #endregion

    void ToggleMap(InputAction.CallbackContext context)
    {
        _CurrentDungeonMap.GetComponent<DungeonMapManager>().ToggleMap();
    }

    #region Tools
    public DungeonMapNode GetMapNode(int ID)
    {
        if (ID >= _map._FloorNodes.Count) return _map._FloorNodes[_map._FloorNodes.Count - 1].GetComponent<DungeonMapNode>();
        return _map._FloorNodes[ID].GetComponent<DungeonMapNode>();
    }

    public int GetCurrentDungeonFloorID()
    {
        return _CurrentRoomID;
    }

    public void SetDungeonLocator(GameObject target)
    {
        _CurrentMapLocator = target;
    }

    #endregion

    #region POI
    public void BuildDungeonDictionaries()
    {
        for (int i = 0; i < _DungeonChapterData.Count; i++)
        {
            _DungeonChapterData[i]._DungeonData.BuildPOIDictionaries();
        }
    }

    public DungeonPOISO GetPOIFromCurrentRoom(POIType.Size size)
    {
        return _CurrentDungeon.GetPOI(GetMapNode(_CurrentRoomID)._Type, size);
    }
    #endregion

    #region Enemies
    public DungeonEnemyTableSO GetCurrentEnemyTable()
    {
        //function that hands enemy spawners the current dungeon's enemy table. Null if not in a dungeon or the dungeon has no table
        if (_CurrentDungeon == null) return null;
        return _CurrentDungeon.EnemyTable;
    }

    public POIType.Type GetCurrentRoomType()
    {
        //function that returns the type of the floor the player is on (Basic, Vault, etc.). Enemy spawners use it for the table's count multipliers
        if (_map == null) { Debug.LogWarning("Warning! No dungeon map to read the room type from, using Basic...", this); return POIType.Type.Basic; }
        return GetMapNode(_CurrentRoomID)._Type;
    }
    #endregion
}

[System.Serializable]
public class DungeonChapterData
{
    public int DungeonChapter;
    public DungeonSO _DungeonData;
}