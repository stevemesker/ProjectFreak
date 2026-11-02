---
status: Done
priority: 6
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
created: 2026-10-29
done: 2026-11-01
---
## What
- Bridges: remove `_StrainProperty`, `ShowStrain` and the strain math on drag (the tear color already is the strain look). Keep **Tear Slack**, the gameplay dead zone
- Zone walls: remove `_OverchargeProperty` and the 0 sent for it (the overcharge pulse was dropped)
- Update [[Rune Field 3D View]] to match

## Completed Info
Nov 1, 2026: removed, not checked in Unity yet (let it recompile and check the console)
- **Bridges** (`Bridge3DObject`): `_StrainProperty`, the `_Strain` read-only value and `ShowStrain` are gone, and so is the strain math in `RuneField3DObject.UpdateTearing`. **Tear Slack** stays as the dead zone
- **Zone walls:** `_OverchargeProperty` is gone from `RuneFieldZonesSO`, and `ZoneBorder3DObject` no longer sends a 0 for it
- [[Rune Field 3D View]] updated. The old values drop off the prefab and zone asset by themselves the next time they're saved

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
