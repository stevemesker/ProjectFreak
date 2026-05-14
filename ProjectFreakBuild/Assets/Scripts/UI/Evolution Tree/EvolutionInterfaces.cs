using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBridgeable
{
    float getMaxRange();
    bool canBridge();
    void BridgeNode(GameObject origin, GameObject bridge);
}

