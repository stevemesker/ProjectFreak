## Overview
How enemies move and get knocked back. Normal enemies move with a plain **NavMeshAgent**: the agent finds the path and moves the enemy directly, with no physics. Something else (a "brain", later the AI decision layer) decides *where* to go and calls `SetDestination`.

Part of step 4 of the [[AI Movement & Dungeon Loading Plan]].

| Piece                 | File                                         | Job                                                        |
| :-------------------- | :------------------------------------------- | :--------------------------------------------------------- |
| `EnemySO`             | `Scripts/Enemies/EnemySO.cs`                 | Template for one kind of enemy (rank, size, movement, knockback) |
| `EnemyType`           | `Scripts/Enemies/EnemyType.cs`               | `EnemyType.Rank` and `EnemyType.SizeClass` enums           |
| `SizeClassRulesSO`    | `Scripts/Enemies/SizeClassRulesSO.cs`        | Shared rules for how each size class reacts to hits        |
| `EnemyMovement`       | `Scripts/Enemies/EnemyMovement.cs`           | Drives the NavMeshAgent, waits for the floor, handles knockback |
| `EnemyChaseTestBrain` | `Scripts/Dev Scripts/EnemyChaseTestBrain.cs` | *Temp.* Chases the player so movement can be tested [[Notes for the future]] |

**Test enemy:** `Prefab/Dev/PFB_Enemy_ChaseTest_Dev` (uses `Scriptable Objects/Enemies/SO_Enemy_ChaseTest_Dev`).

---
## EnemySO
One asset per kind of enemy. **Create → Enemy → Enemy** (`SO_Enemy_Name`). It's a template: enemies copy what they need from it and never change it.

Always visible:

| Field         | Description                                                                 |
| :------------ | :-------------------------------------------------------------------------- |
| `_EnemyName`  | Readable name                                                               |
| `_Rank`       | Popcorn, Basic, Lieutenant, MiniBoss, Boss                                  |
| `_SizeClass`  | Small, Medium, Large, Huge. Decides knockback through the size class rules  |

**Movement** foldout (starts closed):

| Field                | Default | Description                                     |
| :------------------- | :------ | :---------------------------------------------- |
| `_MoveSpeed`         | 6       | Top speed, m/s (the player runs at about 8)     |
| `_Acceleration`      | 40      | m/s². Higher = snappier starts and stops        |
| `_TurnSpeed`         | 720     | Degrees per second                              |
| `_StoppingDistance`  | 1.5     | How close it gets to its destination, m         |

**Knockback** foldout (starts closed):

| Field                 | Default | Description                                                |
| :-------------------- | :------ | :--------------------------------------------------------- |
| `_KnockbackImmune`    | off     | Ignore knockback no matter the size (turrets and the like) |
| `_KnockbackDuration`  | 0.25    | How long the slide lasts, seconds                          |
| `_KnockbackCurve`     | ease-out| Shape of the slide over its duration                       |

*Stats, loot and AI personality sections get added as those systems are built.*

---
## Ranks
- **Popcorn, Basic, Lieutenant** - spread around floors by enemy spawners and events
- **MiniBoss, Boss** - placed by hand in their own arenas (lots of art integration)

Because bosses are placed by hand, movement never depends on a spawner. Spawners only instantiate enemies.

---
## Size Class Rules
One shared `SizeClassRulesSO` for the whole game (`Scriptable Objects/Enemies/SO_SizeClassRules`), assigned once on the [[Game Manager]] and reached with `GameManager._GameManager.GetSizeClassRules()`. **Create → Combat → Size Class Rules**, with a **Fill Defaults** button.

| Size   | Knockback | Bonus damage* | Full knockback from |
| :----- | :-------- | :------------ | :------------------ |
| Small  | 1×        | 1.25×         | -                   |
| Medium | 1×        | 1×            | -                   |
| Large  | 0.5×      | 1×            | Explosion           |
| Huge   | 0×        | 1×            | -                   |

\* *Bonus damage isn't applied yet. It comes with the damage pass (along with the defense bug in [[Known Issues]]).* [[Notes for the future]]

If any damage entry in a hit uses one of the "full knockback" attack types, that size takes the whole distance. The asset warns in the editor if a size class is missing or listed twice.

---
## EnemyMovement

### Starting up
1. `Awake` turns the NavMeshAgent **off** and copies the movement settings from the EnemySO onto it.
2. `Start`:
   - **In a dungeon floor that's still loading**, subscribes to `DungeonFloorObject._Floor.FloorReady` and waits.
   - **Otherwise** (hand-built scene, or the floor is already ready), starts right away.
3. Starting snaps the enemy to the closest NavMesh point within `_NavMeshSnapDistance` (3 m) and turns the agent on. If there's no NavMesh nearby, it logs a warning and stays put. This is the "hand-built scene has AI but no NavMesh" safeguard.

**Keep the NavMeshAgent turned off in enemy prefabs.** If it's on, Unity warns that the agent can't find a NavMesh before the floor finishes building it. The inspector shows a reminder when it's on.

### Functions

| Function                  | Description                                                                |
| :------------------------ | :------------------------------------------------------------------------- |
| `SetDestination(Vector3)` | Go here. Remembered if the enemy can't move yet or is mid-knockback        |
| `StopMoving()`            | Stop and forget the destination                                            |
| `IsActive()`              | True once it's on a NavMesh and able to move                               |
| `TakeKnockback(DamagePackage)` | From `IKnockbackable`. Pushes away from the package's `_Source`       |

### Knockback
Hits carry `_KnockbackDistance` (meters) on the [[Damage Package]]. When an enemy is hit, `EnemyDamagable` finds its `IKnockbackable` and calls `TakeKnockback`:
1. Skips if the distance is 0, the enemy is immune, or it isn't on a NavMesh yet
2. Multiplies the distance by its size class rule
3. Uses `NavMesh.Raycast` to stop the slide at the edge of the NavMesh, so enemies never get pushed into walls or off the map
4. Slides there over `_KnockbackDuration` following `_KnockbackCurve` (with `agent.Move`, which keeps it on the NavMesh), then carries on to its destination

A new hit during a slide replaces it. **Test Knockback** (Odin button, play mode) knocks the enemy away from the player.

*Planned:* knocking enemies over edges into hazards like lava pits for instant kills. That needs to be decided along with how hazards are built (see the plan's To Do). [[Notes for the future]]

### Targets
Enemies don't pick targets yet. The temp chase brain always goes after the player, even while the player is controlling a shade. **Target priorities** (player vs shade vs others, and switching when control changes) belong with the AI decision layer. [[Notes for the future]]

---
## Layers
Enemies go on the **Units** layer so they're never baked into the NavMesh.
