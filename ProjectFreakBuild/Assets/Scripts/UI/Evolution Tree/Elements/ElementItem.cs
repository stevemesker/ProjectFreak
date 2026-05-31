using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ElementItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IBridgeable, IConnectable
{
    [Header("<=====Pointers=====>")]
    public RectTransform RuneFieldTransform;
    [SerializeField] ElementItemSO _ElementAttached;

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

    public GameObject pluggedNode; //currently connected node
    [SerializeField] GameObject FoundNode; //node that may be in range of current element

    RaycastHit hit;

    void Awake()
    {
        if (RuneFieldTransform == null) RuneFieldTransform = gameObject.transform.parent.GetComponent<RectTransform>();
    }

    #region Element Activation

    void ActivateAttachedElement()
    {
        if (_ElementAttached == null) return;
        _ElementAttached.triggerElementEffects(gameObject);
    }

    void DeactivateAttachedElement()
    {
        if (_ElementAttached == null) return;
        _ElementAttached.deactivateElementEffects(gameObject);
    }

    public void setElementSOAttachment(ElementItemSO ele)
    {
        print("Setting element " + ele.name);
        _ElementAttached = ele;
    }

    public ElementItemSO getElementSOAttachment()
    {
        return _ElementAttached;
    }
    #endregion

    #region Drag
    public void OnBeginDrag(PointerEventData eventData)
    {
        //add locking element here
        gameObject.GetComponent<SphereCollider>().enabled = false;

        if (pluggedNode == null) return;
        pluggedNode.GetComponent<EvolutionNode>().UnplugElement();
        pluggedNode = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        ElementPositionUpdate(Input.mousePosition);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //add locking element here
        gameObject.GetComponent<SphereCollider>().enabled = true;
        connectNodes(connectionList);
        StopAllTearing();

        //connect to elements
        if (FoundNode != null && pluggedNode == null) ConnectToNode();
    }

    #endregion
    
    #region Movement
    private void ElementPositionUpdate(Vector3 aimInputLocation)
    {
        //add locking element here

        //<---decide where element is trying to move to (pointer position, or max reach)--->//
        Vector3 aimlocation = new Vector3(); //will be the final position of the element after all sticking is over

        if (connectionsCurrent.Count == 0) aimlocation = aimInputLocation; //no connections found, just update the movement
        else aimlocation = calculatePointerPosition(aimInputLocation); //there are attached bridges, must find the closest point to location
        //<---|||||--->//

        //<---Find possible rune connections--->//
        connectionList = FindConnections(aimlocation);

        if (FoundNode != null) if (testLength(FoundNode.transform.position, aimlocation))
            {
                //this handles snapping to pre made nodes on the field
                print("Moving to node " + FoundNode.name);
                aimlocation = FoundNode.transform.position;
                connectionList = FindConnections(aimlocation);

            }
        //<---|||||--->//

        //<---Cuts the list down to the closest elements that have space for connections--->//
        connectionList = buildConnections(connectionList, aimlocation);
        //<---|||||--->//

        transform.position = aimlocation;
        UpdateConnections();
        drawLineConnectionTemp(false);
    }

    Vector3 calculatePointerPosition(Vector3 target)
    {
        Vector3 adjustedMousePosition = target;
        float comparison = new float(); //final distance between two elements considering the largest range each of them has
        int exitcounter = recursionDetectionResolution; //used to quickly break out of the loop. Dunno if needed

        //make sure target is withing range of all connected nodes
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            //comparison is the total range based on which element has a greater range
            if (Range >= connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange()) { comparison = (Range + (connectionsCurrent[i].GetComponent<RectTransform>().rect.width / 2)) * RuneFieldTransform.localScale.x; }
            else comparison = (connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange() + (gameObject.GetComponent<RectTransform>().rect.width / 2)) * RuneFieldTransform.localScale.x;

            if (Vector3.Distance(adjustedMousePosition, connectionsCurrent[i].transform.position) > comparison)
            {
                adjustedMousePosition = connectionsCurrent[i].transform.position + ((adjustedMousePosition - connectionsCurrent[i].transform.position).normalized) * comparison;

                exitcounter--;
                if (exitcounter > 0) i = 0;
                //else { Debug.LogWarning("Warning! Over " + recursionDetectionResolution + " recalculations were found when finding snapping distance. Man this algorithm is inefficient..."); }

            }
            Debug.DrawLine(connectionsCurrent[i].transform.position, adjustedMousePosition, Color.green);
        }
        Debug.DrawLine(adjustedMousePosition, Input.mousePosition, Color.red);

        for (int x = 0; x < connectionsCurrent.Count; x++)
        {
            if (Range >= connectionsCurrent[x].GetComponent<IBridgeable>().getMaxRange()) { comparison = (Range + (connectionsCurrent[x].GetComponent<RectTransform>().rect.width / 2)) * RuneFieldTransform.localScale.x; }
            else comparison = (connectionsCurrent[x].GetComponent<IBridgeable>().getMaxRange() + (gameObject.GetComponent<RectTransform>().rect.width / 2)) * RuneFieldTransform.localScale.x;

            if (Vector3.Distance(connectionsCurrent[x].transform.position, adjustedMousePosition) >= comparison - (snapPadding * RuneFieldTransform.localScale.x) && Vector3.Distance(connectionsCurrent[x].transform.position, Input.mousePosition) >= comparison - (snapPadding * RuneFieldTransform.localScale.x))
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
            print("Boop");
            CoreNode.GetComponent<IConnectable>().ConsumePower();
        }
    }
    void BridgeInstaceToNode(GameObject bridge, GameObject connectTo)
    {
        bridge.GetComponent<NodeBridge>().BuildConnection(gameObject, connectTo);
        bridge.GetComponent<NodeBridge>().updatePosition(Vector3.Distance(gameObject.transform.position, connectTo.transform.position) / RuneFieldTransform.localScale.x);
        ConnectionBridgeList.Add(connectTo, bridge);
    }

    void UpdateConnections()
    {
        //function goes through all connections after moving and ensures they're in the right length/position/rotation
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            ConnectionBridgeList[connectionsCurrent[i]].GetComponent<NodeBridge>().updatePosition(Vector3.Distance(gameObject.transform.position, connectionsCurrent[i].transform.position) / RuneFieldTransform.localScale.x);
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

    public void LoadReconnect()
    {
        //function called to reset a node after the rune field has been loaded
        GameObject temp;
        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            //if (ConnectionBridgeList[connectionsCurrent[i]] == null)
            if (ConnectionBridgeList.ContainsKey(connectionsCurrent[i]) == false)
            {
                print("Need a bridge");
                temp = Instantiate(BridgePrefabRef, transform.position, Quaternion.identity, transform.parent.transform);
                temp.transform.SetAsFirstSibling();
                temp.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
                ConnectionBridgeList.Add(connectionsCurrent[i], temp);
                temp.GetComponent<NodeBridge>().BuildConnection(gameObject, connectionsCurrent[i]);
                temp.GetComponent<NodeBridge>().updatePosition(Vector3.Distance(gameObject.transform.position, connectionsCurrent[i].transform.position) / RuneFieldTransform.localScale.x);

                if (connectionsCurrent[i].GetComponent<ElementItem>() != null)
                connectionsCurrent[i].GetComponent<ElementItem>().setBridgeOutside(gameObject, temp);
            }
        }
    }

    public void setBridgeOutside(GameObject origin, GameObject existingBridge)
    {
        ConnectionBridgeList.Add(origin, existingBridge);
    }

    List<GameObject> buildConnections(List<GameObject> targets, Vector3 originPoint)
    {
        //function that takes the list of targets within range and returns which of them are free to build a connection
        //prioritizes closest objects and if they have an empty slot in connectionsCurrent
        targets = targets.OrderBy(obj =>
            (obj.transform.position - originPoint).sqrMagnitude
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

    List<GameObject> FindConnections(Vector3 originPoint)
    {
        //finds all of the node objects within range of the element being dragged
        FoundNode = null;

        Collider[] hitColliders = Physics.OverlapSphere(originPoint, Range * RuneFieldTransform.localScale.x);
        List<GameObject> temp = new List<GameObject>();

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.GetComponent<IBridgeable>() != null) if (hitCollider.GetComponent<IBridgeable>().canBridge() == true) temp.Add(hitCollider.gameObject);

            if (pluggedNode == null && hitCollider.GetComponent<iEvolutionNode>() != null) 
            { 
                if (hitCollider.GetComponent<iEvolutionNode>().isPlugged() == false && Vector3.Distance(originPoint, hitCollider.transform.position) <= GetComponent<RectTransform>().rect.width/2) FoundNode = hitCollider.gameObject; 
            }
        }
        return temp;
    }

    public bool testLength(Vector3 position, Vector3 originPosition)
    {
        //takes in how much the node should move and returns if that is possible given the current connection lengths

        Vector3 adjustedMousePosition = originPosition;
        float comparison = new float(); //final distance between two elements considering the larges range each of them has

        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            //go through each connection and see if that new position isn't achievable

            if (Range >= connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange()) { comparison = (Range + (connectionsCurrent[i].GetComponent<RectTransform>().rect.width / 2)) * RuneFieldTransform.localScale.x; }
            else comparison = (connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange() + (gameObject.GetComponent<RectTransform>().rect.width / 2)) * RuneFieldTransform.localScale.x;

            if (comparison < Vector3.Distance(position, connectionsCurrent[i].transform.position))
            {
                print("Not enough room to move node because of connection length of " + connectionsCurrent[i].name + " and " + gameObject.name);
                return false;
            }
        }
        return true;
    }

    void ConnectToNode()
    {
        pluggedNode = FoundNode;
        EvolutionNode plugTemp = pluggedNode.GetComponent<EvolutionNode>();

        plugTemp.PlugElement(gameObject);
        plugTemp.ActivatePluggedNode();
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

    public void ClearConnection()
    {
        print("boop");
        for (int i = connectionsCurrent.Count-1; i > -1; i--)
        {
            print(i);
            ConnectionBridgeList[connectionsCurrent[i]].GetComponent<NodeBridge>().clearConnections();
        }
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
            ActivateAttachedElement();
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
            DeactivateAttachedElement();
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
            print(connectionsCurrent[i].name + " is checking as " + connectionsCurrent[i].GetComponent<IConnectable>().PowerChecked(false));
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

