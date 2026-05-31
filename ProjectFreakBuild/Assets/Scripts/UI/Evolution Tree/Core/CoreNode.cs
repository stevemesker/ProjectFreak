using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CoreNode : MonoBehaviour, IBridgeable, IConnectable, ICoreNode
{
    public int CoreNodeMaxPower;
    public int CoreNodeCurrentPower;
    public List<GameObject> connectionNodes;
    public RuneFieldManager rField;

    #region Bridging
    public void BridgeNode(GameObject origin, GameObject bridge)
    {
        connectionNodes.Add(origin);

    }

    public bool canBridge()
    {
        return true;
    }

    public float getMaxRange()
    {
        return GetComponent<RectTransform>().rect.width / 2;
    }

    public void ConnectNode(GameObject connectTo)
    {
        print("This is the core node");
        return;
    }
    #endregion

    #region debug
    [Button("Send Power")]
    public void SendPowerOut()
    {
        
        ConsumePower();
        /*for (int i = 0; i < connectionNodes.Count; i++)
        {
            connectionNodes[i].GetComponent<IConnectable>().ConsumePower();
        }*/
    }

    public void ResetPower()
    {
        //CoreNodeCurrentPower = CoreNodeMaxPower;
        ClearPower();
        rField.ResetRunePower();
    }
    #endregion

    public void ConsumePower()
    {
        //triggers when a node is linked to the core's chain
        rField.ResetRuneChecked();
        if (hasPower() == false) return; //make sure core even has power

        //activate each node connected to core
        for (int i = 0; i < connectionNodes.Count; i++)
        {
            if (connectionNodes[i].GetComponent<IConnectable>().PowerChecked(true) == false) connectionNodes[i].GetComponent<IConnectable>().ConsumePower();
        }
    }

    public void DisconnectNodeTree()
    {
        //go through connections and have them disconnect:

        return;
    }

    public void ReturnPower(int Power)
    {
        CoreNodeCurrentPower += Power;
    }

    public void ClearPower()
    {
        CoreNodeCurrentPower = CoreNodeMaxPower;
    }

    public GameObject GetCoreNode()
    {
        return gameObject;
    }

    public bool PowerChecked(bool CoreHide)
    {
        return CoreHide;
    }

    public bool hasPower()
    {
        if (CoreNodeCurrentPower > 0) return true;
        return false;
    }

    public bool SearchCore(GameObject Origin)
    {
        return true;
    }

    public bool testLength(Vector3 position, Vector3 originPosition)
    {
        return false;
    }

    public int CoreNodePowerConsume(int AmountToTake)
    {
        if (CoreNodeCurrentPower - AmountToTake >= 0) 
        {
            CoreNodeCurrentPower -= AmountToTake;
            return AmountToTake; 
        }
        print("Does not have enough power, I only have " + (CoreNodeCurrentPower - AmountToTake));
        return CoreNodeCurrentPower - AmountToTake;
    }

    public void disconnectNodes(GameObject nodeToDisconnect)
    {
        connectionNodes.Remove(nodeToDisconnect);
    }

    public void ClearConnection()
    {
        for (int i = connectionNodes.Count; i > 0; i++)
        {
            connectionNodes[i].GetComponent<IConnectable>().ClearConnection();
        }

        connectionNodes.Clear();
    }
    public void LoadReconnect()
    {
        //function called to reset a node after the rune field has been loaded
        GameObject temp;
        for (int i = 0; i < connectionNodes.Count; i++)
        {
            if (connectionNodes[i].GetComponent<ElementItem>() != null)
            connectionNodes[i].GetComponent<ElementItem>().connectionsCurrent.Add(gameObject);
            if (connectionNodes[i].GetComponent<IBridgeable>() != null)
                connectionNodes[i].GetComponent<IBridgeable>().LoadReconnect();
            /*
            //if (ConnectionBridgeList[connectionsCurrent[i]] == null)
            if (connectionNodes.ContainsKey(connectionNodes[i]) == false)
            {
                print("Need a bridge");
                temp = Instantiate(BridgePrefabRef, transform.position, Quaternion.identity, transform.parent.transform);
                temp.transform.SetAsFirstSibling();
                temp.GetComponent<RectTransform>().pivot = new Vector2(.5f, 0);
                ConnectionBridgeList.Add(connectionsCurrent[i], temp);
                temp.GetComponent<NodeBridge>().BuildConnection(gameObject, connectionsCurrent[i]);
                temp.GetComponent<NodeBridge>().updatePosition(Vector3.Distance(gameObject.transform.position, connectionsCurrent[i].transform.position) / RuneFieldTransform.localScale.x);
                connectionsCurrent[i].GetComponent<ElementItem>().setBridgeOutside(gameObject, temp);
            }*/
        }
    }
}
