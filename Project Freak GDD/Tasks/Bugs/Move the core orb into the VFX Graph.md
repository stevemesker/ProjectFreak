---
status: To Do
priority: 5
chapter:
  - Chapter 5 Art
  - Chapter 3 Crafting
owner: Steve
plan: "[[Rune Field Finish Plan]]"
source: "[[Send core glow values]]"
needs:
created: 2026-11-07
done:
archived:
---
## What
Cleanup: the orb sphere is part of `Mesh_RuneField_Core.fbx` and fights the `OrbVortex` particles for sorting. Right now that's fixed by raising the sorting priority, which works. Moving the sphere into the VFX Graph would give a fixed draw order inside one effect and put the orb and vortex in one place for the overload effect.

- [ ] Take the orb sphere out of `Mesh_RuneField_Core.fbx` (or turn its renderer off in `PFB_Core_3D`)
- [ ] Turn on **Support VFX Graph** on `SH_Runefield_Core_Orb` (Graph Settings), then save
- [ ] In `OrbVortex`, add a system that spawns one particle that lives forever at the core's center, with an **Output Particle Mesh** using the sphere and `SH_Runefield_Core_Orb` (needs Edit → Preferences → Visual Effects → Experimental Operators/Blocks on to see the Shader Graph field in Unity 2022)
- [ ] Plug the existing `DepletedValue` Blackboard property into the orb output's depleted port too
- [ ] Put the orb output before the vortex (output Sorting Priority), then undo the sorting priority workaround on `MAT_Core_Orb`
- [ ] The range highlight (`_RangeHighlight`) reaches the orb through its renderer today. Once it's in the VFX Graph it needs an exposed property and a line in `Core3DObject` to send it
- [x] Fix the typo in the orb shader's reference name `_DepleatedValue` → `_DepletedValue`, and update **Depleted Property** on the core (Shader Properties foldout) to match. *Done Nov 7, 2026, along with `DepleatedInnerColor` and the old color names on `MAT_Core_Orb`*
- [ ] Check sorting at every camera distance, and the depleted ramp (**Test Depleted** button on the core) still drives both

## Completed Info


## Notes
- From the depleted hookup talk, Nov 7, 2026. The sorting priority fix is what's in the game now

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
