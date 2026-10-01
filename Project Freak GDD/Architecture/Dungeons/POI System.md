## Overview
The POI system fills [[Dungeon Floor]] scenes with [[POI]]s that fit the current node's type and the space available.

```text
POISpawnerObject (in floor scene)
    │  "I need a Medium POI"
    ▼
DungeonManager.GetPOIFromCurrentRoom(size)
    │  adds the current node's type
    ▼
DungeonSO.GetPOI(type, size)
    │
    ├── TypeTagLookUpSO: type → tag (weighted deck)
    │
    └── Find POIs that have that size AND that tag, pick one at random
    ▼
POI prefab spawned at the spawner
```

For step by step setup see [[Dungeon Creation]].

---
## POIType (enums)
All POI enums live in the `POIType` namespace.

| Enum           | Values                                                                                      |
| :------------- | :------------------------------------------------------------------------------------------ |
| `POIType.Type` | Basic, Boss, MiniBoss, Entrance, Treasure, Vault, Enemy, Safehouse, Unique. The node's gameplay role |
| `POIType.Tag`  | chests, gold, easyEnemies, MediumEnemies, HardEnemies, Structure, Security, healing, special. Describes a POI's content |
| `POIType.Size` | Tiny, Small, Medium, Large, Huge                                                            |

*`POIShape.Shape` (square, L) also exists but isn't used yet.*

---
## DungeonPOISO
One asset per POI. Created from **Create → Dungeon → POI**.

| Variable      | Description                        |
| :------------ | :--------------------------------- |
| `_POI_Prefab` | The prefab to spawn                |
| `_POI_Size`   | Space it needs                     |
| `_POI_Tags`   | What kind of content it has        |

Buttons: **Fill tags with all** and **Clear all tags**.

---
## TypeTagLookUpSO (LUT)
Decides which tag a node type asks for. Created from **Create → Dungeon → Dungeon Type Tag LUT**.

- `_EntryPoolSize` - deck size (default 36)
- `_POIType` - one entry per `POIType.Type`, each with a list of tags + chance (%)

Each type has its own weighted deck, built the same way as the map's node type decks (see [[Dungeon Map Manager]]): every tag gets copies based on its percentage (at least 1), tags are drawn and removed, and the deck refills when empty. If a type isn't in the LUT, the first entry is used and an error is logged.

**Clear All Pools** resets every deck.

---
## DungeonSO lookup
When a dungeon starts, `BuildPOIDictionaries()` sorts the dungeon's `POIList` into two dictionaries:
- by size - `_POISizeDictionary`
- by tag - `_POITagDictionary` (a POI with several tags is in several lists)

`GetPOI(type, size)`:
1. Gets the list of POIs with that size
2. Draws a tag for the type from the LUT
3. Finds POIs that are in both lists and picks one at random
4. If none match, draws another tag and tries again, up to `_EntryPoolSize` times
5. If nothing ever matches, logs an error and returns null

*If a size or tag has no POIs at all, the lookup currently errors instead of returning null. See [[Known Issues]].*

---
## POISpawnerObject
Placed in floor scenes where a POI should appear.

| Variable              | Description                                                            |
| :-------------------- | :--------------------------------------------------------------------- |
| `_SpawnerSize`        | Size of POI to request                                                 |
| `_CurrentPOI` / `_SpawnedPOI` | What was picked and spawned (debug)                            |
| `_SizeVolume`         | Preview box showing the space this size takes up                       |
| `_CellSizePerMeter`   | Size of one cell in meters (default 4)                                 |
| `_ScaleFactorByIndex` | How many cells wide each size is, starting with Tiny at index 0        |

On `Start` it hides the preview box. Spawning happens in `SpawnPOI()`, which asks the [[Dungeon Manager]] for a POI and spawns it at its position (no rotation):
- **On a floor with a [[Dungeon Floor Object]]**, the spawner registers with `DungeonFloorObject._Floor` in its `Start()`, and the floor calls `SpawnPOI()` during loading so the NavMesh can be built right after.
- **Without one**, the spawner calls `SpawnPOI()` in its own `Start()` like before.

`SpawnPOI()` logs a warning and spawns nothing if there's no Dungeon Manager, no current dungeon (e.g. playing the floor scene directly) or no matching POI. It won't spawn twice.

Editor buttons: **UpdateSizeVolume** resizes the preview box for the current size; **ToggleVolume** shows/hides it.

*Planned:* remembering which POIs were spawned so revisited floors stay the same.

---
## DungeonTypeTranslatorSO
Turns a `POIType.Type` into its map icon (`Sprite`) and texture. Falls back to `_DefaultTypePackage` for types without an entry. Used by the [[Dungeon Map Manager]] for node icons.
