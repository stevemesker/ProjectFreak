---
status: To Do
priority: 6
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Status effect system]]"
  - "[[Dash mover]]"
  - "[[Leap mover and leap-over marker]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Elite, Strength / Speed), Cunning preset
- Basic attack: 3-hit combo, two swipes and a thrust
- **Stalker Cloaking:** invisible status for a short time; it repositions for a surprise attack (a reposition action)
- **Stalker Backstab:** needs the cloak. Cloak drops, it charges up (open for a second), then lunges a short distance in a massive attack
- **Stalker Flight:** leaps high and lands within range of its takeoff point. In or out of cloak
- Cloak look: [[Cloak and invisible shaders]]

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
