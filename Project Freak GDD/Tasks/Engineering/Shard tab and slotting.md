---
status: To Do
priority: 4
chapter:
  - Chapter 3 Crafting
  - Chapter 4 Shades
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Orbiting shard models]]"
created: 2026-10-29
done:
---
## What
- A **Shards** tab in the side panel
- Slotting: drag a shard onto the core, or A on a shard then A on the core. Picking a shard up glides the camera to the core
- Part of the draft: the power shows right away, saving makes it permanent and asks first (like the lock-in and evolve questions). Discard / cancel gives it back
- **One shard per shade, for good:** once the core has one, the tab and its entries grey out. A **Shards can be replaced** checkbox on the rune field settings (off) keeps it reversible
- Uses a stand-in for the orbiting shard until the art exists

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
