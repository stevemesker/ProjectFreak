using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CoreNode : MonoBehaviour, IBridgeable, IConnectable
{
    public int CoreNodeMaxPower;
    public int CoreNodeCurrectPower;
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

    [Button("Send Power")]
    public void SendPOwerOut()
    {
        for (int i = 0; i < connectionNodes.Count; i++)
        {
            connectionNodes[i].GetComponent<IConnectable>().ConsumePower();
        }
    }

    public void ConsumePower()
    {
        return;
    }

    public void DisconnectNode()
    {
        throw new System.NotImplementedException();
    }

    public GameObject GetCoreNode()
    {
        return gameObject;
    }

    public bool PowerRequired()
    {
        return false;
    }
}
