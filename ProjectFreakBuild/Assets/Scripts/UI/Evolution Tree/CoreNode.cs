using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoreNode : MonoBehaviour, IBridgeable
{
    public int CoreNodeMaxPower;
    public int CoreNodeCurrectPower;

    public void BridgeNode(GameObject origin)
    {
        throw new System.NotImplementedException();
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
