## Overview
Dungeon doors connect one [[Dungeon Floor]] to the next. A floor scene doesn't know what it connects to, so doors are spawned at runtime by **door spawners** that ask the [[Dungeon Manager]] where they lead.

Two scripts make this work:
- **`DungeonDoorSpawnerObject`** - placed in the floor scene where a door can go
- **`DungeonDoor`** - on the spawned door prefab. Holds the destination and handles color and travel

For the design side see [[Dungeon Floor]].

---
## DungeonDoorSpawnerObject

### Inspector Data

| Variable          | Description                                                          |
| :---------------- | :------------------------------------------------------------------- |
| `_DoorSpawn`      | Door prefab (must have `DungeonDoor`)                                |
| `_NullDoorSpawn`  | Blocked door prefab for spots with no connection                     |
| `_DMWrapper`      | `DungeonManagerWrapper` used to ask for the current node             |
| `_CurrentDoorSpawned` / `_currentRoomData` / `_nextRoomData` | Runtime data, shown for debugging |

### How doors get matched to connections
All spawners in the scene add themselves to a static list (`_CurrentDoors`) in `OnEnable`. A spawner's position in that list decides which of the current node's connections it gets:

```text
Spawner 0 → _NodeConnections[0]
Spawner 1 → _NodeConnections[1]
Spawner 2 → _NodeConnections[2]
Spawner 3 → (no connection) → null door
```

The order comes from the order the spawners are enabled, which usually follows the scene hierarchy order.

### Flow (`OnEnable`)
1. Add self to `_CurrentDoors`
2. Get the current node from the wrapper. If there isn't one (testing a scene on its own), stop
3. If the node has fewer connections than there are spawners so far, spawn a **null door** and stop
4. Otherwise get the connected node, spawn the door, and call `ApplyNodeData()` on it

---
## DungeonDoor

### Inspector Data

| Variable         | Description                                                                          |
| :--------------- | :----------------------------------------------------------------------------------- |
| `_NextRoomNode`  | The [[Dungeon Map Node]] this door leads to                                          |
| `_ColorPalette`  | Taken from the next node, so the door matches the map                                |
| `_DMWrapper`     | Wrapper used to travel. Added automatically if missing                               |
| `_TpLocator`     | Where the player stands when arriving back through this door                         |
| `_DoorRenderers` | Renderers tinted with the door color                                                 |

### `ApplyNodeData(DungeonMapNode data)`
1. Stores the destination node
2. **Return teleport:** if the destination is the room the player just came from (`DungeonManager._PreviousRoomNode`), the player is moved to `_TpLocator`. This puts the player in front of the door they came through instead of at the floor's spawn point
3. Copies the node's color palette and applies it
4. Makes sure a `DungeonManagerWrapper` exists

### Door Color
The primary color of the palette is written to the `_DoorColor` shader property on each renderer using a `MaterialPropertyBlock`. This changes the color per door without creating new materials.

### `EnterNewDungeonScene()`
Moves to the next floor through the wrapper (`MoveToDungeonRoom`). This should be hooked up to the door's interaction, for example through an [[Interaction Object]] UnityEvent.

---
## Notes
- Floors need enough spawners for the most connected node (3 by default). Entrance and boss scenes need one per column. See [[Dungeon Creation]]
