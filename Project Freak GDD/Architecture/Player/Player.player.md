The player script is the runtime root/accessor for the [[Hazen]] character. It is responsible for being the beacon for the player object as well as the accessor for player data and systems.

It's reached anywhere with the static `Player.player`. In `Awake` it destroys duplicates, marks itself `DontDestroyOnLoad`, sets up the equipped weapon, and tells the [[Camera Manager]] to follow it.

---
## Components on the Player

| Script                 | Description                                                                                              |
| :--------------------- | :------------------------------------------------------------------------------------------------------- |
| `Player`               | Root accessor. Weapon equipping/use and turning player control on/off                                    |
| `PlayerData`           | Stats, inventory, and ability lists. Implements `IUnitData` and `IInventory` (see [[Stats & Inventory Data]]) |
| `CharacterMovement`    | Movement, facing, and standing (see [[Player Movement]])                                                 |
| `UnitDash`             | Dashing (see [[Unit Dash Script]])                                                                       |
| `PlayerCombatInteract` | Combat input: attacking with the held weapon                                                             |
| `PlayerPickup`         | Picks up items in range (see [[Items & Pickups]])                                                        |
| `ActivateObject`       | Interact input: finds the closest `IInteractable` (see [[Interaction Object]])                          |
| `PlayerMenuInputs`     | Opens/closes the [[Radial Menu]]                                                                         |
| `AbilityInterpreter`   | Runs abilities (see [[Ability System]])                                                                  |
| `PlayerDamegable`      | Takes damage (see [[Damage Receivers & Projectiles]])                                                    |

*`PlayerEquipment` and `ShadeSummoner` exist but are empty templates.*

---
## Player Script

**Data / Pointers**

| Variable          | Description                                                                         |
| :---------------- | :---------------------------------------------------------------------------------- |
| `pData`           | The `PlayerData` component                                                          |
| `camTarget`       | Object the camera can target                                                        |
| `handPointer`     | The hand bone weapons spawn under. Must be the lowest child, since swapping deletes its child |
| `weaponSelection` | Index of the currently selected equipped weapon                                     |
| `_movement`       | The `CharacterMovement` component                                                   |

**Equipment**

| Function                       | Description                                                                                 |
| :----------------------------- | :------------------------------------------------------------------------------------------ |
| `updateCurrentWeapon()`        | Destroys the held weapon and spawns the selected one under `handPointer`, then calls `SetUpWeapon` (see [[Weapon usage]]) |
| `UpdateEquippedWeaponSlotSize()` | Grows/shrinks the equipped weapon list to match `_EquipmentSize`                         |
| `getActiveWeaponIndex()`       | Returns `weaponSelection`                                                                   |
| `UseCurrentWeapon()` / `releaseCurrentWeapon()` | Calls `TriggerAttack` / `ReleaseAttack` on the held weapon                 |

**Control**

| Function                  | Description                                                                          |
| :------------------------ | :----------------------------------------------------------------------------------- |
| `EnablePlayerControl()`   | Turns movement back on                                                               |
| `DisablePlayerControl()`  | Turns movement off (used when the player takes control of a shade, see [[Shade Manager]]) |
| `SetPlayerTurning(bool)`  | Turns aiming rotation on/off (turned off while the [[Radial Menu]] is open)          |
