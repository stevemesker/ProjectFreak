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
| `_elementType` | `DamageType.ElementType` | None, Normal, Fire, Water, Ice, Electric, Earth, Poison, Dark, Light, Healing. *Stand-in list, getting swapped to the final elements (see [[Elemental Affinity]], tracked in [[Known Issues]])* [[Notes for the future]] |

The enums live in the `DamageType` namespace in `DamageType.cs`.

**Planned for the damage overhaul:** a `_AttackStat` value on each entry, the attacker's stat at the moment the package was built. The receiver needs it for the defense formula (see [[Damage]]), and it can't be looked up later because the attacker may have changed or died. Traps and other attackers without stats fill it from the inspector (a faux stat). `_Damage` then holds the raw damage (attack stat × weapon power × multipliers). [[Notes for the future]]

**Also planned** (see [[Damage]] for the design):
- **Crits:** add `_CritChance` next to `_CritMultiplier` (weapon/ability + rune bonuses, multiplier default 1.5). The package only carries the numbers; **the receiver rolls on each hit** (see [[Damage Receivers & Projectiles]]), so every projectile in a volley rolls separately. The crit only applies to **the first entry** (the main entry), not every entry like `_CritMultiplier` does today [[Notes for the future]]
- **Friendly fire:** a `_HitsAllies` flag (default false). Everything that checks teams (projectiles, hit scan, melee, dashes, explosions) skips allies unless it's set. When set, it can also hit the source itself [[Notes for the future]]
- **Ability tags:** room for a small tag list (Hack, Pierce...) that [[Destructible Objects]] can require. Later [[Notes for the future]]

---
## Related
- [[Damage Receivers & Projectiles]] - what happens when a package hits something
- [[Traps]] - builds packages from inspector values
- [[Unit Dash Script]] - pass-through dashes carry a package
