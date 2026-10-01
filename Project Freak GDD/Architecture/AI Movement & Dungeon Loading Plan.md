## Overview
The plan for giving [[Shade (Runtime)|shades]] and enemies AI movement, and the dungeon floor loading changes that need to come first. Nothing below step 1 is built yet. Update this note as each step lands, then move the finished parts into their own system notes.

Back to [[Architecture Atlas]]

**Status (Oct 2026):** Steps 1 and 2 done. Next up: Step 3.

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
- **Hand-made scenes:** if a scene has AI in it but no NavMesh, log a `Warning!` instead of failing silently. *(Not built yet: belongs with the AI in steps 4 and 6, since nothing uses the NavMesh until then.)*
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
3. **NavMesh prototype**
   - Biggest floor plus building POIs
   - Async update, timed
   - Test on a lower-end machine
4. **Enemy movement**
   - Plain NavMeshAgents
   - Knockback hand-off
   - Speed tuning
5. **Drivers**
   - Add `SetMoveDirection` / `SetLookDirection` to `CharacterMovement`
   - Move player input out into its own driver component (on the player and the shade)
   - `ShadeManager` swaps drivers instead of enabling/disabling input inside `CharacterMovement`
6. **Shade NavGuide driver**
   - The agent-as-guide setup above
   - AI picks a destination; the driver follows the agent's `desiredVelocity`
   - Add a non-carving `NavMeshObstacle` so enemy agents walk around the shade
7. **AI decision layer** *(later, separate system)*
   - Perception → decision → action → state
   - Abilities expose decision info (range, cooldown, role, risk)
   - Personality values (aggression, fear) weight the choices
   - The chosen ability runs through the existing [[Ability System|AbilityInterpreter]]; the chosen destination goes to the driver

---
## To Do (later)
- **Dungeon scene setup tool.** Creating a floor scene means remembering several pieces (floor prefab, player spawn point, door spawners, POI spawners). Goal: one button that sets up a new dungeon floor scene, or at least checks one and lists what's missing. *Design it once the full system is running, not before.* Ideas to weigh then:
  - an editor window or Odin button that creates a scene from a template with the required prefabs already in it
  - a "validate floor" check that warns about missing pieces
  - the Dungeon Manager adding `PFB_Dungeon Floor` by itself when it loads a floor that doesn't have one
- **Move the shade prefab to the Units layer** so it's never baked into the NavMesh.

---
## Open Questions
- How long does the async update take on the biggest floor? Does Unity really only rebuild the changed tiles?
- What agent radius and height fit both the shade and the doorways in building POIs?
- Should revisited floors reuse saved POIs (planned in [[POI System]]) and skip the NavMesh update, or always update?
- Do any enemy types need the physics engine instead of a plain agent?
