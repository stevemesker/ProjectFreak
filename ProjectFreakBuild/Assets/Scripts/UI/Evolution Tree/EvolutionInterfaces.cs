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

    //called from core node to figure out what nodes have enough power
    void ConsumePower();

    //checks if removal of node removes it from the network
    //void DisconnectNode();

    //used to return core node
    GameObject GetCoreNode();

    bool PowerRequired();
}

