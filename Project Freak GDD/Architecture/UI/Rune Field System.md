## Overview
The code behind the [[Rune Field]], the UI where the player drags [[Element Rune]]s out, chains them to the [[Core Node]] for power, and plugs them into [[Ability Node]]s to upgrade a [[Shade]].

*Rebuilt Oct 2026.* The rules now live in plain C# classes (`Scripts/Rune Field`) and the UI scripts (`Scripts/UI/Evolution Tree`) only draw them, so the planned 3D view can reuse the same rules (see [[Rune Field Overhaul Plan]]).

```text
Shade Manager (each slot's saved RuneFieldData + stats)
    │ copy of the slot's field
    ▼
RuneField (the rules: bridges, power, ability nodes)  ◄── RuneFieldLayoutSO (core settings + ability nodes)
    │ OnFieldChanged
    ▼
RuneFieldManager (2D view) ── ElementItem (runes) ── NodeBridge (bridges) ── CoreNode ── EvolutionNode
    │ Save
    ▼
Shade Manager stores the field and works the slot's stats out again
```

| Script | Role |
| :--- | :--- |
| `RuneFieldData` | The saved state of one slot's field: runes, bridges, plugs, active nodes. Plain data |
| `RuneFieldLayoutSO` | What every slot shares: core reach, core max bridges, field radius, ability nodes |
| `RuneField` | The rules. A plain C# class, not a component |
| `RuneFieldManager` | The 2D view. Spawns/positions everything, passes drags to `RuneField`, saves to the [[Shade Manager]] |
| `ElementItem` | A rune on the field. Shows power and forwards drags |
| `NodeBridge` | The line between two pieces. Runs the tear timer |
| `CoreNode` | The center of the field. Shows the core's power |
| `EvolutionNode` | An [[Ability Node]]. Shows its state, fires its events, holds its unlock/lockout lists |
| `IngredientListWindow` / `ElementDataObject` | The inventory list runes are dragged from |
| `ShadeSlotManager` | Shade slot buttons that load a slot's field |
| `SideWindowManager` | Opens/closes side panel windows |
| `RuneFieldLogicTester` (Dev Scripts) | Odin button that tests the rules on made-up fields |

---
## The Rules (RuneField)
Made with `new RuneField(data, layout, maxPower)`. Every change runs `Recalculate()`, which works power and ability nodes out from scratch and fires `OnFieldChanged`.

**Field units:** positions use the old UI's scale (a node is about 100 wide) with the core at (0, 0). The view decides how big a field unit is on screen.

**IDs:** each rune gets an ID that's never reused. Bridges and plugs point at IDs, not list indexes, so removing a rune can't shift anything. The core's ID is `RuneField.CoreID` (-1).

**Bridges:** allowed if neither end is full and the distance is within the bigger of the two reaches. Runes use their `ElementItemSO`'s `connectionDistance` (reach), `connectionsAllowed` (max bridges) and `powerNeeded`. The core uses the layout's `_CoreReach` and `_CoreMaxBridges` (0 = no limit). Dropping a rune bridges it to the closest things in reach, up to its free slots.

**Power:** handed out in layers by **fewest bridges from the core**; inside a layer, the rune placed first goes first. A rune the core can't afford stays dark and power doesn't flow through it, but cheaper runes on other branches still get power. It's all or nothing per rune.

**Ability nodes:** a node is on when a powered rune is plugged in, every node in its unlock list is on, and no active node has it in its lockout list. Nodes that are already on keep their spot, so when two nodes lock each other the first one stays the winner even when something unrelated changes. Active nodes are saved with the field.

**Dragging:** `ClampToBridges` keeps a rune within reach of every bridge it has. `GetBridgeStretch` says how far past reach a bridge is being pulled. Moving a plugged rune unplugs it, and a rune only snaps into a node if snapping wouldn't overstretch its bridges.

