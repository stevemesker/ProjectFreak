Plan for cleaning up the 3D rune field after it won the 2D vs 3D call (Oct 27, 2026). Comes from the **Inspector clean-up** item in [[Rune Field 3D Change Requests and to Do]].

Back to [[AA - AI Info]]

---
## Goals
- **The inspector is UX for a forgetful designer.** Setup comes first and folds away once it's done, day-to-day tuning stays open, and anything rarely touched goes in a closed foldout
- **Everything for one job sits in one place.** Tweaking the zone walls shouldn't mean opening three components and an asset. The Rebuild button sits next to the settings it uses
- **Separate buttons stay separate** (Steve, Oct 28): rebuilding just the walls shouldn't rebuild everything. Buttons get grouped and labeled, not merged into one
- **Nothing about how the field works changes.** No serialized field gets renamed, so every scene and asset keeps its values
- Leave room for the **placement pointer and connection preview** (the pole and ball, the ground ring, the warped bridge preview). They get their own component later, like the range overlay. The drag and placement settings get grouped now so the previews have an obvious home [[Notes for the future]]

---
## The groups every rune field component uses
| Group | Open or closed | What goes in it |
| :-- | :-- | :-- |
| Tuning settings | Open | What gets changed day to day |
| Rebuild / Update buttons | Open | Buttons pressed after a tweak |
| First-Time Setup | Closed | Setup buttons, numbered in the order to press them |
| Shader Properties | Closed | Shader property names (they only change if a shader is rewritten) |
| Stand-ins | Closed | Stand-in looks used until the real art exists |
| Test Tools | Closed | Play mode and debug buttons |

---
## Steps
1. **Remove the old 2D rune field** (Steve, Oct 28: delete it, but don't break the dungeon map, which stays a 2D canvas)
	- Code stops pointing at the 2D scripts: `ShadeSlotManager` (always uses the 3D field), `ElementDataObject` / `IngredientDataObject` (the unused `NodePrefabToSpawn`), comments and tooltips that mention the 2D field
	- `NodeBridge` is cut down to just the dungeon map's line (no tearing, no rune field)
	- `EvolutionInterfaces.cs` keeps only `IBridgeable` (`DungeonMapNode` uses it)
	- Steve deletes the 2D scripts, prefab and scene in Unity (Claude can't delete files on his computer). The list is below
2. **Rune Field (`RuneField3DObject`)**: Dragging & Placing group (open, where the previews will go), Colors and Popup Text in closed foldouts, the setup buttons in First-Time Setup with one Set Up Field, save / discard / clear moved into Test Tools
3. **Zones (`RuneFieldZonesObject`)**: the preview slider moves up to Settings, a Rebuild group (walls, stand-in floors, zone lights), First-Time Setup numbered 1 to 4, stand-in floor settings in their own foldout
4. **`SO_RuneField_Zones`**: zone look, walls and zone opening open. Smoke particles, the strip import, shader properties and stand-in look go in closed foldouts. **Rebuild All Borders** button on the asset itself, so wall settings and the rebuild are in one place
5. **Tear material, one obvious place**: `_TearMaterial` on `SO_RuneField_Zones`. When it's empty, an info box says the strip model's material is being used, and a **Use Wall Strip's Material** button copies it in
6. **Bridge, rune, node, core**: shader property names go into a closed Shader Properties foldout
7. **Ring borders**: Fill Crack List stops being a button (Rebuild Ring already does it)
8. **Test Kit**: a key (\` by default) hides and shows its panels. It stays until the dev command console exists [[Notes for the future]]
9. **Docs**: [[Rune Field 3D View]], [[Rune Field System]], [[How To - Zone Visuals]], and the inspector rule in [[Code Style Rules]] (and the code style skill)

Left alone on purpose: the camera, pointer, labels and range overlay inspectors are already tidy.

---
## Files for Steve to delete in Unity
Delete these from the Project window (Unity removes the .meta files with them):
- `Assets/Scripts/UI/Evolution Tree/UI/Rune Field/RuneFieldManager.cs`
- `Assets/Scripts/UI/Evolution Tree/Elements/ElementItem.cs`
- `Assets/Scripts/UI/Evolution Tree/Nodes/EvolutionNode.cs`
- `Assets/Scripts/UI/Evolution Tree/Core/CoreNode.cs`
- `Assets/Scripts/UI/Evolution Tree/DraggableItem.cs`
- `Assets/Scripts/UI/Evolution Tree/ElementObject.cs`
- `Assets/Prefab/UI/Evolution Tree/P_EvoNode_Element.prefab`
- `Assets/Scenes/Dev Scenes/Evolution_UI_scene.unity` (the 2D field scene, not in Build Settings)

**Keep:** `NodeBridge.cs` and `P_EvoNode_Bridge.prefab`. The dungeon map canvas (`PFB_Dungeon Map Canvas`) uses them for its lines.
