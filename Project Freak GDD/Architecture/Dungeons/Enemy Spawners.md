## Overview
Enemy spawners place Popcorn, Basic and Lieutenant enemies on a [[Dungeon Floor]] **before the player gets there**. Each spawner is a box: it picks spread-out spots on the NavMesh inside the box and spawns one enemy right on each spot, so enemies never start stacked on each other and never visibly walk apart.

They work like the [[POI System|POI spawners]]: the spawner asks the [[Dungeon Manager]] for the current dungeon's enemy table, and the table decides *which* enemies show up. The spawner only decides *how many* and *where*.

MiniBosses and Bosses are never spawned. They're placed by hand in their arenas (see [[Enemy Movement#Ranks]]).

*Enemies that pour out of something during play (a burrow, a doorway) will be a separate **enemy emitter** system. Not built yet.* [[Notes for the future]]

Part of the [[AI Movement & Dungeon Loading Plan]].

| Piece | File | Job |
| :--- | :--- | :--- |
| `EnemySpawnerObject` | `Scripts/Dungeon/EnemySpawnerObject.cs` | The box in the floor scene. Picks spots and spawns enemies |
| `DungeonEnemyTableSO` | `Scripts/Dungeon/DungeonEnemyTableSO.cs` | The dungeon's list of enemies + weights, and count multipliers per floor type |
| `DungeonManager` | `Scripts/Game Manager/DungeonManager.cs` | Hands out the current enemy table and floor type |
| `DungeonFloorObject` | `Scripts/Dungeon/DungeonFloorObject.cs` | Tells every spawner to spawn during its `SpawningEnemies` step |

---
## Flow

```text
EnemySpawnerObject.Start → registers with DungeonFloorObject._Floor
    ▼
Floor loads: SpawningPOIs → BuildingNavMesh → SpawningEnemies
    │  calls SpawnEnemies(takenSpots) on every registered spawner
    ▼
Spawner asks DungeonManager: GetCurrentEnemyTable() + GetCurrentRoomType()
    ▼
For each rank in Spawn Counts:
    count = random(Min..Max) × table's multiplier for this floor type and rank
    for each enemy:
        table.GetEnemy(rank)  → weighted random prefab
        pick a spot (below)   → spawn the prefab on it
```

**Spawners inside POIs** register a frame after their POI spawns, which is still before the NavMesh build, so they're spawned with everything else.

**Without a floor object** (a hand-built scene with a baked NavMesh), the spawner spawns in its own `Start()`.

