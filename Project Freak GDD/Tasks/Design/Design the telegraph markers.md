---
status: To Do
priority: 3
chapter:
  - Chapter 1 Combat
  - Chapter 5 Art
owner: Steve
plan: "[[Fiend Combat Plan]]"
source: 
needs:
created: 2026-11-09
done:
archived:
---
## What
Wildstar-style ground markers for abilities (basic attacks don't get one).
- Shapes: circle, cone, lane, ring (and any others)
- How the fill shows the wind-up, colors for hostile vs ally, the leap landing circle
- The look / shader for the markers
- The code side is [[Telegraph marker system]]

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
