---
status: To Do
priority: 5
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Build the Burst Scarab]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Lieutenant, Defense), immobile (no mover actions), no basic attack
- **Hatch Scarab:** an egg rolls out and bursts into a Scarab or Burst Scarab, up to a cap on the field (10 total until destroyed)
- Hatched scarabs come from a **pool**, are marked **spawned** (no loot), and despawn if they find no enemies after a short time
- Needs a **Spawn Unit** ability function (the interpreter's `SummonUnit` is a start)
- Later: the hive itself is a big loot source

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
