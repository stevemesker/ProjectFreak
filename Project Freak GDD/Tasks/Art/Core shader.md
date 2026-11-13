---
status: Archived
priority: 2
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
created: 2026-10-29
done: 2026-11-07
archived: 2026-11-08
---
## What
The core's look: glows brighter with spare power (full at 10 spare), the regular look at 0, and dulls with a strobing color while over budget (runes waiting for power).

## Completed Info
Created two shaders: Orb and Vortex. They both use a depleted value to change to the red color to show when there's too many runes for the core
- The spare power glow is on hold until playtesting shows it's needed (Steve, Nov 7, 2026)

**Review (Nov 8, 2026):** no new cards. Moving the orb into the VFX Graph already has its card, [[Move the core orb into the VFX Graph]]. The glow on hold is tracked in [[Rune Field Finish Plan]] and [[Rune Field 3D View]]

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
