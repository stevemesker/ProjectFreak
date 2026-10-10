## Overview
`ShadeManager` holds the data for each of the player's [[Shade Slot]]s, brings the [[Shade]] out in its forms ([[Tether Shade]], [[Release Shade]], [[Return Shade]]), and handles switching control between the player and the released shade ([[Control Shade]]).

It lives on the Game Manager object (see [[AA - Managers]]) and is reached through its singleton:

```csharp
ShadeManager._ShadeManager
```

`GameManager.GetShadeManager()` also returns this singleton.

Only one shade is ever out, in one form. See [[Shade Forms Plan]], [[Tethered Shade]] and [[Shade (Runtime)]].

---
## Inspector Data

**Pointers**

| Variable                  | Description                                                                 |
| :------------------------ | :-------------------------------------------------------------------------- |
| `_TetheredPrefab`         | `PFB_Shade_Tethered`, spawned by Tether Shade                                |
| `_ReleasedPrefab`         | `PFB_Shade_Released`, spawned by Release Shade. Was `_ShadePrefab` (the assigned prefab carried over) |

**Runtime Data** (read only)

| Variable              | Description                                                         |
| :-------------------- | :------------------------------------------------------------------ |
| `_TetheredShade`      | The tethered shade currently out                                     |
| `_ReleasedShade`      | The released shade currently out. Never filled at the same time as `_TetheredShade` |
| `_IsControllingShade` | True while the player is driving the released shade                  |

**Shade Slots**

| Variable               | Description                                                     |
| :--------------------- | :-------------------------------------------------------------- |
| `_ShadeSlots`          | Every possible shade slot (`ShadeSO`, see [[Shade (Runtime)]])  |
| `tamerSlotLevel`       | How many slots the player has access to                         |
| `currentShadeSelected` | Index of the active slot                                        |
| `_BoundEvolution`      | The form every shade starts in. A slot's **Reset Slot** button puts it back to this (play mode only, the button can't reach the manager outside play), and dying will too later [[Notes for the future]] |

**Rune Fields**

| Variable | Description |
| :--- | :--- |
| `_RuneFieldSettings` | `SO_RuneField_Settings` (`RuneFieldSettingsSO`, see [[Rune Field System]]). The rune field scene gets it from here. If empty, the defaults are used and a warning is logged |
| `_RuntimeEntries` | Each slot's runtime entry (read only, see **Runtime Entries** below). Shown with Odin's `ShowInInspector`, not saved by Unity |

**Control Switching**

| Variable         | Description                                                    |
| :--------------- | :------------------------------------------------------------- |
| `_switchTimeIn`  | Fade out time when switching control (default 0.5)             |
| `_switchTimeOut` | Fade in time when switching control (default 0.3)              |
| `isBusy`         | True while a switch is happening. Blocks summoning/switching   |

---
## Summoning
Every function returns early while a control switch is running (`isBusy`).

| Shade out now | `TetherShade()` | `ReleaseShade(offset)` | `ReturnShade()` |
| :--- | :--- | :--- | :--- |
| None | Tethered appears | Released appears at player + offset | Nothing |
| Tethered | Tethered goes away | Tethered goes away, released appears | Tethered goes away |
| Released | Released goes away, tethered appears | Released goes away | Released goes away |

| Function | Description |
| :--- | :--- |
| `TetherShade()` | Called by the Tether Shade ability |
| `ReleaseShade(offset)` | Called by the Release Shade ability with a spot it found next to the player. Control stays with the player, the shade runs on its AI |
| `ReturnShade()` | Called by the Return Shade ability. Puts away whichever form is out |
| `ReturnReleased()` | Puts away only the released shade. Called by `DungeonManager.EnterDungeon`, since released shades don't come into dungeons (a tethered shade does, it persists between scenes) |
| `CanSummon()` | The one place that decides if the current slot's shade can come out in any form. Checks the player exists and the selected slot is valid. Health and [[LIFE]] plug in here later [[Notes for the future]] |
| `GetCurrentForm()` | Returns `ShadeFormType.Form` (None, Tethered, Released) |
| `OnReleasedShadeGone(shade)` | Called by `ReleasedShade` when it's destroyed, so a scene change that removes it is handled too |

