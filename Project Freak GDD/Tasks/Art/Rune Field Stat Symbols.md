---
status: To Do
priority: 1
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan:
source:
needs:
created: 2026-10-28
done:
---
## What


## Notes
- Brought over from Trello: https://trello.com/c/xJy0tr2j/35-rune-field-stat-symbols

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
