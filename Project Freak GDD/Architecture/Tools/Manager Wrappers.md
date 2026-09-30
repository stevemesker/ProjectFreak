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
| `GetMapNodeByID(id)`      | `getMapNode`                   |

## SceneManagerWrapper
Calls the [[Scene Manager]].

| Function                  | Description                                            |
| :------------------------ | :----------------------------------------------------- |
| `changeLocation(SceneLocationSO)` | Travel using scene location data               |
| `changeScene(name)`       | Load a scene by name                                   |
| `HudFadeOnOpen(speed)`    | Fade the HUD in after the next scene loads             |
| `ActiveOpeningScene()`    | Show the loaded opening scene                          |
| `LoadOpeningScene()`      | Load the opening scene for the current chapter         |
| `testSingleton()`         | Odin **Test** button that checks the manager exists    |

## HUDWrapper
Calls the [[HUD Manager]]. `FadeHudIn(speed)` and `FadeHudOut(speed)`.

## CameraManagerWrapper
Calls the [[Camera Manager]]. `setCamTargetToPlayer()` and `setGameplayCameraPriority(priority)`.

---
## Notes
- Only `DungeonManagerWrapper` and `HUDWrapper` check that their manager exists. `SceneManagerWrapper` and `CameraManagerWrapper` don't yet
- New wrappers should follow the `DungeonManagerWrapper` pattern
