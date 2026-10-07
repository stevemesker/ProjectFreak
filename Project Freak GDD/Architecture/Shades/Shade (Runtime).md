## Overview
How a [[Shade]] exists in code:
- **`ShadeSO`** - the saved data for a [[Shade Slot]]
- **`ShadeEvolutionSO`** - the data for one [[Evolution]] (stats, art, tether size)
- **`ReleasedShade`** - the component on the released shade (`PFB_Shade_Released`), covered here
- **`TetheredShade`** - the component on the tethered shade (`PFB_Shade_Tethered`), see [[Tethered Shade]]

Both shade prefabs implement `IShadeForm` (`Setup`, `Appear`, `Dismiss`). Every shade uses the same two prefabs; what makes one shade different from another is its data. The [[Shade Manager]] owns the slots and spawns whichever form is asked for. See [[Shade Forms Plan]].

---
## ShadeSO
One asset per shade slot. Created from **Create → ScriptableObjects → Shades → ShadeSlot**.

| Variable            | Description                                                         |
| :------------------ | :------------------------------------------------------------------ |
| `_shadeStats`       | Base stats (`ShadeStats`, see [[Stats & Inventory Data]])          |
| `_AlteredStats`     | Stat changes from [[Rune Field]] runes                              |
| `_CurrentEvolution` | The shade's current [[Evolution]] (`ShadeEvolutionSO`)             |
| `_RuneFieldPackage` | Saved [[Rune Field]] layout                                         |

---
## ShadeEvolutionSO

| Variable          | Description |
| :---------------- | :---------- |
| `_coreStats`      | Base stats for this evolution |
| `_TetheredArt`    | Art prefab spawned while tethered. Should have a `ShadeArtRig` (see [[Tethered Shade]]) |
| `_ReleasedArt`    | Art prefab spawned while released. Was `_characterArt` (assets kept their art through `FormerlySerializedAs`). Can be the same prefab as `_TetheredArt` |
| `_TetherDistance` | How far from the player the tethered shade sits, meters (default 2) |
| `_TetherWidth`    | Body width where the tail connects, meters. Not used yet, for the real tail [[Notes for the future]] |

Size data lives on the evolution instead of the slot, since a shade changes size as it evolves.

---
## ReleasedShade (component)
Renamed from `Shade` (Oct 2026). Reached with the static `ReleasedShade._Released`, which is set when it's enabled and cleared when it's disabled, so a returned shade doesn't stay the static.

| Variable         | Description                                          |
| :--------------- | :--------------------------------------------------- |
| `_shadeSlotData` | The `ShadeSO` this shade uses                        |
| `_shadeEvoData`  | Its evolution data                                   |
| `_movement`      | Its `CharacterMovement` (same script as the player, see [[Player Movement]]). Floating is handled separately by `UnitHover` on the same prefab |
| `_InputDriver`   | Its `PlayerInputDriver` (off by default). Auto-filled from the same object if empty |
| `_AIDriver`      | Its `NavGuideDriver`, the AI steering (see [[Player Movement#NavGuideDriver (AI steering)]]). Auto-filled from the same object if empty |
| `_ArtHolder`     | Empty child the evolution's `_ReleasedArt` is spawned into. Left empty, nothing is spawned and whatever art is already on the prefab stays |
| `PlayerRef`      | The unit that summoned it                            |

### Control
| Function                | Description                                               |
| :---------------------- | :-------------------------------------------------------- |
| `EnablePlayerControl()` | AI driver off, `PlayerInputDriver` on (player controls it) |
| `EnableShadeControl()`  | `PlayerInputDriver` off, AI driver on (shade acts on its own) |

While the shade acts on its own, its [[Unit Brain]] decides what it does (to change its personality, see [[How To - Create Enemies & Shades#Part 2 - Set Up How a Shade Behaves]]): follow the player, fight what its [[Unit Targeting|targeting]] picks, wander nearby, or (with a berserk personality) run off looking for a fight. While the player drives it, the brain pauses. The prefab has `UnitTeam` (Player team, Shade role), `UnitTargeting` and `UnitBrain`.

*The shade has no health script yet, so the brain treats it as always at full health.* [[Notes for the future]]

### IShadeForm
- `Setup(slot, summoner)` - stores the slot and its evolution, calls `AssignSummoner`, and spawns the released art into `_ArtHolder` (applying the rig's released hide list)
- `Appear()` - instant for now. Hook for the magic circle / pop-out effect [[Notes for the future]]
- `Dismiss()` - destroys it for now [[Notes for the future]]

When it's destroyed (returned, or removed by a scene change) it calls `ShadeManager.OnReleasedShadeGone`, so the player is never left controlling a shade that's gone.

### ISummonUnit
The shade implements `ISummonUnit`:
- `AssignSummoner(GameObject)` - stores `PlayerRef`. Called from `Setup`
- `UpdateStats(CoreStats)` - *empty for now* [[Notes for the future]]
