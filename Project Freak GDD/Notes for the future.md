Hub for everything unfinished in the GDD. Any line that ends with [[Notes for the future]] is planned, temporary, a placeholder, or not built yet.

---
## How to use it
- **See everything open:** open this note and look at its **Backlinks** (right sidebar, or "Linked mentions" at the bottom of this note). Every tagged line shows up there, grouped by note.
- **Tagging something new:** end the line with `[[Notes for the future]]`.
- **When it's done:** take the tag off that line and update the line so it describes what exists now. If it's gone from the backlinks list, it's handled.
- [[Known Issues]] keeps its own bug checklist. Its unchecked items count as open too.
- [[AI Movement & Dungeon Loading Plan]] has the bigger planned work (build order, To Do, open questions).

*Design notes (narrative, shade ideas, etc.) aren't tagged, since they're ideas by nature. Tagging is for the Architecture notes, where it means "the code doesn't do this yet".*

---
## Done
### Enum cleanup *(Oct 2026)*
[[Note To Self]]
Probably need to delete Element type and Stat name type and reconfigure any scripts using it. Wanted to put that into damage type to make damage packages a little bit easier to use

**Status:** Done
- `DamageType` has its own `AttackType`, `StatType`, and `ElementType` enums, and [[Damage Package]]s use them
- Stat boost packages ([[Shade Manager]] `ChangeStat`), `ElementItemSO.element`, and `WeaponItem.element` now use the `DamageType` enums too
- Existing element and weapon assets were shifted up by one so they keep the same stat/element (the new enums start with `None`)
- `StatNameType.cs` and `ElementType.cs` aren't used anymore and can be deleted in Unity (tracked in [[Known Issues]])