**Loading:** bad saved entries (missing runes, repeated bridges, plugs into nodes that don't exist) are cleaned out first. With no layout (the [[Shade Manager]] before the UI has given it one) power still works, but saved plugs and node states are left exactly as they are.

**Stats:** `GetStatTotals()` adds up the stat boosts of every powered rune. The [[Shade Manager]] turns that into the slot's stats.

| Function | Description |
| :--- | :--- |
| `TryPlaceRune(element, position, out id)` | Puts a new rune down (no bridges yet) |
| `MoveRune` / `RemoveRune` | Move or remove a rune (removing takes its bridges and plug with it) |
| `ConnectNearby(id)` | Bridges a rune to the closest things in reach (what dropping does) |
| `Connect` / `Disconnect` | Make or remove one bridge |
| `FindBridgeTargets(id, position)` | What a rune would bridge to if dropped here |
| `ClampToBridges` / `GetBridgeStretch` | Drag limits and tear pull |
| `FindNodeAt` / `CanSnapToNode` / `TryPlugRune` / `UnplugRune` | Ability node plugging |
| `SetMaxPower` | Core power changed (shade level) |
| `GetStatTotals()` | Stat boosts from every powered rune |

---
## RuneFieldLayoutSO
Created from **Create → Rune Field → Layout**. Shared by every slot.

| Variable | Description |
| :--- | :--- |
| `_CoreReach` | How far the core reaches to make a bridge (default 150) |
| `_CoreMaxBridges` | Max bridges on the core. 0 = no limit |
| `_FieldRadius` | How far from the core runes can go. 0 = no limit |
| `_AbilityNodes` | Every node: name, position, snap radius, unlock indexes, lockout indexes. Warns in the inspector about bad indexes |

*No layout asset exists yet.* Until one is assigned on the [[Shade Manager]], `RuneFieldManager` builds one in memory from the scene's node objects (their positions and unlock/lockout lists) and hands it over. [[Notes for the future]]

---
## The 2D View
### RuneFieldManager
Kept its name and fields, so the scene didn't need setting up again.

| Variable | Description |
| :--- | :--- |
| `_CoreReach` / `_NodeSnapRadius` | Used when building the layout from the scene (150 / 70) |
| `_TearSlack` | How far past reach a rune can be pulled before its bridges start tearing (2) |
| `CorePointer` | The core object. It's the field's (0, 0) |
| `ListOfNodes` | Ability node objects, in layout order |
| `ListOfRunes` | Rune objects on screen (runtime). Runes placed in the scene by hand are removed on start |
| `elementPrefab` | Rune prefab. Its `BridgePrefabRef` is used for bridges |

- **Start:** removes hand-placed test runes and opens the selected slot
- **`LoadRuneField(index)`:** opens a slot's saved field as a copy. Unsaved changes on the open slot are thrown away
- **`SaveRuneSlot()`** (Odin **Save Current Field**): saves the field to the [[Shade Manager]], which works the slot's stats out again. *No UI save button in the scene yet* [[Notes for the future]]
- **`DiscardChanges()`** (Odin button) and **`ClearRuneField()`** (Odin **Test Clear**)
- **`PlaceRuneFromInventory(element, position)`:** called by `ElementDataObject` when an element is dropped on the field. Places it, plugs it if it's on a node, then bridges it
- **Dragging:** `BeginRuneDrag` unplugs, `DragRune` clamps to bridges, shows the node snap and drives tearing, `EndRuneDrag` plugs and bridges
- **Drawing:** every `OnFieldChanged` syncs the rune objects, rebuilds the bridges, fades unpowered runes, updates node backgrounds and the core's power. Node events only fire on changes while editing, not when a field loads
- **`UpdateScaler()`:** still called by the zoom script but does nothing now. Everything is a child of the field and the rules don't use colliders

### ElementItem (runes)
Set up by the manager with its rune ID and element. Shows power (`RequiredPower` / `CurrentPower`, read only) and fades to `_UnpoweredAlpha` (0.45) without power, using a `CanvasGroup` it adds if the prefab doesn't have one. Drags go straight to the manager.

### NodeBridge
Draws the line between its two ends. While a rune is pulled past reach, `StartTearing(pull)` runs a timer that fills faster the harder it's pulled; when full it calls `RuneFieldManager.TearBridge`, which removes it from the field. Settings: `PullTearRequiredTime`, `MaxPullStrength`, `pullTickTimeLength`, `pullStrengthModifierDampening`.

### CoreNode
Shows the core's power (`CoreNodeCurrentPower` = left, `CoreNodeMaxPower`). Optional `_PowerLabel` text shows "left / total".

### EvolutionNode (ability nodes)
Shows its state with the scene's state backgrounds: **Enabled** while on, **Locked** while locked out, none otherwise. Fires `ActivationEvent` / `DeactivationEvent` when it switches while the player is editing. Its `unlocks` and `Lockouts` lists are read when the layout is built from the scene. What a node actually does (abilities, stat upgrades, evolution) isn't built yet [[Notes for the future]]

*The "may delete" fields and the Enable/Lock/Hide buttons are from an older version of the node states.*

---
## Inventory Side Panel
- `IngredientListWindow` listens to the [[Inventory Manager]]'s `OnInventoryChanged` and shows one `ElementDataObject` button per element rune (the ingredient version is commented out)
- Dropping an `ElementDataObject` on the object tagged **UI Drag Field** calls `RuneFieldManager.PlaceRuneFromInventory`. The inventory count doesn't go down yet (see [[Known Issues]])
- `SideWindowManager.CloseWindow(window)` slides the side panel open/closed and switches windows

## Shade Slots
`ShadeSlotManager` shows one button per unlocked slot and calls `LoadRuneField(index)` when one is clicked. It currently uses the player's level from [[Game Manager]] to decide how many slots are unlocked.

---
## Leftovers
- `DraggableItem`, `ElementObject` and `ShadeSlotDataObject` are small early scripts that don't do anything
- In `EvolutionInterfaces.cs`, `IConnectable`, `ICoreNode` and `iEvolutionNode` aren't used anymore. `IBridgeable` isn't used by the rune field but [[Dungeon Map Node]]s still use it, and the dungeon map draws its lines with `NodeBridge.BuildConnection` / `UpdatePosition`
- `RuneFieldPackage.cs`, `ShadeSO._RuneFieldPackage` and `_AlteredStats`, `ElementManagerSO`, the element assets' `statusEffectEnable/Disable` events, and the Shade Manager's `ReceiveStatBoostPackage` / `ChangeStat` were the old stat path and aren't used anymore

All tracked in [[Known Issues]] (Cleanup).
