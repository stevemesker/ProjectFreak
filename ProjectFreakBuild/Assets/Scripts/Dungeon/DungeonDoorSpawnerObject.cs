using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DungeonDoorSpawnerObject : MonoBehaviour
{
    public static List<DungeonDoorSpawnerObject> _CurrentDoors;
    [SerializeField] GameObject _DoorSpawn;
    [SerializeField] GameObject _NullDoorSpawn;
    [SerializeField] DungeonManagerWrapper _DMWrapper;

    [Header("Dynamic Data")]
    [SerializeField] GameObject _CurrentDoorSpawned;
    [SerializeField] DungeonMapNode _currentRoomData;
    [SerializeField] DungeonMapNode _nextRoomData;

    private void OnEnable()
    {
        //add self to static list
        if (_CurrentDoors == null) _CurrentDoors = new List<DungeonDoorSpawnerObject>();
        _CurrentDoors.Add(this);

        //test list of connections
        _currentRoomData = _DMWrapper.GetCurrentMapNode();
        if (_currentRoomData == null)
        {
            //this is just testing the scene
            return;
        }
        if (_currentRoomData._NodeConnections.Count < _CurrentDoors.Count)
        {
            print($"Not enough connections for door {gameObject.name}. Adding Null door...");
            _CurrentDoorSpawned = Instantiate(_NullDoorSpawn, transform.position, transform.rotation, gameObject.transform);
            return;
        }

        //get data
        _nextRoomData = _DMWrapper.GetCurrentMapNode()._NodeConnections[_CurrentDoors.IndexOf(this)].GetComponent<DungeonMapNode>();
        print($"{_nextRoomData.gameObject.name} contains all the data I need for the room {gameObject.name} requires...");
        _CurrentDoorSpawned = Instantiate(_DoorSpawn, transform.position, transform.rotation, gameObject.transform);
        _CurrentDoorSpawned.GetComponent<DungeonDoor>().ApplyNodeData(_nextRoomData);
    }
    private void OnDisable()
    {
        _CurrentDoors.Remove(this);
    }
}
