---
status: Done
priority: 1
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: 
needs:
created: 2026-11-09
done: 2026-11-09
archived:
---
## What
Bare-bones death so fights can be tested. Loot and XP are the next combat part ([[Defeated enemies drop items]]).
- At 0 health: the brain, abilities, statuses and colliders turn off and the unit leaves `UnitRegistry` (nothing keeps targeting a corpse)
- A death phase goes through the animation bridge (the greybox shrinks or fades it), then it despawns after a delay. Pooled units go back to their pool
- Self-destruct (Burst Scarab's Detonate) uses the same path
- Units spawned by another unit (hatched scarabs) are marked as **spawned**, so the loot part can skip them

## Completed Info
Nov 9, 2026: built, not checked in Unity yet (test together with [[Enemy stats on the EnemySO]])
- New **`EnemyDeath`** (`IKillable`): `Kill()` turns off the brain, targeting, stagger, abilities, movement and agent, every collider, then `UnitTeam` last (leaves the registry; targeting already drops turned-off targets). Fires `_OnDied` and the `Died` C# event, waits, then destroys it
- **EnemySO Death foldout:** despawn delay (1.5 s) and a stand-in shrink with a curve, until death animations
- **No prefab setup:** `EnemyStats` adds `EnemyDeath` at runtime to anything with an EnemySO. The target dummy (no EnemySO) still never dies
- `EnemyStats` ignores hits once dead; `EnemyDamagable` skips aggro, knockback and stagger on the killing hit
- `MarkAsSpawned` / `WasSpawned` for the loot pass. **Test Kill** button
- Change from the plan: the death phase isn't sent through the animation bridge yet (that card isn't built). The shrink stands in, and the bridge hooks into `Died` when it's made
- Still to do elsewhere: pooled units going back to a pool (Living Hive), turning off status effects (Status effect system)
- Notes updated: [[Enemy Movement]], [[Damage Receivers & Projectiles]], [[Interface List]], [[How To - Create Enemies & Shades]]


## Notes
- From the fiend combat talk, Nov 8–9, 2026. Decisions in [[Fiend Combat Plan]]

## Waiting on this
Cards that link to this one, usually through their `needs`.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Waiting on this
    order:
      - file.name
      - file.folder
      - status
      - owner
```
