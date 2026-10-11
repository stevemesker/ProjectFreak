## Overview
The 3D view of the [[Rune Field]], being tested on the `3d rune field test` branch. Same rules as the 2D view ([[Rune Field System]]), drawn in a 3D scene (`3d_Evolution_UI_scene`). The plan and build order are in [[Rune Field 3D Plan]].

*Built Oct 10, 2026: every step of the [[Rune Field 3D Plan]] (camera, drawing, dragging and saving, inventory hookup, gamepad, labels, test kit).* Not tested in Unity yet [[Notes for the future]]

Back to [[Rune Field System]] · [[Architecture Atlas]]

---
## Units
The rules work in **field units** (a rune is 100 wide), the 3D scene in meters. `RuneFieldSettingsSO._FieldUnitsPerMeter` (100) converts between them with `ToMeters` / `ToFieldUnits`, so a rune is 1 m and the core 2 m.

Settings retuned for 3D on Oct 10, 2026 (`SO_RuneField_Settings`): `_CoreSize` 200, `_CoreReach` 250, `_ZoneWidth` 400. The 2D scene shares the asset; its gates were placed for a zone width of 300, so its setup check will complain about gates not on a ring while on this branch.

---
## Camera
Scripts in `Scripts/Rune Field/3D`. No Cinemachine: `RuneFieldCameraObject` places the camera itself.

```text
RuneFieldCameraObject (on the focus target)
    │ reads
    ▼
SO_RuneField_Camera (zoom curve, zoom feel, move speeds)  +  SO_RuneField_Settings (field edge)
    │ every frame: zoom input → spring → spot on the curve, focus moved by WASD / drag / slide, kept inside the edge
    ▼
Main Camera (placed on the curve, looking at the aim spot)
```

### The zoom curve (`RuneFieldCameraSO`)
Created from **Create → Rune Field → Camera**. One asset, `SO_RuneField_Camera` (in `Scriptable Objects/Rune Field`). **Changes made while playing stay after stopping**, so it can be tuned by feel. The code only reads it.

| Variable | Description |
| :--- | :--- |
| `_FollowDistance` | Where the camera's vertical line sits on the ground, from the aim target (meters). X = sideways, Y = behind. (0, 4) is dead-on, (3, 3) isometric. Also decides which way the camera faces |
| `_CameraHeight` | Height of the vertical line (meters) |
| `_ZoomLayers` | The curve's control points, bottom to top, spread evenly from the ground to `_CameraHeight`. Each has `_OutwardPush` (meters out from the line, straight away from the target, negative pulls in) and `_HeightOffset` (fraction of the even gap, −1 to 1). The curve passes through every one. Needs at least 2 |
| `_ZoomRange` | The part of the curve (0 to 1) the camera can use |
| `_StartZoom` | Where the camera starts (kept inside the range) |
| `_AimOffset` | Moves the spot the camera looks at (meters; X sideways, Y up, Z forward) |
| `_ScrollStep` | How far one scroll notch moves along the curve. Notches stack |
| `_HeldZoomSpeed` / `_HeldZoomResponse` | Triggers and `[` `]`: speed at full press, and how pressure maps to speed |
| `_ZoomSmoothTime` | How long the camera takes to catch up to the target zoom (seconds). 0 = instant |
| `_ZoomTowardCursor` | Mouse zooming keeps the spot under the cursor in place. Off = zoom toward the screen center |
| `_MoveSpeedZoomedIn` / `_MoveSpeedZoomedOut` | WASD / stick speed at each end of the zoom (m/s), blended between |
| `_MoveRampTime` | Time to reach full speed and to stop |
| `_UseHoldBoost` / `_BoostDelay` / `_BoostCurve` | Speed up after holding a direction (off by default) |
| `_SlideFriction` / `_MinSlideSpeed` / `_MaxSlideSpeed` | The slide after letting go of a fast drag |

