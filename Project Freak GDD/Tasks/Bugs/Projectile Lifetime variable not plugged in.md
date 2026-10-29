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
I'm setting the lifetime in the projectile prefab but it's not taking it from the SO. Could probably get rid of the SO one, but it might be worth keeping for things like timed explosions. There's probably a better way to deal with this.

## Notes
- Brought over from Trello: https://trello.com/c/NxzVYb9U/58-projectile-lifetime-variable-not-plugged-in

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
