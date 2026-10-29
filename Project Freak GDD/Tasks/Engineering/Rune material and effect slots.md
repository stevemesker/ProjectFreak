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
- Write the shared rune shader property names (`_BaseColor`, `_RangeHighlight`) into [[Rune Field 3D View]] so every rune shader can follow them

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
