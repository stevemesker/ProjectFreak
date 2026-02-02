using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager _craftingManager;

    private void Awake()
    {
        if (_craftingManager != null)
        {
            print("Game manager already exists, deleting " + gameObject.name);
            Destroy(gameObject);
            return;
        }
        _craftingManager = this;
        print("Setting Game Manager to " + gameObject.name);
    }
}
