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
  - "[[Telegraph marker system]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Lieutenant, Strength / Speed), Cunning or Timid preset, Keep Distance
- Basic attack: single projectile
- **Fiendish Archer:** 3 projectiles in quick succession
- **Sharp Shooter:** charges and fires a slow arrow; when a hostile crosses in front it locks on and tracks it with a very slow turn, so it can be dodged. Needs a homing projectile option

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
