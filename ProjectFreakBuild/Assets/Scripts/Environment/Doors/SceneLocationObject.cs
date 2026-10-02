using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLocationObject : MonoBehaviour
{
    public SceneLocationSO _LocationData;
    public GameObject _spawnLocator;
    public Vector3 _spawnLocationPosition;

    private void Awake()
    {
        if (_LocationData == null)
        {
            Debug.LogWarning("Warning! Location data has not been assigned for game object " + gameObject.name + "!");
        }
        if (SceneManagerObject._SceneManager == null) return;
        if (SceneManagerObject._SceneManager.TestDoorEntranceTarget(_LocationData))
        {
            if (_spawnLocator == null) SceneManagerObject._SceneManager.MovePlayerToLocation(_spawnLocationPosition, gameObject.transform.rotation);
            else SceneManagerObject._SceneManager.MovePlayerToLocation(_spawnLocator.transform.position, _spawnLocator.transform.rotation);
        }
    }
}
