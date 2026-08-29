using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonMapNode : MonoBehaviour, IBridgeable
{
    [Header("Data")]
    public string _FloorSceneName;
    public int _ID;
    public int _ColumnNumber;
    public POIType.Type _Type;
    public List<GameObject> _NodeConnections;
    public Dictionary<GameObject, GameObject> _BridgeConnections;
    public ColorPaletteSO _ColorSwatch;
    
    [Header("Settings")]
    public float _DetectionRange;
    public float _MinimumNodePlacementRange;
    public int _ConnectionsMax = 3;

    [Header("Reference")]
    public GameObject _IconPointer;
    public GameObject _IconColorChanger;
    
    void Awake()
    {
        _BridgeConnections = new Dictionary<GameObject, GameObject>();
    }
    public void SetInitialDetectionRange()
    {
        float dist = Vector3.Distance(transform.position, _NodeConnections[0].transform.position);
        float temp;
        for (int i = 0; i < _NodeConnections.Count; i++)
        {
            temp = Vector3.Distance(transform.position, _NodeConnections[i].transform.position);
            if (temp > dist) dist = temp;
        }

        _DetectionRange = dist;
    }

    public void ConnectNodesInRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position,_DetectionRange);

        Array.Sort(hits, (a, b) =>
        {
            float distanceA = (transform.position - a.transform.position).sqrMagnitude;
            float distanceB = (transform.position - b.transform.position).sqrMagnitude;

            return distanceA.CompareTo(distanceB);
        });

        foreach (Collider2D hit in hits)
        {
            if (_NodeConnections.Contains(hit.gameObject) == true || hit.gameObject == gameObject || _NodeConnections.Count >= _ConnectionsMax) continue;
            if (hit.GetComponent<IBridgeable>().canBridge() == false) continue;

            //might need to make sure no node is in the way of adding the node
            _NodeConnections.Add(hit.gameObject);
            hit.GetComponent<IBridgeable>().ConnectNode(gameObject);

            GameObject bridgeInstance = Instantiate(transform.root.gameObject.GetComponent<DungeonMapManager>()._lineConnectionPrefab, transform.position, Quaternion.identity, (transform.root.gameObject.GetComponent<DungeonMapManager>()._BridgeZone.transform));
            bridgeInstance.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
            hit.GetComponent<IBridgeable>().BridgeNode(gameObject, bridgeInstance);
            bridgeInstance.GetComponent<NodeBridge>().BuildConnection(hit.gameObject, gameObject);
            bridgeInstance.GetComponent<NodeBridge>().updatePosition(Vector2.Distance(transform.position, hit.transform.position));
        }
        
    }

    public void SetColorPaletteSwatch(ColorPaletteSO colorSwatch)
    {
        _ColorSwatch = colorSwatch;

        //temp for now til I make the nodes good
        _IconColorChanger.GetComponent<Image>().color = _ColorSwatch._PrimaryColor;
    }

    public void SetIcon(Sprite Icon)
    {
        _IconPointer.GetComponent<Image>().sprite = Icon;
    }

public bool TestSelfAndNeighborColor(ColorPaletteSO colorToTest)
    {
        //function that tests object and its neighbors if a color is being used
        //used to ensure more readable maps
        //print($"Now testing {gameObject.name} and {_NodeConnections.Count} neighbors for {colorToTest.name}...");
        if (TestColor(colorToTest)) return true;
        //print($"{gameObject.name} does not have color assigned, checking neighbors...");
        for (int i = 0; i < _NodeConnections.Count; i++)
        {
            if (_NodeConnections[i].GetComponent<DungeonMapNode>().testNeighborColor(colorToTest)) { /*print($"Cannot chose color {colorToTest.name} as that has been assigned to node: {_NodeConnections[i].name}");*/ return true; }
        }
        //print($"Neighborbeors of {gameObject.name} do not have color {colorToTest.name}...");
        return false;
    }

    public bool testNeighborColor(ColorPaletteSO colorToTest)
    {
        if (TestColor(colorToTest)) return true;
        for (int i = 0; i < _NodeConnections.Count; i++)
        {
            if (_NodeConnections[i].GetComponent<DungeonMapNode>().TestColor(colorToTest)) { /*print($"Cannot chose color {colorToTest.name} as that has been assigned to node: {_NodeConnections[i].name}");*/ return true; }
        }
        return false;
    }

    public bool TestColor(ColorPaletteSO colorTest)
    {
        //print($"Now testing for node {gameObject.name} to see if it has color:{colorTest.name}");
        if (_ColorSwatch == null) { /*print($"{gameObject.name} currently does not have a color assigned");*/ return false; }
        if (colorTest == _ColorSwatch) { /*Debug.LogError($"{gameObject.name} currently has {_ColorSwatch.name}");*/ return true; }
        //print($"Success! {gameObject.name} does not currently have {colorTest.name} assigned!");
        return false;
    }


    #region Bridgeable Interface
    public void BridgeNode(GameObject origin, GameObject bridge)
    {
        //print("bridging connections " + origin.name + " | " + bridge.name);
        _BridgeConnections.Add(origin, bridge);
    }

    public bool canBridge()
    {
        if (_NodeConnections.Count >= _ConnectionsMax) return false;
        return true;
    }

    public void ConnectNode(GameObject connectTo)
    {
        _NodeConnections.Add(connectTo);
    }

    public void disconnectNodes(GameObject nodeToDisconnect)
    {
        throw new System.NotImplementedException();
    }

    public float getMaxRange()
    {
        return _DetectionRange;
    }

    public void LoadReconnect()
    {
        throw new System.NotImplementedException();
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _DetectionRange);
    }
}
