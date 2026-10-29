---
status: To Do
priority: 5
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: 
source: "[[Zones - Art To Do]]"
needs:
created: 2026-10-28
done:
---
## What
Art for the rune field itself and its windows.

## Notes
- Brought over from Trello: https://trello.com/c/gPkf9ve4/33-rune-field-art

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
