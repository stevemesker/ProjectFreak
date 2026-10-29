What's left before the 3D rune field counts as finished, talked through one item at a time with Steve on Oct 28–29, 2026. Every item has its card(s) on the boards, listed in **Cards** at the bottom.

Back to [[AA - AI Info]] · [[Rune Field 3D View]] · [[Rune Field 3D Change Requests and to Do]]

---
## Decisions

### Placement preview
- **No ground ring.** The range overlay already shows reach on the floor and follows the landing spot. It gets one more value, "can't land here", so its decal can tint red when no spot nearby works. Then the rune's own red tint can go
- **Pointer line:** Steve's pole / line from the dragged rune down to where it would land
- **Preview bridges:** one per *new* bridge a drop would make, in a variant of the bridge material (warped / ghosted), straightening once in range. Bridges the rune already has behave as they do now
- Works for all three ways of placing: mouse drag, gamepad carry, dragging out of the list
- Its own component, like the range overlay. Its settings sit next to the field's **Dragging & Placing** group

### Rune looks by tier
- `ElementItemSO` gets `_RuneMaterial` (optional, empty = the prefab's material) and `_RuneEffect` (optional prefab, spawned and parented to the rune), next to `_Palette`. Used for runes on the field and the stand-ins dragged out of the list
- **One rule for every rune shader:** the same property names (`_BaseColor` for color, `_RangeHighlight` for the range glow). Colors go through a property block, so any material with those names works
- The effect prefab gets tinted too if its shader has the color property (easy to change)
- Tiers (basic, elemental, mythical...) are Steve's design work. For now tier is only how good a rune *looks*; whether it changes stats is decided after the first full playtest [[undecided]]

### Lighting, style and the follow light
- Option B: a fixed key light (sun / moon) stays, and a light that follows the camera (aimed at its focus point) sits on top, Diablo-style. The zone tints keep copying the fixed key light
- Waits on Steve's lighting and style test (the ideal look first, then see what holds up)

### Node states
- Four forms: **idle (open)**, **plugged in**, **locked**, **frozen**. Several model pieces move / rotate between them; the details come with the final model
- Frozen and locked-in share a look: the bridged network of runes is what tells a frozen node from a locked-out one
- Whether **powered** looks different from plugged in is an open art question (it may be handled by how runes and bridges look when powered) [[undecided]]
- State changes animate with curves, like the zone opening

### Node links (unlocks / lockouts)
- **Steve draws each link by hand**, with the same kind of tool as the floor cracks (points with scene handles, a strip mesh built along them, a repeating link texture)
- Editor-only warning when a node unlocks or locks out another with no drawn link
- Unlock links and lockout links get their own materials (maybe different shaders)
- Shown on **hover and selection** (selection matters for links that run off screen: edge arrows or the camera fitting them, decided when built)

### Info popup
- **Hover:** a small name tag by the pointer, after a short delay
- **Select:** full details in a docked panel (placeholder: the bottom third of the right panel; Steve's side panel art pass decides where it lives, a 3D pop-out stays an option)
- **Mouse:** a quick **tap** selects, **holding** picks a rune up (a time, tunable, about 0.2 s). Not movement-based
- **Gamepad:** right stick click selects, for now (see how it plays)
- **Shows:** rune: name, element, power cost, reach, bridges used / max, stat changes, powered / frozen / locked in. Node: name, effects, unlocks and lockouts, gate (and which form), state. Core: power used / total, runes waiting, shard
- Shares "selected" with the node links
- Merged with the old **Node stat displays** card

### Core
- Glow follows spare power: 0 spare = the regular look, 10 spare (tunable) = full glow, sent as one 0–1 value
- Over budget (runes waiting for power): an on / off value, blended so it doesn't pop. The shader dulls it and strobes a color

### Bridges
- **Break effect:** one prefab field on the field. When a bridge tears or a save cuts it, the effect spawns at the bridge's middle, turned along it, and deletes itself (`TimedDelete` or the particle system's Stop Action). Same effect for both
- **No more bridge shader work.** The tear color already is the strain look, so the unused `_Strain` value goes, along with the wall's unused `_Overcharge`

### Zone opening extras
- **Dropped (Oct 29, 2026):** the camera move during the opening and the overcharge pulse before a cut. The evolve warning before saving, plus the evolution animation that plays when a shade evolves in combat, is enough feedback
- The final look of the evolve confirm window is a Design card

### Shards
- A **Shards tab** in the side panel (no mixing into the rune list)
- Slotting: drag a shard onto the core, or A on a shard then A on the core. Picking one up glides the camera to the core
- Part of the draft like runes: the power shows right away, saving makes it permanent and **asks first**. Discard / cancel gives it back
- **One shard per shade, for good.** Once the core has one, the tab and every entry grey out. Kept reversible with a **Shards can be replaced** checkbox on the rune field settings (off), in case players ask for swapping later

### Saving to disk
- Not part of this effort. Its own card and the [[Save Data Plan]] note

### Opening the field in the game
- **A plain scene swap, not loaded on top.** Editing only happens at access points, so nothing about the scene behind needs to stay alive. It avoids HDRP lighting and volume conflicts and is lighter on the Steam Deck. The managers live on the Game Manager, so shade data and the inventory carry over anyway
- **Access points:** the hub lab (main one, it moves halfway through the game for story reasons), one **right before each boss**, and rare mid-dungeon safe rooms. Never mid-fight
- **Dungeon access points** are a small lamp a keeper left behind ("A keeper must have left this here before the purge"), with their own start animation
- **Launching:** each access point runs Steve's timeline cutscene (sit, fade), which calls a wrapper; the Scene Manager fades and swaps to the field scene, remembering where to come back to. Leaving the field (after the save question) loads that scene back at the access point
- **Access profiles:** the wrapper hands the field a small asset of checkboxes (can slot shards, can switch shades...). The hub uses a "Hub" profile, dungeon points a "Field Kit" one. The field itself is the same everywhere; the hub just has the extra features. What those are is Steve's design call (ideas: shard slotting, rune crafting, switching shades)
- **Remembering the dungeon:** going to the field unloads the dungeon, so the Dungeon Manager keeps what matters: the floor layout (fixes the open bug where revisiting a floor can load a different one), cleared rooms, opened chests and taken pickups (objects with IDs that register and ask "was I used?"). Access rooms only work once cleared. This is most of what mid-dungeon save and quit needs later
- No camera blend or pausing needed any more (those were for loading on top)

### Smaller ones
- **Tear memory:** tears reset when a rune is let go. Kept as is, no card
- **Form display names:** a display name and flavor text on `ShadeEvolutionSO`, used by the info window and the evolve popup, falling back to the asset name
- **List button shrink animation:** low priority, after the final UI art

---
## Cards
Every card that links this note (through `plan`, `needs` or anywhere in its text), grouped by board.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Cards
    groupBy:
      property: file.folder
      direction: ASC
    order:
      - file.name
      - status
      - owner
      - needs
```
