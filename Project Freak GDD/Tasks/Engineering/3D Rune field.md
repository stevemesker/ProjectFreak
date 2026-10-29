---
status: In Progress
priority: 1
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field 3D Plan]]"
source:
needs:
created: 2026-10-28
done:
---
## What
The 3D rune field as a whole. What's left before it counts as finished is in [[Rune Field Finish Plan]] (planned Oct 29, 2026), with a card per piece.

## Notes
- Brought over from Trello: https://trello.com/c/tThSWwx9/80-3d-rune-field

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
