What a save file needs to hold, so shade slots and progress survive quitting a built game. Right now everything lives on ScriptableObject assets, which only keep their changes in the editor (see [[Known Issues]]). Written Oct 29, 2026 while planning the rune field finish ([[Rune Field Finish Plan]]); not part of that effort.

Back to [[AA - AI Info]] · [[Save Manager]] · [[Save slot]]

---
## What to save

**Save slot (`SaveSlotSO`)**
- Empty or in use, chapter, tamer level, selected shade slot
- The current scene (where to load back into)
- The forms the player has seen (`_SeenEvolutions`)

**Each shade (`ShadeSO`)**
- Current form, the gates it took (its rank), level, XP, lives
- Its slotted shard
- The rune field (`RuneFieldData`): runes (ID, element, position), bridges, plugs, active nodes, next rune ID, frozen runes, frozen bridges, locked-in runes
- The node snapshots (`_PluggedNodes`), so the rules can run without the field scene (a level-up in a dungeon)
- The last open zone the player saw (`_SeenOpenZone`), so the zone opening knows whether to play
- *Compiled effects can be worked out again on load instead of saved*

**Inventory (`InventoryManager`)**
- Runes, ingredients and shards, with counts

**Not built yet, but will need saving** [[Notes for the future]]
- Weapons and equipment, the player's abilities
- Dungeon progress: floor, map, cleared rooms, chests and pickups (the same data [[Remember the dungeon across a field visit]] keeps between scenes)
- Quest progress (where the player is in each quest tree)
- Settings (kept apart from save slots)

---
## The big design point
A save file can't hold a link to an asset (a rune type, a form, a shard tier) the way the editor does. Every saved asset needs a **stable ID**, plus a lookup that turns IDs back into assets on load. That's probably the largest part of the work [[undecided]]

---
## Cards
Every card that links this note (through `plan`, `needs` or anywhere in its text), grouped by board.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Cards
    groupBy:
      property: file.folder
      direction: ASC
    order:
      - file.name
      - status
      - owner
      - needs
```