---
## Picking Spots
For each enemy the spawner tries up to **Tries Per Enemy** random spots. A spot is used as soon as it passes every check:
1. Pick a random point on the box's middle layer.
2. **Snap it to the NavMesh** (searching up to half the box's height). No NavMesh nearby → try again.
3. **Still inside the box?** Snapping can slide a point past the box's edge.
4. **Far enough from other enemies** (`_MinSpacing`). This includes enemies from *other* spawners on the floor, because the floor hands every spawner the same list of taken spots, so overlapping boxes are fine.
5. **Far enough from the player** (`_MinPlayerDistance`). The player has already been moved to the spawn point or door by now.
6. **Reachable** from the middle of the box (`_RequireReachable`). Uses `NavMesh.CalculatePath`, so spots on tabletops or ledges cut off from the floor are skipped.

If no spot works after all the tries, the spawner stops placing that rank and logs a warning saying how many it fit. **It never stacks enemies to make the count.**

Enemies spawn facing a random direction (or the spawner's direction with `_RandomFacing` off) and are moved into the floor's scene so they unload with it. Each enemy's own `EnemyMovement` snaps it onto the NavMesh and turns its agent on (see [[Enemy Movement]]).

---
## EnemySpawnerObject (inspector)
**Data**

| Field | Default | Description |
| :--- | :--- | :--- |
| `_SpawnCounts` | Popcorn 2–4, Basic 1–2, Lieutenant 0–1 | How many of each rank to place. A random number from Min to Max, then multiplied by the floor type multiplier. MiniBoss/Boss entries are skipped (a warning box shows) |

**Settings**

| Field | Default | Description |
| :--- | :--- | :--- |
| `_SpawnAreaSize` | 8 × 4 × 8 m | Size of the box, centered on the object. Rotate the object to turn the box. The floor must be inside the box's height |
| `_MinSpacing` | 2 m | Closest two enemies can spawn to each other |
| `_MinPlayerDistance` | 8 m | Closest an enemy can spawn to the player |
| `_RequireReachable` | On | Only use spots that can be walked to from the middle of the box |
| `_RandomFacing` | On | Random facing, or all face the spawner's way |
| `_TriesPerEnemy` | 30 | Random spots tried per enemy before giving up. Higher fits tight areas better but loads slower |
| `_GizmoColor` | Orange-red | Color of the box in the Scene view |

**Testing** (foldout)

| Field | Description |
| :--- | :--- |
| `_TestEnemyTable` | Used when not inside a dungeon, like playing the floor scene directly. Empty = spawn nothing outside a dungeon |
| `_TestFloorType` | Floor type used with the test table's multipliers. Default Basic |

**Runtime Data:** `_SpawnedEnemies` (read only).

**Buttons (play mode):** **Respawn** clears this spawner's enemies and spawns a new set. **Clear Spawned** just removes them. Handy for tuning spacing and box size.

**Gizmos:** the box always draws in the Scene view. When selected, circles show the spacing (half of `_MinSpacing` each, so two enemies at the minimum distance have touching circles). Before spawning there's one sample circle in the middle; in play mode there's one per spawned enemy.

---
## DungeonEnemyTableSO
One per dungeon, plugged into the `DungeonSO`'s **Enemy Table** slot. **Create → Dungeon → Enemy Table** (`SO_EnemyTable_DungeonName`). Read only at runtime.

| Field | Description |
| :--- | :--- |
| `_Enemies` | List of `EnemyTableEntry`: **Enemy Prefab** + **Weight**. Just drop in prefabs: each one's rank is read from its EnemySO and shown (read only) next to it. Weight 2 is picked twice as often as weight 1 among enemies of the same rank, 0 = never |
| `_FloorTypeMultipliers` | List of `FloorTypeMultiplierEntry`: a **Floor Type** (`POIType.Type`) and a list of **Rank + Multiplier**. e.g. Vault: Popcorn ×0, Lieutenant ×2. Anything not listed is ×1 |

Warning boxes show when an entry has no prefab or its prefab has no EnemyMovement/EnemySO (it's skipped), and when the list has MiniBosses or Bosses (never picked).

| Function | Description |
| :--- | :--- |
| `GetEnemy(rank)` | Weighted random prefab of that rank. Null if the table has none |
| `GetCountMultiplier(floorType, rank)` | The multiplier for that floor type and rank, or 1 |

---
## Warnings You Might See
| Warning | Meaning |
| :--- | :--- |
| `... has no enemy table, so ... is spawning nothing` | The current `DungeonSO`'s Enemy Table slot is empty |
| `Not inside a dungeon and no test enemy table set ...` | Playing a floor scene directly. Set a Test Enemy Table on the spawner to test it |
| `... has no <Rank> enemies for ...` | The spawner wants that rank but the table has none of it |
| `... ran out of room and placed X of Y ...` | The box is too small or crowded for the count and spacing. Make the box bigger, lower the spacing, or check the box's height covers the floor |
| `The middle of ...'s box isn't on the NavMesh ...` | The box's center is over a hole or too far above the floor. Move it, or turn off Require Reachable |

---
## Adding to a Floor Scene
1. Create an empty GameObject where enemies should go, and add an **Enemy Spawner Object** component. Nothing needs to be assigned.
2. Set the box size and turn the object so the box covers the area. Keep the floor inside the box's height.
3. Set the counts per rank.
4. Optional: set a **Test Enemy Table** so the floor scene can be tested on its own.

Spawners also work inside POI prefabs.

*Could be made into a prefab (`PFB_Dungeon_EnemySpawner`) to drag in.* [[Notes for the future]]

---
## Future Ideas [[Notes for the future]]
- **Enemy emitters**: a separate system for enemies that pour out of something (a burrow, a doorway) and fan out during play.
- **Spacing by size class**: bigger enemies might need more room than `_MinSpacing`.
- **Revisited floors**: remember who was spawned (and who died) so going back to a floor doesn't refill it.
- **Groups**: Lieutenants spawning with Popcorn clustered near them.
- **Leash / room volumes**: the spawner's box could double as the area its enemies stay in.
