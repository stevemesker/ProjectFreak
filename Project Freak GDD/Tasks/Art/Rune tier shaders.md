---
status: To Do
priority: 6
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
  - "[[Rune tiers and their looks]]"
created: 2026-10-29
done:
---
## What
A shader (and maybe an effect prefab) per rune tier. Every rune shader uses the shared property names (`_BaseColor` for color, `_RangeHighlight` for the range glow) so the field can color any of them.

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
