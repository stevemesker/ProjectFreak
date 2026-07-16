## Purpose

Handles all functionality related to a unit's dash ability including:

- Dash charges and cooldowns
- Movement over a fixed duration using an Animation Curve
- Floor-aware dash direction
- Collision detection with walls and obstacles
- Pass-through damage collection
- Delayed damage application after the dash completes
- Dash start/end events

---

# Dash Flow

The dash is broken into three phases:

1. Dash initialization
2. Dash movement
3. Dash completion

---

## 1. Dash Initialization

Triggered through either:

- `DashCharacter(Vector3 direction)`
- `DashPassthrough(Vector3 direction, DamagePackage damage)`

### Charge Check

Before a dash begins the script verifies:

- The unit has at least one dash charge.
- Dashing is currently allowed.

If successful:

- One dash charge is consumed.
- The recharge coroutine begins if it is not already running.

### Floor Direction Calculation

`DashFloorDirectionCalculation()`

The incoming direction is projected onto the floor normal.

This allows dashes to naturally follow ramps and uneven terrain instead of remaining perfectly horizontal.

During this step the script also stores the floor position (`dashOriginPoint`) which becomes the origin for future raycasts.

### Dash Obstruction Scan

A `Physics.RaycastAll` is fired along the dash path.

Every hit is sorted by distance.

The raycast serves two purposes:

- Collect all damageable objects already along the dash path.
- Detect the first non-damageable object that blocks movement.

If a blocking object is found:

- The dash distance is shortened so the unit stops before the obstacle.

Otherwise the dash travels the full dash distance.

The dash coroutine is then started using the calculated start and end positions.

---

# 2. Dash Movement

Handled inside:

`DashRoutine()`

Movement uses an Animation Curve instead of a constant speed.

Each FixedUpdate:

1. Calculate normalized dash time.
2. Evaluate the animation curve.
3. Lerp between the start and end positions.
4. Move the Rigidbody using `MovePosition()`.

This allows the dash speed profile to be adjusted without modifying code.

---

## Pass-Through Damage

If a `DamagePackage` exists, the dash periodically performs sphere casts while moving.

### Detection Frequency

Rather than checking every frame, the script waits until the dash has traveled another:

`passThroughDetectionRadius`

Once that distance has been reached:

- A SphereCast is performed between the previous cast position and the current position.
- All damageable objects found are added to `hitList`.
- The next detection checkpoint is advanced by another radius distance.

This provides reliable hit detection while avoiding unnecessary physics queries every frame.

### Hit List

The hit list is implemented as a `HashSet<GameObject>`.

Benefits:

- Duplicate hits are automatically ignored.
- Multi-hit sphere casts cannot add the same object multiple times.
- Objects only receive damage once regardless of how many times they intersect the dash.

---

# 3. Dash Completion

When the dash reaches its destination:

- The Rigidbody is snapped to the final position.
- Every object stored in `hitList` receives the stored `DamagePackage`.
- `endDashEvent` is invoked.

Because damage is applied only after movement finishes, enemies appear to be cut only after the player has already dashed through them, creating the intended "anime samurai" effect.

---

# Dash Recharge

Dash charges regenerate independently using `DashRefresh()`.

Process:

- Wait for `cooldownTime`
- Restore one charge
- Continue restoring charges until `DashNumberMax` is reached

Only a single recharge coroutine can exist at a time.

---

# Important Functions

### DashCharacter()

Primary dash entry point.

Responsible for:

- Charge consumption
- Direction calculation
- Obstacle detection
- Starting the dash coroutine

---

### DashPassthrough()

Alternative dash entry point that stores a `DamagePackage` before beginning the dash.

Used for offensive dash abilities.

---

### DashRoutine()

Controls all dash movement.

Responsible for:

- Animation curve interpolation
- Rigidbody movement
- Periodic pass-through detection
- Applying delayed damage
- Triggering completion events

---

### DashFloorDirectionCalculation()

Projects dash movement onto the floor normal.

Also determines the raycast origin used for obstruction detection.

---

### FindDashStopperIndex()

Searches the ordered raycast results.

Returns:

- Index of the first object that blocks movement.
- `-1` if nothing obstructs the dash.

Damageable objects encountered before the blocker are immediately added to the hit list.

---

### SphereCastForHits()

Performs a sphere cast between the previous detection point and the current dash position.

Every object implementing `IDamagable` is added to the hit list.

---

### ApplyDashDamage()

Iterates through every object stored in `hitList` and calls:

`TakeDamage(DamagePackage)`

Since the hit list is a `HashSet`, each target is damaged at most once per dash.

---

# Events

### startDashEvent

Reserved for logic that should occur when a dash begins.

Examples:

- Play animation
- Spawn VFX
- Play audio
- Enable temporary effects

### endDashEvent

Invoked after movement and damage have completed.

Examples:

- End invulnerability
- Spawn landing effects
- Trigger combo windows
- Notify state machines

---

# Design Notes

- Dash movement follows terrain slopes.
- Dash distance shortens automatically when blocked by walls.
- Damage is delayed until the dash completes.
- Pass-through detection is sampled at fixed distance intervals rather than every frame.
- HashSet storage guarantees each target can only be damaged once during a dash.
- Animation Curves control dash feel without changing movement code.