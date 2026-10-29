---
status: To Do
priority: 5
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: 
needs:
created: 2026-10-28
done:
---
## What
Node models with four forms: **idle (open)**, **plugged in**, **locked**, **frozen** (locked-in looks the same as frozen). Several pieces move / rotate between forms; the engineering side is [[Node state forms and transitions]].

**To solve while making it:** does powered look different from plugged in, or is that handled by how runes and bridges look when powered?

## Notes
- Brought over from Trello: https://trello.com/c/WFhvC2Yi/34-node-art

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
