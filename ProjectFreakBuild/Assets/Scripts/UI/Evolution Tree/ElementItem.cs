using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ElementItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IBridgeable, IConnectable
{
    [Header("<=====Pointers=====>")]
    public RectTransform RuneFieldTransform;

    [Header("<=====Node Settings=====>")]
    [Tooltip("How far from the node's center will it reach to make a bridge. Recommended at least 100")]
    public int Range = 100;
    [Tooltip("How many nodes can be bridged to this node. Should have at least 1 and no more than 6 is recommended")]
    public int connectionsMax = 2;
    [SerializeField, Tooltip("Bridge prefab object to be spawned when connections are formed")] 
    private GameObject BridgePrefabRef;
    [SerializeField, Tooltip("Number of times we'll recalculate to find snapping point during dragging")] 
    private int recursionDetectionResolution = 30;
    [SerializeField, Tooltip("Extra padding so that snapping is continuous")]
    private float snapPadding = 2;

    [Header("<=====Connections Lists")]
    [Tooltip("Points to all the nodes connected to this one")]
    public List<GameObject> connectionsCurrent;
    [Tooltip("Points to the core node if it somehow connects to it via a chain or direct connection")]
    public GameObject CoreNode;

    [Header("<=====Power=====>")]
    [Tooltip("how much power the node needs to function")]
    public int RequiredPower; //how much power the node needs to function
    [Tooltip("how much power this node is using")]
    public int CurrentPower; //how much power this node is using
    [Tooltip("used to stop recursion on power checks")]
    [SerializeField] private bool isChecked; //used to stop recursion on power checks

    [Header("<-----Private/Debug----->")]
    [SerializeField, Tooltip("All of the nodes within range")] private List<GameObject> connectionList; //all of the nodes within range
    private Dictionary<GameObject, GameObject> ConnectionBridgeList = new Dictionary<GameObject,GameObject>(); //other node is key, value is the bridge connecting them

    RaycastHit hit;

    void Awake()
    {
        if (RuneFieldTransform == null) RuneFieldTransform = gameObject.transform.parent.GetComponent<RectTransform>();
    }

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
        Vector3 adjustedMousePosition = Input.mousePosition;
        float comparison = new float(); //final distance between two elements considering the larges range each of them has
        int exitcounter = recursionDetectionResolution; //used to quickly break out of the loop. Dunno if needed

        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            if (Range >= connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange()) { comparison = Range + (connectionsCurrent[i].GetComponent<RectTransform>().rect.width / 2);  }
            else comparison = connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange() + (gameObject.GetComponent<RectTransform>().rect.width / 2);

            if (Vector3.Distance(adjustedMousePosition, connectionsCurrent[i].transform.position) > comparison) 
            {
                adjustedMousePosition = connectionsCurrent[i].transform.position + ((adjustedMousePosition - connectionsCurrent[i].transform.position).normalized) * comparison;
                
                exitcounter--;
                if (exitcounter > 0) i = 0;
                else { Debug.LogWarning("Warning! Over " + recursionDetectionResolution + " recalculations were found when finding snapping distance. Man this algorithm is inefficient...");  }
                
            }
            Debug.DrawLine(connectionsCurrent[i].transform.position, adjustedMousePosition, Color.green);
        }
        Debug.DrawLine(adjustedMousePosition, Input.mousePosition, Color.red);

        for (int x = 0; x < connectionsCurrent.Count; x++)
        {
            if (Range >= connectionsCurrent[x].GetComponent<IBridgeable>().getMaxRange()) { comparison = Range + (connectionsCurrent[x].GetComponent<RectTransform>().rect.width / 2); }
            else comparison = connectionsCurrent[x].GetComponent<IBridgeable>().getMaxRange() + (gameObject.GetComponent<RectTransform>().rect.width / 2);

            if (Vector3.Distance(connectionsCurrent[x].transform.position, adjustedMousePosition) >= comparison - snapPadding && Vector3.Distance(connectionsCurrent[x].transform.position, Input.mousePosition) >= comparison - snapPadding)
            {
                ConnectionBridgeList[connectionsCurrent[x]].GetComponent<NodeBridge>().StartTearing(Vector3.Distance(Input.mousePosition, connectionsCurrent[x].transform.position) - comparison, gameObject);
            }
            else
            {
                ConnectionBridgeList[connectionsCurrent[x]].GetComponent<NodeBridge>().StopTearing();
            }
        }

        return adjustedMousePosition;
    }

    

    public void OnEndDrag(PointerEventData eventData)
    {
        gameObject.GetComponent<SphereCollider>().enabled = true;
        connectNodes(connectionList);
        StopAllTearing();
    }

    void StopAllTearing()
    {
        //cancels all tearing of bridges
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            ConnectionBridgeList[connectionsCurrent[i]].GetComponent<NodeBridge>().StopTearing();
        }
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
            connectionsCurrent.Add(targets[i]);
            temp = Instantiate(BridgePrefabRef, transform.position, Quaternion.identity, transform.parent.transform);
            temp.transform.SetAsFirstSibling();
            temp.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
            targets[i].GetComponent<IBridgeable>().BridgeNode(gameObject, temp);
            BridgeInstaceToNode(temp, targets[i]);
            if (targets[i].GetComponent<IConnectable>() != null) { if (targets[i].GetComponent<IConnectable>().GetCoreNode() != null) CoreNode = targets[i].GetComponent<IConnectable>().GetCoreNode(); }
        }
        if (CoreNode != null)
        {
            ConnectNode(CoreNode);
            CoreNode.GetComponent<IConnectable>().ConsumePower();
        }
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
        return Range /*+ (GetComponent<RectTransform>().rect.width / 2)*/;
    }

    public void disconnectNodes(GameObject nodeToDisconnect)
    {
        ConnectionBridgeList.Remove(nodeToDisconnect);
        connectionsCurrent.Remove(nodeToDisconnect);
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
        if (connectionsCurrent.Count >= connectionsMax) return temp;

        for (int i = 0; i < targets.Count; i++)
        {
            if (connectionsCurrent.Contains(targets[i]) == false) temp.Add(targets[i]);
            if (connectionsCurrent.Count >= connectionsMax) return temp;
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
        if (CoreNode.GetComponent<ICoreNode>() == null) return;
        isChecked = true;
        if (CurrentPower != RequiredPower)
        {
            int temp = CoreNode.GetComponent<ICoreNode>().CoreNodePowerConsume(RequiredPower - CurrentPower);
            if (temp < RequiredPower - CurrentPower) return;
            CurrentPower += temp;
        }
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            if (connectionsCurrent[i].GetComponent<IConnectable>().PowerChecked(true) == false)
            {
                connectionsCurrent[i].GetComponent<IConnectable>().ConsumePower();
            }
        }
    }
    public void DisconnectNodeTree()
    {
        //go through connections and have them disconnect:
        if (CoreNode == null) return;
        
        if (CurrentPower > 0) 
        {
            CoreNode.GetComponent<ICoreNode>().ReturnPower(CurrentPower);
            CurrentPower = 0;
        }
        CoreNode = null;
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            connectionsCurrent[i].GetComponent<IConnectable>().DisconnectNodeTree();
        }
        
    }

    public bool PowerChecked(bool CoreHide)
    {
        return isChecked;
    }

    public void CheckedReset()
    {
        isChecked = false;
    }

    public GameObject GetCoreNode()
    {
        return CoreNode;
    }

    public bool SearchCore(GameObject Origin)
    {
        isChecked = true;

        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            if (connectionsCurrent[i].GetComponent<IConnectable>().PowerChecked(false) == false)
            {
                if (connectionsCurrent[i].GetComponent<IConnectable>().SearchCore(Origin)) return true;
            }
        }

        return false;
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
        Gizmos.DrawWireSphere(transform.position, Range * RuneFieldTransform.localScale.x);
    }

    #endregion
}

