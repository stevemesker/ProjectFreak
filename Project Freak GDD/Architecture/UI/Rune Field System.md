## Overview
The code behind the [[Rune Field]], the UI where the player drags [[Element Rune]]s out, chains them to the [[Core Node]] for power, and plugs them into [[Ability Node]]s to upgrade a [[Shade]].

*Rebuilt Oct 2026.* The rules now live in plain C# classes (`Scripts/Rune Field`) and the UI scripts (`Scripts/UI/Evolution Tree`) only draw them, so the planned 3D view can reuse the same rules (see [[Rune Field Overhaul Plan]]).

```text
Shade slot (ShadeSO: saved field, compiled effects, node snapshots)
    │ copy of the slot's field (through the Shade Manager)
    ▼
RuneField (the rules)  ◄── RuneFieldSettingsSO (shared numbers)  ◄── node list built from the scene's node objects
    │ OnFieldChanged
    ▼
RuneFieldManager (2D view) ── ElementItem (runes) ── NodeBridge (bridges) ── CoreNode ── EvolutionNode
    │ Save
    ▼
Shade Manager writes the field, its compiled effects and node snapshots onto the slot, then works the slot's stats out again
```

| Script | Role |
| :--- | :--- |
| `RuneFieldData` | The saved state of one slot's field: runes, bridges, plugs, active nodes. Plain data |
| `RuneFieldSettingsSO` | The numbers every slot shares: core reach and max bridges, edge bleed, node snap radius, zones, rune size. Also the zone math (which zone a spot is in, rings) |
| `AbilityNodeEntry` | One ability node as the rules see it (index, position, unlocks, lockouts, effects). Built from the scene's nodes, and saved on the slot as snapshots. In `RuneFieldData.cs` |
| `RuneField` | The rules. A plain C# class, not a component |
| `RuneEffect` + `StatChangeEffect`, `GrantAbilityEffect`, `EvolveEffect` | What a field does to its shade (see **Effects** below). In `Scripts/Rune Field/Effects` |
| `RuneFieldManager` | The 2D view. Spawns/positions everything, passes drags to `RuneField`, saves to the [[Shade Manager]] |
| `ElementItem` | A rune on the field. Shows power and forwards drags |
| `NodeBridge` | The line between two pieces. Runs the tear timer |
| `CoreNode` | The center of the field. Shows the core's power |
| `EvolutionNode` | An [[Ability Node]]. Shows its state, fires its events, holds its unlock/lockout lists |
| `IngredientListWindow` / `ElementDataObject` | The inventory list runes are dragged from |
| `ShadeSlotManager` | Shade slot buttons that open a slot's field (asking about unsaved changes first) |
| `ConfirmPopup` | Reusable Save / Discard / Cancel style popup (see [[Confirm Popup]]) |
| `SideWindowManager` | Opens/closes side panel windows |
| `RuneFieldLogicTester` (Dev Scripts) | Odin button that tests the rules on made-up fields |

---
## The Rules (RuneField)
Made with `new RuneField(data, settings, nodes, maxPower, rank)` (rank = how many times the shade has evolved, Bound = 0; leaving it out means Bound). `nodes` can be the full list built from the field scene, or just a slot's saved snapshots (the nodes it plugged into); the rules look nodes up by index, so gaps are fine. Every change runs `Recalculate()`, which works power and ability nodes out from scratch and fires `OnFieldChanged`.

**Field units:** positions use the old UI's scale (a node is about 100 wide) with the core at (0, 0). The view decides how big a field unit is on screen.

**IDs:** each rune gets an ID that's never reused. Bridges and plugs point at IDs, not list indexes, so removing a rune can't shift anything. The core's ID is `RuneField.CoreID` (-1).

