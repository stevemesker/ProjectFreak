---
status: Done
priority: 6
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
  - "[[Rune field lighting and style test]]"
created: 2026-10-29
done: 2026-11-12
---
## What
A light on the rune field camera rig aimed at the camera's focus point (Diablo-style), on top of a fixed key light. The zone lights keep copying the fixed key light.
Starts once Steve's lighting and style test shows the ideal look, then gets adjusted to it.

## Completed Info
Nov 12, 2026: built, not checked in Unity yet (let it recompile and check the console)
- **The light:** Steve's **Spot Light** under `Rune Field Camera` (15 m up, pointing down, 400 lux at 15 m, warm white). Being a child of the rig is what makes it follow the focus, no code needed
- **New `RuneFieldFocusLightObject`** on the Spot Light: widens the cone as the camera zooms out, so the pool covers more of the field. Set by the pool's size on the floor (`_PoolSizeZoomedIn` 17 m / `_PoolSizeZoomedOut` 40 m, `_PoolGrowth` curve), the angle is worked out from the light's height. Range is set so it always reaches the pool's edge. `_PreviewZoom` slider to tune in edit mode
- **Changed from the plan:** the zone lights no longer copy a key light in this look. The light-based zone tint is being removed ([[Strip out zone state lighting]])
- [[Rune Field 3D View]] updated (new RuneFieldFocusLightObject section and setup step)

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
