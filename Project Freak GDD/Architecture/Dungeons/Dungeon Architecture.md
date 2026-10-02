[[Dungeon]]s are semi procedural linked environments where the majority of combat is held. They're controlled by a tiered data structure, starting with the [[Dungeon Manager]] down to the [[Dungeon Floor]] and the [[POI]]s inside it.

***For information on creating a dungeon from scratch see: [[Dungeon Creation]]***

---
## Overview

```text
DungeonManager (runtime, on the Game Manager)
    │
    ├── _DungeonChapterData → DungeonSO (one per dungeon)
    │
    └── Dungeon Map (DungeonMapManager, spawned per run)
            │
            └── Dungeon Map Nodes (one per floor)
                    │
                    └── Dungeon Floor scene (loaded when entered)
                            │
                            ├── DungeonDoorSpawnerObjects → Dungeon Doors
                            └── POISpawnerObjects → POI prefabs
```

**Each map node is one floor.** A dungeon run is a web of floors. The player moves node to node by walking through doors on each floor.

---
## Dungeon Manager
The dungeon manager's responsibility is maintaining all dungeon data and running the current dungeon. A dungeon only exists if its data is in the manager's `_DungeonChapterData` list. See [[Dungeon Manager]].

### Dungeon Data
`_DungeonChapterData` - The master list of `DungeonChapterData`. Each entry holds a chapter number and a `DungeonSO`.

### Current Dungeon Tracking
`_CurrentDungeon` - When null it means the save manager does not need to worry about saving/loading dungeon data as the player has finished their latest dungeon and exists in a hub world of some kind.

### Realtime Systems
The manager spawns the map, moves the player between floors, and answers POI requests from floor scenes.

---
## Dungeon SO
The data definition for a single dungeon. It holds:
- Map shape (column count, row count, node wiggle)
- Entrance and boss scene names
- The list of floor scenes normal nodes can use
- Per-column node type weights (how often each `POIType.Type` shows up)
- The POI list and the type → tag lookup table (see [[POI System]])
- Enemy and loot tables (*not built yet*) [[Notes for the future]]

---
## Dungeon Map
Generated fresh for each run by the [[Dungeon Map Manager]]. A grid of [[Dungeon Map Node]]s gets an entrance and boss node, extra connections by distance, a type and icon for each node, and a color per node. See those notes for details.

---
## Dungeon Floor
A scene file with a room layout. Normal nodes pick a random scene from the `DungeonSO`'s floor list the first time they're entered. The entrance and boss nodes always use their own scenes.

Floor scenes don't know anything about the dungeon. They contain:
- **Door spawners** that ask the current node where each door leads (see [[Dungeon Door]]s)
- **POI spawners** that ask for a POI of a set size, matching the current node's type (see [[POI System]])
- A player spawn point (see [[Scene Manager]])
