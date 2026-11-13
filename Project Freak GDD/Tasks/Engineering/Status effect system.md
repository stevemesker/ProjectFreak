---
status: To Do
priority: 2
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: 
needs:
created: 2026-11-09
done:
archived:
---
## What
- **`StatusEffectSO`** with parts picked in the inspector: Damage Over Time, Stat Modifier, Stun, Invisible, Visual. Reapply rule: refresh, stack or strongest
- **`UnitStatus`** on every unit: effects are small records (effect, source, stacks, end time, next tick). Joins one shared update list only while it has effects
- Timers store end times, damage ticks on an interval, stat changes cached when an effect starts or ends
- **Apply Status** ability function
- Invisible: viewer-side look (ally vs hostile material), and targeting can't pick an invisible hostile as a new target
- Which effects exist: [[Define status effects]]

## Completed Info


## Notes
- From the fiend combat talk, Nov 8–9, 2026. Decisions in [[Fiend Combat Plan]]

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
