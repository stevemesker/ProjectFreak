---
status: To Do
priority: 5
chapter:
  - Chapter 0 Game Flow
owner: Both
plan: "[[Save Data Plan]]"
source: "[[Known Issues]]"
needs:
created: 2026-10-29
done:
---
## What
Write save slots to disk so a built game keeps shades, rune fields and progress. Everything lives on ScriptableObject assets right now, which only keep changes in the editor. What to save, and the stable asset ID problem, are in [[Save Data Plan]].
Not part of finishing the rune field.

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
