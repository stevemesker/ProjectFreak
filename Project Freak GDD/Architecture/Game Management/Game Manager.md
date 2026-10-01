## Overview
`GameManager` is the root of the persistent manager object. Every other manager is a component on the same object (see [[AA - Managers]]). The object is spawned before the first scene by the [[Runtime Bootstrapper & Runtime Asset|Runtime Bootstrapper]].

Reached with `GameManager._GameManager`.

---
## Setup
In `Awake` it:
1. Destroys itself if another Game Manager already exists
2. Sets `_GameManager`
3. Marks the object `DontDestroyOnLoad`, which keeps all the managers on it alive between scenes

---
## Data

| Variable      | Description                                          |
| :------------ | :--------------------------------------------------- |
| `shade`       | Reference to the [[Shade Manager]] on this object    |
| `PlayerLevel` | The player's current level                           |

---
## Functions

| Function                        | Description                                         |
| :------------------------------ | :-------------------------------------------------- |
| `GetShadeList()`                | Returns the [[Shade Manager]]'s shade slot list     |
| `GetShadeManager()`             | Returns the [[Shade Manager]] component             |
| `GetPlayerCurrentLevel()`       | Returns `PlayerLevel`                               |
| `SetPlayerCurrentLevel(int)`    | Sets `PlayerLevel`                                  |
| `IncrementPlayerLevel(int)`     | Adds to `PlayerLevel`                               |

*`GameManagerEventSO` exists in the same folder but is an empty template.*
