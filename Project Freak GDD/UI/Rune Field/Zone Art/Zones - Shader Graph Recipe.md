A starting point for `SHA_RuneField_Zone_Border_0`, the shader on the ring walls and floor cracks. It gets the basic look working; everything after that is your call artistically. If any step gets awkward in nodes, Claude can write it as one Custom Function node.

The full plan is [[Zone Visuals Plan]]. Back to [[Zones - Art To Do]].

---
## Graph settings
- **Create → Shader Graph → HDRP → Unlit Shader Graph** (the glow doesn't need lighting)
- Graph Inspector: **Surface Type** Transparent, **Blending Mode** Additive, **Double-Sided** on
- If the smoke looks wrong as added light (smoke usually darkens), try a second material on Alpha blending for the smoky state later. Start with one

---
## Properties (Blackboard)
The **Reference** name (not the display name) is what the code sets, so match these exactly. Leave them **Exposed**. If you'd rather use other names, keep yours and tell Claude: the names are inspector fields.

**Set by the code, per wall / crack** (leave them at these defaults so the material looks lit in the editor):

| Reference | Type | Default | What it is |
| :--- | :--- | :--- | :--- |
| `_Tear` | Float, slider 0 to 1 | 1 | How much of the bright tear look |
| `_Smoke` | Float, slider 0 to 1 | 0 | How much of the smoky look (cracks always get 0, they fade instead) |
| `_Reveal` | Float, slider 0 to 1 | 1 | The light-up sweep. 0 = dark, 1 = fully lit |
| `_Overcharge` | Float, slider 0 to 1 | 0 | *Later:* the pulse right before bridges get cut. Add it whenever |

**Yours to tune on the material:**

| Reference | Type | Suggested start |
| :--- | :--- | :--- |
| `_TearColor` | Color, HDR | Bright purple, intensity 3 to 5 |
| `_SmokeColor` | Color, HDR | Grey, intensity around 0.5 |
| `_ScrollSpeed` | Float | 0.5 |
| `_NoiseScale` | Float | 10 |
| `_RevealSoftness` | Float | 0.05 |

---
## What the mesh gives you
Read these with a **UV** node, changing its **Channel**, then a **Split** node (R = x, G = y).

| UV node channel | R (x) | G (y) |
| :--- | :--- | :--- |
| UV0 | Your strip UVs along the wall (carries on from copy to copy, no seam) | Your strip UVs up the wall |
| UV2 | **Reveal order**, 0 to 1: when this spot lights up in the sweep | **Height** up the strip, 0 bottom to 1 top |
| UV3 | **Gate gap mask**: 1 normal, 0 in a gap at an evolution node | **Distance along a crack**, 0 at its start, 1 at its tip (0 on walls) |

---
## Node by node
**1. Moving noise (the "energy")**
- UV (UV0) → **Tiling And Offset**. Offset = (0, Time × `_ScrollSpeed`), so it flows upward. (**Time** node, Multiply by `_ScrollSpeed`, into the Y of a Vector2)
- → **Simple Noise** (Scale = `_NoiseScale`) → call this **Noise**

**2. Fade toward the top**
- UV (UV2) → Split → **G** (height) → **One Minus** → **Power** (exponent around 2) → call this **Height Fade**

**3. Gate gaps**
- UV (UV3) → Split → **R** → call this **Gap Mask**

**4. The reveal sweep**
- UV (UV2) → Split → **R** → call this **Order**
- `_Reveal` × (1 + `_RevealSoftness`) → Subtract **Order** → Divide by `_RevealSoftness` → **Saturate** → call this **Reveal Mask**
- *Why the extra math:* without it the very last spot in the sweep never reaches full brightness
- *Nice extra later:* a hot bright line right at the sweep's front edge

**5. The two looks**
- **Tear look** = `_TearColor` × Noise × Height Fade × `_Tear`
- **Smoke look** = `_SmokeColor` × (a second, slower and bigger Simple Noise) × Height Fade × `_Smoke`
- **Add** them together

**6. Crack tips (optional)**
- UV (UV3) → Split → **G** → One Minus → Multiply into the result, so cracks also fade in brightness toward the tip (the mesh already shrinks to nothing there)

**7. Output**
- Result × Reveal Mask × Gap Mask → **Emission**
- **Base Color** black
- **Alpha** = Reveal Mask × Gap Mask × (Height Fade, or 1)

---
## Checking it
- Drag the material onto a stand-in wall in the scene and slide `_Tear`, `_Smoke` and `_Reveal` on the material to see each look
- While the game runs, the code overrides those three per wall, so material slider changes to them won't show in play mode. The colors and tuning values still will
