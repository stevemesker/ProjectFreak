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
- **No friendly fire:** flies through units on the shooter's team (`CombatTools.CanHitTeam`, which uses `UnitTeam.IsHostileTo`, see [[Unit Targeting#UnitTeam]]), unless the package's `_HitsAllies` is on. The shooter's team is looked up once in `LaunchProjectile`. Projectiles with no team behind them (like [[Traps]]) hit every team
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
## Shared Damage Calculation
All damage math lives in one place, `CombatTools.ResolveHit(package, defenderStats, isStaggered)`, so enemies and the player always follow the same rules. It returns a `HitResult`: the final damage of each entry, the total, and whether it crit. It follows the formula in [[Damage]]:

1. **Crit:** if the target is staggered it's always a crit, otherwise roll against the package's `_CritChance`. Rolled once per hit, on the target
2. For each entry, starting from its raw `_Damage`:
   - × `_CritMultiplier` if it crit and this is the **first (main) entry**
   - × effectiveness. *Always 1 for now, the element chart isn't decided yet (see [[Elemental Affinity]])* [[Notes for the future]]
   - × resistance from `CoreStats.GetAttackResistanceModifier`: 0 if immune, 0.5 if resisted, 1 otherwise
   - × **ATK ÷ (ATK + 2 × DEF)**, where ATK is the entry's `_AttackStat` and DEF is the target's matching defense stat (DEF for physical stats, SPR for magical, through `GetCombatStat`, so never below 1). **True damage skips this step**, but immunities and resistances above still apply. Attacks with no stat behind them (like explosions) have no matching defense and get through in full
   - **rounded up** once at the very end (`Mathf.CeilToInt`), so anything not fully blocked does at least 1, and an immune hit stays exactly 0

The defense weight (2) and the stat floor (1) are constants at the top of the Damage region in `CombatTools`. *Move them to an inspector asset if they need frequent tuning.* [[Notes for the future]]

**Popups:** `CombatTools.ShowHitPopups` shows one number per entry, with only the main entry shown as a crit. `ShowHealPopup` shows a heal (the popup draws negative numbers in the healing color). See [[Damage Popup System]].

---
## EnemyStats
Health for enemies, hooked to `EnemyDamagable`'s `onDamage` event (`TakeDamage(DamagePackage)`).
- Asks its `IStaggerable` (if it has one) whether it's staggered, runs `ResolveHit`, shows the popups, subtracts the total from `eStats._Health`, and fires `onDeath` at 0
- `Heal(amount)` adds health (capped at max HP) and shows a heal popup. It skips the damage formula. *A real healing system comes later* [[Notes for the future]]
- `GetHealthPercent()` for the AI (`IUnitHealth`)

*Enemy death and loot don't do anything yet, and the size class bonus damage (`SizeClassRulesSO`) isn't applied yet (see [[AI Movement & Dungeon Loading Plan#To Do]]).* [[Notes for the future]]

---
## PlayerDamegable
Handles damage to the player. *(Spelling matches the class name in code.)*

Hits reach it the same way they reach enemies: the player prefab has an `EnemyDamagable` (the `IDamagable` projectiles, swings and dashes look for), and its `onDamage` event calls `PlayerDamegable.TakeDamage`. That way the player gets the same aggro, knockback and stagger checks as every unit (it has none of those components, so nothing extra happens).

`TakeDamage(DamagePackage)`:
1. Runs `ResolveHit` against the player's stats (`Player.player.pData.pStats`) and shows the popups through the [[UIDamage Manager]]
2. Subtracts the total from the player's health and fires `onDeath` at 0
3. If the camera is following the player, shakes it using the package's impact strength × the current danger level's dampen value

`Heal(amount)` works like the enemy version. `Death()` is a placeholder that refills health. [[Notes for the future]]

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
## Damage Overhaul (Oct 2026)
Built from the plan decided in Oct 2026 (see [[Damage]] for the formula and [[Damage Balance]] for why):
- Defense is now ATK ÷ (ATK + 2 × DEF) instead of subtracting the defense stat (which was actually adding it)
- One shared calculation (`CombatTools.ResolveHit`) instead of separate copies in `EnemyStats` and `PlayerDamegable`
- True damage skips defense but not resistances or immunities. Immunities are checked now
- Healing is a separate `Heal` call, not negative damage. The Healing element is gone
- Damage stays a decimal until it's rounded up once per entry
- Stats used in the formula never go below 1
- Entries carry the attack stat, and weapons use a power multiplier instead of base damage
- Crits: weapon crit chance and multiplier, rolled by the target, guaranteed on staggered targets, main entry only
- Friendly fire: dashes now skip allies too, and everything respects the package's `_HitsAllies` flag
- WIS was removed from the stats, and the element list was swapped to the final one

**Still open:** explosions don't exist yet (they should use the same team check and can hit the source when `_HitsAllies` is on), the element effectiveness chart, and hazards dealing true damage as a percent of max HP [[Notes for the future]]
