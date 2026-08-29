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
    }

    private void OnEnable()
    {
        pInput = new PlayerInput();
        pInput.Enable();

        pInput.Player.OptionsMenu.performed += toggleMap;
    }

    private void OnDisable()
    {
        pInput.Player.OptionsMenu.performed -= toggleMap;
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

        //SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        //SceneManagerObject._SceneManager.changeScene(_DungeonChapterData[dungeonID]._DungeonData._DungeonEntranceSceneName);
    }

    public void MoveToFloor(int floorID)
    {
        DungeonMapNode temp = getMapNode(floorID);
        string floorToEnter;
        if (temp._FloorSceneName == "")
        {
            print("Need to generate new floor...");
            floorToEnter = _CurrentDungeon._DungeonFloorList[Random.Range(0, _CurrentDungeon._DungeonFloorList.Count)];
        }
        else floorToEnter = temp._FloorSceneName;
        _PreviousRoomNode = _CurrentDungeonMap.GetComponent<DungeonMapManager>()._FloorNodes[_CurrentRoomID].GetComponent<DungeonMapNode>();
        _CurrentRoomID = floorID;
        _CurrentMapLocator.transform.position = temp.gameObject.transform.position;

        SceneManagerObject._SceneManager.HudFadeOnOpen(1);
        SceneManagerObject._SceneManager.changeScene(floorToEnter);
    }

    #endregion

    void toggleMap(InputAction.CallbackContext context)
    {
        _CurrentDungeonMap.GetComponent<DungeonMapManager>().ToggleMap();
    }

    #region Tools
    public DungeonMapNode getMapNode(int ID)
    {
        if (ID >= _map._FloorNodes.Count) return _map._FloorNodes[_map._FloorNodes.Count - 1].GetComponent<DungeonMapNode>();
        return _map._FloorNodes[ID].GetComponent<DungeonMapNode>();
    }

    public int getCurrentDungeonFloorID()
    {
        return _CurrentRoomID;
    }

    public void setDungeonLocator(GameObject target)
    {
        _CurrentMapLocator = target;
    }

    #endregion
}

[System.Serializable]
public class DungeonChapterData
{
    public int DungeonChapter;
    public DungeonSO _DungeonData;
}