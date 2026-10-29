## Overview
How the rune field's zones look and change in the 3D view: the raised floors, the crack of magic light on the forward border, the smoke on the border behind, and the tools for building the walls and floor cracks. Worked out with Claude on Oct 12, 2026, from the zones item in [[Rune Field 3D Change Requests and to Do]] (it was in the Art Pile of [[Rune Field 3D Plan]]). The rules side of zones (open / frozen / locked, gates, the cut on save) is in **Zones and Evolving** in [[Rune Field System]].

Back to [[AA - AI Info]] · [[Architecture Atlas]]

**Status (Oct 12, 2026):** all five build steps built and working in Unity with stand-in art. Written up in **Zones** in [[Rune Field 3D View]]; how to change it all is [[How To - Zone Visuals]]. *Oct 13, 2026: the walls now swap between a tear material and a smoke material (sharing a height value the code animates), and smoke particles are off by default (Steve does the smoke in the shader). See **Zones** in [[Rune Field 3D View]].* Next: the art pass (Steve, [[Zones - Art To Do]]), then a run-through of the guide [[Notes for the future]]

**Art briefs for Steve** (in `UI/Rune Field/Zone Art`):
- [[Zones - Art To Do]]: every mesh, material and effect to make, with sizes
- [[Zones - Shader Graph Recipe]]: the shader's inputs and a node-by-node starting point
- [[Zones - Reference.png]] (in Attachments): sizes from above and the side, and how the wall strip should be built
- [[Zones - Lighting Guide]]: how the zone tint works with light layers, and how to build the final lighting

![[Pasted image 20261012075302.png]]

