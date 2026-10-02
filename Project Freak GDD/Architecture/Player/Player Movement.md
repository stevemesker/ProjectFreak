## Overview

The player uses a physics-driven character controller inspired by the locomotion system used in [Toyful Games' controllers](https://www.youtube.com/watch?v=qdskE8PJy6Q). Movement, facing, and standing are handled as separate systems which operate simultaneously.

**Scripts:** three components that sit side by side on the unit. All three are used by the player and by [[Shade (Runtime)|shades]], so the shade moves the same way when the player takes control of it.

| Script              | Layer   | Job                                                                                          |
| :------------------ | :------ | :------------------------------------------------------------------------------------------- |
| `UnitHover`         | Stance  | Idle floating / standing spring (see [[#Standing System]]). Always on                        |
| `CharacterMovement` | Engine  | Turns a move direction and a look direction into physics forces. Always on. Reads no controls |
| `PlayerInputDriver` | Driver  | Reads the player's controls and feeds directions into the engine. Turned on/off to switch control |
| `NavGuideDriver`    | Driver  | *Shade only.* AI steering along the NavMesh, see [[#NavGuideDriver (AI steering)]] |

**Rule: switch drivers, never the engine.** Turning `CharacterMovement` off would also turn off its braking (the unit slides), and turning `UnitHover` off drops the unit. The shade also has an AI driver, `NavGuideDriver`, that feeds the same engine (see below and [[AI Movement & Dungeon Loading Plan]]). *(The older `PlayerMovement.cs` is from a previous version and is no longer used.)*

---

## CharacterMovement (engine) functions

| Function                  | Description                                                                                          |
| :------------------------ | :--------------------------------------------------------------------------------------------------- |
| `SetMoveDirection(Vector3)` | Which way to move, length 0 to 1 (1 = full speed). Up/down is ignored                              |
| `SetLookDirection(Vector3)` | Which way to face. A zero direction is ignored, so the unit keeps its facing                       |
| `GetMoveDirection()`      | The current move direction                                                                           |
| `Dash()`                  | Dashes in the current move direction using the unit's [[Unit Dash Script|UnitDash]]                  |
| `SetTurning(bool)`        | Turns rotation on/off without affecting movement. Turned off while the [[Radial Menu]] is open       |
| `DeactivateMovement()`    | Pauses the rigidbody and saves its velocity (used by dashes)                                         |
| `ReactivateMovement()`    | Restores the saved velocity                                                                          |

## PlayerInputDriver (player controls)

`Scripts/Unit Scripts/PlayerInputDriver.cs`. On the player (on by default) and the shade prefab (off by default).

- **On enable:** turns the controls on and listens for move, look (right stick), point (mouse) and dash.
- **On disable:** stops listening and **clears the move direction**, so the body you leave stops instead of walking on by itself. Facing is kept.
- Converts input to camera-relative directions using the [[Camera Manager]]'s gameplay camera (falls back to `Camera.main`), and works out mouse aim. Both are described below.
- Dash input calls the engine's `Dash()`.

| Field             | Description                                                                              |
| :---------------- | :--------------------------------------------------------------------------------------- |
| `_TurnIdleTime`   | Seconds after aiming stops before the unit faces its move direction again (default 1)    |
| `_Movement`       | The `CharacterMovement` it drives (auto-filled from the same object)                     |
| `_IsTurnSnapped`  | Read only. True while facing follows movement                                            |
| `_IsUsingMouse`   | Read only. True if the last aim came from the mouse                                      |

Switching control: `Player.EnablePlayerControl()` / `DisablePlayerControl()` and `Shade.EnablePlayerControl()` / `EnableShadeControl()` just turn the matching drivers on or off (called by the [[Shade Manager]]).

## NavGuideDriver (AI steering)

`Scripts/Unit Scripts/NavGuideDriver.cs`. On the shade prefab, turned on while the shade acts on its own.

A NavMeshAgent on the same object plans the route and steps around other agents, but **never moves the unit**: `updatePosition` and `updateRotation` are off. Every frame the driver:
1. Finds the NavMesh point under the floating unit (`NavMeshTools.TryGetNavMeshPoint`) and sets `agent.nextPosition` to it, so the agent always knows where the unit really is.
2. Reads `agent.desiredVelocity` (the direction the agent wants to go, already including path corners, slowing down near the end and avoidance).
3. Passes it to the engine: `SetMoveDirection(direction × speed fraction)` and `SetLookDirection(direction)`.

So knockback, dashing and hovering work the same whether the player or the AI is steering.

**Starting up:** waits for the dungeon floor's NavMesh like enemies do (`NavMeshTools.IsWaitingOnFloor`), then turns the agent on and warps it under the unit. If there's no NavMesh nearby it logs a warning and stays put. The agent's speed is set to the engine's top speed (`CharacterMovement.GetMaxSpeed()`) so the slowdown lines up.

**Off the NavMesh** (mid-air, knocked off an edge): the unit stops being steered until it's back over the NavMesh, then the agent is warped back under it and carries on to its destination.

| Function                  | Description                                                              |
| :------------------------ | :----------------------------------------------------------------------- |
| `SetDestination(Vector3)` | Go here. Remembered if the driver can't steer yet                        |
| `StopMoving()`            | Stop and forget the destination. Does nothing to movement while the driver is off |
| `HasArrived()`            | True when within `_StoppingDistance` of the destination (or no destination) |
| `IsGuiding()`             | True while the agent is on the NavMesh and steering                      |
| `IsActive()`              | From `IUnitMover`: true while guiding and over the NavMesh, so it can take AI orders (false while the player drives the shade) |

| Field                   | Description                                                                        |
| :---------------------- | :--------------------------------------------------------------------------------- |
| `_StoppingDistance`     | How close to the destination it stops, m (default 1.5)                              |
| `_NavMeshSnapDistance`  | How far to look for the NavMesh under the unit, m. Must be more than the float height (default 3) |
| `_FaceMoveThreshold`    | Below this fraction of full speed it stops turning toward its path, to avoid jitter (default 0.1) |
| `_Movement`             | The `CharacterMovement` it drives (auto-filled)                                     |

**Keep the NavMeshAgent turned off in the prefab.** The driver turns it on once there's a NavMesh (the inspector warns if it's on).

The controller is designed to support:

* Camera-relative movement
* Independent movement and aiming directions
* Mouse and gamepad aiming
* Strafing and backpedaling
* Physics-based acceleration and deceleration
* Stable movement across moving platforms and uneven terrain

---

# Movement

## Camera Relative Movement

Movement input is always interpreted relative to the camera's orientation.

Examples:

| Input | Result                                   |
| ----- | ---------------------------------------- |
| Up    | Move toward the top of the screen        |
| Down  | Move toward the bottom of the screen     |
| Left  | Move toward the left side of the screen  |
| Right | Move toward the right side of the screen |

The player's movement direction is converted into world space using the camera's yaw rotation.

This ensures movement remains intuitive regardless of camera angle.

---

## Locomotion Model

Movement is not applied directly to the transform.

Instead, player input generates a desired movement direction (`m_UnitGoal`) which is converted into a target velocity.

```text
Input → Desired Direction → Goal Velocity → Applied Force
```

A smoothed internal velocity (`m_GoalVel`) is used to gradually approach the desired velocity.

This provides:

* Adjustable acceleration
* Adjustable deceleration
* Responsive movement
* Reduced jitter and force spikes

---

## Acceleration

Acceleration is influenced by the relationship between:

* Current movement direction
* Desired movement direction

The dot product between these vectors is evaluated through animation curves.

This allows different acceleration behavior when:

* Continuing forward
* Turning
* Reversing direction
* Starting from rest

Example:

```text
Forward → Forward
High acceleration

Forward → Reverse
Lower acceleration
```

The acceleration curves are used to control movement feel without modifying code.

### Curve shape

Both curves (`AccelerationFactorFromDot` and `MaxAccelerationForceFactorFromDot`) take the dot product on the **X axis from -1 to 1**:

| Dot | Meaning                 | Current value |
| :-- | :---------------------- | :------------ |
| -1  | Full reverse            | 2             |
| 0   | 90° turn / from a stop  | 1             |
| 1   | Same direction          | 1             |

So reversing gets double acceleration and a double force cap, and running straight uses the base values. Raise the -1 value for snappier turnarounds, or lower `acceleration` / `maxAccelForce` for a heavier feel.

*Oct 2026 fix:* the curves used to only cover 0 to 1 and dropped to almost 0 at 1, so once the unit was moving the way it wanted, its acceleration almost switched off. It never reached `maxSpeed`, and building speed after a reverse crawled. The force cap is also now applied after converting to acceleration (divide by `fixedDeltaTime`, then clamp), the same as Toyful's controller.

---

## Force Application

The controller calculates the acceleration required to reach the target velocity.

Acceleration is clamped to a configurable maximum force before being applied to the Rigidbody.

This provides:

* Stable physics behavior
* Consistent acceleration limits
* Easy tuning of movement responsiveness

---

# Facing System

## Overview

Movement direction and facing direction are intentionally separated.

The player can:

* Move while facing forward
* Strafe
* Walk backwards
* Move in one direction while aiming in another

Examples:

```text
Move Left
Face Right

Move Forward
Face Left

Move Backward
Face Forward
```

This behavior is required for ranged combat and twin-stick style aiming.

---

## Movement Facing

When no aiming input is active:

```text
Facing Direction = Movement Direction
```

The character automatically faces the direction they are moving.

---

## Aim Facing

When aiming input is detected:

```text
Facing Direction = Aim Direction
```

The character rotates toward:

* Mouse position
* Right stick direction

depending on the active input device.

Movement and facing become independent while aiming.

---

## Returning To Movement Facing

When aiming input stops:

1. A timer begins.
2. The character maintains the current facing direction.
3. After the timer expires, movement-facing mode resumes.

This prevents unwanted snapping when briefly releasing the aim controls.

---

# Mouse Aiming

Mouse aiming uses a virtual horizontal plane located at the player's height.

A ray is projected from the cursor position onto this plane.

The resulting world-space position is used to calculate the desired facing direction.

Benefits:

* Independent of level geometry
* Consistent aiming behavior
* No interference from colliders
* Works on slopes and uneven terrain

---

# Gamepad Aiming

Gamepad aiming uses the right stick.

The right stick direction is converted from screen space into world space using the current camera orientation.

This creates camera-relative aiming behavior consistent with movement controls.

---

# Standing System

## Overview

The character uses a spring-based hovering system rather than relying solely on gravity and collider contact.

**Script:** `UnitHover` (`Assets/Scripts/Unit Scripts/UnitHover.cs`)

A downward raycast measures the distance between the player and the ground.

A spring force is then applied to maintain the desired ride height.

---

## Spring Model

The standing system consists of:

* Ride Height
* Spring Strength
* Spring Damping

The spring attempts to maintain the configured ride height while damping vertical velocity.

This creates:

* Smooth traversal over uneven surfaces
* Stable platform interaction
* Predictable movement behavior

| Inspector field         | Description                                                              |
| :---------------------- | :----------------------------------------------------------------------- |
| `_RayLength`            | How far down to look for the floor (m). Must be longer than ride height  |
| `_RideHeight`           | How high above the floor the unit floats (m)                             |
| `_RideSpringStrength`   | How hard the spring pushes back to ride height                           |
| `_RideSpringDamper`     | How much the spring resists bouncing                                     |
| `_RB`                   | The unit's rigidbody (auto-filled from the same object if empty)         |
| `_OnDebugDrawLines`     | Draws the spring force in the scene view                                 |

`IsGrounded()` returns whether the floor was found this physics step, for AI, animation or abilities to check.

---

# Update Order

Each physics step, `UnitHover` applies the standing force and `CharacterMovement` applies the movement and rotation forces. They are separate components, so Unity doesn't guarantee which runs first, but forces add together so the order doesn't matter.

```text
UnitHover          → Standing Force
CharacterMovement  → Movement Force → Rotation Force
```

Each system is independent and can be tuned separately.

This separation allows future expansion of:

* Dashes
* Knockback
* Lock-on targeting
* Status effects
* Movement abilities
* Additional locomotion modes

```
```
