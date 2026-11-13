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
- A **Stats** section on the EnemySO: HP, STR, DEF, AGI, INT, SPR, resistances and immunities. `EnemyStats` reads it on spawn, so health stops living on the prefab
- No special case for Popcorn: they get real stats ([[Fiend stats and enemy stat curve]])
- Add **Elite** to `EnemyType.Rank` between Lieutenant and MiniBoss so "MiniBoss and up" checks stay right, and fix the dev test enemy's asset
- Placeholder stats per rank so the fiends can be tested before the real numbers
- Update [[Enemy Movement]] (EnemySO) and [[Damage Receivers & Projectiles]] (EnemyStats)

## Completed Info
Nov 9, 2026: built, not checked in Unity yet (let it recompile, check the console, then hit the test enemy)
- **EnemySO** has a **Stats** foldout (`_Stats`, an `EnemyCoreStats`). New enemies start at HP 10 and 1 in each stat. Warning box if HP is 0. Health on the SO is ignored
- **EnemyStats** copies the SO's stats in `Awake` (`EnemyCoreStats.Copy()`, own lists so damage never touches the asset), sets Health to HP and fills the name from `_EnemyName`. On enemies with an EnemySO the prefab's stats are greyed out with an info box. **No EnemySO** (the target dummy) or **HP 0** (warns) keeps the prefab's stats
- **Elite rank** added. Change from the plan: enum values got fixed numbers like `DamageType` (Elite = 5), so it shows between Lieutenant and MiniBoss but nothing saved shifted. No asset used MiniBoss or Boss anyway, and nothing compares ranks by order. Spawners already place Elites (they only skip MiniBoss and Boss)
- **Dev test enemy:** its prefab stats (HP 10, DEF 1, INT 1, SPR 1...) were copied into `SO_Enemy_ChaseTest_Dev`'s Stats, so it plays the same
- **Placeholder stats per rank:** not built as a tool. Each fiend's SO gets its own starting numbers when it's built, until [[Fiend stats and enemy stat curve]]
- Notes updated: [[Enemy Movement]], [[Damage Receivers & Projectiles]], [[Stats & Inventory Data]], [[How To - Create Enemies & Shades]], [[Coding Concepts]] (generics)


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
