## Overview
How enemies move and get knocked back. Normal enemies move with a plain **NavMeshAgent**: the agent finds the path and moves the enemy directly, with no physics. Its [[Unit Brain]] decides *where* to go and calls `SetDestination`.

**Making a new enemy?** Follow [[How To - Create Enemies & Shades#Part 1 - Make a New Enemy]].

Part of step 4 of the [[AI Movement & Dungeon Loading Plan]].

| Piece                 | File                                         | Job                                                        |
| :-------------------- | :------------------------------------------- | :--------------------------------------------------------- |
| `EnemySO`             | `Scripts/Enemies/EnemySO.cs`                 | Template for one kind of enemy (rank, size, stats, movement, knockback) |
| `EnemyType`           | `Scripts/Enemies/EnemyType.cs`               | `EnemyType.Rank` and `EnemyType.SizeClass` enums           |
| `SizeClassRulesSO`    | `Scripts/Enemies/SizeClassRulesSO.cs`        | Shared rules for how each size class reacts to hits        |
| `EnemyMovement`       | `Scripts/Enemies/EnemyMovement.cs`           | Drives the NavMeshAgent, waits for the floor, handles knockback |
| `UnitBrain`           | `Scripts/AI/UnitBrain.cs`                    | Decides what the enemy does and gives `EnemyMovement` its orders. See [[Unit Brain]] |

**Test enemy:** `Prefab/Dev/PFB_Enemy_ChaseTest_Dev` (uses `Scriptable Objects/Enemies/SO_Enemy_ChaseTest_Dev`).

---
## EnemySO
One asset per kind of enemy. **Create → Enemy → Enemy** (`SO_Enemy_Name`). It's a template: enemies copy what they need from it and never change it.

Always visible:

| Field         | Description                                                                 |
| :------------ | :-------------------------------------------------------------------------- |
| `_EnemyName`  | Readable name                                                               |
| `_Rank`       | Popcorn, Basic, Lieutenant, Elite, MiniBoss, Boss. Saved as fixed numbers, so Elite (added Nov 2026) is saved as 5 and nothing else shifted |
| `_SizeClass`  | Small, Medium, Large, Huge. Decides knockback through the size class rules  |

**Stats** foldout (starts closed). *Added Nov 9, 2026 ([[Enemy stats on the EnemySO]])*:

| Field | Description |
| :--- | :--- |
| `_Stats` | An `EnemyCoreStats`: HP, STR, DEF, AGI, INT, SPR, LVL, which stats power and defend attacks, resistances, immunities (see [[Stats & Inventory Data]]). New enemies start at HP 10 and 1 in each stat. **Health** here is ignored: it's filled in from HP on spawn. A warning box shows if HP is 0 |

`EnemyStats` gives each enemy its own copy of these stats when it spawns (`EnemyCoreStats.Copy()`), so damage never changes the asset. The stats also include **Loot Drops** (`_DropGold`, `DropItems`), which aren't used until the death and loot pass [[Notes for the future]]

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

**Stagger** foldout (starts closed). Read by `EnemyStagger` (see [[Melee Weapon System#Stagger]]):

| Field                    | Default | Description |
| :----------------------- | :------ | :---------- |
| `_StaggerImmune`         | off     | Never staggers from regular hits, no matter the size. Boss thresholds still work |
| `_StaggerDuration`       | 0.6     | How long a stagger stuns it, seconds |
| `_StaggerImmunityTime`   | 1       | Seconds after a stagger ends before regular hits can stagger it again (stops stunlocks). Starting value, to tune |
| `_BossStaggerThresholds` | 66, 33  | *MiniBoss and Boss only.* Health percentages it staggers at, once each |

**Death** foldout (starts closed). Read by `EnemyDeath`, *added Nov 9, 2026*:

| Field | Default | Description |
| :--- | :--- | :--- |
| `_DespawnDelay` | 1.5 | Seconds the body stays after dying before it's removed |
| `_ShrinkOnDeath` | on | Shrinks the body away during the delay. Stand-in until death animations [[Notes for the future]] |
| `_ShrinkCurve` | holds, then drops | Body size over the delay (1 = full, 0 = gone) |

**AI** foldout (starts closed):

| Field          | Description                                                                 |
| :------------- | :-------------------------------------------------------------------------- |
| `_Personality` | A personality preset plus optional overrides. The enemy's [[Unit Brain]] builds its personality from this when the game starts. See [[How To - Create Enemies & Shades#Make a Personality Preset]] |

*A loot section gets added with the death and loot pass.* [[Notes for the future]]

---
## Ranks
- **Popcorn, Basic, Lieutenant, Elite** - spread around floors by [[Enemy Spawners]] (and later, emitters)
- **MiniBoss, Boss** - placed by hand in their own arenas (lots of art integration)

Because bosses are placed by hand, movement never depends on a spawner. Spawners only instantiate enemies.

---
## Size Class Rules
One shared `SizeClassRulesSO` for the whole game (`Scriptable Objects/Enemies/SO_SizeClassRules`), assigned once on the [[Game Manager]] and reached with `GameManager._GameManager.GetSizeClassRules()`. **Create → Combat → Size Class Rules**, with a **Fill Defaults** button.

| Size   | Knockback | Bonus damage* | Full knockback from | Stagger |
| :----- | :-------- | :------------ | :------------------ | :------ |
| Small  | 1×        | 1.25×         | -                   | 1×      |
| Medium | 1×        | 1×            | -                   | 0.4×    |
| Large  | 0.5×      | 1×            | Explosion           | 0.15×   |
| Huge   | 0×        | 1×            | -                   | 0×      |

**Stagger** multiplies a hit's stagger power to get the chance it staggers (`GetStaggerMultiplier`). Mini bosses and bosses ignore it.

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
| `SetDestination(Vector3)` | Go here. Remembered if the enemy can't move yet, is mid-knockback or is stunned |
| `StopMoving()`            | Stop and forget the destination                                            |
| `HasArrived()`            | True when it reached its destination (or has none)                         |
| `IsActive()`              | True when it can take orders: on a NavMesh, not mid-knockback and not stunned. The [[Unit Brain]] pauses while this is false |
| `TakeKnockback(DamagePackage)` | From `IKnockbackable`. Pushes away from the package's `_Source`       |
| `Stun(float)`             | Stops it where it stands for that many seconds (used by stagger). A new stun replaces one still going. Knockback can still slide it while stunned |

The movement functions come from `IUnitMover`, so the same AI brain can steer enemies and shades (see [[Unit Targeting#IUnitMover]]).

### Knockback
Hits carry `_KnockbackDistance` (meters) on the [[Damage Package]]. When an enemy is hit, `EnemyDamagable` finds its `IKnockbackable` and calls `TakeKnockback`:
1. Skips if the distance is 0, the enemy is immune, or it isn't on a NavMesh yet
2. Multiplies the distance by its size class rule
3. Uses `NavMesh.Raycast` to stop the slide at the edge of the NavMesh, so enemies never get pushed into walls or off the map
4. Slides there over `_KnockbackDuration` following `_KnockbackCurve` (with `agent.Move`, which keeps it on the NavMesh), then carries on to its destination (unless it's still stunned: whichever of knockback or stun ends last lets it move again, through `ResumeMoving()`)

A new hit during a slide replaces it. **Test Knockback** (Odin button, play mode) knocks the enemy away from the player.

*Planned:* knocking enemies over edges into hazards like lava pits for instant kills. That needs to be decided along with how hazards are built (see the plan's To Do). [[Notes for the future]]

### Targets
Enemies pick targets with `UnitTargeting` (see [[Unit Targeting]]), and their [[Unit Brain]] decides what to do about it (chase, hold, flee, wander). The shade hitting an enemy pulls it off the player's body. Enemy agents should steer around the shade while it's AI-controlled, since its `NavGuideDriver` agent takes part in avoidance.

Enemy prefabs need a `UnitTeam` (Team: Enemy, Role: Enemy), `UnitTargeting` and `UnitBrain`. `PFB_Enemy_ChaseTest_Dev` already has all three.

---
## EnemyDeath
`Scripts/Enemies/EnemyDeath.cs` (`IKillable`). Bare-bones death, *built Nov 9, 2026* ([[Basic enemy death]]). Loot and XP come with the death and loot pass ([[Defeated enemies drop items]]) [[Notes for the future]]

**Setup:** none. `EnemyStats` adds it at runtime to anything with an EnemySO if the prefab doesn't have one. Add it to the prefab by hand to hook up its **On Died** event in the inspector. Units without an EnemySO (the target dummy) don't get one, so they never die.

**`Kill()`** runs once (later calls are ignored). `EnemyStats` calls it at 0 health, and self-destructs can call it directly. It:
1. Turns off the `UnitBrain`, `UnitTargeting` and `EnemyStagger`, interrupts and turns off the `AbilityInterpreter`, stops and turns off `EnemyMovement` (its OnDisable stops knockback and stun) and the NavMeshAgent
2. Turns off every collider on the enemy and its children, so weapons and projectiles pass through
3. Turns off `UnitTeam` last: that takes it out of the `UnitRegistry`, and `UnitTargeting` already drops targets that are turned off, so nothing keeps chasing the corpse
4. Fires `_OnDied` (UnityEvent, for effects and sounds) and `Died` (C# event, for code like a hive counting its scarabs)
5. Waits the EnemySO's despawn delay (shrinking if set), then destroys it. *Pooled units will go back to their pool instead, once pooling is built* [[Notes for the future]]

*Status effects get turned off here too once they exist* [[Notes for the future]]

`EnemyDamagable` skips aggro, knockback and stagger on the hit that kills, and `EnemyStats` ignores hits once dead.

**Spawned units:** `MarkAsSpawned(spawnedBy)` flags a unit made during play (like a hatched scarab) so the loot pass can skip it. `WasSpawned()` reads it.

**Test Kill (Play Mode)** button kills it on the spot. Runtime readouts: `_IsDead`, `_WasSpawned`, `_SpawnedBy`.

---
## Layers
Enemies go on the **Units** layer so they're never baked into the NavMesh.
