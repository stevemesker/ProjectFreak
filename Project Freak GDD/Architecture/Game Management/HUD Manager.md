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
- `ToggleBattleHUD(bool)` is a placeholder and does nothing yet
