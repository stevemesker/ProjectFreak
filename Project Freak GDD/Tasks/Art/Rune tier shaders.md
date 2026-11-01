---
status: Archived
priority: 1
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source:
needs:
  - "[[Rune tiers and their looks]]"
created: 2026-10-29
done: 2026-10-31
archived: 2026-10-31
---
## What
A shader (and maybe an effect prefab) per rune tier. Every rune shader uses the shared property names (`_BaseColor` for color, `_RangeHighlight` for the range glow) so the field can color any of them.

## Completed Info
finished the first basic shader, uses `_Basecolor` as its color. Won't need `_RangeHighLight` anymore, I'm satisfied with the look for now. Need to update3 the algorithm to not apply the color change to the label mesh's material

**Review (Oct 31, 2026):**
- Label mesh shouldn't be colored → added to [[Rune material and effect slots]] (pick the tinted renderers so the label mesh is skipped, and don't swap its material)
- `_RangeHighlight` no longer required → [[Rune Field Finish Plan]] and [[Rune material and effect slots]] now say only `_BaseColor` is required, `_RangeHighlight` is optional (shaders without it ignore it, the `RangeEffect` child still shows)
- `_Basecolor` in the note is a typo: the shader uses `_BaseColor` and Steve tested it with the field colors (Oct 31, 2026)

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
