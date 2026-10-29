---
status: To Do
priority: 5
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Core shader]]"
created: 2026-10-29
done:
---
## What
The core sends its shader:
- **Glow:** spare power as 0 to 1. 0 spare = the regular look, full glow at 10 spare (tunable on the core)
- **Over budget:** on / off while runes are waiting for power, blended over a short time so it doesn't pop
Property names are inspector fields in the core's Shader Properties foldout. The shader does the dulling and strobing.

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
