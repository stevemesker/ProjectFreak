using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CoreNode : MonoBehaviour, IBridgeable, IConnectable
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

    [Button("Send Power")]
    public void SendPOwerOut()
    {
        ResetPower();
        for (int i = 0; i < connectionNodes.Count; i++)
        {
            connectionNodes[i].GetComponent<IConnectable>().ConsumePower(0);
        }
    }

    public void ResetPower()
    {
        CoreNodeCurrentPower = CoreNodeMaxPower;
        rField.ResetRunePower();
    }

    public void ConsumePower(int amount)
    {
        CoreNodeCurrentPower -= amount;
        if (CoreNodeCurrentPower < 0)
        {
            Debug.LogWarning("Warning! Not enough power! Cannot save current shade...");
        }
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
