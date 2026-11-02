---
status: In Progress
priority: 3
chapter:
  - Chapter 0 Game Flow
  - Chapter 3 Crafting
owner: Both
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Rune field access points]]"
created: 2026-10-29
done:
---
## What
- **A plain scene swap** (not loaded on top): an access point's timeline cutscene calls a wrapper, the Scene Manager fades and loads the field scene, remembering where to come back to. Leaving the field (after the save question) loads that scene back at the access point
- **Two launch functions, one per access point type**, called from the event timeline: **Advanced** (hub labs) and **Basic** (dungeon entrances, before bosses, Safe and Vault Floors). Every access point is the same prefab; only the function its timeline calls changes
- What each type allows is written in code, not set on the prefab, so changing it later doesn't touch prefabs or timelines. To start: both can edit runes and slot shards, only Advanced can edit every shade slot (Basic edits the active one)
- The chosen type has to survive the scene swap, so the launch function leaves it on a Game Manager–side manager for the field to read
- Remember which lab the player came from (the Black Lantern Bar, then the Laughing Man Inn)
- No camera blend or pause needed any more

## Completed Info
Nov 1, 2026, code written, not tested in Unity yet:
- **`RuneFieldAccessWrapper`** (new, `Scripts/Common/Wrappers`): goes on the access point prefab. The timeline calls `LaunchAdvancedField()` or `LaunchBasicField()`. Optional `_ReturnLocator` for where the player comes back
- **Scene Manager:** `TravelToRuneField` / `LeaveRuneField`, a Rune Field foldout (scene name, fade times), and putting the player back at the access point when the scene reloads
- **Shade Manager:** Rune Field Access region holds the type and the rules (`CanEditAllSlots`, `CanSlotShards`)
- **Basic:** the field only edits the selected shade (slot buttons hidden, `SelectSlot` refuses)
- `SceneManagerWrapper.LeaveRuneField()` for the field's On Leave
- Written up in [[Scene Manager]], [[Manager Wrappers]], [[Shade Manager]] and [[Rune Field 3D View]]

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]
- Nov 1, 2026: the access profile asset was swapped for two launch functions, from the review of [[Rune field access points]]

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
