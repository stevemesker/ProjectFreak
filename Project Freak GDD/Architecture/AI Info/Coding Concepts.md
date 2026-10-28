C# and Unity features that showed up in Project Freak code, with a short explanation and where they're used. For reviewing at your own pace. New entries get added as they come up.

Back to [[AA - AI Info]]

---
## C# Features

### Storing a function in a variable: `System.Action`
`System.Action` is a variable that holds a function instead of a number or object. You can pass it around and run it later with `?.Invoke()`.
```csharp
System.Action onSave = SaveRuneSlot;   //stores the function, doesn't run it
onSave?.Invoke();                      //runs it now ("?." skips it if it's null)
```
`System.Action<int>` holds a function that takes an int, and so on. C# `event`s (like `OnFieldChanged`, `OnInventoryChanged`) are built on the same idea.
**Used in:** [[Confirm Popup]] (each button remembers what to do), `RuneFieldManager.AskAboutUnsavedChanges` (remembers "switch slot" or "leave" until the popup is answered).

### Small inline functions: lambdas `() => ...`
A lambda is a function written right where it's needed, without a name. `() =>` means "takes nothing, then does this".
```csharp
button.onClick.AddListener(() => PressButton(index));
_Popup.Show("Unsaved Changes", "...", "Save", () => { SaveRuneSlot(); continueWith?.Invoke(); }, ...);
```
`(a, b) => ...` takes two values, like the sort in `RuneField.FindBridgeTargets`.
**Used in:** [[Confirm Popup]], `RuneFieldManager`, `RuneField` (sorting).

### Copying a loop variable before a lambda uses it
A lambda doesn't copy the variables it uses, it *remembers the variable itself*. In a loop, every lambda would remember the same `i`, and by the time a button is clicked the loop is over, so they'd all see the last value. Copying it into a new variable inside the loop gives each lambda its own.
```csharp
for (int i = 0; i < _Buttons.Count; i++)
{
    int index = i; //each button gets its own copy
    _Buttons[i]._Button.onClick.AddListener(() => PressButton(index));
}
```
**Used in:** `ConfirmPopup.Awake`.

### Checking an object's real type: `is` and `as`
A list of a parent type (like `List<RuneEffect>`) can hold any child type. `is` asks "is it this type?", `as` hands it back as that type, or `null` if it isn't.
```csharp
if (effects[i] is EvolveEffect) return true;
StatChangeEffect statChange = effects[i] as StatChangeEffect;
if (statChange == null) continue;
```
**Used in:** `RuneEffect` (`HasEvolve`, `AddUpStats`), `ShadeManager.BuildAbilityList`.

### `virtual` and `override`
A parent class marks a function `virtual` ("children may replace this"), and a child class replaces it with `override`. Calling it on any effect runs the child's version.
**Used in:** `RuneEffect.GetSetupProblem` / `Clone`, overridden by each effect type. Same idea as `AbilityFunction.ActivateAbility`.

### `static` functions
A `static` function belongs to the class itself, not to one object, so it's called on the class name: `RuneEffect.CloneList(list)`. Good for tools that don't need any one object's data.
**Used in:** `RuneEffect` (`CloneList`, `HasEvolve`, `AddUpStats`, `GetListProblems`), `RuneField.FixOneWayLockouts` / `CheckGates`.

A `static` **field** works the same way: one value shared by the whole class instead of one per object. `RuneFieldSettingsSO._editorCache` remembers the settings asset once it's found, for every caller.

### Quick copies: `MemberwiseClone()`
Makes a new object with the same field values. References inside (like an `AbilitySO`) still point at the same asset, which is what we want for effects.
**Used in:** `RuneEffect.Clone`.

### Looking things up by key: `Dictionary`, `TryGetValue`, `HashSet`
- `Dictionary<key, value>` finds an entry by its key instead of its position, so gaps and order don't matter
- `TryGetValue(key, out value)` looks the key up and hands the value back through `out` in one step, returning false if it isn't there
- `HashSet` is a list that's fast at "is this in here?" and can't hold duplicates

**Used in:** `RuneField` (nodes by index, powered runes), `RuneFieldData.CountRunes`, `InventoryManager`.

