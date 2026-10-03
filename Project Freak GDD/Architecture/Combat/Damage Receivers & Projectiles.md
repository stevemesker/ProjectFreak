## Overview
Once a [[Damage Package]] is built, it's delivered by a projectile (or a dash) and processed by whatever it hits. This follows the design in [[Damage]]: projectiles only carry the package, receivers do the math.

---
## ProjectileObject
A simple projectile.

| Variable           | Description                          |
| :----------------- | :----------------------------------- |
| `_DefaultLifeTime` | Seconds before it removes itself if whatever spawned it didn't give it a lifetime (default 10, used by [[Traps]]) |
| `_Speed`           | Meters per second, moves forward     |
| `_Damage`          | The [[Damage Package]] it carries    |
| `_Launch`          | The `LaunchPackage` from the weapon (charge %, finisher, source). Not used by this simple projectile yet [[Notes for the future]] |

`LaunchProjectile(damage, launch, speed, lifeTime)` is how weapons send it off: it fills the fields above and destroys the projectile after `lifeTime` seconds if it hasn't hit anything. Traps still set `_Speed` and `_Damage` directly and get the default lifetime. See [[Ranged Weapon System#Damage and Projectiles]].

On trigger enter:
- Ignores other triggers and its own source (including the source's child colliders)
- **No friendly fire:** flies through units on the shooter's team (`UnitTeam.IsHostileTo`, see [[Unit Targeting#UnitTeam]]). The shooter's team is looked up once in `LaunchProjectile`. Projectiles with no team behind them (like [[Traps]]) hit every team
- If the object or one of its parents has `IDamagable`, calls `TakeDamage(_Damage)`
- Destroys itself either way (also if it never got a package)

Used by `WeaponAttackRanged` (see [[Weapon usage]]) and [[Traps]]. Projectile behaviors (arcs, homing, growing with charge) get their own note later. [[Notes for the future]]

---
## EnemyDamagable
Implements `IDamagable` for enemies. It doesn't calculate damage itself. Instead it fires events so each enemy can decide what happens:

| Event      | Description                                                                  |
| :--------- | :--------------------------------------------------------------------------- |
| `onHit`    | Fires on any hit. For effects that don't need the damage data                |
| `onDamage` | Fires with the `DamagePackage`. Functions hooked here must take a `DamagePackage` as their first parameter |

After the events, it checks the object for three optional components (no inspector hookup needed):
- `IAggroReceiver` (like `UnitTargeting`): calls `AddAggro(package)` so the unit's AI turns on whoever hit it. See [[Unit Targeting]]
- `IKnockbackable` (like `EnemyMovement`): calls `TakeKnockback(package)`. See [[Enemy Movement]]
- `IStaggerable` (like `EnemyStagger`): calls `TakeStagger(package)` on every hit, last. See [[Melee Weapon System#Stagger]]

*Note:* the player prefab also uses `EnemyDamagable` (forwarding to `PlayerDamegable`), so these checks run on the player too. The player has neither component, so nothing changes for it.

---
## PlayerDamegable
Handles damage to the player. *(Spelling matches the class name in code.)*

`TakeDamage(DamagePackage)`:
1. For each damage entry, calculates damage with `DamageCalculation()` × crit, and shows a popup through the [[UIDamage Manager]]
2. Subtracts the total from the player's health
3. Fires `onDeath` if health hits 0, clamps health to max HP
4. If the camera is following the player, shakes it using the package's impact strength × the current danger level's dampen value

`Death()` is a placeholder that refills health. [[Notes for the future]]

*Known issues:* `PlayerDamegable` doesn't implement `IDamagable`, so projectiles can't damage the player yet, and the defense math adds defense instead of subtracting it. Both are planned for the damage overhaul. See [[Known Issues]]. [[Notes for the future]]

---
## DangerLevel
Controls how much camera shake is dampened based on how hurt a unit is. Stored in `PartyStats._Danger` (see [[Stats & Inventory Data]]) and reached through `IUnitData.GetDangerLevelSettings()`.

`_DangerLevels` is a list of `DangerSetting`:

| Variable                | Description                                                              |
| :---------------------- | :----------------------------------------------------------------------- |
| `_Type`                 | `DangerType`: None, Small, Medium, Large                                 |
| `_Percentage`           | Health % threshold for this level                                        |
| `_ShakeDampenMultiplier` | Multiplier applied to camera shake at this level                        |

`GetCurrentDangerType(health, maxHealth)` returns the first level whose percentage the unit's health is above. The list must be ordered as described in its tooltip.

---
## Other
- `EnvironmentDamageable` exists but is an empty template. Planned for [[Destructible Objects]] [[Notes for the future]]
- Pass-through dashes deliver damage packages too. See [[Unit Dash Script]]

---
## Planned: Damage Overhaul
The defense math is getting rebuilt. Nothing here is decided yet beyond the direction. [[Notes for the future]]
- **Mitigation formula:** replace the current "subtract the defense stat" step with Blizzard's k/(k+x) style: damage reduction = defense / (defense + K). Defense always helps, never reaches 100%, and each extra point helps a little less than the last [[Notes for the future]]
- **K and leveling:** K usually grows with the attacker's level, so the same defense protects less against higher level enemies. How K scales with level, and how level ups and stat boosts (shades, runes, gear) feed into defense, needs working out before the formula goes in. Plotting a few K curves across the planned level range would help pick one [[Notes for the future]]
- **Where it changes:** only the receivers (`EnemyStats.DamageCalculation`, `PlayerDamegable.DamageCalculation`). Weapons, abilities, traps and dashes only build [[Damage Package]]s, so they shouldn't need changes
- **Folds in:** the damage bugs in [[Known Issues]] (defense adding damage, `PlayerDamegable` not implementing `IDamagable`, immunities not checked) and crits, which are always ×1 for now [[Notes for the future]]
