---
status: To Do
priority: 4
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Build the Scarab]]"
  - "[[Ground zones]]"
  - "[[Telegraph marker system]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Basic, Speed), **Flying** (hover height, ignores ground zones, melee can still hit it). Keep Distance instead of Chase
- Basic attack: short thrust that pierces several enemies, single hit
- **Stinger Missile:** small straight projectile, short cooldown
- **Sap Grenade:** charges up, lobs a ball of goo that leaves a small slow patch

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
