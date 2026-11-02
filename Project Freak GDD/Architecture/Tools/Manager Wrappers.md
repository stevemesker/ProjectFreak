## Overview
Wrappers are small components that sit on scene objects and forward calls to a manager on the Game Manager object (see [[AA - Managers]]).

**Why they exist:** scene objects can't drag a reference to the Game Manager in the inspector, because it's spawned at runtime by the [[Runtime Bootstrapper & Runtime Asset|Runtime Bootstrapper]]. A wrapper sits in the scene, so UnityEvents (buttons, [[Interaction Object]]s, [[Timeline Runner System|timeline]] events) can call its functions directly.

Wrappers should check that the manager exists before calling it, so scenes can be tested on their own.

---
## DungeonManagerWrapper
Calls the [[Dungeon Manager]]. Every function checks the manager exists first and logs an error naming the function if it doesn't.

| Function                  | Calls                          |
| :------------------------ | :----------------------------- |
| `EnterDungeon(id)`        | `EnterDungeon`                 |
| `EndDungeon(returnMap)`   | `CompleteDungeon`              |
| `MoveToDungeonRoom(id)`   | `MoveToFloor`                  |
| `GetCurrentMapNode()`     | The current [[Dungeon Map Node]] (null if no manager) |
| `GetCurrentFloorID()`     | Current room ID (0 if no manager) |
| `GetMapNodeByID(id)`      | `GetMapNode`                   |

## SceneManagerWrapper
Calls the [[Scene Manager]]. Every function checks the manager exists first and logs an error naming the function if it doesn't. `ChangeLocation` also errors on missing scene data, and `ChangeScene` on a blank scene name.

| Function                  | Description                                            |
| :------------------------ | :----------------------------------------------------- |
| `ChangeLocation(SceneLocationSO)` | Travel using scene location data               |
| `ChangeScene(name)`       | Load a scene by name                                   |
| `HudFadeOnOpen(speed)`    | Fade the HUD in after the next scene loads             |
| `ActiveOpeningScene()`    | Show the loaded opening scene                          |
| `LoadOpeningScene()`      | Load the opening scene for the current chapter         |
| `LeaveRuneField()`        | *Nov 1, 2026.* Go back to the scene and access point the rune field was opened from. Hook the field's On Leave to this |
| `TestSingleton()`         | Odin **Test** button that checks the manager exists    |

## RuneFieldAccessWrapper
*Added Nov 1, 2026.* Goes on the [[Rune Field Access Point]] prefab. The access point's timeline calls one of its two functions. It tells the [[Shade Manager]] which kind of access point this is, then has the [[Scene Manager]] swap to the field (`TravelToRuneField`). Both functions check the Scene Manager and Shade Manager exist first.

| Function | Description |
| :--- | :--- |
| `LaunchAdvancedField()` | Opens the field with everything allowed (hub labs) |
| `LaunchBasicField()` | Opens the field with the basic tools (dungeon entrances, before bosses, safe and vault floors) |

`_ReturnLocator` (optional): where the player comes back to, and which way they face. Left empty, the wrapper's own object is used. A cyan gizmo shows the spot when the object is selected.

What each kind allows isn't set here, it's in the Shade Manager's Rune Field Access functions, so changing it doesn't touch the prefab or its timelines.

## HUDWrapper
Calls the [[HUD Manager]]. `FadeHudIn(speed)` and `FadeHudOut(speed)`.

## ShadeManagerWrapper
*Added Oct 9, 2026.* Calls the [[Shade Manager]]. Works on the selected shade slot unless the function takes a slot index. Every function checks the manager exists first and logs an error naming the function if it doesn't.

| Function | Calls |
| :--- | :--- |
| `RebuildCurrentSlot()` / `RebuildSlot(index)` | `RebuildSlot` (works the slot's runtime entry out again) |
| `HealCurrentShade(amount)` | `Heal` |
| `RefillCurrentShadeHealth()` / `RefillAllShadeHealth()` | `RefillHealth` / `RefillAllHealth` |
| `AddLevelsToCurrentShade(amount)` | `AddLevels` |

## CameraManagerWrapper
Calls the [[Camera Manager]]. `SetCamTargetToPlayer()` and `SetGameplayCameraPriority(priority)`. Both check the manager exists first.

---
## Notes
- New wrappers should follow the `DungeonManagerWrapper` pattern
