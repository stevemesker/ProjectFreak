## Overview

![[ArcMover Example.png]]
`ArcMover` is a reusable utility component responsible for moving any GameObject from its current position to a target position using a configurable arc. The component is intentionally generic and contains no knowledge of the object it is moving, making it suitable for loot drops, thrown weapons, spell effects, projectiles, cutscene objects, and future gameplay systems.

The movement is deterministic and does not rely on Unity physics or projectile calculations. Instead, the object interpolates horizontally while its vertical motion is controlled by an `AnimationCurve`.

---

## Responsibilities

- Move an object from its current position to a destination.
- Create a configurable arc regardless of elevation differences.
- Handle both uphill and downhill movement automatically.
- Fire an event when movement has completed.
- Disable itself after completing the movement.

---

## Inspector Variables

### Arc Curve Shape (`AnimationCurve`)

Controls the shape of **half** of the arc.

The curve is evaluated twice:

- Once during ascent.
- Once during descent (in reverse).

This allows a single curve to control the entire arc while keeping both halves perfectly symmetrical.

---

### Finish Event (`UnityEvent`)

Invoked after the object reaches its destination.

Typical uses include:

- Enable loot pickup.
- Spawn landing particles.
- Play a landing sound.
- Begin idle animations.
- Destroy temporary objects.
- Trigger follow-up gameplay events.

---

## Public API

### `LaunchTo(Vector3 destination, float arcHeight, float duration)`

Begins moving the object toward a destination.

#### Parameters

| Parameter | Description |
|-----------|-------------|
| `destination` | Final world-space landing position. |
| `arcHeight` | Height added above the highest endpoint. |
| `duration` | Time required to complete the movement. |

When called:

1. Stops any existing arc movement.
2. Stores the current position as the start position.
3. Calculates the peak of the arc.
4. Stores the destination.
5. Enables the component.
6. Starts the movement coroutine.

---

## Peak Height Calculation

Instead of assuming the destination is level with the start, the peak is always calculated relative to whichever endpoint is higher.

```csharp
peakY = Mathf.Max(start.y, destination.y) + arcHeight;
```

This guarantees the object always rises before descending. No special-case calculations are required.

---

## Movement Algorithm

Movement is separated into two completely independent calculations.

### Horizontal Movement

Horizontal position is interpolated linearly.

```text
Start ------------------> Destination
```

```csharp
horizontal = Lerp(start, destination, t)
```

---

### Vertical Movement

Vertical movement is calculated independently.

The movement is divided into two phases.

### Phase 1 (Ascent)

Time Range:

```text
0.0 → 0.5
```

The animation curve is evaluated normally.

```text
Start Height
      │
      │
      ▲
   Peak Height
```

---

### Phase 2 (Descent)

Time Range:

```text
0.5 → 1.0
```

The same animation curve is evaluated in reverse.

```text
Peak Height
      │
      ▼
Destination Height
```

Because the curve is mirrored automatically, only a single `AnimationCurve` is required.

---

## Time Normalization

Movement is driven using normalized time.

```text
t = elapsedTime / duration
```

This normalized value controls:

- Horizontal interpolation
- Arc progression
- Completion timing

This keeps all movement independent of frame rate.

---

## Component Lifecycle

### Idle

- Component disabled.
- No active coroutine.

### Launch

`LaunchTo()` is called.

- Stores movement data.
- Starts coroutine.
- Enables component.

### Moving

Each physics update:

- Calculate normalized time.
- Calculate horizontal position.
- Calculate vertical position.
- Apply final transform.

### Complete

Upon reaching the destination:

- Snap precisely to the destination.
- Invoke `finishEvent`.
- Clear coroutine reference.
- Disable component.

---

## Design Notes

### Deterministic

Movement is entirely deterministic.

Advantages:

- No Rigidbody required.
- No gravity calculations.
- Identical results every execution.
- Easy to tune.

---

### Reusable

`ArcMover` contains no loot-specific logic.

Any object can use it, including:

- Loot drops
- Weapons
- Spell projectiles
- Environmental objects
- Boss attacks
- Cutscene props
- Future gameplay mechanics

---

### Separation of Responsibilities

The component intentionally **does not** determine where an object should land.

Another system is responsible for calculating the destination.

```text
Spawner
│
├── Calculates landing position
│
└── ArcMover
     │
     ├── Receives destination
     ├── Animates movement
     └── Fires completion event
```

This keeps landing logic separate from movement logic, allowing the same `ArcMover` component to be reused by any gameplay system.

---

## Future Expansion Ideas

Potential features that can be added without changing the overall architecture:

- Rotation while airborne.
- Spin speed settings.
- Horizontal easing curves.
- Landing bounce.
- Cancel movement.
- Complete movement instantly.
- Pause and resume movement.
- Optional facing direction during flight.
- Multiple movement profiles via ScriptableObjects.

These additions can be implemented while preserving the existing public API.