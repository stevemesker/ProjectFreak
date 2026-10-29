---
status: To Do
priority: 4
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-29
done:
---
## What
- The pointer line / pole from a dragged rune down to where it lands
- The preview bridge material (a warped / ghosted variant of the bridge material that straightens out once in range)
- A red "can't land here" look on the range overlay decal

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
