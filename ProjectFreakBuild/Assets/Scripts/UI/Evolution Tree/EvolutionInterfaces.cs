using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBridgeable
{
    float getMaxRange();
    bool canBridge();
    void BridgeNode(GameObject origin, GameObject bridge);
    void disconnectNodes(GameObject nodeToDisconnect);
}

public interface IConnectable
{
    
    void ConnectNode(GameObject connectTo);

    void DisconnectNodeTree();

    void ConsumePower();

    GameObject GetCoreNode();

    bool PowerChecked(bool CoreHide);

    bool SearchCore(GameObject Origin);

    bool testLength(Vector3 position, Vector3 originPosition);
}

public interface ICoreNode
{
    bool hasPower();

    int CoreNodePowerConsume(int AmountToTake);

    void ReturnPower(int Power);
}

public interface iEvolutionNode
{
    bool isPlugged();
    void PlugElement(GameObject ElementToPlug);
}

