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
            print(gameObject.name + " is where we need to enter from!");
            if (_spawnLocator == null) SceneManagerObject._SceneManager.movePlayerToLocation(_spawnLocationPosition, gameObject.transform.rotation);
            else SceneManagerObject._SceneManager.movePlayerToLocation(_spawnLocator.transform.position, _spawnLocator.transform.rotation);
        }
    }
}
