---
status: To Do
priority: 5
chapter:
  - Chapter 0 Game Flow
owner: 
plan: 
source: 
needs:
created: 2026-10-28
done:
---
## What


## Notes
- Nov 9, 2026: shades, enemies and Hazen share one ability system that works from player input or AI, so shade abilities are built the same way as fiend abilities. See [[Fiend Combat Plan]]
- Brought over from Trello: https://trello.com/c/gFTd5ors/76-shade-abilities

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
