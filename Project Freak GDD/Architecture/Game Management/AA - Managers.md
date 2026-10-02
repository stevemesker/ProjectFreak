The game runs off of a set of managers that each keep track of their own task. All managers live as components on the single **Game Manager** object, which is spawned at startup by the [[Runtime Bootstrapper & Runtime Asset|Runtime Bootstrapper]] and persists between scenes.

---
## Access Rules
- Every manager exposes a **static singleton** so any script can reach it directly (see table below)
- Managers **can** call each other directly through their singletons
- Scene objects and UnityEvents that need a manager go through a **Wrapper** component instead (see [[Manager Wrappers]]). Wrappers check the manager exists before forwarding the call, which keeps scenes testable on their own
- *Old rule (removed):* managers used to be required to go through [[Game Manager]] instead of calling each other. That was dropped once all managers moved onto one object

---
## Manager List

| Manager               | Script                  | Singleton                           | Description                                                                                |
| :-------------------- | :---------------------- | :---------------------------------- | :----------------------------------------------------------------------------------------- |
| [[Game Manager]]      | `GameManager`           | `GameManager._GameManager`          | Root of the manager object. Holds high level data like player level and shade list access |
| [[Shade Manager]]     | `ShadeManager`          | *(none yet, use `GetComponent`)*    | Data for each shade in the [[Shade Slot]]s, summoning, and switching control to the shade |
| [[Inventory Manager]] | `InventoryManager`      | `InventoryManager._PlayerInventory` | Adds/removes ingredients and elements from the player's inventory                         |
| [[Save Manager]]      | `SaveManager`           | `SaveManager._save`                 | In charge of saving/loading game data and the active [[Save slot]]                        |
| [[Camera Manager]]    | `CameraManager`         | `CameraManager._CamManager`         | Creates the gameplay camera, sets its follow target, and handles camera shake             |
| [[UIDamage Manager]]  | `ScreenDamageUIManager` | `ScreenDamageUIManager._UIdamage`   | Oversees damage popups on screen                                                           |
| [[Scene Manager]]     | `SceneManagerObject`    | `SceneManagerObject._SceneManager`  | Scene loading, opening scenes, and moving the player between scene locations             |
| [[Dungeon Manager]]   | `DungeonManager`        | `DungeonManager._DM`                | Runs the current dungeon: map creation, moving between floors, and POI lookups            |
| [[HUD Manager]]       | `HUDManager`            | `HUDManager._HUD`                   | Screen fades and the [[Radial Menu]]                                                       |
| [[Shade Manager]]     | `ShadeManager`          | `ShadeManager._ShadeManager`        | Shade slot data, summoning the shade, and switching control between player and shade      |

---
## Notes
- Singleton setup isn't consistent yet. Some managers destroy duplicates in `Awake`, others just skip assigning. See [[Known Issues]] [[Notes for the future]]
