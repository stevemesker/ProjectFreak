## Overview
Traps are dungeon hazards that damage whatever they hit. They fall under [[Destructible Objects]] on the design side.

Currently there is one trap script, `TrapProjectileSpawner`.

---
## TrapProjectileSpawner
Fires a projectile on a timer for as long as it's enabled.

### Inspector Data

**Damage Data**

| Variable          | Description                                                 |
| :---------------- | :---------------------------------------------------------- |
| `_fireSpeed`      | Seconds between shots (clamped to `_minimumFireSpeed`)      |
| `_projectileSpeed` | How fast the projectile travels                            |
| `_FauxAttackStat` | Stand-in attack stat (default 10), since preset traps have no stats. Copied onto every entry so the target's defense has something to compare against |
| `_CritChance`     | Chance for each projectile to crit (default 0) |
| `_CritMultiplier` | Damage multiplier on a crit. Only the main entry is multiplied |
| `_ImpactStrength` | Camera shake strength when it hits                          |
| `_DamageStats`    | List of `DamageEntry` the projectile carries. `_Damage` is the raw damage before the target's defense. `_AttackStat` is filled in from `_FauxAttackStat` automatically. Each projectile gets its own copy of the list |

**Settings**

| Variable                  | Description                                    |
| :------------------------ | :--------------------------------------------- |
| `_projectilePrefab`       | Prefab with a `ProjectileObject`               |
| `_projectileSpawnLocator` | Where and which direction projectiles spawn    |
| `_minimumFireSpeed`       | Lowest allowed fire delay (default 0.25)       |

### Flow
1. `OnEnable` starts the `FireCountdown` coroutine
2. Every `_fireSpeed` seconds, it spawns a projectile, sets its speed, and gives it a [[Damage Package]] built from the inspector values (source = the trap, `_HitsAllies` on, since preset traps hit everyone)
3. `OnDisable` stops the timer

Missing prefab or locator references log an error and the trap doesn't start. *Traps placed by a player or unit should snapshot the caster's stats instead of the faux stat (see [[Damage Balance]]).* [[Notes for the future]]

See [[Damage Receivers & Projectiles]] for what the projectile does on hit.
