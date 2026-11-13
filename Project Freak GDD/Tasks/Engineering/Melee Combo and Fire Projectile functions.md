---
status: To Do
priority: 1
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: 
needs:
  - "[[Shared attack runner]]"
  - "[[Ability overhaul - cast context, cooldowns and phases]]"
created: 2026-11-09
done:
archived:
---
## What
Ability functions that run the shared attack runner, so basic attacks are abilities.
- **Melee Combo:** runs a list of `ComboStepEntry`s with `SwingShapeSO`s. For the AI one use plays the whole combo; for a player-driven shade each press does the next hit
- **Fire Projectile:** fires from the cast context's aim, with count and spacing (for Fiendish Archer's 3 shots)
- Related: [[Projectile Lifetime variable not plugged in]], worth settling here

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
