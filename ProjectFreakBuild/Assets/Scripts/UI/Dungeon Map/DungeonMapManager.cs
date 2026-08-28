using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class DungeonMapManager : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The ScriptableObject containing the configuration and generation data for the current dungeon")]
    public DungeonSO _CurrentDungeonData;

    [Tooltip("Controls the vertical/column distribution of dungeon nodes during map generation")]
    public AnimationCurve _nodeDistribution;


    [Header("Pointers")]
    [Tooltip("Pointer for the visuals to toggle on/off the map")]
    [SerializeField] GameObject _MapVisuals;

    [Tooltip("The parent/container representing the boss area of the dungeon")]
    [SerializeField] GameObject _BossZone;

    [Tooltip("The parent/container GameObject used for generated floor nodes")]
    [SerializeField] GameObject _FloorZone;

    [Tooltip("The parent/container representing the dungeon entrance area")]
    [SerializeField] GameObject _EntranceZone;

    [Tooltip("The parent/container used to visually connect dungeon areas/nodes")]
    [SerializeField] public GameObject _BridgeZone;

    [Tooltip("The parent/container for locators and other player information about the dungeon")]
    [SerializeField] public GameObject _LocatorZone;


    [Header("Runtime Data")]
    [Tooltip("Runtime list containing all floor node GameObjects generated for the current dungeon")]
    [SerializeField] public List<GameObject> _FloorNodes;

    [Tooltip("Reference to the DungeonMapNode representing the dungeon entrance")]
    public DungeonMapNode _EntranceNode;

    [Tooltip("Separate weighted POI type pools for each dungeon path. Types are drawn from these pools as nodes are assigned and the pool is regenerated when exhausted")]
    public List<List<POIType.Type>> _FloorPool;


    [Header("Prefab Settings")]
    [Tooltip("Prefab instantiated when creating a dungeon floor node")]
    [SerializeField] GameObject _floorNodePrefab;

    [Tooltip("Prefab instantiated to visually connect adjacent dungeon nodes")]
    [SerializeField] public GameObject _lineConnectionPrefab;

    [Tooltip("Possible color combos a floor node can have. Used in the map and the doors to this node")]
    [SerializeField] List<ColorPaletteSO> _ColorSwatches;

    public void StartNewMap(DungeonSO data)
    {
        _CurrentDungeonData = data;

        if (_BossZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_FloorZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_EntranceZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_BridgeZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_LocatorZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }

        SpawnFloorNodes();
        //connectNodes();
        StartCoroutine(detectNodeRange());
    }
    #region Test Tools
    [Button("test")]
    public void test()
    {
        clear();
        SpawnFloorNodes();
        StartCoroutine(detectNodeRange());
        //connectNodes();
    }

    [Button("Clear")]
    public void clear()
    {
        if (_FloorNodes.Count > 0)
        {
            for (int i = 0; i < _FloorNodes.Count; i++)
            {
                if (_FloorNodes[i] != null) { DestroyImmediate(_FloorNodes[i]); }
                //else Debug.LogError(_FloorNodes[i].name);
            }
            _FloorNodes.Clear();
        }
        _EntranceNode = null;
    }
    #endregion

    #region Initial Node Creation
    void SpawnFloorNodes()
    {
        //function that spawns the majority of floor nodes
        //gives them subtle random changes in position based on an animation curve

        RectTransform zoneTrans = _FloorZone.GetComponent<RectTransform>();
        DungeonMapNode mapNode = new DungeonMapNode();

        int columns = _CurrentDungeonData._DungeonColumnCount;
        int rows = _CurrentDungeonData._DungeonRowCount;
        
        int counter = 0;

        //set new floor pool
        _FloorPool = new List<List<POIType.Type>>();
        for (int x = 0; x < columns; x++)
        {
            _FloorPool.Add(new List<POIType.Type>());
            _FloorPool[x] = new List<POIType.Type>();
            //print(_FloorPool[x].Count);
            getFloorNodeTypePool(x);
            //print(_FloorPool[x].Count);
        }

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject instance = Instantiate(
                    _floorNodePrefab,
                    _FloorZone.GetComponent<RectTransform>().position,
                    Quaternion.identity,
                    _FloorZone.transform
                );
                instance.name = "Dungeon Floor Node: " + counter;
                _FloorNodes.Add(instance);
                mapNode = instance.GetComponent<DungeonMapNode>();

                RectTransform instanceTrans = instance.GetComponent<RectTransform>();

                Vector3 instancePosition = new Vector3(
                    (zoneTrans.rect.width / (_CurrentDungeonData._DungeonColumnCount + 1) * (i+1)) + curveEval(i, _CurrentDungeonData._DungeonRowCount),
                    (zoneTrans.rect.height / (_CurrentDungeonData._DungeonRowCount-1) * (j)) + (+curveEval(i, _CurrentDungeonData._DungeonRowCount)/ _CurrentDungeonData._DungeonColumnCount),
                    0);

                instanceTrans.transform.position += instancePosition;

                //fill node data
                mapNode._ID = counter;
                mapNode._ColumnNumber = i;

                if (j > 0) 
                {
                    instance.GetComponent<IBridgeable>().ConnectNode(_FloorNodes[counter - 1]);
                    _FloorNodes[counter - 1].GetComponent<IBridgeable>().ConnectNode(instance);

                    GameObject bridgeInstance = Instantiate(_lineConnectionPrefab, instance.transform.position + new Vector3(0, instance.GetComponent<RectTransform>().rect.height, 0), Quaternion.identity, _BridgeZone.transform);
                    bridgeInstance.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);

                    instance.GetComponent<IBridgeable>().BridgeNode(_FloorNodes[counter - 1], bridgeInstance);
                    _FloorNodes[counter - 1].GetComponent<IBridgeable>().BridgeNode(instance, bridgeInstance);

                    bridgeInstance.GetComponent<NodeBridge>().BuildConnection(_FloorNodes[counter - 1], instance);

                    _FloorNodes[counter - 1].GetComponent<DungeonMapNode>().SetInitialDetectionRange();
                    bridgeInstance.GetComponent<NodeBridge>().updatePosition(Vector2.Distance(_FloorNodes[counter - 1].transform.position, instance.transform.position));
                }
                mapNode._Type = setNodeType(i);


                counter++;
            }
        }

        SpawnKeyNodes();
    }

    void SpawnKeyNodes()
    {
        //function that spawns entrance and boss room nodes
        //there's probably a more efficient way to do this but I don't care...
        int counter = _FloorNodes.Count;
        DungeonMapNode mapNode = new DungeonMapNode();

        int columns = _CurrentDungeonData._DungeonColumnCount;
        int rows = _CurrentDungeonData._DungeonRowCount;

        //spawn entrance node
        GameObject KeyInstance = Instantiate(
                    _floorNodePrefab,
                    _EntranceZone.GetComponent<RectTransform>().position,
                    Quaternion.identity,
                    _EntranceZone.transform
                );
        KeyInstance.name = "Dungeon Floor Node: Entrance " + counter;
        _FloorNodes.Add(KeyInstance);
        mapNode = KeyInstance.GetComponent<DungeonMapNode>();

        mapNode._ID = counter;
        mapNode._ColumnNumber = columns + 1;
        mapNode._Type = POIType.Type.Entrance;
        mapNode._FloorSceneName = DungeonManager._DM._CurrentDungeon._DungeonEntranceSceneName;
        counter++;
        DungeonManager._DM._CurrentMapLocator = Instantiate(DungeonManager._DM._LocatorPrefab, KeyInstance.transform.position, Quaternion.identity, _LocatorZone.transform);
        //DungeonManager._DM.setDungeonLocator(KeyInstance);

        //spawn boss node
        KeyInstance = Instantiate(
                    _floorNodePrefab,
                    _BossZone.GetComponent<RectTransform>().position,
                    Quaternion.identity,
                    _BossZone.transform
                );
        KeyInstance.name = "Dungeon Floor Node: Boss " + counter;
        _FloorNodes.Add(KeyInstance);
        mapNode = KeyInstance.GetComponent<DungeonMapNode>();

        mapNode._ID = counter;
        mapNode._ColumnNumber = columns + 1;
        mapNode._Type = POIType.Type.Boss;
        mapNode._FloorSceneName = DungeonManager._DM._CurrentDungeon._DungeonBossSceneName;

        //set connections
        for (int i = 0; i < columns; i++)
        {
            _FloorNodes[_FloorNodes.Count-2].GetComponent<DungeonMapNode>()._NodeConnections.Add(_FloorNodes[i * rows]);
            _FloorNodes[i * rows].GetComponent<DungeonMapNode>()._NodeConnections.Add(_FloorNodes[_FloorNodes.Count - 2]);
            _EntranceNode = _FloorNodes[_FloorNodes.Count - 2].GetComponent<DungeonMapNode>();

            GameObject bridgeInstance = Instantiate(_lineConnectionPrefab, _FloorNodes[_FloorNodes.Count - 2].transform.position, Quaternion.identity, _BridgeZone.transform);
            bridgeInstance.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);

            _FloorNodes[_FloorNodes.Count - 2].GetComponent<IBridgeable>().BridgeNode(_FloorNodes[i * rows], bridgeInstance);
            _FloorNodes[i * rows].GetComponent<IBridgeable>().BridgeNode(_FloorNodes[_FloorNodes.Count - 2], bridgeInstance);

            bridgeInstance.GetComponent<NodeBridge>().BuildConnection(_FloorNodes[_FloorNodes.Count - 2], _FloorNodes[i * rows]);
            bridgeInstance.GetComponent<NodeBridge>().updatePosition(Vector2.Distance(_FloorNodes[_FloorNodes.Count - 2].transform.position, _FloorNodes[i * rows].transform.position));

            _FloorNodes[_FloorNodes.Count - 1].GetComponent<DungeonMapNode>()._NodeConnections.Add(_FloorNodes[i * rows + rows-1]);
            _FloorNodes[i * rows + rows - 1].GetComponent<DungeonMapNode>()._NodeConnections.Add(_FloorNodes[_FloorNodes.Count - 1]);




            bridgeInstance = Instantiate(_lineConnectionPrefab, _FloorNodes[_FloorNodes.Count - 1].transform.position, Quaternion.identity, _BridgeZone.transform);
            bridgeInstance.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);

            _FloorNodes[_FloorNodes.Count - 1].GetComponent<IBridgeable>().BridgeNode(_FloorNodes[i * rows + rows - 1], bridgeInstance);
            _FloorNodes[i * rows + rows - 1].GetComponent<IBridgeable>().BridgeNode(_FloorNodes[_FloorNodes.Count - 1], bridgeInstance);

            bridgeInstance.GetComponent<NodeBridge>().BuildConnection(_FloorNodes[_FloorNodes.Count - 1], _FloorNodes[i * rows + rows - 1]);
            bridgeInstance.GetComponent<NodeBridge>().updatePosition(Vector2.Distance(_FloorNodes[_FloorNodes.Count - 1].transform.position, _FloorNodes[i * rows + rows - 1].transform.position));
        }
    }

    float curveEval(int location, int max)
    {
        bool randomBool = UnityEngine.Random.value > 0.5f;
        float switcher = -1;
        if (randomBool) switcher = 1;
        float offset = (Random.Range(_CurrentDungeonData._DungeonMapNodeWiggle / 2, _CurrentDungeonData._DungeonMapNodeWiggle)*switcher) *_nodeDistribution.Evaluate(location / max);
        return offset;
    }
    
    void connectNodes()
    {
        for (int i = 0; i < _FloorNodes.Count; i++)
        {
            if (_FloorNodes[i].GetComponent<IBridgeable>().canBridge() == false) { /*print(_FloorNodes[i].name + " returned false");*/ continue; }
            _FloorNodes[i].GetComponent<DungeonMapNode>().ConnectNodesInRange();
            //print(i);
        }
        
    }

    #endregion

    #region Set Node Type
    void getFloorNodeTypePool(int index)
    {
        //function that refills the type pool of index
        //index represents the column the pool is supposed to handle
        List<floorPoolEntry> data = _CurrentDungeonData._DungeonFloorPoolTypes[index].Entry;
        int poolMin = _CurrentDungeonData._DungeonFloorPoolSize;
        float temp;
        int tempInt;

        for (int i = 0; i < data.Count; i++)
        {
            temp = poolMin * (data[i]._EntryChace / 100);
            tempInt = (int)temp;
            if (tempInt <= 0) tempInt = 1;

            print($"Now adding {(int)temp} notes of { data[i]._EntryType } to the pool");
            for (int j = 0; j < tempInt; j++)
            {
                _FloorPool[index].Add(data[i]._EntryType);
            }
        }

        if (_FloorPool[index].Count < poolMin)
        {
            //print($"Not enough entries to pool, adding {poolMin - _FloorPool[index].Count} units of basic to pool");
            while (_FloorPool[index].Count < poolMin)
            {
                _FloorPool[index].Add(POIType.Type.Basic);
            }
        }
    }
    POIType.Type setNodeType(int index)
    {
        int rand = Random.Range(0, _FloorPool[index].Count);
        POIType.Type temp = _FloorPool[index][rand];
        _FloorPool[index].RemoveAt(rand);
        //print($"now assigning {temp} to node");

        if (_FloorPool[index].Count <= 0) getFloorNodeTypePool(index);

        return temp;
    }

    #endregion

    #region NodeColor
    void setNodeColorPalettes()
    {
        int colorPalletCount = 0;
        //print($" <-----Now starting color assign, current floor node count: {_FloorNodes.Count} with possible swatch count: {_ColorSwatches.Count}----->");
        for (int i = 0; i < _FloorNodes.Count; i++)
        {
            //Debug.LogWarning($"Now assigning color for {_FloorNodes[i].name}");
            for (int j = 0; j < _ColorSwatches.Count; j++)
            {
                if (_FloorNodes[i].GetComponent<DungeonMapNode>().TestSelfAndNeighborColor(_ColorSwatches[colorPalletCount]) == false)
                {
                    _FloorNodes[i].GetComponent<DungeonMapNode>().SetColorPaletteSwatch(_ColorSwatches[colorPalletCount]);
                    j = _ColorSwatches.Count;
                }
                colorPalletCount++;
                if (colorPalletCount >= _ColorSwatches.Count) colorPalletCount = 0;
            }
        }
    }
    #endregion

    [Button("Toggle Map")]
    public void ToggleMap()
    {
        _MapVisuals.SetActive(!_MapVisuals.activeSelf);
    }

    IEnumerator detectNodeRange()
    {
        //yield return new WaitForSeconds(.01f);
        yield return null;
        connectNodes();
        setNodeColorPalettes();
        DungeonManager._DM.MoveToFloor(_FloorNodes.Count - 2);
    }
}
