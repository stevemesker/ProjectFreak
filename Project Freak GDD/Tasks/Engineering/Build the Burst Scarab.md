---
status: To Do
priority: 2
chapter:
  - Chapter 1 Combat
owner: Both
plan: "[[Fiend Combat Plan]]"
source: "[[Fiend Enemies]]"
needs:
  - "[[Build the Scarab]]"
  - "[[Ground zones]]"
created: 2026-11-09
done:
archived:
---
## What
- EnemySO (Popcorn), Aggressive preset
- Basic attack: the same short thrust as the Scarab
- **Detonate:** spawns a small poison patch (magic damage over a short time) at its spot and kills itself through the normal death path

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
