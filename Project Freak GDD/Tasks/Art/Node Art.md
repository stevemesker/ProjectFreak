---
status: Archived
priority: 1
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source:
needs:
created: 2026-10-28
done: 2026-11-03
archived: 2026-11-08
---
## What
Node models with four forms: **idle (open)**, **plugged in**, **locked**, **frozen** (locked-in looks the same as frozen). Several pieces move / rotate between forms; the engineering side is [[Node state forms and transitions]].

**To solve while making it:** does powered look different from plugged in, or is that handled by how runes and bridges look when powered?

## Completed Info
![[Pasted image 20261103130727.png]]

![[Pasted image 20261103130756.png]]

Node has been made. It is composed of 4 sub-parts: Cover, Shield, Tray, and Rim. 

By default the node will be in its locked position, so rotations and translations are set to 0. The Tray and the Rim don't move at all. For idle, the cover is turned 180 degrees and the shield is down -0.1 in the y translation. Plugged in moves the shield up to its 0 on translation. For locked and frozen the cover's x rotation goes to 0. The shield is only up (0) when the node has a powered rune; locked or frozen with no power, it stays down (-0.1). How the code uses this: [[Node state forms and transitions]]

**Review (Nov 8, 2026):** no new cards. It answered the open question: powered doesn't get its own look beyond the shield. Effects and sounds on the cover and shield wait for the future sound pass
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
