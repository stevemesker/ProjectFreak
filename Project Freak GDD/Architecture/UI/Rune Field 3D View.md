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
| `_UseEdgePan` / `_EdgePanMargin` / `_EdgePanSpeed` / `_EdgePanCurve` | *Oct 11, 2026.* Edge panning while dragging a rune: on / off, the margin (fraction of the screen's shorter side, 0.08), the speed at the very edge (a multiple of the Move speed at the current zoom, 1) and how speed builds up through the margin (X = depth into it, 0 to 1) |

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
4. **Edge panning** *(Oct 11, 2026)*: while any rune is being dragged (`RuneField3DObject.IsDraggingAnything`: a field rune, a gamepad carry, or a list button), the pointer in the edge margin pans that way, faster the deeper it goes (`_EdgePanCurve`). It only starts once the pointer has been outside the margin during that drag, so drags starting at the inventory on the right edge don't pan straight away, and never while the pointer is over the canvas (like taking a rune back to the list)
5. **Edge:** the focus can't go past the outer ring plus edge bleed. Movement pushing outward is cut, so sliding along the edge works. *A hard stop for now, not an eased one* [[undecided]]
6. **Place the camera** on the curve and point it at the aim spot. Its "up" comes from its fixed right side, so it never flips, even passing straight overhead

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
| `_ElementColors` | *Fallback since Oct 11, 2026:* rune color per element, only for element assets with no `_Palette` yet. **Fill Element Colors** (Tools) adds one entry per element |
| `_DefaultRuneColor` | For runes with no palette and no entry in the list |
| `_CoreColor` | *Oct 11, 2026.* The core's color, used for the core's end of bridges (purple) |
| `_Core` | The core. Moved to this object's position on start. Optional (bridges then end at this object) |
| `_Nodes` | Every ability node. A node's spot is its index, which saved fields point at, so *don't reorder it once fields are saved*. **Fill Node List** (Tools) adds nodes under this object at the end |
| `_RunePrefab` / `_BridgePrefab` | `PFB_Rune_3D` / `PFB_Bridge_3D`. The scripts get added at spawn if the prefabs don't have them |
| `_BridgeHolder` | Where spawned bridges go. Empty = a `<===== Bridged =====>` divider is made under the field the first time a bridge spawns (in play mode it's gone after stopping). Keep it unscaled |
| `_OnEvolved` | Runs when the open shade evolves, after the field reloads |
| `_SlotIndex` / `_HasUnsavedChanges` | Read only |
| Zone Gizmos foldout | Ring colors, same as the 2D field: rings flat on the field, colored frozen / open / locked in play mode, frozen runes outlined. The real zone look is **Zones** below (floors and lights, Oct 12, 2026); *the border tear and smoke are still planned in [[Zone Visuals Plan]]* [[Notes for the future]] |

- **Adding the component** finds the core and nodes under it and both prefabs by name, so nothing needs dragging in
- **Start:** puts the core at the center, then opens the selected slot (`LoadRuneField`), the same as the 2D field
- **Nodes:** built once from `_Nodes` (position, unlocks, lockouts, a copy of the effects) with the same setup checks as 2D (one-way lockouts, gates off a ring)
- **Drawing:** spawns a rune object per rune and places it, rebuilds every bridge, shows power and frozen on runes, on / locked on nodes, and power on the core. Fires `OnDraftChanged`
- `TearBridge(a, b)` removes a bridge (called by a bridge when its tear timer fills)
- **`OnFieldLoaded`** *(Oct 12, 2026)*: fires every time a field is swapped in (opening or switching a slot, the reload after a save or evolve, Reset Slot, Test Clear). The zones listen to it. `GetSlotIndex()` gives the open slot
- **Runs after the camera** each frame (`[DefaultExecutionOrder(100)]`), so a dragged rune follows the pointer with the camera already moved
- **Zone tools:** `SnapNodeToRing` / `GetNextRingOut`, used by the nodes' snap buttons. They keep the node's height
- **Create Dividers** (Tools, Oct 11, 2026): makes a `<===== label =====>` empty under the field for each label (comma-separated for several at once, the field stays selected), on its center, to group nodes in the hierarchy (core / gates / ability nodes). Nodes can sit under dividers at any depth: Fill Node List searches every level, positions are world space, and node indexes come from the Nodes list, not the hierarchy. *Don't move, turn or scale a divider once nodes are under it*
- **Test Tools foldout:** **Test Place Rune** (a rune asset and a spot in field units: a rune is 100 wide, zone 1's edge is 400 out) for quick tests in the editor

**More settings (step 3):** `_TearSlack` (field units past reach before tearing starts), `_InvalidColor`, `_DragLift` (how high a dragged rune floats, meters), `_PickPadding` (extra grab room around a rune, field units), the evolve popup texts (`_EvolveTitle`, `_EvolveNowMessage`, `_EvolveLaterMessage`), the lock-in popup texts (`_LockInTitle`, `_LockInMessage`, `{0}` = how many runes; *added Oct 11, 2026*), `_Popup` ([[Confirm Popup]]), `_SaveButton` (optional, greyed out with nothing to save) and `_OnLeave` (runs after leaving).

### Dragging (mouse)
*Built Oct 10, 2026 (step 3).* Same rules and order as the 2D field:
- **Picking is math, no colliders:** the pointer hits the field's plane, and the rune whose footprint (half `_RuneSize` plus `_PickPadding`) covers that spot is the one under it. The rules already know where every rune is, so there are no colliders or layers to set up. The hovered rune brightens. *Oct 11, 2026:* nodes (rune sized) and the core (`_CoreSize`) are picked the same way, runes first, then nodes, then the core. `GetHoverKind()` / `GetHoverID()` say what's hovered (`RuneFieldType.Hover`: Nothing, Rune, Node, Core; the ID is a rune ID, a node index or `RuneField.CoreID`) and `OnHoverChanged` fires when it changes. Nothing listens yet: the info popup hooks in here once it's designed [[Notes for the future]]. *The plan put this on the pointer; it lives on the field since the field already does the picking math*
- **Grab:** a left click on a rune (not over the canvas). *Since Oct 11, 2026 (Playtest 0 build step 3)* frozen runes can be grabbed too; only locked-in runes (frozen runes plugged into a node) can't
- **Drag:** the rune follows the pointer on the field, floating `_DragLift` up, kept within reach of its bridges (`ClampToBridges`). It shows where it would land (snapped into a node when on one) or turns `_InvalidColor` over a bad spot. Bridges pulled more than `_TearSlack` past reach start tearing. Frozen bridges never tear: they're a hard leash, so a frozen rune stops at the end of them. The camera can still be moved with middle mouse or WASD while dragging
- **Let go:** over the inventory list it comes off the field (a frozen rune shows `_InvalidColor` there and goes back instead); on an allowed spot it moves there and bridges to what's in reach; anywhere else it goes back with any torn bridges put back, as if the drag never happened
- **Escape / B while dragging** puts it back the same way

### Draft and Saving
Copied from the 2D field (see [[Rune Field System]], Draft and Saving): **Save Current Field** / `SaveRuneSlot` (asks first when frozen runes would be locked into their nodes, then when a gate was newly plugged; Cancel on either keeps the draft), **Discard Changes**, **Test Clear**, `SelectSlot(index)` and `RequestLeave()` (both ask Save / Discard / Cancel with unsaved changes), `GetPendingUse` / `GetAvailable`, and `PlaceRuneFromInventory` / `CanPlaceFromInventory` (used by the inventory list). Escape / B closes the popup if one is open, otherwise asks to leave. `RefreshCorePower()` gives the open draft the slot's current core power after a level change (used by the test kit).
- **Cut bridges** *(Oct 11, 2026)*: saving cuts normal bridges crossing the open zone's outer ring (see **Zones and Evolving** in [[Rune Field System]]). They disappear when the saved field reloads. Just before that, the field's `OnBridgesCut` event passes them on, for the break effect (Art Pile in [[Rune Field 3D Plan]]) [[Notes for the future]]

### Gamepad
*Built Oct 10, 2026 (step 5).*
- **Y** opens and closes the side panel (inventory and shade slots), on the last window used or the inventory the first time. The field finds the scene's `SideWindowManager` on start (`_SidePanel`)
- **Panel focus** *(Oct 10, 2026)*: opening the panel with Y selects its first item (the first rune in the list, otherwise the first tab). The D-pad / left stick move between items and A presses (normal Unity UI navigation). While the panel has focus the left stick doesn't move the camera, A doesn't grab runes on the field, and the reticle hides. B closes the panel. Using the mouse, or the panel closing, ends focus. *Oct 11, 2026:* with the panel open but not focused (after a mouse click, or focus dropping off), **D-pad down** gives it focus: nothing selected selects the top rune, a rune already selected stays and navigation moves on from it. The D-pad now counts as gamepad use for the pointer, so it doesn't drop straight back to mouse mode
- **Taking a rune out of the list (gamepad):** A on a rune in the list closes the panel and puts a stand-in rune on the reticle (red over a bad spot). A places it, B or X put it back, Y puts it back and reopens the list. Rune buttons get a `Button` added if their prefab has none (`ElementDataObject._SelectedColor` is its selected tint). Mouse clicks on the list don't do this, the mouse drags instead
- **A** on a rune picks it up (locked-in runes can't be). It's **carried**: it follows the reticle and the left stick can pan while carrying. **A** again drops it (same rules as letting go of a mouse drag, including over the inventory list). **X** sends the hovered or carried rune back to the inventory (refused for frozen runes; a carried one stays carried). **B** puts a carried rune back, closes the popup, or asks to leave
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
- **`ElementDataObject`**: dragging a button over the 3D field (anywhere the pointer isn't over UI) drops the rune there. While dragged, the button stops blocking clicks so it doesn't count as UI under the pointer. *Oct 11, 2026:* once it leaves the panel the button hides and the field shows a stand-in rune where it would land (red over a bad spot, `RuneField3DObject.UpdateListDrag` / `EndListDrag`); back over the list the button shows again. Works for the mouse and the gamepad's hold-A drag. *The shrink-to-a-circle animation is for a polish pass* [[Notes for the future]]
- **`ShadeSlotManager`**: uses the 2D field if set, otherwise the 3D field

### ShadeInfoWindow
*Built Oct 11, 2026 (Playtest 0 build step 6).* A side panel window (next to the inventory and the shade slots) showing the open slot's shade. In `Scripts/UI/Evolution Tree/UI`. Works with the 3D field.
- **Title:** the slot's name, its form and level. *Uses asset names until forms get display names* [[Notes for the future]]
- **Stats:** one row per entry in `_ShownStats` (stat + label, HP to WILD by default): base (the form's `_BaseStats`), what the saved runes and nodes add (`_BonusColor`), the total (the [[Shade Manager]]'s numbers, so the "can't go below 1" rule shows), and the draft's change in brackets (`_DraftUpColor` / `_DraftDownColor`) until it's saved. Refreshes on the field's `OnDraftChanged` and the Shade Manager's `OnShadeEvolved`
- **Render:** the form's `_TetheredArt` (with its rig's Tethered parts) on a stage spawned at `_StagePosition` (1000 m below the field), seen by a camera rendering into a texture shown by `_RenderImage`. Framed to fit (`_ViewDirection`, `_FieldOfView`, `_FramePadding`), turning at `_SpinSpeed`. The camera only renders while the window is open. `_StagePrefab` (optional) is a prefab with the camera and lights, for the lighting pass; without it a plain camera is made [[Notes for the future]]
- **Seen forms:** a form the player hasn't seen shows blacked out (`_UnseenColor` on the render, a "?" over it, "???" as its name). Seen forms are a list on the save slot (see [[Save slot]]). *The real blacked out look is part of the art pass* [[Notes for the future]]
- **Buttons:** **Refresh**, **Build Default Layout** (Tools), **Forget Current Form (play mode)** (takes the open form off the seen list, to check the blacked out look)

**Setup (Steve):**
1. Under the side panel, next to the inventory and shade slot windows, add an empty UI object (**Shade Info Window**), add `ShadeInfoWindow` and press **Build Default Layout**. It stretches to fill its parent like the other windows. Turn it off (closed windows start off)
2. Duplicate a tab button, point its On Click at `SideWindowManager.CloseWindow` with the new window, and change its label

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
Colored by its element's `_Palette` primary color (`ElementItemSO`, *Oct 11, 2026*; the field's `_ElementColors` list is the fallback), darker without power (`_UnpoweredBrightness`), brighter under the pointer (`_HoverBrightness`), a bit of `_FrozenColor` mixed in while frozen (`_FrozenBlend`), and the invalid tint while dragged over a bad spot. `_ColorProperty` is the material's color name (`_BaseColor` for HDRP Lit). `_TintRenderers` picks which renderers are colored (empty = all of them).

### Bridge3DObject
Sits on end A, points at end B, Z scale = the distance (the prefab's X and Y scale are kept). `_HeightOffset` lifts it off the field. The tear timer is the 2D one (`_PullTearRequiredTime`, `_MaxPullStrength`, `_PullTickTime`, `_PullDampening`), tinted `_TearColor` while tearing, and it runs on real time so it works while the game is paused.

*Oct 11, 2026, for the bridge shader (Steve's):* every bridge sends its own values through `RuneFieldTint`'s `MaterialPropertyBlock`, so all bridges share **one material** (no material per color combo, no copy while tearing). Values a shader doesn't have are ignored, so this works before the shader exists:
- **End colors:** `_ColorA` / `_ColorB`, the colors of the two things it joins (a rune's palette color, or the field's `_CoreColor`)
- **Tear:** `_Tear`, 0 to 1, how close the tear timer is to snapping it (updated every tick)
- **Strain:** `_Strain`, 0 to 1, how far into the field's `_TearSlack` it's pulled before the timer starts, for the tear dead zone look

The property names are inspector fields (**Shader Properties** header: `_ColorAProperty`, `_ColorBProperty`, `_TearProperty`, `_StrainProperty`), with placeholder names to match once the shader exists [[Notes for the future]]. The `_TearColor` tint stays until the shader shows tearing itself

### Node3DObject
The 3D `EvolutionNode`: `_Unlocks`, `_Lockouts` (other node objects) and `_Effects` (with the same inspector warnings). While on, its `_OnEffect` child shows (a child named `OnEffect` is used if nothing is assigned); while it can't take a rune it's tinted `_LockedColor`. `_ActivationEvent` / `_DeactivationEvent` fire on changes while editing. **Zone Tools** foldout: **Snap To Next Ring Out**, **Snap To Previous Ring In** (added Oct 10, 2026; warns and does nothing if the node is already on or inside ring 1) and **Snap To Ring**.

### Core3DObject
Holds the core's power (left / total, read only). Its label is drawn by `RuneFieldLabelsObject`. *Oct 11, 2026:* also `HasRunesWaiting()` (`RuneField.HasRunesWaitingForPower`: a rune that could have power doesn't; planning runes don't count), for the core shader's bright / normal / dull purple. The shader's property names become inspector fields here once it exists [[Notes for the future]]

### RuneFieldTint
A small shared helper (plain C# class) that colors a group of renderers with a `MaterialPropertyBlock`, so every rune can have its own color without copying materials. Used by runes, nodes and bridges. *Oct 11, 2026:* `StartValues` / `PutColor` / `PutFloat` / `PutTint` / `ApplyValues` set several shader values at once (the bridges use it)

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

---
## Zones
*Built Oct 12, 2026 ([[Zone Visuals Plan]]): step 1 (floors, lights, seen zone) working in Unity; steps 2 to 5 (walls, cracks, border looks, the zone opening) the same evening, all working in Unity with stand-in art.* Scripts in `Scripts/Rune Field/3D`. How to change all of it: [[How To - Zone Visuals]]. Everything comes from one number, the open zone (`RuneField.GetOpenZone()`):

| Zone | Height | Zone light |
| :--- | :--- | :--- |
| Below the open zone (**frozen**) | Raised (the field's height) | `_FrozenLight` (a bit blue) |
| The **open** zone | Raised | `_OpenLight` (neutral) |
| Past it (**locked**) | Lowered by `_LiftHeight` | `_LockedLight` (muted, darker) |

| Border (ring k = zone k's outer edge) | Look |
| :--- | :--- |
| Ring N (the forward border) | **Tear**: bright, cracks lit |
| Ring N − 1 | **Smoke**: smoky, cracks faded out |
| Older rings | **Off** |
| The field's edge (last ring) | No border at all (never glows) |

```text
RuneField3DObject ──OnFieldLoaded──► RuneFieldZonesObject ──ShowState──► Zone3DObject (one per zone)
                                        │ compares with the slot's seen zone        │ lifts / lowers everything under it
                                        │ (ShadeSO._SeenOpenZone), then saves it    │ its renderers on its light layer
                                        ▼                                           ▼
                                   SO_RuneField_Zones (lift, light looks)      its zone light (color, intensity)
                                        │
                                        └──ShowLook(tear, smoke, reveal)──► ZoneBorder3DObject (one per gate ring)
                                                                              │ wall (stays put) + cracks (ride the zone outside)
                                                                              ▼
                                                                         MaterialPropertyBlock: _Tear, _Smoke, _Reveal, _Overcharge
```

### The seen zone
Each slot saves the last open zone the player saw (`_SeenOpenZone` on the slot, see [[Shade (Runtime)]]). Every time the field loads it compares:
- **Same, or never seen (0):** snaps to the look
- **Behind** (the shade evolved since): the zone opening plays (see **Zone opening**), and the seen zone is saved once it finishes. If something else loads first (another slot), it's dropped and plays again next time
- **Ahead** (the slot was reset): snaps
- Otherwise the real open zone is saved as seen straight away. Evolving on save reloads the field, so the opening plays right after the save; leveling into a gate mid-fight plays it the next time that slot's field opens

### Light layers
The floors are tinted with HDRP **light layers**, not materials, so kit pieces need nothing special:
- Zone N's renderers are on the **default** layer plus **Light Layer N** (decal layers are left alone). The key light lights the default layer, so it still lights and shadows the floors; each zone light only lights its own layer
- Runes, nodes, the core and anything not under a zone stay on the default layer only, so they're never tinted
- **`ZoneTintSkipObject`**: a marker. Renderers on it or under it stay on the default layer only (glowing crystals, magic bits)
- Zone lights are copies of the key light with shadows, the sky (`interactsWithSky`), fog (`affectsVolumetric`) and color temperature turned off, and their light layer set to their zone's
- HDRP has 8 light layers and layer 0 is the default, so the field can have up to 7 zones

### RuneFieldZonesSO
Created from **Create → Rune Field → Zones**, one asset `SO_RuneField_Zones`. **Changes made while playing stay after stopping.** The code only reads it.

| Variable | Description |
| :--- | :--- |
| `_LiftHeight` | How far a locked zone sits below the raised level (meters, 0.25). Tuned by feel [[undecided]] |
| `_OpenLight` / `_FrozenLight` / `_LockedLight` | Each state's zone light: `_Color` and `_Intensity` (in the key light's unit, lux for the scene's directional Sun, which is 130000). Defaults are a starting guess: 30000 neutral, 30000 blue, 8000 muted purple. Final colors [[undecided]] |

| `_WallStrip` / `_CrackStrip` | The strip the walls (and cracks) are built from. Wall empty = a stand-in card (3 m, 0.4 m under to 1 m up, two-sided). Crack empty = the wall strip. Saving over either from Maya rebuilds every border by itself |
| `_StripScale` / `_FlipLength` / `_FlipDepth` | Fixes for a strip that comes in at the wrong size or turned around. The strip's +Z faces the core by default |
| `_TearMaterial` / `_SmokeMaterial` | *Oct 13, 2026: two materials the walls swap between (replaced the single `_BorderMaterial`).* Tear = the forward wall and every crack; empty = the strip model's own material (`MAT_ZoneWall_Flame`, found on Rebuild). Smoke = the wall behind; empty = a grey stand-in |
| `_HeightProperty` / `_HiddenHeight` / `_SwapPoint` | The height value both materials share (`_GradiantHeight`). The code animates it from `_HiddenHeight` (gone) to the material's own value (full). `_SwapPoint` (0.5) is where in the tear → smoke blend the tear wall finishes going out and the smoke wall starts coming in |
| `_MeshFolder` | Where the baked wall and crack meshes are saved (`Assets/Generated/Rune Field Zones`) |
| `_RevealProperty` / `_OverchargeProperty` | Optional extra values for the tear material: the light-up sweep (0 to 1, read with the mesh's UV2.x) and the overcharge pulse (always 0 for now) |
| `_StandInColorProperty` / `_StandInTearColor` / `_StandInSmokeColor` | The stand-in tint, only used where a material is empty and HDRP's default material stands in |
| `_UseSmokeParticles` | *Oct 13, 2026, off by default:* smoke particles on the smoky border. Steve handles the smoke in the shader |
| `_SmokePrefab` | Steve's smoke particles (Simulation Space World, moves its own particles). Empty = stand-in grey puffs (`_StandInSmokeLifetime`, `_StandInSmokeSize`, `_StandInSmokeRise`, `_StandInPuffColor`) [[Notes for the future]] |
| `_SmokePerMeter` | Puffs per second per meter of ring at full smoke (1.5) |
| `_RiseTime` / `_RiseCurve` | The zone opening: the zone rising and its light changing (1.2 s) |
| `_BorderBlendTime` / `_BorderBlendCurve` | Tear to smoke and smoke to off (1 s) |
| `_RevealDelay` / `_RevealTime` / `_RevealCurve` | The new border's light-up sweep: when it starts (0.6 s in) and how long it takes (2 s) |
| `_StepGap` | Pause between steps when catching up more than one zone (0.3 s) |

### RuneFieldZonesObject
On the field object, next to `RuneField3DObject` (it requires one). Adding it finds the zones asset and any zones under the field.

| Variable | Description |
| :--- | :--- |
| `_Settings` | `SO_RuneField_Zones` |
| `_LiveTuning` | Editor play mode only: shows the asset's values every frame, so tuning shows straight away (on by default) |
| `_StandInSkirtDepth` / `_StandInSegments` / `_StandInMaterial` | The stand-in floors: rock face depth (0.5 m), roundness (128), material (empty = HDRP's default) |
| `_Zones` | Every `Zone3DObject`. A warning shows if it's empty, has an empty entry, or two zones share a number |
| `_Borders` | Every `ZoneBorder3DObject`. **Set Up Borders** makes them and fills this in |
| `_KeyLight` | The shadowed directional light the zone lights copy. Empty = found by **Set Up Zone Lighting** |
| `_ShownOpenZone` / `_PlayingOpening` | Read only |

- **Start:** puts every zone's renderers on its light layer (so kit pieces never need setting by hand), then listens to the field's `OnFieldLoaded` (and catches a field that already loaded)
- `ShowOpenZone(zone)` snaps every zone and border to its look; `GetZoneState(zone, openZone)` says frozen / open / locked
- **Tools foldout:**
  - `_PreviewOpenZone` slider: shows any open zone in edit mode without pressing play (Ctrl+Z works). 4 = every zone open
  - **Set Up Zones:** makes any missing `Zone 1` to `Zone N` (the field's zone count) under a `Zone Floors` object at the field's center, gives zones with nothing to draw a stand-in floor, fills the list and applies the layers. Safe to press again
  - **Set Up Borders:** presses Set Up Zones first, then makes any missing `Ring k Border` (ring 1 to the last gate ring) under a `Zone Borders` object at the field's center and builds them all. Safe to press again
  - **Rebuild All Borders**, **Fill Border List**
  - **Rebuild Stand-In Floors:** remakes them (after a zone width, skirt or material change)
  - **Set Up Zone Lighting:** gives every zone a light under a `Zone Lights` object (a copy of the key light, see Light layers). Pressing it again keeps them and turns them to match the key light
  - **Name Light Layers:** names Light Layers 1 to 4 "Zone 1" to "Zone 4" in the HDRP Global Settings. Only layers still on their default name are renamed
  - **Apply Zone Layers**, **Fill Zone List**
- **Test Tools:** **Replay From Seen Zone (play mode)** sets the open slot's seen zone to a number and reloads the field, as if the shade evolved while the field was closed, so the zone opening plays (throws away unsaved changes)

### Zone3DObject
One zone: everything under it is that zone's floor (Steve's floor mesh, kit rocks, the cracks of the ring inside it, later burn decals) and lifts with it.

| Variable | Description |
| :--- | :--- |
| `_ZoneNumber` | 1 is the disc around the core, then rings outward. Also its light layer |
| `_ZoneLight` | Its shadowless zone light (filled by Set Up Zone Lighting) |
| `_State` | Read only |

- `ShowState(state, settings)` snaps to a state; `ApplyLook(drop, color, intensity)` sets the height and light directly (the zone opening blends through it)
- Only its height along its parent's up changes, so the raised level is its parent's center height (the field's plane)
- `ApplyZoneLayer()` (also a Tools button on the zone)
- **Stand-in floor:** a disc or ring made by code (top at the zone's height, a rock face down from the outer edge), saved inside the scene as a `Stand-In Floor` child. Delete it once the real floor is under the zone [[Notes for the future]]

### Zone opening
*Built Oct 12, 2026 (step 5).* When the seen zone is behind, `RuneFieldZonesObject` snaps to the seen zone, then plays one step per new zone N (a coroutine on real time, so it works while paused; input is never blocked):
1. Zone N rises (`_LiftHeight` → 0) and its light goes locked → open; zone N − 1's light goes open → frozen (`_RiseTime`, `_RiseCurve`)
2. Border N − 1 swaps tear → smoke: its tear wall and cracks animate out, then (past `_SwapPoint`) its smoke wall animates in. Border N − 2's smoke animates out (`_BorderBlendTime`, `_BorderBlendCurve`)
3. After `_RevealDelay`, border N's tear wall and cracks animate in, and `_Reveal` runs 0 → 1 with them (`_RevealTime`, `_RevealCurve`). The edge ring has no border, so the last zone skips this

Catching up more than one zone plays the steps one after another with `_StepGap` between, then snaps to the end look and saves the seen zone. A save that evolves reloads the field twice; the second load is ignored if the same opening is already playing. *A camera move during it, the bridge break before it and the overcharge pulse are still to come* [[Notes for the future]]

### ZoneBorder3DObject
*Built Oct 12, 2026 (steps 2 and 4).* One gate ring's border: `Zone Borders/Ring k Border`. Holds the **ring wall tool** and shows the border's look.

| Variable | Description |
| :--- | :--- |
| `_Ring` | Which ring (1 = zone 1's outer edge) |
| Wall Shape: `_BottomDepth` | How far below the raised floor the wall's bottom reaches (0.4 m). The strip's part under its Y = 0 is stretched to this, so the wall always goes under a lowered zone. *This is how Steve's card (bottom at −0.24 m) works without changing it in Maya* |
| Wall Shape: `_HeightScale`, `_RadiusOffset` | Height multiplier; moves the wall out or in (m) |
| Wall Shape: `_RevealStartAngle` | Where the sweep starts (degrees, 0 = field right, 90 = field forward). It spreads both ways and meets on the far side |
| Lean: `_LeanAngle`, `_LeanCurve` | Tilt outward the higher it goes (degrees), and the tilt's shape up the wall (straight line = straight slant) |
| Noise: `_HeightNoise`, `_WobbleNoise`, `_LeanNoise`, `_NoiseBumps`, `_NoiseSeed` | Height (fraction), in / out (m) and lean (degrees) wobble, how bumpy, and the pattern. Read around a circle, so it loops with no seam |
| Hand Spots: `_Spots`, `_SpotFalloff` | Hand-placed changes (`WallSpotEntry`: `_Angle`, `_Width` in m each side, `_HeightBoost`, `_Push` m, `_Lean` degrees), faded toward their edges by the curve. Dragged around the ring with scene handles |
| Gate Gaps: `_GapMargin`, `_GapRampWidth`, `_GapRampCurve` | Around each evolution gate on the ring the wall is gone across the gate plus the margin (0.3 m), then slopes back up over the ramp (1.5 m). Gates are found from the field's node list (`RuneField3DObject.GetNodeObjects()`, nodes with an Evolve effect sitting on this ring) |
| `_WallRenderer` | The `Wall` child. Made by Rebuild Ring |
| `_CrackRenderer` | `Ring k Cracks`, under zone k + 1 (so it lifts with that floor), with a `ZoneTintSkipObject`. Made by Rebuild Ring |
| `_Cracks` | This ring's cracks, branches included (**Fill Crack List**) |
| Runtime Data | `_Radius`, `_StripLength` (should say 4 for Steve's card), `_CopyCount`, `_GateAngles`, `_GateHalfWidth`, `_Look` (tear, smoke, reveal) |

**Rebuild Ring** (Tools, edit mode only; also run by Set Up Borders, Rebuild All Borders, a crack's Rebuild My Ring, and the strip being reimported):
1. Reads the strip (`ZoneStripBuilder`). Warns if its end edges don't match (the copies then aren't welded)
2. Fits a whole number of copies around the ring (stretched a little so the circle closes exactly) and welds each copy's start edge onto the one before. The first and last copy aren't welded, so U can count up all the way round
3. Bends every vertex onto the circle: along the strip = around the ring, the strip's depth = radius (its +Z faces the core), height stays height, then the noise, spots, gate gaps and lean
4. Builds this ring's cracks into one mesh in the crack renderer's space
5. Saves both as `M_RuneField_ZoneWall_Ring{k}` / `M_RuneField_ZoneCracks_Ring{k}` in `_MeshFolder` (an existing asset is overwritten in place, so references keep working)

**Baked channels** (match [[Zones - Shader Graph Recipe]]): UV0 = the strip's UVs with U carried on copy to copy; UV2.x = reveal order (0 to 1, distance the sweep travels to get there, divided by the longest on this ring, so walls and cracks light at the same speed); UV2.y = height up the strip (0 bottom, 1 top); UV3.x = gate gap mask (1 normal, 0 in a gap; always 1 on cracks); UV3.y = distance along a crack (0 on walls).

**Look** (`ShowLook(look, height, crackHeight, reveal, settings)`, *changed Oct 13, 2026*): `look` (`RuneFieldType.BorderLook`: Off, Tear, Smoke) picks the wall's material, swapped on `sharedMaterial` (no copies). `height` (0 to 1) is sent as the height value, from `_HiddenHeight` to the material's own value, through `RuneFieldTint`. Cracks show the tear material at `crackHeight` only while the wall is the tear, and are off otherwise. Anything at height 0 or Off has its renderer turned off. `_Reveal` and `_Overcharge` (0) are sent too. Only the stand-in (HDRP's default material) gets tinted.

**Smoke:** *off unless `_UseSmokeParticles` is ticked.* While smoky (play mode), puffs are emitted at random spots on the ring, less often inside gate gaps, `_SmokePerMeter` × circumference × smoke per second (real time, at most 200 a frame). The emitter is made the first time: an instance of `_SmokePrefab`, or a stand-in particle system (grey opaque squares that rise and shrink).

**Tools:** **Rebuild Ring**, **Fill Crack List**, **Add Crack** (an angle in degrees: makes a 3-point crack heading straight out, under zone k + 1, and selects it). **Gizmos** when selected: the ring (white), gate gaps (red) and their slopes (orange), hand spots' reach (cyan).

### ZoneCrackPath
*Built Oct 12, 2026 (step 3).* One floor crack: the wall's strip laid along a path instead of a circle, tall where it leaves the ring and shrinking to nothing at its tip. Keep crack objects under the zone outside their ring.

| Variable | Description |
| :--- | :--- |
| `_StartBorder` | The ring it leaves from |
| `_ParentCrack` / `_BranchAt` | Optional: makes it a branch starting on another crack, this far along it (0 to 1). A branch belongs to its parent's ring |
| `_Points` | The path, start to tip, relative to this object. The first point snaps onto the ring (keeping its angle) or the parent crack, and keeps its own height |
| `_HeightScale` / `_WidthScale` | Size compared to the strip (0.5 / 1) |
| `_HeightCurve` | Height along the crack (1 at the start to 0 at the tip) |
| Noise: `_HeightNoise`, `_WobbleNoise`, `_NoiseBumps` (per meter), `_NoiseSeed` | Same idea as the wall's |

- **Buttons:** **Add Point** (1 m past the tip, same heading), **Remove Last Point** (keeps at least 2), **Rebuild My Ring**
- **Reveal order** carries on from the ring: the sweep's distance around the ring to where the crack leaves (a branch: to its parent's start plus along the parent), then along the crack, so cracks light as the sweep passes them
- The strip below its Y = 0 is only scaled by `_HeightScale`, so it stays in its trench; above it follows the curve and noise
- Parent loops (a crack set as its own parent through others) are caught after 32 steps with a warning
- **Gizmo:** its path as a pink line, always shown

### ZoneStripBuilder
A plain C# helper (like `RuneFieldTint`) shared by the walls and cracks: reads a strip mesh (or makes the stand-in card), applies the scale and flips (one flip also reverses the triangles so the faces stay the right way out), pairs each start-edge vertex with its matching end-edge vertex, lays copies end to end (`Layout`), and builds / saves meshes (`MakeMesh`, `SaveMesh`; UV channels 2 and 3 are Shader Graph's UV2 and UV3). If the strip's data can't be read (Read/Write off, in play mode) it says so.

### Editor tools (`Editor/ZoneToolsEditor.cs`)
- **Scene handles** (`ZoneToolsEditor`, hooked into the scene view on load): with a border selected, a cyan dot per hand spot to drag around the ring; with a crack selected, a pink dot per point to drag across the floor (a snapped start shows as a white ring). Undo works. Press Rebuild to see the change on the mesh
- **Auto rebuild** (`ZoneStripImportWatcher`, an `AssetPostprocessor`): when the zones asset's wall or crack strip is reimported (saved over from Maya), every border in the open scenes rebuilds right after the import (edit mode only)

### Setup (Steve)
1. On the `Rune Field` object, add `RuneFieldZonesObject`. *Oct 12, 2026:* if there's no `SO_RuneField_Zones` yet, the Tools buttons, the slider or pressing play make one next to `SO_RuneField_Settings` and fill it in (editor only)
2. Open **Tools**, press **Set Up Zones**, then **Set Up Zone Lighting**, then **Name Light Layers**, then **Set Up Borders** (Oct 12, 2026)
2b. Drag `M_ZoneWall_WallEffectCard`'s mesh into the zones asset's `_WallStrip` and press **Rebuild All Borders**. Check a border's `_StripLength` says 4; if not, fix `_StripScale`. Add cracks with a border's **Add Crack**
3. Move the slider to check each open zone. Then press play and use the test kit
4. If the zone lights don't tint anything: check the rune field camera's Frame Settings have **Light Layers** on (camera → Rendering → Custom Frame Settings, or the default in HDRP Global Settings → Frame Settings → Camera → Lighting)
5. Real floors: put them (and kit pieces) under `Zone Floors/Zone N`, top surface at the zone object's height, then delete that zone's `Stand-In Floor`
