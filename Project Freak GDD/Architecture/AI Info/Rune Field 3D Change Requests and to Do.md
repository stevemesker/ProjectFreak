This is a log of changes after play testing the 3d rune field prototype

*Playtest 0:*
Overall results very positive. System works better than original 2d canvas version. Tools are easy to use for the most part and readability and usability has not been sacrificed converting to 3d

---
Things to address
- [x] Add current shade info tab
- along with the shade slot and rune inventory tab, need an area that shows current stats of the shade
- thinking it will be another tab but could possibly be its own window at the bottom that stays open
- Should show base stats, stats that are changing because of runes, and possibly show either an image of the current shade evolution or an actual render of it
- if we include that render of the shade, I want to eventually set it so that if you have not had that shade evolution before it's blacked out and you don't know what it looks like until you watch the evolution cutscene

- [x] Defining zones visually
- need clear walls to define borders between zones. I like the idea of some sort of crack in the floor with a wall of magic light emitting. Need to talk to claude about how to achieve that
- Need to color the floor to show the difference between active zones, previous zones, and future zones
- ![[Pasted image 20261012075302.png]]
- *Talked through with Claude Oct 12, 2026: planned in [[Zone Visuals Plan]] (raised floors, tear on the forward border, smoke on the one behind, gaps at evolution nodes, ring wall and crack tools). Art briefs: [[Zones - Art To Do]]*
- *Built Oct 12, 2026 and working in Unity with stand-in art (floors, tint, walls, cracks, smoke, the zone opening). How to change it: [[How To - Zone Visuals]]. The art pass is Steve's ([[Zones - Art To Do]])*

- [x] Allow moving of nodes in previous zones but don't allow bridges to sever if locked
- makes sure evolutions and abilities can't be depowered
- need to handle if other branches are made shorter than the one that makes the evolution that that evolution never loses power
- keep slotted nodes locked if zone lock happens
- allow rune placement in forward zones but cut bridges before saving

- [x] Panning when a dragged rune is held near the screen edge

- [ ] Need to hide the button when dragging to rune field. *Card: [[Shrink animation on dragged list buttons]] (low priority, after the final UI art)*
- ~~Currently the full button pulls out and obscures the area being placed
- would be nice if we could animate it into a circle or something before it collapses and reforms if brought back to the list (will probably add that in a future polish pass)

- [x] Bridge Tear Shader (SH_NodeBridge hooked up Oct 23, 2026: end A faces the core, colors and tear progress sent per bridge)
- Need to have a shader that builds up and changes color to show the percentage of the pull tear complete
- might need a separate particle when the pull is completed for the break or if the system has to sever the connection via zone crossing or whatever

- [ ] Node glowing effect shader for powering on Nodes. *Oct 29, 2026: became four node forms (idle, plugged in, locked, frozen), cards [[Node Art]] and [[Node state forms and transitions]]. Lines showing unlocks / lockouts: [[Node link tool and display]], [[Node link textures and shaders]]*
- needs to also have a growth option to lerp when activated/deactivated
- need to make sure the lerping is performant so we don't have a ton of checks and counters for every single node
- would be nice in the future to have each ends of the color match the rune color they're attached to
- need a transition to locked version

- [ ] Rune color/labels functioning. *Palette color done Oct 11, 2026. Oct 29, 2026: a material and effect per rune, cards [[Rune material and effect slots]], [[Rune tiers and their looks]], [[Rune tier shaders]]*
- need to pull either a texture or just a shade to swap on the rune prefab
- data for that will be in the rune SO
- color for now will be in SO but will probably be algorithmically defined when we flesh out rune stats and functionality more
- color should use the color swatch SO

- [x] add visualizer for node ranges while placing
- *Researched with Claude Oct 23, 2026 (Factorio, Satisfactory, Death Stranding, KSP). Range overlay built Oct 24, 2026: floor decal + runes lighting up from 150% of bridge length. See [[Rune Field 3D View#Range Overlay]]. Decal graph, styling and the RangeEffect are Steve's*

- [ ] Placement preview: pole with a ball that hits the floor (Steve's model), ground ring turning red / green through its material, and a warped version of the bridge material showing the bridges a drop would make (straightens out once in range) [[Notes for the future]]. *Oct 29, 2026: no ground ring, the range overlay tints red instead; only new bridges get a preview. Cards [[Placement preview - pointer line and preview bridges]], [[Placement pointer and preview bridge art]]*

- [ ] Core Node Shader. *Oct 29, 2026: glows brighter with spare power (full at 10), dull and strobing while over budget. Cards [[Core shader]], [[Send core glow values]]*
- needs to dim when out of power
- not sure if it should dim as you plug in runes or only when there's not enough power for all the chains

- [x] Add dead zone for rune tear
- area that shows the tear effect on the shader but does not start the tearing time

- [x] D-pad quick select from the inventory list

- [ ] Info popup for runes and nodes (needs a button now that Y opens the side panel). *Oct 29, 2026: hover shows the name, tap / right stick click shows the details. Card [[Node stat displays]] (merged)*

- [x] Shards: dragging a shard into the core, orbiting shard per tier
- Need to make page for this in the side menu. *Card: [[Shard tab and slotting]], art [[Orbiting shard models]]*

*Finish*
Tasks to be done after locking in the final prototype and deciding to go with the 3d version

- [ ] **Code walkthrough with Steve** of the 3D view scripts. *Oct 27, 2026: 3D won, so this is ready to do. Card: [[Code walkthrough of the 3D rune field]]*

- [x] **Inspector clean-up** *(asked Oct 13, 2026, for the post-prototype talk)*. *Done Oct 28, 2026, along with removing the 2D field: see [[Rune Field Clean-Up Plan]]. The lens became the **Inspector layout** rule in [[Code Style Rules]]*: the rune field and zone components have too many buttons and settings, and it's hard to remember what they do
- **Why it happened (Steve):** we build in steps and never go back to remove the buttons and settings each step added, and we haven't been looking at it as building tools for a designer
- **The lens for the clean-up:** treat the inspector as UX for a forgetful designer. Arrange everything in the order you'd build out and then adjust the system (set up → place → tune), so the next thing to do is always the next thing you see. Once the clean-up shows what that takes, decide how to phrase it in the code style skill
- Go through every button on `Rune Field` (field, pointer, labels, zones) and the zone / border / crack objects: drop the ones that aren't needed any more, merge setup steps into fewer buttons (one "Set Up Everything"?), and give every button a clear label (and a tooltip or info box saying what it does)
- Put rarely touched settings into closed foldouts (stand-in floors, stand-in look, smoke particles, strip scale and flips, mesh folder, shader property names), so the inspector shows what gets tuned day to day first
- Consider moving setup-only tools to a separate editor window or a "Setup" foldout that stays closed
- Make the tear material easier to find: falling back to the strip model's material (needs Wall Strip set and a rebuild) was confusing on Oct 13. Tear Material is set by hand on `SO_RuneField_Zones` for now. Decide on one obvious place for it
- Update [[How To - Zone Visuals]] and [[Rune Field 3D View]] after

- [ ] Loading into the real game: Hazen terminal, additive scene, camera blend, pause, input map switch, safe rooms. *Oct 29, 2026: a plain scene swap from access points (hub, before bosses, rare safe rooms), no additive load, blend or pause. See [[Rune Field Finish Plan]]. Cards [[Rune field access points]], [[Load the rune field from an access point]], [[Remember the dungeon across a field visit]], [[Hub lamp and dungeon field kit]]*

- [ ] Speed and sensitivity options in the settings menu [[Notes for the future]]