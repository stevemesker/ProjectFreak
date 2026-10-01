## Overview
Once a [[Damage Package]] is built, it's delivered by a projectile (or a dash) and processed by whatever it hits. This follows the design in [[Damage]]: projectiles only carry the package, receivers do the math.

---
## ProjectileObject
A simple projectile.

| Variable  | Description                          |
| :-------- | :----------------------------------- |
| `_Speed`  | Units per second, moves forward      |
| `_Damage` | The [[Damage Package]] it carries    |

On trigger enter:
- Ignores its own source and other triggers
- If the object has `IDamagable`, calls `TakeDamage(_Damage)`
- Destroys itself either way

Used by `WeaponAttackRanged` (see [[Weapon usage]]) and [[Traps]].

---
## EnemyDamagable
Implements `IDamagable` for enemies. It doesn't calculate damage itself. Instead it fires events so each enemy can decide what happens:

| Event      | Description                                                                  |
| :--------- | :--------------------------------------------------------------------------- |
| `onHit`    | Fires on any hit. For effects that don't need the damage data                |
| `onDamage` | Fires with the `DamagePackage`. Functions hooked here must take a `DamagePackage` as their first parameter |

---
## PlayerDamegable
Handles damage to the player. *(Spelling matches the class name in code.)*

`TakeDamage(DamagePackage)`:
1. For each damage entry, calculates damage with `DamageCalculation()` × crit, and shows a popup through the [[UIDamage Manager]]
2. Subtracts the total from the player's health
3. Fires `onDeath` if health hits 0, clamps health to max HP
4. If the camera is following the player, shakes it using the package's impact strength × the current danger level's dampen value

`Death()` is a placeholder that refills health.

*Known issues:* `PlayerDamegable` doesn't implement `IDamagable`, so projectiles can't damage the player yet, and the defense math adds defense instead of subtracting it. Both are planned for the damage overhaul. See [[Known Issues]].

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
- `EnvironmentDamageable` exists but is an empty template. Planned for [[Destructible Objects]]
- Pass-through dashes deliver damage packages too. See [[Unit Dash Script]]
