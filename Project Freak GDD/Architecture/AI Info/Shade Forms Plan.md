## Overview
The plan for how a [[Shade]] is brought out in its different forms (**Tethered**, **Released**, **Controlled**, **Fused**), starting with the **Tethered** form. Worked out with Claude in Oct 2026. Built pieces are written up in their system notes ([[Tethered Shade]], [[Shade (Runtime)]], [[Shade Manager]], [[Ability System]]) and their tags are off here.

Back to [[AA - AI Info]] · [[Architecture Atlas]]

**Status (Oct 2026):** the code for build order steps 1-9 is written. The Unity side (file renames, the new prefab and ability assets) is in **Unity Setup** below, then it needs a playtest. [[Notes for the future]]

The design side lives in [[Shade]] (Summoning Styles), [[Tethered]], and the ability notes ([[Tether Shade]], [[Release Shade]], [[Return Shade]], [[Control Shade]], [[Fuse Shade]]).

---
## Naming
Every form has a matching ability: the ability is the verb, the form is what the shade is after using it. Use these names everywhere (GDD, code, assets).

| Form | Ability | Meaning |
| :--- | :--- | :--- |
| **Tethered** | [[Tether Shade]] | Bound to Hazen by the tail, floats beside him |
| **Released** | [[Release Shade]] | Off the tether, a full unit in the level |
| **Controlled** | [[Control Shade]] | Hazen is driving the released shade |
| **Fused** | [[Fuse Shade]] | Hazen and the shade are combined |
| *(none out)* | [[Return Shade]] | Puts away whatever form is out |

**Summon** is the umbrella word for bringing the shade out in any form ("summoning", `CanSummon()`, `ISummonUnit`). It is never the name of one form or one ability.

| Thing | Tethered | Released |
| :--- | :--- | :--- |
| Prefab | `PFB_Shade_Tethered` | `PFB_Shade_Released` (renamed from `PFB_Shade`) |
| Component | `TetheredShade` | `ReleasedShade` (renamed from `Shade`) |
| Ability function | `TetherShade` | `ReleaseShade` (renamed from `SummonShade`) |
| Ability asset | `SO_Ability_TetherShade` | `SO_Ability_ReleaseShade` (renamed from `SO_Ability_SummonShade`) |

*Rename scripts inside Unity (or move the `.meta` file with them) so prefabs keep their references.*

---
## The Forms

| Form | Prefab | Status |
| :--- | :--- | :--- |
| **Tethered** | `PFB_Shade_Tethered` (new) | Code built (see [[Tethered Shade]]). Prefab still to be made, see Unity Setup [[Notes for the future]] |
| **Released** | `PFB_Shade_Released` (the current `PFB_Shade`) | Code renamed, uses the tether/release/return flow and can spawn art from its evolution. Prefab rename and art holder still to do, see Unity Setup [[Notes for the future]] |
| **Controlled** | `PFB_Shade_Released` | Exists ([[Control Shade]] through the [[Shade Manager]]). Only works on the released shade |
| **Fused** | Undecided, maybe a third prefab | Later [[Notes for the future]] |

---
## Key Decisions

### 1. Two prefabs, not one shade with a state machine
Tethered and released work in very different ways, so they're separate prefabs instead of one prefab that turns scripts on and off per form. Each prefab only has the scripts it needs, which keeps them easy to read. Shades can't be dragged and dropped like enemy prefabs anyway, since they depend on slot data, evolution and rune fields, so nothing is lost by splitting them.

- Every shade uses the **same two prefabs**. What makes one shade different from another is all **data** (its `ShadeSO` slot and `ShadeEvolutionSO`)
- The [[Shade Manager]] handles the **handoff**: it spawns the new form, gives it the slot data, and removes the old one
- The tethered shade moves itself to its slot instead of using `CharacterMovement` + a driver. The "switch drivers, never the engine" rule from [[AI Movement & Dungeon Loading Plan]] still applies to the released shade

