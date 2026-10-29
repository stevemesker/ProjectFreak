---
status: To Do
priority: 5
chapter:
  - Chapter 3 Crafting
  - Chapter 4 Shades
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
created: 2026-10-29
done:
---
## What
The real look and wording of the popup that asks before a save commits the shade to evolving (it uses the placeholder Confirm Popup layout now). It's the main feedback before evolving, now that the zone opening's camera move and overcharge pulse were dropped.

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
