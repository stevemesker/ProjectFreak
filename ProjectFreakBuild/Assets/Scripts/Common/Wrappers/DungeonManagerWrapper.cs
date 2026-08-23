using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManagerWrapper : MonoBehaviour
{
    public void EnterDungeon(int dungeonID)
    {
        DungeonManager._DM.EnterDungeon(dungeonID);
    }
}
