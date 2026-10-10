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
**Used in:** `RuneFieldManager` (zone rings, edge bleed, frozen rune outlines).
