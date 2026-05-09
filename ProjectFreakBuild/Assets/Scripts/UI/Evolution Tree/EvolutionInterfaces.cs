using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBridgeable
{
    bool canBridge();
    void BridgeNode(GameObject origin);
}
