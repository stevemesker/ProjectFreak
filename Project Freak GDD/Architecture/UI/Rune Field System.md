## Overview
The code behind the [[Rune Field]], the UI where the player drags [[Element Rune]]s out, chains them to the [[Core Node]] for power, and plugs them into [[Ability Node]]s to upgrade the active [[Shade]]. Scripts live in `Scripts/UI/Evolution Tree`.

```text
Inventory list (ElementDataObject)
    │ drag onto field
    ▼
ElementItem (rune on the field) ──bridge── ElementItem ──bridge── CoreNode
    │ dropped on
    ▼
EvolutionNode (ability node)
    │ runes powered by the core apply their stat boosts
    ▼
ElementManagerSO → ShadeManager (stats on the active ShadeSO)
```

| Script               | Role                                                                       |
| :------------------- | :------------------------------------------------------------------------- |
| `RuneFieldManager`   | Owns the field. Tracks runes and nodes, saves/loads the layout             |
| `CoreNode`           | The power source                                                           |
| `ElementItem`        | A rune on the field. Dragging, bridging, and passing power                 |
| `NodeBridge`         | The line between two connected pieces. Also handles tearing                |
| `EvolutionNode`      | An [[Ability Node]] a rune can plug into                                   |
| `ElementManagerSO`   | Link between the UI and the [[Shade Manager]] for stat boosts              |
| `IngredientListWindow` / `ElementDataObject` | The inventory list runes are dragged from          |
| `ShadeSlotManager`   | Shade slot buttons that load a slot's field                                |
| `SideWindowManager`  | Opens/closes side panel windows                                            |

The interfaces used here (`IBridgeable`, `IConnectable`, `ICoreNode`, `iEvolutionNode`) are listed in [[Interface List]].

---
## RuneFieldManager

| Variable        | Description                              |
| :-------------- | :--------------------------------------- |
| `CorePointer`   | The `CoreNode` object                    |
| `ListOfNodes`   | Every ability node on the field          |
| `ListOfRunes`   | Every rune currently placed              |
| `elementPrefab` | Rune prefab used when loading a layout   |

| Function                         | Description                                                                   |
| :------------------------------- | :---------------------------------------------------------------------------- |
| `addRuneList` / `removeRuneList` | Track placed runes                                                            |
| `ResetRunePower()`               | Sets every rune's power to 0                                                  |
| `ResetRuneChecked()`             | Clears the "already checked" flag used while passing power around             |
| `UpdateScaler()`                 | Resizes colliders when the field is zoomed out                               |
| `loadRuneField(index)`           | Clears the field, selects that shade slot, and loads its saved layout         |
| `SaveRuneSlot()`                 | Saves the current layout onto the active shade slot (Odin **Save Current Field** button) |
| `ClearRuneField()`               | Removes all runes, resets the core and every ability node (Odin **Test Clear** button) |

### Saving and Loading
The layout is stored as a `RuneFieldPackage` on the shade's `ShadeSO` (see [[Shade (Runtime)]]):

| Package        | Holds                                                                              |
| :------------- | :--------------------------------------------------------------------------------- |
| `RunePackage`  | The rune's `ElementItemSO`, its position (without zoom), current power, and the indexes of the runes it's connected to (`-1` = none) |
| `NodePackage`  | Which ability node, which rune is plugged in, and whether it's powered             |
| `CorePackage`  | Core's current power and the indexes of runes connected to it                      |

Connections are saved as **list indexes** into `ListOfRunes`, so loading spawns every rune first, then reconnects them by index. The core's max power is set from the shade's `_LVL`.

