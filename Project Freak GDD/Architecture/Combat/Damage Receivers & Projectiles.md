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
- `EnvironmentDamageable` exists but is an empty template. Planned for [[Destructible Objects]]: no health, just a tier and a material, checked against the package's entry types/elements, stagger power and (later) ability tags [[Notes for the future]]
- Pass-through dashes deliver damage packages too. See [[Unit Dash Script]]

---
## Planned: Damage Overhaul
The damage math is getting rebuilt. The formula was decided in Oct 2026, see [[Damage]] for it and [[Damage Balance]] for why. Not built yet. [[Notes for the future]]
- **Mitigation formula:** replace "subtract the defense stat" with: damage that gets through = ATK ÷ (ATK + 2 × DEF), where ATK is the attacker's stat carried on the damage entry and DEF is the receiver's matching defense (DEF or SPR). K is the attacker's stat, not a level, because level never drives stats in this game [[Notes for the future]]
- **One shared calculation:** `EnemyStats.DamageCalculation` and `PlayerDamegable.DamageCalculation` are separate copies today. Both should call one shared function (likely a static in `CombatTools` or `CoreStats`) [[Notes for the future]]
- **True damage:** skips defense but still checks element resistance and immunity. Hazards will use true damage as a percent of max HP [[Notes for the future]]
- **Healing:** stop treating healing as negative damage (the Healing element is being removed). For now a separate small heal call that skips the damage formula is enough. The popup already shows negative numbers as heals. A real healing system comes later [[Notes for the future]]
- **Stat floor:** stats used in the formula never go below 1, so a stat pushed down by negative runes can't make damage 0 or negative [[Notes for the future]]
- **Where it changes:** the receivers, plus the attackers in a small way: entries need to carry the attack stat (see [[Damage Package]]), and weapons switch from base damage to a power multiplier (see [[Ranged Weapon System#Damage and Projectiles]], [[Melee Weapon System#Damage]])
- **Crits:** all crit logic lives in the shared damage calculation. On each hit: crit if the unit is staggered (ask its `IStaggerable`), otherwise roll against the package's `_CritChance`. A crit multiplies the first (main) entry only, by `_CritMultiplier`. The popup's `isCrit` comes from this result [[Notes for the future]]
- **Friendly fire:** dashes and explosions need the same `UnitTeam.IsHostileTo` check projectiles and hit scan already have, skipped only when the package's `_HitsAllies` flag is set [[Notes for the future]]
- **Folds in:** the damage bugs in [[Known Issues]] (defense adding damage, `PlayerDamegable` not implementing `IDamagable`, immunities not checked, dashes hitting allies) and crits, which are always ×1 for now [[Notes for the future]]
