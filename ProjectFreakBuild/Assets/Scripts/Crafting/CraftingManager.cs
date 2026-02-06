using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager _craftingManager;

    [TitleGroup("===Base Data===")]
    public GameObject itemDragObjectReference;
    public GameObject UtensilObjectReference;
    public GameObject ListReference;

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
