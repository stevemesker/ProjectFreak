## Overview

The **Camera Manager** is responsible for creating, configuring, and controlling the game's primary gameplay camera.

It provides a centralized interface for the gameplay camera so that other systems do not need to directly manage the Cinemachine virtual camera.

The Camera Manager currently focuses specifically on the **Gameplay Virtual Camera** and leaves camera selection and blending between Cinemachine cameras to Cinemachine's priority system.

---

## Responsibilities

The `CameraManager` is responsible for:

* Maintaining a global reference to the Camera Manager.
* Creating the gameplay camera from a prefab.
* Retrieving the Cinemachine Virtual Camera component.
* Retrieving and configuring the camera's Cinemachine Transposer.
* Applying the gameplay camera's follow offset.
* Setting the player as the camera's Follow and LookAt target.
* Providing the gameplay camera to the player's movement system.
* Controlling the gameplay camera's Cinemachine priority.

---

# Singleton Access

The Camera Manager maintains a static reference:

```csharp
public static CameraManager _CamManager;
```

During `Awake()`, the first active instance assigns itself to this reference.

Other systems can access the Camera Manager through:

```csharp
CameraManager._CamManager
```

This allows camera-related systems to request changes without needing a direct reference to the Camera Manager object.

---

# Gameplay Camera Creation

The gameplay camera is created from the `_gameplayCameraPrefab`.

If `_currentGameplayCamera` has not already been assigned when `Awake()` runs, the Camera Manager instantiates the prefab:

```text
CameraManager
      │
      ▼
Check Current Gameplay Camera
      │
      ├── Exists → Use Existing Camera
      │
      └── Null
           │
           ▼
     Instantiate Prefab
           │
           ▼
 Current Gameplay Camera
```

This allows the gameplay camera to be created at runtime rather than requiring a manually placed gameplay camera in every scene.

---

# Cinemachine Configuration

The Camera Manager retrieves the `CinemachineVirtualCamera` component from the spawned gameplay camera.

It then retrieves the camera's `CinemachineTransposer`:

```csharp
_transposer = _followCam.GetCinemachineComponent<CinemachineTransposer>();
```

The Transposer is responsible for controlling the camera's positional relationship to its Follow target.

The configured follow offset is applied through:

```csharp
_transposer.m_FollowOffset = _followOffset;
```

This makes `_followOffset` the centralized setting for the gameplay camera's default position relative to the player.

---

# Camera Initialization

Camera configuration occurs during `Start()`.

The initialization process is:

1. Retrieve the `CinemachineVirtualCamera`.
2. Retrieve the `CinemachineTransposer`.
3. Set the initial camera priority.
4. Apply the configured follow offset.
5. Set the camera target to the player.

### Initialization Flow

```text
Camera Manager Start
        │
        ▼
Get Virtual Camera
        │
        ▼
Get Cinemachine Transposer
        │
        ▼
Set Priority = 0
        │
        ▼
Apply Follow Offset
        │
        ▼
Set Player as Camera Target
```

The initial priority of `0` keeps the gameplay camera from automatically becoming the active Cinemachine camera until another system explicitly gives it the desired priority.

---

# Player Camera Target

The `setCamTargetToPlayer()` function assigns the player as both the camera's `Follow` and `LookAt` target.

```csharp
_followCam.Follow = Player.player.transform;
_followCam.LookAt = Player.player.transform;
```

This establishes the gameplay camera's relationship with the player.

The function first verifies that the player exists before attempting to assign the target.

---

## Movement System Camera Reference

When the player is assigned as the camera target, the Camera Manager also provides the instantiated gameplay camera to the player's `CharacterMovement` component:

```csharp
Player.player.GetComponent<CharacterMovement>()._MainCamera = _currentGameplayCamera;
```

This allows the movement system to use the gameplay camera when calculating camera-relative movement.

The dependency therefore flows in one direction:

```text
Camera Manager
      │
      ▼
Gameplay Camera
      │
      ▼
Character Movement
      │
      ▼
Camera-Relative Movement
```