---
## Power
The [[Core Node]] has `CoreNodeMaxPower` (the shade's level) and `CoreNodeCurrentPower`.

When a rune connects to a chain that reaches the core, the core runs `ConsumePower()`:
1. Reset every rune's checked flag
2. For each rune connected to the core, `ConsumePower()` on it
3. Each rune takes `RequiredPower` from the core. If it gets enough, it turns on its element's effects, then passes the call to its own connections
4. The checked flag stops the same rune being processed twice

If the core runs out, runes further down the chain stay unpowered.

**Disconnecting:** when a bridge is torn, both sides run `SearchCore()` to see if they can still reach the core. Any side that can't runs `DisconnectNodeTree()`, which returns its power to the core, turns off its effects, and does the same down its chain. Then the core runs `ConsumePower()` again to re-power what's still connected.

---
## ElementItem (runes on the field)

| Variable              | Description                                                           |
| :-------------------- | :-------------------------------------------------------------------- |
| `_ElementAttached`    | The rune's `ElementItemSO`                                            |
| `Range`               | How far it can reach to connect (default 100)                         |
| `connectionsMax`      | Max bridges (default 2)                                               |
| `connectionsCurrent`  | What it's connected to                                                |
| `CoreNode`            | The core it's powered by (null if not connected)                      |
| `RequiredPower` / `CurrentPower` | Power needed / power it has                                |
| `pluggedNode`         | Ability node it's plugged into                                        |

**Dragging**
- While dragging, the rune looks for nearby runes/nodes it can bridge to
- If it's already bridged, it can only move within reach of its connections. `calculatePointerPosition` clamps the position
- Pulling past that reach starts **tearing** the bridge (see below)
- Hovering an ability node snaps the rune onto it
- On release it builds bridges to what's in range and, if over an ability node, plugs in and tries to activate it
- Starting a drag unplugs it from any ability node

**Element effects:** when powered it calls `triggerElementEffects` on its `ElementItemSO`, which fires the SO's `statusEffectEnable` UnityEvent (usually hooked to `ElementManagerSO.boostStats`). Losing power fires `statusEffectDisable`.

*Note:* `ElementItemSO` has `connectionsAllowed`, `connectionDistance`, and `powerNeeded`, but they aren't copied onto the rune yet, so every rune uses the prefab's values.

---
## NodeBridge
The visual line between two pieces.
- `BuildConnection(a, b)` and `updatePosition(length)` place and stretch it
- **Tearing:** while a rune is pulled past its reach, `StartTearing` ticks a timer that fills faster the harder it's pulled. When it fills, `SeverConnection` disconnects both sides, re-checks power (see above), and destroys the bridge. Letting go calls `StopTearing`
- Settings: `PullTearRequiredTime`, `MaxPullStrength`, `pullTickTimeLength`, `pullStrengthModifierDampening`
- `clearConnections()` removes the bridge without the power checks (used when clearing the field)

---
## EvolutionNode (ability nodes)

| Variable            | Description                                                   |
| :------------------ | :------------------------------------------------------------ |
| `_ActivationState`  | Is it active                                                  |
| `_LockedOut`        | Locked by another active node                                 |
| `PluggedInNode`     | Rune plugged into it                                          |
| `unlocks`           | Nodes that must be active before this one can activate       |
| `Lockouts`          | Nodes this one locks while active                             |
| `ActivationEvent` / `DeactivationEvent` | UnityEvents for what the node actually does |

A plugged node activates if the rune has power, it isn't locked out, and every node in `unlocks` is active. Activating locks its `Lockouts`; unplugging unlocks them and fires `DeactivationEvent`. This matches the lockout types in [[Ability Node]].

*The fields under "may delete" (`nodeID`, `connectedNodes`, `NodeEnabled`, `Nodelocked`) and the Enable/Lock/Hide buttons are from an older version of the node states.*

---
## ElementManagerSO
A ScriptableObject both the UI and the [[Shade Manager]] can reference. The Shade Manager registers itself in `manager` on enable.
- `boostStats(ElementItemSO)` / `reduceStats(ElementItemSO)` send the element's `statBoostPackage` list to the Shade Manager

`statBoostPackage`: the linked element, the rune object, which stat (`StatNameType.Stat`), and the amount.

---
## Inventory Side Panel
- `IngredientListWindow` listens to the [[Inventory Manager]]'s `OnInventoryChanged` and shows one `ElementDataObject` button per element rune (the ingredient version is commented out)
- Dragging an `ElementDataObject` onto an object tagged **UI Drag Field** spawns a rune on the field
- `SideWindowManager.CloseWindow(window)` slides the side panel open/closed and switches windows

## Shade Slots
`ShadeSlotManager` shows one button per unlocked slot and calls `loadRuneField(index)` when one is clicked. It currently uses the player's level from [[Game Manager]] to decide how many slots are unlocked.

---
## Leftovers
`DraggableItem`, `ElementObject`, and `ShadeSlotDataObject` are small early scripts that don't appear to do anything now.

See [[Known Issues]] for bugs found in these scripts.
