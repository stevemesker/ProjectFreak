## Overview
How a [[Shade]] exists in code:
- **`ShadeSO`** - the saved data for a [[Shade Slot]]
- **`ShadeEvolutionSO`** - the data for one [[Evolution]] (stats, art, body, abilities)
- **`ReleasedShade`** - the component on the released shade (`PFB_Shade_Released`), covered here
- **`TetheredShade`** - the component on the tethered shade (`PFB_Shade_Tethered`), see [[Tethered Shade]]

Both shade prefabs implement `IShadeForm` (`Setup`, `Appear`, `Dismiss`). Every shade uses the same two prefabs; what makes one shade different from another is its data. The [[Shade Manager]] owns the slots and spawns whichever form is asked for. See [[Shade Forms Plan]].

---
## ShadeSO
One asset per shade slot. Created from **Create → ScriptableObjects → Shades → ShadeSlot**. *Reworked Oct 9, 2026 (see [[Rune Field Overhaul Plan]], step 2).*

**The slot is the source of truth** for everything long term about its shade. Base stats come from the current evolution, so the slot only holds what's personal to this shade. It's changed during play **on purpose** (saving the rune field, leveling and evolving now; dying later), and the changes stay in the editor afterwards, which is handy for playtests. **Reset Slot (playtests)** starts it over: empty field, level 1, no fragment, no gates, and (in play mode) back to the [[Shade Manager]]'s Bound form. Outside play mode the form stays as it is (warning).

| Variable | Description |
| :--- | :--- |
| `_CurrentEvolution` | The shade's current [[Evolution]] (`ShadeEvolutionSO`). Warns in the inspector if empty |
| `_GatesTaken` | The gate node taken in each zone, in order. **Rank** = how many (`GetRank()`, Bound = 0). Filled by `Evolve(gate, form)`, which also swaps `_CurrentEvolution` (see [[Shade Manager]], Evolving) |
| `_Level` | Level (default 1) |
| `_XP` | Experience. Leveling isn't built yet [[Notes for the future]] |
| `_Lives` | Times the shade can fall in a dungeon (default 3) |
| `_FragmentPower` | Core fragment power. 0 = none. **Core power = level + fragment** (`GetCorePower()`) |
| `_RuneField` | The saved [[Rune Field]] (`RuneFieldData`). Was `_StartingRuneField` (kept through `FormerlySerializedAs`) |
| `_CompiledEffects` | Everything the saved field does, worked out on save (read only). The [[Shade Manager]] builds stats from this |
| `_PluggedNodes` | Snapshots of every node the saved field has a rune plugged into (read only). Lets the rules run again without the field scene |

`SaveRuneField(field, effects, nodes)` replaces all three rune field lists at once (never adds to them) and marks the asset changed so the editor saves it to disk.

*Removed Oct 9:* `_shadeStats` (base stats are on the evolution, level/XP/lives moved onto the slot), `_AlteredStats` and `_RuneFieldPackage` (old). Existing slot assets start at level 1.

---
## ShadeEvolutionSO
*Filled out Oct 9, 2026.* Making an evolution = filling in one asset. No element on purpose (affinity only comes from runes). Warns in the inspector about missing pieces (no HP, no art, no ultimate, empty natural ability slots).

| Section | Variable | Description |
| :--- | :--- | :--- |
| Stats | `_BaseStats` | This form's base stats (`ShadeStats`). Was `_coreStats` (kept through `FormerlySerializedAs`). Level, XP, current health, lives and name in here are ignored, those come from the slot. *Reviewed in step 3: kept, so every character shares one stat sheet* |
| Art | `_TetheredArt` | Art prefab spawned while tethered. Should have a `ShadeArtRig` (see [[Tethered Shade]]) |
| Art | `_ReleasedArt` | Art prefab spawned while released. Was `_characterArt`. Can be the same prefab as `_TetheredArt` |
| Body | `_TetherDistance` | How far from the player the tethered shade sits, meters (default 2) |
| Body | `_TetherWidth` | Body width where the tail connects, meters. Not used yet, for the real tail [[Notes for the future]] |
| Body | `_SizeClass` | Same size classes as enemies (`EnemyType.SizeClass`). Not read by the shade yet [[Notes for the future]] |
| Abilities | `_UltimateAbility` / `_NaturalAbilities` | The form's ultimate and always-on abilities. Not used yet, they come with the Shade Manager runtime entries (step 3) [[Notes for the future]] |

Size data lives on the evolution instead of the slot, since a shade changes size as it evolves. *Capabilities (like "can hold weapons") are skipped until a system needs them* [[undecided]]

---
## ReleasedShade (component)
Renamed from `Shade` (Oct 2026). Reached with the static `ReleasedShade._Released`, which is set when it's enabled and cleared when it's disabled, so a returned shade doesn't stay the static.

| Variable         | Description                                          |
| :--------------- | :--------------------------------------------------- |
| `_shadeSlotData` | The `ShadeSO` this shade uses                        |
| `_shadeEvoData`  | Its evolution data                                   |
| `_RuntimeEntry`  | Its slot's runtime entry on the [[Shade Manager]] (live stats, `_Health` = current health, abilities). The same object as the manager's, not a copy, so returning or switching shades loses nothing. Grabbed in `Setup`, read with `GetRuntimeEntry()`. Nothing reads it yet, the health component will [[Notes for the future]] |
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
- `RefreshForm()` (not part of the interface) - re-reads the slot's evolution and swaps in its art. The [[Shade Manager]] calls it when the shade evolves while it's out (the tethered shade has one too, which also re-hooks its tail)

When it's destroyed (returned, or removed by a scene change) it calls `ShadeManager.OnReleasedShadeGone`, so the player is never left controlling a shade that's gone.

### ISummonUnit
The shade implements `ISummonUnit`:
- `AssignSummoner(GameObject)` - stores `PlayerRef`. Called from `Setup`
- `UpdateStats(CoreStats)` - *empty for now* [[Notes for the future]]
