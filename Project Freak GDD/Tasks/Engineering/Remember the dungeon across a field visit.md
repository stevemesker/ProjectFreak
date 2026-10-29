---
status: To Do
priority: 4
chapter:
  - Chapter 0 Game Flow
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Known Issues]]"
needs:
created: 2026-10-29
done:
---
## What
Going to the rune field from a dungeon access point unloads the dungeon, so the Dungeon Manager keeps what matters and puts it back on return:
- The floor layout (fixes the [[Known Issues]] bug: `DungeonManager.MoveToFloor` picks a random floor scene without saving it)
- Cleared rooms (no respawns), opened chests, taken pickups: objects get IDs, register with the manager and ask "was I already used?"
- The spawn spot at the access point
Access rooms only work once cleared. This is most of what mid-dungeon save and quit needs for [[Save slots to disk]].

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
