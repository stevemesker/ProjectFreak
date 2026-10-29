---
status: To Do
priority: 4
chapter:
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Node Art]]"
created: 2026-10-29
done:
---
## What
Nodes get four forms: **idle (open)**, **plugged in**, **locked**, **frozen** (frozen and locked-in share one look). Several pieces of the model move / rotate between them, animated with curves (timing in the inspector), with a hook for effects on each change.
The rules already know everything needed (plugged, on, locked out, frozen / locked in, gate); this only shows it. Details come with Steve's final model.

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