### Returning extra values: `out`
A parameter marked `out` is filled in by the function, so it can hand back more than one thing.
```csharp
if (_field.TryPlaceRune(element, position, out int runeID) == false) return;
```
**Used in:** `RuneField.TryPlaceRune`, `TryGetComponent(out ...)`, `TryGetValue(out ...)`.

### Overloads: two functions with the same name
C# allows several functions with the same name as long as their parameters differ. The one that runs depends on what you pass in.
```csharp
public float GetBridgeReach(int idA, int idB)                     //what other scripts use
float GetBridgeReach(int idA, int idB, int movingRuneID)          //the private version that also knows which rune is being dragged
```
A common pattern: the public one calls the private one with a default value, so outside scripts keep a simple call.
**Used in:** `RuneField.GetBridgeReach`, `RuneField.FindNodeAt`, `RuneFieldData.CountRunes`.

### Optional parameters
Giving a parameter a default value makes it optional: `Show(title, message, labelA, actionA, labelB = null, ...)`. Leave it out and it uses the default.
**Used in:** `ConfirmPopup.Show`, `RuneFieldLogicTester.Place`.

### Events: `event`, `+=` and `-=`
A C# `event` is a list of functions to run when something happens. Other scripts add their function with `+=` (subscribe) and remove it with `-=` (unsubscribe). The owner runs them all with `?.Invoke()`.
```csharp
public event System.Action OnDraftChanged;           //on RuneFieldManager
_runeField.OnDraftChanged += UpdateList;              //IngredientListWindow.OnEnable
_runeField.OnDraftChanged -= UpdateList;              //IngredientListWindow.OnDisable
```
Always unsubscribe in `OnDisable` (or `OnDestroy`), otherwise the event keeps calling into an object that's gone.
**Used in:** `RuneField.OnFieldChanged`, `RuneFieldManager.OnDraftChanged`, `InventoryManager.OnInventoryChanged`.

### Named constants: `const`
`const` is a value fixed when the code is written, which never changes. Naming it beats a bare number in the code, and a `public const` can be read from other classes through the class name.
```csharp
public const float RingTolerance = 1f;            //in RuneFieldSettingsSO
position.magnitude <= edge + RuneFieldSettingsSO.RingTolerance
```
Good for precision values that aren't design knobs (designer values still go in the inspector).
**Used in:** `RuneField.CoreID` / `NoRuneID`, `RuneFieldSettingsSO.RingTolerance`.

### Filling in text: `string.Format`
`string.Format("-{0}", 3)` gives `"-3"`: `{0}` is replaced with the first value after the text. Handy when the wording is an inspector setting.
**Used in:** `ElementDataObject._PendingFormat`.

### One-line if/else: `a ? b : c`
"If a, then b, otherwise c."
```csharp
_EvolutionData = slot != null ? slot._CurrentEvolution : null;
```

---
## Unity Features

### `[SerializeReference]`
Lets one list hold different child types (stat change, grant ability, evolve), and the inspector asks which type to add. Without it, Unity would only save the parent type's fields.
**Used in:** effect lists on nodes, slots and `AbilityNodeEntry`. Same as `AbilitySO` steps.

### `[FormerlySerializedAs("oldName")]`
Renaming a saved field normally loses its data on every asset. This tells Unity "this used to be called oldName", so the values carry over. Keep it until every asset has been re-saved.
**Used in:** `ElementItemSO._StatBoosts` (was `mypackage`), `ShadeSO._RuneField`, `ShadeEvolutionSO._BaseStats`.

### `[ShowInInspector]` vs `[SerializeField]`
- `[SerializeField]` = Unity **saves** the field and shows it
- Odin's `[ShowInInspector]` = only **shows** it, nothing is saved

For runtime-only data, showing without saving is safer: Unity's saving can quietly swap a plain class for a copy, which breaks two scripts sharing one object.
**Used in:** `ShadeManager._RuntimeEntries`, the shade forms' `_RuntimeEntry`.

### `Graphic`: one type for any UI visual
`Image`, `RawImage` and TextMeshPro text all inherit from `Graphic`, which has `color`. A field typed `Graphic` accepts any of them, so a tint works whatever the prefab uses.
**Used in:** `ElementItem._TintTarget`, `ElementDataObject._TintTarget`.

