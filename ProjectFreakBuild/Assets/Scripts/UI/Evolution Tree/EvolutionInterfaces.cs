using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBridgeable
{
    float getMaxRange();
    bool canBridge();
    void BridgeNode(GameObject origin, GameObject bridge);
}

public interface IConnectable
{
    
    void ConnectNode(GameObject connectTo);

    void ConsumePower();

    GameObject GetCoreNode();

    bool PowerChecked();
}

public interface ICoreNode
{
    bool hasPower();

    int CoreNodePowerConsume(int AmountToTake);
}

