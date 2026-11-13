---
status: To Do
priority: 5
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Build the Scarab]]"
  - "[[Leap mover and leap-over marker]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Lieutenant, Strength / Speed), Cunning preset
- Basic attack: two-hit combo, wide swing then forward thrust
- **Chaos Dive:** short charge-up, leaps and crashes down a set distance ahead (not steerable). Massive damage and knockback under the landing, then a recovery window

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