**Bridges:** allowed if neither end is full and the distance is within the bigger of the two reaches. Runes use their `ElementItemSO`'s `connectionDistance` (reach), `connectionsAllowed` (max bridges) and `powerNeeded`. The core uses the settings' `_CoreReach` and `_CoreMaxBridges` (0 = no limit). Dropping a rune bridges it to the closest things in reach, up to its free slots. New bridges always use that normal reach (`GetBaseReach`). An existing bridge on a rune sitting in a node gets the settings' `_SnapStretch` on top (`GetBridgeReach`), so a snap that stretched it doesn't count as overstretched.

**Power:** handed out in layers by **fewest bridges from the core**; inside a layer, the rune placed first goes first. A rune the core can't afford stays dark and power doesn't flow through it, but cheaper runes on other branches still get power. It's all or nothing per rune. **Frozen runes go first** (see **Zones**): a first pass powers them outward along the frozen chain, then the normal pass hands out what's left, with frozen runes passing power on for free.

**Ability nodes:** a node is on when a powered rune is plugged in, every node in its unlock list is on, and it isn't locked out. Active nodes are saved with the field.

**Lockouts** *(step 7)*: trigger on **plugging, not power**. A rune plugged into X locks every node X lists, even with no power. A locked-out node won't take a rune (refused, red). If two rivals both hold runes (old data), the one plugged first wins. **Evolution gates in the same zone lock each other automatically**, nothing to wire. Lockouts are always two-way: `RuneField.FixOneWayLockouts` fills in a missing reverse link and `RuneFieldManager` logs a `Debug.LogError` naming both nodes, once, when it builds the node list. Unlocks stay power-based. A dragged rune ignores its own plug, so it can move straight from one rival node to the other.

**Footprints** *(overhaul step 6, Oct 9, 2026)*: every rune is a circle `_RuneSize` wide; nodes are rune sized (each holds one rune) and the core is `_CoreSize` wide. `IsSpotFree` refuses a spot outside the open zone, or one that overlaps another rune (frozen ones too), the core or a node. `CanDropAt(rune, position)` is the one check for placing and dropping: an empty node in snap range means "land on its center" if the node can take the rune (`CanPlugInto`: open zone, not locked out, snapping allowed), otherwise the drop is refused; anything else has to be free. Snap radius is smaller than rune size, so **landing near a node always means snapping into it**. A bad spot is refused, nothing gets pushed into a gap. Fields saved before footprints may overlap; they're left alone, the check only runs on placing and dropping.

**Snapping:** allowed if every bridge on the rune would be at most `_SnapStretch` (30) past its normal reach.

**Dragging:** `ClampToBridges` keeps a rune within reach of every bridge it has, and `GetBridgeStretch` says how far past reach a bridge is being pulled (the dragged rune counts as out of its node, so normal reach applies). `ClampToBridges` also keeps it within the outer ring plus `_EdgeBleed`, so it can be pulled a little past the edge (shown red) but no further. `TryDropRune(rune, position)` moves it if `CanDropAt` allows, unplugging from its old node and plugging into a new one if needed; otherwise nothing changes. Frozen runes can't be dropped, removed or unplugged, and frozen bridges can't be disconnected (the rules refuse, not just the UI). `RestoreBridges(rune, ids)` puts back bridges it had (used when a drop is refused).

