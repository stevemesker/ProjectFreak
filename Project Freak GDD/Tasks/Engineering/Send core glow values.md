---
status: Done
priority: 5
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Rune Field Finish Plan]]"
source: "[[Rune Field 3D Change Requests and to Do]]"
needs:
  - "[[Core shader]]"
created: 2026-10-29
done: 2026-11-07
---
## What
The core sends its shader:
- **Glow:** spare power as 0 to 1. 0 spare = the regular look, full glow at 10 spare (tunable on the core)
- **Over budget:** on / off while runes are waiting for power, blended over a short time so it doesn't pop
Property names are inspector fields in the core's Shader Properties foldout. The shader does the dulling and strobing.

## Completed Info
- **Over budget built** as the depleted value (Nov 7, 2026): `Core3DObject` ramps 0 to 1 while runes are waiting for power and back down after, and sends it to the orb shader (`_DepletedValue`) and the `OrbVortex` VFX Graph (`DepletedValue`). Ramp times and curve are in the core's **Depleted** foldout, with a **Test Depleted** play mode button. Details in [[Rune Field 3D View]], Core3DObject
- **Glow not built.** Steve put it on hold until playtesters show it's needed, so there's no spare power value in the shader or the code
- Change from the plan: the over budget value ramps with a curve (up and down times are separate) instead of a fixed blend, like the node cover and shield
- **Rule change** (Steve, Nov 7): the core only goes depleted when it can't afford a rune that's bridged to it. Runes with no bridge back to the core used to count as waiting too, so unplugging the rune that tipped it over left the core depleted. `RuneField` now keeps `_starvedRunes` while working out power, and `HasRunesWaitingForPower` reads it. New `TestWaitingOnlyCountsStarved` in the logic tester
- **Found while testing:** the scene's Rune Field had an empty Core slot (emptied when the core was swapped for the new prefab), so the core never got power updates. `RuneField3DObject.Start` now finds the core under the field with a warning to press **Set Up Field**
- **Watch for:** a new serialized field keeps the value it was first loaded with until the prefab is saved. The core's **Depleted Property** kept the old typo spelling after the default was fixed, until it was changed by hand on `PFB_Core_3D`
- The orb sphere's sorting against the vortex is fixed with sorting priority for now. Moving it into the VFX Graph is [[Move the core orb into the VFX Graph]]

## Notes
- From the rune field finish talk, Oct 29, 2026. Decisions in [[Rune Field Finish Plan]]

## Waiting on this
Cards that link to this one, usually through their `needs`.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Waiting on this
    order:
      - file.name
      - file.folder
      - status
      - owner
```
