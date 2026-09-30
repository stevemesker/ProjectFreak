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
| `_movement`      | Its `CharacterMovement` (same script as the player, see [[Player Movement]]) |
| `PlayerRef`      | The unit that summoned it                            |

### Control
| Function                | Description                                               |
| :---------------------- | :-------------------------------------------------------- |
| `EnablePlayerControl()` | Turns on the shade's movement input (player controls it)  |
| `EnableShadeControl()`  | Turns off movement input (shade acts on its own)          |

*Shade AI doesn't exist yet, so when not player controlled the shade just stands still.*

### ISummonUnit
The shade implements `ISummonUnit`:
- `AssignSummoner(GameObject)` - stores `PlayerRef`
- `UpdateStats(CoreStats)` - *empty for now*

*Note:* the Summon Shade flow spawns the shade through the [[Shade Manager]] and doesn't call `AssignSummoner` yet.
