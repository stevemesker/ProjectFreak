## Overview
`HUDManager` controls the on-screen HUD: full screen fades and the [[Radial Menu]].

Reached with `HUDManager._HUD`. Scene objects should use `HUDWrapper` (see [[Manager Wrappers]]).

In `Awake` it destroys duplicates, sets `_HUD`, and marks itself `DontDestroyOnLoad`.

---
## Inspector Data

| Variable              | Description                                  |
| :-------------------- | :------------------------------------------- |
| `FadeOutCanvasObject` | The `ScreenFadeOut` component used for fades |
| `_RadialMenu`         | The radial menu object                       |

---
## Screen Fades
Fades are done by `ScreenFadeOut`, which lives on a full screen UI `Image`.

| Function                 | Description                                                                  |
| :----------------------- | :--------------------------------------------------------------------------- |
| `FadeOut(float speed)`   | Fades the screen to black over `speed` seconds                               |
| `FadeIn(float speed)`    | Fades the black away over `speed` seconds                                    |
| `GetCurrentFadeValue()`  | Returns the current alpha (0 = clear, 1 = black)                             |

**ScreenFadeOut details**
- The fade follows `_fadeCurve` (an Animation Curve) so the feel can be tuned in the inspector
- Starting a new fade stops any fade already running
- The image is disabled when fully faded in, so it doesn't block UI clicks

Fades are used by the [[Scene Manager]] (fade in after a scene loads) and the [[Shade Manager]] (blink when switching control). Odin buttons for testing fades are under the **Screen Fades** foldout.

---
## Radial Menu
`ToggleRadialMenu()` is called by `PlayerMenuInputs` when the radial menu button is pressed or released:

1. If the menu was open, use the selected button
2. Flip the menu on/off
3. Fill the menu with the player's Tamer abilities (`PlayerData._TamerAbilities`)
4. Turn off player aiming while the menu is open

See [[Radial Menu]] for how the menu itself works.

---
## Other
- `ToggleHud(bool)` turns the whole HUD object on/off
- `ToggleBattleHUD(bool)` is a placeholder and does nothing yet [[Notes for the future]]

---
## Planned: UI Root [[Notes for the future]]
*Decided Oct 2026, not built yet. Build it when work on the final HUD starts.*

All persistent UI moves into one **`PFB_UI_Root`** prefab, spawned at startup by the [[Runtime Bootstrapper & Runtime Asset|Runtime Bootstrapper]] (added to the `RuntimeSettings` list next to the Game Manager), so it exists in every scene like the managers do.

**Split: logic on the Game Manager, visuals on the UI root**
- Manager scripts (`HUDManager`, `ScreenDamageUIManager`) stay on the [[AA - Managers|Game Manager]]
- The UI root holds only the visual objects, and **registers itself** with the managers in `Start()` (registration over searching, like `POISpawnerObject`, see [[Code Style Rules]]). The managers keep the references they get from it
- The managers handle a missing UI root safely (null checks, a warning), so a scene can still be tested without it

**Why not make it a child of the Game Manager**
- The HUD will be the most-edited prefab in the game. Keeping it separate means UI changes never touch the prefab that holds every manager
- Turning the whole UI on or off (title screen, cutscenes, menus) is one object instead of digging through the Game Manager's children
- Same idea as Wrappers: managers don't own scene visuals

**One root, several canvases.** When anything on a canvas changes, Unity rebuilds that whole canvas. So things that change every frame get their own canvas, and the static HUD isn't rebuilt each time a number pops up:

| Canvas | Holds | Changes |
| :--- | :--- | :--- |
| HUD | Health, shade slots, radial menu, ability icons | Now and then |
| Damage popups | `UIDamageCanvas` and its pooled labels | Every frame during combat |
| Overlay | Screen fades (`ScreenFadeOut`), menus | Now and then, drawn on top (highest sort order) |

**Moving the damage canvas.** For now, `DamageCanvasUI` is a child of the Game Manager prefab (see [[UIDamage Manager]]). Once the UI root exists, move it into the UI root as the damage popup canvas and have it register its `UIDamageCanvas` with `ScreenDamageUIManager`, instead of the manager sitting on the canvas object [[Notes for the future]]

**Open questions** [[Notes for the future]]
- Does `HUDManager` keep its own `DontDestroyOnLoad`, or does the Game Manager's persistence cover it? (It already lives on the Game Manager, so its own call is likely redundant)
- Is the current HUD canvas (`PFB_HUD Canvas`) the starting point for the HUD canvas, or is the final HUD built fresh?
