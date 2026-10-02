using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBridgeable
{
    float GetMaxRange();
    bool CanBridge();
    void BridgeNode(GameObject origin, GameObject bridge);
    void DisconnectNodes(GameObject nodeToDisconnect);
    void LoadReconnect();
    void ConnectNode(GameObject connectTo);
}

public interface IConnectable
{
    void ClearConnection();

    void DisconnectNodeTree();

    void ConsumePower();

    GameObject GetCoreNode();

    bool PowerChecked(bool CoreHide);

    bool SearchCore(GameObject Origin);

    bool TestLength(Vector3 position, Vector3 originPosition);
}

public interface ICoreNode
{
    bool HasPower();

    int CoreNodePowerConsume(int AmountToTake);

    void ReturnPower(int Power);
}

public interface iEvolutionNode
{
    bool IsPlugged();
    void PlugElement(GameObject ElementToPlug);

    void ResetNode();
}

