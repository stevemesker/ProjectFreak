## Overview

The Runtime Bootstrapper is responsible for automatically creating persistent runtime systems when the game starts. It works alongside the `RuntimeAssetSO`, which contains a list of prefabs that need to exist before the game begins.

This allows gameplay scenes to remain clean and self-contained without requiring persistent systems such as the Game Manager to be manually placed in every scene.

## Bootstrapper

`Bootstrapper` is a static class that uses Unity's `RuntimeInitializeOnLoadMethod` to execute before the first scene is loaded.

It:

1. Loads the `RuntimeSettings` asset from the `Resources/Runtime/` folder.
2. Reads the list of persistent prefabs from the asset.
3. Instantiates each prefab.

```csharp
using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        RuntimeAssetSO settings = Resources.Load<RuntimeAssetSO>("Runtime/RuntimeSettings");

        if (settings == null)
        {
            Debug.LogError("RuntimeSettings could not be found in Resources/Runtime/");
            return;
        }

        foreach (GameObject prefab in settings.PersistentPrefabs)
        {
            if (prefab != null)
            {
                Object.Instantiate(prefab);
            }
        }
    }
}
```

## RuntimeAssetSO

`RuntimeAssetSO` is a `ScriptableObject` that acts as the configuration asset for the Bootstrapper. Its `PersistentPrefabs` array contains any prefabs that should automatically be instantiated when the game starts.

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "RuntimeSettings", menuName = "Game/Runtime Settings")]
public class RuntimeAssetSO : ScriptableObject
{
    [SerializeField] private GameObject[] _persistentPrefabs;

    public GameObject[] PersistentPrefabs => _persistentPrefabs;
}
```

The asset is stored at:

```text
Resources/
└── Runtime/
    └── RuntimeSettings.asset
```

### Adding New Persistent Systems

To add another system that should automatically exist at runtime:

1. Create the system's prefab.
2. Add the prefab to the `Persistent Prefabs` array on `RuntimeSettings`.
3. No changes to the Bootstrapper are required.

This keeps the startup process centralized while allowing the list of required runtime systems to be configured through the Unity Inspector.

## Workflow

The resulting startup flow is:

```text
Game starts
    ↓
Bootstrapper.Initialize()
    ↓
Load RuntimeSettings
    ↓
Read Persistent Prefabs
    ↓
Instantiate runtime systems
    ↓
First scene loads
```

Persistent systems should **not** be manually placed in individual scenes. This allows any scene to be opened and tested independently while still receiving the same runtime infrastructure used by the final game.

## Notes

* `Bootstrapper` does not need to be attached to a GameObject.
* `RuntimeAssetSO` is stored in `Resources` so the static Bootstrapper can locate it at startup.
* Only runtime configuration assets and required runtime prefabs should be placed in `Resources`; normal game assets should remain outside of it.
* The Bootstrapper is intentionally unaware of what individual systems do. It only handles creating the objects defined by `RuntimeSettings`.
