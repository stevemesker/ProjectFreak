---
status: To Do
priority: 3
chapter:
  - Chapter 1 Combat
  - Chapter 4 Shades
owner: Both
plan: "[[Fiend Combat Plan]]"
source: 
needs:
  - "[[Melee Combo and Fire Projectile functions]]"
  - "[[Leap mover and leap-over marker]]"
created: 2026-11-09
done:
archived:
---
## What
When the player drives a shade (or Hazen uses an ability), the cast context comes from input.
- Aim from the same aim the weapons use
- Basic attack button runs the shade's basic attack, each press the next combo hit
- Leap steering from movement input, mouse vs gamepad aiming for point abilities
- Cooldowns shown somewhere simple until the HUD art

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
