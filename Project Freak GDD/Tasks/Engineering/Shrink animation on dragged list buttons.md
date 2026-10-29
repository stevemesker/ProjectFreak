---
status: To Do
priority: 8
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Side panel and rune list art pass]]"
created: 2026-10-29
done:
---
## What
When a rune button is dragged out of the inventory list, it shrinks into a circle before hiding (and grows back over the list), driven by a curve and a time. Feels OK without it, so it waits for the final UI art.

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
