All hand held weapons utilize the `ITriggerable` interface in `CombatInterface.cs`. They have the following functions:

| Function                                          | Description                                                                                                  |
| :------------------------------------------------ | :----------------------------------------------------------------------------------------------------------- |
| `SetUpWeapon(ItemSO item, GameObject wielder, CoreStats stats)` | Initializes the held weapon so it knows its item data, its stats, and who the user is so self attacking won't happen |
| `TriggerAttack()`                                 | The trigger button is pressed, begin activation of the weapon                                                 |
| `ReleaseAttack()`                                 | The trigger button is released, do any final effects and reset the weapon                                    |
| `IsRange()`                                       | Returns true for ranged weapons. Used to pick the right attack stat (see [[Stats & Inventory Data]])          |
| `IsBusy()`                                        | *Planned.* True while the weapon is waiting out an attack, so it can't be switched. See [[Ranged Weapon System#Weapon Switching]] [[Notes for the future]] |

*An `updateStats` function for adding bonuses after equipping was planned but isn't part of the interface yet.* [[Notes for the future]]

---
## Equipping
Weapons are spawned by `Player.UpdateCurrentWeapon()` under the player's hand bone (`handPointer`), then `SetUpWeapon` is called with the equipped `WeaponItem` and the player's stats. See [[Player.player]].

---
## Current Weapon Scripts
**`WeaponAttackRanged`** - the ranged weapon. A rewrite is planned, see [[Ranged Weapon System]] [[Notes for the future]]. Currently it:
- Reads its settings from a `WeaponRangedItem` (fire rate, warm up, charge, automatic, etc.)
- Builds a [[Damage Package]] when firing and hands it to each spawned `ProjectileObject`
- Supports single shot, multishot spread, charged shots, and automatic fire through coroutines

**`WeaponAttackMelee`** - the melee weapon. Planned, see [[Melee Weapon System]] [[Notes for the future]]

**`WeaponItem`** is the base weapon data (`ItemSO`): attack type, prefab, fire rate, warm up, knockback, camera shake, automatic/charged settings, base damage, and element.

`WeaponItem.element` uses `DamageType.ElementType`.
