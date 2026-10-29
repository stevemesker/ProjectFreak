---
status: To Do
priority: 6
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
created: 2026-10-29
done:
---
## What
- Bridges: remove `_StrainProperty`, `ShowStrain` and the strain math on drag (the tear color already is the strain look). Keep **Tear Slack**, the gameplay dead zone
- Zone walls: remove `_OverchargeProperty` and the 0 sent for it (the overcharge pulse was dropped)
- Update [[Rune Field 3D View]] to match

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
