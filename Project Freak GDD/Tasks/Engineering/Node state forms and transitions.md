---
status: Done
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
The rules already know everything needed (plugged, on, locked out, frozen / locked in, gate); this only shows it.

**Decided Nov 3, 2026 (with the final model, [[Node Art]]):** the four forms are really two separate parts, each with one question:
- **Cover** (turns on X): **closed** while no runes can go in or come out, meaning the node is locked out or holds a locked-in rune (frozen). Open (180°) otherwise. Gates the shade didn't take are locked out, the taken gate is locked in, so both close
- **Shield** (slides on Y): **up** while the node is on (a powered rune plugged in). Down (−0.1) otherwise, locked or not. Plugged in with no power is just the normal open look

| Node | Cover | Shield |
| :--- | :--- | :--- |
| Empty, or plugged with no power | open | down |
| Powered rune plugged in | open | up |
| Locked out | closed | down |
| Locked in / frozen, powered | closed | up |
| Locked in / frozen, no power | closed | down |

Powered doesn't get its own look beyond the shield; runes and bridges show power their own way.

## Completed Info
Built Nov 3, 2026, waiting on Steve's test in Unity. Details in [[Rune Field 3D View]] (Node3DObject):
- `Node3DObject.ShowCoverAndShield(coverClosed, shieldUp, animate)`: finds `Mesh_RuneField_Node_Cover` / `_Shield` itself, reads the model's pose as closed / up, and moves each part with its own time and curve (**Cover & Shield** foldout). A change mid-move blends from where the part is; a saved field loading snaps instead of playing
- `RuneField3DObject.RefreshView` sends cover closed = `IsNodeBlocked || IsNodeLockedIn` and shield up = `IsNodeActive`
- `RuneField.IsNodeLockedIn(index)`: the node holds a locked-in rune
- Effect hooks: `_CoverClosedEvent` / `_CoverOpenedEvent`, and the existing `_ActivationEvent` / `_DeactivationEvent` for the shield
- Play mode test buttons: **Test Cover** / **Test Shield**

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]
- Cover / shield rules settled with Steve, Nov 3, 2026

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
