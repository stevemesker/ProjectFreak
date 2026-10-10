## Overview
The plan for the next big pass on the [[Rune Field]], plus the [[Shade]] death / burn-down rules it depends on. Worked out with Claude in Oct 2026. Built pieces get written up in [[Rune Field System]] and their tags come off here.

Back to [[AA - AI Info]] · [[Architecture Atlas]]

**Status (Oct 8, 2026):** architecture talked through and written up below (**Architecture**). The code written on Oct 8 (the `RuneField` logic layer and the 2D UI hooked up to it) is a **draft**: parts of it fit, parts move. Nothing else gets changed until this plan is signed off. [[Notes for the future]]

**Oct 9, 2026:** build order steps 1 (effect types), 2 (slot data, evolution asset, settings asset), 3 (runtime entries, health, level-ups), 4 (save flow), 5 (inventory list counts), 6 (rune footprints) and 7 (zones and evolving) are built, see [[Rune Field System]], [[Shade (Runtime)]] and [[Shade Manager]]. Step 8 (3D prototype) is still to do [[Notes for the future]]

---
## Architecture
*Agreed Oct 8, 2026.*

### Three layers of stats
| Layer | What's in it | Lives on | Changes when |
| :--- | :--- | :--- | :--- |
| **Base** | The evolution's stats, set by hand per evolution (like rolling stats for a character). No multiplier math | `ShadeEvolutionSO` (`_BaseStats`) | Never at runtime |
| **Hard stats** | Base + the slot's compiled rune field effects (runes, ability nodes) + core fragment | Worked out by the [[Shade Manager]] from the slot | The rune field is saved, the shade evolves, a shard is slotted, the game loads |
| **Live stats** | Hard stats + timed effects (buffs, debuffs, potions) + equipment, and current health | The Shade Manager's runtime entry for that slot | Every moment in a dungeon |

### The shade slot (`ShadeSO`) is the source of truth
- One asset per slot holds everything long term about that shade: level, lives, its evolution and which gate it evolved through, its fragment, its saved rune field, and the rune field's **compiled effect list**
- **Changes are allowed to stick in the editor on purpose** (handy for playtests). This is a deliberate exception to "don't change SO data at runtime", for shade slots only. No new game reset for now, add one only if a build needs it
- The slot only changes at deliberate moments (saving the rune field, leveling, evolving, slotting a shard, dying), never from moment to moment gameplay
- Base stats come from the current evolution, so the duplicate stats on the slot shrink to what's personal to that shade (level, lives, etc.)
- The disk save reads the slots later. Asset references (evolution, rune elements) will need IDs then; not built until a system needs it

### The Shade Manager holds the runtime side
- One **runtime entry per slot**: hard stats (the "interpreter" result), live stats, current health, and (later) timed effects. Shown in the inspector until there's a stats UI
- A summoned shade **points at** its slot's runtime entry instead of copying it, so returning or switching shades loses nothing: health and timed effects carry on
- **Timed effects (good and bad) keep ticking while the shade is put away**, so re-summoning can't shake off a debuff. They tick on a **game clock** that only runs during live gameplay (not paused, not loading, not in menus like the rune field). They clear on entering the hub. *Exception idea:* legendary potions that last through the hub [[undecided]]
- **Current health** refills on entering the hub, at the rest floor refill, or from healing items/abilities. When max HP changes, current HP moves by the same amount, and that can kill the shade (even while put away, handled with the [[Fail State]] work)

