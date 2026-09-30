[[Note To Self]]
Probably need to delete Element type and Stat name type and reconfigure any scripts using it. Wanted to put that into damage type to make damage packages a little bit easier to use

**Status (Sept 2026):**
- Done: `DamageType` now has its own `AttackType`, `StatType`, and `ElementType` enums, and [[Damage Package]]s use them
- Still to do: the old enums are still in use and can't be deleted yet
    - `StatNameType.Stat` is used by [[Shade Manager]] (`ChangeStat`) and stat boost packages
    - `ElementType.Element` is used by `WeaponItem.element` (see [[Weapon usage]])
- These should get swapped over during the planned damage overhaul (see [[Known Issues]])
