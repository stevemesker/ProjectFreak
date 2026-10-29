---
status: To Do
priority: 6
chapter:
  - Chapter 4 Shades
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
  - "[[Form display names and flavor text]]"
created: 2026-10-29
done:
---
## What
A display name and flavor text for every shade form, once [[Form display names and flavor text]] adds the fields.

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
