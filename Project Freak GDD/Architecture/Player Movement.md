## Overview

The player uses a physics-driven character controller inspired by the locomotion system used in [Toyful Games' controllers](https://www.youtube.com/watch?v=qdskE8PJy6Q). Movement, facing, and standing are handled as separate systems which operate simultaneously.

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

---

# Update Order

The controller executes the following systems during each physics step:

```text
Standing Force
    ↓
Movement Force
    ↓
Rotation Force
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
