---
status: To Do
priority: 5
chapter:
  - Chapter 5 Art
owner: Steve
plan: 
source: 
needs:
created: 2026-10-28
done:
---
## What


## Notes
- Brought over from Trello: https://trello.com/c/wGdsUh8A/26-shadow-wall

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
