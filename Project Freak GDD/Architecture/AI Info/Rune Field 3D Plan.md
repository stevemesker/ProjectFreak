## Overview
The plan for the 3D rune field test (step 8 of the [[Rune Field Overhaul Plan]]). Design notes are in [[Rune Field 3D]]; this note is how it gets built. Worked out with Claude on Oct 10, 2026.

Back to [[AA - AI Info]] · [[Architecture Atlas]]

**Status (Oct 10, 2026):** signed off. All 7 steps built (camera feel signed off).

**Oct 11, 2026:** tested in Unity and playtested with friends. Steve's verdict: a big success, and it feels better than the 2D version. Next up: tools to make it feel intuitive (new chat), and live gates (see Future Ideas in [[Rune Field Overhaul Plan]]). Keep an eye out for the camera slipping past the field (not seen since). Built pieces are written up in [[Rune Field 3D View]] [[Notes for the future]]

**Oct 11, 2026 (later):** went through [[Rune Field 3D Change Requests and to Do]] with Claude. Systems and gameplay come first, art waits for its own talks. The plan for that round is **Playtest 0 Changes** below; everything under it (Decisions through Open Questions) is the record of the first build.

**Oct 11, 2026 (evening):** Playtest 0 build steps 1 (frozen saved and moving) and 2 (runes in any zone) built in the rules, see **Zones and Evolving** in [[Rune Field System]]. Steps 3 (the 3D view for them), 4 (dragging and gamepad), 5 (code ready for the art), 6 (shade info window) and 7 (shard data) built, see [[Rune Field 3D View]]. **Every Playtest 0 build step is built.** Left from that round: the Art Pile (own talks) and the shard page

**Oct 12, 2026:** zone visuals talked through and planned, see [[Zone Visuals Plan]] (its own plan and build order, art briefs in `Art/UI/Rune Field/3D Assets/Zones`).

**Branch:** `dev > rune field overhaul > 3d rune field test`. If the test doesn't work out, the branch gets dropped and the 2D view stays.

**Goal:** a build of `3d_Evolution_UI_scene` that runs on its own and can be sent to friends to play with. Loading it into the real game (Hazen terminal, additive scene, camera blend) comes after the test.

