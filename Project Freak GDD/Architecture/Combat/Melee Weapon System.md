## Overview
How melee weapons work in code. **Built Oct 2026** (build order steps 1-6): swing shapes with a gizmo, combo data, the combo flow, the sweeping hit check (height, walls, big bodies), damage and stagger. Animations are next. [[Notes for the future]]

Design side: [[Weapons#Melee]]. Ranged counterpart: [[Ranged Weapon System]]. Back to [[Architecture Atlas]].

```text
MeleeWeaponItem (WeaponItem)                         Create → Combat → Melee Weapon
 ├── _Reach, _ComboResetTime (+ attack speed, damage, knockback from WeaponItem)
 └── _Combo
       ├── ComboStepEntry 0 → SwingShapeSO (Slash) + timing, damage, knockback
       ├── ComboStepEntry 1 → SwingShapeSO (Backslash)
       └── ComboStepEntry 2 → SwingShapeSO (Thrust)   ← finisher

WeaponAttackMelee (ITriggerable, on the held weapon prefab)
 └── runs the combo and does the hit checks
```

**Scripts:** `Combat/Melee/SwingShapeSO.cs`, `Combat/Melee/MeleeType.cs` (the `SweepMode` enum), `Items/WeaponData/MeleeWeaponItem.cs` (also holds `ComboStepEntry`), `Combat/WeaponAttackMelee.cs`, `Combat/CombatTools.cs` (shared with ranged), `Dev Scripts/SwingShapePreviewObject.cs`.

### Decisions (Oct 2026)
- **Facing locks at the start of a swing.** The swing aims where the wielder was aiming when it started, and the wielder's body holds that facing until the active part ends. They can turn again during recovery
- **Dashing cancels a swing**, and resets the combo to hit 1, but the weapon **stays busy** until the swing would have ended, so dashing can't be used to bypass attack speed or switch weapons faster
- **Attack speed sets the average pace.** A full combo takes `hits ÷ attack speed` seconds. Step timings are relative, so a slow finisher stays slower than the other hits, and attack speed stays the real, readable number
- **Players can slow down while swinging**, per hit (`_MoveMultiplier`), using the same move speed multiplier as ranged charging

---
## Why a Swept Shape
Two other ways were considered:
- **Trigger collider on the weapon:** physics only checks overlaps at fixed steps, while the animation moves the weapon every frame. A fast swing can jump right past an enemy between two checks and never touch it.
- **Tracing points on the blade:** reliable, but every animation has to be set up and tuned, and it's more precision than the isometric camera needs.

Instead, each swing is a **shape defined as data** (an arc with a reach and height range) checked with physics queries. The shape grows across the swing's active time, so hits still happen in order (left side of a slash first). Because the check covers the whole angle swept so far, not where the weapon is this frame, a fast swing or a frame hitch can't skip anyone.

---
## SwingShapeSO
One asset per swing type, shared by every weapon. **Create → Combat → Swing Shape** (`SO_Swing_Name`). The starter set lives in `Scriptable Objects/Weapons/Melee/Swing Shapes`: Slash, Backslash, Overhead, UpStrike, Thrust, Spin, Slam.

| Field           | Description |
| :-------------- | :---------- |
| `_SweepMode`    | `Angle`: the arc grows sideways from start to end angle (slashes, spins). `Reach`: the arc grows outward from the wielder (thrusts). `Instant`: the whole shape hits at once (slams, chops) |
| `_StartAngle`   | Degrees from the wielder's forward where the swing starts. Negative = left |
| `_EndAngle`     | Degrees where the swing ends. A backslash goes right to left (80 → -80), a spin is -180 to 180 |
| `_SweepCurve`   | 0-1 curve for how the sweep moves over the active time, so a swing can start slow and whip through |
| `_Reach`        | Meters from the wielder's center at 1× weapon reach |
| `_InnerRadius`  | Meters. Usually 0. Lets a shape skip things right on top of the wielder |
| `_HeightBelow`  | How far below the **bottom of the wielder's body** the swing reaches, meters |
| `_HeightAbove`  | How far above the bottom of the wielder's body the swing reaches, meters |

*Heights are measured from the bottom of the wielder's solid collider instead of its pivot, because the player's pivot is in the middle of its body while enemies' pivots are at their feet.*

The shape math (`IsInsideShape`, `GetCurrentAngles`, `GetCurrentReach`) and the gizmo drawing (`DrawGizmo`) live on the asset, so the hit check and the preview always agree.

**Starter shapes** (all tunable):

| Shape     | Mode    | Angles      | Reach | Below / Above |
| :-------- | :------ | :---------- | :---- | :------------ |
| Slash     | Angle   | -80 → 80    | 2     | 0.75 / 2.5    |
| Backslash | Angle   | 80 → -80    | 2     | 0.75 / 2.5    |
| Overhead  | Instant | -20 → 20    | 2.4   | 1.25 / 3.25   |
| UpStrike  | Instant | -20 → 20    | 2     | 0.5 / 3.75    |
| Thrust    | Reach   | -10 → 10    | 3     | 0.5 / 2.25    |
| Spin      | Angle   | -180 → 180  | 1.6   | 0.75 / 2.5    |
| Slam      | Instant | -45 → 45    | 1.8   | 1.25 / 1.5    |

### Previewing shapes
- **In edit mode:** add `SwingShapePreviewObject` (Dev Scripts) to an empty object standing on the floor. Its position is the wielder's feet and its forward is the aim. Pick a weapon and a combo step (or a single shape), drag `_SweepTime` to watch the sweep grow, or tick `_ShowWholeCombo` to see every hit at once
- **In play mode:** `WeaponAttackMelee._DrawSwingGizmos` draws each swing while it's active (faint = whole shape, bright = swept so far)

---
## MeleeWeaponItem
Inherits `WeaponItem` (see [[Ranged Weapon System#WeaponItem]]): attack type, prefab, attack speed, activation shake, base damage, element and knockback. **Create → Combat → Melee Weapon** (`SO_Weapon_Melee_Name_0`).

| Field             | Description |
| :---------------- | :---------- |
| `_Reach`          | Multiplier on every swing shape's reach. A dagger might be 0.7, a greatsword 1.5 |
| `_ComboResetTime` | Seconds without attacking before the combo goes back to hit 1 |
| `_Combo`          | The ordered list of `ComboStepEntry`s. The last one is the finisher |

The inspector shows **Full Combo Seconds** (hits ÷ attack speed) and warns about an empty combo or a hit with no swing shape (also logged once when the weapon is set up).

### ComboStepEntry
One hit in the combo.

| Field                   | Description |
| :---------------------- | :---------- |
| `_Swing`                | The `SwingShapeSO` this hit uses |
| `_Windup`               | Relative time before the swing can hit |
| `_ActiveTime`           | Relative time the swing sweeps and can hit |
| `_Recovery`             | Relative time after the swing before the next hit can start |
| `_DamageMultiplier`     | × the weapon's damage |
| `_KnockbackMultiplier`  | × the weapon's knockback |
| `_StaggerPower`         | 0-1. How likely this hit is to stagger, before the enemy's size class (see [[#Stagger]]). Default 0.5. Finishers usually get the most |
| `_MoveMultiplier`       | Wielder's move speed from wind up to the end of recovery. 1 = normal |
| `_Animation`            | Which attack animation to play. *Not added yet* [[Notes for the future]] |
| `_StepEffect`           | Optional extra effect when the hit happens, like a magic slash projectile on a finisher. Added later [[Notes for the future]] |

**Timing:** every timing is multiplied by `MeleeWeaponItem.GetTimeScale()` = (hits ÷ attack speed) ÷ (sum of every step's wind up + active + recovery). Example: the radiant sword's steps add up to 1.75, it has 3 hits at 1.5 attacks per second, so the combo takes 2 seconds and every timing is ×1.14.

---
## WeaponAttackMelee
The melee version of `WeaponAttackRanged`. Lives on the weapon prefab and implements `ITriggerable`.

| Function           | What it does |
| :----------------- | :--- |
| `SetUpWeapon(...)` | Caches the item, the wielder, its stats, `UnitTeam`, `CharacterMovement`, body collider, and listens to its `UnitDash` |
| `TriggerAttack()`  | If idle, starts the combo. If a swing is going, buffers the press so the next hit starts right after recovery |
| `ReleaseAttack()`  | Clears the held flag. Holding the button keeps the combo going |
| `IsRange()`        | `false`, so melee uses the primary attack stat ([[STR]] for physical) |
| `IsBusy()`         | True from the start of a swing until its recovery ends, even if a dash cancelled it (see [[Ranged Weapon System#Weapon Switching]]) |

| Field              | Description |
| :----------------- | :---------- |
| `_HitLayers`       | Layers swings and their wall checks can hit (default everything). Triggers are always ignored |
| `_DrawSwingGizmos` | Draws active swings in the Scene view |

Runtime readouts: `_Wielder`, `_WeaponData`, `_ComboStep`, `_IsTriggerHeld`, `_IsAttackBuffered`. **Test Press Attack / Test Release Attack** buttons work in play mode.

### Combo Flow
Runs in one stored coroutine (`ComboRoutine`):

```text
TriggerAttack
    ↓
Wait while busy (a dash-cancelled swing still runs out its time)
    ↓
Pause longer than _ComboResetTime? → back to hit 1
    ↓
Busy until: now + wind up + active + recovery (scaled)
Lock facing to the aim, slow the wielder, clear the hit list, activation shake
    ↓
Wind up
    ↓
Active   → sweep + hit check every frame (Instant shapes: one check at the start)
           one last check at full sweep, then facing unlocks
    ↓
Recovery → speed back to normal, move to the next hit (after the finisher, back to hit 1)
    ↓
Buffered or held? → next hit
Otherwise         → idle
```

Weapon swaps or disabling stop it and give the wielder back their facing and speed. A **dash** stops it, resets the combo to hit 1, and if attack is still held, starts again once the cancelled swing's busy time is over.

**Facing:** `CharacterMovement.LockTurning(direction)` snaps the body to the aim (`GetLookDirection()`, which can be ahead of where the body faces mid turn) and holds it; `UnlockTurning()` lets it turn to the latest aim. It's separate from `SetTurning` (used by the [[Radial Menu]]), so the two never undo each other. Units without `CharacterMovement` (enemies) swing along their current forward without locking.

### Hit Check (each frame of the active time)
1. Work out how far the sweep has gone: `_SweepCurve` at elapsed / active time, turned into an angle (or a reach for `Reach` mode).
2. `Physics.OverlapSphereNonAlloc` around the wielder's base point (center, at the bottom of its body), big enough to cover the shape's reach and height.
3. For each collider:
   - Skip it if it's the wielder, an ally (`UnitTeam`), has no `IDamagable` (in it or a parent), or is already in this swing's hit list
   - Get the closest point on its collider to the middle of the wielder's body (`Collider.ClosestPoint`), so big enemies are hit by their body. Non-convex mesh colliders use their bounding box instead
   - **Inside the shape** (`SwingShapeSO.IsInsideShape`): height within `_HeightBelow` / `_HeightAbove` of the wielder's base, flat distance between inner radius and the current reach, and angle from the locked forward inside the part swept so far
   - **Line of sight:** a raycast from the middle of the wielder's body to the point hits no level geometry. Blocked means no hit
4. Passed everything: add it to the hit list, build the [[Damage Package]], call `TakeDamage`.

Each target is hit once per swing (keyed by the object with `IDamagable`, so several colliders don't count twice). Destructibles (`IDamagable` with no `UnitTeam`) get hit like anything else.

The timing data is the source of truth for when a swing hits, not the animation. Animations should be made to match, and animation events are only for effects and sounds. If hand matching gets tedious, an editor tool could later read a swing animation and fill in the timing and angles automatically. [[Notes for the future]]

---
## Damage
Per [[Weapons#Damage Package]], melee builds its package at the moment of the hit, using the wielder's current stats (`CombatTools.BuildWeaponDamagePackage`, shared with ranged):
- One damage entry: `(base damage + attack stat) × the step's _DamageMultiplier`, with the wielder's primary stat (STR for physical, INT for magical)
- `_KnockbackDistance`: weapon knockback × the step's `_KnockbackMultiplier`. Enemies are pushed away from the wielder
- Crit is always ×1 for now (see the damage overhaul in [[Damage Receivers & Projectiles]])

- `_StaggerPower`: the step's `_StaggerPower` (see [[#Stagger]])

Still planned for the [[Damage Package]]: `_HitDirection`, an optional direction for knockback, so a finisher can push enemies along the swing instead of straight away from the wielder. [[Notes for the future]]

---
## Stagger
**Built Oct 2026.** A stagger interrupts what an enemy is doing and stuns it briefly. Knockback is separate and still comes from the weapon.

**How a hit gets there:** melee sets the [[Damage Package]]'s `_StaggerPower` from the combo step. `EnemyDamagable` calls `IStaggerable.TakeStagger(package)` on **every** hit, after damage and knockback (so a boss sees its health after the hit). Ranged weapons and traps leave the power at 0 for now. [[Notes for the future]]

### EnemyStagger
`Scripts/Enemies/EnemyStagger.cs`, on enemies next to `EnemyMovement` (already on `PFB_Enemy_ChaseTest_Dev`). Reads its settings from the EnemySO's **Stagger** foldout (see [[Enemy Movement#EnemySO]]).

**Regular enemies (Popcorn to Lieutenant):**
- Skipped if the EnemySO is `_StaggerImmune`, the hit has no stagger power, or it's still staggered or immune from the last one
- **Chance** = the hit's `_StaggerPower` × the size class's `_StaggerMultiplier` (on the `SizeClassRulesSO`, see [[Enemy Movement#Size Class Rules]]):

| Size   | Stagger multiplier | Sword slash (0.6) | Sword thrust finisher (1.0) |
| :----- | :----------------- | :---------------- | :-------------------------- |
| Small  | 1×                 | 60%               | 100%                        |
| Medium | 0.4×               | 24%               | 40%                         |
| Large  | 0.15×              | 9%                | 15%                         |
| Huge   | 0×                 | never             | never                       |

**MiniBoss and Boss ranks** skip the chance entirely. They stagger once each time their health drops past one of `_BossStaggerThresholds` (66% and 33% to start). The thresholds are sorted highest first; a big hit that passes several at once staggers once and uses them all up. Boss staggers ignore immunity, since they're designed moments in the fight. Needs a health script (`IUnitHealth`, like `EnemyStats`); it warns if there isn't one.

**Being staggered:**
1. `AbilityInterpreter.InterruptAbility()`, if the enemy has one
2. `EnemyMovement.Stun(_StaggerDuration)`: it stops where it stands. `IsActive()` is false while stunned, so the [[Unit Brain]] pauses (the same way it does during knockback) and picks a fresh action afterwards
3. The `_OnStaggered` UnityEvent fires. **Hook the hit reaction up here** (flash, sound, animation) until animations are built [[Notes for the future]]
4. Regular hits can't stagger it again until `_StaggerImmunityTime` (1 second to start) after the stagger ends, so enemies can't be stunlocked forever

**Test Stagger** (Odin button, play mode) staggers the enemy right away. Runtime readouts: `_IsStaggered`, `_IsStaggerImmune`, `_ThresholdsUsed`.

---
## Setting Up a Melee Weapon
1. Make a **Melee Weapon** asset and fill in the `WeaponItem` fields (`_WeaponPrefab`, attack speed, damage...)
2. Add combo steps and drag a swing shape into each. Tune the relative timings; check **Full Combo Seconds**
3. The weapon prefab needs `WeaponAttackMelee` on it
4. Preview the shapes with `SwingShapePreviewObject`, then test in play mode with `_DrawSwingGizmos` on

### Oct 2026: the radiant sword
`PFB_Weapon_Sword_Radiant_0` now has `WeaponAttackMelee` (it used to have the ranged script). Its asset (`REMAKE_WHEN_I_MAKE_MELEE_SO_Sword_Radiant_0`, rename it in Unity) was turned into a Melee Weapon: 1.5 attacks per second, Slash → Backslash → Thrust (finisher ×1.5 damage, full knockback), 0.6 move speed while swinging, knockback 3, stagger power 0.6 / 0.6 / 1. It's still set to **magical** attack type and Light element from before; switch it to physical if it should use STR.

---
## Build Order
1. ~~Fix the `Player.UseCurrentWeapon` / `ReleaseCurrentWeapon` check and add `IsBusy()` with the switching block (shared with ranged)~~ *Done Oct 2026*
2. ~~`SwingShapeSO` with its Scene view gizmo~~ *Done Oct 2026*
3. ~~`MeleeWeaponItem` and `ComboStepEntry`~~ *Done Oct 2026*
4. ~~`WeaponAttackMelee`: combo timing, sweep and hit list, damage~~ *Done Oct 2026*. Test with the radiant sword on `PFB_Enemy_ChaseTest_Dev`
5. ~~Height check, line of sight and closest point~~ *Done Oct 2026*
6. ~~Stagger: package field, `IStaggerable`, size class column, boss thresholds~~ *Done Oct 2026*
7. Hook up animations [[Notes for the future]]
8. Later: step effects (finisher magic slashes), weapon skills, the animation baking tool [[Notes for the future]]

---
## Open Questions [[Notes for the future]]
- How long should stagger immunity last? (1 second for now, on each EnemySO)
- Should ranged weapons be able to stagger? `FireCycleEntry` would need a `_StaggerPower` (a heavy finisher round or a rocket could)
- Should enemies use this same system for their melee attacks? An ability function that runs one `ComboStepEntry` would let the ability overhaul reuse all of it.
- What else should melee and ranged share? So far: `WeaponItem` fields, `CombatTools` (allies, walls, body colliders, damage packages, activation shake) and the busy/switching rules.
