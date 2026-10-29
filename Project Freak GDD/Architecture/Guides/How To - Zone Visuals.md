## Overview
A guide to changing the rune field's **zone visuals**: the raised and lowered floors, their open / frozen / locked tint, the walls of light on the rings, the glowing floor cracks, the smoke, and the zone opening that plays when a shade evolves. It covers every tool and setting, and where to look in the code when you want to change how the system itself works.

**This page is a draft.** Everything here works in Unity (Oct 12, 2026). We'll run through it together once the architecture is finished and fix anything that turns out different [[Notes for the future]]. The art (floors, strip, shader, smoke) is still stand-ins.

The design and the decisions behind it are in [[Zone Visuals Plan]]. The full reference for every script is **Zones** in [[Rune Field 3D View]].

Back to [[Architecture Atlas]]

**Contents**
- [[#Before You Start]]
- [[#How It Decides What to Show]]
- [[#Part 1 - Tune the Look]]
- [[#Part 2 - Floors and Kit Pieces]]
- [[#Part 3 - Ring Walls]]
- [[#Part 4 - Cracks]]
- [[#Part 5 - Plug In the Wall Materials]]
- [[#Part 6 - Smoke Particles]]
- [[#Part 7 - The Zone Opening]]
- [[#Part 8 - Changing the System Itself]]
- [[#Testing Checklist]]
- [[#Troubleshooting]]
- [[#Not Built Yet]]

---
## Before You Start

### Words you'll see
| Word | What it means here |
| :--- | :--- |
| **Zone** | One piece of floor. Zone 1 is the disc around the core, zones 2 to 4 are rings outward |
| **Ring** | The circle where one zone ends and the next starts. Ring k is zone k's outer edge. Evolution gates sit on rings 1 to 3; ring 4 is the field's edge |
| **Open zone (N)** | The zone the shade can build in right now (rank + 1). Everything else follows from this one number |
| **Frozen / Open / Locked** | Zones below N / zone N / zones past N |
| **Border** | One ring's wall of light and its cracks. Looks: **tear** (the tear material, ring N, cracks lit), **smoke** (the smoke material, ring N − 1, cracks off), **off** (older rings) |
| **Strip** | The short straight piece of wall Steve models in Maya. The tool bends copies of it around each ring and along each crack |
| **Rebuild** | Making a border's wall and crack meshes again from the strip and the settings. They're saved as mesh assets (baked), not made while playing |
| **Seen zone** | The last open zone the player saw on a shade slot. If the real one is further out, the zone opening plays |
| **Light layer** | An HDRP setting saying which lights reach which objects. Each zone has its own, so its zone light tints only that zone |
| **Stand-in** | A placeholder made by code (floors, the strip card, the wall color, the smoke) until the real art is in |

### Where things live
| What | Where |
| :--- | :--- |
| Scripts | `Assets/Scripts/Rune Field/3D/` (`RuneFieldZonesSO`, `RuneFieldZonesObject`, `Zone3DObject`, `ZoneTintSkipObject`, `ZoneBorder3DObject`, `ZoneCrackPath`, `ZoneStripBuilder`) and its `Editor/` folder (`ZoneToolsEditor`) |
| The settings asset | `Assets/Scriptable Objects/Rune Field/SO_RuneField_Zones` |
| Zone count and width | `SO_RuneField_Settings` (shared with the rules, so change it carefully) |
| Baked wall and crack meshes | `Assets/Generated/Rune Field Zones/` (set by `_MeshFolder` on the settings asset) |
| The strip | `Assets/Art/UI/Rune Field/3D Assets/M_ZoneWall_WallEffectCard` |
| The scene | `3d_Evolution_UI_scene` |

### The hierarchy
```text
Rune Field  (RuneField3DObject + RuneFieldZonesObject)
 ├─ Zone Floors
 │   ├─ Zone 1  (Zone3DObject)  ← floor mesh, kit rocks, decals go in here and lift with it
 │   ├─ Zone 2  (Zone3DObject)
 │   │   ├─ Stand-In Floor         (delete once the real floor is in)
 │   │   ├─ Ring 1 Cracks          (the baked crack mesh for ring 1, made by Rebuild)
 │   │   └─ Ring 1 Crack ...       (ZoneCrackPath objects for ring 1)
 │   └─ ...
 ├─ Zone Borders
 │   ├─ Ring 1 Border  (ZoneBorder3DObject)
 │   │   └─ Wall                   (the baked wall mesh, stays put when zones lift)
 │   └─ ...
 └─ Zone Lights  (one shadowless light per zone)
```
Cracks for ring k sit on **zone k + 1** (the floor outside the ring), so they lift with it.

### Where the buttons are
*Reorganized Oct 28, 2026 ([[Rune Field Clean-Up Plan]]).* Select the `Rune Field` object and look at the zones component:
- **Rebuild** (open): **Rebuild All Borders** and **Match Zone Lights To Key Light**, the ones pressed after a tweak
- **First-Time Setup** (closed): **1. Name Light Layers**, **2. Set Up Zones**, **3. Set Up Borders**, **4. Set Up Zone Lighting**, plus Fill Zone List, Fill Border List and Apply Zone Layers
- **Stand-In Floors** (closed): the stand-in floor settings and **Rebuild Stand-In Floors**

The zones asset (`SO_RuneField_Zones`) has its own **Rebuild All Borders** next to the wall settings, so changing a wall setting and rebuilding happens in one place. A single ring: **Rebuild Ring** on that border (Ring Tools). Hover any button for what it does. Everything is safe to press again.

---
## How It Decides What to Show
Every time a shade slot's field loads (opening the field, switching slots, saving, evolving), the zones component:
1. Reads the open zone N from the rules
2. Reads the slot's seen zone
3. **Same, or never seen:** snaps everything to N's look. **Behind:** plays the zone opening from the seen zone to N. **Ahead** (the slot was reset): snaps
4. Saves N as the seen zone (after the opening finishes, if one played)

| | Floor height | Zone light | Its border (the ring outside it) |
| :--- | :--- | :--- | :--- |
| Zones below N | Raised | Frozen | Smoke (ring N − 1) or off |
| Zone N | Raised | Open | **Tear** |
| Zones past N | Lowered | Locked | Off |

To see any state without pressing play: move the **Preview Open Zone** slider (Settings, on the zones component).

---
## Part 1 - Tune the Look
Everything you'd tune lives on **`SO_RuneField_Zones`**. Changes made while playing **stay after you stop**, and with **Live Tuning** on (zones component) they show up straight away.

1. Press **Play**, then select `SO_RuneField_Zones`
2. **Floors:** change `_OpenLight`, `_FrozenLight` and `_LockedLight` (color and intensity), and `_LiftHeight`
3. **Walls:** tune the tear and smoke materials themselves (their values work live while playing, except the height, which the code animates). The stand-in colors only matter while a material is empty
4. **Zone opening timings:** see Part 7
5. To flip between states while playing, use the test kit's **Level Up** (into a powered gate), or the zones component's **Replay From Seen Zone**

The lighting side (key light, ambient, how much brightness to give the zone lights) is in [[Zones - Lighting Guide]].

---
## Part 2 - Floors and Kit Pieces
### Swap in a real floor
1. Drag the floor mesh into the scene under `Zone Floors/Zone N`
2. Line it up with the zone object (pivot at the field's center, top surface at the zone object's height)
3. Delete that zone's `Stand-In Floor`
4. Press **Apply Zone Layers** (or just press play, it runs on start)

### Kit pieces (rocks, debris)
Drop them anywhere under the right zone (sub-groups are fine). Never set their light layers by hand: **Apply Zone Layers** or play does it.

### Something that shouldn't be tinted
Add **`ZoneTintSkipObject`** to it (or to a parent group). Everything under it stays on the default light layer. Use it for glowing crystals, magic bits, anything emissive.

### Stand-in floor settings (zones component, Stand-In Floors foldout)
`_StandInSkirtDepth` (rock face depth), `_StandInSegments` (roundness), `_StandInMaterial`. Press **Rebuild Stand-In Floors** (same foldout) after changing them.

---
## Part 3 - Ring Walls
### Use your strip
1. Select `SO_RuneField_Zones`, open **Wall Strip Import** and drag the strip's **mesh** (expand the FBX in the Project window, drag the mesh inside it) into `_WallStrip`
2. Press **Rebuild All Borders** (right there in Wall Strip Import)
3. Select a border and check **Strip Length** (Runtime Data) says about **4**. If not, change `_StripScale` on the asset and rebuild
4. If the wall faces the wrong way or runs backwards: `_FlipDepth` / `_FlipLength`, then rebuild

From then on, **saving over the strip from Maya rebuilds every wall and crack by itself**.

**Rules for the strip:** length along X, height along Y, its Y = 0 is the raised floor. Both end edges need the same profile (same vertex heights and depths) so the copies weld. Enough edge loops along the length for the gaps and noise to bend smoothly. The part below Y = 0 gets stretched down to `_BottomDepth`, so any depth works.

### Shape one ring
Select `Zone Borders/Ring k Border`. Change settings, then press **Rebuild Ring**. The foldouts:

| Foldout | Settings | Notes |
| :--- | :--- | :--- |
| Wall Shape | `_BottomDepth`, `_HeightScale`, `_RadiusOffset`, `_RevealStartAngle` | Bottom Depth must be more than the lift height or a gap shows under lowered zones |
| Lean | `_LeanAngle`, `_LeanCurve` | A straight-line curve = a straight slant. A curve that rises late = flares at the top |
| Noise | `_HeightNoise`, `_WobbleNoise`, `_LeanNoise`, `_NoiseBumps`, `_NoiseSeed` | Give each ring its own seed |
| Hand Spots | `_Spots`, `_SpotFalloff` | See below |
| Gate Gaps | `_GapMargin`, `_GapRampWidth`, `_GapRampCurve` | Gaps come from the gates on that ring automatically |

### Hand spots
1. In the border's **Hand Spots** foldout, press **+** to add a spot
2. Set its **Width** (meters each side), **Height Boost**, **Push** and **Lean**
3. In the scene view, drag its cyan dot around the ring
4. **Rebuild Ring**

With the border selected, the gizmos show the gate gaps (red), their slopes (orange) and the spots' reach (cyan).

### After moving a gate or changing the zone width
Press **Rebuild All Borders**. Gates are found by looking for nodes with an Evolve effect sitting on the ring, so a gate off its ring gets no gap (use the node's snap buttons first).

---
## Part 4 - Cracks
### Add a crack
1. Select a border, open **Ring Tools**, type an angle (degrees; 0 = the field's right, 90 = its forward) and press **Add Crack**
2. The new crack is selected. Drag its pink dots across the floor. The white ring is the start, which sticks to the ring by itself
3. **Add Point** / **Remove Last Point** for more or fewer points
4. Press **Rebuild My Ring**

### Branches
1. Make a crack (any way), then set its **Parent Crack** to the crack it branches off, and **Branch At** (0 = the parent's start, 1 = its tip)
2. Its start now sticks to that spot on the parent. **Rebuild My Ring**

### Crack settings
`_HeightScale`, `_WidthScale`, `_HeightCurve` (height from start to tip) and the same noise controls as the wall. Change, then **Rebuild My Ring**.

**Keep crack objects under their zone** (zone k + 1 for ring k), or they won't lift with the floor. Add Crack puts them there. The trenches in the floor mesh and the burn decals (HDRP Decal Projectors) are placed by hand under the same zone.

---
## Part 5 - Plug In the Wall Materials
*Changed Oct 13, 2026: the walls swap between two materials.*

| Look | Material | Cracks |
| :--- | :--- | :--- |
| Tear (forward border) | `_TearMaterial` (the one place to set it). If empty, the strip model's own material (`MAT_ZoneWall_Flame`), and an info box says so | Shown, same material |
| Smoke (border behind) | `_SmokeMaterial` | Off |

1. On `SO_RuneField_Zones` (Walls): drag the tear material into `_TearMaterial`, or press **Use Wall Strip's Material** to copy in the strip model's material
2. Drag the smoke material into `_SmokeMaterial`
3. Press **Rebuild All Borders** (in Walls)
4. Both materials need the **same height property** (`_HeightProperty`, `_GradiantHeight` now). Set each material's full height on the material itself; the code animates from `_HiddenHeight` (gone) up to that value. If "gone" isn't 0 for your shader, change `_HiddenHeight`
5. Optional on the tear material: `_Reveal` (0 to 1, the sweep, read with UV2.x) and `_Overcharge` (later)

**How the swap plays** when a zone opens: the old tear wall and its cracks animate out, the smoke wall animates in (`_SwapPoint` splits `_BorderBlendTime`), the smoke behind it animates out, then the new tear wall and cracks animate in. Height is set per wall in play mode, so to check a material's full look outside play mode, use the **Preview Open Zone** slider.

What the mesh carries for the shader: UV2.x reveal order, UV2.y height up the strip, UV3.x gate gap mask, UV3.y distance along a crack.

---
## Part 6 - Smoke Particles
*Off by default since Oct 13, 2026* (the smoke material does the look). To turn them on, tick `_UseSmokeParticles` on `SO_RuneField_Zones`. Then:
- Optional prefab in `_SmokePrefab`: **Simulation Space: World**, **Emission** off (the code places every puff), moved by **Velocity over Lifetime**
- `_SmokePerMeter` sets how thick it is. The code skips the gate gaps. Play mode only
- Without a prefab: stand-in grey squares (`_StandInSmoke...` settings)

---
## Part 7 - The Zone Opening
One step per new zone:

```text
time ─────────────────────────────────────────────►
[ zone rises, lights change ......... ] (_RiseTime, _RiseCurve)
[ old tear → smoke, older smoke → off ] (_BorderBlendTime, _BorderBlendCurve)
      ↑ _RevealDelay
      [ new border lights up, sweep around and out ..... ] (_RevealTime, _RevealCurve)
```
- Several zones at once (rare) play one after another, `_StepGap` apart
- The sweep starts at the border's `_RevealStartAngle` and spreads both ways
- **To test:** play, select the `Rune Field`, Test Tools → **Replay From Seen Zone** with a lower zone than the open one (the slot needs to have evolved at least once). Or set a slot's `_SeenOpenZone` lower by hand on its `SO_Shade_Slot` asset before playing

---
## Part 8 - Changing the System Itself
Where to look for common changes:

| I want to... | Open | Look for |
| :--- | :--- | :--- |
| Change which zone is frozen / open / locked | `RuneFieldZonesObject` | `GetZoneState` |
| Change which border shows tear / smoke | `RuneFieldZonesObject` | `ShowOpenZone` (snap) and `PlayOpeningStep` (the opening) |
| Add a new value to a zone's look (say, fog) | `RuneFieldZonesSO` (the value), `Zone3DObject` | `ShowState` / `ApplyLook` |
| Add a step to the zone opening (camera move, sound) | `RuneFieldZonesObject` | `PlayOpeningStep` (per zone) or `PlayOpeningSteps` (before / after all of them) |
| Play something when the opening ends | `RuneFieldZonesObject` | the end of `PlayOpeningSteps`. A UnityEvent there would let it be hooked up in the inspector |
| Send another value to the wall materials | `RuneFieldZonesSO` (property name), `ZoneBorder3DObject` | `ApplyLook` |
| Change how tear swaps to smoke | `RuneFieldZonesObject` | `ShowTearToSmoke` |
| Change how the wall bends or what the noise does | `ZoneBorder3DObject` | `BuildWallMesh` |
| Change how cracks are shaped | `ZoneCrackPath` | `AppendMesh` |
| Change how the strip is read or welded | `ZoneStripBuilder` | `FromMesh`, `MeasureAndMatchEnds`, `Layout` |
| Change the scene handles | `Editor/ZoneToolsEditor` | `DrawSpotHandles`, `DrawCrackHandles` |
| Change what triggers an automatic rebuild | `Editor/ZoneToolsEditor` | `ZoneStripImportWatcher` |
| Change when the opening plays | `RuneFieldZonesObject` | `OnFieldLoaded` |
| Add a 5th zone | `SO_RuneField_Settings` `_ZoneCount`, then **Set Up Borders** and **Set Up Zone Lighting**. Up to 7 zones (HDRP has 8 light layers, one is the default) | |

**How the pieces talk:** the field fires `OnFieldLoaded` → the zones component works out the look → it calls `ShowState` / `ApplyLook` on each zone and `ShowLook` on each border. Nothing else changes them, so a new look only needs adding in those places.

**Things to keep in mind:**
- The code never writes to `SO_RuneField_Zones`. Keep it that way, so tuning in play mode stays safe
- Meshes are built in the editor and saved as assets. Rebuilding while playing is refused, since the strip can't be read then
- Walls are under `Zone Borders` (they stay put), cracks are under their zone (they lift). A new piece that should lift goes under a zone

---
## Testing Checklist
- [ ] **Preview Open Zone** 1 to 4: floors lift / lower, lights change, the right border is the tear and the one behind smokes
- [ ] **Set Up Borders**: three rings of wall, gaps around every gate
- [ ] Move a gate off and back onto its ring, **Rebuild All Borders**: the gap follows it
- [ ] Add a hand spot, drag it, rebuild: the wall changes there
- [ ] **Add Crack**, drag points, add a branch, rebuild: the cracks show and lift with their zone in the preview
- [ ] Save over the strip in Maya: the walls rebuild by themselves
- [ ] Preview: the forward ring shows the tear material with cracks, the ring behind shows the smoke material without cracks
- [ ] **Replay From Seen Zone**: the zone rises, the old tear animates out and the smoke animates in, the new tear animates in
- [ ] Level a shade into a gate, close and reopen the field: the opening plays once, then never again

---
## Troubleshooting
| Problem | Check |
| :--- | :--- |
| Floors don't tint | The camera's Frame Settings have **Light Layers** on; the zone lights exist (**Set Up Zone Lighting**); the floor is under a zone (**Apply Zone Layers**) |
| "can't use the wall strip ... no vertices" | Rebuild in edit mode, not while playing. If it still happens, tick **Read/Write** in the strip FBX's import settings (Model tab) |
| Walls look torn at every copy seam | The strip's two end edges don't match (a warning says so). Fix the ends in Maya |
| A gap where the wall meets a lowered floor | Raise the border's `_BottomDepth` above `_LiftHeight` |
| No gap at a gate | The gate isn't exactly on the ring (use the node's snap buttons), or it has no Evolve effect. Then **Rebuild All Borders** |
| A crack doesn't lift with its floor | Its object (and `Ring k Cracks`) must be under zone k + 1 |
| A crack doesn't show after rebuild | It needs at least two points apart from each other, and a Start Border (or a parent on a ring) |
| A wall is invisible | Its height property name must match `_HeightProperty` on both materials, and `_HiddenHeight` must be the 'gone' value, not the full one |
| The opening doesn't play | The seen zone must be lower than the open zone and above 0. Check the slot's `_SeenOpenZone` |
| Pink smoke | The smoke prefab's material is missing or not HDRP |

---
## Not Built Yet
- The art: floor meshes with trenches, the final strip, the border shader, the smoke, burn decals ([[Zones - Art To Do]]) [[Notes for the future]]
- The bridge break effect, played before the zone opening after a save [[Notes for the future]]
- The overcharge pulse before bridges are cut on save (`_Overcharge`) [[Notes for the future]]
- A camera move during the opening [[undecided]]
- Something in the background at Legend (ring 4) [[undecided]]
- Automatic burn decals along cracks [[Notes for the future]]

---
## Related
[[Zone Visuals Plan]] · [[Rune Field 3D View]] · [[Zones - Lighting Guide]] · [[Zones - Shader Graph Recipe]] · [[Zones - Art To Do]] · [[Shade (Runtime)]]
