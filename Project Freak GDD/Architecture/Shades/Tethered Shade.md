## Overview
How the [[Tethered]] form of a [[Shade]] works in code. The tethered shade is bound to the player by its tail and floats beside them. It glides in a straight line to its slot (passing through the player instead of circling around) and only turns to face things, never to travel.

Built Oct 2026 from the [[Shade Forms Plan]]. The released form is covered in [[Shade (Runtime)]], and spawning / swapping forms in [[Shade Manager]].

Back to [[Shade (Runtime)]] · [[Architecture Atlas]]

---
## Pieces

| Script | Lives on | Job |
| :--- | :--- | :--- |
| `TetheredShade` | `PFB_Shade_Tethered` root | Picks its slot, glides there, holds height over pits, faces the player's facing |
| `ShadeTail` | `PFB_Shade_Tethered` root (added automatically) | Placeholder tail drawn with a `LineRenderer` |
| `UnitHover` | `PFB_Shade_Tethered` root (added automatically) | Floats it over the floor, same script as the player |
| `ShadeArtRig` | Root of every shade art prefab | Tail and cast points, parts to hide per form |
| `IShadeForm` | Both shade prefabs (`ShadeInterface.cs`) | `Setup`, `Appear`, `Dismiss`, so the [[Shade Manager]] treats both forms the same |
| `ShadeFormType` | `ShadeFormType.cs` | Enums: `Form` (None, Tethered, Released) and `Slot` (Resting, Casting) |

The prefab has **no collider and no `UnitTeam`**, so it can't bump into anything and it never joins the `UnitRegistry` (enemies can't target it, see [[Unit Targeting]]). If it takes damage later, that's its own component on the prefab, not part of the movement code [[Notes for the future]]

---
## Prefab Setup
`PFB_Shade_Tethered` is an empty GameObject with `TetheredShade` on it. Adding `TetheredShade` adds `Rigidbody`, `UnitHover`, `ShadeTail` and `LineRenderer` automatically, and the rigidbody is set up in code (no gravity, rotation frozen, interpolated). New `UnitHover`s and `TetheredShade`s fill in their layer masks by themselves (see below), so there's nothing to fill out by hand.

Optional: an empty child for the art to spawn into (`_ArtHolder`). Without one the art spawns on the root.

The prefab was written by Claude without a `LineRenderer`, so `ShadeTail` adds one in `Awake` if it's missing. Until art exists, every evolution uses `PFB_Shade_Art_Placeholder_0` (a sphere with a `ShadeArtRig`, a tail point below it and a cast point in front) [[Notes for the future]]

---
## TetheredShade

**Settings**

| Variable | Default | Description |
| :--- | :--- | :--- |
| `_CatchUpTime` | 0.15 | Seconds to close the gap to its slot. Lower = snappier |
| `_MaxSpeed` | 20 | Fastest glide, m/s |
| `_TurnSpeed` | 540 | Degrees per second when turning to face |
| `_SnapDistance` | 15 | Farther than this from its slot (teleport, spawn point) and it snaps instead of gliding, meters |
| `_FallbackTetherDistance` | 2 | Used if the evolution has no `_TetherDistance` |

**Walls & Height** (foldout)

| Variable | Default | Description |
| :--- | :--- | :--- |
| `_WallLayers` | Everything but Units, Projectile, Item | What counts as a wall |
| `_WallCheckRadius` | 0.4 | Thickness of the wall check, roughly the body radius |
| `_WallPadding` | 0.2 | Gap kept from a wall |
| `_HeightCatchUpTime` | 0.2 | Seconds to reach the player's floor height over a pit |

**Runtime Data** (read only): `_SlotData`, `_EvolutionData`, `_RuntimeEntry`, `_CurrentSlot`, `_SlotPosition`. `_RuntimeEntry` is its slot's runtime entry on the [[Shade Manager]] (the same object, not a copy, shown with Odin's `ShowInInspector`), grabbed in `Setup` and readable with `GetRuntimeEntry()`. Nothing reads it yet [[Notes for the future]]

