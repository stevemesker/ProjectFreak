## Overview
The plan for giving [[Shade (Runtime)|shades]] and enemies AI movement, and the dungeon floor loading changes that need to come first. Update this note as each step lands, then move the finished parts into their own system notes.

Back to [[Architecture Atlas]]

**Status (Oct 2026):** Steps 1-4 done, step 5 built and waiting on testing. Next up: Step 6.

---
## Core Ideas

### Split "who steers" from "the engine"
Every unit that uses physics movement has three layers:

```text
Driver (who steers)         →  Engine (how the body moves)  +  Stance (stay floating)
PlayerInput driver               CharacterMovement               UnitHover
  or NavGuide driver (AI)
```

- **Engine** - `CharacterMovement`. Takes a move direction and a look direction and turns them into physics forces. Always on.
- **Stance** - `UnitHover`. The floating spring. Always on.
- **Drivers** - the only parts that get turned on and off. Player input and the AI's nav driver each feed directions into the engine.

**Rule: switch drivers, never the engine.** With physics movement:
- Disabling `CharacterMovement` doesn't stop a unit. It also turns off the braking, so the unit slides like an air hockey puck.
- Disabling `UnitHover` doesn't freeze it. Gravity drops it.
- To truly freeze a unit, use `DeactivateMovement()` or set the Rigidbody to kinematic.

### NavMesh is the map, not the legs (for physics units)
For the shade, a NavMeshAgent only plans the route. It never moves the shade:
- `agent.updatePosition = false` and `agent.updateRotation = false`
- Each physics step:
  - read `agent.desiredVelocity` (the direction to go, already steering around other agents)
  - feed it into `CharacterMovement` as the move direction
  - set `agent.nextPosition` to the shade's real position so the agent stays in sync
- `agent.baseOffset` matches `UnitHover._RideHeight` so the agent plans from the floor point under the floating unit

This keeps knockback, dashing and hover working the same whether the player or the AI is in control, and the shade only ever has one movement mode.

### Enemies use plain NavMeshAgents
Normal enemies let the agent move them directly. It's simpler and cheaper. Two things to plan for from the start:
- **Knockback.** An agent ignores physics. During a hit:
  1. turn agent movement off
  2. let a Rigidbody take the impulse
  3. `Warp` the agent back to the enemy's position once it stops moving
- **Speed matching.** Tune agent `speed`, `acceleration` (the default of 8 is far too slow next to the player) and `angularSpeed` to match the player's feel.

Elite or boss enemies can still use the physics engine + NavGuide driver later if they need to feel heavier.

---
## Dungeon Floor Loading

### Why
`SceneManagerObject.OnSceneLoaded` currently starts the fade-in as soon as the scene loads, but POI spawners run their `Start()` after that, so POIs pop in on screen. Building POIs also add walkable space, so the NavMesh has to be updated after they spawn and before enemies wake up.

### Only on dungeon floors
- **Dungeon floor scenes** get a floor-level object (working name `DungeonFloorObject`) that runs the loading phases and reports when the floor is ready.
- **Every other scene** (hub, title, dev scenes) has no floor object. The Scene Manager treats it as ready as soon as it loads, and it keeps its pre-made NavMesh. Nothing gets rebaked.

### Phases
```text
Scene loaded
    ↓
POIs spawned       (floor object tells each POISpawnerObject to spawn, instead of each one using Start())
    ↓
NavMesh updated    (async, see below)
    ↓
Enemies spawned / activated
    ↓
Floor ready        → Scene Manager fades in
```

- Phases are reported with events so the loading screen just reacts and doesn't run the load itself.
- Each phase prints how long it took to the console (dev only), so we can see where the time goes.
- The loading screen is a short "Preparing Dungeon" transition (gate animation, camera pan, tips). It never adds a fake delay. A "still working" overlay only shows if loading runs long.

### NavMesh on dungeon floors
- Uses the AI Navigation package's `NavMeshSurface`.
- **Async only.** Use `NavMeshSurface.UpdateNavMesh()`, which returns an `AsyncOperation` to wait on. `BuildNavMesh()` freezes the game until it finishes, which would freeze the loading animation too.
- **First approach to try:** bake the empty floor in the editor, then async update after POIs spawn. Unity should only rebuild the tiles whose geometry changed. *Verify this in the prototype.*
- Build from **physics colliders**, not render meshes (faster and cleaner), with a **layer mask** that leaves out the player, shades, enemies and pickups.
- Check agent radius against building doorway widths. A doorway narrower than the agent leaves no path inside, and that fails silently.
- Fallbacks if it's too slow: tune voxel size, then a full async rebake, then pre-baked NavMesh per POI. Avoid making POIs separate scenes, because separate NavMeshes need NavMesh Links at every seam to connect.

