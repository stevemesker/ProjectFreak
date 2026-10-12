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

- [ ] Defining zones visually
- need clear walls to define borders between zones. I like the idea of some sort of crack in the floor with a wall of magic light emitting. Need to talk to claude about how to achieve that
- Need to color the floor to show the difference between active zones, previous zones, and future zones
- ![[Pasted image 20261012075302.png]]
- *Talked through with Claude Oct 12, 2026: planned in [[Zone Visuals Plan]] (raised floors, tear on the forward border, smoke on the one behind, gaps at evolution nodes, ring wall and crack tools). Art briefs in `Art/UI/Rune Field/3D Assets/Zones`*

- [x] Allow moving of nodes in previous zones but don't allow bridges to sever if locked
- makes sure evolutions and abilities can't be depowered
- need to handle if other branches are made shorter than the one that makes the evolution that that evolution never loses power
- keep slotted nodes locked if zone lock happens
- allow rune placement in forward zones but cut bridges before saving

- [x] Panning when a dragged rune is held near the screen edge

- [ ] Need to hide the button when dragging to rune field
- ~~Currently the full button pulls out and obscures the area being placed
- would be nice if we could animate it into a circle or something before it collapses and reforms if brought back to the list (will probably add that in a future polish pass)

- [ ] Bridge Tear Shader
- Need to have a shader that builds up and changes color to show the percentage of the pull tear complete
- might need a separate particle when the pull is completed for the break or if the system has to sever the connection via zone crossing or whatever

- [ ] Node glowing effect shader for powering on Nodes
- needs to also have a growth option to lerp when activated/deactivated
- need to make sure the lerping is performant so we don't have a ton of checks and counters for every single node
- would be nice in the future to have each ends of the color match the rune color they're attached to
- need a transition to locked version

- [ ] Rune color/labels functioning
- need to pull either a texture or just a shade to swap on the rune prefab
- data for that will be in the rune SO
- color for now will be in SO but will probably be algorithmically defined when we flesh out rune stats and functionality more
- color should use the color swatch SO

- [ ] add visualizer for node ranges while placing

- [ ] Core Node Shader
- needs to dim when out of power
- not sure if it should dim as you plug in runes or only when there's not enough power for all the chains

- [x] Add dead zone for rune tear
- area that shows the tear effect on the shader but does not start the tearing time

- [x] D-pad quick select from the inventory list

- [ ] Info popup for runes and nodes (needs a button now that Y opens the side panel)

- [x] Shards: dragging a shard into the core, orbiting shard per tier
- Need to make page for this in the side menu

*Finish*
Tasks to be done after locking in the final prototype and deciding to go with the 3d version

- [ ] **Code walkthrough with Steve** of the 3D view scripts, once the 2D vs 3D decision is made

- [ ] Loading into the real game: Hazen terminal, additive scene, camera blend, pause, input map switch, safe rooms

- [ ] Speed and sensitivity options in the settings menu [[Notes for the future]]