### Where it goes (slots)
- **Resting:** behind the player, the evolution's `_TetherDistance` away, opposite `CharacterMovement.GetLookDirection()`. The [[Player Movement|PlayerInputDriver]] already makes facing follow travel when not aiming and point at the aim while aiming, so this one rule covers "behind the direction of travel" and "opposite the aim target"
- **Casting:** in front of the player toward the aim. `SetSlot(ShadeFormType.Slot.Casting)` switches to it, but nothing calls it yet. It gets used once abilities can be cast from the tethered form [[Notes for the future]]

### How it moves (`FixedUpdate`)
```text
Work out the slot (direction × tether distance, pulled in by walls)
    ↓
Farther than _SnapDistance? → snap there and stop
    ↓
MoveTowardSlot: player's velocity + gap / _CatchUpTime (flat only, capped at _MaxSpeed)
    ↓
HoldHeight: only when there's no floor under it
    ↓
FaceForward: turn toward the player's facing
```

- Matching the player's velocity is what stops it trailing behind a running player
- **Wall check:** a `SphereCast` from the player toward the slot. If it hits, the slot is pulled in to the hit minus `_WallPadding`. In the casting slot the check also reaches as far as the art's `_CastPoint` sticks out, so the cast point isn't behind a wall either
- **Height:** over a floor, `UnitHover` floats it. Over a pit or ledge, `HoldHeight` moves it to the player's floor point + its own ride height. If the player is in the air too, it just holds its height. *Try it and see how it feels* [[Notes for the future]]

### Scenes
The tethered shade calls `DontDestroyOnLoad`, so it stays out between scenes like the player. After a scene loads it waits one frame (so spawn points have moved the player) and snaps to its slot.

### Art
`Setup` spawns the evolution's `_TetheredArt` into `_ArtHolder` and calls `ShadeArtRig.ApplyForm(Tethered)`. With no art or no rig it logs a warning and uses the shade's own position for the tail and cast points.

---
## ShadeTail (placeholder)
Draws an L with a `LineRenderer`: from the floor under the player (the player's `UnitHover.GetFloorHit()`), along the floor to under the body, then up to the rig's tail connection point. Hidden while the player isn't grounded.

| Variable | Default | Description |
| :--- | :--- | :--- |
| `_LineWidth` | 0.08 | Line thickness, meters |
| `_FloorOffset` | 0.02 | Lifts the flat part off the floor so it doesn't flicker |
| `_LineMaterial` | *(empty)* | Uses the built-in `Sprites/Default` shader if empty |

The real tail (a mesh along a curve, sized by `_TetherWidth`, with a shader that hides it while the player is in the air) waits for the art style [[Notes for the future]]

---
## ShadeArtRig
Goes on the root of every shade art prefab, so one FBX can serve both forms.

| Variable | Description |
| :--- | :--- |
| `_TailConnectionPoint` | Where the tail meets the body. Falls back to the art root |
| `_CastPoint` | Where abilities come from. Falls back to the art root |
| `_HideWhenTethered` | Child objects turned off while tethered (like legs) |
| `_HideWhenReleased` | Child objects turned off while released |

Selecting the art shows the tail point (cyan) and cast point (red) in the scene view, and the inspector warns if either point is empty.

---
## UnitHover additions
- `_FloorLayers`: which layers count as floor. Existing hovers keep the old "everything" mask. A newly added `UnitHover` leaves the **Units** layer off, so the shade's floor ray doesn't land on the player when it passes over them
- `GetFloorHit()`: where the floor ray landed (only up to date while `IsGrounded()`). Used for the tail's start point
- `GetRideHeight()`: how high it floats

---
## Related
- [[Shade Forms Plan]] - the full plan and what's left
- [[Tethered]] / [[Tether Shade]] (design)
- [[Shade Manager]] - `TetherShade()`, `ReturnShade()`
