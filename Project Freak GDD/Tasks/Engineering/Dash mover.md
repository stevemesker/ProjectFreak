---
status: To Do
priority: 3
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
- Ability function: a straight line to a point, stopping early at walls, and on a hit if set
- Optional turn rate while dashing (the Juggernaut's charge)
- Enemies slide along the NavMesh like knockback; physics units use their engine. Blocked by everything, leap-over objects included
- **Plows Through:** keeps going through destructibles it breaks
- Damage and knockback on contact, with optional tags on the package (Crush)

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
