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
- A **Telegraph** part on ability steps: shape (circle, cone, lane, ring), sized from the ability's numbers, fill that tracks the wind-up, locked where aimed or following the caster
- Pooled marker objects (a decal with one shader taking shape and fill values). Basic attacks don't use it
- Stand-in look until [[Design the telegraph markers]]
- The leap landing circle uses it too

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
