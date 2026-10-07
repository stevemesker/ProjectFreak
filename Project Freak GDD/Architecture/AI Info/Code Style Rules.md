Rules Claude follows when writing code for Project Freak so it matches my style and stays readable. Based on my recent code (Dungeon, Radial Menu and Ability systems). Older scripts that break these rules will get cleaned up later.

Back to [[AA - AI Info]]

---
## Project Layout
- Unity project: `Build\ProjectFreak\ProjectFreakBuild` (scripts in `Assets\Scripts\<System>\`)
- GDD: `Build\ProjectFreak\Project Freak GDD`. Claude checks the system's note in Architecture before working on it
- Plans and to-do lists for upcoming work go in `Architecture\AI Info` (listed in [[AA - AI Info]]). Once a piece is built, it's written up in its system's note in Architecture
- Claude doesn't rename, move or delete GDD notes. It updates the contents and links under the new name, then lists the notes for me to retitle in Obsidian (which fixes the links) or delete
- In the GDD, any line about something unfinished, temporary or planned ends with `[[Notes for the future]]`. When it gets done, the tag comes off and the line is updated
- `Art` folders are reference only. Claude doesn't change them unless asked
- **GDD notes outside `Architecture`** (design, narrative, shade ideas, etc.) are treated the same way: Claude reads them for reference when building systems but doesn't change them unless asked. The one exception is the `[[Notes for the future]]` hub note at the GDD root, which the tagging system uses
- Packages: Odin Inspector, new Input System (`PlayerInput`), Cinemachine 2.x, TextMeshPro

---
## Naming

| Thing                                          | Style                  | Example                               |
| :--------------------------------------------- | :--------------------- | :------------------------------------ |
| Public and `[SerializeField]` fields           | `_PascalCase`          | `_CurrentRoomID`, `_DoorRenderers`    |
| Private runtime fields (not in inspector)      | `_camelCase`           | `_currentTimer`, `_propertyBlock`     |
| Methods (public or private, getters/setters)   | PascalCase             | `GetMapNode()`, `SetDungeonLocator()` |
| Locals and parameters                          | camelCase              | `bridgeInstance`, `colorToTest`       |
| Singleton static                               | short `_Name`          | `DungeonManager._DM`, `HUDManager._HUD` |
| Acronyms                                       | all caps               | `POI`, `ID`, `LUT`, `HUD`             |

### Class suffixes

| Suffix    | Meaning                                                   | Example            |
| :-------- | :-------------------------------------------------------- | :----------------- |
| `SO`      | ScriptableObject                                          | `DungeonSO`        |
| `Manager` | Singleton manager on the Game Manager object              | `DungeonManager`   |
| `Wrapper` | Scene-side component that forwards calls to a manager     | `DungeonManagerWrapper` |
| `Object`  | MonoBehaviour placed in a scene                           | `POISpawnerObject` |
| `Package` | Serializable bundle of data passed around                 | `DamagePackage`    |
| `Entry`   | Serializable item in a list                               | `TypeEntry`        |

### Other naming
- Enums are grouped in a namespace used as a category, in a file named `<Category>Type.cs` (`POIType.Size`, `DamageType.StatType`)
- Interfaces for one system can share a file named `<System>Interface.cs`
- `CreateAssetMenu` uses the asset naming convention and a system menu: `fileName = "SO_Dungeon_Name", menuName = "Dungeon/Dungeon"`
- Asset names follow [[File Naming Conventions]]

---
## Class Layout
1. Singleton static (if any)
2. Inspector fields in `[Header]` groups: **Data → Settings → References → Runtime Data**
3. Every inspector field gets a `[Tooltip]`
4. `[SerializeField]` fields are written without `private`
5. Private runtime fields last, under a `//local variables` comment
6. Unity lifecycle methods (`Awake`, `OnEnable`, `OnDisable`, `Start`)
7. Everything else in `#region` blocks by job: Initialize, feature regions, Tools, Test Tools, Debugging, one per interface implementation
8. Small `[System.Serializable]` helper classes go at the bottom of the file that uses them

---
## Patterns
- **Managers** all live on the single Game Manager object with a static singleton. Managers can call each other directly. Scene objects and UnityEvents reach managers through a Wrapper
- **Singleton setup** in `Awake`: destroy duplicates, then assign the static
- **Input**: create `PlayerInput`, enable and subscribe in `OnEnable`, unsubscribe and disable in `OnDisable`
- **Wrappers** check the manager exists before forwarding a call
- **Registration over searching**: scene objects announce themselves to a manager or scene singleton (e.g. `POISpawnerObject` → `DungeonFloorObject._Floor.RegisterPOISpawner(this)`) instead of the manager using `FindObjectsOfType`. Register in `Start()` (every `Awake()` has run by then, so the singleton is set), and have the receiver wait a frame if it needs the full list
- **Scene singletons** (one per scene, like `DungeonFloorObject._Floor`) clear themselves in `OnDestroy` and only treat a second copy *in the same scene* as a mistake, since the old scene's copy can briefly overlap during a scene change
- **Interfaces** for cross-system contracts, checked with `TryGetComponent`
- **Polymorphic data** (like ability steps) uses `[SerializeReference]` lists of a base class
- **Coroutines** are stored in a `Coroutine` field, then stopped and nulled on disable or interrupt
- **Odin**: `[Button]` for editor tools and tests, `[FoldoutGroup]` for big inspectors, `[GUIColor]` for important buttons
- **Shader properties** use `static readonly int` IDs and a `MaterialPropertyBlock`

---
## Control Flow
- Guard clauses return early. One-line guards are fine
- Single-statement `if`s can skip braces. Anything longer gets braces
- Indexed `for` when the index matters, `foreach` for a simple pass
- If a `GetComponent` result is used more than once in a function, store it in a local first

---
## Readability
- Straightforward code over clever code. Avoid LINQ chains, nested lambdas and advanced C# tricks unless they make things much simpler
- When a less common feature is the right choice, add a one-line comment explaining it
- Non-trivial functions start with a short casual comment saying what they're for: `//function that refills the type pool for a column`. No XML `///` comments
- Leave honest `//temp` or `//todo` notes on placeholders

---
## Logging and Errors
- No progress `print`/`Debug.Log` spam. Only add a log when asked or when testing a new system
- **`Debug.LogError`** = something is broken and needs fixing (missing inspector reference, missing manager, data not found). Starts with `Error!`, says what's missing and where, and passes `this` so clicking the log in the Console selects the object
- **`Debug.LogWarning`** = a problem the code recovers from with a fallback. Starts with `Warning!` and names the fallback
- Required references are checked once up front (`Awake`/`Start` or top of the entry function)
- No exceptions (`throw`) for gameplay flow. `NotImplementedException` is fine for unfinished interface members

```csharp
if (_BossZone == null) { Debug.LogError($"Error! Boss zone not assigned on {gameObject.name}", this); return; }
```

---
## Example

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TrapSpawnerObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("How often the trap fires, in seconds")]
    [SerializeField] float _FireRate = 1f;

    [Header("References")]
    [Tooltip("Where projectiles spawn from")]
    [SerializeField] GameObject _SpawnLocator;

    //local variables
    Coroutine _fireTimer;

    private void OnEnable()
    {
        if (_SpawnLocator == null) { Debug.LogError($"Error! Spawn locator not assigned on {gameObject.name}", this); return; }
        _fireTimer = StartCoroutine(FireTimer());
    }

    private void OnDisable()
    {
        if (_fireTimer != null) StopCoroutine(_fireTimer);
        _fireTimer = null;
    }

    #region Firing
    IEnumerator FireTimer()
    {
        //function that fires the trap on a loop
        while (true)
        {
            yield return new WaitForSeconds(_FireRate);
            Fire();
        }
    }

    void Fire()
    {
        //temp until projectiles are hooked up
    }
    #endregion

    #region Test Tools
    [Button("Test Fire")]
    void TestFire()
    {
        Fire();
    }
    #endregion
}
```
