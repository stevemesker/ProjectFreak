---
status: To Do
priority: 3
chapter:
  - Chapter 0 Game Flow
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-29
done:
---
## What
- Where access points go: the hub lab (it moves halfway through the game), one right before each boss, rare mid-dungeon safe rooms
- What the hub can do that a dungeon **field kit** can't (ideas: shard slotting, rune crafting, switching shades)
- The dungeon lamp's story and Hazen's lines when finding one ("A keeper must have left this here before the purge", "Master said keepers liked to hide these in plain sight")

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]

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
