## Overview
The plan for rebuilding ranged weapons in code. **Not built yet (Oct 2026).** The current `WeaponAttackRanged` works but is incomplete and buggy, so this is the design for its rewrite. [[Notes for the future]]

Design side: [[Weapons#Ranged]]. Melee counterpart: [[Melee Weapon System]]. Back to [[Architecture Atlas]].

This page covers how a weapon **fires**. How projectiles move and behave after they're spawned gets its own note later. [[Notes for the future]]

```text
WeaponRangedItem (WeaponItem)
 ├── automatic, warm up, hit scan settings
 ├── charge settings
 ├── _Cycle          → FireCycleEntry (what one cycle fires)
 └── _FinisherCycle  → FireCycleEntry (fired every _FinisherEvery cycles, optional)

WeaponAttackRanged (ITriggerable, on the held weapon)
 └── runs warm up → charge → cycle → repeat
```

---
## Data

### WeaponItem split
`WeaponItem` currently holds ranged-only settings (warm up, automatic, charged). These move down into `WeaponRangedItem` so melee weapons don't show them. `WeaponItem` keeps what every weapon shares, matching [[Weapons#Shared Traits]]: [[Notes for the future]]

| Stays on `WeaponItem` | Description |
| :-------------------- | :---------- |
| Attack type           | Physical / magical |
| Prefab                | The held weapon |
| Attack speed          | Attacks per second. The current fire rate field becomes this |
| Base damage, element  | |
| Knockback             | Meters, goes on the [[Damage Package]] |
| Activation shake      | Camera shake when the weapon fires (separate from the package's impact shake on hit) |

### WeaponRangedItem

| Field            | Description |
| :--------------- | :---------- |
| `_IsAutomatic`   | Holding the trigger keeps firing cycles |
| `_WarmUpTime`    | Seconds of spin up before the first cycle. 0 = none |
| `_IsHitScan`     | Fires rays instead of projectiles (see [[#Hit Scan]]) |
| `_Cycle`         | The normal `FireCycleEntry` |
| `_FinisherEvery` | Every Nth cycle fires the finisher instead. 0 = no finisher |
| `_FinisherCycle` | The finisher's `FireCycleEntry` |

**Charge** foldout:

| Field                  | Description |
| :--------------------- | :---------- |
| `_UsesCharge`          | This weapon charges its shots |
| `_ChargeTime`          | Seconds to reach max charge |
| `_MaxChargeMultiplier` | Damage multiplier at full charge |
| `_RequiresFullCharge`  | If on, releasing early doesn't fire |
| `_SlowWhileCharging`   | If on, the wielder moves slower while charging |
| `_ChargeMoveMultiplier`| Move speed while charging (0.5 = half speed) |

**Hit Scan** foldout (only shown when `_IsHitScan` is on):

| Field            | Description |
| :--------------- | :---------- |
| `_FireDistance`  | How far the ray goes, meters. 0 = infinite |
| `_HitPierce`     | How many enemies one ray can hit. Destructibles don't count |
| `_VerticalAimAngle` | How far up or down a shot can tilt to find a target, degrees (default 45) |
| `_AimWidth`      | How close to the aim line an enemy has to be to count as a target, meters. Small, so it only catches enemies actually on the line |
| `_TracerEffect`  | Line effect from the muzzle to where the ray ends |
| `_ImpactEffect`  | Spawned at each hit |

### FireCycleEntry
What one cycle fires. The normal cycle and the finisher both use it, so a finisher can change anything: a different projectile, more of them, a wider spread or more damage.

| Field               | Description |
| :------------------ | :---------- |
| `_Projectile`       | Projectile prefab (ignored by hit scan) |
| `_ProjectileCount`  | Projectiles (or rays) fired at the same time |
| `_BurstCount`       | Times the pattern fires per cycle. 1 = no burst |
| `_BurstSpeed`       | Burst shots per second |
| `_FiringOrigin`     | Width of the line projectiles start along, meters |
| `_RandomOrigin`     | Random start points within the width instead of evenly spaced |
| `_Spread`           | Total arc in degrees, centered on forward. Negative points inward |
| `_SpreadRandomness` | ± degrees added to each projectile's angle |
| `_DamageMultiplier` | × the weapon's base damage (lets a finisher hit harder) |

### Inspector warnings
Shown as Odin info boxes on the asset, and logged once with `Debug.LogWarning` when the weapon is set up:
- **Burst too long:** the burst takes as long as 1 / attack speed or longer, so attack speed no longer matters. *"Warning! Burst on {weapon} is longer than its attack speed. Set it up as automatic instead."*
- **Stacked projectiles:** projectile count > 1 with 0 origin and 0 spread. Only one projectile fires.

---
## Timing

**Cycle time** = the longer of `1 / attack speed` and `charge time + burst time`.
**Burst time** = `(_BurstCount - 1) / _BurstSpeed`.

Warm up isn't part of the cycle. It happens once when the trigger is pressed.

### Firing Flow
```text
TriggerAttack (held = true)
    ↓
Warm up (if any, and not already warm)
    ↓
┌→ Charge (if any)
│     automatic: fires itself at max charge
│     not automatic: waits for ReleaseAttack
│       ↓
│  Fire the cycle (finisher if it's the Nth cycle)
│     burst loop: spawn the pattern, wait 1 / burst speed
│       ↓
│  Wait out the rest of the cycle time
│       ↓
└─ Automatic and still held? → next cycle
   Otherwise → idle
```

`ReleaseAttack` sets held to false, loses the warm up, and fires a charging non-automatic weapon (or cancels it if it needs a full charge and isn't there yet).

Pressing a non-automatic weapon mid-cycle is ignored. Attack speed can never be bypassed (see [[#Weapon Switching]]).

### Charge
- Damage multiplier = charge % × `_MaxChargeMultiplier`. A ×2 weapon at half charge does ×1.
- **Dash cancels the charge.** The weapon subscribes to the wielder's `UnitDash.startDashEvent` in `SetUpWeapon` (and unsubscribes when destroyed). On a dash, the charge is dropped and nothing fires. **Warm up is kept** through dashes.
- **Slowing the wielder** needs a move speed multiplier on `CharacterMovement`, which doesn't exist yet (see [[Player Movement]]). Set it when charging starts, clear it on fire, cancel, weapon swap or disable. Since shades use the same `CharacterMovement` engine, the slowdown works on them too, whether the player or the AI is driving. Melee can use the same multiplier for swings. [[Notes for the future]]

### Finisher Count
A runtime counter of cycles fired. When it reaches `_FinisherEvery`, that cycle uses `_FinisherCycle` and the count starts over. No timer:
- **Automatic:** `ReleaseAttack` resets the count
- **Not automatic:** the count carries over between trigger pulls
- **Switching weapons** resets it for free, since `Player.UpdateCurrentWeapon` destroys the old weapon

---
## Weapon Switching
Attack speed can never be bypassed, including by swapping weapons mid-cycle. `ITriggerable` gets a new function: [[Notes for the future]]

| Function   | Description |
| :--------- | :---------- |
| `IsBusy()` | True from the moment a weapon attacks until its attack time is over. Ranged: from firing until the cycle time ends. Melee: from the start of a swing until its recovery ends |

`Player.UpdateCurrentWeapon` won't switch while the held weapon is busy. Warm up and charging don't count as busy, since nothing has fired yet. Switching during them just cancels them.

### Switch Buffer
A switch pressed while the weapon is busy isn't thrown away. The player remembers which weapon was picked and switches the moment the weapon stops being busy.
- **No time limit.** The buffer holds until the weapon is free. Attack times run from a fraction of a second up to about 5 seconds (rocket launcher), so a short fixed window would drop presses on slow weapons and make switching feel broken on exactly the weapons where players want to switch most
- **The latest press wins.** Scrolling past several weapons while busy switches to the last one picked
- **Picking the current weapon again clears the buffer**, so a player can change their mind
- The HUD should show the queued weapon so the delay reads as intentional, not lag [[Notes for the future]]

---
## Spawning the Pattern
For each projectile `i` of `count`, using the wielder's forward and right:

**Start point** (along the wielder's right, centered on the muzzle):
- count 1: the muzzle
- even: `-width/2 + width × i / (count - 1)`
- random: anywhere in `-width/2` to `width/2`

**Angle** (around up, centered on forward):
- count 1: 0
- spread is a full circle (360 or more): step = `spread / count`, so the first and last don't overlap
- otherwise: step = `spread / (count - 1)`, so the ends land exactly on the edges
- angle = `-spread/2 + step × i`, then add `Random.Range(-randomness, randomness)`

*The current weapon already has spread code. Keep it and check it against the full-circle rule.*

---
## Damage and Projectiles
The [[Damage Package]] is built when the cycle fires (per [[Weapons#Damage Package]]), so buffs gained after firing don't change shots already in the air:
- Damage entries: base damage × the cycle's `_DamageMultiplier` × the charge multiplier, with AGI or INT depending on attack type (`IsRange()` = true)
- Knockback and crit as usual

Each projectile also gets a small **launch package** with information that isn't damage, for the projectile to use however it wants: [[Notes for the future]]

| Field           | Description |
| :-------------- | :---------- |
| `_Source`       | Who fired it |
| `_ChargePercent`| 0-1. A projectile can grow, speed up or ignore it |
| `_IsFinisher`   | Whether this came from the finisher cycle |

Projectiles get both through one call (for example `LaunchProjectile(DamagePackage, LaunchPackage)` on an `IProjectile` interface), decided when projectiles get their own note.

---
## Hit Scan
Hit scan is the only ranged attack that can aim up and down at enemies (ledges, stairs). Projectiles fly level. Each "projectile" in the pattern becomes one shot along its angle:
1. **Find targets.** `Physics.BoxCastNonAlloc` a thin, tall box from the start point along the shot's direction, up to `_FireDistance`, on the units and destructibles layers. The box is `_AimWidth` wide and tall enough to cover `_VerticalAimAngle` at that distance (capped at a sensible height, since floors don't stack forever). Skip the wielder and allies (`UnitTeam`), then keep only targets that are:
   - Within `_VerticalAimAngle` up or down from the muzzle (measured to the target's center)
   - In line of sight: linecast from the muzzle to the target's center against level geometry is clear
2. **Pick the aim.** The target **closest to straight ahead** wins: the smallest up/down angle, with distance breaking near ties (within a few degrees). So with an enemy in front and one on a floating platform, the shot goes to the one in front, even if the platform enemy is closer. No target → the shot stays level
3. **Fire along that line.** `Physics.SphereCastNonAlloc` (small radius) from the muzzle toward the chosen target's center, continuing to `_FireDistance`. Sort the hits by distance and walk them:
   - Level geometry → the shot stops here
   - The wielder or an ally → skip
   - Destructible → damage it, doesn't count toward pierce
   - Unit with `IDamagable` → damage it, count it. Stop when the count reaches `_HitPierce`

   Piercing follows the tilted line, so a shot aimed down a staircase can pierce through a line of enemies on the stairs
4. **Effects:** an impact effect at each hit. The tracer goes from the muzzle along the shot's line to the last thing hit, or to where the shot stopped

The package is built at the moment of firing, which for hit scan is also the moment of the hit.

*"0 = infinite" uses a large fixed distance in code, since a cast needs a length.*

---
## Build Order
1. Fix the `Player.UseCurrentWeapon` / `ReleaseCurrentWeapon` check from [[Known Issues]], and add `IsBusy()` with the switching block and switch buffer (all shared with melee)
2. Split `WeaponItem` / `WeaponRangedItem` and add `FireCycleEntry`. Move existing weapon assets over
3. Inspector warnings
4. Rewrite `WeaponAttackRanged` firing flow: warm up, automatic, burst, cycle time
5. Pattern spawning (reuse the existing spread code)
6. Charge: multiplier, full charge rule, dash cancel, move slowdown
7. Finishers
8. Hit scan
9. Later: projectile behaviors in their own note

---
## Open Questions [[Notes for the future]]
- Starting value for `_AimWidth`
- Should the buffered switch also be cancelled by attacking again, or only by picking the current weapon?
