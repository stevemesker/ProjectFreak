using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ElementItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IBridgeable, IConnectable
{
    [Header("<=====Node Settings=====>")]
    public int Range;
    public int connectionsMax = 2;
    [SerializeField, Tooltip("Bridge prefab object to be spawned when connections are formed")] private GameObject BridgePrefabRef;
    [SerializeField, Tooltip("Number of times we'll recalculate to find snapping point during dragging")] private int recursionDetectionResolution = 30; //number of times we'll recalculate to find snapping point during dragging

    [Header("<=====Connections Lists")]
    public List<GameObject> connectionsCurrent;
    public GameObject CoreNode;

    [Header("<=====Power=====>")]
    public int RequiredPower; //how much power the node needs to function
    public int CurrentPower; //how much power this node is using

    [Header("<-----Private/Debug----->")]
    [SerializeField, Tooltip("All of the nodes within range")] private List<GameObject> connectionList; //all of the nodes within range
    private Dictionary<GameObject, GameObject> ConnectionBridgeList = new Dictionary<GameObject,GameObject>(); //other node is key, value is the bridge connecting them

    RaycastHit hit;

    #region Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        gameObject.GetComponent<SphereCollider>().enabled = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (connectionsCurrent.Count == 0) transform.position = Input.mousePosition;
        else transform.position = calculatePointerPosition();

        connectionList = FindConnections();
        connectionList = buildConnections(connectionList);
        UpdateConnections();
        drawLineConnectionTemp(false);
    }

    Vector3 calculatePointerPosition()
    {
        //print("Calculating snapping...");
        Vector3 temp = Input.mousePosition;
        float comparison; //used to tell distance comparison. Will use the largest value between this object and the connected object
        int exitcounter = recursionDetectionResolution; //used to quickly break out of the loop. Dunno if needed

        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            if (Range >= connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange()) comparison = Range + (connectionsCurrent[i].GetComponent<RectTransform>().rect.width/2);
            else comparison = connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange();

            if (Vector3.Distance(temp, connectionsCurrent[i].transform.position) > comparison) 
            { 
                //print("Too Far! Trying to move past distance of " + comparison + ". Currently at " + Vector3.Distance(temp, connectionsCurrent[i].transform.position));
                temp = connectionsCurrent[i].transform.position + ((temp - connectionsCurrent[i].transform.position).normalized) * comparison;
                
                exitcounter--;
                if (exitcounter > 0) i = 0;
                else { Debug.LogWarning("Warning! Over " + recursionDetectionResolution + " recalculations were found when finding snapping distance. Man this algorithm is inefficient..."); return transform.position; }
            }

            Debug.DrawLine(temp, Input.mousePosition, Color.red);
            Debug.DrawLine(connectionsCurrent[i].transform.position, temp, Color.green);
        }

        

        return temp;
    }

    

    public void OnEndDrag(PointerEventData eventData)
    {
        gameObject.GetComponent<SphereCollider>().enabled = true;
        connectNodes(connectionList);
    }

    #endregion

    #region Bridgeable Interface
    public void connectBridge(GameObject bridge, GameObject connectTo)
    {
        ConnectionBridgeList.Add(connectTo, bridge);
    }

    void connectNodes(List<GameObject> targets)
    {
        GameObject temp;
        for (int i = 0; i < targets.Count; i++)
        {
            print(targets[i].name + " is added to the list");
            connectionsCurrent.Add(targets[i]);
            temp = Instantiate(BridgePrefabRef, transform.position, Quaternion.identity, transform.parent.transform);
            temp.transform.SetAsFirstSibling();
            temp.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
            targets[i].GetComponent<IBridgeable>().BridgeNode(gameObject, temp);
            BridgeInstaceToNode(temp, targets[i]);
            if (targets[i].GetComponent<IConnectable>() != null) { if (targets[i].GetComponent<IConnectable>().GetCoreNode() != null) CoreNode = targets[i].GetComponent<IConnectable>().GetCoreNode(); }
            //ConnectionBridgeList.Add(targets[i], temp);
        }
        if (CoreNode != null) ConnectNode(CoreNode);
    }
    void BridgeInstaceToNode(GameObject bridge, GameObject connectTo)
    {
        bridge.GetComponent<NodeBridge>().BuildConnection(gameObject, connectTo);
        bridge.GetComponent<NodeBridge>().updatePosition(Vector3.Distance(gameObject.transform.position, connectTo.transform.position));
        ConnectionBridgeList.Add(connectTo, bridge);
    }

    void UpdateConnections()
    {
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            ConnectionBridgeList[connectionsCurrent[i]].GetComponent<NodeBridge>().updatePosition(Vector3.Distance(gameObject.transform.position, connectionsCurrent[i].transform.position));
        }
    }
    

    
    public bool canBridge()
    {
        if (connectionsCurrent.Count >= connectionsMax) return false;
        return true;
    }

    public void BridgeNode(GameObject origin, GameObject bridge)
    {
        print(gameObject.name + " received a bridge to " + origin.name);
        connectionsCurrent.Add(origin);
        ConnectionBridgeList.Add(origin, bridge);
    }

    public float getMaxRange()
    {
        return Range + (GetComponent<RectTransform>().rect.width / 2);
    }

    #endregion

    #region Connection Building
    List<GameObject> buildConnections(List<GameObject> targets)
    {
        //function that takes the list of targets within range and returns which of them are free to build a connection
        //prioritizes closest objects and if they have an empty slot in connectionsCurrent
        targets = targets.OrderBy(obj =>
            (obj.transform.position - transform.position).sqrMagnitude
        ).ToList();

        List<GameObject> temp = new List<GameObject>();
        for (int i = 0; i < connectionsMax - connectionsCurrent.Count; i++)
        {
            if (targets.Count <= i) return temp;
            if (connectionsCurrent.Contains(targets[i]) == false)
            temp.Add(targets[i]); 
        }
        return temp;
    }

    List<GameObject> FindConnections()
    {
        //finds all of the node objects within range of the element being dragged
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, Range);
        List<GameObject> temp = new List<GameObject>();

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.GetComponent<IBridgeable>().canBridge() == true)
            temp.Add(hitCollider.gameObject); 
        }
        return temp;
    }
    #endregion

    #region Connectable Interface
    public void ConnectNode(GameObject connectTo)
    {
        CoreNode = connectTo;
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            if (connectionsCurrent[i].GetComponent<IConnectable>() != null)
            {
                if (connectionsCurrent[i].GetComponent<IConnectable>().GetCoreNode() != connectTo) 
                { 
                    print("Connecting nodes to " + connectTo.name);
                    connectionsCurrent[i].GetComponent<IConnectable>().ConnectNode(connectTo);
                }
            }
        }
        return;
    }

    public void ConsumePower()
    {
        throw new System.NotImplementedException();
    }

    public bool PowerRequired()
    {
        return false;
    }

    public GameObject GetCoreNode()
    {
        return CoreNode;
    }

    #endregion

    #region debug

    private void drawLineConnectionTemp(bool type)
    {
        //false is lose bond true is strong bond 
        if (type == false)
        {
            for (int i = 0; i < connectionList.Count; i++)
            {
                Debug.DrawLine(transform.position, connectionList[i].transform.position, Color.blue);
            }
        }
        else
        {
            for (int i = 0; i < connectionList.Count; i++)
            {
                Debug.DrawLine(transform.position, connectionsCurrent[i].transform.position, Color.red);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, Range);
    }

    #endregion
}

