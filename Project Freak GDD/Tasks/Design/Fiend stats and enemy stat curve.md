---
status: To Do
priority: 3
chapter:
  - Chapter 1 Combat
owner: Steve
plan: "[[Fiend Combat Plan]]"
source: "[[Damage Balance]]"
needs:
created: 2026-11-09
done:
archived:
---
## What
- The enemy stat curve per dungeon (still undecided in [[Damage Balance]])
- Stats for each Chapter 1 fiend. No special case for Popcorn: a player at the dungeon's level should out-damage their health
- Ability numbers as each fiend gets built (ranges, cooldowns, damage)

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
