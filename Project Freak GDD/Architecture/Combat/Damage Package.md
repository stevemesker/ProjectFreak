The damage package class is a data holder class used when anything deals damage to anything. It's a snapshot of an attack at the moment it's created (see [[Damage]] for the design reasoning). It is composed of the following variables:

| Variable                | Type                | Description                                                                 |
| :---------------------- | :------------------ | :-------------------------------------------------------------------------- |
| `_Source`               | `GameObject`        | Who made the attack. Projectiles and swings never hit their own source      |
| `_CritChance`           | `float`             | 0-1 chance to crit (from the weapon or ability). The **target** rolls it when the hit lands, so every projectile in a volley rolls separately. Staggered targets always take a crit |
| `_CritMultiplier`       | `float`             | Damage multiplier on a crit (default 1.5). Only applies to the **first (main) entry**. Never treated as less than 1 |
| `_DamageImpactStrength` | `float`             | How hard the hit shakes the camera (default 0.25). See [[Camera Manager]]   |
| `_KnockbackDistance`    | `float`             | How far the hit pushes the target back, in meters (default 0 = none). Shrunk by the target's size class. See [[Enemy Movement]] |
| `_StaggerPower`         | `float`             | 0-1, how likely the hit is to stagger (default 0 = can't). Set by melee swings. Shrunk by the target's size class, ignored by bosses. See [[Melee Weapon System#Stagger]] |
| `_HitsAllies`           | `bool`              | Friendly fire (default off). Projectiles, hit scan, melee and dashes skip the attacker's allies unless it's on. Traps turn it on, since they hit everyone |
| `_Entries`              | `List<DamageEntry>` | Each separate chunk of damage in the attack. **The first entry is the main one** |

Weapons fill the crit numbers and friendly fire flag from their `WeaponItem` (see [[Ranged Weapon System#WeaponItem]]). *Runes adding crit chance or crit multiplier come with the rune effects.* [[Notes for the future]]

---
## Damage Entry
One attack can deal several kinds of damage at once (for example physical + fire), so each kind is its own `DamageEntry` struct:

| Variable       | Type                     | Description                                          |
| :------------- | :----------------------- | :--------------------------------------------------- |
| `_Damage`      | `float`                  | Raw damage: attacker's stat × weapon power × multipliers. Kept as a decimal, the target rounds up once at the end |
| `_AttackStat`  | `float`                  | The attacker's attack stat when the package was built. The target compares its defense against it. Traps fill it with a faux stat |
| `_atkType`     | `DamageType.AttackType`  | None, Physical, Explosion, Magical, TrueDamage       |
| `_statType`    | `DamageType.StatType`    | Which stat the attack used (decides which defense stat blocks it) |
| `_elementType` | `DamageType.ElementType` | Normal, Fire, Water, Air, Earth, Ice, Lava, Lightning, Plant, Void, Light (see [[Elemental Affinity]]). Normal is the default |

The enums live in the `DamageType` namespace in `DamageType.cs`. **Every enum value has an explicit number** (`Fire = 1`) because Unity saves enums on assets as numbers. Removing or adding a value never shifts saved assets, as long as new values get the next unused number and removed numbers aren't reused (StatType 7 was Wisdom).

**Still planned:**
- **Ability tags:** room for a small tag list (Hack, Pierce...) that [[Destructible Objects]] can require. Later [[Notes for the future]]

---
## Related
- [[Damage Receivers & Projectiles]] - what happens when a package hits something
- [[Traps]] - builds packages from inspector values
- [[Unit Dash Script]] - pass-through dashes carry a package
