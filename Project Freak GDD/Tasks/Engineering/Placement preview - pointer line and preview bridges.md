---
status: To Do
priority: 3
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Placement pointer and preview bridge art]]"
created: 2026-10-29
done:
---
## What
While a rune is being placed (mouse drag, gamepad carry, or dragged out of the list), show how it will land and connect:
- A pointer line (Steve's pole / line) from the dragged rune down to where it would land
- A preview bridge for each **new** bridge the drop would make, in a variant of the bridge material, straightening out once in range. Bridges the rune already has behave as now
- The range overlay gets a "can't land here" value so its decal can tint red when no spot nearby works; then the rune's own red tint can go
- No ground ring (the range overlay already covers it)
- Its own component, like the range overlay. The field already works out the landing spot (`FindDropSpot`), what it would bridge to (`FindBridgeTargets`) and the reach

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