---
## The Look
*Agreed Oct 12, 2026 (Steve's sketches).*

Each zone is its own piece of floor: zone 1 is a disc around the core, zones 2 to 4 are rings (pipe shapes). The art is rough rock with cracks in it.

**Everything comes from one number, the open zone (N).** The field already works it out (`GetOpenZone()`), so the visuals don't need rules of their own:

| Piece | Look |
| :--- | :--- |
| **Open zone** (N) | Raised, normal (lighter) floor |
| **Frozen zones** (below N) | Raised, floor tinted a bit blue (the "frozen, locked in, slow" feel) |
| **Locked zones** (above N) | Lowered, floor tinted muted and slightly darker. Purple for now; might end up blue or green [[undecided]] |
| **Ring N** (the forward border) | The bright magic tear: light coming up out of the crack, with the floor cracks glowing |
| **Ring N − 1** (the border behind) | Smoky grey. Floor cracks fade off |
| **Older rings** | Off: just the crack in the rock |
| **Ring 4** (the field's edge) | Never glows. *Something in the background at Legend maybe, decided with the final art* [[undecided]] |

- **Raised zones are all one flat level**, so open and frozen zones read as one plane ("unlocked"), and it helps the lighting. Only locked zones sit lower
- **Zone 1 at the start** (Bound): already raised, bright tear on its forward border, no smoke (nothing behind it). No transition plays
- **Runes always sit at the same height**, so runes on a lowered (locked) zone float a little above it. Steve makes the art work for that
- **Evolution nodes (gates) get a gap** in the border: the wall slopes down to nothing around each gate, so the gates stand out (Steve's sketch: the wall ramps down over a couple of steps to a gap around the node)

---
## Zone Changes
**Saved "seen" zone:** each shade slot saves the last open zone the player actually saw. When the field loads, it compares that to the real open zone:
- **Same:** everything snaps straight to its look
- **Behind:** the transition plays, then the seen zone is saved
- **Ahead** (the test kit's Reset Slot; *Level Down never takes a shade's rank away, so it can't make this happen*): snaps, then saves

That covers both ways a shade evolves ([[Rune Field System]], **When it evolves**):
- **Evolving on save:** saving already reloads the field after the shade evolves (`OnShadeEvolved`), so the transition plays right after the save. That gives the "locked in" feel
- **Leveling into a planned layout** (mid-fight, no field open): the seen zone is behind, so the transition plays the next time that slot's field opens

**One transition step** (zone N opens):
1. Zone N rises to the raised level, and its floor goes from the locked tint to the open tint. Zone N − 1 goes from the open tint to the frozen tint
2. Ring N − 1 goes from the tear to smoke, and ring N − 2's smoke turns off
3. Ring N lights up: the light sweeps around the crack, and the floor cracks grow outward as the sweep reaches them. Ring 4 skips this

- **Catching up more than one zone** (rare: it needs pre-planned gates): play the steps one after another, the simplest version, then test the feel
- **Input is never blocked** while it plays
- **Camera move during the transition:** decided in the polish pass [[undecided]]
- **On save with cut bridges:** the bridge break effect plays first, then the zone opens
- **Overcharge (later):** before the cut on save, the forward border "erupts" (an overcharge pulse in the shader) and then the bridges break. This happens on any save that cuts bridges, even with no new zone (Steve's example: zone 2 open, runes planned into zone 3, save). The code side is cheap: save already knows which bridges it cuts (`OnBridgesCut`) [[Notes for the future]]

---
## Ring Wall Tool
An editor tool that builds each ring's wall of light from one strip Steve models in Maya.

- **The strip:** a short straight piece of wall (its length runs along X, height is up). The tool works out how many copies fit around the ring, stretches them a little so the circle closes exactly, and **bends** every vertex onto the circle (along the strip = the angle around the ring, depth = the radius, height stays height). Copies follow the curve smoothly and are welded into one mesh
- **Rules for the strip (Steve):** the two end edges need the same profile (same vertex heights and depths) so the copies weld cleanly, like a tiling texture. It needs enough edge loops along its length for the gate slopes and the noise to bend smoothly
- **Lean** *(asked for Oct 12, 2026)*: a setting that pushes the wall out (or in) more the higher up it goes, so the top sits at a bigger radius than the bottom. Set as an angle, with an optional curve so it can flare instead of being a straight slant. Noise and hand spots can change it too (a hand spot can lean one stretch more). A slant modeled into the strip in Maya also works, since the strip's depth becomes radius; the two add up
- **Unevenness, two layers:**
  - **Noise:** height variation, in / out wobble, how often, and a seed. Sampled around a circle so it loops with no seam where the ring closes
  - **Hand-placed spots:** a list of points dragged around the ring with scene handles, each with a width, a height boost or dip, and an in / out push, with a smooth falloff
- **Gate gaps:** the tool finds the gates on that ring and makes a gap mask around each one: off across the node plus a margin, then a ramp back up to full (gap width and ramp width are sliders). The mask lowers the wall's height to nothing, and it's baked into the mesh so the shader fades the glow there too. Smoke skips the gaps
- **Baked in the editor** and saved as a mesh asset, since every shade shares the same gate layout (decided Oct 12, 2026). If `_ZoneWidth` or a gate's spot changes, press rebuild
- **Steve's strip (Oct 12, 2026):** `M_ZoneWall_WallEffectCard`, a flat 4 m card with its pivot in the middle. The tool lays copies out from the mesh's bounds, so the pivot spot along the length doesn't matter; its Y = 0 counts as the raised floor level. Details in [[Zones - Art To Do]]
- **The wall stays put** when zones lift. Its bottom goes below the lowered floor so there's never a gap

---
## Crack Tool
Floor cracks are smaller versions of the ring wall, along a path instead of a circle: tall where they leave the ring, collapsing to nothing at the end. Steve models trenches in the floor for them and adds burn decals.

- **A crack is a path of points**, as many as wanted, so they can zig-zag. Each point is dragged with a scene handle; points can be added and removed
- **The start snaps** to a ring, or to a point on another crack (a branch). A crack belongs to the ring it starts from (a branch belongs to its parent's ring), so it follows that ring's tear / smoke / off state by itself
- **Branches are placed by hand** (Steve wants full control, the detail comes with the style). No automatic branching or scattering
- **Height:** a curve Steve edits, full at the start, falling to 0 at the end
- **The same noise controls** as the ring, so no two cracks look copied
- **The same strip and material** as the ring wall, or a narrower crack strip if Steve makes one
- **Crack starts inside a gate gap** are left up to Steve (no warning)
- **Growing on reveal:** each crack knows where it meets the ring, so when a ring lights up, the cracks light up as the sweep passes them and grow outward (branches start when the glow reaches the branch point)
- **Cracks ride their floor:** a ring's cracks sit on the zone outside it, so they're parented to that zone and lift with it
- All of a ring's cracks are combined into one mesh (cheap to draw), baked like the walls
- **Smoke state:** cracks fade out instead of smoking (the simpler option, Oct 12, 2026)
- **Workflow:** Steve models the floor with trenches, brings it into Unity, then places the cracks over them. Burn decals are HDRP Decal Projectors placed by hand, parented to the zone so they lift with it. *An automatic decal along each crack could come later* [[Notes for the future]]

---
## For the Shader
Steve builds the wall and crack shader in Shader Graph. Claude does the mesh and data side, and gives Steve the inputs plus a node-by-node starting point ([[Zones - Shader Graph Recipe]]). If a piece of math gets awkward in nodes, Claude writes it as a Custom Function node.

**One material for the walls and the cracks.** Each renderer gets its own values through `RuneFieldTint`'s `MaterialPropertyBlock` (like the bridges), so cracks can fade while their wall smokes.

**Set by code (per renderer):**

| Property | Range | What it is |
| :--- | :--- | :--- |
| `_Tear` | 0 to 1 | How much of the bright tear look |
| `_Smoke` | 0 to 1 | How much of the smoky look (always 0 on cracks) |
| `_Reveal` | 0 to 1 | The light-up sweep: 0 = dark, 1 = fully lit |
| `_Overcharge` | 0 to 1 | Later: the pulse before a cut on save |

**Baked into the mesh:**

| Channel | What it is |
| :--- | :--- |
| UV0 | Steve's UVs from the strip, with U carried on from copy to copy so a scrolling texture flows all the way around with no seam |
| UV2.x | **Reveal order** (0 to 1): when this spot lights up in the sweep. On a wall it's how far around from where the sweep starts (it spreads both ways from a start angle set on the tool, and meets on the far side); on a crack it carries on from where the crack leaves the ring |
| UV2.y | Height up the strip (0 = bottom, 1 = top), for fading toward the top |
| UV3.x | Gate gap mask (1 = normal, 0 = in a gap). Always 1 on cracks |
| UV3.y | Distance along a crack (0 at its start, 1 at its tip). 0 on walls |

Property names become inspector fields (like the bridges), so they can follow whatever Steve names them.

---
## Floor Tint: Light Layers
*Decided Oct 12, 2026.* The floors will be built from an asset kit (lots of small rocks and debris), so the open / frozen / locked tint is done with **lighting**, not shaders or materials. Nothing on the kit pieces needs a special material.

- **Each zone gets its own HDRP light layer** (Light Layers 1 to 4, named Zone 1 to Zone 4). Layer 0 (default) stays for everything else: runes, nodes, the core, the walls of light
- **The code puts every renderer under a zone on that zone's layer**, on load and with an editor button, so kit pieces are never set by hand. Anything that should skip the tint (a glowing crystal, say) gets a small marker component
- **One shared key light** (with shadows) lights everything a little dimmer than the final look. Each zone also gets a **shadowless zone light** that only lights its own layer and brings it up to its state's look: neutral for open, blue for frozen, little or muted purple for locked. Changing state = blending that light's color and intensity
- Shadowless zone lights are cheap. Only the key light casts shadows, which keeps the cost down (shadows are the expensive part, more so on the Steam Deck)
- **Catch:** ambient / sky light isn't split by layers, so the tint shows most on the parts the zone light hits. Keep ambient modest and let the zone lights do the work
- **Already on:** all three quality levels' HDRP assets (High Fidelity, Balanced, Performant) have Light Layers supported (checked Oct 12, 2026). The rune field camera's Frame Settings need Light Layers on (checked during setup)
- **Steam Deck:** light layers work there. Their cost is a bit of extra data per pixel plus four shadowless lights. To keep gameplay free of it, Light Layers can be turned on only for the rune field's camera (custom Frame Settings). Profile on the Deck once the kit is in [[Notes for the future]]
- **If the tint isn't strong enough:** per-zone material copies (tint Base Color) or a screen effect tinting by distance from the core (talked through Oct 12, 2026) [[undecided]]
- How Steve builds the final lighting: [[Zones - Lighting Guide]]

---
## Scripts
New scripts go in `Scripts/Rune Field/3D`. *All built Oct 12, 2026 under these names, plus `ZoneStripBuilder` (shared strip code) and `Editor/ZoneToolsEditor.cs` (scene handles, auto rebuild). The zone state enum is `RuneFieldType.ZoneState`.*

| Script | What it does |
| :--- | :--- |
| `RuneFieldZonesSO` | Settings asset (`SO_RuneField_Zones`): the zone light color and intensity for open / frozen / locked, lift height, transition timings. Tuned in play mode, changes stick (like the camera asset) |
| `RuneFieldZonesObject` | On the field object. Reads the open zone and the slot's seen zone on load, sets every zone and border, plays the transition steps, saves the seen zone |
| `Zone3DObject` | One zone: lifts or lowers everything under it, puts its renderers on the zone's light layer, and drives its zone light |
| `ZoneTintSkipObject` | Marker: renderers under it stay on the default layer (no zone tint) |
| `ZoneBorder3DObject` | One ring's border: the wall, its cracks and smoke. Blends between tear / smoke / off, and runs the reveal. Holds the ring wall tool (build button, noise, hand spots, gap settings) |
| `ZoneCrackPath` | One crack: its points, start snapping, height curve, noise. The border's build button combines all of a ring's cracks into one mesh |

---
## Build Order
Each step gets tested in Unity before the next. Steve's art can happen alongside; stand-ins are used until it's in.

1. **Zone states and the seen zone**: *built and working in Unity, Oct 12, 2026*. Written up in **Zones** in [[Rune Field 3D View]]
	- `RuneFieldZonesSO`, `RuneFieldZonesObject`, `Zone3DObject`, `ZoneTintSkipObject`
	- Stand-in floors (a disc and rings with a skirt, made by code) until Steve's meshes are in (**Set Up Zones**, **Rebuild Stand-In Floors**)
	- Light layers: **Name Light Layers** button, **Set Up Zone Lighting** (zone lights are copies of the key light, shadows / sky / fog off), **Apply Zone Layers** (editor) and the same on load, the skip marker. Renderers go on the default layer *plus* their zone's, so the key light keeps lighting and shadowing them
	- A **Preview Open Zone** slider (not buttons) in edit mode
	- The seen zone lives on the slot asset (`ShadeSO._SeenOpenZone`, 0 = never seen), through the Shade Manager
	- Snapping on load (same, behind, ahead). Behind snaps too until step 5
	- Testing: Level Up into a powered gate (evolves, reloads), Reset Slot (ahead), and **Replay From Seen Zone** (behind). Live tuning of `SO_RuneField_Zones` while playing
2. **Ring wall tool**: *built and working in Unity, Oct 12, 2026*
	- Bending and welding the strip, closing the circle exactly; looping noise; hand spots with scene handles; lean (angle + curve, noise and spots add to it); gate gaps found from the field's node list; the baked channels; mesh assets in `_MeshFolder`; auto rebuild on strip reimport; a stand-in card strip
	- **Decided while building:** the strip settings (strip, crack strip, scale, flips, material, mesh folder) are on `SO_RuneField_Zones`, shared by every ring; the per-ring shape settings are on each border. The strip's +Z faces the core. **`_BottomDepth`** stretches the strip's part under its Y = 0 down to 0.4 m, so Steve's card works as it is (the bottom-gap question in [[Zones - Art To Do]]). The field's edge ring gets no border at all
	- Test: build all three rings, move a gate, rebuild
3. **Crack tool**: *built and working in Unity, Oct 12, 2026*
	- `ZoneCrackPath`: points with scene handles, Add Point / Remove Last Point, start snapping to a ring or another crack (Branch At), height curve and noise, all of a ring's cracks in one mesh with the reveal order carried on from the ring and along branches
	- Crack objects and the ring's crack renderer go under the zone outside the ring. The border's **Add Crack** makes one there
4. **Border states**: *built and working in Unity, Oct 12, 2026*
	- `ZoneBorder3DObject.ShowLook` sends tear / smoke / reveal / overcharge through `RuneFieldTint`; property names on the zones asset; cracks get the tear only, so they fade as the wall turns to smoke
	- **Stand-in look (decided while building):** until the shader exists, the material's color is tinted off → smoke → tear (`_UseStandInLook`), and off walls are hidden (`_HideWhenOff`)
	- Stand-in smoke: puffs emitted along the ring by code (fewer in gate gaps); Steve's prefab replaces it (`_SmokePrefab`)
5. **Transitions**: *built and working in Unity, Oct 12, 2026*
	- One step per new zone on real time (rise and lights; tear → smoke, smoke → off; reveal after a delay), timings and curves on the zones asset; catch-up plays steps one after another; no input blocking
	- The seen zone is saved when the opening finishes (dropped and replayed if another slot loads first). A save that evolves reloads twice; the second is ignored while the same opening plays
	- The bridge break before it is still to come (the effect doesn't exist yet) [[Notes for the future]]

**Steve (art, alongside):** zone floor meshes with trenches, the wall strip (and a crack strip if wanted), the Shader Graph, the floor material, burn decals, the smoke look. See [[Zones - Art To Do]].

---
## Open Questions
- Final colors: the frozen blue, the locked tint, the tear and smoke colors (zone light colors on `SO_RuneField_Zones`) [[undecided]]
- Camera move during the transition [[undecided]]
- Overcharge pulse before a cut on save [[Notes for the future]]
- Background at Legend (ring 4 open) [[undecided]]
- The bridge break effect itself (Art Pile in [[Rune Field 3D Plan]]) [[Notes for the future]]
- Lift height: starts at 0.25 m on the settings asset, tuned by feel [[undecided]]