Spawning: the prefab is instantiated, then `Setup(slot, player)` and `Appear()` are called through `IShadeForm`. A prefab without the right component on its root logs an error and is destroyed.

**Returning a shade the player is driving:** the player is put straight back in their own body (no fade), the camera goes back to the player, and any control switch that was mid-fade is stopped. Same when a scene change removes the released shade.

## Control Switching — `ShadeControlAbility()`
Called by the `ControlShade` ability. It's a toggle: while the player is driving the shade, it switches back to the player; otherwise it takes over the **released** shade (does nothing if none is out). Either way it starts the `ShadeControlSwitch` coroutine:

```text
Fade screen out (HUD Manager)
    ↓
Wait
    ↓
Move camera to the shade, or back to the player (Camera Manager)
    ↓
Turn off player movement, turn on shade movement
    ↓
Fade screen back in
```

`ControlShade(bool control)` does the actual swap:
- `true` - player movement off, shade movement on
- `false` - player movement on, shade movement off

*While controlling, the radial menu still shows the player's abilities. It should show the shade's abilities instead (system for later).* [[Notes for the future]]

---
## Rune Fields and Stats
*Rebuilt Oct 2026, moved onto the slots Oct 9.* Each slot's saved [[Rune Field]] lives on its slot asset (`ShadeSO`, see [[Shade (Runtime)]]). The manager reads it, writes it when the player saves, and keeps each slot's runtime entry (see [[Rune Field System]]).

| Function | Description |
| :--- | :--- |
| `GetSavedRuneField(slot)` | A copy of a slot's saved field for the UI to edit |
| `SaveRuneField(slot, field)` | Returns whether it saved. **1.** Counts every rune type on the old and new field. **2.** Checks the inventory has enough for every rune the new field adds; if not, logs an error and changes **nothing**. **3.** Takes added runes out of the inventory and gives removed runes back. **4.** Sets the field to the slot's current core power (in case it leveled while the field was open), writes its data, compiled effects and plugged node snapshots onto the slot (`ShadeSO.SaveRuneField`), **evolves** the shade if a gate in its open zone now has power (`TryEvolve`), then rebuilds the slot's runtime entry. With no Inventory Manager it saves without touching the inventory (warning). A draft made before the shade evolved (its rank is out of date) is refused with a warning |
| `GetSavedRuneCount(slot, element)` | How many of a rune the slot's saved field has (the rune field UI uses it for pending counts) |
| `GetCorePower(slot)` | The slot's core power: its level + fragment power |
| `GetRank(slot)` | How many times the slot's shade has evolved (Bound = 0) |
| `GetBoundEvolution()` | The shared Bound form |
| `GetRuneFieldSettings()` | The shared rune field settings |
| `IsValidSlot(slot)` | Checks a slot index points at a real slot |

---
## Evolving
*Added Oct 9, 2026 (overhaul step 7).* A shade evolves when a gate (a node with an Evolve effect) in its open zone gets power. See **Zones and Evolving** in [[Rune Field System]].

