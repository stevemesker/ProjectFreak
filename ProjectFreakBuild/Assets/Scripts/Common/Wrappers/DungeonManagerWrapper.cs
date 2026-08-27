using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManagerWrapper : MonoBehaviour
{
    public void EnterDungeon(int dungeonID)
    {
        if (ManagerTester("EnterDungeon") == false) return;
        DungeonManager._DM.EnterDungeon(dungeonID);
    }

    public void MoveToDungeonRoom(int ID)
    {
        if (ManagerTester("MoveToDungeonRoom") == false) return;
        DungeonManager._DM.MoveToFloor(ID);
    }

    public DungeonMapNode GetCurrentMapNode()
    {
        if (ManagerTester("GetCurrentMapNode") == false) return null;
        return DungeonManager._DM.getMapNode(DungeonManager._DM.getCurrentDungeonFloorID());
    }

    public int GetCurrentFloorID()
    {
        if (ManagerTester("GetCurrentFloorID") == false) return 0;
        return DungeonManager._DM.getCurrentDungeonFloorID();
    }

    public DungeonMapNode GetMapNodeByID(int ID)
    {
        if (ManagerTester("GetMapNodeByID") == false) return null;
        return DungeonManager._DM.getMapNode(ID);
    }

    bool ManagerTester(string type)
    {
        if (DungeonManager._DM == null) { Debug.LogError($"{type} Wrapper ERROR! Dungeon Manager not found..."); return false; }
        return true;
    }
}
