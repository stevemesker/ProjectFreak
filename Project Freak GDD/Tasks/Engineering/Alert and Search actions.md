---
status: To Do
priority: 7
chapter:
  - Chapter 1 Combat
owner: Claude
plan: "[[Fiend Combat Plan]]"
source: 
needs:
  - "[[Brain attack layer and facing]]"
created: 2026-11-09
done:
archived:
---
## What
- **Alert:** after noticing something (a sound, an ally getting hit), detection range goes up for a while
- **Searching:** after losing its target, goes to the last known spot, looks around, then gives up
- Low priority: fiends are bestial. An "alert others" action comes with the first Passive enemy

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
