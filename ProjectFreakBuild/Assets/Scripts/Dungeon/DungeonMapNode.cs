using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DungeonMapNode : MonoBehaviour, IBridgeable
{
    [Header("Data")]
    public int _ID;
    public int _ColumnNumber;
    public POIType.Type _Type;
    public List<GameObject> _NodeConnections;
    public Dictionary<GameObject, GameObject> _BridgeConnections;
    public string _FloorSceneName;
    
    [Header("Settings")]
    public float _DetectionRange;
    public float _MinimumNodePlacementRange;
    public int _ConnectionsMax = 3;
    
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


    #region Bridgeable Interface
    public void BridgeNode(GameObject origin, GameObject bridge)
    {
        print("bridging connections " + origin.name + " | " + bridge.name);
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
