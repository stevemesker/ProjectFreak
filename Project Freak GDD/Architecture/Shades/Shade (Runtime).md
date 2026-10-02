## Overview
How a [[Shade]] exists in code. There are two parts:
- **`ShadeSO`** - the saved data for a [[Shade Slot]]
- **`Shade`** - the component on the shade in the world

The [[Shade Manager]] owns the slots and spawns the shade.

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
## Shade (component)
Reached with the static `Shade.shade` (the first shade enabled sets it).

| Variable         | Description                                          |
| :--------------- | :--------------------------------------------------- |
| `_shadeSlotData` | The `ShadeSO` this shade uses                        |
| `_shadeEvoData`  | Its evolution data                                   |
| `_movement`      | Its `CharacterMovement` (same script as the player, see [[Player Movement]]). Floating is handled separately by `UnitHover` on the same prefab |
| `_InputDriver`   | Its `PlayerInputDriver` (off by default). Auto-filled from the same object if empty |
| `_AIDriver`      | Its `NavGuideDriver`, the AI steering (see [[Player Movement#NavGuideDriver (AI steering)]]). Auto-filled from the same object if empty |
| `PlayerRef`      | The unit that summoned it                            |

### Control
| Function                | Description                                               |
| :---------------------- | :-------------------------------------------------------- |
| `EnablePlayerControl()` | AI driver off, `PlayerInputDriver` on (player controls it) |
| `EnableShadeControl()`  | `PlayerInputDriver` off, AI driver on (shade acts on its own) |

While the shade acts on its own, its [[Unit Brain]] decides what it does (to change its personality, see [[How To - Create Enemies & Shades#Part 2 - Set Up How a Shade Behaves]]): follow the player, fight what its [[Unit Targeting|targeting]] picks, wander nearby, or (with a berserk personality) run off looking for a fight. While the player drives it, the brain pauses. The prefab has `UnitTeam` (Player team, Shade role), `UnitTargeting` and `UnitBrain`.

*The shade has no health script yet, so the brain treats it as always at full health.* [[Notes for the future]]

### ISummonUnit
The shade implements `ISummonUnit`:
- `AssignSummoner(GameObject)` - stores `PlayerRef`
- `UpdateStats(CoreStats)` - *empty for now* [[Notes for the future]]

*Note:* the Summon Shade flow spawns the shade through the [[Shade Manager]] and doesn't call `AssignSummoner` yet. [[Notes for the future]]
