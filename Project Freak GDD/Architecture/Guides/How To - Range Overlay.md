## Overview
A guide to setting up and styling the rune field's **range overlay**: the glowing splotch on the floor that shows where a rune being placed would make bridges, and the light-up on runes and the core as it gets close. The code is done; this page covers the parts done in Unity: the decal Shader Graph, putting the decal in the scene, keeping it off the runes, and the rune / core effect.

**This page is a draft.** Written Oct 24, 2026, before the graph was built in Unity. Fix anything that turns out different once it's been run through [[Notes for the future]]

The full reference for the scripts (what every function and setting does) is **Range Overlay** in [[Rune Field 3D View]].

Back to [[Architecture Atlas]]

**Contents**
- [[#Before You Start]]
- [[#How It Works]]
- [[#Part 1 - Build the Decal Shader Graph]]
- [[#Part 2 - Put It in the Scene]]
- [[#Part 3 - Keep It Off the Runes]]
- [[#Part 4 - Rune and Core Light-Up]]
- [[#Part 5 - Tune It]]
- [[#Testing Checklist]]
- [[#Troubleshooting]]
- [[#Not Built Yet]]

---
## Before You Start

### Words you'll see
| Word | What it means here |
| :--- | :--- |
| Reach | How far a rune (or the core) reaches to make a bridge, in field units. A rune's comes from its element (`connectionDistance`), the core's from `SO_RuneField_Settings` |
| Bridge length | How long a bridge between two things can be: the **bigger** of their two reaches. This is the range drawn on the floor |
| In range | The rune being placed would bridge to this rune if dropped here |
| Approach zone | Between 100% and 150% of bridge length. Runes fade in here, so you can see what you're getting close to |
| Highlight | A 0 to 1 number per rune and the core: 0 = not lit, 1 = in range |
| Decal | An HDRP Decal Projector: a box that paints a material onto whatever surfaces are inside it. Used so the overlay follows the raised and lowered zone floors |

### What you need
- `RuneRangeOverlayObject.cs` and `Shaders/RuneRangeField.hlsl` in `Scripts/Rune Field/3D` (already there)
- The `3d_Evolution_UI_scene` open, with the `Rune Field` object

---
## How It Works
```text
you drag / carry a rune ──► RuneField3DObject.TryGetPlacementPreview (where it is, its reach)
                                    │
                                    ▼
                         RuneRangeOverlayObject (every frame)
                           ├──► each rune / the core: ShowRangeHighlight(0 to 1)  ──► their shader + RangeEffect child
                           └──► global shader values (positions, ranges, strengths)
                                    │
                                    ▼
                         RuneRangeField.hlsl in your Decal Shader Graph ──► the decal on the floor
```
The C# does all the math on what's in range. The graph only gets masks and gradients back (`RangeMask`, `RangeGradient`, `HaloMask`, `HaloGradient`) and decides what they look like, so styling never needs code.

---
## Part 1 - Build the Decal Shader Graph

### 1. Make the graph
1. In the Project window: right click → **Create → Shader Graph → HDRP → Decal Shader Graph**. Name it `SHA_RuneField_RangeOverlay_0`
2. Open it. In the **Graph Inspector → Graph Settings**, turn on **Affect BaseColor** and **Affect Emission**. Turn the others off (Normal, Metal, AO, Smoothness) so the floor keeps its own surface

### 2. Find the spot on the field
3. Add a **Position** node and set its Space to **Absolute World**
   - *Not plain World.* HDRP draws relative to the camera, so World would make the ranges slide around as the camera moves
4. Add a **Custom Function** node. In its Node Settings:
   - Type: **File**
   - Source: `RuneRangeField.hlsl`
   - Name: `RuneFieldPosition` (no `_float` at the end, Unity adds it)
   - Inputs: `WorldPos` (Vector 3)
   - Outputs: `FieldPosition` (Vector 2)
5. Plug the Position node into `WorldPos`. `FieldPosition` is now the spot on the field's floor in meters, with the core at 0, 0

### 3. Get the masks and gradients
6. Add a second **Custom Function** node:
   - Type: **File**, Source: `RuneRangeField.hlsl`
   - Name: `RuneRangeField`
   - Inputs, in this order: `FieldPosition` (Vector 2), `EdgeNoise` (Float)
   - Outputs, in this order: `RangeMask` (Float), `RangeGradient` (Float), `HaloMask` (Float), `HaloGradient` (Float)
7. Plug the first node's `FieldPosition` into this one's `FieldPosition`

There's no softness: every edge is crisp, and the look (feathering, rims, glow) is built in the graph from these:

| Output | What it is |
| :--- | :--- |
| `RangeMask` | 1 inside a range (dropping here would bridge), 0 outside. Crisp edge |
| `RangeGradient` | 1 at the rune, fading to 0 at the range's edge |
| `HaloMask` | 1 inside the approach circle (150%), 0 outside. Covers the range too: `HaloMask − RangeMask` is just the ring |
| `HaloGradient` | 1 at the rune, fading to 0 at the approach circle's edge |

- Overlapping ranges merge (the strongest wins), so there are no bright overlap blobs
- Every output is multiplied by the range's fade-in (0 to 1 as the placing rune gets close), so nothing pops in

### 4. Wobbly edges
8. Add these properties (Blackboard): `_NoiseScale` (Float, 2), `_NoiseSpeed` (Float, 0.2), `_NoiseStrength` (Float, 0.08)
9. `FieldPosition` × `_NoiseScale` → **Add** (**Time** × `_NoiseSpeed`) → **Simple Noise** UV (or **Sample Texture 2D** with your own noise texture)
10. Noise → **Remap** (In Min Max 0, 1 → Out Min Max −`_NoiseStrength`, `_NoiseStrength`) → `EdgeNoise`
    - In **meters**: the noise pushes every edge in and out by up to that much

*Using `FieldPosition` as the noise UV keeps the noise stuck to the floor instead of the screen.*

### 5. Color and glow (a starting point, restyle freely)
11. Add properties: `_FillColor`, `_HaloColor` (Color, HDR on), `_FillAlpha` (Float, 0.35), `_HaloAlpha` (Float, 0.15), `_GlowStrength` (Float, 1)
12. Fill = `RangeMask`. Ring = `HaloMask` − `RangeMask`
13. **Color** = `_FillColor` × Fill **+** `_HaloColor` × Ring
14. **Alpha** = Fill × `_FillAlpha` **+** Ring × `_HaloAlpha`
    - Crisp edge with a feathered inside: use `RangeMask` × (1 − `RangeGradient`) as the fill instead. It's brightest right at the crisp edge and fades toward the rune (**Power** on the gradient changes how fast). `RangeGradient` on its own does the opposite: a glow around the rune
15. Into the master stack:
    - **Base Color** ← Color
    - **Alpha** ← Alpha
    - **Emission** ← Color × Alpha × `_GlowStrength` (decal emission isn't faded by alpha on its own, so it's multiplied in)
16. Save. Right click the graph → **Create → Material**, name it `MAT_RuneField_RangeOverlay_0`

*Oct 27, 2026:* the node used to have an `EdgeSoftness` input and `InRange` / `Approach` / `Edge` outputs. A node made before that needs its inputs and outputs set to the lists in step 6.

---
## Part 2 - Put It in the Scene
1. Select the `Rune Field` object and add **`RuneRangeOverlayObject`**. It finds the field by itself
2. Open the **Decal** foldout and drag `MAT_RuneField_RangeOverlay_0` into **Decal Material**
3. Press **Create Range Decal**. It makes `Range Overlay Decal` under the field: centered, pointing down, sized to the outer ring plus edge bleed. Ctrl+Z undoes it
4. Check **Decal Depth** (3 m) reaches every zone's floor height, raised and lowered. If a zone's floor doesn't get the overlay, raise it and press Create Range Decal again (it reuses the same decal)

---
## Part 3 - Keep It Off the Runes
The decal paints everything inside its box, so runes and the core sitting on the floor get the blue too. Decal layers fix that: the decal only paints renderers on its layer.

1. **HDRP asset** (the one set in Project Settings → Graphics / Quality): Inspector → **Rendering → Decals** → tick **Layers**. This only turns the feature on
2. **Frame Settings:** Project Settings → Graphics → **HDRP Global Settings** → Frame Settings (Default Values) → **Camera → Rendering** → tick **Decal Layers**. If the rune field camera uses Custom Frame Settings, tick it there too (camera → Rendering → Frame Settings Overrides → Rendering)
   - Optional: in HDRP Global Settings, rename a layer under **Decal Layer Names**, e.g. Decal Layer 1 → `Range Overlay`. Decal layers are separate from the zones' light layers, so they don't clash
3. **Floors are on Decal Layer Default already** (every renderer starts on it). The runes, core and nodes are too, which is why they get painted. Either:
   - **Easiest:** on `PFB_Rune_3D`, the core prefab and `PFB_Node_3D`, open **Rendering Layer Mask** (Mesh Renderer → Additional Settings) and untick **Decal Layer Default**, leaving their light layers alone. Keep the decal on Default
   - **Or:** set the decal projector's **Decal Layer** to `Range Overlay` only, and add `Range Overlay` to every floor renderer's Rendering Layer Mask instead
4. Ghost runes from the inventory use `PFB_Rune_3D`, so they're covered by the prefab change
5. On the decal, use **Angle Fade** so it doesn't smear down the steep zone walls [[Notes for the future]]

---
## Part 4 - Rune and Core Light-Up
Every rune and the core get a highlight number (0 to 1) while something is placed near them. Two ways to show it, use either or both:

- **A `RangeEffect` child** (the big magic effect): open `PFB_Rune_3D` (and the core prefab), add a child named exactly `RangeEffect` with the effect on it. It's turned on while the highlight is above 0, and hidden otherwise. It's also left out of the rune's element tint, so it keeps its own colors. Or drag any object into **Range Effect** on `Rune3DObject` / `Core3DObject`
- **`_RangeHighlight` in a shader**: add a Float property with the reference `_RangeHighlight` to the rune's shader, the core's shader or the effect's shader, and use it for glow, size, dissolve, whatever. It fades smoothly, so it can drive a grow-in. The name can be changed with **Range Highlight Property** on the rune / core

Nothing to code for either [[Notes for the future]]

---
## Part 5 - Tune It
On `RuneRangeOverlayObject`:

| Setting | What it does |
| :--- | :--- |
| Approach Ratio | How far out runes start lighting up, as a multiple of bridge length (1.5 = 150%). Also how far the floor halo reaches |
| Approach Curve | How the light-up builds through the approach zone. X: 0 = just entered, 1 = in range. Y: strength |
| Fade Speed | Fades per second (6 = about a sixth of a second) |
| Full Rune Strength | How strong an in-range rune shows when all its bridge slots are used (it won't connect). 0.35 |
| Decal Depth | Height of the decal box, in meters |
| Test Show Every Range | Shows every range all the time in play mode. Turn it on while styling the graph |

The look itself (colors, alpha, noise, softness, glow) is all on the material.

---
## Testing Checklist
- [ ] Play, tick **Test Show Every Range**: every rune and the core show their range on the floor
- [ ] Overlapping ranges merge into one shape with one outline, no bright overlaps
- [ ] Move the camera: the ranges stay put (if they slide, the Position node isn't Absolute World)
- [ ] Untick it and drag a rune toward another: it fades in from about 150%, and is fully lit right when dropping would bridge
- [ ] Drop it at full light: the bridge is made. Just outside: no bridge
- [ ] Same from the inventory list (mouse drag) and the gamepad carry
- [ ] Dragging a rune over the inventory list: everything fades out
- [ ] A rune with every bridge slot used shows dimmer
- [ ] Runes and the core aren't painted by the decal (Part 3)
- [ ] Every zone's floor gets the overlay, raised and lowered

---
## Troubleshooting
| Problem | Check |
| :--- | :--- |
| Shader errors about `RuneRangeField_float` | The Custom Function's Name has no `_float`, and its inputs / outputs match step 6 in Part 1 in order and type |
| Nothing on the floor | Material on the decal? Decals on in Frame Settings? Decal box tall enough? `Shown Ranges` on the component above 0 while dragging? |
| Ranges slide with the camera | Position node isn't set to Absolute World |
| Ranges in the wrong spot or the wrong size | The decal reads the field object's position and directions, so check the `Rune Field` object's rotation is flat |
| Runes are painted blue | Decal layers (Part 3) |
| A warning about more than 32 ranges | More than 32 lit at once. Raise `RUNE_RANGE_MAX` in the hlsl **and** `MaxShownRanges` in `RuneRangeOverlayObject` together |
| RangeEffect never shows | The child must be named exactly `RangeEffect`, or assigned in Range Effect |

---
## Not Built Yet
- The placement pole with the ball, and its ground ring that goes red / green (Steve's model) [[Notes for the future]]
- The warped bridge preview showing which bridges a drop would make, straightening out once in range [[Notes for the future]]
