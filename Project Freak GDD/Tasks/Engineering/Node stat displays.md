---
status: In Progress
priority: 2
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-28
done:
---
## What
The info popup for runes, nodes and the core (merged with this card Oct 29, 2026):
- **Hover:** a small name tag by the pointer after a short delay
- **Select:** full details in a docked panel (placeholder: bottom third of the right panel, until the side panel art pass decides)
- **Mouse:** a quick tap selects, holding picks a rune up (a tunable time, about 0.2 s). **Gamepad:** right stick click selects, for now
- **Rune:** name, element, power cost, reach, bridges used / max, stat changes, powered / frozen / locked in
- **Node:** name, effects, unlocks and lockouts, gate (and which form), state
- **Core:** power used / total, runes waiting, shard
- Shares "selected" with [[Node link tool and display]]. Hooks into the field's `OnHoverChanged`

## Notes
- Steve to retitle in Obsidian: "Info popup for runes, nodes and core"
- Decisions in [[Rune Field Finish Plan]]
- Brought over from Trello: https://trello.com/c/ULr1SksJ/32-node-stat-displays

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
