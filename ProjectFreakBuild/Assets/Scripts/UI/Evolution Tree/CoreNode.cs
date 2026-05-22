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
        CoreNodeCurrentPower = CoreNodeMaxPower;
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
}
