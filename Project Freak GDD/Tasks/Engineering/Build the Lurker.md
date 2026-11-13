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
  - "[[Dash mover]]"
  - "[[Telegraph marker system]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Basic, Strength), Cunning or Aggressive preset
- Basic attack: two fairly narrow swipes, no combo effects
- **Lunge:** short wind-up, dashes a short distance forward, stops early on a hit. Only used to close long ranges (AI Use min range)

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
