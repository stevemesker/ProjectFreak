---
status: To Do
priority: 6
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
  - "[[Rune field lighting and style test]]"
created: 2026-10-29
done:
---
## What
A light on the rune field camera rig aimed at the camera's focus point (Diablo-style), on top of a fixed key light. The zone lights keep copying the fixed key light.
Starts once Steve's lighting and style test shows the ideal look, then gets adjusted to it.

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]

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
