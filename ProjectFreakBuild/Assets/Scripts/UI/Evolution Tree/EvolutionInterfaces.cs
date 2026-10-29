using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//something lines can be drawn to and from on the dungeon map (DungeonMapNode)
//the old 2D rune field's interfaces (IConnectable, ICoreNode, iEvolutionNode) were removed with it on Oct 28, 2026
public interface IBridgeable
{
    float GetMaxRange();
    bool CanBridge();
    void BridgeNode(GameObject origin, GameObject bridge);
    void DisconnectNodes(GameObject nodeToDisconnect);
    void LoadReconnect();
    void ConnectNode(GameObject connectTo);
}