### Safeguards
- **Timeout:** if a floor never reports ready, log an `Error!` naming the floor and fade in anyway, so the player is never stuck on a black screen.
- **NavMesh check:** after the update, use `NavMesh.SamplePosition` at the player spawn point. If nothing is found, log an `Error!`.
- **Hand-made scenes:** if a scene has AI in it but no NavMesh, log a `Warning!` instead of failing silently. *(Built: enemies log a warning if there's no NavMesh near them, see [[Enemy Movement]].)*
- **No floor object:** fall back to the current behavior (ready on load).

---
## Build Order

1. **Movement cleanup** ✅ *(Oct 2026)*
   - Floating split into `UnitHover`; `CharacterMovement` handles direction only
   - Turning around fixed (acceleration curves now cover -1 to 1)
   - Dash start/end events fire in pairs
   - Shade rotation frozen on its Rigidbody
   - Switching control clears the move direction
2. **Dungeon floor loading phases** ✅ *(Oct 2026, see [[Dungeon Floor Object]])*
   - Also added the AI Navigation package (`com.unity.ai.navigation` 1.1.5)
   - `DungeonFloorObject` runs the phases above
   - `POISpawnerObject` spawns when the floor object tells it to
   - Scene Manager waits for "floor ready" on dungeon floors and is ready on load everywhere else
   - Safeguards and phase timing logs
3. **NavMesh prototype** ✅ *(Oct 2026)*
   - Tested on a bigger floor with POIs: about 36 ms on Steve's PC. Good enough to move on; the loading screen covers slower machines
   - *Still worth doing later:* time it on a lower-end machine and in a build [[Notes for the future]]
   - Findings:
     - **Only colliders get baked.** A POI piece with just a mesh (like `Art/Dev/Cube.prefab`, which had no collider) is invisible to the NavMesh, and the player can walk through it
     - **Mesh Collider meshes need Read/Write turned on**, or a built game bakes nothing from them (fixed for `M_Plane_4x4.fbx`)
     - **Missing triangles / z-fighting in the Scene view is a drawing artifact.** NavMesh corners sit a few cm above or below the real floor. Navigation isn't affected. Use Wireframe draw mode to see the real NavMesh
     - **Raised areas need ramps/stairs** within the agent's step height (0.4 m) and slope (45°), or they become unreachable islands
     - **Build Height Mesh** (on the NavMesh Surface) is the fix if agents ever look like they float or sink on stairs
4. **Enemy movement** ✅ *(Oct 2026, see [[Enemy Movement]])*
   - `EnemySO` template (rank, size class, Movement and Knockback foldouts that start closed)
   - `EnemyMovement` drives a plain NavMeshAgent and waits for the floor's NavMesh. Works without a spawner
   - Knockback by size class through the shared `SizeClassRulesSO` on the Game Manager, slid along the NavMesh and stopped at its edges
   - `_KnockbackDistance` added to the damage package; `EnemyDamagable` calls `IKnockbackable`
   - Temp `EnemyChaseTestBrain` + `PFB_Enemy_ChaseTest_Dev` for testing
5. **Drivers** 🔨 *(built Oct 2026, needs testing. See [[Player Movement]])*
   - `CharacterMovement` is now just the engine: `SetMoveDirection`, `SetLookDirection`, `GetMoveDirection`, `Dash`
   - New `PlayerInputDriver` holds all the controls, camera-relative input and mouse/stick aiming. On the player (on) and the shade (off)
   - Control switching (`Player`/`Shade` functions called by `ShadeManager`) turns drivers on and off. The driver clears the move direction when it turns off
   - `CameraManager` no longer pushes the camera into the movement script; the driver reads it
6. **Shade NavGuide driver** [[Notes for the future]]
   - The agent-as-guide setup above
   - AI picks a destination; the driver follows the agent's `desiredVelocity`
   - Add a non-carving `NavMeshObstacle` so enemy agents walk around the shade
7. **AI decision layer** *(later, separate system)* [[Notes for the future]]
   - Perception → decision → action → state
   - Abilities expose decision info (range, cooldown, role, risk)
   - Personality values (aggression, fear) weight the choices
   - The chosen ability runs through the existing [[Ability System|AbilityInterpreter]]; the chosen destination goes to the driver

---
## To Do (later) [[Notes for the future]]
- **Dungeon scene setup tool.** Creating a floor scene means remembering several pieces (floor prefab, player spawn point, door spawners, POI spawners). Goal: one button that sets up a new dungeon floor scene, or at least checks one and lists what's missing. *Design it once the full system is running, not before.* Ideas to weigh then:
  - an editor window or Odin button that creates a scene from a template with the required prefabs already in it
  - a "validate floor" check that warns about missing pieces
  - the Dungeon Manager adding `PFB_Dungeon Floor` by itself when it loads a floor that doesn't have one
  - add dungeon door spawners as well
- **Move the shade prefab to the Units layer** so it's never baked into the NavMesh.
- **Enemy spawning** (its own step, after enemy movement):
  - `EnemySpawnerObject`s placed in floors **and** inside POIs, so a floor always has spawners no matter which POIs show up. They register with the floor like POI spawners and spawn during the floor's `SpawningEnemies` step
  - Each spawner has a **default count per rank** (Popcorn, Basic, Lieutenant)
  - The dungeon's `DungeonEnemyTableSO` lists which enemies each rank can be, and **per floor type overrides as multipliers** (e.g. Vault: Popcorn ×0, Lieutenant ×2; Treasure: Popcorn only). Floor types it doesn't list use the spawner defaults
  - MiniBosses and Bosses aren't spawned: they're placed by hand in their arenas
- **Target priorities.** Enemies always chase the player, even while the player controls a shade. Decide how enemies pick between the player, shades and other targets (and what happens when control switches). Goes with the AI decision layer.
- **Leash / room volumes** so enemies don't chase the player across the whole floor.
- **Hazard knockback.** Let units be knocked over NavMesh edges into hazards like lava pits for instant kills. Decide alongside how hazards are built in levels (a NavMesh area type or a trigger volume are the likely options).
- **Damage pass:** apply the size class bonus damage, and fix the defense math (see [[Known Issues]]).

---
## Open Questions [[Notes for the future]]
- If bakes ever get slow: does pre-baking the floor and updating only rebuild the changed tiles? (About 36 ms so far, so not urgent.)
- What agent radius and height fit both the shade and the doorways in building POIs?
- Should revisited floors reuse saved POIs (planned in [[POI System]]) and skip the NavMesh update, or always update?
- Do any enemy types need the physics engine instead of a plain agent?
