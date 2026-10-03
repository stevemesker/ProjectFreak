## Overview
The plan for how melee weapons work in code. **Nothing here is built yet (Oct 2026).** It's the design to build from once the ranged notes are in and the weapon overhaul starts. [[Notes for the future]]

Design side: [[Weapons#Melee]]. Ranged counterpart: [[Ranged Weapon System]]. Back to [[Architecture Atlas]].

```text
MeleeWeaponItem (WeaponItem)
 ├── _Reach, _ComboResetTime, damage, knockback...
 └── _Combo
       ├── ComboStepEntry 0 → SwingShapeSO (Slash) + timing, damage, stagger
       ├── ComboStepEntry 1 → SwingShapeSO (Backslash)
       └── ComboStepEntry 2 → SwingShapeSO (Thrust)   ← finisher

WeaponAttackMelee (ITriggerable, on the held weapon)
 └── runs the combo and does the hit checks
```

---
## Why a Swept Shape
Two other ways were considered:
- **Trigger collider on the weapon:** physics only checks overlaps at fixed steps, while the animation moves the weapon every frame. A fast swing can jump right past an enemy between two checks and never touch it.
- **Tracing points on the blade:** reliable, but every animation has to be set up and tuned, and it's more precision than the isometric camera needs.

Instead, each swing is a **shape defined as data** (an arc with a reach and height range) checked with physics queries. The shape grows across the swing's active time, so hits still happen in order (left side of a slash first). Because the check covers the whole angle swept so far, not where the weapon is this frame, a fast swing or a frame hitch can't skip anyone.

---
## SwingShapeSO
One asset per swing type (Slash, Overhead, Thrust...), shared by every weapon. **Create → Combat → Swing Shape** (`SO_Swing_Slash`). [[Notes for the future]]

| Field           | Description |
| :-------------- | :---------- |
| `_SweepMode`    | `Angle`: the arc grows sideways from start to end angle (slashes, spins). `Reach`: the arc grows outward from the wielder (thrusts). `Instant`: the whole shape hits at once (slams) |
| `_StartAngle`   | Degrees from the wielder's forward where the swing starts. Negative = left |
| `_EndAngle`     | Degrees where the swing ends. A spin is -180 to 180 |
| `_InnerRadius`  | Meters. Usually 0. Lets a shape skip things right on top of the wielder |
| `_Reach`        | Meters at 1× weapon reach |
| `_HeightBelow`  | How far below the wielder's feet the swing reaches, meters |
| `_HeightAbove`  | How far above the wielder's feet the swing reaches, meters |
| `_SweepCurve`   | 0-1 curve for how the sweep moves over the active time, so a swing can start slow and whip through |

Should get a gizmo that draws the shape in the Scene view (on a test weapon or the player) so shapes can be tuned by eye.

---
## MeleeWeaponItem
Inherits `WeaponItem` (see [[Weapon usage]]). Keeps base damage, element and knockback from it. [[Notes for the future]]

| Field             | Description |
| :---------------- | :---------- |
| `_Reach`          | Multiplier on every swing shape's reach. A dagger might be 0.7, a greatsword 1.5 |
| `_ComboResetTime` | Seconds without attacking before the combo goes back to hit 1 |
| `_Combo`          | The ordered list of `ComboStepEntry`s. The last one is the finisher |

*The ranged-only settings moved down into `WeaponRangedItem` in Oct 2026, so `WeaponItem` only holds what every weapon shares. See [[Ranged Weapon System#WeaponItem]].*

### ComboStepEntry
One hit in the combo.

| Field                   | Description |
| :---------------------- | :---------- |
| `_Swing`                | The `SwingShapeSO` this hit uses |
| `_Animation`            | Which attack animation to play |
| `_Windup`               | Seconds before the swing can hit |
| `_ActiveTime`           | Seconds the swing sweeps and can hit |
| `_Recovery`             | Seconds after the swing before the next hit can start |
| `_DamageMultiplier`     | × the weapon's base damage |
| `_KnockbackMultiplier`  | × the weapon's knockback |
| `_StaggerPower`         | 0-1. How likely this hit is to stagger (see [[#Stagger]]) |
| `_StepEffect`           | Optional extra effect when the hit happens, like a magic slash projectile on a finisher. Added later [[Notes for the future]] |

Timings are written at 1× speed. The wielder's attack speed scales the timings and the animation together.

---
## WeaponAttackMelee
The melee version of `WeaponAttackRanged`. Lives on the weapon prefab and implements `ITriggerable`. [[Notes for the future]]

| Function           | Plan |
| :----------------- | :--- |
| `SetUpWeapon(...)` | Caches the item, the wielder, its stats and its `UnitTeam` |
| `TriggerAttack()`  | If idle, starts the current combo step. If mid-swing, buffers the press so the next step starts right after recovery |
| `ReleaseAttack()`  | Clears the held flag. Holding the button keeps the combo going |
| `IsRange()`        | `false`, so melee uses [[STR]] |
| `IsBusy()`         | True from the start of a swing until its recovery ends, so weapons can't be switched mid-swing (see [[Ranged Weapon System#Weapon Switching]]) |

### Combo Flow
```text
TriggerAttack
    ↓
Windup   → wielder's facing is locked for the swing
    ↓
Active   → sweep + hit checks every frame, hit list cleared at the start
    ↓
Recovery
    ↓
Buffered or held? → next step (after the finisher, back to step 0)
Otherwise         → wait _ComboResetTime, then back to step 0
```

Runs in a stored `Coroutine`. Weapon swaps, disabling, or the wielder getting staggered stop it and reset the combo.

### Hit Check (each frame of the active time)
1. Work out how far the sweep has gone: `_SweepCurve` at elapsed / active time, turned into an angle (or a reach for `Reach` mode).
2. `Physics.OverlapSphereNonAlloc` around the wielder at full reach × weapon reach, on the Units and destructibles layers.
3. For each collider:
   - Skip it if it's the wielder, an ally (`UnitTeam`), has no `IDamagable`, or is already in this swing's hit list
   - Get the closest point on its collider to the wielder (`Collider.ClosestPoint`), so big enemies are hit by their body
   - **Height check:** the point's height relative to the wielder's feet is within `_HeightBelow` / `_HeightAbove`
   - **Flat check:** on the wielder's horizontal plane, the distance is between inner radius and reach, and the angle from the locked forward is inside the part swept so far
   - **Line of sight:** linecast from the wielder's chest to the point against level geometry. Blocked means no hit
4. Passed everything: add it to the hit list, build the [[Damage Package]], call `TakeDamage`.

*`ClosestPoint` only works on box, sphere, capsule and convex mesh colliders. Units should use those.*

The timing data is the source of truth for when a swing hits, not the animation. Animations should be made to match, and animation events are only for effects and sounds. If hand matching gets tedious, an editor tool could later read a swing animation and fill in the timing and angles automatically. [[Notes for the future]]

---
## Damage
Per [[Weapons#Damage Package]], melee builds its package at the moment of the hit, using the wielder's current stats:
- Damage entries: weapon base damage × the step's `_DamageMultiplier`, with the wielder's STR
- `_KnockbackDistance`: weapon knockback × the step's `_KnockbackMultiplier`
- Crit and impact strength as usual

New fields the [[Damage Package]] will need: [[Notes for the future]]

| Field             | Description |
| :---------------- | :---------- |
| `_StaggerPower`   | The hit's stagger power. 0 for things that can't stagger |
| `_HitDirection`   | Optional direction for knockback. Lets a finisher push enemies along the swing instead of straight away from the wielder |

---
## Stagger
Works like knockback: a new `IStaggerable` interface that `EnemyDamagable` checks for after `IKnockbackable` (see [[Damage Receivers & Projectiles]]). [[Notes for the future]]

**Chance** = the hit's `_StaggerPower` × the enemy size's stagger multiplier. The multiplier is a new column on the `SizeClassRulesSO` (see [[Enemy Movement#Size Class Rules]]). Starting values to tune:

| Size   | Stagger multiplier |
| :----- | :----------------- |
| Small  | 1×                 |
| Medium | 0.4×               |
| Large  | 0.15×              |
| Huge   | 0×                 |

**MiniBoss and Boss ranks** skip the chance entirely. Their `EnemySO` gets a list of health percentages to stagger at instead.

**Being staggered:** calls `AbilityInterpreter.InterruptAbility()`, stops movement for a short time and plays a hit reaction. The [[Unit Brain]] treats the unit as busy until it ends. A short stagger immunity afterwards stops enemies from being stunlocked forever.

---
## Build Order
1. Fix the `Player.UseCurrentWeapon` / `ReleaseCurrentWeapon` check from [[Known Issues]], since melee attacks go through it, and add `IsBusy()` with the switching block (shared with ranged)
2. `SwingShapeSO` with its Scene view gizmo
3. `MeleeWeaponItem` and `ComboStepEntry`
4. `WeaponAttackMelee`: combo timing, sweep and hit list, damage. Test with a test sword on `PFB_Enemy_ChaseTest_Dev`
5. Height check, line of sight and closest point
6. Stagger: package field, `IStaggerable`, size class column, boss thresholds
7. Hook up animations
8. Later: step effects (finisher magic slashes), weapon skills, the animation baking tool

---
## Open Questions [[Notes for the future]]
- Should the swing's facing lock at the start of the swing (current plan) or keep following the aim?
- Can dashing cancel a swing? (Ranged: a dash cancels a charge but keeps warm up.) If it does, the weapon should still stay busy until the swing's time would have ended, so dashing can't be used to bypass attack speed Does the player slow down while swinging? If so, it can use the same move speed multiplier as ranged charging
- How does the weapon's [[Attack Speed]] (X/sec) map to combo timing? Total combo time, or a multiplier on each step?
- How long should stagger immunity last?
- Should enemies use this same system for their melee attacks? An ability function that runs one `ComboStepEntry` would let the ability overhaul reuse all of it.
- What should melee and ranged share after the ranged overhaul (`WeaponItem` fields, damage building, hit reactions)?
