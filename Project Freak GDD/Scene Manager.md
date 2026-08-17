## Overview

The **Scene Manager** is responsible for controlling scene transitions, opening/title scene selection, scene-load transition behavior, and player relocation between connected scene locations.

It acts as the central point for scene-changing operations so that other systems do not need to directly interact with Unity's `SceneManager`. It also coordinates with the `SaveManager`, `GameManager`, `HUDManager`, and `Player` systems when necessary.

---

## Responsibilities

The `SceneManagerObject` is responsible for:

* Selecting the appropriate opening scene based on the player's current save chapter.
* Loading opening scenes additively while keeping the initial scene active.
* Temporarily disabling the loaded opening scene until it is ready to be displayed.
* Activating the opening scene when requested.
* Loading gameplay scenes.
* Detecting when a scene has finished loading.
* Triggering HUD fade-in transitions after scene loads.
* Handling scene-location data for doors and other location transitions.
* Determining whether a loaded location matches the player's intended destination.
* Moving and rotating the player to a specified world-space location.

---

## Singleton Access

The Scene Manager maintains a static reference:

```csharp
public static SceneManagerObject _SceneManager;
```

During `Awake()`, the first instance assigns itself to this reference.

Other systems can therefore access the Scene Manager through:

```csharp
SceneManagerObject._SceneManager
```

The Scene Manager is intended to function as a centralized runtime service rather than having multiple active instances.

---

# Opening Scene Management

## Chapter-Based Opening Scenes

Opening scenes are selected using the `_loadSceneOpeningByChapterIndex` list.

Each list index corresponds to a chapter in the save data:

```text
List Index 0 → Chapter 0
List Index 1 → Chapter 1
List Index 2 → Chapter 2
...
```

When `loadStartScene()` is called, the Scene Manager retrieves the current save slot from the `SaveManager` and checks its saved chapter.

If the chapter has a corresponding entry in the opening scene list, that scene is loaded.

If the chapter exceeds the number of available entries, the system defaults to index `0`.

This allows the opening/title presentation to change as the player progresses through the game's chapters without requiring the save system to store a specific scene name.

### Example

```text
Chapter 0 → OpeningScene_Chapter0
Chapter 1 → OpeningScene_Chapter1
Chapter 2 → OpeningScene_Chapter2
```

The save system only needs to know:

```text
Save Chapter = 2
```

The Scene Manager determines which opening scene that corresponds to.

---

## Additive Opening Scene Loading

Opening scenes are loaded using:

```csharp
LoadSceneMode.Additive
```

This allows the initial scene to remain loaded while the opening scene is prepared in the background.

The Scene Manager waits for the asynchronous load operation to finish before continuing.

Once loaded, the scene is stored in `_currentOpeningScene`.

---

## Delayed Opening Scene Activation

After the opening scene has finished loading, all of its root GameObjects are immediately disabled.

This allows the scene to be completely loaded without allowing its contents to become visible before the appropriate point in the opening sequence.

```text
Initial Scene
      │
      ▼
Load Opening Scene Additively
      │
      ▼
Wait for Scene Load
      │
      ▼
Disable Opening Scene Root Objects
      │
      ▼
Opening / Timeline Sequence
      │
      ▼
activateOpeningScene()
      │
      ▼
Opening Scene Becomes Visible
```

`activateOpeningScene()` re-enables all root objects in the stored opening scene.

If no valid opening scene has been loaded, the function logs a warning and exits safely.

---

# Scene Selection

## Changing Scenes

The `changeScene()` function provides the primary method for loading a new gameplay scene.

```csharp
public void changeScene(string sceneName)
```

The requested scene name is stored in `_currentSceneName` and then loaded using Unity's standard scene loading system.

This gives other gameplay systems a simple interface for requesting a scene change without needing to directly interact with Unity's Scene Management API.

---

# Scene Load Events

The Scene Manager subscribes to Unity's:

```csharp
SceneManager.sceneLoaded
```

event when enabled.

This allows the Scene Manager to react after Unity has successfully completed loading a scene.

The event is unsubscribed when the object is disabled to prevent stale event subscriptions.

---

## HUD Fade-In

Scene transitions can optionally request a HUD fade-in after the next scene finishes loading.

This is controlled by:

```csharp
_fadeInTransition
_fadeInTransitionSpeed
```

`HudFadeOnOpen()` enables this behavior and stores the desired fade speed.

When the next scene finishes loading, `OnSceneLoaded()` checks the flag.

If enabled, it calls:

```csharp
HUDManager._HUD.FadeIn(_fadeInTransitionSpeed);
```

The flag is then reset so that subsequent scene loads do not automatically trigger another fade.

### Intended Transition Flow

