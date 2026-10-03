The damage package class is a data holder class used when anything deals damage to anything. It's a snapshot of an attack at the moment it's created (see [[Damage]] for the design reasoning). It is composed of the following variables:

| Variable                | Type                | Description                                                                 |
| :---------------------- | :------------------ | :-------------------------------------------------------------------------- |
| `_Source`               | `GameObject`        | Who made the attack. Projectiles ignore hitting their own source            |
| `_CritMultiplier`       | `float`             | Multiplier applied to every entry's damage                                  |
| `_DamageImpactStrength` | `float`             | How hard the hit shakes the camera (default 0.25). See [[Camera Manager]]   |
| `_KnockbackDistance`    | `float`             | How far the hit pushes the target back, in meters (default 0 = none). Shrunk by the target's size class. See [[Enemy Movement]] |
| `_StaggerPower`         | `float`             | 0-1, how likely the hit is to stagger (default 0 = can't). Set by melee swings. Shrunk by the target's size class, ignored by bosses. See [[Melee Weapon System#Stagger]] |
| `_Entries`              | `List<DamageEntry>` | Each separate chunk of damage in the attack                                 |

---
## Damage Entry
One attack can deal several kinds of damage at once (for example physical + fire), so each kind is its own `DamageEntry` struct:

| Variable       | Type                     | Description                                          |
| :------------- | :----------------------- | :--------------------------------------------------- |
| `_Damage`      | `int`                    | Raw damage amount                                    |
| `_atkType`     | `DamageType.AttackType`  | None, Physical, Explosion, Magical, TrueDamage       |
| `_statType`    | `DamageType.StatType`    | Which stat the attack used (decides which defense stat blocks it) |
| `_elementType` | `DamageType.ElementType` | None, Normal, Fire, Water, Ice, Electric, Earth, Poison, Dark, Light, Healing |

The enums live in the `DamageType` namespace in `DamageType.cs`.

---
## Related
- [[Damage Receivers & Projectiles]] - what happens when a package hits something
- [[Traps]] - builds packages from inspector values
- [[Unit Dash Script]] - pass-through dashes carry a package