**How zoom works:** the layers make a Catmull-Rom curve (it passes through every point). The curve is cut into small pieces and measured, so **zoom 0 to 1 is distance along the curve**: the camera moves at the same speed along it however the layers are spaced. The curve is only rebuilt when a value changes (checked with a signature of the curve's values, so no edit gets missed). The camera always looks at the aim spot, so the tilt comes from where it sits on the curve.

### RuneFieldCameraObject
Goes on an empty object: that object **is** the focus target. Adding the component fills in the main camera and both settings assets by itself.

| Variable | Description |
| :--- | :--- |
| `_CameraSettings` | `SO_RuneField_Camera`. Edit the curve on the asset itself; the gizmos stay visible while it's selected (`_AlwaysShowGizmos`). *Showing the asset inside this component with Odin's `InlineEditor` was tried Oct 10, 2026 and dropped: it spammed Unity 2022 inspector layout errors* |
| `_FieldSettings` | `SO_RuneField_Settings`, for the field's edge. Empty = taken from the [[Shade Manager]] |
| `_Camera` | The camera it moves. Empty = main camera |
| `_FieldCenter` | The field's (0, 0) and plane (its up is the field's up). Empty = the scene's `RuneField3DObject`, or the world origin if there isn't one |
| `_CurrentZoom` / `_TargetZoom` | Read only |
| Tools foldout | `_PreviewZoom` slider (edit mode: moves the camera along the curve, Ctrl+Z works), **Preview Start Zoom** button, `_ShowDebugReadout` (zoom, height, out, pitch, FOV, curve length in the game view) |
| Gizmos foldout | `_AlwaysShowGizmos` (on by default; off = only while this object is selected) and the colors |

**Each frame (`LateUpdate`, unscaled time so it works while the game is paused):**
1. **Zoom:** scroll notches and held zoom move the target; the current zoom eases toward it with `SmoothDamp`. If it moved, the focus shifts so the field spot under the pointer stays under it
2. **Grab-drag:** a middle click anywhere, or a left click on empty field that isn't over the canvas, grabs the field spot under the cursor and keeps it under the cursor. Letting go of a fast drag starts a slide
3. **Move:** WASD / left stick, relative to the way the camera faces, ramped, faster when zoomed out, with the optional hold boost. Stops any slide
4. **Edge:** the focus can't go past the outer ring plus edge bleed. Movement pushing outward is cut, so sliding along the edge works. *A hard stop for now, not an eased one* [[undecided]]
5. **Place the camera** on the curve and point it at the aim spot. Its "up" comes from its fixed right side, so it never flips, even passing straight overhead

**Pointer:** asks the scene's `RuneFieldPointerObject` (see **The Field**). Without one (a scene with no field) it falls back to the mouse, or the screen center on gamepad. A left click on a rune grabs the rune instead of panning (`RuneField3DObject.IsHoveringRune`).

**Gizmos:** the vertical line (green), each layer (purple), the curve (yellow, faded outside the zoom range), the camera's spot and aim (red), and the edge circle.

### Input: RuneField action map
In `PlayerInput.inputactions` (Unity regenerates `PlayerInput.cs` from it).

| Action | Keyboard / mouse | Gamepad | Used by |
| :--- | :--- | :--- | :--- |
| Move | WASD, arrows | Left stick | Camera |
| Zoom (positive = in) | `[` out, `]` in | Left trigger out, right trigger in | Camera |
| ZoomScroll | Scroll wheel | – | Camera |
| Point | Mouse position | – | Camera |
| Select | Left mouse | A | Camera (panning), field (mouse: hold to drag; gamepad: A picks up, A again drops), the canvas through the virtual mouse (A on the canvas) |
| DragPan | Middle mouse | – | Camera |
| Reticle | – | Right stick | Pointer (moves the free reticle) |
| ReturnToInventory | – | X | Field (sends the hovered or carried rune back to the inventory) |
| Back | Escape | B | Field: puts a dragged rune back, else closes the popup, else asks to leave |
| TogglePanel | Tab | Y | Field: opens / closes the side panel (`SideWindowManager.ToggleSidePanel`), not while the popup is open |

Scroll notches are counted so Windows (120 per notch) and Linux / Steam Deck (1 per notch) both work.

### Setup
1. In `3d_Evolution_UI_scene`, add an empty object at the field's center, name it `Rune Field Camera`, and add `RuneFieldCameraObject`. The references fill themselves in
2. Press play

---
## The Field
*Built Oct 10, 2026 (step 2).* Scripts in `Scripts/Rune Field/3D`. Drawing works like the 2D view: every change to the rules fires `OnFieldChanged`, and the field makes the objects match the data.

```text
Shade slot ──(copy of the saved field)──► RuneField (rules) ◄── node list built from the Node3DObjects
                                              │ OnFieldChanged
                                              ▼
RuneField3DObject ── spawns / places ──► Rune3DObject, Bridge3DObject   ── shows state on ──► Node3DObject, Core3DObject
```

### RuneField3DObject
The scene singleton (`RuneField3DObject._Field3D`). **This object is the field:** its position is the core's (0, 0), its right is field X, its forward is field Y, and its up is the field's up. `FieldToWorld` / `WorldToField` convert using `_FieldUnitsPerMeter`.