### 2. Only one shade is ever out
Having more than one [[Shade Slot]] is for when a shade dies, or for switching to a better fit for the current dungeon (through a menu that isn't built yet). For now everything uses the currently selected slot. **Switching slots while a shade is out** is a later problem [[Notes for the future]]

### 3. The player always chooses to summon
Shades never come out on their own. Summoning a new evolution for the first time should feel like a moment, and a shade with no health or [[LIFE]] left shouldn't be summonable at all. That rule lives in **one** check, `ShadeManager.CanSummon()`, which only checks the player and slot exist for now. Health and lives plug into it later without the abilities needing to know [[Notes for the future]]

### 4. Scene rules
- **Tethered** shade persists between scenes (`DontDestroyOnLoad`, like the player) and snaps to the player after a scene loads. If it's tethered in the hub, it's still tethered when the player enters a dungeon
- **Released** shade does **not** persist. Entering a dungeon (`DungeonManager.EnterDungeon`) returns it, and it doesn't follow the player to the next floor. If the shade went on a rampage, the player can choose not to release it again on the next floor

### 5. Tethered shade can't be hit (for now)
`PFB_Shade_Tethered` has no `UnitTeam`, so it never joins the `UnitRegistry` and enemies can't target it ([[Unit Targeting]]). It also has no collider. If the tethered shade takes damage later, that's added as its own component on the prefab, not built into its movement code [[Notes for the future]]

---
## Abilities
Three [[Ability System|abilities]], so the player never has to think about what form the shade is in:

| Shade out now | **Tether Shade** | **Release Shade** | **Return Shade** |
| :--- | :--- | :--- | :--- |
| None | Tethered appears | Released appears | Nothing |
| Tethered | Tethered goes away | Tethered goes away, released appears | Tethered goes away |
| Released | Released goes away, tethered appears | Released goes away | Released goes away |

**Return Shade** always just brings the shade back, whatever form it's in. Good for hiding the shade in the middle of a fight without having to work out where it is.

| Ability function | Asset | Status |
| :--- | :--- | :--- |
| `TetherShade` | `SO_Ability_TetherShade` | Function built. Asset still to be made [[Notes for the future]] |
| `ReleaseShade` | `SO_Ability_ReleaseShade` | Function renamed from `SummonShade` and follows the table above. Asset still to be renamed [[Notes for the future]] |
| `ReturnShade` | `SO_Ability_ReturnShade` | Function built. Still to be added to the asset [[Notes for the future]] |

The new abilities need to be added to the player's `_TamerAbilities` list (see [[Stats & Inventory Data]]) so they show in the [[Radial Menu]].

---
## Pieces to Build

### Shade Manager *(built)*
| Change | Description |
| :--- | :--- |
| `_TetheredPrefab` / `_ReleasedPrefab` | Replace `_ShadePrefab` |
| `_TetheredShade` / `_ReleasedShade` | The shade currently out in each form (only one is ever filled). Replaces `_CurrentShade` |
| `TetherShade()` | Called by the Tether Shade ability |
| `ReleaseShade(offset)` | Replaces `SummonShade(offset)`. Same placement, but returns the tethered shade first |
| `ReturnShade()` | Puts away whichever form is out |
| `ReturnReleased()` | Puts away only the released shade. Called by `DungeonManager.EnterDungeon` |
| `CanSummon()` | The one place that decides if the current slot's shade can come out in any form. Always true for now |

`ShadeControlAbility()` keeps working on the released shade only.

### IShadeForm (interface) *(built)*
Both prefabs implement it, so the manager treats them the same way. Lives in `ShadeInterface.cs`.

| Function | Description |
| :--- | :--- |
| `Setup(ShadeSO slot, GameObject summoner)` | Gives the form its slot data and who summoned it, then spawns its art |
| `Appear()` | Instant for now. Hook for the magic circle / pop-out later |
| `Dismiss()` | Destroys it for now. Hook for the tethered shade sinking into the player later |

### ShadeEvolutionSO *(built)*
| Variable | Description |
| :--- | :--- |
| `_TetheredArt` | Art prefab used while tethered |
| `_ReleasedArt` | Art prefab used while released. Replaces `_characterArt`. Can be the same prefab as `_TetheredArt` |
| `_TetherDistance` | How far from the player the tethered shade sits, so big and small shades both look right |
| `_TetherWidth` | How wide the shade is, for the tail later |

Size data goes on the evolution instead of the slot, since a shade changes size as it evolves.

### ShadeArtRig (on every shade art prefab) *(built)*
| Variable | Description |
| :--- | :--- |
| `_TailConnectionPoint` | Where the tail meets the body |
| `_CastPoint` | Where abilities come from |
| `_HideWhenTethered` | Child objects to turn off while tethered (like the legs on a one-FBX shade) |
| `_HideWhenReleased` | Child objects to turn off while released |

Lets one FBX serve both forms: use it for both art fields on the evolution and say "turn off legs while tethered".

### TetheredShade (on PFB_Shade_Tethered) *(built)*
Prefab: `TetheredShade`, `Rigidbody` + `UnitHover`, `ShadeTail`, and an empty art holder the art is spawned into.

**Where it goes (slots)**
- **Resting:** behind the player, `_TetherDistance` away, opposite the player's facing (`CharacterMovement.GetLookDirection()`). The [[Player Movement|PlayerInputDriver]] already makes facing follow travel when not aiming and point at the aim while aiming, with its timer to snap back. So this one rule covers both "behind the direction of travel" and "opposite the aim target"
- **Casting:** in front of the player toward the aim. Only a hook for now, used once abilities can be cast [[Notes for the future]]

**How it moves**
- Glides in a straight line to its slot (smoothed translation), passing through the player instead of circling around. It only turns to face things, never to travel
- No collider, so it can't bump the player or get stuck on corners
- **Wall check:** before using a slot, a spherecast from the player to the slot pulls it in if a wall is in the way. The casting slot also checks the `_CastPoint` isn't behind a wall, so a player backed into a wall still has a shade that can defend
- **Height:** `UnitHover` floats it over its own floor. If there's no floor under it (a pit or ledge), it holds at the player's floor height. *Try it and see how it feels*

### ShadeTail (placeholder) *(built)*
The real tail waits until the art style is locked in. This pass only sets up the points it will need:
- **Start:** the floor point under the player, from the player's `UnitHover` raycast
- **End:** the art rig's `_TailConnectionPoint`
- Drawn with a **LineRenderer** as a simple L (along the floor, then up into the body). Turns off while the player isn't grounded
- Later: a real tail mesh along a curve, sized by `_TetherWidth`, plus a shader to hide it while the player is in the air [[Notes for the future]]

### UnitHover *(built)*
- `_FloorLayers` layer mask, so the shade's ray doesn't land on the player when it passes over them
- `GetFloorHit()` so the tail can read the player's floor point

### ReleasedShade (on PFB_Shade_Released) *(built)*
- The current `Shade` component, renamed. Its static `Shade.shade` becomes `ReleasedShade._Released`
- Implements `IShadeForm` and spawns its art from the evolution's `_ReleasedArt`
- Clears its static when disabled, so a returned shade doesn't stay the static

---
## Build Order *(code done Oct 2026)*
1. Renames: `Shade` → `ReleasedShade`, `PFB_Shade` → `PFB_Shade_Released`, `SummonShade` → `ReleaseShade` (function and asset)
2. `IShadeForm`, `ShadeArtRig`, `ShadeEvolutionSO` fields
3. Shade Manager: two prefabs, tether / release / return, `CanSummon()`
4. `PFB_Shade_Tethered` + `TetheredShade` resting slot and movement
5. Wall check and height fallback
6. `UnitHover` changes + `ShadeTail` LineRenderer
7. Abilities: Tether Shade, Return Shade, update Release Shade
8. Scene rules: tethered shade persists and snaps to the player, `EnterDungeon` returns the released shade
9. Released shade gets art from its evolution

---
## Unity Setup [[Notes for the future]]
Things the code can't do by itself. Do the renames **in Unity's Project window** (not in Explorer) so the `.meta` files move with them and prefabs keep their references, and do them **before** opening or saving the shade prefab.
1. ~~Rename `Shade.cs` → `ReleasedShade.cs`~~ *(done)*
2. ~~Rename `SummonShade.cs` → `ReleaseShade.cs`~~ *(done)*
3. Rename `PFB_Shade` → `PFB_Shade_Released` and `SO_Ability_SummonShade` → `SO_Ability_ReleaseShade`
4. ~~Make `PFB_Shade_Tethered` and assign it to `_TetheredPrefab` on the Shade Manager~~ *(done by Claude, Oct 2026)*
5. ~~Make `SO_Ability_TetherShade` and add it to the player's `_TamerAbilities`~~ *(done, it reuses the Summon Shade icon for now)*. Still to do: add the `ReturnShade` function to `SO_Ability_ReturnShade` and add it to `_TamerAbilities`
6. Every evolution (Baby, Mino, Turtle) uses the placeholder art `PFB_Shade_Art_Placeholder_0` (a sphere with a `ShadeArtRig`) as `_TetheredArt`, with `_TetherDistance` 2. Swap in real art per evolution later
7. Add a `ShadeArtRig` to each real shade art prefab and set its tail and cast points
8. Optional, for released art from the evolution: remove the art child from `PFB_Shade_Released`, add an empty `ArtHolder` child and drag it into `_ArtHolder`. Until then the prefab keeps its own art
9. Playtest: catch-up feel, wall pull-in, the pit/ledge height rule, scene changes, entering a dungeon with each form out

---
## Later (not this pass)
- **Casting from the tethered form:** waits for the ability overhaul (abilities as brain actions). The tethered shade will get its own small action list, and moving to the casting slot is its "get into position" [[Notes for the future]]
- **Appear / dismiss effects:** the tethered shade sinks into the player, a magic circle opens and the released shade pops out. May be partly procedural, partly animation. Waits for shade art [[Notes for the future]]
- **Real tail:** see ShadeTail above [[Notes for the future]]
- **Tethered shade taking damage** (playtest call) [[Notes for the future]]
- **Released time limit:** whether a released shade can only stay out for a limited time is undecided (see [[Release Shade]]) [[Notes for the future]]
- **Lives and health** feeding `CanSummon()` and the [[Fail State]] [[Notes for the future]]
- **Switching slots while a shade is out** [[Notes for the future]]
- **Fused form** [[Notes for the future]]
