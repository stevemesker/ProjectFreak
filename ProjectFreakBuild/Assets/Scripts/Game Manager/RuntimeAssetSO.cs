using UnityEngine;

[CreateAssetMenu(fileName = "RuntimeSettings", menuName = "Game/Runtime Settings")]

public class RuntimeAssetSO : ScriptableObject
{
    //script that allows the bootstrapper class to know what assets need to be spawned in before the game starts
    //example: the game manager
    [SerializeField] private GameObject[] _persistentPrefabs;
    public GameObject[] PersistentPrefabs => _persistentPrefabs;
}
