## Overview
`DungeonFloorObject` runs a [[Dungeon Floor]]'s loading steps in order and tells the [[Scene Manager]] when the floor is ready to be seen. It only goes in dungeon floor scenes. Any scene without one counts as ready as soon as it loads and keeps its pre-made NavMesh.

Part of step 2 of the [[AI Movement & Dungeon Loading Plan]].

**Script:** `Assets/Scripts/Dungeon/DungeonFloorObject.cs`
**Prefab:** `Assets/Prefab/Dungeon/PFB_Dungeon Floor.prefab`. Drop one into every dungeon floor scene
**Singleton:** `DungeonFloorObject._Floor`, the floor in the currently loaded scene. Scene objects register with it instead of the floor searching for them (see [[Code Style Rules#Patterns]])

---
## Loading Flow

```text
Scene loads
    │  DungeonFloorObject.Awake: sets _Floor, registers with the Scene Manager
    ▼
Scene Manager OnSceneLoaded: sees a floor that isn't ready → waits (with timeout) instead of fading in
    ▼
Every Start() in the scene runs
    │  POISpawnerObject.Start: _Floor exists → RegisterPOISpawner(this) instead of spawning
    ▼
DungeonFloorObject.Start → LoadFloor() → waits 1 frame so every spawner has registered
    ├── SpawningPOIs      every registered POISpawnerObject.SpawnPOI(), then wait 1 frame
    ├── BuildingNavMesh   async NavMesh build, then NavMesh check   (skipped if _BuildNavMesh is off)
    ├── SpawningEnemies   todo, the enemy system doesn't exist yet
    └── Ready             _OnFloorReady (UnityEvent), then FloorReady (C# event)
    ▼
Scene Manager sees the floor is ready → HUD fades in
```

The steps are listed in `FloorLoadType.Phase` (`NotStarted, SpawningPOIs, BuildingNavMesh, SpawningEnemies, Ready`).

---
## Inspector

| Field                    | Description                                                                                     |
| :----------------------- | :---------------------------------------------------------------------------------------------- |
| `_BuildNavMesh`          | Build a NavMesh after POIs spawn. Turn off for floors that never have AI                         |
| `_NavMeshLayers`         | Layers that count as ground/walls. Only used when no surface is assigned. **Untick the units layer** |
| `_NavMeshCheckDistance`  | How far (m) the NavMesh check looks from the check point. Default 3                              |
| `_LogPhaseTimes`         | Prints how long each step took (`SCN_DungeonTestRoom_0: BuildingNavMesh took 42ms`)              |
| `_NavMeshSurface`        | Optional. Assign one to set its options yourself. If empty, one is added at runtime              |
| `_NavMeshCheckPoint`     | Optional. Where to check for a NavMesh. If empty, checks under the player (already placed by the spawn point or door) |
| `_OnFloorReady`          | UnityEvent called once the floor finishes loading, right before the fade-in                      |
| `_CurrentPhase`          | Read only. Which step the floor is on, useful when a load gets stuck                             |
| `_POISpawners`           | Read only. Spawners that registered with this floor. Fills in during play                        |

---
## Registration
- **POI spawners** register in their own `Start()`. Every `Awake()` in a scene runs before any `Start()`, so `_Floor` is always set by then. Spawners also check the floor is in their own scene, so they never register with a floor left over from the last scene.
- The floor waits **one frame** before spawning. Unity doesn't promise the floor's `Start()` runs after the spawners', but all of them are done by the next frame.
- **A spawner that registers late** (after the POI step) spawns right away with a warning, since it missed the NavMesh build.
- **Two floors in one scene:** the second one logs an error and turns itself off.

---
## NavMesh Build
- Uses the AI Navigation package's `NavMeshSurface` (package `com.unity.ai.navigation` 1.1.5, added Oct 2026).
- When no surface is assigned, the added one collects **all** loaded objects, uses **physics colliders**, and uses `_NavMeshLayers`.
- The build always goes into a **fresh runtime `NavMeshData`**, which is destroyed when the floor unloads. A NavMesh baked in the editor is never changed, because changes to assets made in play mode can stick.
- Built with `UpdateNavMesh()`, which runs in the background over several frames, so the loading screen keeps animating.
- Agent size (radius, height, step height, slope) comes from the agent type in **Window → AI → Navigation → Agents**.

*Pre-baking the empty floor and only updating the changed tiles is the step 3 experiment.*

---
## Safeguards
- **Timeout** - the Scene Manager waits at most `_FloorLoadTimeout` seconds (default 10). After that it logs `Error! ... (stuck on <phase>)` and fades in anyway.
- **NavMesh check** - after the build, `NavMesh.SamplePosition` at the check point. If nothing is found, it logs an error.
- **Units baked into the NavMesh** - if the player's layer is included in the NavMesh layers, it logs a warning. Everything is on `Default` right now, so this warning shows until units get their own layer.
- **POI spawners outside a dungeon** - `SpawnPOI()` warns and spawns nothing when there's no Dungeon Manager or no current dungeon (e.g. playing a floor scene directly) instead of throwing an error.
- **Spawners without a floor object** - still spawn in their own `Start()` like before.

---
## Seeing the NavMesh
The NavMesh is only built in **Play mode**, so there's nothing to see in Edit mode.
1. Enter Play mode and get to a dungeon floor.
2. In the Hierarchy, select the `PFB_Dungeon Floor` object (the NavMesh Surface gets added to it at runtime).
3. In the Scene view, open the **AI Navigation** overlay (if it's hidden: the `⋮` menu at the top right of the Scene view → Overlays → AI Navigation) and turn on **Show NavMesh**. If **Show only selected** is on, the NavMesh only draws while the floor object is selected.
4. The walkable area shows as a blue overlay. Gaps around walls are normal: the agent's radius is kept clear of edges.

The console should also show `BuildingNavMesh took ___ms`. If there's an `Error! No NavMesh found...` line instead, the build found nothing walkable under the player.

## Layers
- **Units** (layer 8): player and shades. Must be **unticked** in `_NavMeshLayers` or they get baked in as obstacles. *(Oct 2026: the player is on Units, the shade prefab still needs moving.)*
- **Projectile / Item**: should usually be unticked too, so bullets and pickups lying around don't cut holes.
- Floors and walls stay on Default (or any ticked layer).

## Adding to a New Floor Scene
Drag `PFB_Dungeon Floor` into the scene. Nothing to fill in.
