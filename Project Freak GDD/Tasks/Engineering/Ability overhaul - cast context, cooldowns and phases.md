---
status: To Do
priority: 1
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: "[[AI Movement & Dungeon Loading Plan]]"
needs:
created: 2026-11-09
done:
archived:
---
## What
Extend `AbilitySO` so one ability works for Hazen, shades and enemies, from player input or AI.
- **Cast context** handed to every ability: caster, aim direction, aim point, target. Functions never check who's driving
- **Per-unit cooldowns** using `_AbilityCooldown`
- **Per step** (default on the ability): move multiplier (1 free, 0 rooted), turning (free or locked to aim), blocks other abilities
- **Per step phase:** Wind Up, Active, Recovery or None, plus an optional animation name. Events when a phase starts
- **AI Use section:** min / max range, when it's a good idea (close distance, finish someone, low health), weight, needs line of sight
- Fix the step Timing bug ([[Known Issues]]), add a busy check
- Update [[Ability System]]

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
