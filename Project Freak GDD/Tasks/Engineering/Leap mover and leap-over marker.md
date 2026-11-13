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
  - "[[Telegraph marker system]]"
created: 2026-11-09
done:
archived:
---
## What
- Ability function: airborne for a set time, landing circle on the ground (telegraph marker)
- **Steerable:** the player steers the circle with movement input during the air time, within a max distance from takeoff; the AI aims at its target. Off = lands a set distance ahead (Chaos Dive)
- **Blocked by everything solid unless marked** with a `LeapOverObject` component (crates, railings, rubble). The arc is checked live and the circle stops at the last clear spot
- Only lands on valid ground (NavMesh for enemies). Landing hit: damage, knockback, optional recovery time

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
