This section describes the process for creating a new dungeon from scratch, matching how the dungeon system currently works. For how the pieces fit together see [[Dungeon Architecture]].

A dungeon is built from these pieces:

```text
DungeonSO  ─────────────── "What dungeon am I creating?"
    ├── Map settings ───── "How big is the map?"
    ├── Floor pool types ─ "What kinds of nodes show up in each column?"
    ├── Floor scenes ───── "What layouts can a floor use?"
    └── POI list + LUT ─── "What content can fill those layouts?"
          ↓
Dungeon Manager (_DungeonChapterData) ── "Register it so the game can find it"
```

Build from the top down: set up the dungeon's data first, then the floor scenes, then the POIs that fill them.

---

# Dungeon Creation Workflow

1. Create the `DungeonSO`
2. Configure map settings
3. Configure node type weights per column
4. Build the entrance, boss, and floor scenes
5. Add door spawners to each floor
6. Add POI spawners to each floor
7. Create the POIs
8. Set up the type → tag lookup table
9. Fill the `DungeonSO` lists
10. Register the dungeon with the Dungeon Manager
11. Hook up a way to enter the dungeon
12. Test

---

# 1. Create the DungeonSO

Create it from **Create → Dungeon → Dungeon**.

Name it following [[File Naming Conventions]]:

```text
SO_Dungeon_DungeonName_0
```

---

# 2. Configure Map Settings

| Field                    | Description                                                                   |
| :----------------------- | :---------------------------------------------------------------------------- |
| `_DungeonName`           | Readable name                                                                 |
| `_DungeonColumnCount`    | Number of paths (columns) on the map. Default 3                               |
| `_DungeonRowCount`       | Number of floors in each column. Default 10                                   |
| `_DungeonMapNodeWiggle`  | How far nodes can be randomly offset so the map doesn't look like a grid      |
| `_DungeonEntranceSceneName` | Scene used for the entrance node                                           |
| `_DungeonBossSceneName`  | Scene used for the boss node                                                  |

Total floors on the map = columns × rows, plus the entrance and boss.

---

# 3. Configure Node Type Weights

Each column has its own weighted "deck" of node types (see [[Dungeon Map Manager]] for how the deck works).

- `_DungeonFloorPoolSize` - how many cards are in each column's deck (default 36)
- `_DungeonFloorPoolTypes` - a list with **one entry per column**. Each entry has a list of `POIType.Type` + chance (%)

Anything left over after the percentages is filled with `Basic`.

> **Important:** `_DungeonFloorPoolTypes` must have at least as many entries as `_DungeonColumnCount`, or map generation will error.

---

# 4. Build the Scenes

Every dungeon needs:
- **Entrance scene** - small hub with the first doors (see [[Dungeon Entrance]])
- **Boss scene**
- **Floor scenes** - the room layouts normal nodes pick from at random

Name scenes following [[File Naming Conventions]] (`SCN_...`).

All of these must be added to **Build Settings**, since they're loaded by name.

Each floor scene needs a player spawn point (`PlayerSpawnPointObject`, see [[Scene Manager]]).

---

# 5. Add Door Spawners

Place a `DungeonDoorSpawnerObject` wherever a door can go. See [[Dungeon Door]] for details.

- Each spawner is matched to one of the current node's connections, in the order the spawners are enabled
- A node has up to 3 connections by default, so give each floor **at least 3 door spots**
- The entrance and boss scenes connect to the first/last floor of every column, so they need **one door per column**
- Spots beyond the node's connection count get a null door

Assign the door prefab, null door prefab, and a `DungeonManagerWrapper` on each spawner.

---

# 6. Add POI Spawners

Place a `POISpawnerObject` wherever a POI should go. See [[POI System]] for details.

- Set `_SpawnerSize` (Tiny → Huge)
- Use the **UpdateSizeVolume** button to preview how much space that size takes up, and **ToggleVolume** to show/hide it. The preview is hidden automatically when the game runs
- POIs spawn at the spawner's position with no rotation, so build POIs facing the default direction

