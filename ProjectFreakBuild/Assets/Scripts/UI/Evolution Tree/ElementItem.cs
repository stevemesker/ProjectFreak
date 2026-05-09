using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

public class ElementItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IBridgeable
{
    public int Range;
    public int connectionsMax = 2;
    public List<GameObject> connectionsCurrent;

    [SerializeField]private List<GameObject> connectionList; //all of the nodes within range

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
        drawLineConnectionTemp(false);
    }

    Vector3 calculatePointerPosition()
    {
        print("Calculating snapping...");
        Vector3 temp = Input.mousePosition;
        float comparison; //used to tell distance comparison. Will use the largest value between this object and the connected object
        int exitcounter = 20; //used to quickly break out of the loop. Dunno if needed

        for (int i = 0; i < connectionsCurrent.Count; i++)
        {
            if (Range >= connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange()) comparison = Range + (connectionsCurrent[i].GetComponent<RectTransform>().rect.width/2);
            else comparison = connectionsCurrent[i].GetComponent<IBridgeable>().getMaxRange();

            if (Vector3.Distance(temp, connectionsCurrent[i].transform.position) > comparison) 
            { 
                print("Too Far! Trying to move past distance of " + comparison + ". Currently at " + Vector3.Distance(temp, connectionsCurrent[i].transform.position));
                temp = connectionsCurrent[i].transform.position + ((temp - connectionsCurrent[i].transform.position).normalized) * comparison;
                
                exitcounter--;
                if (exitcounter >0) i = 0;
                else Debug.LogError("Warning! Over 20 recusions were found when calculating snapping distance. That's not good");
            }

            Debug.DrawLine(temp, Input.mousePosition, Color.red);
            Debug.DrawLine(connectionsCurrent[i].transform.position, temp, Color.green);
        }

        

        return temp;
    }

    public float getMaxRange()
    {
        return Range + (GetComponent<RectTransform>().rect.width/2);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        gameObject.GetComponent<SphereCollider>().enabled = true;
        connectNodes(connectionList);
    }

    void connectNodes(List<GameObject> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            print(targets[i].name + " is added to the list");
            connectionsCurrent.Add(targets[i]);
        }
    }
    #endregion

    #region Bridgeable Interface
    public bool canBridge()
    {
        if (connectionsCurrent.Count >= connectionsMax) return false;
        return true;
    }

    public void BridgeNode(GameObject origin)
    {
        print(gameObject.name + " received a bridge to " + origin.name);
    }
    #endregion

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

