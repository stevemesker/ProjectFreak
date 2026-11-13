---
status: To Do
priority: 6
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Ground zones]]"
  - "[[Dash mover]]"
  - "[[Destructible barriers and doors]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Elite, Speed / Defense, Large or Huge), Aggressive preset
- Basic attack: two-hit combo, slash then smash
- **Roar of Chaos:** applied to itself automatically when low on health. Faster attacks, and many attacks leave chaotic flames (fire ground zones)
- **Immovable Chaos:** long charge-up, then charges forward with a very low turn rate. Knocks back what it hits, breaks barriers (Crush tag, Plows Through). With Roar of Chaos it leaves a hellfire trail

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
