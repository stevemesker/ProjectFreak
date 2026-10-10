## Overview
The plan for the 3D rune field test (step 8 of the [[Rune Field Overhaul Plan]]). Design notes are in [[Rune Field 3D]]; this note is how it gets built. Worked out with Claude on Oct 10, 2026.

Back to [[AA - AI Info]] · [[Architecture Atlas]]

**Status (Oct 10, 2026):** signed off. All 7 steps built (camera feel signed off; the rest not tested in Unity yet). Built pieces are written up in [[Rune Field 3D View]] [[Notes for the future]]

**Branch:** `dev > rune field overhaul > 3d rune field test`. If the test doesn't work out, the branch gets dropped and the 2D view stays.

**Goal:** a build of `3d_Evolution_UI_scene` that runs on its own and can be sent to friends to play with. Loading it into the real game (Hazen terminal, additive scene, camera blend) comes after the test.

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
1. Copy the canvas from `Evolution_UI_scene` into `3d_Evolution_UI_scene` (select it, Ctrl+C, open the other scene, Ctrl+V)
2. ~~Add a **RuneField** layer~~ *Not needed: picking is math, not colliders (Oct 10, 2026)*
3. Add `Node3DObject` to `PFB_Node_3D` and `Core3DObject` to `PFB_Core_3D` (the rune and bridge prefabs get their scripts added at spawn if missing). *No picking colliders needed*
4. ~~Retune `SO_RuneField_Settings`~~ *Done Oct 10, 2026 (step 1)*
5. Reset the shade slots before testing and before building
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
- Panning when a dragged rune is held near the screen edge [[Notes for the future]]
- D-pad quick select from the inventory list [[Notes for the future]]
- Info popup for runes and nodes (needs a button now that Y opens the side panel) [[Notes for the future]]
- Shards: dragging a shard into the core, orbiting shard per tier [[Notes for the future]]
- Loading into the real game: Hazen terminal, additive scene, camera blend, pause, input map switch, safe rooms [[Notes for the future]]
- Real feedback visuals (shaders, zone borders, rune glyphs) once 2D vs 3D is decided [[Notes for the future]]
- Speed and sensitivity options in the settings menu [[Notes for the future]]
- **Picking at low tilt:** picking is math on the ground plane, so pointing at the top of a rune at a low camera angle can land behind it. Switch to colliders (or a ray-vs-cylinder test) if it's noticeable in testing. Colliders would also cover hovering nodes and the core [[Notes for the future]]
- **Less hand setup:** self-wiring buttons (a small component with a Save / Discard / Leave dropdown that finds the field itself), UI that registers with the field, and one rig prefab (field, camera, canvas, popup) once the test settles [[Notes for the future]]
- **Code walkthrough with Steve** of the 3D view scripts, once the 2D vs 3D decision is made [[Notes for the future]]
