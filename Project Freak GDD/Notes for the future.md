[[Note To Self]]
Probably need to delete Element type and Stat name type and reconfigure any scripts using it. Wanted to put that into damage type to make damage packages a little bit easier to use

**Status (Oct 2026):** Done
- `DamageType` has its own `AttackType`, `StatType`, and `ElementType` enums, and [[Damage Package]]s use them
- Stat boost packages ([[Shade Manager]] `ChangeStat`), `ElementItemSO.element`, and `WeaponItem.element` now use the `DamageType` enums too
- Existing element and weapon assets were shifted up by one so they keep the same stat/element (the new enums start with `None`)
- `StatNameType.cs` and `ElementType.cs` aren't used anymore and can be deleted in Unity
