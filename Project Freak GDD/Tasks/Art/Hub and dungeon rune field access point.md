---
status: To Do
priority: 5
chapter:
  - Chapter 5 Art
  - Chapter 0 Game Flow
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
  - "[[Rune field access points]]"
created: 2026-10-29
done:
---
## What
- The hub's big projection wall / lamp and the small keeper's lamp found in dungeons
- The start animations (timelines) for sitting down at each one

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
