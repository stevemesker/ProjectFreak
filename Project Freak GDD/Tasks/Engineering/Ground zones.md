---
status: To Do
priority: 2
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: 
needs:
  - "[[Status effect system]]"
created: 2026-11-09
done:
archived:
---
## What
- **`GroundZoneObject`**: spawned by an ability, radius and lifetime, applies its status to anyone standing in it. Poison patch, sap patch and fire trail are one object with different settings
- Hits hostile factions only by default (`IsHostileTo`), **Hits Everyone** checkbox for chaotic spells and traps
- **Flying** checkbox on the EnemySO; flyers ignore ground zones
- **Spawn Ground Zone** ability function

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
