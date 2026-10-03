Small general-purpose scripts. Most live in `Scripts/Common`.

---
## Components

| Script         | Description                                                                                                  |
| :------------- | :----------------------------------------------------------------------------------------------------------- |
| `DontDestroy`  | Keeps the object alive between scenes. When disabled, moves it back into the active scene                    |
| `TimedDelete`  | Destroys the object after `DeleteTime` seconds. Good for effects                                             |
| `Unselectable` | Editor only. Stops the object from being clicked in the Scene view (useful for big volumes or backgrounds)   |
| `ItemFloatAndSpin` | Makes dropped items bob and spin (see [[Items & Pickups]])                                               |
| [[ArcMover]]   | Moves an object along an arc                                                                                 |
| [[Interaction Object]] / `ActivateObject` | Interaction system                                                                |
| [[Timeline Runner System]] | Runs a list of timed UnityEvents                                                                 |
| `UINav`        | Pan and zoom for big UI windows (dungeon map, rune field). In `Scripts/UI`. Drag to pan; on release, if an edge has crossed the middle of the parent (the screen) plus `maxDragDistance`, it snaps back. Scroll zooms toward the middle of the screen, so the spot you're looking at stays centered, then snaps the same way. Bounds are measured in the parent's local space with the scaled corners, so they work at any zoom and screen size. If the window is zoomed out too small to reach both limits, it centers. Fires `ScaleEvent` after each zoom |

---
## Data

| Script             | Description                                                                                    |
| :----------------- | :--------------------------------------------------------------------------------------------- |
| `ColorPaletteSO`   | Primary, secondary, and tertiary colors. Used by map nodes, [[Dungeon Door]]s, and the [[Radial Menu]]. Created from **Create → Color → ColorPalette** |
| `NavMeshTools`     | Static helpers for NavMesh users: `IsWaitingOnFloor(unit, out floor)` (is this unit's dungeon floor still building its NavMesh?) and `TryGetNavMeshPoint(position, maxDistance, out point)` and `TryGetRandomPoint(center, radius, out point)` (a random spot on the NavMesh, used by Wander and Seek Fight). Used by `EnemyMovement`, `NavGuideDriver` and the [[Unit Brain]] actions |
| `SceneReference`   | Lets a scene asset be dragged into the inspector and stores its path. *Doesn't appear to be used yet* [[Notes for the future]] |
| `RarityType`, `CraftingBenchType`, `ElementMaterialType`, `DamageType` | Enum files |

---
## Editor Tools

| Script           | Description                                                                             |
| :--------------- | :-------------------------------------------------------------------------------------- |
| `DividerCreator` | Adds **GameObject → Create Divider**, which makes an empty `<===== Divider =====>` object to organize the hierarchy |

---
## Dev Scripts
In `Scripts/Dev Scripts`. For testing only.

| Script         | Description                                                                              |
| :------------- | :--------------------------------------------------------------------------------------- |
| `DamageTester` | Buttons that show a damage, healing, or crit popup at the object (see [[UIDamage Manager]]) |
| `SceneGetter`  | Button that writes a scene asset's name into a `SceneLocationSO` (see [[Scene Manager]]) |
| `SwingShapePreviewObject` | Draws a melee weapon's swing shapes in the Scene view without playing. Put it on an empty object on the floor (see [[Melee Weapon System#Previewing shapes]]) |
| `EnemyChaseTestBrain` | *Old temp brain, replaced by [[Unit Brain]] and no longer on any prefab. Safe to delete* [[Notes for the future]] |
| `ShadeFollowTestBrain` | *Old temp brain, replaced by [[Unit Brain]] and no longer on any prefab. Safe to delete* [[Notes for the future]] |
