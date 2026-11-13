---
status: To Do
priority: 5
chapter:
  - Chapter 3 Crafting
owner: Claude
plan: "[[Zone Visuals Plan]]"
source: "[[Rune field lighting and style test]]"
needs:
created: 2026-11-12
done:
archived:
---
## What
Take out the light-based zone tint (one shadowless directional light per zone, each on its own HDRP light layer). The open / frozen / locked look moves to the zone materials instead (how is still to be decided).

**Remove**
- `Zone3DObject`: `_ZoneLight`, the light half of `ApplyLook` (color and intensity), `GetZoneLight` / `SetZoneLight`, and the light layer code (`ApplyZoneLayer`, `GetLightLayerMask`, `IsSkipped`, `LightLayerBits`, `DefaultLightLayer`). `MaxZone` no longer needs to stop at 7 for light layers
- `RuneFieldZonesObject`: `_KeyLight`, **4. Set Up Zone Lighting**, **Match Zone Lights To Key Light**, `FindKeyLight`, `MakeZoneLight`, **1. Name Light Layers** / `PickLayerName`, **Apply Zone Layers** (and its calls on start and in setup), the `Zone Lights` holder
- Zone opening: the locked → open and open → frozen light blends. The rise and the timing stay
- `RuneFieldZonesSO`: `_OpenLight`, `_FrozenLight`, `_LockedLight`, `ZoneLightEntry`, `GetLight`. Keep the state colors if the material tint wants them
- Scene (`3d_Evolution_UI_scene`): delete **Rune Field > Zone Lights** (Zone 1–4 Light)
- Zone renderers go back to the Default light layer only
- HDRP Global Settings: light layers 1–4 can go back from "Zone 1–4" to their default names
- Update [[Zone Visuals Plan]], [[How To - Zone Visuals]], [[Rune Field 3D View]] and [[Zones - Lighting Guide]] to match

**Decide before removing**
- `ZoneTintSkipObject`: keep it as "skip the material tint" for crystals and glowing bits, or remove it
- Where the state tint colors live (the zones asset is the obvious spot)

**Keep**
- Lift heights, borders, cracks, tear / smoke, the opening sequence timing
- The scene lighting from the style test: Sun as ~30 lux moonlight, the Spot Light under Rune Field Camera (400 lux at 15 m), Gradient Sky (exposure ~8) with Dynamic ambient, Fixed Exposure EV 7

## Completed Info


## Notes
- From the lighting pass with Claude, Nov 12, 2026. Under the new moody lighting the zone lights were buried by the sky ambient and the spotlight (frozen at 90 lux was about 25% of the floor's light outside the pool, much less inside it), so the states stopped reading. A material tint reads the same in the light and in the dark
- Until the material tint is in, the zones won't show their state. Fine to do this first or alongside the material card
- [[Camera follow light]] says the zone lights keep copying the fixed key light. That line goes away with this card

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
