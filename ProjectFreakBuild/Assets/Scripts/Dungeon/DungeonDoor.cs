using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonDoor : MonoBehaviour
{
    public DungeonMapNode _NextRoomNode;
    //public string _SceneName;
    [SerializeField] DungeonManagerWrapper _DMWrapper;

    public void ApplyNodeData(DungeonMapNode data)
    {
        _NextRoomNode = data;
        //if (_NextRoomNode._FloorSceneName != "") _SceneName = _NextRoomNode._FloorSceneName;
        if (_DMWrapper == null) gameObject.GetComponent<DungeonManagerWrapper>();
    }

    public void EnterNewDungeonScene()
    {
        _DMWrapper.MoveToDungeonRoom(_NextRoomNode._ID);
    }
}
