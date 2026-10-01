## Overview
`ShadeManager` holds the data for each of the player's [[Shade Slot]]s, summons the [[Shade]] into the world, and handles switching control between the player and the shade ([[Summon Shade]] and [[Control Shade]]).

It lives on the Game Manager object (see [[AA - Managers]]) and is reached through its singleton:

```csharp
ShadeManager._ShadeManager
```

`GameManager.GetShadeManager()` also returns this singleton.

---
## Inspector Data

**Pointers**

| Variable                  | Description                                                                 |
| :------------------------ | :-------------------------------------------------------------------------- |
| `managerScriptableObject` | `ElementManagerSO`. The manager registers itself here in `OnEnable` so the [[Rune Field]] UI can reach it |
| `_ShadePrefab`            | Prefab spawned when summoning                                                |
| `_CurrentShade`           | The shade currently in the world                                             |

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
## Summoning — `SummonShade(Vector3 offset)`
Called by the `SummonShade` ability (see [[Ability System]]) with an offset from the player.
- If a shade already exists, it's moved to the new spot
- Otherwise `_ShadePrefab` is spawned there
- Control stays with the player

## Control Switching — `ShadeControlAbility()`
Called by the `ControlShade` ability. If a shade exists and nothing else is happening, it starts the `ShadeControlSwitch` coroutine:

```text
Fade screen out (HUD Manager)
    ↓
Wait
    ↓
Move camera to the shade (Camera Manager)
    ↓
Turn off player movement, turn on shade movement
    ↓
Fade screen back in
```

`ControlShade(bool control)` does the actual swap:
- `true` - player movement off, shade movement on
- `false` - player movement on, shade movement off

*Currently the switch always goes to the current shade. Switching back to the player isn't hooked up to an input yet.*

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
