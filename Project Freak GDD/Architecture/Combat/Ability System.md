## Overview
Abilities are built as data in the inspector instead of as one script per ability. An ability is a list of **steps**, and each step runs a list of **functions**. A unit runs abilities with an `AbilityInterpreter` component.

```text
AbilitySO
 ├── Step 0 (Timing, StepObject)
 │     ├── AbilityFunction
 │     └── AbilityFunction
 ├── Step 1
 │     └── AbilityFunction
 └── ...
```

The player's abilities are listed in `PlayerData` (`_TamerAbilities`, `_StandardAbilities`) and currently used through the [[Radial Menu]].

---
## AbilitySO
Created from **Create → Ability** (default name `SO_Ability_`).

| Variable           | Description                                    |
| :----------------- | :--------------------------------------------- |
| `_AbilityName`     | Name shown in the radial menu                  |
| `_AbilitySprite`   | Icon shown in the radial menu                  |
| `_AbilityCooldown` | *Not used yet* [[Notes for the future]] |
| `_steps`           | The ordered list of `AbilityStep`s             |

## AbilityStep

| Variable      | Description                                                                          |
| :------------ | :----------------------------------------------------------------------------------- |
| `DevNotes`    | Notes for yourself in the inspector                                                  |
| `Timing`      | How long to wait after this step before moving on. 0 means move on right away        |
| `_StepObject` | Object this step can spawn (used by `SummonUnit`)                                    |
| `_Functions`  | The functions this step runs, in order                                               |

## AbilityFunction
The base class for every ability function. Each one overrides:

```csharp
public virtual void ActivateAbility(GameObject source, AbilityInterpreter interpreter)
```

`source` is the unit using the ability. `interpreter` gives access to the interpreter's helpers.

Steps and functions use `[SerializeReference]`, which lets one list hold different function types. In the inspector you pick which function type to add.

### Current Functions

| Function       | Description                                                                                          |
| :------------- | :--------------------------------------------------------------------------------------------------- |
| `TetherShade`  | Asks the [[Shade Manager]] to tether the shade (or put it away if it's already tethered). Used by `SO_Ability_TetherShade` |
| `ReleaseShade` | Renamed from `SummonShade`. If the shade is already released, puts it away. Otherwise finds open space around the source (raycasts in a circle along the ground) and asks the [[Shade Manager]] to release the shade there. Used by `SO_Ability_ReleaseShade` |
| `ReturnShade`  | Asks the [[Shade Manager]] to put away whatever form is out. Used by `SO_Ability_ReturnShade` |
| `ControlShade` | Asks the [[Shade Manager]] to switch control to the shade                                            |
| `TestAbility`  | Logs a message. For testing                                                                          |

### Making a New Function
1. Create a class that inherits `AbilityFunction` and add `[System.Serializable]`
2. Override `ActivateAbility`
3. It will now show up as an option in any ability step

**Renaming a function class:** steps save their function by class name, so renaming one breaks existing ability assets. Add `[MovedFrom(false, sourceClassName: "OldName")]` (from `UnityEngine.Scripting.APIUpdating`) to the renamed class, like `ReleaseShade` does.

---
## AbilityInterpreter
Runs one ability at a time on its unit.

```text
InitializeAbility(ability)
    ↓
ExecuteAbility() — runs the current step's functions
    ↓
Timing > 0 ? wait (AdvancementTimer) : continue
    ↓
AdvanceAbilityStep() — next step, or EndAbility() when out of steps
```

| Function                          | Description                                                        |
| :-------------------------------- | :----------------------------------------------------------------- |
| `InitializeAbility(AbilitySO)`    | Starts an ability from step 0                                      |
| `AdvanceAbilityStep()`            | Moves to the next step                                             |
| `EndAbility()`                    | Clears the current ability                                         |
| `InterruptAbility()`              | Stops the ability and any running timer                            |
| `SummonUnit(position, rotation)`  | Spawns the current step's `_StepObject`                            |
| `AbilIntLog(message)`             | Logs a message tagged with the unit's name                         |

*Known issue:* the wait always uses the **first** step's `Timing` instead of the current step's. See [[Known Issues]]. [[Notes for the future]]

---
## Related
- [[Tether Shade]] / [[Release Shade]] / [[Return Shade]] / [[Control Shade]] (design)
- [[Radial Menu]]
- [[Shade Manager]]
