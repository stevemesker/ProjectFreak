using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreNode : MonoBehaviour, IBridgeable
{
    public int CoreNodeMaxPower;
    public int CoreNodeCurrectPower;
    public List<GameObject> connectionNodes;

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
        return GetComponent<RectTransform>().rect.width/2;
    }
}
