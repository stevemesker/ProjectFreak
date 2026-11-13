---
status: To Do
priority: 2
chapter:
  - Chapter 1 Combat
owner: Steve
plan: "[[Fiend Combat Plan]]"
source: 
needs:
created: 2026-11-09
done:
archived:
---
## What
The list of status effects and what each does: poison, on fire, frozen, chilled, plus unique ones.
- For each: duration, damage over time (element), stat changes, stun, how reapplying works (refresh, stack, strongest), and the look
- Reactions between them (fire thaws frozen...) can wait
- The code side is [[Status effect system]]

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
