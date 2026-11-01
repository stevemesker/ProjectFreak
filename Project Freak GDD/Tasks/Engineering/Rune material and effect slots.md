---
status: To Do
priority: 5
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-29
done:
---
## What
- `ElementItemSO` gets `_RuneMaterial` (optional, empty = the rune prefab's material) and `_RuneEffect` (optional prefab, spawned and parented to the rune), next to `_Palette`
- Used for runes on the field and the stand-ins dragged out of the inventory list
- The effect gets the rune's color too if its shader has the color property
- Write the shared rune shader property names into [[Rune Field 3D View]] so every rune shader can follow them: `_BaseColor` (required), `_RangeHighlight` (optional, the first tier shader doesn't use it)
- **Label mesh:** the rune's color (and a `_RuneMaterial` swap) must skip the label mesh. `Rune3DObject._TintRenderers` already picks which renderers get colored (empty = all), so either set it on the rune prefab or have the code skip the label mesh on its own. Decide which when this is built

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]
- Oct 31, 2026: label mesh and optional `_RangeHighlight` came from the Completed Info on [[Rune tier shaders]]

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