The Camera Manager is therefore responsible for ensuring that the movement system receives the correct gameplay camera reference.

---

# Camera Priority

The `setGameplayCameraPriority()` function provides a simple interface for changing the gameplay camera's Cinemachine priority:

```csharp
public void setGameplayCameraPriority(int priority)
```

The Camera Manager does not directly determine which camera should be active.

Instead, it relies on **Cinemachine's priority system** to determine which virtual camera should control the main camera.

This allows other virtual cameras—such as cutscene, dialogue, or special gameplay cameras—to take control simply by having a higher priority.

### Example

```text
Gameplay Camera
Priority: 10

Cutscene Camera
Priority: 20
        │
        ▼
Cinemachine selects Cutscene Camera
```

When the cutscene finishes, the gameplay camera can be given a higher priority again.

This keeps camera selection centralized within Cinemachine rather than requiring the Camera Manager to manually enable and disable cameras.

---

# Inspector Data

## Settings

### `_gameplayCameraPrefab`

Prefab containing the game's primary gameplay Cinemachine Virtual Camera.

The Camera Manager instantiates this prefab at runtime if no gameplay camera currently exists.

### `_followOffset`

Defines the default positional offset of the gameplay camera relative to its Follow target.

This value is applied to the camera's `CinemachineTransposer`.

---

# Runtime Data

### `_currentGameplayCamera`

Stores the instantiated gameplay camera GameObject.

This is the actual camera object used by the game at runtime.

### `_followCam`

Reference to the `CinemachineVirtualCamera` component on the gameplay camera.

Used to control:

* Follow target
* LookAt target
* Priority

### `_transposer`

Reference to the camera's `CinemachineTransposer`.

Used to control the camera's positional offset from the player.

---

# System Relationships

The Camera Manager primarily acts as the bridge between the game's gameplay systems and Cinemachine.

```text
                    ┌────────────────────┐
                    │   Camera Manager   │
                    └─────────┬──────────┘
                              │
                              ▼
                   Gameplay Camera Prefab
                              │
                              ▼
                ┌──────────────────────────┐
                │ Cinemachine Virtual      │
                │ Camera                   │
                └────────────┬─────────────┘
                             │
                 ┌───────────┴───────────┐
                 ▼                       ▼
             Transposer             Priority
                 │                       │
                 ▼                       ▼
           Follow Offset          Camera Selection
                                         │
                                         ▼
                                  Cinemachine Brain
                                         │
                                         ▼
                                   Main Camera
```

The Camera Manager also connects the gameplay camera to the player:

```text
Camera Manager
      │
      ├──────────────► Cinemachine Virtual Camera
      │
      │                    Follow
      │                      │
      │                      ▼
      │                   Player
      │                      │
      │                      ▼
      └──────────────► CharacterMovement
                         Main Camera
```

---

# Design Goals

The Camera Manager is intentionally kept focused on the **gameplay camera** rather than becoming a general-purpose camera controller.

Cinemachine is responsible for:

* Determining the active virtual camera.
* Camera blending.
* Camera transitions.
* Managing the relationship between virtual cameras and the main camera.

The Camera Manager is responsible for:

* Creating the gameplay camera.
* Configuring its initial settings.
* Assigning the player as its target.
* Providing the gameplay camera to systems that require it.
* Exposing gameplay camera priority control.

This separation allows additional virtual cameras to be introduced without requiring the Camera Manager to directly manage every camera in the project.

---

# Current Camera Architecture

The resulting camera architecture is:

```text
                         Camera Manager
                               │
                               ▼
                    Gameplay Virtual Camera
                               │
                     ┌─────────┴─────────┐
                     ▼                   ▼
                  Player             Transposer
                  Target            Follow Offset
                     │
                     ▼
              Character Movement
                     │
                     ▼
              Camera-Relative Input


Other Virtual Cameras
        │
        │ Priority
        ▼
  Cinemachine Brain
        │
        ▼
    Main Camera
```

The gameplay camera can therefore exist independently of individual gameplay scenes while Cinemachine handles which virtual camera is currently driving the main camera.
