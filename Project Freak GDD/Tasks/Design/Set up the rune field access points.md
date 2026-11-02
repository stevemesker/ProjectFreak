---
status: To Do
priority: 3
chapter:
  - Chapter 0 Game Flow
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune field access points]]"
needs:
created: 2026-11-01
done:
archived:
---
## What
Finish the design side of the access points, picked up from the review of [[Rune field access points]]:
- Format the access point notes ([[Rune Field Access Point]], [[Hub Scene]] and the notes they link)
- Write up what Advanced and Basic each allow. To start: both edit runes and slot shards, only Advanced edits every shade slot
- Hazen's lines and the keeper's lamp story for dungeon access points
- Talk through any architecture changes before [[Load the rune field from an access point]] is built

## Completed Info


## Notes
- From the weekly review, Nov 1, 2026. Launching is two functions called from the event timeline (one per type), not a profile asset
- Access point art is done with the level art, not its own card

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
