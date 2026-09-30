## Overview
`DungeonManager` runs the current [[Dungeon]] at runtime. It starts dungeons, builds the dungeon map, moves the player between [[Dungeon Floor]]s, and answers "what POI goes here?" questions for spawners in the floor scene.

It lives on the Game Manager object (see [[AA - Managers]]) and is reached with `DungeonManager._DM`. Scene objects should use `DungeonManagerWrapper` instead (see [[Manager Wrappers]]).

For the bigger picture see [[Dungeon Architecture]]. For making a new dungeon see [[Dungeon Creation]].

---
## Inspector Data

**Current Data** (runtime)

| Variable            | Description                                                                                  |
| :------------------ | :------------------------------------------------------------------------------------------- |
| `_CurrentDungeon`   | The `DungeonSO` currently being played. `null` means the player isn't in a dungeon          |
| `_CurrentRoomID`    | ID of the [[Dungeon Map Node]] the player is currently on                                    |
| `_PreviousRoomNode` | The node the player just came from. Doors use this to place the player at the right door     |
| `_CurrentDungeonMap` | The spawned map prefab (holds the [[Dungeon Map Manager]])                                  |
| `_CurrentMapLocator` | The "you are here" marker on the map                                                        |

**Dungeon Chapter Settings**

| Variable              | Description                                                                                   |
| :-------------------- | :-------------------------------------------------------------------------------------------- |
| `_DungeonChapterData` | Master list of every dungeon in the game. Each entry is a `DungeonChapterData` (chapter number + `DungeonSO`). A dungeon only exists if it's in this list |

**Dungeon Settings**

| Variable                 | Description                                                         |
| :----------------------- | :------------------------------------------------------------------ |
| `_MapPrefab`             | Prefab containing the [[Dungeon Map Manager]] UI                    |
| `_NodePrefab`            | Map node prefab                                                     |
| `_BridgePrefab`          | Map connection line prefab                                          |
| `_LocatorPrefab`         | "You are here" marker spawned on the map                            |
| `_DungeonTypeTranslator` | `DungeonTypeTranslatorSO` that turns a `POIType.Type` into a map icon (see [[POI System]]) |

---
## Flow

### Startup
`Start()` sets the singleton and calls `BuildDungeonDictionaries()`, which has every `DungeonSO` in `_DungeonChapterData` build its POI lookup dictionaries (see [[POI System]]).

### Entering a dungeon — `EnterDungeon(int dungeonID)`
1. Sets `_CurrentDungeon` from `_DungeonChapterData[dungeonID]`
2. Sets `_CurrentRoomID` past the last normal node (the entrance node's ID)
3. Spawns `_MapPrefab` and calls `StartNewMap()` on its [[Dungeon Map Manager]]
4. When the map finishes generating, it calls `MoveToFloor()` with the entrance node, which loads the entrance scene

### Moving between floors — `MoveToFloor(int floorID)`
1. Gets the target [[Dungeon Map Node]]
2. If the node has no `_FloorSceneName` yet, a random scene is picked from the dungeon's `_DungeonFloorList`. *Note: the picked scene isn't saved back onto the node yet, so revisiting may give a different floor*
3. Stores the current node as `_PreviousRoomNode` and updates `_CurrentRoomID`
4. Moves the map locator to the new node
5. Requests a HUD fade-in and changes scene through the [[Scene Manager]]

### Finishing — `CompleteDungeon(string returnMap)`
Clears the current dungeon, destroys the map, and loads `returnMap`. Rewards are still a TODO.

### Map toggle
The manager listens to the `OptionsMenu` input and calls `ToggleMap()` on the [[Dungeon Map Manager]].

---
## Tools / Queries

| Function                        | Description                                                                          |
| :------------------------------ | :----------------------------------------------------------------------------------- |
| `getMapNode(int ID)`            | Returns the [[Dungeon Map Node]] with that ID (clamps to the last node if too high)  |
| `getCurrentDungeonFloorID()`    | Returns `_CurrentRoomID`                                                             |
| `setDungeonLocator(GameObject)` | Sets the map locator object                                                          |
| `getPOIFromCurrentRoom(size)`   | Asks the current `DungeonSO` for a POI that fits the current node's type and the requested size. Used by `POISpawnerObject` |

---
## Related
- [[Dungeon Map Manager]] / [[Dungeon Map Node]]
- [[Dungeon Door]]s
- [[POI System]]
- [[Manager Wrappers]]
