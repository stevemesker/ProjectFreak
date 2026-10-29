How the zone tint works with light layers, what Claude sets up, and how to build the final lighting. The full plan is [[Zone Visuals Plan]], section **Floor Tint: Light Layers**. Back to [[Zones - Art To Do]].

---
## How it works (the short version)
- Every zone has its own **light layer** (Zone 1 to Zone 4). Everything under a zone's object goes on its layer automatically
- Each zone has one **shadowless zone light** that only lights its own layer. The open / frozen / locked look is that light's color and brightness
- One shared **key light** casts the shadows and lights everything, a bit dimmer than the final look. The zone lights bring each zone up to its look
- Runes, nodes, the core and the walls of light stay on the **default** layer, so they're never tinted

---
## What Claude sets up (build step 1)
1. Checks the settings. **Already confirmed Oct 12, 2026:** Light Layers are supported in all three quality levels' HDRP assets (High Fidelity, Balanced, Performant). Still to check: the camera's Frame Settings have Light Layers on. Claude will point you to the exact spot if it's off
2. Layer names: Light Layers 1 to 4 named **Zone 1** to **Zone 4** (Project Settings → Graphics → HDRP Global Settings). Layer 0 stays the default. *A one-minute rename you do by hand if code can't do it safely*
3. **Set Up Zone Lighting** button on the zones object: makes a `Zone Lights` group with four shadowless directional lights, each set to its zone's layer, pointing the same way as the key light
4. **Apply Zone Layers** button: puts every renderer under each zone on that zone's layer (the same happens when the field loads, so it can't drift)
5. **Preview Open Zone** buttons (1 to 4): show any state in edit mode, without pressing play
6. `ZoneTintSkipObject`: put it on anything under a zone that should **not** be tinted (glowing crystals, magic bits). Its renderers stay on the default layer

---
## Building the final lighting (Steve)
**1. Kit pieces go under their zone.** Drop rocks and debris anywhere under the right zone's object (sub-groups are fine). Never set layers by hand: press **Apply Zone Layers**, or just press play.

**2. Get the open look first.** Press **Preview Open Zone 4**: every zone is open (raised, neutral). Light the scene the way you want the open look:
- The key light (direction, color, shadows)
- Sky and ambient in the scene's Volume. Keep ambient fairly low: it isn't split by layers, so the more of the look comes from ambient, the weaker the tint will be
- The background and the area past the edge

**3. Split the brightness between the key light and the zone lights.** Turn the key light down a bit and the **open** zone light up until it looks like step 2 again. The zone light's share is what the tint can change, so the more brightness it carries, the stronger frozen and locked can look. Start around half and half and adjust.

**4. Tune the three states.** With the game playing, open `SO_RuneField_Zones` and change the **Open**, **Frozen** and **Locked** light colors and intensities. Changes made while playing stay after stopping. Use the test kit's **Level Up** / **Level Down** to flip between states.
- **Frozen:** shift toward blue, maybe a touch dimmer
- **Locked:** dimmer, muted purple (or blue / green, still undecided). Darker comes from giving the zone light less
- **Open:** neutral

**5. Check the transitions.** Level up and watch the blend between states (timings are on the same asset).

**6. Check on the Steam Deck.** The zone lights are shadowless, so they're cheap. If the frame rate needs it, Claude can turn Light Layers on for the rune field's camera only, so gameplay never pays for them.

---
## If the tint isn't strong enough
Two fallbacks, talked through Oct 12, 2026. Either one stacks with or replaces the lights:
- **Material copies per zone:** the code makes one copy of each kit material per zone and tints its Base Color. Exact color, but it tints the texture flat
- **Screen effect by distance from the core:** one fullscreen tint that works out each pixel's zone from its distance to the core. Covers everything with no setup, but runes and walls need keeping out of it. Claude writes the shader

---
## Tips
- A light only affects objects that share one of its layers. If a kit piece isn't tinting, check it's under a zone and press **Apply Zone Layers**
- Things that glow on their own (emission) aren't changed by lights at all, so emissive kit pieces won't tint. Use the skip marker on them to keep things clear
- Moving the key light? Press **Set Up Zone Lighting** again (or turn the zone lights to match) so their direction follows
