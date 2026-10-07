## Overview
`ShadeManager` holds the data for each of the player's [[Shade Slot]]s, brings the [[Shade]] out in its forms ([[Tether Shade]], [[Release Shade]], [[Return Shade]]), and handles switching control between the player and the released shade ([[Control Shade]]).

It lives on the Game Manager object (see [[AA - Managers]]) and is reached through its singleton:

```csharp
ShadeManager._ShadeManager
```

`GameManager.GetShadeManager()` also returns this singleton.

Only one shade is ever out, in one form. See [[Shade Forms Plan]], [[Tethered Shade]] and [[Shade (Runtime)]].

---
## Inspector Data

**Pointers**

| Variable                  | Description                                                                 |
| :------------------------ | :-------------------------------------------------------------------------- |
| `managerScriptableObject` | `ElementManagerSO`. The manager registers itself here in `OnEnable` so the [[Rune Field]] UI can reach it |
| `_TetheredPrefab`         | `PFB_Shade_Tethered`, spawned by Tether Shade                                |
| `_ReleasedPrefab`         | `PFB_Shade_Released`, spawned by Release Shade. Was `_ShadePrefab` (the assigned prefab carried over) |

**Runtime Data** (read only)

| Variable              | Description                                                         |
| :-------------------- | :------------------------------------------------------------------ |
| `_TetheredShade`      | The tethered shade currently out                                     |
| `_ReleasedShade`      | The released shade currently out. Never filled at the same time as `_TetheredShade` |
| `_IsControllingShade` | True while the player is driving the released shade                  |

**Shade Slots**

| Variable               | Description                                                     |
| :--------------------- | :-------------------------------------------------------------- |
| `_ShadeSlots`          | Every possible shade slot (`ShadeSO`, see [[Shade (Runtime)]])  |
| `tamerSlotLevel`       | How many slots the player has access to                         |
| `currentShadeSelected` | Index of the active slot                                        |

**Control Switching**

| Variable         | Description                                                    |
| :--------------- | :------------------------------------------------------------- |
| `_switchTimeIn`  | Fade out time when switching control (default 0.5)             |
| `_switchTimeOut` | Fade in time when switching control (default 0.3)              |
| `isBusy`         | True while a switch is happening. Blocks summoning/switching   |

---
## Summoning
Every function returns early while a control switch is running (`isBusy`).

| Shade out now | `TetherShade()` | `ReleaseShade(offset)` | `ReturnShade()` |
| :--- | :--- | :--- | :--- |
| None | Tethered appears | Released appears at player + offset | Nothing |
| Tethered | Tethered goes away | Tethered goes away, released appears | Tethered goes away |
| Released | Released goes away, tethered appears | Released goes away | Released goes away |

| Function | Description |
| :--- | :--- |
| `TetherShade()` | Called by the Tether Shade ability |
| `ReleaseShade(offset)` | Called by the Release Shade ability with a spot it found next to the player. Control stays with the player, the shade runs on its AI |
| `ReturnShade()` | Called by the Return Shade ability. Puts away whichever form is out |
| `ReturnReleased()` | Puts away only the released shade. Called by `DungeonManager.EnterDungeon`, since released shades don't come into dungeons (a tethered shade does, it persists between scenes) |
| `CanSummon()` | The one place that decides if the current slot's shade can come out in any form. Checks the player exists and the selected slot is valid. Health and [[LIFE]] plug in here later [[Notes for the future]] |
| `GetCurrentForm()` | Returns `ShadeFormType.Form` (None, Tethered, Released) |
| `OnReleasedShadeGone(shade)` | Called by `ReleasedShade` when it's destroyed, so a scene change that removes it is handled too |

Spawning: the prefab is instantiated, then `Setup(slot, player)` and `Appear()` are called through `IShadeForm`. A prefab without the right component on its root logs an error and is destroyed.

**Returning a shade the player is driving:** the player is put straight back in their own body (no fade), the camera goes back to the player, and any control switch that was mid-fade is stopped. Same when a scene change removes the released shade.

## Control Switching — `ShadeControlAbility()`
Called by the `ControlShade` ability. It's a toggle: while the player is driving the shade, it switches back to the player; otherwise it takes over the **released** shade (does nothing if none is out). Either way it starts the `ShadeControlSwitch` coroutine:

```text
Fade screen out (HUD Manager)
    ↓
Wait
    ↓
Move camera to the shade, or back to the player (Camera Manager)
    ↓
Turn off player movement, turn on shade movement
    ↓
Fade screen back in
```

`ControlShade(bool control)` does the actual swap:
- `true` - player movement off, shade movement on
- `false` - player movement on, shade movement off

*While controlling, the radial menu still shows the player's abilities. It should show the shade's abilities instead (system for later).* [[Notes for the future]]

---
## Stat Boosts
[[Rune Field]] runes change the active shade's stats through stat boost packages.

| Function                          | Description                                                           |
| :-------------------------------- | :-------------------------------------------------------------------- |
| `ReceiveStatBoostPackage(list)`   | Adds every boost in the list                                           |
| `RemoveStatBoostPackage(list)`    | Removes every boost in the list                                        |
| `ChangeStat(package, multiplier)` | Adds `amount × multiplier` to the matching stat on `_AlteredStats`. Uses `DamageType.StatType`; `None` falls into the warning default |

---
## Other Functions

| Function                                  | Description                                          |
| :---------------------------------------- | :--------------------------------------------------- |
| `GetCurrentShade()`                       | Returns the active `ShadeSO`                         |
| `GetShadeOfIndex(int)`                    | Returns the `ShadeSO` in that slot                   |
| `SaveCurrentShadeRuneFieldPackage(pkg)`   | Stores the [[Rune Field]] layout on the active slot  |
| `SetShadeSelection(int)` / `GetShadeSelectionIndex()` | Set/get the active slot              |
