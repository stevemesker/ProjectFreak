using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player player;
    public GameObject camTarget;

    private void Start()
    {
        if (Player.player != null) { Destroy(gameObject); return; }
        Player.player = this;
    }
}
