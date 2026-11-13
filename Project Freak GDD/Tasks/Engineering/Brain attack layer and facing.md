---
status: To Do
priority: 1
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
- The brain runs **two layers**: a movement action (Chase, Keep Distance, Flee...) and an attack choice (which ready ability scores best from its AI Use section, if any)
- A rooted step pauses the movement action until it ends; mover abilities take movement over while they run
- **Engage goes away**; fight range comes from the abilities instead of `_FightRange`
- Enemies **turn to face** their target while attacking or holding still
- Scores list in Runtime Data shows the attack choices too
- Update [[Unit Brain]] and [[How To - Create Enemies & Shades]]

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
