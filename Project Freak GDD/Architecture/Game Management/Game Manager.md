## Overview
`GameManager` is the root of the persistent manager object. Every other manager is a component on the same object (see [[AA - Managers]]). The object is spawned before the first scene by the [[Runtime Bootstrapper & Runtime Asset|Runtime Bootstrapper]].

Reached with `GameManager._GameManager`.

---
## Setup
In `Awake` it:
1. Destroys itself if another Game Manager already exists
2. Sets `_GameManager`
3. Marks the object `DontDestroyOnLoad`, which keeps all the managers on it alive between scenes

The prefab also has the game's only **EventSystem** as a child (with `InputSystemUIInputModule`), so UI clicks and drags work in every scene, including runtime-spawned UI like the dungeon map. Don't add EventSystems to scenes; a second one causes "multiple EventSystems" warnings.

---
## Data

| Variable      | Description                                          |
| :------------ | :--------------------------------------------------- |
| `shade`       | Reference to the [[Shade Manager]] on this object    |
| `PlayerLevel` | The player's current level                           |
| `_SizeClassRules` | Shared `SizeClassRulesSO` for knockback by size class. See [[Enemy Movement]] |

---
## Functions

| Function                        | Description                                         |
| :------------------------------ | :-------------------------------------------------- |
| `GetShadeList()`                | Returns the [[Shade Manager]]'s shade slot list     |
| `GetShadeManager()`             | Returns the [[Shade Manager]] component             |
| `GetPlayerCurrentLevel()`       | Returns `PlayerLevel`                               |
| `SetPlayerCurrentLevel(int)`    | Sets `PlayerLevel`                                  |
| `IncrementPlayerLevel(int)`     | Adds to `PlayerLevel`                               |
| `GetSizeClassRules()`           | Returns `_SizeClassRules` (logs an error if it isn't assigned) |

*`GameManagerEventSO` exists in the same folder but is an empty template.* [[Notes for the future]]
