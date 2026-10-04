## Overview
How ranged weapons fire in code. **Built Oct 2026** (build order steps 1-8): weapon switching rules, the data split, inspector warnings, the firing flow, pattern spawning, charge, finishers and hit scan. Not tested in Unity yet. [[Notes for the future]]

Design side: [[Weapons#Ranged]]. Melee counterpart: [[Melee Weapon System]]. Back to [[Architecture Atlas]].

This page covers how a weapon **fires**. How projectiles move and behave after they're spawned gets its own note later. [[Notes for the future]]

```text
WeaponRangedItem (WeaponItem)                      Create → Combat → Ranged Weapon
 ├── automatic, warm up, hit scan settings
 ├── charge settings
 ├── _Cycle          → FireCycleEntry (what one cycle fires)
 └── _FinisherCycle  → FireCycleEntry (fired every _FinisherEvery cycles, optional)

WeaponAttackRanged (ITriggerable, on the held weapon prefab)
 └── runs warm up → charge → cycle → repeat
```

**Scripts:** `Items/WeaponData/WeaponItem.cs`, `Items/WeaponData/WeaponRangedItem.cs` (also holds `FireCycleEntry`), `Combat/WeaponAttackRanged.cs`, `Combat/LaunchPackage.cs`, `Combat/ProjectileObject.cs`, `Combat/HitScanTracerObject.cs`. Rules shared with melee (allies, walls, body colliders, damage packages, activation shake) live in `Combat/CombatTools.cs` (see [[Weapon usage]]).

---
## Data

### WeaponItem
What every weapon shares, matching [[Weapons#Shared Traits]]. It's **abstract**, so assets are always made from a weapon type (`WeaponRangedItem` now, `MeleeWeaponItem` later). The ranged-only settings that used to live here moved down into `WeaponRangedItem`.

| Field              | Description |
| :----------------- | :---------- |
| `_AttackType`      | Physical / magical. Picks the wielder's attack stat |
| `_WeaponPrefab`    | The held weapon. Needs an `ITriggerable` script like `WeaponAttackRanged` |
| `_AttackSpeed`     | Attacks per second (2 = twice a second). `GetAttackTime()` gives seconds per attack |
| `_ActivationShake` | Camera shake each time the weapon fires (separate from the package's impact shake on hit). 0 = none |
| `_BaseDamage`      | Damage before the attack stat and modifiers |
| `_Element`         | `DamageType.ElementType` |
| `_Knockback`       | Meters, goes on the [[Damage Package]] |

*The old "weapon knockback" field (recoil on the wielder) was never used and was removed.*

### WeaponRangedItem

| Field            | Description |
| :--------------- | :---------- |
| `_IsAutomatic`   | Holding the trigger keeps firing cycles |
| `_WarmUpTime`    | Seconds of spin up before the first cycle. 0 = none |
| `_IsHitScan`     | Fires instant rays instead of projectiles (see [[#Hit Scan]]) |
| `_Cycle`         | The normal `FireCycleEntry` |
| `_FinisherEvery` | Every Nth cycle fires the finisher instead. 0 = no finisher |
| `_FinisherCycle` | The finisher's `FireCycleEntry` (only shown when `_FinisherEvery` > 0) |

**Charge** foldout (only shown when `_UsesCharge` is on):

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
| `_AimWidth`      | How close to the aim line (side to side) an enemy's body has to be to count as a target, meters (default 0.5, still to be tuned) |
| `_RayRadius`     | Thickness of the ray, meters (default 0.1). It's a sphere cast, so a little thickness makes shots easier to land |
| `_TracerEffect`  | Line effect from the muzzle to where the ray ends. Needs a `LineRenderer` + `HitScanTracerObject` (see [[#Setting Up a Hit Scan Weapon]]) |
| `_ImpactEffect`  | Spawned at each hit, facing out of the surface |
| `_ImpactLifeTime`| Seconds before an impact effect is removed, in case it doesn't clean itself up (default 2) |

### FireCycleEntry
What one cycle fires. The normal cycle and the finisher both use it, so a finisher can change anything: a different projectile, more of them, a wider spread or more damage. Lives at the bottom of `WeaponRangedItem.cs`.

| Field                 | Description |
| :-------------------- | :---------- |
| `_Projectile`         | Projectile prefab with a `ProjectileObject` (ignored by hit scan) |
| `_ProjectileSpeed`    | Meters per second. *Temporary here until projectiles get their own behaviors* [[Notes for the future]] |
| `_ProjectileLifeTime` | Seconds before a projectile that hit nothing removes itself. *Same as speed* [[Notes for the future]] |
| `_ProjectileCount`    | Projectiles (or rays) fired at the same time |
| `_BurstCount`         | Times the pattern fires per cycle. 1 = no burst |
| `_BurstSpeed`         | Burst shots per second (only shown when bursting) |
| `_FiringOrigin`       | Width of the line projectiles start along, meters |
| `_RandomOrigin`       | Random start points within the width instead of evenly spaced (only shown when the width > 0) |
| `_Spread`             | Total arc in degrees, centered on forward. Negative points inward |
| `_SpreadRandomness`   | ± degrees added to each projectile's angle |
| `_DamageMultiplier`   | × the weapon's damage (lets a finisher hit harder) |

### Inspector warnings
Shown as Odin info boxes on the asset, and logged once per play session with `Debug.LogWarning` the first time the weapon is set up (`WeaponRangedItem.LogSetupWarnings`):
- **Burst too long:** the burst takes as long as 1 / attack speed or longer, so attack speed no longer matters
- **Stacked projectiles:** projectile count > 1 with 0 origin and 0 spread. Only one projectile fires
- **No projectile:** the cycle has nothing to fire (skipped for hit scan, which doesn't use one)
- **Charge longer than attack time** (on the charge time field only): charge + burst is longer than 1 / attack speed, so the weapon really fires slower than its attack speed says

The same checks run on the finisher cycle when it's used.

---
## Timing

**Cycle time** = the longer of `1 / attack speed` and `charge time + burst time`.
**Burst time** = `(_BurstCount - 1) / _BurstSpeed`.

Warm up isn't part of the cycle. It happens once when the trigger is pressed.

### Firing Flow
All of this runs in one stored coroutine, `FireRoutine`, started by `TriggerAttack` (if it isn't already running).

```text
TriggerAttack (held = true)
    ↓
Warm up (if any, and not already warm). Let go → stop
    ↓
┌→ Charge (if any)
│     automatic: fires itself at max charge, let go early = cancel
│     not automatic: waits for ReleaseAttack, then fires (or cancels if it needs a full charge)
│       ↓
│  Pick the cycle (finisher if it's the Nth cycle)
│  Busy until: now + max(attack time - charge time used, burst time)
│  Build the Damage Package and Launch Package once
│     burst loop: spawn the pattern, wait 1 / burst speed
│       ↓
│  Wait out the rest of the cycle
│       ↓
└─ Automatic and still held? → next cycle
   Otherwise → idle
```

`ReleaseAttack` sets held to false, loses the warm up, flags the release for the charge loop, and resets the finisher count on automatic weapons.

Pressing a non-automatic weapon mid cycle is ignored (the coroutine is still running). Attack speed can never be bypassed (see [[#Weapon Switching]]).

Disabling or destroying the weapon (like switching weapons during warm up or charge) stops the coroutine and clears any slowdown.

### Charge
- Damage multiplier = charge % × `_MaxChargeMultiplier`. A ×2 weapon at half charge does ×1.
- **Automatic charged weapons** fire on their own at full charge. Letting go early cancels the shot.
- **Dash cancels the charge.** The weapon listens to the wielder's `UnitDash.startDashEvent` (added in `SetUpWeapon`, removed in `OnDestroy`). On a dash the charge starts over from 0 and nothing fires. **Warm up is kept** through dashes. *A chained dash doesn't fire the start event again, so only the first dash in a chain restarts the charge.*
- **Slowing the wielder:** `CharacterMovement.SetMoveSpeedMultiplier` when charging starts, `ClearMoveSpeedMultiplier` on fire, cancel, or the weapon being disabled. Since shades use the same `CharacterMovement` engine, it works on them too. Melee can use the same multiplier for swings (see [[Player Movement]]).

### Finisher Count
`_FinisherCount` counts cycles since the last finisher. When it reaches `_FinisherEvery`, that cycle uses `_FinisherCycle` and the count starts over. No timer:
- **Automatic:** `ReleaseAttack` resets the count
- **Not automatic:** the count carries over between trigger pulls
- **Switching weapons** resets it for free, since the old weapon is destroyed

---
## Weapon Switching
Attack speed can never be bypassed, including by swapping weapons mid cycle. `ITriggerable` has:

| Function   | Description |
| :--------- | :---------- |
| `IsBusy()` | True from the moment a weapon attacks until its attack time is over. Ranged: from firing until the cycle time ends (`Time.time < _busyUntil`). Melee: from the start of a swing until its recovery ends |

`Player.SelectWeapon(index)` won't switch while the held weapon is busy. Warm up and charging don't count as busy, since nothing has fired yet. Switching during them just cancels them.

`Player.UpdateCurrentWeapon()` also skips respawning if the hand already holds the selected item, so picking up a weapon into another slot doesn't reset the held weapon's timer.

### Switch Buffer
A switch pressed while the weapon is busy isn't thrown away. `Player` stores it in `_QueuedWeaponSelection` and a coroutine (`SwitchWhenFree`) switches the moment the weapon stops being busy.
- **No time limit.** The buffer holds until the weapon is free. Attack times run from a fraction of a second up to about 5 seconds (rocket launcher), so a short fixed window would drop presses on slow weapons and make switching feel broken on exactly the weapons where players want to switch most
- **The latest press wins.** Scrolling uses `GetSelectedWeaponIndex()` (the queued slot if there is one), so scrolling past several weapons while busy switches to the last one picked
- **Picking the current weapon again clears the buffer**, so a player can change their mind
- The HUD should show the queued weapon so the delay reads as intentional, not lag [[Notes for the future]]

---
## Spawning the Pattern
For each projectile `i` of `count`, using the wielder's flat forward and right. Projectiles start at the weapon's `_Muzzle` (a Transform on `WeaponAttackRanged`, falls back to the weapon itself) and fly level.

**Start point** (along the wielder's right, centered on the muzzle):
- width 0: the muzzle
- random: anywhere in `-width/2` to `width/2` (works with a single projectile too)
- count 1: the muzzle
- even: `-width/2 + width × i / (count - 1)`

**Angle** (around up, centered on forward):
- count 1: 0
- spread is a full circle (360 or more): step = `spread / count`, so the first and last don't overlap
- otherwise: step = `spread / (count - 1)`, so the ends land exactly on the edges
- angle = `-spread/2 + step × i`, then add `Random.Range(-randomness, randomness)` (randomness also works with a single projectile, like an inaccurate pistol)

*The old spread code always divided by `count - 1` and its random mode replaced the pattern instead of adding to it. Both were replaced by the rules above.*

---
## Damage and Projectiles
The [[Damage Package]] is built once when the cycle fires (per [[Weapons#Damage Package]]), so buffs gained after firing don't change shots already in the air. Every projectile in the cycle (burst shots included) carries the same package:
- One damage entry: `(base damage + attack stat) × the cycle's _DamageMultiplier × the charge multiplier`, following the formula in [[Damage]]. The attack stat is AGI or INT depending on attack type (`IsRange()` = true). The wielder's stats are read when the package is built, not when the weapon is equipped. *The damage overhaul changes this to `attack stat × weapon power × multipliers`, so `_BaseDamage` becomes a power multiplier, and the entry carries the attack stat (see [[Damage]]).* [[Notes for the future]]
- `_KnockbackDistance` = the weapon's `_Knockback`
- Crit is always ×1 for now [[Notes for the future]]

Each projectile also gets a `LaunchPackage` with information that isn't damage, for the projectile to use however it wants:

| Field           | Description |
| :-------------- | :---------- |
| `_Source`       | Who fired it |
| `_ChargePercent`| 0-1. Weapons that don't charge send 1 (full power). A projectile can grow, speed up or ignore it |
| `_IsFinisher`   | Whether this came from the finisher cycle |

Projectiles get both through `ProjectileObject.LaunchProjectile(DamagePackage, LaunchPackage, speed, lifeTime)`. Turning this into an `IProjectile` interface is decided when projectiles get their own note. [[Notes for the future]]

**Activation shake** only plays when the camera is following the wielder, so a shade firing off screen doesn't shake the player's camera.

---
## Hit Scan
**Built Oct 2026.** Hit scan is the only ranged attack that can aim up and down at enemies (ledges, stairs). Projectiles fly level. Each "projectile" in the cycle's pattern becomes one ray (`FireHitScanRay`) from its start point along its angle, so spread, firing origin and bursts all work the same way. Counts as **level geometry** anything that isn't a unit (`UnitTeam`) and can't be damaged (`IDamagable`). **Destructibles** are anything with `IDamagable` but no `UnitTeam`.

1. **Find targets** (`FindHitScanTarget`). Loops over the [[Unit Targeting|UnitRegistry]] (every active unit) instead of a box cast, following the registration-over-searching rule. Skips the wielder and allies (`UnitTeam.IsHostileTo`; a wielder with no team has no allies). Uses each unit's first solid (non-trigger) collider as its body. Keeps units that are:
   - In front and within `_FireDistance`
   - Within `_AimWidth / 2` plus the body's radius of the aim line, side to side, so big enemies count by their body
   - Within `_VerticalAimAngle` up or down from the muzzle, measured to the body's center
   - In line of sight: a raycast from the muzzle to the body's center hits no level geometry
2. **Pick the aim.** The target **closest to straight ahead** wins: the smallest up/down angle, with targets within `AimTieAngle` (5°, a constant in the script) of each other going to the closer one. So with an enemy in front and one on a floating platform, the shot goes to the one in front, even if the platform enemy is closer. No target → the shot stays level
3. **Fire along that line.** `Physics.SphereCastNonAlloc` (radius `_RayRadius`) from the start point toward the target's center (or level), up to `_FireDistance`. Triggers are ignored. The hits are sorted by distance and walked:
   - The wielder or an ally → skip (shots pass through)
   - Level geometry → impact effect, the shot stops here
   - A unit that can't take damage → skip
   - Anything with `IDamagable` → `TakeDamage`, impact effect. Each object only takes damage once per ray, even with several colliders
   - Only units count toward pierce. The shot stops when the count reaches `_HitPierce`. Destructibles don't count

   Piercing follows the tilted line, so a shot aimed down a staircase can pierce through a line of enemies on the stairs
4. **Tracer:** spawns `_TracerEffect` and draws its line from the muzzle to where the shot stopped (a wall, the last pierced unit, or full distance)

The [[Damage Package]] is built once per cycle at the moment of firing, which for hit scan is also the moment of the hit.

*"0 = infinite" fire distance uses `InfiniteFireDistance` (500 m) in code, since a cast needs a length.* A unit without a `UnitTeam` (like an old test dummy) won't be tilted toward, but still gets hit when it's on the level line.

**On `WeaponAttackRanged`** (the weapon prefab):

| Field            | Description |
| :--------------- | :---------- |
| `_HitScanLayers` | Layers rays and line of sight checks can hit (default everything). Triggers are always ignored |
| `_DrawDebugRays` | Draws each ray in the Scene view for half a second, for testing |

### HitScanTracerObject
`Combat/HitScanTracerObject.cs`. Goes on the tracer prefab with a `LineRenderer` (added automatically). The weapon calls `SetLine(start, end)` right after spawning it; it shrinks the line by `_WidthOverLife` over `_LifeTime` seconds (default 0.1), then removes itself.

### Setting Up a Hit Scan Weapon
1. Make a tracer prefab: empty GameObject → add `HitScanTracerObject` (adds a `LineRenderer`) → give the `LineRenderer` a material and width. Name it `PFB_Effect_Tracer_Name_0`
2. On the weapon asset: tick `_IsHitScan`, drag the tracer into `_TracerEffect`, optionally an impact effect into `_ImpactEffect`
3. The cycle's projectile fields are ignored. Spread, origin, count and burst still apply (a hit scan shotgun is just count > 1 with spread)

---
## Setting Up a Ranged Weapon
1. **Create → Combat → Ranged Weapon** (`SO_Weapon_Ranged_Name_0`)
2. Assign `_WeaponPrefab` (a held weapon prefab with `WeaponAttackRanged`) and the cycle's `_Projectile` (a prefab with `ProjectileObject`)
3. Optional: drag a child Transform into `WeaponAttackRanged._Muzzle` on the weapon prefab so shots start at the barrel
4. Fix anything the yellow info boxes complain about

`WeaponAttackRanged` has **Test Press Trigger** / **Test Release Trigger** buttons for trying a weapon in play mode without input.

### Oct 2026 asset migration
The existing weapon assets were moved to the new fields by hand:
- The old fire rate was **seconds between shots**, so it became attack speed = 1 / old value (0.4 → 2.5 per second)
- Charged weapons were set to `_RequiresFullCharge` on and a ×1 max multiplier, matching how they behaved before (they only ever fired at full charge)
- The rusty pistol's old random spread (45) became 0 spread with ±22.5 randomness
- Projectiles that had a lifetime of 0 (infinite) got 5 seconds so missed shots don't pile up

---
## Build Order
1. ~~Fix the `Player.UseCurrentWeapon` / `ReleaseCurrentWeapon` check, add `IsBusy()` with the switching block and switch buffer~~ *Done Oct 2026*
2. ~~Split `WeaponItem` / `WeaponRangedItem` and add `FireCycleEntry`. Move existing weapon assets over~~ *Done Oct 2026*
3. ~~Inspector warnings~~ *Done Oct 2026*
4. ~~Rewrite `WeaponAttackRanged` firing flow: warm up, automatic, burst, cycle time~~ *Done Oct 2026*
5. ~~Pattern spawning~~ *Done Oct 2026*
6. ~~Charge: multiplier, full charge rule, dash cancel, move slowdown~~ *Done Oct 2026*
7. ~~Finishers~~ *Done Oct 2026*
8. ~~Hit scan~~ *Done Oct 2026*
9. Later: projectile behaviors in their own note [[Notes for the future]]

---
## Open Questions [[Notes for the future]]
- Starting value for `_AimWidth` (0.5 for now) and whether `AimTieAngle` (5°) needs to be tunable
- Should the buffered switch also be cancelled by attacking again, or only by picking the current weapon? (Only picking the current weapon for now)