### The rune field saves a compiled list
- While editing, the field is a **draft** (a working copy). Nothing is sent anywhere until the player saves
- **On save,** the field gathers everything active (stat boosts from powered runes, effect lists from active ability nodes) and sends the whole list up. The slot's list is **replaced**, never added to, so stats can't drift. Then the Shade Manager rebuilds that slot's runtime entry
- So the slot stores: **placements and connections** (for the UI to rebuild the field), the **compiled effect list** (for the Shade Manager), and a **snapshot of every plugged node** (its effects, unlock/lockout links, and whether it's a gate), powered or not. Loading the game just reads the list; the Shade Manager never needs the field scene or its nodes
- **Leveling up re-runs the rules on the saved field** (the rules are plain C#, so they run without the field scene, using the node snapshots). Runes that were waiting for power can light up, nodes they reach switch on, and the compiled list and hard stats are rebuilt (current HP moves with max HP). This happens wherever the shade is, including mid-dungeon
- *Trade-off:* if a node's effects are changed in the editor, slots already saved keep the old list until that field is saved again (could also re-compile whenever a field is opened)
- **One draft at a time.** Switching slots or leaving with unsaved changes asks "save or discard?". Pending rune use is counted across drafts from the start so multiple drafts can be added later if wanted

### Ability nodes and effects
- **Nodes are generic:** one node script for every node. A node is an evolution gate simply because its effect list has an **Evolve** effect, so there's no separate gate type to keep in sync
- Nodes are objects in the field scene, wired up by hand (unlocks/lockouts are references to other node objects, so they're easy to follow in the inspector and don't break when reordered)
- Each node holds a **list of effects** picked from a small set of types, like the [[Ability System]]'s steps: **stat change**, **grant shade ability** (`AbilitySO`), **evolve** (`ShadeEvolutionSO`), more later (Hazen stats, element affinity). Rune stat boosts compile into the same stat change type
- Saved plugs point at a node by its spot in `ListOfNodes`. *Don't reorder that list once fields are saved* (revisit if it ever causes trouble)
- **Lockouts trigger on plugging, not power.** Once a rune is plugged into node X, every node X locks out is locked, even if X has no power yet. A locked-out node won't let a rune snap in (same red "can't place" feedback). Runes go in one at a time, so the first node plugged always wins and two rival gates can never both hold a rune
- **Lockouts are always two-way.** When the field sets up, any one-sided lockout (X locks Y but Y doesn't list X) gets its missing reverse link filled in so the game behaves correctly, and a `Debug.LogError` names both nodes so the scene can be fixed. Checked once at setup, not every time a node is checked. *One-way lockouts could be added as an option later if ever needed*
- **Unlocks stay power-based** (Y needs X on first). They're about progression, not branch choice

### Shared settings asset
- The global rune field numbers live in **one small settings asset** used by both the field scene and the [[Shade Manager]]: **zone width**, **rune size** (footprint) and **core reach**. The rules need them even without the field scene (level-ups in a dungeon need the zone edges for "frozen runes get power first"), and it gives one place to tune them
- Only the shared numbers. Nodes stay as scene objects

### Evolutions (`ShadeEvolutionSO`)
- **Making an evolution = filling in one asset; the system handles the rest.** The asset is the complete definition of a form:
  - **Stats:** its base stats
  - **Art:** tethered and released art (plus any evolve effect/VFX later)
  - **Body:** tether distance and width, size class (collisions, AI)
  - **Abilities:** its **ultimate ability** (`AbilitySO`), its **natural abilities** (list of `AbilitySO`), and **capabilities** like "can hold weapons". Capabilities are probably checkboxes (traits the game checks, not abilities the shade uses), or they come from the evolution type (every biped can hold weapons). Decided when built [[undecided]]
- **No element on the evolution, on purpose.** A shade's affinity comes only from the player's runes, so the form a player likes never pushes them toward an element and every evolution stays viable
- *No identity section (name, rank, type, icon) for now*
- Anything personal to one shade (level, lives, runes, fragment) stays on the slot. Evolve effects on nodes point at the target evolution asset, and evolving swaps the slot over to it
- Warns in the inspector about missing pieces (no released art, no stats set, etc.)

### Runes
- **Fixed rune types.** Every rune asset is a set recipe, made on purpose. Randomness comes from how runes are obtained (a future system), not rolled stats
- **All runes are the same size** and have a footprint. A rune can't be placed or dropped where it overlaps another rune, the core, or an ability node (frozen runes count). The only way onto a node is snapping to its center, and snapping is allowed to stretch a bridge slightly past its reach (like the old version). A bad spot is **denied** and shown with color/shader, nothing gets pushed into a gap. A dragged rune goes back to its last valid spot; a new rune from the inventory just isn't placed

### Inventory
- The inventory holds runes that **aren't on a saved field**. In the rune field list each rune shows its count (black) and how many the current draft is using (brighter). At 0 available it stays in the list, greyed out, so it can still be looked at
- **On save** the inventory changes by the difference between the old and new saved field: added runes come out, removed runes go back. A rune at 0 leaves the list then
- When a shade dies its runes are already out of the inventory, so nothing extra happens. A breakdown adds some back

### Zones and evolution
- The field is **rings (radius zones) around the core**, the same for every shade. A Bound shade can only use zone 1
- **Every ring is the same width** (one setting in the shared settings asset): zone 1's edge is 1 × width from the core, zone 2's is 2 × width, and so on. Outer rings have more room, so more branching options
- **Snap tool:** an Odin button on the node with a "which zone edge" number (plus **Snap To Next Ring Out**, which picks the next ring past the node by itself). It keeps the node's angle around the core and moves it exactly onto that ring, so every gate on a ring is the same distance from the core (no gate can accidentally be closer and become a faster-evolving meta). Works in field units, so it's the same for 2D or 3D
- **The scene view draws the zone rings** so the layout is visible while placing nodes. No tool for keeping normal nodes inside their zone, the art pass will make the borders obvious
- **Setup check (once, like lockouts):** a gate that isn't sitting on a ring edge (nudged after snapping) logs a `Debug.LogError` naming it
- **Evolution nodes are the gates** on the edge of each zone. **A shade evolves when the rune on a gate gets power**, either right when the field is saved, or later when a level-up gives the core enough power (see "Leveling up" above)
- **The confirm popup is at save time.** Saving a field with a rune on a gate asks something like "Your shade will evolve as soon as it has enough power, locking zone 1 for good. Continue?", so a later evolution is never a surprise. Before the gate gets power, the player can still unplug that rune
- **Evolving can happen mid-fight** (a big moment): the shade's model, stats and max HP change on the spot
- Evolving **freezes everything in the zone behind it**: runes, bridges and plugs can't be moved, removed, unplugged or torn. The next zone opens. The other gates on that ring lock for good (the branch is chosen). Devolving or switching branches is impossible
- Frozen runes **can still take new bridges** if they have free slots, so the next zone can be powered through them (leaving free slots near the border is part of the planning)
- "Frozen" isn't saved separately: a rune is frozen if it sits in a zone the shade has evolved out of (zone number ≤ rank, so after the first evolve zone 1 is frozen)
- **Frozen runes always get power first.** The power pass powers every frozen rune before anything else (still working outward from the core), then hands what's left to everything else by fewest bridges. Otherwise a new rune bridged close to the core could take power from the frozen chain and cut the gate off, which would be a devolve. It's safe because the frozen chain was fully powered when the shade evolved, its cost never changes, frozen bridges can't tear, and core power only goes up (death resets everything). *The only way around it is changing a rune's power cost in its asset after a shade has evolved, which is development only*
- ~~**Add-only option:** a setting that lets inner zones take new runes after evolving (never remove)~~ *Dropped Oct 9, 2026: strict only. Add it later if playtests ask for it*
- Dying resets to Bound, so the zones lock again (the field is wiped anyway)

### What happens to the Oct 8 draft code
| Piece | Keep / change |
| :--- | :--- |
| `RuneField` (rules) | Keep. Add zones, frozen runes, rune footprints/overlap, the looser snap, and effect compiling |
| `RuneFieldData` | Keep as the slot's "placements and connections" |
| `RuneFieldLayoutSO` | Becomes the **shared settings asset** (core reach, zone width, rune size). The node list comes out: node info comes from the field scene's node objects, and the Shade Manager reads the compiled list. *Done Oct 9 (step 2): `RuneFieldSettingsSO`, nodes are `AbilityNodeEntry` lists built from the scene or saved as snapshots* |
| 2D view (`RuneFieldManager`, `ElementItem`, `NodeBridge`, `CoreNode`, `EvolutionNode`) | Keep as the view for now. `EvolutionNode` gets an effect list. Draft/save flow, popups, overlap feedback added |
| `ShadeManager` slot rune fields (`_SlotRuneFields`, `ShadeSO._StartingRuneField`) | Move onto the slot asset itself. *Done Oct 9 (step 2): `ShadeSO._RuneField`* |
| `ShadeManager` slot stats (`_SlotStats`) | Becomes the per-slot runtime entry (hard stats, live stats, current health) |
| Old stat path (`ElementManagerSO`, `_AlteredStats`, `ChangeStat`, element `statusEffectEnable/Disable` events) | Remove. *Done Oct 9 (steps 1 and 2)* |

### Build order
1. ~~**Effect types** (stat change, grant ability, evolve) and effect lists on nodes; rune stat boosts compiled to the same type~~ *Built Oct 9, 2026 (`RuneEffect` types, `RuneField.CompileEffects`), plus removing the old stat path*
2. ~~**Slot data on `ShadeSO`**: saved field, compiled effect list, plugged node snapshots, evolution + gate, level, lives, fragment. Base stats from the evolution. **Evolution asset** filled out (stats, art, body, abilities). **Shared settings asset**~~ *Built Oct 9, 2026. Saving already writes the field, compiled list and snapshots onto the slot (the rest of the save flow is step 4). Capabilities ("can hold weapons") skipped until a system needs them* [[undecided]]
3. ~~**Shade Manager runtime entries**: hard stats, live stats, current health and the max HP rule, visible in the inspector. Level-up re-runs the saved field's rules~~ *Built Oct 9, 2026. Every layer stays `ShadeStats` (one shared character sheet), live `_Health` is current health. Also added: the abilities list on the entry, shade forms pointing at their entry, and the `ShadeManagerWrapper`*
4. ~~**Save flow**: draft, save compiles and writes to the slot, inventory difference, runtime entry rebuilt, one draft with the save/discard popup, UI save button~~ *Built Oct 9, 2026. Also added: taking runes off the field (drop on the inventory list), the reusable [[Confirm Popup]], and `RequestLeave` / Escape / `_OnLeave` for the timeline open/close. The buttons and popup still need adding in the scene*
5. ~~**Inventory list**: count, pending count, greyed at 0~~ *Built Oct 9, 2026. Runes the draft took off with 0 in the inventory aren't listed until saved*
6. ~~**Rune footprints**: overlap check, invalid feedback, looser snap~~ *Built Oct 9, 2026. The core has its own `_CoreSize`. A refused drop puts the rune back as if the drag never happened, including bridges torn during the drag*
7. ~~**Zones**: rings (one zone width), snap tool and ring gizmos, locked zones, gates, evolve when the gate gets power (on save or level-up) with the confirm at save time, freezing, add-only toggle, plug-based two-way lockouts~~ *Built Oct 9, 2026. Also decided: `_ZoneCount` (4, one per rank) with the last ring as the field's edge and `_EdgeBleed` past it (replacing `_FieldRadius`); gates in the same zone lock each other automatically; a **Snap To Next Ring Out** button; one shared Bound form on the Shade Manager for Reset Slot; gizmos instead of field visuals for now. No add-only toggle*
8. **3D prototype** (see The View)

*Later:* timed effects and the game clock (with the buff/debuff system), core fragments, the disk save.

---
---
## The View: 3D, one camera
The field goes **2.5D**: the rules stay flat (runes sit on a plane, positions are `Vector2`), but everything is drawn in 3D. Node models, glowing bridges, power pulses along chains, shards orbiting the core, and dragging a shard into the core to slot it.

**Minimum spec is Steam Deck** (no Switch). The project is on HDRP, where every camera runs the full pipeline and camera stacking isn't supported, so:
- The field is a small 3D space, loaded additively when the menu opens. The Cinemachine camera blends to it, the game pauses, and input switches to the menu map. Only the field is drawn while it's open
- **No second camera or render texture** (that would roughly double the rendering cost)
- The hub/dungeon is never touched. Hazen stands where he was. Opening builds the view from the slot's data, closing saves or throws away the data and unloads the view, so there's no scene state to restore. Works the same in dungeon safe rooms
- Dragging = raycast from the cursor onto the field's plane. Works for a mouse or a gamepad virtual cursor
- Zoom moves the camera instead of scaling the field (no more collider scaling bug)
- The camera looks down at the plane (straight or a slight tilt) so distances and reach stay easy to read

Not built yet [[Notes for the future]]. The dungeon map stays 2D for now.

**Field feedback visuals** (how powered vs unpowered runes, bridges, nodes and "can't place" look) get planned once the field works and 2D vs 3D is decided. For now unpowered runes just fade to 45% (`ElementItem._UnpoweredAlpha`) [[Notes for the future]]

---
## Logic Layer
*This section describes the Oct 8 draft as written. Where it disagrees with **Architecture** above (layout asset, snapping, stats on the Shade Manager), Architecture wins.*

*Written Oct 2026 and hooked up the same night: the 2D UI scripts (`ElementItem`, `CoreNode`, `NodeBridge`, `EvolutionNode`, `RuneFieldManager`) are now just a view over it, and saving a field works the slot's stats out in the [[Shade Manager]]. Full write-up in [[Rune Field System]].* Scripts are in `Scripts/Rune Field`.

| Script | What it is |
| :--- | :--- |
| `RuneFieldData` | The saved state of one slot's field: runes (ID, element, position), bridges, plugs, active nodes. Plain data. `Clone()` makes a full copy for the save button |
| `RuneFieldLayoutSO` | What every slot shares: core reach, core max bridges, field radius, and the ability nodes (position, snap radius, unlocks, lockouts). Warns in the inspector about bad node indexes |
| `RuneField` | The rules. A plain C# class made with `new RuneField(data, layout, maxPower)`. The view calls it and redraws when `OnFieldChanged` fires |
| `RuneFieldLogicTester` (Dev Scripts) | Odin button that runs the rules through test fields and logs pass/fail |

**Rules:**
- **Field units:** positions use the old UI's scale (a node is about 100 wide), so existing `ElementItemSO` values still make sense. The core is at (0, 0). The 3D view decides how big a field unit is in the world
- **IDs:** each rune gets an ID that's never reused, so bridges and plugs point at IDs instead of list indexes (removing a rune can't shift anything). The core's ID is `RuneField.CoreID` (-1)
- **Bridges:** allowed if neither end is full and the distance is within the bigger of the two reaches. Runes use their `ElementItemSO`'s `connectionDistance`, `connectionsAllowed` and `powerNeeded` (finally applied, see [[Known Issues]]). Dropping a rune bridges it to the closest things in reach, up to its free slots
- **Power:** handed out in layers by **fewest bridges from the core**; inside a layer, the rune placed first goes first. A rune the core can't afford stays dark and power doesn't flow through it, but cheaper runes elsewhere still get power
- **Ability nodes:** on when a powered rune is plugged in, every unlock is on, and no active node locks it out. Nodes already on keep their spot, so when two nodes lock each other the first one stays the winner even when something unrelated changes. Active nodes are saved
- **Dragging:** `ClampToBridges` keeps a rune within reach of all its bridges, and `GetBridgeStretch` says how far past reach a bridge is being pulled (the view runs the tear timer). Moving a plugged rune unplugs it. A rune only snaps into a node if snapping wouldn't overstretch its bridges
- **Loading:** `RuneField` cleans out bad saved entries (missing runes, repeated bridges, plugs into nodes that don't exist) before working anything out
- **Stats:** `GetStatTotals()` adds up the boosts from every powered rune. The minimum of 1 per stat and Hazen's stats get added when it's hooked up [[Notes for the future]]

**Main functions:** `TryPlaceRune`, `MoveRune`, `RemoveRune`, `ConnectNearby`, `Connect`, `Disconnect`, `FindBridgeTargets`, `ClampToBridges`, `GetBridgeStretch`, `FindNodeAt`, `CanSnapToNode`, `TryPlugRune`, `UnplugRune`, `SetMaxPower`, `Recalculate`, `GetStatTotals`, plus getters for power, connections, plugs and node states.

**Not covered yet:** applying what ability nodes do (granting abilities, evolving; their effect lists exist since Oct 9), core fragments, Hazen's stats [[Notes for the future]]. *The `GetStatTotals` tests were added Oct 9*

---
## Core Fragments
When a shade dies or is broken down, the player gets a **core fragment** (a "shard"): some extra core power for a shade slot.

**Rules (decided):**
- A fragment is its **own value on the shade slot**, separate from level. Core power = level + fragment power
- A shard is worked out from the dead shade's **level only**. Its own slotted fragment doesn't count, so power can't compound over generations (e.g. a lvl 40 shade with a 20 power fragment still drops a shard based on lvl 40)
- **One fragment per shade.** Slotting one is **permanent**, with a confirm box to catch accidental clicks
- A slotted fragment is **lost** when its shade dies or is broken down
- **Dying:** all runes on the field are lost. **Breaking down:** some runes come back (amount decided in playtests) [[undecided]]
- This replaces the "about 25% of its levels carry over" idea in [[Shade]] (Death and Burning Down)

**What the player can do with a shard:**
1. Slot it into a new shade that starts at Bound (lvl 1). It keeps the fragment for life
2. Slot it into an existing shade that doesn't have a fragment yet (a lvl 20 shade + a 3 power shard = 23 power)
3. Don't use it. Maybe the player was testing, or didn't plan their slots

**Tiers (starting numbers, will change in playtests):**

| Shade level | Shard   | Power |
| :---------- | :------ | :---- |
| 1–3         | none    | –     |
| 4–10        | Minor   | 1     |
| 11–20       | Basic   | 3     |
| 21–40       | Major   | 7     |
| 41–50       | Perfect | 15    |

The power more than doubles each tier on purpose, so sacrificing one high-level shade beats several small ones. Watch for big gaps between tiers (lvl 40 gives 7, lvl 41 gives 15): players will level to the next tier's start before sacrificing. Fine if that feels good, flatten it if not. [[undecided]]

**As items:** each tier is a normal item asset (one `ItemSO` per tier, holding its power value). Shards have no per-shard data, so two of the same tier stack in the inventory like any other item. [[Notes for the future]]

*Revisit if something needs shards to carry their own data* (like a memorial "Shard of Ignis, Ascendant, Lv 23", or crafting that cares about which shade it came from). That needs item instances in the inventory, which runes will probably need anyway for rolled stats (see Open Questions). Kept simple until then.

**Ideas, not decided:**
- Shards as a crafting ingredient for weapons or high-level runes. If this happens, recipes should ask for a minimum tier rather than a count, so farming cheap shades for Minor shards doesn't pay off [[undecided]]
- Ways to stack more than one fragment on a shade, if one feels too limiting [[undecided]]
- The core as a movable node was considered and dropped for now (too easy to abuse)

---
## Open Questions
- ~~**Rune instances**~~ *Decided Oct 8, 2026: fixed rune types, no rolled stats (see Architecture)*
- ~~**Power priority**~~ *Decided Oct 2026: fewest bridges from the core first, ties go to the rune placed first*
- **Hazen's stats:** from the active shade's runes only, or every slot? (see [[Element Rune]]) [[undecided]]
- **Layout:** keep free placement with bridges and tearing, or move to fixed sockets like the AI mockup? Going with free placement for the prototype, the playtest decides [[undecided]]
- ~~**Lowered core power**~~ *Handled: power is handed out from scratch every time, so a smaller core just powers fewer runes*
- **Slotting a shard:** drag the shard into the core on the field (fits the 3D view, see The View)

---
## Future Ideas (after first playtests)
- **Special areas on the field**, like board game bonus squares: walls that block bridges, areas that boost range, bridge slots or stat effects
- **Large nodes that need large runes** (all runes are one size for now)
- **Multiple drafts** (editing several shades before saving)
- **Legendary potions** whose effects survive entering the hub
- **XP rates per shade:** XP could decide how fast a shade levels, so some shades level faster but have weaker stats. Probably not, but worth keeping in mind [[undecided]]
- *Dropped:* zones that spin to change the layout (adds frustration, not fun)