**Loading:** bad saved entries (missing runes, repeated bridges, plugs into nodes the field doesn't know) are cleaned out first. With no settings asset, the defaults are used and a warning is logged.

**Effects:** `CompileEffects()` gathers everything the field does into one list of copies: the stat boosts of every powered rune (in placement order), then the effects of every active node (in the order they turned on). Empty or broken effects are skipped.

**Node snapshots:** `GetPluggedNodeSnapshots()` copies every node with a rune plugged in, powered or not. The slot saves them, so the rules can run again later without the field scene (level-ups and evolving, even mid-dungeon).

**Stats:** `GetStatTotals()` adds up every `StatChangeEffect` in the compiled list (`RuneEffect.AddUpStats`), so runes and nodes feed stats the same way. The [[Shade Manager]] adds up the slot's saved compiled list the same way to work out its stats.

| Function | Description |
| :--- | :--- |
| `TryPlaceRune(element, position, out id)` | Puts a new rune down if `CanDropAt` allows, snapping into a node if it's on one (no bridges yet) |
| `TryDropRune(id, position)` / `RemoveRune` | Move a dragged rune if the spot is allowed, or remove a rune (removing takes its bridges and plug with it) |
| `CanDropAt(id, position, out final, out node)` / `IsSpotFree(id, position)` | The footprint and snap checks. `NoRuneID` for a rune that isn't placed yet |
| `RestoreBridges(id, ids)` | Puts back bridges a rune had, without a reach check |
| `ConnectNearby(id)` | Bridges a rune to the closest things in reach (what dropping does) |
| `Connect` / `Disconnect` | Make or remove one bridge |
| `FindBridgeTargets(id, position)` | What a rune would bridge to if dropped here |
| `ClampToBridges` / `GetBridgeStretch` | Drag limits and tear pull |
| `FindNodeAt` / `CanSnapToNode` / `CanPlugInto` / `TryPlugRune` / `UnplugRune` | Ability node plugging |
| `IsNodeLockedOut(index)` / `IsNodeBlocked(index)` | Locked out by a plug / can't take a rune right now (outside the open zone or locked out) |
| `GetOpenZone()` / `GetZone(position)` / `IsInOpenZone` / `IsInsideField` | Zones |
| `IsRuneFrozen(id)` / `IsBridgeFrozen(a, b)` | Frozen checks |
| `GetPluggedGate()` / `GetEvolvingGate()` | The open zone's gate with a rune in it / that's on (the shade evolves through it). -1 if none |
| `FixOneWayLockouts(nodes)` / `CheckGates(nodes, settings)` (static) | Setup checks. Return the problems as text ("" if none) |
| `SetMaxPower` | Core power changed (shade level) |
| `CompileEffects()` | Every effect from powered runes and active nodes, as copies |
| `GetStatTotals()` | Adds up the stat changes in the compiled list |
| `GetPluggedNodeSnapshots()` | Copies of every node with a rune plugged in |
| `GetNode(index)` / `HasNode(index)` | Node lookup |

---
## Zones and Evolving
*Built Oct 9, 2026 (overhaul step 7).*

**Rings:** the field is `_ZoneCount` (4) rings around the core, one per rank (Bound, Unbound, Ascendant, Legend), each `_ZoneWidth` (300) wide. Ring k is k × width from the core and is zone k's outer edge. A spot's zone goes by its center, and a spot **on** ring k counts as zone k (within `RingTolerance`, 1 field unit), so a gate on ring 1 belongs to zone 1. Ring 4 is the field's edge; `_EdgeBleed` (100) is extra room past it for the background and dragging, but runes can't be placed there.

**The open zone:** a shade of rank R can only build in zone **R + 1** (Bound uses zone 1). Zones further out are **locked**; zones further in are **frozen**. Placing or dropping outside the open zone is refused (red), and nodes outside it show their Locked background.

**Frozen:** a rune is frozen if it's in a zone ≤ rank, a bridge if both ends are (the core counts as frozen). Frozen runes can't be picked up, removed or unplugged, frozen bridges can't tear, and frozen runes get power first. They can still take new bridges from the open zone if they have free slots. "Frozen" isn't saved, it comes from the rank. *No add-only option (decided Oct 9, not needed).*

**Gates:** a node with an Evolve effect. The shade evolves when a gate in the open zone is **on** (`GetEvolvingGate`). Gates in the same zone lock each other, so only one can hold a rune. After evolving, the taken gate stays on (its rune is frozen) and the zone's other gates are closed for good, since they're no longer in the open zone.

**When it evolves:** `ShadeManager.TryEvolve` runs after every save and every level-up, on the slot's saved field (snapshots, no scene needed, works mid-fight). See [[Shade Manager]].

**Confirm on save:** if the draft has a rune in a gate that the saved field didn't, saving asks first (`_EvolveTitle`, and `_EvolveNowMessage` if the gate already has power or `_EvolveLaterMessage` if not; `{0}` = form name, `{1}` = zone). Save goes ahead, Cancel keeps the draft. Saving again later doesn't ask again. Before the gate gets power, the rune can still be taken out.

**After evolving:** `ShadeManager.OnShadeEvolved` fires; the field reloads if it's the open slot (the new zone opens, the old one freezes) and runs `_OnEvolved`. A draft left open while the shade evolved some other way (the test level-up button) is reloaded; the Shade Manager also refuses to save a draft whose rank is out of date.

**Setup checks** (logged once when the scene builds its node list): one-way lockouts (fixed for the run), gates not on a ring, gates on the outer edge.

**Editor tools:**
- **Zone gizmos:** `RuneFieldManager` draws the rings around the core with labels, plus the edge bleed circle. Scene view always, game view while Gizmos is on, never in a build. In play mode the rings are colored frozen / open / locked and frozen runes get an outline (colors in its **Zone Gizmos** foldout)
- **Snap buttons** on `EvolutionNode` (**Zone Tools** foldout): **Snap To Next Ring Out** moves the node out to the closest ring past it, keeping its angle around the core; a node already on a ring goes up to the next one. **Snap To Ring** takes a ring number. Gate rings only (1 to `_ZoneCount` − 1). Ctrl+Z works
- Both find the settings asset in the project by themselves (`RuneFieldSettingsSO.FindInProject`), nothing to assign

*Feedback visuals (frozen runes, locked zones) are gizmos for now, until 2D vs 3D is decided* [[Notes for the future]]

---
## Effects
*Added Oct 9, 2026 (overhaul step 1).* An effect is one thing a rune field does to its shade. Works like the [[Ability System]]'s functions: a base class (`RuneEffect`) with small subclasses, held in `[SerializeReference]` lists so the inspector asks which type to add.

| Effect | Fields | What it does |
| :--- | :--- | :--- |
| `StatChangeEffect` | `_Stat`, `_Amount` | Adds a flat amount to one stat (negative takes it away). Used by nodes and by every rune's stat boosts |
| `GrantAbilityEffect` | `_Ability` (`AbilitySO`) | Gives the shade an ability |
| `EvolveEffect` | `_Evolution` (`ShadeEvolutionSO`) | Evolves the shade. **A node with one is an evolution gate** (`IsGate()`), there's no separate gate type |

- **Applying:** stat changes reach the slot's stats through `RuneEffect.AddUpStats`, Grant Ability effects go on the runtime entry's ability list (nothing uses that list yet [[Notes for the future]]), and Evolve effects are read by `ShadeManager.TryEvolve` (see **Zones and Evolving**)
- **Copies everywhere:** `Clone()` copies an effect (asset references still point at the same asset). Compiled lists, scene-built node lists and snapshots only hold copies, so changing them never changes a node or a rune asset
- **Setup checks:** `GetSetupProblem()` says what's wrong with an effect (no stat, 0 amount, no ability, no evolution). Broken effects are skipped when compiling, and nodes and element runes show the problems in an inspector warning, along with empty entries and nodes with more than one Evolve
- **Adding an effect type:** make a class that inherits `RuneEffect`, add `[System.Serializable]`, override `GetSetupProblem` if it can be set up wrong. Renaming one later breaks saved lists, same as ability functions (see [[Ability System]])

**Rune stat boosts** are a `StatChangeEffect` list on each `ElementItemSO` (`_StatBoosts`, read with `GetStatBoosts()`). It used to be `mypackage` (old `statBoostPackage` entries); `[FormerlySerializedAs]` keeps the values already set on element assets.

---
## RuneFieldSettingsSO
*Added Oct 9, 2026 (overhaul step 2), replacing `RuneFieldLayoutSO`.* Created from **Create → Rune Field → Settings**. One asset, `SO_RuneField_Settings` (in `Scriptable Objects/Rune Field`), assigned on the [[Shade Manager]]. If none is assigned, the defaults are used and a warning is logged.

| Variable | Description |
| :--- | :--- |
| `_CoreReach` | How far the core reaches to make a bridge (default 150) |
| `_CoreMaxBridges` | Max bridges on the core. 0 = no limit |
| `_CoreSize` | The core's footprint (default 100). Its own setting so the core can stand out as the centerpiece |
| `_EdgeBleed` | Extra room past the outer ring, for the background and dragging (default 100). Runes can't be placed there. *Replaced `_FieldRadius` on Oct 9* |
| `_NodeSnapRadius` | How close to a node's center a rune has to be dropped to plug in (default 70). Keep it smaller than `_RuneSize` |
| `_SnapStretch` | How far past their reach a rune's bridges may stretch when it snaps into a node (default 30) |
| `_ZoneCount` | How many zones (default 4, one per rank). The last ring is the field's edge |
| `_ZoneWidth` | Width of every zone ring (default 300) |
| `_RuneSize` | A rune's footprint (default 100). Nodes use it too |

**Zone math:** `GetRingRadius(ring)`, `GetFieldEdge()`, `GetLastGateRing()`, `GetZone(distance)`, `IsOnRing(distance, out ring)`, `GetNextRingOut(distance)`. `FindInProject()` (editor only) finds the asset for editor tools.

**Ability nodes aren't in here.** They're the node objects in the field scene. `RuneFieldManager` builds the rules' node list from them when a field opens, and each slot saves snapshots of the nodes it plugged into.

---
## The 2D View
### RuneFieldManager
Kept its name and fields, so the scene didn't need setting up again.

| Variable | Description |
| :--- | :--- |
| `_TearSlack` | How far past reach a rune can be pulled before its bridges start tearing (2) |
| `_InvalidColor` | Tint for a rune or inventory button dragged over a spot it can't go (red) |
| `_EvolveTitle` / `_EvolveNowMessage` / `_EvolveLaterMessage` | The evolve confirm popup's text (see **Zones and Evolving**) |
| Zone Gizmos foldout | `_OpenZoneColor`, `_FrozenZoneColor`, `_LockedZoneColor`, `_BleedColor` |
| `CorePointer` | The core object. It's the field's (0, 0) |
| `ListOfNodes` | Ability node objects. A node's spot in this list is its index, which saved plugs and snapshots point at, so *don't reorder it once fields are saved* |
| `ListOfRunes` | Rune objects on screen (runtime). Runes placed in the scene by hand are removed on start |
| `elementPrefab` | Rune prefab. Its `BridgePrefabRef` is used for bridges |
| `_Popup` | The [[Confirm Popup]] that asks Save / Discard / Cancel. Without one, unsaved changes are thrown away with a warning |
| `_SaveButton` | Optional UI save button. Greyed out while there's nothing to save. Its On Click should call `SaveRuneSlot` |
| `_OnLeave` | UnityEvent run when the player leaves (after any save/discard question). Hook the exit timeline here |
| `_OnEvolved` | UnityEvent run when the open shade evolves, after the field reloads. Hook evolve effects or a timeline here |
| `_HasUnsavedChanges` | True while the draft has unsaved changes (read only) |

- **Start:** removes hand-placed test runes and opens the selected slot
- **`LoadRuneField(index)`:** opens a slot's saved field as a copy (with the slot's rank), without asking. It gets the settings from the [[Shade Manager]], and the first time it builds the node list from `ListOfNodes` (position, unlocks, lockouts, a copy of the effects) and runs the setup checks
- **`SaveRuneSlot()`** (Odin **Save Current Field**, and the UI save button): hands the field to the [[Shade Manager]], which changes the inventory by the difference, writes the field, its compiled effects and its node snapshots onto the slot asset and rebuilds the slot's runtime entry. Then the saved field is loaded again so the draft starts over. If the save is refused, the draft stays as it is. A newly plugged gate asks first (see **Zones and Evolving**)
- **`DiscardChanges()`** (Odin button) and **`ClearRuneField()`** (Odin **Test Clear**, counts as an unsaved change)
- **`PlaceRuneFromInventory(element, position)`:** called by `ElementDataObject` when an element is dropped on the field. Refused if none of that rune are available (`GetAvailable`). Otherwise places it, plugs it if it's on a node, then bridges it
- **Dragging:** `BeginRuneDrag` refuses frozen runes, otherwise remembers the rune's bridges. `DragRune` moves only the rune's **view**: clamped to its bridges, shown where it would land (snapped into a node if it's on one), tinted `_InvalidColor` over a bad spot, and driving tearing. `EndRuneDrag`: on the inventory list the rune comes off the field (anything under the `IngredientListWindow` counts, no setup needed); on an allowed spot it moves there and bridges to what's in reach; anywhere else it goes back with any bridges torn during the drag put back, as if the drag never happened (the unsaved flag goes back too). The rune stays plugged during the drag, so its node doesn't flicker off
- **Drawing:** every `OnFieldChanged` syncs the rune objects, rebuilds the bridges, fades unpowered runes, updates node backgrounds and the core's power. Node events only fire on changes while editing, not when a field loads
- **`UpdateScaler()`:** still called by the zoom script but does nothing now. Everything is a child of the field and the rules don't use colliders

#### Draft and Saving
*Added Oct 9, 2026 (overhaul step 4).* The open field is a **draft**. Nothing leaves the inventory or reaches the slot until the player saves.
- **Unsaved changes:** placing, dragging, tearing and removing runes mark the draft as changed. Loading, saving and discarding clear it
- **Pending runes:** `GetPendingUse(element)` = how many of that rune the draft has minus how many the saved field has. `GetAvailable(element)` = the inventory count minus the pending use. With no Inventory Manager (testing the scene on its own) there's no limit. *Only one draft exists for now; with more drafts later, pending use would add up every open draft*
- **Switching slots:** `SelectSlot(index)` (the slot buttons) asks **Save / Discard / Cancel** if there are unsaved changes. Save only switches if the save worked. Cancel stays on the current slot
- **Leaving:** `RequestLeave()` (a close button, and **Escape**, the UI map's Cancel action) asks the same question, then runs `_OnLeave`. Escape closes the popup instead if one is open
- **Opening/closing:** meant to be driven by [[Timeline Runner System|timelines]]: the open timeline shows the field, a close button or Escape calls `RequestLeave`, and `_OnLeave` plays the exit timeline. *No timelines are hooked up yet* [[Notes for the future]]
- **Evolve confirm:** saving with a newly plugged gate asks first, with the same popup. From the Save / Discard / Cancel question, Save goes through this too and only continues once the save happens

**Unity setup still to do** [[Notes for the future]]:
1. Under the field's canvas, add an empty object with `ConfirmPopup` and press **Build Default Layout** (see [[Confirm Popup]]). Drag it into `_Popup` on `RuneFieldManager`
2. Add a save button: On Click → `RuneFieldManager.SaveRuneSlot`, then drag it into `_SaveButton`
3. Add a close button: On Click → `RuneFieldManager.RequestLeave`, and hook `_OnLeave` to whatever closes the field

### ElementItem (runes)
Set up by the manager with its rune ID and element. Shows power (`RequiredPower` / `CurrentPower`, read only) and fades to `_UnpoweredAlpha` (0.45) without power, using a `CanvasGroup` it adds if the prefab doesn't have one. Drags go straight to the manager. `ShowInvalid` tints its `_TintTarget` (the root image if left empty) while it's over a bad spot.

### NodeBridge
Draws the line between its two ends. While a rune is pulled past reach, `StartTearing(pull)` runs a timer that fills faster the harder it's pulled; when full it calls `RuneFieldManager.TearBridge`, which removes it from the field. Settings: `PullTearRequiredTime`, `MaxPullStrength`, `pullTickTimeLength`, `pullStrengthModifierDampening`.

### CoreNode
Shows the core's power (`CoreNodeCurrentPower` = left, `CoreNodeMaxPower`). Optional `_PowerLabel` text shows "left / total".

### EvolutionNode (ability nodes)
Shows its state with the scene's state backgrounds: **Enabled** while on, **Locked** while it can't take a rune (outside the open zone or locked out), none otherwise. Fires `ActivationEvent` / `DeactivationEvent` when it switches while the player is editing. Its `unlocks` and `Lockouts` lists are read when the node list is built from the scene.

**Effects:** what the node does is its `_Effects` list (see **Effects** above), filled in on the node object in the scene. Building the node list from the scene copies it into the node's `AbilityNodeEntry`. Shows an inspector warning for broken effects. `IsGate()` is true when the list has an Evolve effect.

**Zone Tools** foldout: **Snap To Next Ring Out** and **Snap To Ring** (see **Zones and Evolving**).

*The "may delete" fields and the Enable/Lock/Hide buttons are from an older version of the node states.*

---
## Inventory Side Panel
- `IngredientListWindow` shows one `ElementDataObject` button per element rune in the inventory (the ingredient version is commented out). It refreshes when it's enabled, on the [[Inventory Manager]]'s `OnInventoryChanged`, and on `RuneFieldManager.OnDraftChanged` (the field is read from its existing `RuneFieldPointer`, so no new setup)
- Dropping an `ElementDataObject` on the object tagged **UI Drag Field** calls `RuneFieldManager.PlaceRuneFromInventory`. Runes only leave the inventory when the field is saved (see **Draft and Saving**)
- **Counts** *(overhaul step 5, Oct 9, 2026)*: each button shows the inventory count (`_CountText`, `_CountColor`, black by default) and how many the draft is using on top of the saved field (`_PendingText`, `_PendingColor`, brighter, written with `_PendingFormat` = `-{0}`, hidden at 0). With none left to place it fades to `_UnavailableAlpha` (0.4) and can't be dragged, but stays in the list. A rune only leaves the list when a save takes the inventory to 0. While dragging a button over the field, it tints with the field's `_InvalidColor` over a spot where the rune can't go (`_TintTarget`, the root image if left empty)
- Runes the draft took off whose inventory count is 0 aren't listed until the save puts them back (decided Oct 9)
- **Setup:** open the button prefab (`IngredientListWindow.ButtonListPrefab`) in prefab mode, select its root and press **Build Count Labels** on `ElementDataObject`. It adds the two texts on the right edge; move and restyle them as you like [[Notes for the future]]
- `SideWindowManager.CloseWindow(window)` slides the side panel open/closed and switches windows

## Shade Slots
`ShadeSlotManager` shows one button per unlocked slot and calls `SelectSlot(index)` when one is clicked (it asks about unsaved changes first). It currently uses the player's level from [[Game Manager]] to decide how many slots are unlocked.

---
## Leftovers
- `DraggableItem`, `ElementObject` and `ShadeSlotDataObject` are small early scripts that don't do anything
- In `EvolutionInterfaces.cs`, `IConnectable`, `ICoreNode` and `iEvolutionNode` aren't used anymore. `IBridgeable` isn't used by the rune field but [[Dungeon Map Node]]s still use it, and the dungeon map draws its lines with `NodeBridge.BuildConnection` / `UpdatePosition`
- *Removed Oct 9, 2026 (overhaul step 2):* `RuneFieldPackage` (old save format), `ShadeSO._RuneFieldPackage`, `_AlteredStats` and `_shadeStats`, and `RuneFieldLayoutSO`. The emptied `RuneFieldPackage.cs` and `RuneFieldLayoutSO.cs` were deleted
- *Removed Oct 9, 2026 (overhaul step 1):* `ElementManagerSO` / `statBoostPackage`, the element runes' `statusEffectEnable/Disable` events and `TriggerElementEffects` / `DeactivateElementEffects`, and the Shade Manager's `managerScriptableObject`, `shadeAlterPackages`, `ReceiveStatBoostPackage` / `RemoveStatBoostPackage` / `ChangeStat`

All tracked in [[Known Issues]] (Cleanup).
