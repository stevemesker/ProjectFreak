---
status: To Do
priority: 3
chapter:
  - Chapter 0 Game Flow
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Rune field access points]]"
created: 2026-10-29
done:
---
## What
- **A plain scene swap** (not loaded on top): an access point's timeline cutscene calls a wrapper, the Scene Manager fades and loads the field scene, remembering where to come back to. Leaving the field (after the save question) loads that scene back at the access point
- An **access profile** asset handed to the field (checkboxes: can slot shards, can switch shades...). "Hub" and "Field Kit" profiles to start
- Remember which lab the player came from (it changes halfway through the game)
- No camera blend or pause needed any more

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
