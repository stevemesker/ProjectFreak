---
status: To Do
priority: 5
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field 3D Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-28
done:
---
## What
Go through the 3D view scripts together so Steve knows how they fit. 3D won the call on Oct 27, 2026, so this is ready.

## Notes
- First card made on the Obsidian board.

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
