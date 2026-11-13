---
status: To Do
priority: 6
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: "[[Destructible Objects]]"
needs:
created: 2026-11-09
done:
archived:
---
## What
Build the destructible system from [[Destructible Objects]] for barriers and doors (walls and buildings in POIs come after the demo).
- `EnvironmentDamageable`: tier (Small, Traps, Medium, Heavy) and material, breaking from the damage package (attack type, element, stagger power)
- **Tags on the [[Damage Package]]** (Crush first, room for Hack, Pierce...) that a destructible can ask for
- Break effect hook, and NavMesh updates if a barrier was blocking a path

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