- **`TryEvolve(slot)`** runs after every save and every level-up. It runs the rules on the slot's saved field with its node snapshots (no field scene, so it works mid-dungeon and mid-fight). If a gate is on, `ShadeSO.Evolve(gate, form)` records the gate in `_GatesTaken` (rank + 1) and swaps `_CurrentEvolution`, then the field runs again so the zone it left counts as frozen (frozen runes get power first)
- Then the runtime entry is rebuilt (new base stats; the max HP rule moves current HP with max HP), and **`FinishEvolving`**: if that slot's shade is out, `TetheredShade.RefreshForm()` / `ReleasedShade.RefreshForm()` swap in the new form's art on the spot (and the tethered shade's tether distance and tail point). Then the C# event **`OnShadeEvolved(slotIndex)`** fires; the rune field listens to reload
- A gate with power but no form set on its Evolve effect logs an error and doesn't evolve
- *No evolve effect or VFX yet, the art just swaps* [[Notes for the future]]

---
## Runtime Entries
*Added Oct 9, 2026 (overhaul step 3).* One `ShadeRuntimeEntry` per slot: what the shade is like right now. Every layer uses the same `ShadeStats` character sheet as the player and enemies.

| Field | Description |
| :--- | :--- |
| `_SlotName` | The slot asset's name |
| `_HardStats` | The evolution's `_BaseStats` + every stat change in the slot's saved compiled list + the slot's level, XP and lives. `_Health` just equals `_HP` here |
| `_LiveStats` | Hard stats + timed effects and equipment (later [[Notes for the future]]). **`_Health` is the shade's current health.** This is what combat should read |
| `_Abilities` | The evolution's natural abilities and ultimate + every Grant Ability effect, each listed once. Not used yet [[Notes for the future]] |

**`RebuildSlot(slot)`** is the one place an entry is worked out (also on the [[Manager Wrappers|Shade Manager Wrapper]]). It runs at game start, on field save, on level change and on evolving (later also slotting a shard [[Notes for the future]]). It keeps the **same entry object** and fills it in again, so summoned shades pointing at it stay pointed at it. Stats are worked out from scratch, never added or removed bit by bit, so they can't drift. A negative rune can't push a stat below 1 (unless the base was already below 1). No evolution logs a warning and starts from all 0.

**Max HP rule:** when max HP changes, current health moves by the same amount, clamped between 0 and the new max. The first build of the session starts at full health. A shade already at 0 stays at 0 (a stat change doesn't revive it). Dropping to 0 this way only logs a warning for now; falling is the [[Fail State]] work [[Notes for the future]]

| Function | Description |
| :--- | :--- |
| `RebuildSlot(slot)` | Works the entry out again from the slot asset |
| `GetRuntimeEntry(slot)` | By index or by `ShadeSO`. Null if the slot doesn't exist |
| `GetSlotStats(slot)` / `GetCurrentShadeStats()` | A slot's **live** stats |
| `Heal(slot, amount)` | Never above max. A shade at 0 isn't healed back up (Fail State decides that) |
| `RefillHealth(slot)` / `RefillAllHealth()` | Back to full. For the hub and rest floor refills. *Nothing calls these on entering the hub yet, there's no "entered the hub" moment in the code* [[Notes for the future]] |
| `SetSlotLevel(slot, level)` / `AddLevels(slot, amount)` | Changes the slot's level (minimum 1), **re-runs its saved rune field** with the new core power using its node snapshots (no field scene needed, works mid-dungeon), writes the result back to the slot, then evolves the shade if the extra power reached a plugged gate, then rebuilds the entry |

**Test Tools** (Odin foldout, play mode only): **Level Up Current Slot**, **Hurt Current Slot** (takes health off, to test healing and the max HP rule), **Refill All Health**.

*Not in yet:* Hazen's share of rune stats, timed effects and the game clock, and saving slots to disk [[Notes for the future]]. Nothing reads live stats in combat yet, since the released shade has no health component (see [[Shade (Runtime)]]).

*Oct 9, 2026:* the old stat path (`managerScriptableObject`, `shadeAlterPackages`, `ReceiveStatBoostPackage`, `RemoveStatBoostPackage`, `ChangeStat`) and the slot copies (`_SlotRuneFields`, `_RuneFieldLayout`) were removed.

---
## Other Functions

| Function                                  | Description                                          |
| :---------------------------------------- | :--------------------------------------------------- |
| `GetCurrentShade()`                       | Returns the active `ShadeSO`                         |
| `GetShadeOfIndex(int)`                    | Returns the `ShadeSO` in that slot                   |
| `SetShadeSelection(int)` / `GetShadeSelectionIndex()` | Set/get the active slot              |
