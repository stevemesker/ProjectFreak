---
status: To Do
priority: 2
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: 
needs:
  - "[[Ability overhaul - cast context, cooldowns and phases]]"
created: 2026-11-09
done:
archived:
---
## What
- **`UnitAnimationBridge`** on every unit: when a phase starts, sets Animator parameters by name (a trigger, plus 0–1 wind-up progress). Skips units with no Animator
- **`GreyboxTelegraph`** (temporary): tints renderers toward a charge color during wind-up (stronger as it fills), flashes when active, dulls during recovery, shrinks on death. Deleted from prefabs when real animations come in
- Listen to `MeleeComboRunner.PhaseChanged` (weapons and Melee Combo) and `EnemyDeath.Died` for the death phase (the EnemySO's shrink is the stand-in until then)
- Add phases to `WeaponAttackRanged` too (charge = wind up, fire = active, cycle wait = recovery), from [[Shared attack runner]]

## Completed Info


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