### `CanvasGroup`
A UI component that controls a whole object and its children at once: `alpha` fades all of it, `interactable` and `blocksRaycasts` turn clicking on or off.
**Used in:** `ElementItem` (unpowered runes fade), `ElementDataObject` (greyed out at 0 available). Both add one automatically if the prefab doesn't have it.

### Editor-only code: `#if UNITY_EDITOR`
Code between `#if UNITY_EDITOR` and `#endif` only exists in the editor and is left out of the built game (anything from `UnityEditor` isn't allowed in a build).
**Used in:** `ShadeSO.MarkChanged` (`EditorUtility.SetDirty` so play-mode changes get saved to disk), `ConfirmPopup.BuildDefaultLayout` (`Undo` so Ctrl+Z works), `RuneFieldManager.SnapNodeToRing` (`Undo.RecordObject`).

`#else` gives the built game its own version: `RuneFieldManager.GetToolSettings` finds the settings asset in the editor and returns `null` in a build.

### `.meta` files and GUIDs
Every asset has a `.meta` file holding a GUID (a unique ID). Assets point at each other by GUID, not by file name, which is why renaming in Unity is safe but deleting the `.meta` breaks references. Moving a script outside Unity needs its `.meta` moved with it.
**Used in:** `SO_RuneField_Settings` was written outside Unity by giving the script's `.meta` a GUID and pointing the asset at it.

### Odin `$` references in attributes
`[InfoBox("$_problems", InfoMessageType.Warning, "HasProblems")]`: the `$` tells Odin to read the text from a field, and the last part names a function that decides if the box shows.
**Used in:** `EvolutionNode`, `ElementItemSO`, `ShadeEvolutionSO`, `RuneFieldLayoutSO` (old).

### Finding assets from code (editor only): `AssetDatabase`
`AssetDatabase` is the editor's view of the project's files. `FindAssets("t:RuneFieldSettingsSO")` returns the GUID of every asset of that type, `GUIDToAssetPath` turns one into a file path and `LoadAssetAtPath` loads it. It doesn't exist in a build, so it always sits inside `#if UNITY_EDITOR`. It saves setup: tools find what they need instead of you assigning it.
**Used in:** `RuneFieldSettingsSO.FindInProject` (the zone gizmos and the node Snap buttons).

### Drawing in the scene: `OnDrawGizmos` and `Handles`
`OnDrawGizmos` runs every time the scene view draws, so a component can draw guides (ranges, rings, paths). `Gizmos` draws lines, spheres and boxes; the editor's `Handles` adds flat circles (`DrawWireDisc`) and text (`Label`). Both show in the Scene view, in the Game view while its Gizmos toggle is on, and never in a build.
**Used in:** `RuneFieldManager` (zone rings, edge bleed, frozen rune outlines), `RuneFieldCameraObject` (zoom curve, edge circle).

### Easing toward a value: `Mathf.SmoothDamp`
Moves a value toward a target a little each frame, fast at first and slowing as it arrives, without overshooting. It needs a variable to remember the current speed (passed with `ref`, so the function can change it).
```csharp
_CurrentZoom = Mathf.SmoothDamp(_CurrentZoom, _TargetZoom, ref _zoomVelocity, smoothTime, Mathf.Infinity, deltaTime);
```
`smoothTime` is roughly how long it takes to get there. Lower = snappier.
**Used in:** `RuneFieldCameraObject` (zoom).

### Time that ignores pausing: `Time.unscaledDeltaTime`
`Time.deltaTime` becomes 0 when the game is paused (`Time.timeScale = 0`). `Time.unscaledDeltaTime` keeps counting, so menus and menu cameras still move while the game is paused.
**Used in:** `RuneFieldCameraObject`.

### Smoothing that's the same at any frame rate: `Mathf.Exp`
Multiplying a speed by a fixed number every frame (like `* 0.9f`) slows it down faster at 144 fps than at 30 fps. Using `Mathf.Exp(-rate * deltaTime)` gives the same slowdown per second at any frame rate.
```csharp
_slideVelocity *= Mathf.Exp(-friction * deltaTime);
```
**Used in:** `RuneFieldCameraObject` (slide, drag speed tracking).

### A flat surface in math: `Plane` and `Raycast`
`Plane` is an endless flat surface that only exists in code, so there's no collider to set up. `plane.Raycast(ray, out distance)` says where a ray (like one from the camera through the mouse) hits it.
```csharp
Plane field = new Plane(Vector3.up, fieldCenter);
Ray ray = _Camera.ScreenPointToRay(mousePosition);
if (field.Raycast(ray, out float distance)) point = ray.GetPoint(distance);
```
**Used in:** `RuneFieldCameraObject` (grab-drag, zoom toward cursor).

### Sideways directions: `Vector3.Cross`
The cross product of two directions gives a third direction at right angles to both. In Unity, `Vector3.Cross(up, forward)` is "right", and `Vector3.Cross(forward, right)` is "up".
**Used in:** `RuneFieldCameraObject` (which way the camera faces, keeping its up steady so it never flips).

### Curves through points: Catmull-Rom
A way to draw a smooth curve that passes through every point it's given (unlike Bezier handles, which pull the curve without touching it). Each piece between two points also looks at the point before and after. The "centripetal" version spaces things by distance so close points don't make loops.
**Used in:** `RuneFieldCameraSO` (the zoom curve through the zoom layers).

### Filling in defaults when a component is added: `Reset()`
Unity calls `Reset()` when a component is first added (or Reset is picked from its menu). Good for finding references automatically so nothing has to be dragged in.
**Used in:** `RuneFieldCameraObject` (finds the main camera and both settings assets).

### One material, many colors: `MaterialPropertyBlock`
Changing `renderer.material.color` makes a copy of the material for that renderer. A `MaterialPropertyBlock` instead overrides a value for one renderer only, with no copies, and `SetPropertyBlock(null)` puts the material's own value back.
```csharp
renderer.GetPropertyBlock(block);
block.SetColor(colorID, Color.red);   //colorID = Shader.PropertyToID("_BaseColor")
renderer.SetPropertyBlock(block);
```
**Used in:** `RuneFieldTint` (runes, nodes and bridges on the 3D field).

### Plain C# helper classes
Not everything has to be a component. A class without `MonoBehaviour` is made with `new` and lives inside whatever made it. Good for shared logic that doesn't need to sit on an object.
**Used in:** `RuneField` (the rules), `RuneFieldTint`.

### How far along a direction: `Vector3.Dot`
`Vector3.Dot(offset, direction)` (with a direction of length 1) says how far `offset` goes along that direction. It's how a world position is turned into field X and Y.
**Used in:** `RuneField3DObject.WorldToField`.

### Pointing at something: `Quaternion.LookRotation`
Turns an object so its forward (+Z) faces a direction, with a second direction saying which way is up.
**Used in:** `Bridge3DObject` (pointing the bridge at its other end), `RuneFieldCameraObject`.

### Waiting while paused: `WaitForSecondsRealtime`
`WaitForSeconds` uses game time, so it freezes while the game is paused. `WaitForSecondsRealtime` uses real time.
**Used in:** `Bridge3DObject` (tear timer).

### Every value of an enum: `System.Enum.GetValues`
Lists every value an enum has, so a loop can cover new values without the code changing.
**Used in:** `RuneField3DObject` (Fill Element Colors).

### Choosing who runs first: `[DefaultExecutionOrder]`
Unity runs every script's `Update` (and `LateUpdate`) in an order it picks. `[DefaultExecutionOrder(100)]` on a class makes it run after scripts with a lower number (the default is 0).
**Used in:** `RuneField3DObject` (runs after the camera, so a dragged rune is placed with this frame's camera).

### Needing another component: `[RequireComponent]`
`[RequireComponent(typeof(Other))]` on a class makes Unity add `Other` whenever this component is added in the editor, and stops `Other` from being removed while this one is there.
**Used in:** `RuneField3DObject` (always has a `RuneFieldPointerObject`).

### Interfaces as a type
A variable can be an interface (`IRuneFieldView _runeField`) instead of a class. It can then hold any object that has that interface, and only the interface's functions can be called on it. `TryGetComponent(out IRuneFieldView view)` finds any component that has it.
**Used in:** `IngredientListWindow`, `ElementDataObject`, `ShadeSlotManager` (they work with the 2D or the 3D field).

### Pretend input devices: `InputSystem.AddDevice` and `InputState.Change`
The Input System can make a device that isn't real (`InputSystem.AddDevice<Mouse>("Name")`). `InputState.Change` writes straight into its state (position, buttons), and everything that listens to mice, like the canvas, reacts to it like a real one. Remove it with `InputSystem.RemoveDevice` when done.
**Used in:** `RuneFieldPointerObject` (the gamepad reticle's virtual mouse).

### World to screen: `Camera.WorldToScreenPoint`
Turns a spot in the world into a pixel on screen. The z of the result is how far in front of the camera it is (negative = behind, so it shouldn't be drawn). On a Screen Space Overlay canvas, a UI object's position *is* in screen pixels, so the result can be used directly.
**Used in:** `RuneFieldLabelsObject`.

### Building UI from code: layout groups and size fitters
A `VerticalLayoutGroup` stacks its children; a `ContentSizeFitter` makes the box grow to fit them; a `LayoutElement` tells the layout how big a child wants to be. Together they lay out a panel without placing anything by hand.
**Used in:** `RuneFieldTestKitObject` (its button and controls panels).

### Values Unity doesn't save: `[System.NonSerialized]`
Marks a field Unity shouldn't save, even on an asset. Handy for caches that get rebuilt, like the zoom curve's measured points.
**Used in:** `RuneFieldCameraSO`.

### Values every shader can read: `Shader.SetGlobal...`
`Shader.SetGlobalFloat`, `SetGlobalVector`, `SetGlobalVectorArray` and `SetGlobalFloatArray` set a value once for every material at the same time. The shader just declares a variable with the same name (in a `.hlsl` file), and the material doesn't need it as a property. Arrays get their size locked the first time they're set, so they're always sent at full size with a separate count of how many entries are used.
**Used in:** `RuneRangeOverlayObject` (sends the ranges to `RuneRangeField.hlsl`).

### Shader Graph code: Custom Function nodes and `.hlsl` files
A Custom Function node set to File runs a function from a `.hlsl` file. The function in the file ends in `_float` (`RuneRangeField_float`), and the node's Name leaves that off. Its inputs and outputs have to match the function's in order and type (`out` values are the node's outputs). Handy for things nodes can't do, like looping over a list.
**Used in:** `RuneRangeField.hlsl` (the range overlay decal).

### HDRP positions: camera relative vs Absolute World
HDRP draws everything relative to the camera, so Shader Graph's Position node set to World gives a position measured from the camera. To compare with positions sent from C# (real world positions), use **Absolute World**.
**Used in:** the range overlay decal graph.

### Reading a key directly: `Keyboard.current`
The Input System's keyboard, read without setting up an input action: `Keyboard.current[Key.Backquote].wasPressedThisFrame` is true on the frame the key goes down. `Keyboard.current` is null when there's no keyboard, so check that first. Fine for dev tools; real controls still go through `PlayerInput` actions so they can be rebound and work on a gamepad.
**Used in:** `RuneFieldTestKitObject` (the key that hides its panels).

### Ordering Odin groups and buttons: `FoldoutGroup` order and `[PropertyOrder]`
Odin draws things in the order they're declared, and a group shows up where its first member is. `[FoldoutGroup("Rebuild", true, 10)]` gives a group its own order (10), so groups made of buttons can be pushed to the bottom (higher = lower down) no matter where their functions sit in the script. `[PropertyOrder(1)]` orders things inside a group, like numbered setup buttons. `true` there means the foldout starts open.
**Used in:** `RuneFieldZonesObject`, `RuneField3DObject`, `ZoneBorder3DObject`.

### Tooltips on buttons and checks in attributes: `PropertyTooltip` and `@`
`[Tooltip]` only works on fields, so Odin buttons use `[PropertyTooltip("...")]` for their hover text. An Odin attribute value starting with `@` is a tiny bit of C# worked out in the inspector: `[ShowIf("@_TearMaterial == null")]` only shows a button while that field is empty.
**Used in:** `RuneFieldZonesObject`, `RuneFieldZonesSO`.
