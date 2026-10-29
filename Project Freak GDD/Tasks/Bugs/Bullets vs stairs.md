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
Bullets travel in a straight line parallel with the world space plane, so when they run into stairs they collide with them and you can't hit anything up the stairs. Need to decide how bullets navigate terrain:
- Do they all raycast down and follow the ground they're on like a player?
- Do I do a calculation aim so they're pointed at the nearest enemy in front of them?
- Do I leave it as is?

## Notes
- Brought over from Trello: https://trello.com/c/x6vFAodM/57-bullets-vs-stairs

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