| Variable | Description |
| :--- | :--- |
| `_ElementColors` | Rune color per element until the rune shader exists. **Fill Element Colors** (Tools) adds one entry per element |
| `_DefaultRuneColor` | For elements not in the list |
| `_Core` | The core. Moved to this object's position on start. Optional (bridges then end at this object) |
| `_Nodes` | Every ability node. A node's spot is its index, which saved fields point at, so *don't reorder it once fields are saved*. **Fill Node List** (Tools) adds nodes under this object at the end |
| `_RunePrefab` / `_BridgePrefab` | `PFB_Rune_3D` / `PFB_Bridge_3D`. The scripts get added at spawn if the prefabs don't have them |
| `_BridgeHolder` | Where spawned bridges go. Empty = a `<===== Bridged =====>` divider is made under the field the first time a bridge spawns (in play mode it's gone after stopping). Keep it unscaled |
| `_OnEvolved` | Runs when the open shade evolves, after the field reloads |
| `_SlotIndex` / `_HasUnsavedChanges` | Read only |
| Zone Gizmos foldout | Ring colors, same as the 2D field: rings flat on the field, colored frozen / open / locked in play mode, frozen runes outlined |

- **Adding the component** finds the core and nodes under it and both prefabs by name, so nothing needs dragging in
- **Start:** puts the core at the center, then opens the selected slot (`LoadRuneField`), the same as the 2D field
- **Nodes:** built once from `_Nodes` (position, unlocks, lockouts, a copy of the effects) with the same setup checks as 2D (one-way lockouts, gates off a ring)
- **Drawing:** spawns a rune object per rune and places it, rebuilds every bridge, shows power and frozen on runes, on / locked on nodes, and power on the core. Fires `OnDraftChanged`
- `TearBridge(a, b)` removes a bridge (called by a bridge when its tear timer fills)
- **Runs after the camera** each frame (`[DefaultExecutionOrder(100)]`), so a dragged rune follows the pointer with the camera already moved
- **Zone tools:** `SnapNodeToRing` / `GetNextRingOut`, used by the nodes' snap buttons. They keep the node's height
- **Create Dividers** (Tools, Oct 11, 2026): makes a `<===== label =====>` empty under the field for each label (comma-separated for several at once, the field stays selected), on its center, to group nodes in the hierarchy (core / gates / ability nodes). Nodes can sit under dividers at any depth: Fill Node List searches every level, positions are world space, and node indexes come from the Nodes list, not the hierarchy. *Don't move, turn or scale a divider once nodes are under it*
- **Test Tools foldout:** **Test Place Rune** (a rune asset and a spot in field units: a rune is 100 wide, zone 1's edge is 400 out) for quick tests in the editor

**More settings (step 3):** `_TearSlack` (field units past reach before tearing starts), `_InvalidColor`, `_DragLift` (how high a dragged rune floats, meters), `_PickPadding` (extra grab room around a rune, field units), the evolve popup texts (`_EvolveTitle`, `_EvolveNowMessage`, `_EvolveLaterMessage`), `_Popup` ([[Confirm Popup]]), `_SaveButton` (optional, greyed out with nothing to save) and `_OnLeave` (runs after leaving).

### Dragging (mouse)
*Built Oct 10, 2026 (step 3).* Same rules and order as the 2D field:
- **Picking is math, no colliders:** the pointer hits the field's plane, and the rune whose footprint (half `_RuneSize` plus `_PickPadding`) covers that spot is the one under it. The rules already know where every rune is, so there are no colliders or layers to set up. The hovered rune brightens
- **Grab:** a left click on a rune (not over the canvas). Frozen runes can't be grabbed
- **Drag:** the rune follows the pointer on the field, floating `_DragLift` up, kept within reach of its bridges (`ClampToBridges`). It shows where it would land (snapped into a node when on one) or turns `_InvalidColor` over a bad spot. Bridges pulled more than `_TearSlack` past reach start tearing. The camera can still be moved with middle mouse or WASD while dragging
- **Let go:** over the inventory list it comes off the field; on an allowed spot it moves there and bridges to what's in reach; anywhere else it goes back with any torn bridges put back, as if the drag never happened
- **Escape / B while dragging** puts it back the same way

### Draft and Saving
Copied from the 2D field (see [[Rune Field System]], Draft and Saving): **Save Current Field** / `SaveRuneSlot` (asks first when a gate was newly plugged), **Discard Changes**, **Test Clear**, `SelectSlot(index)` and `RequestLeave()` (both ask Save / Discard / Cancel with unsaved changes), `GetPendingUse` / `GetAvailable`, and `PlaceRuneFromInventory` / `CanPlaceFromInventory` (used by the inventory list). Escape / B closes the popup if one is open, otherwise asks to leave. `RefreshCorePower()` gives the open draft the slot's current core power after a level change (used by the test kit).

### Gamepad
*Built Oct 10, 2026 (step 5).*
- **Y** opens and closes the side panel (inventory and shade slots), on the last window used or the inventory the first time. The field finds the scene's `SideWindowManager` on start (`_SidePanel`)
- **Panel focus** *(Oct 10, 2026)*: opening the panel with Y selects its first item (the first rune in the list, otherwise the first tab). The D-pad / left stick move between items and A presses (normal Unity UI navigation). While the panel has focus the left stick doesn't move the camera, A doesn't grab runes on the field, and the reticle hides. B closes the panel. Using the mouse, or the panel closing, ends focus
- **Taking a rune out of the list (gamepad):** A on a rune in the list closes the panel and puts a stand-in rune on the reticle (red over a bad spot). A places it, B or X put it back, Y puts it back and reopens the list. Rune buttons get a `Button` added if their prefab has none (`ElementDataObject._SelectedColor` is its selected tint). Mouse clicks on the list don't do this, the mouse drags instead
- **A** on a rune picks it up (frozen runes can't be). It's **carried**: it follows the reticle and the left stick can pan while carrying. **A** again drops it (same rules as letting go of a mouse drag, including over the inventory list). **X** sends the hovered or carried rune back to the inventory. **B** puts a carried rune back, closes the popup, or asks to leave
- **The canvas on gamepad:** the reticle drives a **virtual mouse** (a pretend mouse device). Pressing A while the reticle is over the canvas holds its left button until A is let go, so the inventory list can be dragged from (hold A, move, let go over the field) and popup buttons pressed. A on the field is left to the field
- Only the **real** mouse counts as "using the mouse", so the virtual one never flips the pointer back to mouse mode

### RuneFieldPointerObject
Where the player is pointing. Lives on the field object (the field adds it if it's missing). A scene singleton (`_Pointer`) that the field, the camera and the labels ask:
- `GetScreenPosition()`: the real mouse, or the reticle after a gamepad was used
- **Reticle** (gamepad): built on the canvas at start, drawn on top, ignored by clicks. **Centered** (faded, `_CenteredAlpha`) sits in the middle and the left stick moves the camera under it; the **right stick** makes it **free** (solid) and moves it around the screen (`_ReticleSpeed` in screen heights per second, `_ReticleResponse` curve, `_StickDeadzone`). After `_RecenterDelay` seconds of left stick only, it goes back to the middle. Look: `_ReticleSize` (pixels at 1080p), `_ReticleColor`, `_ReticleSprite` (empty = a square) [[Notes for the future]]
- `TryGetFieldPoint` / `TryScreenToField`: where the pointer (or any screen spot) lands on the field's plane
- `IsOverCanvas()` / `IsOverInventoryList()`: asks the canvas what's under the pointer once a frame, using the pointer's position (works for the reticle)
- `IsVirtualButtonDown()`: true while A holds the virtual mouse button on the canvas
- `_Camera` (main camera if empty), `_Canvas` (the first canvas found if empty)

### Inventory and Shade Slots
*Built Oct 10, 2026 (step 4).* The 2D UI scripts work with either view through `IRuneFieldView` (in `Scripts/Rune Field/RuneFieldInterface.cs`, see [[Interface List]]). `RuneField3DObject` and `RuneFieldManager` both have it.
- **`IngredientListWindow`**: uses the 2D field on `RuneFieldPointer` if set, otherwise the scene's 3D field. Nothing to assign in the 3D scene
- **`ElementDataObject`**: dragging a button over the 3D field (anywhere the pointer isn't over UI) tints it over a bad spot and drops the rune there. While dragged, the button stops blocking clicks so it doesn't count as UI under the pointer
- **`ShadeSlotManager`**: uses the 2D field if set, otherwise the 3D field

### RuneFieldLabelsObject
*Built Oct 10, 2026 (step 6).* Lives on the field object (the field adds it). Labels are text on the canvas placed at each rune's screen position, so they stay readable at any zoom and never clip into meshes. They're pooled (made once, reused) and drawn behind the rest of the canvas.
- A rune shows "power it has / power it needs" when it's within the label radius of the camera's focus, or is hovered or dragged. The radius blends from `_RadiusZoomedIn` (500 field units) to `_RadiusZoomedOut` (200) with the zoom
- The core always shows "left / total"
- Settings: `_RuneLabelHeight` / `_CoreLabelHeight` (meters above), `_FontSize` / `_CoreFontSize`, `_PoweredColor` / `_UnpoweredColor`. References: `_Canvas`, `_CameraRig` (both found on start if empty)

### RuneFieldTestKitObject
*Built Oct 10, 2026 (step 7). Temp: only for the test build, delete it after the test* [[Notes for the future]]
- On start, puts `_StartingRunes` (rune + count) into the inventory
- Builds its own panels on the canvas: top-left the open slot's name, level and form, plus **Level Up**, **Level Down**, **Reset Slot** (calls the slot's own Reset Slot, now public on `ShadeSO`, then reloads the field), **Refill Runes** (tops the inventory back up to the starting counts) and **Quit**. Bottom-left the controls (`_ControlsText`)
- Settings: `_ShowPanels`, `_FontSize`, `_PanelColor`, `_ButtonColor`, `_Canvas`
- Slot changes only last one play session in a build (assets can't be saved outside the editor)

### Rune3DObject
Colored by element, darker without power (`_UnpoweredBrightness`), brighter under the pointer (`_HoverBrightness`), a bit of `_FrozenColor` mixed in while frozen (`_FrozenBlend`), and the invalid tint while dragged over a bad spot. `_ColorProperty` is the material's color name (`_BaseColor` for HDRP Lit). `_TintRenderers` picks which renderers are colored (empty = all of them).

### Bridge3DObject
Sits on end A, points at end B, Z scale = the distance (the prefab's X and Y scale are kept). `_HeightOffset` lifts it off the field. The tear timer is the 2D one (`_PullTearRequiredTime`, `_MaxPullStrength`, `_PullTickTime`, `_PullDampening`), tinted `_TearColor` while tearing, and it runs on real time so it works while the game is paused.

### Node3DObject
The 3D `EvolutionNode`: `_Unlocks`, `_Lockouts` (other node objects) and `_Effects` (with the same inspector warnings). While on, its `_OnEffect` child shows (a child named `OnEffect` is used if nothing is assigned); while it can't take a rune it's tinted `_LockedColor`. `_ActivationEvent` / `_DeactivationEvent` fire on changes while editing. **Zone Tools** foldout: **Snap To Next Ring Out**, **Snap To Previous Ring In** (added Oct 10, 2026; warns and does nothing if the node is already on or inside ring 1) and **Snap To Ring**.

### Core3DObject
Holds the core's power (left / total, read only). Its label is drawn by `RuneFieldLabelsObject`.

### RuneFieldTint
A small shared helper (plain C# class) that colors a group of renderers with a `MaterialPropertyBlock`, so every rune can have its own color without copying materials. Used by runes, nodes and bridges.

### Setup
1. Add `Node3DObject` to `PFB_Node_3D` and `Core3DObject` to `PFB_Core_3D` (open the prefab, add the component). The scene's copies get it too
2. Add an empty object at the field's center named `Rune Field`. Put the core and the nodes under it
3. Add `RuneField3DObject` to it. The core, nodes and prefabs fill themselves in
4. Reset the shade slot you test with (saved plugs from the 2D scene point at its node list, not this one)
5. Under the canvas, add an empty object with `ConfirmPopup`, press **Build Default Layout**, and drag it into the field's `_Popup` [[Notes for the future]]
6. Add a save button (On Click → `RuneField3DObject.SaveRuneSlot`) and drag it into `_SaveButton`; a close button (On Click → `RuneField3DObject.RequestLeave`) if wanted. `_OnLeave` can stay empty for the test build [[Notes for the future]]
7. Copy the canvas (without `Navigation Area`) and the EventSystem in from `Evolution_UI_scene`. The inventory list and slot buttons find the 3D field by themselves
7b. **Swap the EventSystem's input module:** select the EventSystem and press **Replace with InputSystemUIInputModule** on its Standalone Input Module. The old module can't see the new Input System (no virtual mouse, no gamepad navigation). The pointer logs a warning on start while the old one is there
8. Test build: add an empty object `Test Kit` with `RuneFieldTestKitObject` and fill in Starting Runes. Put `3d_Evolution_UI_scene` in Build Settings [[Notes for the future]]