---
## Playtest 0 Changes
*Agreed Oct 11, 2026, from [[Rune Field 3D Change Requests and to Do]].* **Systems and gameplay first.** Anything touching art (shaders, effects, zone visuals, the info popup's look) is in the **Art Pile** and gets its own talk. Where art needs code to drive it, the code side gets set up now so the shader only has to read it.

### Frozen runes can move
Playtest feedback: not being able to touch old runes felt bad. Frozen runes can now be picked up and moved **anywhere on the field**, as long as these always hold:
- **A. A frozen rune plugged into a node never moves or unplugs.** Only frozen runes: runes in the open zone can still be tried in and out of nodes as much as the player wants
- **B. A frozen bridge is never cut.** Not by tearing, not by the forward zone cut (below), not by anything. Dragging a frozen rune is a **hard leash**: it can't be pulled past a frozen bridge's reach, so there's no tearing at all
- **C. A frozen rune's place in the power order never changes.** Frozen runes are powered first, along frozen bridges only (the rules already do this). Since frozen bridges can't break and placement order never changes, a new short chain to the core can't take power away from the old chain. *Example: a 20 rune chain to the evolve gate, then a new 2 rune chain at level 21. Without this the 2 new runes would be powered first and the gate's chain could lose power*

Together these stop anything cutting an evolution gate (or any frozen node) off from power.

**Frozen is saved, not worked out from position.** Right now a rune is frozen because of the zone it's in. Once frozen runes can move, that stops working (drag one into the open zone and it would thaw). So `RuneFieldData` saves three lists:
- **Frozen runes** (IDs)
- **Frozen bridges** (pairs), saved as their own list rather than "both ends are frozen", so a new bridge made later between two frozen runes doesn't become permanent by accident
- **Locked-in runes:** frozen runes plugged into a node, which rule A covers

**When things freeze:** when the shade evolves, every rune inside the zone being closed (or further in) freezes, every bridge with both ends frozen at that moment is added to the frozen bridges, and every frozen rune in a node is locked in. Runes placed later in old zones are normal runes until the next evolve freezes them too.

**Locking in on save:** a frozen rune can be plugged into a node in the draft (to try out an old ability). It's only locked in once the field is **saved**, and saving asks first, like the gate confirm: "These frozen runes will be locked into their nodes permanently." Before saving it can still come back out.

**Frozen runes past the open zone:** allowed, so nothing ever has to be dragged back. Out there a frozen rune keeps its power and keeps passing it along frozen bridges (rule C), but:
- it **can't plug into a node** past the open zone (a powered frozen rune is the only thing that could turn a future node on early)
- runes past the open zone that **aren't frozen never get power**, even bridged to a powered frozen rune, so nobody can power a future zone early

**Old fields:** slots saved before this have no frozen lists. When a slot with rank above Bound loads with empty lists, they're built once from positions (the old zone rule), so nothing saved breaks. Slots get reset for testing anyway.

### Runes in any zone
- **Old (frozen) zones:** new runes can be placed there. They're normal runes (movable, removable, powered after the frozen runes). Ability nodes in old zones take runes again, with the usual unlocks and lockouts. Old gates stay closed: the taken gate's rune is locked in, so it keeps its rivals locked out, and evolving only ever checks gates in the open zone
- **Future (locked) zones:** runes can be placed there to **plan ahead**. They never get power, come out of the inventory like any rune, and save with the field. They **can snap into nodes**, but with no power the node stays off. Lockouts still trigger on plugging, so a planning rune in a future gate locks its rival until it's pulled out
- **Cut on save:** in the draft, bridges work normally, including ones crossing the open zone's outer ring (they just don't carry power across). **When the field is saved, every bridge that crosses that ring and isn't frozen is cut.** Bridges between two runes past the ring stay, so a planned chain keeps its shape; when the zone opens, one bridge reconnects it. The cut fires an event listing the cut bridges, for the break effect later
- **Load and save checks** in `RuneField` must not drop or refuse runes past the open zone. *Check everything that assumes every rune is in the open zone or further in.* This is the "runes left in a closed zone" half of live gates (Future Ideas in [[Rune Field Overhaul Plan]]); evolving without saving is still for later

**Logic tester cases** (`RuneFieldLogicTester`): the 20 chain vs 2 chain example, moving frozen runes (inside the leash, refused past it), refusing to unplug a locked-in rune, a frozen rune past the open zone (keeps power, can't plug, doesn't power planning runes), planning runes never powered, the cut on save (frozen crossing bridges kept), old gates staying closed, and an old slot with no frozen lists.

### Dragging and gamepad
- **Hide the inventory button over the field:** once a rune dragged from the list leaves the panel, the button hides and the gamepad's stand-in rune shows on the field instead. Back over the list, the button comes back. *The shrink-to-a-circle animation is for a polish pass* [[Notes for the future]]
- **Edge panning:** while dragging a rune (mouse, gamepad carry, stand-in, or a list drag), holding the pointer near the screen edge pans the camera, faster the deeper into the margin. Settings on `SO_RuneField_Camera` (margin, speed, curve). It only starts once the pointer has been out of the margin once, so drags starting at the inventory on the right edge don't pan straight away. The field edge limit still applies
- **D-pad focus fix:** with the side panel open and nothing in the list selected (after a mouse click, or focus dropping off), D-pad down selects the top rune. With a rune selected, normal navigation moves to the next one, as now

