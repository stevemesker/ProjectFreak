using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPointObject : MonoBehaviour
{
    private void Awake()
    {
        if (Player.player == null) return;
        MovePlayerToSpawner();
    }

    public void MovePlayerToSpawner()
    {
        Player.player.transform.position = gameObject.transform.position;
        Player.player.transform.rotation = gameObject.transform.rotation;
    }
}
