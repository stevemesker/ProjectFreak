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
| `managerScriptableObject` | `ElementManagerSO`. The manager registers itself here in `OnEnable`. Part of the old rune stat path, not used anymore (see [[Known Issues]]) |
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

**Rune Fields**

| Variable | Description |
| :--- | :--- |
| `_RuneFieldLayout` | Shared `RuneFieldLayoutSO` (core settings + ability nodes). Optional for now: if empty, the rune field UI builds one from its scene and hands it over |
| `_SlotRuneFields` | Each slot's saved `RuneFieldData` (read only). Copied from each `ShadeSO`'s `_StartingRuneField` in `Awake`, so playing never changes the assets |
| `_SlotStats` | Each slot's stats with its saved runes applied (read only) |

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
## Rune Fields and Stats
*Rebuilt Oct 2026.* Each slot's [[Rune Field]] and stats live here at runtime (see [[Rune Field System]]).

Stats are **worked out from scratch**, never added or removed bit by bit: the slot's base `_shadeStats`, copied, plus the stat totals from every powered rune in its *saved* field. So unsaved changes in the rune field UI never touch a shade's stats, and they can't drift out of sync. A negative rune can't push a stat below 1 (unless the base was already below 1).

| Function | Description |
| :--- | :--- |
| `GetSavedRuneField(slot)` | A copy of a slot's saved field for the UI to edit |
| `SaveRuneField(slot, field)` | Stores a copy and works the slot's stats out again |
| `GetCorePower(slot)` | The slot's core power: its `_LVL`. Core fragments get added here later [[Notes for the future]] |
| `GetSlotStats(slot)` / `GetCurrentShadeStats()` | A slot's stats with runes applied |
| `GetRuneFieldLayout()` / `SetRuneFieldLayout(layout)` | The shared layout. Setting it works every slot's stats out again |
| `IsValidSlot(slot)` | Checks a slot index points at a real slot |

*Not in yet:* Hazen's share of rune stats, and saving fields between play sessions [[Notes for the future]]. Nothing reads the slot stats in combat yet, since the released shade has no stats component (see [[Shade (Runtime)]]).

The old stat path (`ReceiveStatBoostPackage`, `RemoveStatBoostPackage`, `ChangeStat` on `_AlteredStats`) is still in the script but nothing calls it anymore (see [[Known Issues]], Cleanup).

---
## Other Functions

| Function                                  | Description                                          |
| :---------------------------------------- | :--------------------------------------------------- |
| `GetCurrentShade()`                       | Returns the active `ShadeSO`                         |
| `GetShadeOfIndex(int)`                    | Returns the `ShadeSO` in that slot                   |
| `SetShadeSelection(int)` / `GetShadeSelectionIndex()` | Set/get the active slot              |