---

# 7. Create the POIs

For each POI:
1. Build the prefab (following [[File Naming Conventions]], `PFB_...`)
2. Create a `DungeonPOISO` from **Create → Dungeon → POI** (default name `SO_POI_DungeonName_Size_Type`)
3. Assign `_POI_Prefab`
4. Set `_POI_Size` to match the space it needs
5. Add `_POI_Tags` describing the content (chests, gold, enemies, healing, etc.). The **Fill tags with all** and **Clear all tags** buttons help when a POI fits most tags

---

# 8. Set Up the Type → Tag Lookup Table

Create a `TypeTagLookUpSO` from **Create → Dungeon → Dungeon Type Tag LUT**.

For each node type (`POIType.Type`), add an entry listing which tags it wants and how often (%). For example, a `Treasure` node might pull `chests` 60% and `gold` 40%.

`_EntryPoolSize` sets the deck size, just like the node type weights. Use **Clear All Pools** to reset the decks while testing.

See [[POI System]] for how the lookup works.

---

# 9. Fill the DungeonSO Lists

- **Floor scenes:** drag the floor scene assets into `_SceneAdd`, press **Fill Floor List**. This copies their names into `_DungeonFloorList` and clears `_SceneAdd`
- **POIs:** add every `DungeonPOISO` for this dungeon to `POIList`
- **LUT:** assign the `TypeTagLookUpSO` to `POILUT`
- **Tables:** `LootTable` and `EnemyTable` exist but those systems aren't built yet

**Clear All Lists** empties the floor list and POI list.

---

# 10. Register with the Dungeon Manager

On the Game Manager prefab, add a new entry to the [[Dungeon Manager]]'s `_DungeonChapterData`:
- `DungeonChapter` - the story chapter
- `_DungeonData` - your `DungeonSO`

The entry's index in the list is the dungeon ID used to enter it.

---

# 11. Enter the Dungeon

Something in the world needs to call `DungeonManagerWrapper.EnterDungeon(dungeonID)`, usually an interactable door hooked up through an [[Interaction Object]] UnityEvent. See [[Manager Wrappers]].

To leave, call `DungeonManagerWrapper.EndDungeon(returnScene)`.

---

# 12. Testing Checklist

### Dungeon Data
- [ ] `DungeonSO` created and named correctly
- [ ] Entrance and boss scene names set
- [ ] `_DungeonFloorPoolTypes` has an entry for every column
- [ ] Floor list filled
- [ ] POI list and LUT assigned
- [ ] Registered in `_DungeonChapterData`

### Scenes
- [ ] All scenes in Build Settings
- [ ] Each floor has a player spawn point
- [ ] Each floor has at least 3 door spawners
- [ ] POI spawners have sizes set

### POIs
- [ ] Every POI has a prefab, size, and tags
- [ ] Every node type in the LUT has at least one POI for each spawner size used. If a size or tag has no POIs at all, the lookup currently throws an error instead of failing gracefully (see [[Known Issues]])

### Gameplay
- [ ] Map generates without errors
- [ ] Doors lead to the right nodes and match the map colors
- [ ] Going back through a door puts the player in front of the right door
- [ ] POIs match each node's type

---

# Important Design Rule

When creating dungeon content, remember the distinction between **data, structure, and physical content**.

```text
DungeonSO
    ↓
"What dungeon am I creating?"

Dungeon Map
    ↓
"Where can the player go?"

Node Type
    ↓
"What kind of experience belongs here?"

POI
    ↓
"What physical space provides that experience?"

Spawn Points
    ↓
"Where does the gameplay happen?"

Enemy / Loot Systems (not built yet)
    ↓
"What actual content appears?"
```

Keeping these responsibilities separate is what allows the dungeon system to remain flexible.
