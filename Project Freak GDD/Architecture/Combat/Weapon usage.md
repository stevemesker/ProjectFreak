All hand held weapons utilize the `ITriggerable` interface in `CombatInterface.cs`. They have the following functions:

| Function                                          | Description                                                                                                  |
| :------------------------------------------------ | :----------------------------------------------------------------------------------------------------------- |
| `SetUpWeapon(ItemSO item, GameObject wielder, CoreStats stats)` | Initializes the held weapon so it knows its item data, its stats, and who the user is so self attacking won't happen |
| `TriggerAttack()`                                 | The trigger button is pressed, begin activation of the weapon                                                 |
| `ReleaseAttack()`                                 | The trigger button is released, do any final effects and reset the weapon                                    |
| `IsRange()`                                       | Returns true for ranged weapons. Used to pick the right attack stat (see [[Stats & Inventory Data]])          |
| `IsBusy()`                                        | True while the weapon is waiting out an attack, so it can't be switched. See [[Ranged Weapon System#Weapon Switching]] |

*An `updateStats` function for adding bonuses after equipping was planned but isn't needed for now: weapons keep a reference to the wielder's stats and read them when an attack is built.*

---
## Equipping
The player picks a slot with `Player.SelectWeapon(index)`, which waits if the held weapon is busy (see [[Ranged Weapon System#Switch Buffer]]). `Player.UpdateCurrentWeapon()` then spawns the slot's `WeaponItem._WeaponPrefab` under the player's hand bone (`handPointer`) and calls `SetUpWeapon` with the item and the player's stats. See [[Player.player]].

Attack input goes `PlayerCombatInteract` → `Player.UseCurrentWeapon()` / `ReleaseCurrentWeapon()` → the held weapon's `TriggerAttack()` / `ReleaseAttack()`.

---
## Current Weapon Scripts
**`WeaponItem`** is the abstract base weapon data (`ItemSO`): attack type, prefab, attack speed, activation shake, power, crit chance and multiplier, friendly fire, element and knockback. See [[Ranged Weapon System#WeaponItem]].

**`WeaponRangedItem`** + **`WeaponAttackRanged`** - the ranged weapon data and the script on the held weapon that fires it: warm up, automatic, burst, charge, finishers, projectile patterns and hit scan. See [[Ranged Weapon System]]

**`MeleeWeaponItem`** + **`WeaponAttackMelee`** - the melee weapon data and the script on the held weapon that swings it: combos built from shared `SwingShapeSO`s, a sweeping hit check, damage and knockback. See [[Melee Weapon System]]. Stagger and animations aren't hooked up yet [[Notes for the future]]

**`CombatTools`** - static helpers both weapon scripts share: `IsPartOf`, `IsAlly`, `CanHitTeam` (the friendly fire rule), `IsLevelGeometry`, `GetBodyCollider`, `BuildWeaponDamagePackage`, `ResolveHit` (the shared damage calculation, see [[Damage Receivers & Projectiles#Shared Damage Calculation]]), the popup helpers and `ShakeCameraForWielder`. New attack code (abilities, enemy attacks) should use these so the rules for allies, walls and damage stay in one place.

`WeaponItem._Element` uses `DamageType.ElementType`.
