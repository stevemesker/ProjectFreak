What Steve makes for the rune field zone visuals. The full plan is [[Zone Visuals Plan]]. Also see [[Zones - Shader Graph Recipe]] and [[Zones - Lighting Guide]].

![[Zones - Reference.png]]

Names follow the GDD's File Naming Conventions (`Type_Category_Subcategory_Object_count`). Change them freely, the code only needs them dragged in.

---
## Sizes (from SO_RuneField_Settings, 3D branch)
1 m = 100 field units. Zone width is 400 field units = **4 m**.

| Piece | Inner radius | Outer radius |
| :--- | :--- | :--- |
| Zone 1 (disc, holds the core) | 0 | 4 m |
| Zone 2 | 4 m | 8 m |
| Zone 3 | 8 m | 12 m |
| Zone 4 | 12 m | 16 m (the field's edge) |
| Edge bleed (camera room, no runes) | 16 m | 17 m |

- Core: 2 m across. Runes and nodes: 1 m across
- Gates sit on rings 1 to 3 (4, 8 and 12 m)
- If `_ZoneWidth` changes, the floors need remaking and the walls and cracks need a rebuild (button)

---
## 1. Zone floors (Maya)
- [ ] `M_RuneField_Zone_Floor_0` to `_3`: zone 1 is a disc, zones 2 to 4 are rings, sizes above
- Pivot at the field's center (0, 0, 0) for all four, so they line up by just sitting at the origin
- **Top surface at height 0.** That's the raised (unlocked) level. The code lowers locked zones by the lift height, so runes, which always sit at the same height, line up with raised floors
- **Outer edge skirt:** each floor needs a rock face going down from its outer edge, since that face shows when the next zone out is lowered. Lift height starts at **0.25 m** (tunable), so give the skirt about **0.5 m** for room. Zone 4's outer edge is the field's edge, so it can go deeper
- Trenches in the floor where the glowing cracks will run (cracks get placed over them in Unity afterwards)
- **Floor materials:** anything you like (HDRP Lit or your own). The open / frozen / locked tint is done with **light layers**, not the material, so kit pieces need nothing special. Just keep each zone's pieces under that zone's object. See [[Zones - Lighting Guide]]

## 2. Wall strip (Maya)
- [x] `M_ZoneWall_WallEffectCard` *(made Oct 12, 2026, in `Art/UI/Rune Field/3D Assets`)*: a flat card, 4 m long, top edge 0.78 to 1.15 m, bottom at −0.24 m, 17 vertical edges 25 cm apart, ends match exactly, UVs 0 to 1. Pivot in the middle (fine, the tool works from the mesh's bounds). **Its Y = 0 is treated as the raised floor level.** With a 0.25 m lift, the bottom (−0.24 m) sits about 1 cm above the lowered floor, so either pull the bottom down to about −0.4 m or the tool gets a height offset
- Original brief: one short straight piece of the wall of light. The tool bends copies of it around each ring
- **Length runs along X**, height is up (Y), depth (the wall's thickness) along Z. The tool has flip options if it comes in backwards
- **Length:** about 1.5 to 3 m. The tool fits a whole number of copies around each ring and stretches them slightly (ring 1 is about 25 m around, ring 3 about 75 m)
- **Height:** yours. The tool keeps it, then the noise, hand spots and gate gaps change it
- **The bottom** should go below the lowered floor (more than the lift height under 0) so there's never a gap where it meets the floor
- **Both end edges must match** (same vertex count, heights and depths on the left and right ends), so the copies weld into one clean mesh. Like a tiling texture
- **Plenty of edge loops along the length.** The wall bends only where there are loops: the slope down at each gate and the noise need them (see the lines in the gate gap sketch)
- **UVs:** U from 0 to 1 along the length, V from 0 (bottom) to 1 (top). The tool carries U on from copy to copy so scrolling textures flow all the way around
- [ ] *Optional:* `M_RuneField_Zone_CrackStrip_0`, a narrower / taller strip just for the floor cracks. Same rules. Without it the cracks use the wall strip

## 3. Wall and crack shader (Shader Graph)
- [ ] `SHA_RuneField_Zone_Border_0` and its material `MAT_RuneField_Zone_Border_0`. One material for both walls and cracks
- Inputs and a node-by-node starting point are in [[Zones - Shader Graph Recipe]]
- Looks needed: the **bright tear** (forward border), **smoky grey** (border behind), **the reveal** (light sweeping around and out along the cracks), the fade at gate gaps. Later: the **overcharge** pulse before bridges get cut

## 4. Effects and decals
- [ ] **Smoke particles** for the smoky border. Claude makes a stand-in particle system that spawns along the ring and skips the gate gaps; the real look is yours
- [ ] **Burn decals** along the cracks: HDRP Decal Projectors, placed by hand, parented to the zone floor they sit on so they lift with it. *Decals need to be on in the HDRP settings (HDRP asset and the camera's Frame Settings) if they aren't already*

## 5. Lighting and colors (pending)
- [ ] The scene's base lighting (key light, sky / ambient) with every zone in its open look
- [ ] Zone light colors: open (normal), frozen (a bit blue), locked (muted, slightly darker; purple for now, maybe blue or green)
- [ ] Tear color and smoke color (on the border material)

Zone light colors live on the settings asset `SO_RuneField_Zones` once it exists, and changes made in play mode stick. The step-by-step is in [[Zones - Lighting Guide]].

---
## Order
The code gets built with stand-ins (plain floors, a plain strip), so nothing waits on the art. When a piece is ready, drag it in and press rebuild.
