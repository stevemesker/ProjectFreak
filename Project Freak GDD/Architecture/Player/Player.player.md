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

*`PlayerEquipment` and `ShadeSummoner` exist but are empty templates.* [[Notes for the future]]

---
## Player Script

**Data / Pointers**

| Variable          | Description                                                                         |
| :---------------- | :---------------------------------------------------------------------------------- |
| `pData`           | The `PlayerData` component                                                          |
| `camTarget`       | Object the camera can target                                                        |
| `handPointer`     | The hand bone weapons spawn under. Swapping only removes the weapon `Player` spawned, so other children are safe |
| `weaponSelection` | Index of the equipped weapon slot currently in the hand                             |
| `_QueuedWeaponSelection` | Read only. Slot waiting to be switched to once the held weapon stops being busy. -1 = none |
| `_movement`       | The `CharacterMovement` component                                                   |
| `_InputDriver`    | The `PlayerInputDriver` that reads the controls. Auto-filled from the same object if empty |

**Equipment**

| Function                       | Description                                                                                 |
| :----------------------------- | :------------------------------------------------------------------------------------------ |
| `SelectWeapon(int)`            | Picks a slot to hold. If the held weapon is busy the pick is queued and switches the moment it's free. Picking the held slot clears the queue (see [[Ranged Weapon System#Switch Buffer]]) |
| `UpdateCurrentWeapon()`        | Makes the hand hold the selected slot: removes the old weapon, spawns the new one under `handPointer`, then calls `SetUpWeapon` (see [[Weapon usage]]). Does nothing if it's already holding that item |
| `UpdateEquippedWeaponSlotSize()` | Grows/shrinks the equipped weapon list to match `_EquipmentSize`                         |
| `GetActiveWeaponIndex()`       | Returns `weaponSelection` (the slot in the hand)                                            |
| `GetSelectedWeaponIndex()`     | The queued slot if a switch is waiting, otherwise the held one. Used for scrolling          |
| `UseCurrentWeapon()` / `ReleaseCurrentWeapon()` | Calls `TriggerAttack` / `ReleaseAttack` on the held weapon. `PlayerCombatInteract` calls these from the attack input |

**Control**

| Function                  | Description                                                                          |
| :------------------------ | :----------------------------------------------------------------------------------- |
| `EnablePlayerControl()`   | Turns the `PlayerInputDriver` back on                                                |
| `DisablePlayerControl()`  | Turns the `PlayerInputDriver` off, so the body stops and keeps floating (used when the player takes control of a shade, see [[Shade Manager]]) |
| `SetPlayerTurning(bool)`  | Turns aiming rotation on/off (turned off while the [[Radial Menu]] is open)          |
