## Overview
The record of how AI movement for [[Shade (Runtime)|shades]] and enemies was built (Oct 2026), along with the dungeon floor loading it needed first. Each finished piece has its own system note, linked below. This page keeps the big-picture ideas, the decisions behind them, lessons learned, and what's **still to do**.

Back to [[Architecture Atlas]]

**Status (Oct 2026):** all 7 steps are built and tested. AI units can notice targets, chase, hold position, flee, wander, follow the player and go looking for fights, each with its own personality. **They can't attack yet:** hooking abilities into the AI is part of the ability overhaul. See [[#To Do]].

**Making new enemies or setting up shades?** Follow [[How To - Create Enemies & Shades]].

---
## Where Everything Lives Now

| Piece | What it does | Note |
| :--- | :--- | :--- |
| `CharacterMovement`, `UnitHover`, `PlayerInputDriver` | Physics movement engine, floating, and player controls | [[Player Movement]] |
| `NavGuideDriver` | Lets AI steer a physics unit (the shade) along the NavMesh | [[Player Movement#NavGuideDriver (AI steering)]] |
| `DungeonFloorObject` | Runs a dungeon floor's loading steps and builds its NavMesh | [[Dungeon Floor Object]] |
| `EnemySO`, `EnemyMovement`, `SizeClassRulesSO` | Enemy templates, NavMesh movement, knockback by size | [[Enemy Movement]] |
| `UnitTeam`, `UnitRegistry`, `UnitTargeting` | Teams, the list of active units, and who each AI unit goes after | [[Unit Targeting]] |
| `UnitBrain`, AI actions, personality presets | Decides what each AI unit does | [[Unit Brain]] |

---
## Core Ideas

### 1. Split "who steers" from "the engine"
Every physics unit (the player and shades) has three layers:

```text
Driver (who steers)              →  Engine (how the body moves)  +  Stance (stay floating)
PlayerInputDriver or NavGuideDriver   CharacterMovement               UnitHover
```

**Rule: switch drivers, never the engine.** Turning off `CharacterMovement` doesn't stop a unit (it slides like an air hockey puck), and turning off `UnitHover` drops it. Taking control of the shade just swaps which driver is on.

### 2. The NavMesh is the map, not the legs (for physics units)
The shade's NavMeshAgent only **plans** the route. It never moves the shade. Each frame `NavGuideDriver` keeps the agent on the NavMesh point under the floating shade, reads the direction the agent wants to go, and feeds it into `CharacterMovement`. Physics still moves the body, so knockback, dashing and floating work the same whether the player or the AI is in control.

### 3. Enemies use plain NavMeshAgents
Normal enemies let the agent move them directly, which is simpler and cheaper. **Knockback** slides the enemy along the NavMesh (it stops at walls and ledges) instead of using physics, and its size class decides how far. Elite or boss enemies could still use the physics engine + `NavGuideDriver` later if they need to feel heavier.

### 4. Registration, not searching
Units and spawners announce themselves (to `UnitRegistry`, `DungeonFloorObject._Floor`) instead of anything searching the scene. See [[Code Style Rules]].

### 5. Score every option, pick the best
No behavior tree. The brain asks each action "how good an idea are you right now, 0 to 1?", and personality values are part of every score, so the same actions play differently for a coward and a berserker. See [[Unit Brain]].

---
## Key Decisions
- **Target rules:** whoever hit an enemy recently comes first, so the shade can pull enemies off the player. The player's empty body (while the player is in the shade) is only a fallback target. Details in [[Unit Targeting#Target rules]].
- **Hits from the shade** cause aggro whether the player or the shade's AI is driving. Easy to limit to player-driven only if playtests say so.
- **Personality comes from presets** with per-value overrides. Enemies set theirs on their EnemySO, and shades on their brain for now.
- **Crowds:** every enemy in range may attack at once for now. Attack tokens wait for playtests.
- **Abilities** hook into the brain during the ability overhaul, not before. Until then Engage is a placeholder that holds in range.
- **Loyal shades stay put** when idle and never walk through the player (after testing showed wandering shades shoving the player around).

---
## Lessons Learned
NavMesh, from testing a big floor with POIs (bake took about 36 ms on Steve's PC):
- **Only colliders get baked.** A piece with just a mesh is invisible to the NavMesh, and units walk through it.
- **Mesh Collider meshes need Read/Write turned on** in their import settings, or a built game bakes nothing from them.
- **Missing triangles or flickering in the Scene view is a drawing artifact.** Use Wireframe draw mode to see the real NavMesh.
- **Raised areas need ramps or stairs** within the agent's step height (0.4 m) and slope (45°), or they become unreachable islands.
- **Build Height Mesh** (on the NavMesh Surface) is the fix if agents ever look like they float or sink on stairs.
- **The NavMesh only exists in Play mode** on dungeon floors. Turn on **Show NavMesh** in the AI Navigation overlay to see it.

AI:
- **Physics units with a free-spinning Rigidbody slide around after bumps.** Freeze rotation on the Rigidbody.
- **Turning off a driver has to clear the move direction**, or the unit keeps walking with nobody steering.
- **Watch the Scores list** (Unit Brain → Runtime Data) when an AI does something odd. It always shows why.

---
## Build Order (record)
1. **Movement cleanup** ✅: floating split into `UnitHover`, faster turning around, dash events fixed, shade rotation frozen, switching control stops the unit.
2. **Dungeon floor loading** ✅: `DungeonFloorObject` spawns POIs, builds the NavMesh, then tells the Scene Manager the floor is ready. Other scenes are ready on load. See [[Dungeon Floor Object]].
3. **NavMesh prototype** ✅: fast enough. See [[#Lessons Learned]].
4. **Enemy movement** ✅: `EnemySO`, `EnemyMovement`, knockback by size class. See [[Enemy Movement]].
5. **Drivers** ✅: `CharacterMovement` became just the engine; `PlayerInputDriver` holds the controls. See [[Player Movement]].
6. **Shade NavGuide driver** ✅: AI can steer the shade. See [[Player Movement#NavGuideDriver (AI steering)]].
7. **AI decision layer** ✅
   - **7a. Targeting:** teams, the unit registry, target rules, aggro from hits. See [[Unit Targeting]].
   - **7b. Brain and movement actions:** Chase, Engage (placeholder), Keep Distance, Flee, Wander, Follow Leader, Seek Fight. See [[Unit Brain]].
   - **7c. Personality presets:** 6 starter presets, overrides, Reload buttons. See [[Unit Brain#Personality]].
   - **7d. Tuning tools:** debug labels, slower thinking for far-away and Popcorn enemies. See [[Unit Brain#Performance]].

---
## To Do
Everything still open from this work, roughly in the order it's likely to come up. [[Notes for the future]]

**Next phases**
- **Ability overhaul → AI attacks.** Give `AbilitySO` an **AI Use** section (range, role, risk, weight, needs line of sight), per-unit cooldowns using `_AbilityCooldown`, `AbilityInterpreter.IsBusy()`, and a target or aim point handed to abilities. Fix the step Timing bug ([[Known Issues]]). Brains then add a unit's abilities as scored actions, and Engage goes away.
- **Enemy spawning.**
  - `EnemySpawnerObject`s placed in floors and inside POIs. They register with the floor like POI spawners and spawn during its `SpawningEnemies` step.
  - Each spawner has a default count per rank (Popcorn, Basic, Lieutenant).
  - The dungeon's `DungeonEnemyTableSO` lists which enemies each rank can be, with per floor type multipliers (e.g. Vault: Popcorn ×0, Lieutenant ×2).
  - MiniBosses and Bosses aren't spawned. They're placed by hand in their arenas.
- **Shade overhaul.** New kinds of shades (own look, abilities, commands), training that changes personality during play (Digimon World style), and shade health so its brain can react to being hurt.

**Smaller items**
- **Leash / room volumes** so enemies don't chase the player across the whole floor.
- **Body damage warning** when the player's empty body is hurt while they're in the shade (Steve has ideas). Should also cover the shade taking damage.
- **Attack tokens** (only a few enemies attack the same target at once). Revisit after playtests.
- **Hazard knockback:** let units be knocked over NavMesh edges into hazards like lava for instant kills. Decide with how hazards are built (NavMesh area type or trigger volume).
- **Weapons that knock back.** The knockback system works, but no weapon sets a knockback distance yet.
- **Damage pass:** apply the size class bonus damage and fix the defense math ([[Known Issues]]). Move enemy health and stats from the prefab into the EnemySO.
- **Enemy death and loot** (`EnemyStats` On Death does nothing yet).
- **Facing:** enemies don't turn to face their target while holding still in Engage.
- **Enemies don't steer around the shade while the player drives it** (its agent is off then).
- **Dungeon scene setup tool:** one button that sets up a new floor scene (floor prefab, spawn point, door and POI spawners), or checks one and lists what's missing. Design it once the full system is running.
- **NavMesh bake timing** on a lower-end machine and in a build.

---
## Open Questions [[Notes for the future]]
- What agent radius and height fit both the shade and the doorways in building POIs?
- Should revisited floors reuse saved POIs (planned in [[POI System]]) and skip the NavMesh update?
- Do any enemy types need the physics engine instead of a plain agent?
- Should hits from the shade cause aggro only while the player is driving it?
