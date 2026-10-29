---
status: To Do
priority: 6
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Bridge break effect prefab]]"
created: 2026-10-29
done:
---
## What
One **Break Effect** prefab field on the rune field. When a bridge tears or a save cuts it, spawn the effect at the bridge's middle, turned along it. The prefab deletes itself (`TimedDelete`, or the particle system's Stop Action set to Destroy). Same effect for tears and cuts.

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
