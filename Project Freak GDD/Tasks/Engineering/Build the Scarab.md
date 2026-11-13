---
status: To Do
priority: 2
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Enemy stats on the EnemySO]]"
  - "[[Basic enemy death]]"
  - "[[Brain attack layer and facing]]"
  - "[[Melee Combo and Fire Projectile functions]]"
  - "[[Animation bridge and greybox telegraph]]"
created: 2026-11-09
done:
archived:
---
## What
The first fiend, and the test for everything in the foundation.
- EnemySO (Popcorn, Small), Aggressive preset, greybox prefab
- Basic attack: a very short thrust (Melee Combo, one step). No abilities
- **Swarm test:** 50 on the player. Watch the feel and the frame time. Attack limits wait for this ([[Fiend Combat Plan#Swarms]])

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
