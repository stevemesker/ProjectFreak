## Overview
How AI units decide **who** to go after. Part of the AI decision layer (step 7a of the [[AI Movement & Dungeon Loading Plan]]). Targeting never moves anything: a brain asks `UnitTargeting.GetTarget()` and then tells its mover where to go.

Scripts live in `Scripts/AI/`.

```text
UnitTeam (on every unit)  →  UnitRegistry (list of active units)
                                   ↓
UnitTargeting (on AI units) scores hostile units → GetTarget()
                                   ↓
Brain → IUnitMover (EnemyMovement / NavGuideDriver)
```

---
## UnitTeam
Goes on **every unit**: the player, shades and enemies. It says which side the unit is on and adds it to the `UnitRegistry` while it's enabled. Already on `Player Character_PFB`, `PFB_Shade` and `PFB_Enemy_ChaseTest_Dev`.

| Field             | Description                                                                                     |
| :---------------- | :---------------------------------------------------------------------------------------------- |
| `_Team`           | `Player` or `Enemy`. Units on different teams are hostile to each other                          |
| `_Role`           | `PlayerBody`, `Shade` or `Enemy`. The player's body gets special rules while the player is in the shade |
| `_TargetPriority` | How much hostile AI prefers this unit inside its priority group. 1 = normal, 2 = twice as appealing |
| `_InputDriver`    | *Player Body only.* The `PlayerInputDriver` that shows whether the player is in this body (auto-filled) |

| Function            | Description                                                         |
| :------------------ | :------------------------------------------------------------------ |
| `IsHostileTo(unit)` | True if the other unit is on the opposite team                      |
| `IsVacantBody()`    | True for the player's body while its input driver is off (the player is controlling the shade) |
| `GetTargetPriority()` | Returns `_TargetPriority`                                         |

---
## UnitRegistry
A static list of every active `UnitTeam`, so AI loops over it instead of searching the scene. Units add and remove themselves. It empties itself when play mode starts, so no old units carry over between play sessions.

---
## UnitTargeting
Goes on AI units: enemies and shades. Needs a `UnitTeam` on the same object (added automatically if you add the component in the editor).

### Target rules
Every hostile unit falls into a priority group. **A higher group always wins**, no matter the distance:

| Group           | Who                                                                      |
| :-------------- | :----------------------------------------------------------------------- |
| 1. Attacker     | Anything that hit this unit in the last `_AggroDuration` seconds. This is how the shade pulls enemies off the player's body, even if they were already on their way to it |
| 2. Normal       | Hostile units it can see within `_DetectionRange`                        |
| 3. Vacant Body  | The player's body while the player is in the shade. Only targeted when nothing else is around |

Inside a group, the score is `closeness × _TargetPriority`, where closeness is 1 right next to it and 0 at the edge of the range.

- **Line of sight:** new targets have to be seen (nothing on `_SightBlockers` in the way). The current target and attackers don't, so stepping behind a pillar doesn't make an enemy forget you.
- **Losing a target:** known targets are kept out to `_LoseTargetRange`, which is bigger than the detection range so targets don't flicker at the edge.
- **Stickiness:** it only switches to another target in the same group if that one scores `_SwitchMargin` better.
- **Getting hit** rechecks the target right away instead of waiting for the next check.
- Hits only cause aggro from hostile units (no aggro from friendly fire), and only from units. A trap hit doesn't count.

What a unit **does** about its target (like a coward fleeing from your body instead of attacking it) is up to its [[Unit Brain]].

### Fields

| Field               | Description                                                              | Default |
| :------------------ | :----------------------------------------------------------------------- | :------ |
| `_DetectionRange`   | How far it notices hostile units, m                                      | 15      |
| `_LoseTargetRange`  | How far a known target can get before it gives up, m. Never less than the detection range | 25 |
| `_AggroDuration`    | How long a hit keeps the attacker as top priority, s. Each hit restarts it | 5     |
| `_RetargetInterval` | How often it rechecks, s                                                 | 0.25    |
| `_SwitchMargin`     | How much better a same-group target must score to switch (0-1)           | 0.2     |
| `_NeedsLineOfSight` | Must it see new targets?                                                 | On      |
| `_EyeHeight`        | Eye height for sight checks, m                                           | 1       |
| `_SightBlockers`    | Layers that block sight. Leaves out Units, Projectile and Item           | All but those 3 |

These are per-prefab for now. They'll likely move into the personality presets or the `EnemySO` in step 7c. [[Notes for the future]]

Runtime Data (read only) shows the current target, its group, and the list of recent attackers.

### Functions

| Function           | Description                                                               |
| :----------------- | :------------------------------------------------------------------------ |
| `GetTarget()`      | Who to go after. Null when there's no one                                 |
| `HasTarget()`      | True if there's a target                                                  |
| `GetTargetTier()`  | Which group the target is in (so a brain can react differently to an empty body) |
| `TargetChanged`    | C# event fired with the new target whenever it changes                    |
| `AddAggro(DamagePackage)` | From `IAggroReceiver`. Called by `EnemyDamagable` on every hit. Blames the package's `_Source` |

### Debugging
- Selecting a unit draws its detection range (yellow), lose range (gray) and a red line to its target.
- **Test Aggro From Shade** / **Test Aggro From Player** (Odin buttons, play mode) act like that unit just hit this one.

---
## IUnitMover
The shared way a brain steers a unit (`AIInterface.cs`), implemented by `EnemyMovement` and `NavGuideDriver`:

| Function                  | Description                                                    |
| :------------------------ | :------------------------------------------------------------- |
| `SetDestination(Vector3)` | Go here                                                        |
| `StopMoving()`            | Stop and forget the destination                                |
| `HasArrived()`            | Made it (or nowhere to go)                                     |
| `IsActive()`              | Can take orders right now. False while waiting on the NavMesh, mid-knockback (enemies), or while the player is driving (shade) |

---
## Related
- [[How To - Create Enemies & Shades]]
- [[Enemy Movement]]
- [[Player Movement#NavGuideDriver (AI steering)]]
- [[Damage Receivers & Projectiles]]
