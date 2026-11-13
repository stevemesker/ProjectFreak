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
The next combat part after [[Fiend Combat Plan]] (Nov 9, 2026):
- A drop table and drop chance on each EnemySO, spawning the existing `ItemDrop` when it dies (death itself comes with [[Basic enemy death]])
- Units marked **spawned** (hatched scarabs) don't drop loot. The **Living Hive** itself should be a big loot source to make up for it
- XP for kills comes with this part


## Notes
- Brought over from Trello: https://trello.com/c/j6ZszJmM/10-defeated-enemies-drop-items

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