```text
Request Scene Change
       │
       ▼
HudFadeOnOpen()
       │
       ▼
_fadeInTransition = true
       │
       ▼
Scene Loads
       │
       ▼
OnSceneLoaded()
       │
       ▼
HUD Fade In
       │
       ▼
Reset Transition Flag
```

This allows the fade request to be made **before** the scene change while the actual fade-in occurs only after Unity confirms that the new scene has loaded.

---

# Scene Location System

The Scene Manager works with `SceneLocationSO` objects to handle location-based transitions.

A location can represent destinations such as:

* Doors
* Room entrances
* Teleport locations
* Scene transition points
* Other designated player spawn locations

The `sceneLocationDataChange()` function determines whether the supplied location exists in another scene or within the current scene.

---

## Linked Scene Locations

If the `SceneLocationSO` is marked as linked:

```csharp
if (data._IsLinked)
```

the Scene Manager:

1. Stores the linked destination in `_PlayerMoveTarget`.
2. Loads the scene specified by the location data.

The destination is saved before the scene changes so that the newly loaded scene can determine which location the player should be placed at.

### Flow

```text
Player Uses Door
      │
      ▼
SceneLocationSO
      │
      ├── Is Linked?
      │       │
      │       ▼
      │   Store Target
      │       │
      │       ▼
      │   Load Destination Scene
      │
      ▼
New Scene
      │
      ▼
Find Matching SceneLocationSO
      │
      ▼
Move Player To Destination
```

---

## Local Scene Locations

If the destination is not linked to another scene, the Scene Manager does not perform a scene change.

Instead, it immediately moves the player to the location specified by the `SceneLocationSO`.

The player's rotation is also updated using the supplied rotation.

This allows doors and transitions within the same scene to use the same location system without requiring unnecessary scene loads.

---

# Destination Matching

The `TestDoorEntranceTarget()` function determines whether a particular `SceneLocationSO` matches the destination that was previously stored during a scene transition.

```csharp
public bool TestDoorEntranceTarget(SceneLocationSO data)
```

It compares the supplied location against `_PlayerMoveTarget`.

If they are the same object, the function returns `true`.

This allows scene-local objects to identify the specific door or entrance that the player was travelling toward after a scene change.

---

# Player Relocation

The `movePlayerToLocation()` function directly sets the player's position and rotation.

```csharp
public void movePlayerToLocation(Vector3 location, Quaternion rotation)
```

This is used to place the player at the correct entrance after a scene transition or when moving between locations within the same scene.

The player is accessed through the global `Player.player` reference.

---

# Inspector Data

## Opening Scene Data

### `_loadSceneOpeningByChapterIndex`

A list of scene names where each list index corresponds to a save-game chapter.

Used by the opening scene loading system.

---

## Scene Data

### `_currentSceneName`

Stores the name of the scene currently being requested by the Scene Manager.

Primarily used internally when changing scenes.

---

## Location Change

### `_fadeInTransition`

Determines whether the next successfully loaded scene should trigger a HUD fade-in.

### `_fadeInTransitionSpeed`

Controls the speed/duration used when requesting the HUD fade-in.

### `_PlayerMoveLocation`

Stores a world-space player destination.

### `_PlayerMoveTarget`

Stores the `SceneLocationSO` that the player is attempting to reach after a scene transition.

---

# System Relationships

The Scene Manager acts as an intermediary between several systems:

```text
                 ┌──────────────┐
                 │  GameManager │
                 └──────┬───────┘
                        │
                        ▼
                 ┌──────────────┐
                 │ SaveManager  │
                 └──────┬───────┘
                        │
                        │ Chapter
                        ▼
              ┌─────────────────────┐
              │   Scene Manager     │
              │ SceneManagerObject  │
              └──────┬───────┬──────┘
                     │       │
          ┌──────────┘       └──────────┐
          ▼                             ▼
   Scene Management                 HUD Manager
          │                             │
          ▼                             ▼
    Loaded Scene                   Fade Transition
          │
          ▼
   SceneLocationSO
          │
          ▼
     Player Position
```

---

# Design Goals

The Scene Manager is designed to keep scene-transition logic centralized.

Other systems should primarily communicate their intent to the Scene Manager rather than directly handling scene loading, scene-load events, or player relocation.

This keeps responsibilities separated:

* **SaveManager** — determines the player's saved progression.
* **Scene Manager** — determines what scene needs to be loaded and manages the transition.
* **SceneLocationSO** — defines where a location leads.
* **HUDManager** — handles visual fade transitions.
* **Player** — provides the player object that needs to be relocated.
* **Unity Scene Management** — performs the actual scene loading.

The result is a single system responsible for coordinating the scene transition while allowing the individual systems involved in that transition to remain relatively independent.