### Code ready for the art
- **Hovering nodes and the core:** picking only finds runes now. Nodes (rune sized) and the core (`_CoreSize`) get picked the same way (math on the field's plane), and the pointer says what's hovered (rune, node, core or nothing) with an event when it changes. The info popup will hook in here once it's designed
- **Core state:** `Core3DObject` exposes spare power, total power, and whether any rune on the field has no power, so the core shader (Steve's) can show bright / normal / dull purple. Property names become inspector fields once the shader exists
- **Bridge end colors:** each bridge gets the colors of the two runes it joins (the core counts as a color too), plus its tear amount (0 to 1) and its strain (how far into `_TearSlack` it's pulled, before the timer starts, for the tear dead zone look). All per bridge through `RuneFieldTint`'s `MaterialPropertyBlock`, so it's **one shared material**, no material per color combo and no copy while tearing. Names become inspector fields once Steve's shader exists
- **Rune color and glyph data:** `ElementItemSO` gets a `ColorPaletteSO` and a glyph texture. Runes use the palette's primary color (a plain tint until the shader exists); the field's temp `_ElementColors` list stays only as a fallback for runes with no palette yet. *Later the color may be worked out from the rune's stats* [[undecided]]

### Shade info window
A window in the side panel (with the inventory and shade slots), showing the open slot's shade:
- **Stats:** base stats (from the evolution), what the saved runes and nodes add, and what the draft would change, shown apart until saved
- **Render:** a render texture camera showing the evolution's art (`_TetheredArt`), spawned out of sight
- **Seen evolutions:** a saved list of evolutions the player has seen. One not seen yet shows blacked out. For now an evolution counts as seen when the shade evolves; later, when its evolve cutscene plays [[Notes for the future]]
- Layout, the blacked out look and the render's lighting are part of the art pass

### Shard data
Only the data now; the shard page, dragging a shard into the core and the orbiting shard need a bigger talk (the page may be its own inventory window) [[Notes for the future]]
- **`ShardItemSO`** (an `ItemSO`): its tier and power. One asset per tier, they stack like other items (see Core Fragments in [[Rune Field Overhaul Plan]])
- **One shard per shade:** `ShadeSO` holds its slotted shard. Core power = level + the shard's power (replacing the plain `_FragmentPower` number). Slotting is permanent and refused if the shade already has one; the Shade Manager does it and takes the shard out of the inventory
- Dropping shards when a shade dies or is broken down is part of the fail state, not this round

### Build Order
Each step gets tested in Unity before the next.
1. ~~**Frozen saved and moving:**~~ *Built Oct 11, 2026, see [[Rune Field System]]. The 20 vs 2 chain test is shortened to a 6 rune chain (same rule). Frozen bridges with no real bridge behind them are dropped on load with a warning. The frozen-only plug check for nodes past the open zone gets its own tester case in step 2, once those nodes open. The views still can't pick up frozen runes until step 3.* the three frozen lists, freezing on evolve, filling them for old slots, frozen runes movable with the hard leash, locked-in runes, frozen-first power from the saved lists, frozen runes can't plug past the open zone. Tester cases
2. ~~**Runes in any zone:**~~ *Built Oct 11, 2026, see [[Rune Field System]]. No load or save code assumed runes were in the open zone, so nothing needed changing there. Nodes outside the open zone no longer show the Locked look, only locked-out ones do.* placing in old and future zones, old ability nodes open again, no power past the open zone, the cut on save with its event, load / save checks. Tester cases
3. ~~**3D view for the new rules:**~~ *Built Oct 11, 2026, see [[Rune Field 3D View]]. Cut bridges disappear through the reload after saving; the view also passes them on (`OnBridgesCut`) for the break effect. The evolve popup's default text was updated for frozen runes moving. The 2D view is left as it is (it still can't pick up frozen runes).* dragging frozen runes, the lock-in confirm on save, X / sending to inventory refused for frozen runes, cut bridges removed on save
4. ~~**Dragging and gamepad:**~~ *Built Oct 11, 2026, see [[Rune Field 3D View]]. Edge panning also skips while the pointer is over the canvas, so taking a rune back to the list doesn't pan. Its speed is a multiple of the Move speed, so it scales with zoom. The D-pad fix also needed the pointer to count the D-pad as gamepad use.* hidden button with the stand-in, edge panning, D-pad focus fix
5. ~~**Code ready for the art:**~~ *Built Oct 11, 2026, see [[Rune Field 3D View]]. The hover lives on the field (it does the picking), not the pointer. Bridge shader property names are already inspector fields with placeholder names. Planning runes don't count as "a rune with no power" for the core.* hovering nodes and the core, core state, bridge end colors / tear / strain, rune color and glyph data
6. ~~**Shade info window**~~ *Built Oct 11, 2026, see **ShadeInfoWindow** in [[Rune Field 3D View]]. Seen forms are saved on the save slot. The window builds its own layout; adding it to the side panel and its tab is Steve's (two steps in the note).*
7. ~~**Shard data**~~ *Built Oct 11, 2026, see [[Items & Pickups]] (`ShardItemSO`) and [[Shade Manager]] (`SlotShard`). The Shade Manager also got `OnCorePowerChanged`, so the open 3D field picks up new core power (level or shard) by itself*

### Art Pile (own talks)
- ~~Zone visuals: the crack in the floor with a wall of magic light, and floor colors for frozen / open / locked zones~~ *Planned Oct 12, 2026, see [[Zone Visuals Plan]]*
- Bridge tear shader (builds up and changes color with the tear), the break effect (tears, the cut on save, anything severed), the tear dead zone look
- Node glow shader: fades on / off and to the locked look. *Idea: the shader runs the fade itself from a start time set once, so there's nothing to tick per node*
- Core shader (Steve makes it and shares the control variables)
- Bridge end color gradient (Steve's shader, fed by step 5)
- Rune color and glyph shader
- Range visualizer while placing (reach ring, preview bridges)
- Info popup for runes, nodes and the core: a hover panel with a 3D feel. Steve mocks it up first; canvas or not is decided then
- Shade info window look, the blacked out render
- Shards: page, dragging into the core, orbiting shard per tier

---
## Decisions
*Agreed Oct 10, 2026.*

- **New view scripts, 2D left alone.** The branch ends with only one view surviving (3D wins and the 2D view is deleted, or the branch is dropped), so copying code from the 2D view costs nothing long term, and the working 2D code never gets touched
- **The rules don't change.** `RuneField`, `RuneFieldData`, `RuneFieldSettingsSO`, effects and the [[Shade Manager]] are used exactly as they are. The 3D view sits on top of `RuneField` the same way `RuneFieldManager` does
- **No physics for the rules.** Bridges, snapping, footprints and overlap stay the rules' math, since level-ups and evolving run the rules with no scene loaded. Picking (what's under the cursor) is math too: the pointer hits the field's plane and the rules say which rune is there. *Colliders on their own layer were the first idea, dropped Oct 10, 2026 since the math needs no setup*
- **One shared canvas** (Screen Space Overlay) for the inventory, save button, shade slots, labels and the reticle. Overlay canvases don't need a second camera, so this is fine for HDRP / Steam Deck
- **Middle mouse pans** whether or not a rune is held
- **Gamepad layout:** A = grab / drop, X = send back to inventory, B = cancel drag / back, Y = open / close the side panel (*changed Oct 10, 2026: Y was saved for the info popup, which will need another button*)
- **No shards in this test.** The core fragment visuals are done; dragging a shard into the core waits until 2D vs 3D is decided

---
## Units
The rules work in **field units** (a rune is 100 wide, the core sits at (0, 0)). The 3D scene works in meters (a rune is 1 m, the core 2 m).

- One converter on the field object: `_FieldUnitsPerMeter` (100). `FieldToWorld(Vector2)` and `WorldToField(Vector3)` turn field X/Y into world X/Z under the field's root transform, so the field can sit anywhere in a scene
- Field units stay as they are, so saved fields, rune assets and the settings asset all keep working without changes

**Settings to retune for 3D** (`SO_RuneField_Settings`). Reach is measured center to center, so the bigger core changes things:

| Setting | Now | 3D | Why |
| :--- | :--- | :--- | :--- |
| `_CoreSize` | 100 (default) | **200** | The core mesh is 2 m |
| `_CoreReach` | 150 | **250** | At 150 a rune has to touch the 2 m core to bridge to it |
| `_ZoneWidth` | 300 | **400** | At 300 zone 1 only has room for about one ring of runes around the bigger core |
| `_RuneSize` | 100 | 100 | Matches the 1 m rune |

The 2D scene shares the asset, but it isn't used on this branch. [[undecided]]

---
## Scripts
New view scripts go in `Scripts/Rune Field/3D`.

| Script | What it does |
| :--- | :--- |
| `RuneField3DObject` | The scene singleton (`_Field3D`). The 3D version of `RuneFieldManager`: opens a slot's field, spawns and positions everything, runs dragging and dropping through `RuneField`, and the draft / save / popup / slot switching flow (copied from `RuneFieldManager`). Holds the unit converter and draws the zone ring gizmos |
| `Rune3DObject` | One rune. Power, hover, invalid and frozen looks |
| `Bridge3DObject` | One bridge. Sits on rune A, points at rune B, Z scale = distance in meters (the mesh is 1 m along +Z from its origin, so the tiling material just works). Runs the tear timer (copied from `NodeBridge`) |
| `Node3DObject` | One ability node. The 3D version of `EvolutionNode`: effect list, unlocks, lockouts, snap-to-ring buttons, and the `OnEffect` child switched on while the node is on |
| `Core3DObject` | The core. Shows its power through a label |
| `RuneFieldCameraObject` | On the focus target. Places the camera on the zoom curve (with the zoom spring) and points it at the aim target, and moves the focus (panning, grab-drag with slide, zoom toward cursor, edge limits). Gizmos and preview slider |
| `RuneFieldCameraSO` | The camera settings asset (zoom curve, speeds, spring). Tuned in play mode |
| `RuneFieldPointerObject` | One pointer for everything: the mouse, or the gamepad reticle. Finds what's under it, hits the field plane, and works out whether it's over the canvas |
| `RuneFieldLabelObject` | Pooled labels on the canvas that follow runes and the core on screen |
| `RuneFieldTestKitObject` | Test build tools (see **Test Build**) |

**Shared code that changes:** `ElementDataObject` and `IngredientListWindow` point straight at `RuneFieldManager`. A small interface (`IRuneFieldView`: available counts, can place / place from inventory, invalid color, draft changed) lets them talk to either view. `RuneFieldManager` gets the interface too, so the 2D scene keeps working.

**Input:** a new **RuneField** action map in `PlayerInput.inputactions`:

| Action | Keyboard / mouse | Gamepad |
| :--- | :--- | :--- |
| Move | WASD | Left stick |
| Zoom | Scroll wheel, `[` and `]` | Left trigger out, right trigger in |
| Point | Mouse position | – |
| Reticle | – | Right stick |
| Select (grab / drop) | Left mouse | A |
| Drag Pan | Middle mouse | – |
| Return To Inventory | (drag onto the list) | X |
| Back | Escape | B |
| TogglePanel | Tab | Y |

---
## Camera
*Agreed Oct 10, 2026 (Steve's sketch). The tool has to handle any feel, since the feel gets found in testing. Details still open are tagged.*

**No Cinemachine for the test.** Our script places the camera and points it at the aim target. When the field loads into the real game, a virtual camera can sit on top (Body and Aim set to Do Nothing) so the brain can still blend from the gameplay camera [[Notes for the future]]

### The zoom curve
| Setting | What it does |
| :--- | :--- |
| `_FollowDistance` | A 2D offset along the ground from the aim target to the base of the camera's vertical line. Straight back = dead-on view, back and to the side = isometric. Same for every zoom |
| `_CameraHeight` | How tall that vertical line is. The top of it is the top of the curve |
| `_ZoomLayers` | Ordered list, bottom to top, **spread evenly** from the ground (height 0) up to `_CameraHeight`. Each layer has an **outward push** (how far the camera sits out from the vertical line at that height, pushed straight away from the aim target; negative pulls it in, even past the line) and a **height offset** (a fraction of the even spacing: −0.1 on a middle layer moves it 10% of the way toward the layer below). Every layer uses the same rule, so the bottom layer can lift off the ground and the top layer can drop below `_CameraHeight` |
| `_ZoomRange` | A 2D value (min, max): the part of the 0 to 1 curve the camera is allowed to use. Lets the bottom layer sit on the ground while the camera never goes that low, without working out any math |
| `_AimOffset` | Offset for the aim target, starting at 0, there if the framing needs it |

- **The control curve passes through every layer point** (a Catmull-Rom spline), so "at this layer, the camera is exactly here" is always true
- **Zoom 0 to 1 is measured by distance along the curve** (0 = the bottom layer, 1 = the top), so the camera always moves at the same speed along it, however the layers are spaced. Zoom sensitivity = how far one zoom step moves along the 0 to 1
- **The camera always looks at the aim target**, so the tilt comes from where it sits on the curve
- **If `_FollowDistance` is 0** (camera straight above the target), the outward push uses the field's "back" direction so the curve still has a direction
- *Later if needed:* outward push as an X/Y pair (side angle changing with zoom), FOV along the curve [[undecided]]

### Snappy response
- Two values in our script: **target zoom** (changes the moment input comes in) and **current zoom** (chases the target with a spring, then places the camera on the curve). The spring's speed is one setting (`_ZoomResponse`), with an optional bit of overshoot (`_ZoomBounce`) for a springy settle. Tuned so it's fast with no lag, never floaty
- **Scroll wheel:** each notch moves the target one step, and quick notches stack, so a fast flick covers the whole range and a single click is a fine adjustment
- **Triggers and `[` `]`:** zoom at a speed set by how hard the trigger is pressed (with a response curve), so a light press is slow and precise
- **Zoom toward the cursor (mouse):** the spot under the mouse stays under the mouse while zooming, like map apps, so zooming in on a rune takes you to that rune. On gamepad it zooms toward the reticle (the screen center when centered). Can be turned off to compare
- **Detents** (snap points along the curve the camera settles into) are an option to try if smooth doesn't feel right [[undecided]]

### Moving
- **Move (WASD / left stick):** moves the focus, relative to the way the camera faces (set by `_FollowDistance`), with a quick ramp up and down so it doesn't feel stiff. Speed scales with zoom, so the same push covers the same amount of screen. Optional **hold boost**: holding for `_BoostDelay` seconds speeds up along a curve, with a toggle
- **Grab-drag pan (left mouse on empty field, or middle mouse anywhere):** the spot on the plane under the cursor at the click stays under the cursor. Exact, no smoothing, so it feels glued to the mouse
- **Slide:** letting go of a fast drag keeps the field moving and slowing down (`_SlideFriction`). Any new drag or Move input stops it
- **Edge limit:** the focus is kept within the outer ring plus `_EdgeBleed`, so the field's edge can't pass the middle of the screen. *Built as a hard stop (movement pushing outward is cut, sliding along the edge works). An eased edge can come later if it feels harsh* [[undecided]]
- **No spinning:** the player never rotates the camera. Which way it faces comes from `_FollowDistance`

### Tuning tools
- **Camera settings asset** (`SO_RuneField_Camera`): the zoom curve, speeds and spring values live on an asset, not the component, so **changes made in play mode stick** after stopping. Tune while playing, keep what feels good
- **Scene view gizmos:** the vertical line, the zoom layers, the curve, the usable `_ZoomRange` part and the camera's current spot, updating live as values change
- **Preview Zoom slider** on the component: moves the camera along the curve in edit mode, to check framing without pressing play
- **Debug readout** (toggle): zoom, height, pitch and FOV on screen while tuning
- Speeds and sensitivities become player options later [[Notes for the future]]

---
## Pointer and Gamepad Reticle
Everything that asks "where is the player pointing" asks `RuneFieldPointerObject`. It switches between mouse and gamepad depending on which was used last.

- **Mouse:** the pointer is the mouse. No reticle
- **Gamepad, centered:** the reticle sits in the middle of the screen, faded. The left stick moves the camera, so whatever passes under the middle gets hovered
- **Gamepad, free:** moving the right stick makes the reticle solid and moves it around the screen. Letting go leaves it where it is. After `_RecenterDelay` seconds of left-stick-only input it goes back to the middle and fades
- **Carrying a rune on gamepad:** A grabs, A again drops (toggle, so the left stick can pan while carrying). B cancels and puts the rune back. X sends the hovered rune back to the inventory (refused for frozen runes)
- **Reaching the canvas on gamepad:** the reticle drives a **virtual mouse** (Input System `VirtualMouseInput`, or a small version of our own if centering needs more control). The canvas sees it as a real mouse, so dragging runes out of the inventory list works without changes
- **Hover:** the pointer hits the field's plane (math, no collider) and the rune whose footprint covers that spot is hovered

---
## Field Pieces
- **Runes:** colored by element (a temp element → color list on `RuneField3DObject` until the shader exists). Unpowered, invalid spot, frozen and hovered looks use a `MaterialPropertyBlock` (dim / tint / brighten), not transparency, since HDRP transparent materials sort badly and need their own material. Property names are inspector fields so they can follow the real shader later. The rune's label object (its glyph) waits for the shader [[Notes for the future]]
- **Bridges:** placed at rune A's center, pointed at rune B, scaled to the distance. Tearing shows as a tint while the timer fills
- **Nodes:** `OnEffect` child on while the node is on, tint while locked, nothing otherwise. Snap rings and zone tools work in field units through the converter
- **Core:** 2 m footprint (`_CoreSize` 200). Power shown by its label
- **Zones:** ring gizmos in the scene view, like the 2D version

**Note on saved fields:** saved plugs and snapshots point at nodes by their spot in the node list, and the 3D scene's list isn't the 2D scene's. Slots get reset before testing (Reset Slot) so nothing saved in the 2D scene is loaded into the 3D one.

---
## Labels
- Labels live on the canvas and follow their rune on screen, so they're readable at any zoom and tilt and never clip into meshes
- A rune shows "current / needed" power when it's within `_LabelRadius` (field units) of the focus point, when it's hovered, or while it's dragged. The radius grows a little as the camera zooms in
- The core always shows "left / total" above the orb
- Labels are pooled (made once, reused), not created per rune

---
## Test Build
For the build sent to friends:
- `RuneFieldTestKitObject` puts a starting set of runes into the inventory on start (a list of rune + count), since Odin buttons don't exist in a build
- Small on-screen buttons: **Level Up** (to try evolving through gates), **Reset Slot**, **Refill Runes**, **Quit**
- A short controls panel (keyboard/mouse and gamepad)
- Shade slot changes only last for one play session in a build (assets can't be changed on disk outside the editor), which is fine for a test

---
## Unity Setup (Steve)
Things that are safer done in the editor than by editing scene files:
1. ~~Copy the canvas from `Evolution_UI_scene` into `3d_Evolution_UI_scene` (select it, Ctrl+C, open the other scene, Ctrl+V)
2. ~~Add a **RuneField** layer~~ *Not needed: picking is math, not colliders (Oct 10, 2026)*
3. ~~Add `Node3DObject` to `PFB_Node_3D` and `Core3DObject` to `PFB_Core_3D` (the rune and bridge prefabs get their scripts added at spawn if missing). *No picking colliders needed*
4. ~~Retune `SO_RuneField_Settings`~~ *Done Oct 10, 2026 (step 1)*
5. ~~Reset the shade slots before testing and before building
6. Put the scene in Build Settings for the friends build

Anything with a setup button (camera rig, labels, reticle) will say so when it's built.

Starting rune assets for the test: Steve makes these.

---
## Build Order
1. ~~**Input and camera:**~~ *Built Oct 10, 2026, see [[Rune Field 3D View]].* RuneField action map, camera settings asset, zoom curve with gizmos and preview slider, spring zoom, zoom toward cursor, Move, grab-drag pan with slide, edge limit, hold boost, debug readout. Test by flying around an empty field with the ring gizmos. *The camera gets signed off on feel before step 2 starts*
2. ~~**Drawing a field:**~~ *Built Oct 10, 2026, see [[Rune Field 3D View]]. Also: a shared `RuneFieldTint` helper, Test Tools buttons (place a rune, save, discard, clear) until step 3.* `RuneField3DObject` with the converter, opening a slot, spawning the core, nodes, runes and bridges, power and node state looks. `Node3DObject` with effects and snap tools
3. ~~**Mouse dragging and saving:**~~ *Built Oct 10, 2026, see [[Rune Field 3D View]]. Picking turned out simpler without colliders (math on the field's plane), so no RuneField layer or picking colliders are needed.* pointer, hover, dragging runes (bridge limits, snapping, tearing, invalid spots, frozen runes), the draft / save / popup / slot flow
4. ~~**Canvas and inventory:**~~ *Built Oct 10, 2026, see [[Rune Field 3D View]].* canvas copied in, `IRuneFieldView`, dragging runes from the list onto the field, dropping runes on the list to take them off
5. ~~**Gamepad:**~~ *Built Oct 10, 2026. Inventory drags on gamepad are hold A, move, let go (like the mouse); carrying runes on the field is A / A.* reticle (centered / free), virtual mouse for the canvas, A / X / B
6. ~~**Labels**~~ *Built Oct 10, 2026*
7. ~~**Test build:**~~ *Built Oct 10, 2026 (`RuneFieldTestKitObject`; Level Down added). Building and sending it is Steve's.* test kit, buttons, controls panel, build

---
## Open Questions
- ~~**Settings numbers**~~ *Decided Oct 10, 2026: core size 200, core reach 250, zone width 400 to start*
- ~~**Camera yaw**~~ *Decided Oct 10, 2026: set by `_FollowDistance` on the zoom curve, so dead-on and isometric are both one value away*
- ~~**Zones in the build**~~ *Decided Oct 10, 2026: gizmos only. Boundaries get drawn in the art pass*
- ~~**Starting runes**~~ *Steve is making the test rune assets*

---
## Later (not in this test)
*Oct 11, 2026: edge panning, the D-pad, the info popup and shards moved into **Playtest 0 Changes** above (the info popup is now a mouse hover panel, so it no longer needs a button).*
- Loading into the real game: Hazen terminal, additive scene, camera blend, pause, input map switch, safe rooms [[Notes for the future]]
- Real feedback visuals (shaders, zone borders, rune glyphs) once 2D vs 3D is decided [[Notes for the future]] *Zone borders: planned Oct 12, 2026 in [[Zone Visuals Plan]]*
- Speed and sensitivity options in the settings menu [[Notes for the future]]
- **Picking at low tilt:** picking is math on the ground plane, so pointing at the top of a rune at a low camera angle can land behind it. Switch to colliders (or a ray-vs-cylinder test) if it's noticeable in testing. Colliders would also cover hovering nodes and the core [[Notes for the future]]
- **Less hand setup:** self-wiring buttons (a small component with a Save / Discard / Leave dropdown that finds the field itself), UI that registers with the field, and one rig prefab (field, camera, canvas, popup) once the test settles [[Notes for the future]]
- **Code walkthrough with Steve** of the 3D view scripts, once the 2D vs 3D decision is made [[Notes for the future]]
