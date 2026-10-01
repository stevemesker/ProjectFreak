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
| `_CritMultiplier` | *Not used yet, the package always uses 1*                   |
| `_ImpactStrength` | Camera shake strength when it hits                          |
| `_DamageStats`    | List of `DamageEntry` the projectile carries                |

**Settings**

| Variable                  | Description                                    |
| :------------------------ | :--------------------------------------------- |
| `_projectilePrefab`       | Prefab with a `ProjectileObject`               |
| `_projectileSpawnLocator` | Where and which direction projectiles spawn    |
| `_minimumFireSpeed`       | Lowest allowed fire delay (default 0.25)       |

### Flow
1. `OnEnable` starts the `FireCountdown` coroutine
2. After `_fireSpeed` seconds, it spawns a projectile, sets its speed, and gives it a [[Damage Package]] built from the inspector values (source = the trap)
3. It starts the countdown again
4. `OnDisable` stops the timer

See [[Damage Receivers & Projectiles]] for what the projectile does on hit.
