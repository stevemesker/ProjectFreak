using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPointObject : MonoBehaviour
{
    private void Awake()
    {
        if (Player.player == null) return;
        movePlayerToSpawner();
    }

    public void movePlayerToSpawner()
    {
        Player.player.transform.position = gameObject.transform.position;
        Player.player.transform.rotation = gameObject.transform.rotation;
    }
}
