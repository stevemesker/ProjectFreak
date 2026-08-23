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

    [Header("Runetime Data")]
    [SerializeField] List<GameObject> _FloorNodes;

    [Header("Prefab Settings")]
    [SerializeField] GameObject _floorNodePrefab;
    [SerializeField] GameObject _lineConnectionPrefab;

    public void StartNewMap(DungeonSO data)
    {
        _CurrentDungeonData = data;

        if (_BossZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_FloorZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }
        if (_EntranceZone == null) { Debug.LogError("Error! Selection zone not assigned!"); return; }

        SpawnFloorNodes();
        connectNodes();
    }

    [Button("test")]
    void test()
    {
        if (_FloorNodes.Count > 0)
        {
            for(int i = 0; i < _FloorNodes.Count; i++)
            {
                if (_FloorNodes[i] != null) { DestroyImmediate(_FloorNodes[i]); }
                //else Debug.LogError(_FloorNodes[i].name);
            }
            _FloorNodes.Clear();
        }
        SpawnFloorNodes();
    }
    void SpawnFloorNodes()
    {
        RectTransform zoneTrans = _FloorZone.GetComponent<RectTransform>();

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
                _FloorNodes.Add(instance);

                RectTransform instanceTrans = instance.GetComponent<RectTransform>();

                Vector3 instancePosition = new Vector3(
                    (zoneTrans.rect.width / (_CurrentDungeonData._DungeonColumnCount + 1) * (i+1)) + curveEval(i, _CurrentDungeonData._DungeonRowCount),
                    (zoneTrans.rect.height / (_CurrentDungeonData._DungeonRowCount-1) * (j)) + (+curveEval(i, _CurrentDungeonData._DungeonRowCount)/ _CurrentDungeonData._DungeonColumnCount),
                    0);

                instanceTrans.transform.position += instancePosition;
                counter++;
            }
        }
    }

    float curveEval(int location, int max)
    {
        bool randomBool = UnityEngine.Random.value > 0.5f;
        float switcher = -1;
        if (randomBool) switcher = 1;
        //float offset = (Random.Range(_CurrentDungeonData._DungeonMapNodeWiggle / 2, _CurrentDungeonData._DungeonMapNodeWiggle) - _CurrentDungeonData._DungeonMapNodeWiggle / 2) * _nodeDistribution.Evaluate(location / max);
        float offset = (Random.Range(_CurrentDungeonData._DungeonMapNodeWiggle / 2, _CurrentDungeonData._DungeonMapNodeWiggle)*switcher) *_nodeDistribution.Evaluate(location / max);
        //print(offset);
        return offset;
    }
    void setNodeType()
    {

    }

    void connectNodes()
    {

    }
}
