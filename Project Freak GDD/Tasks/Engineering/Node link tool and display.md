---
status: To Do
priority: 4
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
  - "[[Node link textures and shaders]]"
created: 2026-10-29
done:
---
## What
Lines from a node to every node it unlocks or locks out:
- **Hand drawn by Steve**, with a tool like the floor cracks (points with scene handles, a strip mesh along them, a repeating link texture)
- Separate materials for unlock links and lockout links
- Editor-only warning when a node unlocks or locks out a node with no drawn link
- Shown on **hover and selection** (shares "selected" with the info popup). Links running off screen get edge arrows or the camera fits them (pick when building)

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
