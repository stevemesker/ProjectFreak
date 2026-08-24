using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class DungeonMapManager : MonoBehaviour
{
    [Header("Data")]
    public DungeonSO _CurrentDungeonData;
    public AnimationCurve _nodeDistribution;

    [Header("Pointers")]
    [SerializeField] GameObject _BossZone;
    [SerializeField] GameObject _FloorZone;
    [SerializeField] GameObject _EntranceZone;
    [SerializeField] public GameObject _BridgeZone;

    [Header("Runetime Data")]
    [SerializeField] public List<GameObject> _FloorNodes;
    public DungeonMapNode _EntranceNode;

    [Header("Prefab Settings")]
    [SerializeField] GameObject _floorNodePrefab;
    [SerializeField] public GameObject _lineConnectionPrefab;

    public void StartNewMap(DungeonSO data)
    {
        _CurrentDungeonData = data;

        if (_BossZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_FloorZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_EntranceZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_BridgeZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }

        SpawnFloorNodes();
        connectNodes();
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
    void SpawnFloorNodes()
    {
        RectTransform zoneTrans = _FloorZone.GetComponent<RectTransform>();
        DungeonMapNode mapNode = new DungeonMapNode();

        int columns = _CurrentDungeonData._DungeonColumnCount;
        int rows = _CurrentDungeonData._DungeonRowCount;
        
        int counter = 0;

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
        counter++;

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
            if (_FloorNodes[i].GetComponent<IBridgeable>().canBridge() == false) { print(_FloorNodes[i].name + " returned false"); continue; }
            _FloorNodes[i].GetComponent<DungeonMapNode>().ConnectNodesInRange();
        }
    }

    void setNodeType()
    {

    }

    IEnumerator detectNodeRange()
    {
        //yield return new WaitForSeconds(.01f);
        yield return null;
        connectNodes();
    }
}
