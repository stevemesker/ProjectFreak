---
status: To Do
priority: 5
chapter:
  - Chapter 1 Combat
owner: 
plan: 
source: 
needs:
created: 2026-10-28
done:
---
## What


## Notes
- Brought over from Trello: https://trello.com/c/YfacLq8U/17-weapon-scrolling-mouse

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
