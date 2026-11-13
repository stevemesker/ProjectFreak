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
Pull the attack code out of the weapons so weapons and abilities share it.
- Move combo timing, the swing sweep, hit checks, damage, knockback and stagger out of `WeaponAttackMelee` into one shared runner, along with the "next press continues the combo, a pause resets it" input logic
- Same for projectile firing in `WeaponAttackRanged`
- Weapons stay items and hand the runner their combo / firing data. The runner reports a phase per combo hit (Wind Up, Active, Recovery) for animations
- **Check:** Hazen's sword and guns play exactly the same afterwards
- Update [[Melee Weapon System]] and [[Ranged Weapon System]]

## Completed Info
Nov 9, 2026: built, not checked in Unity yet. **Check:** the radiant sword and the guns should play exactly the same as before
- **`MeleeComboRunner`** (plain class): the whole combo moved out of `WeaponAttackMelee` unchanged (press, hold, buffer, reset after a pause, wind up / active / recovery, sweep and hit checks, facing lock, slow, dash cancel, gizmo). `WeaponAttackMelee` now just owns one and passes presses in
- **`RangedPatternFirer`** (plain class): pattern spawning, spread / origin offsets and all of hit scan moved out of `WeaponAttackRanged`. The weapon keeps its trigger flow (warm up, charge, automatic, finishers, busy) since that's about its own input
- **Two interfaces** say what the runners need: `IMeleeComboSource` (on `MeleeWeaponItem`, which now builds each hit's damage and stagger) and `IRangedFireSource` (on `WeaponRangedItem`). The ability functions fill them next. No asset data changed
- **Phases:** the melee runner fires `PhaseChanged` (`CombatType.Phase`: None, Wind Up, Active, Recovery) per hit. Ranged doesn't report phases yet (added with the animation bridge)
- Notes updated: [[Melee Weapon System]] (Attack Runner), [[Ranged Weapon System]] (Pattern Firer), [[Interface List]], [[Coding Concepts]]


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
