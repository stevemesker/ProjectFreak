## Overview
The radial menu is a hold-to-open ring of ability buttons. The player holds the radial menu button, points at a slice with the mouse or right stick, and releases to use that ability.

Scripts:
- **`PlayerMenuInputs`** (on the player) - listens for the radial menu button
- **[[HUD Manager]]** - opens/closes the menu and fills it with abilities
- **`RadialMenuManager`** - builds the ring and tracks the selection
- **`RadialButton`** - one slice of the ring

---
## Flow

```text
Press radial button
    ↓
PlayerMenuInputs → HUDManager.toggleRadialMenu()
    ↓
Menu turns on, gets filled with PlayerData._TamerAbilities
Player aiming is turned off
    ↓
Mouse / right stick picks a slice
    ↓
Release radial button
    ↓
HUDManager.toggleRadialMenu() → RadialMenuManager.UseButton()
    ↓
Selected RadialButton → AbilityInterpreter.InitializeAbility()
```

See [[Ability System]] for what happens after that.

---
## RadialMenuManager

### Inspector Data

| Variable                    | Description                                                        |
| :-------------------------- | :----------------------------------------------------------------- |
| `_DialPrefab`               | Slice prefab (has `RadialButton` and a radial fill `Image`)        |
| `_ButtonDescriptionText`    | Text showing the selected ability's name                           |
| `_DialButtonSpacing`        | Gap between slices, in degrees                                     |
| `_radialButtonHolder`       | Parent and center point of the ring                                |
| `_minimumSelectionDistance` | How far the stick must move before anything is selected            |
| `_colorPalette`             | Primary color for normal slices, secondary for the selected slice  |

### Building the Ring — `UpdateRadialButtonSetUp(count, abilities)`
- Removes extra slices if there are fewer abilities than before
- Updates existing slices
- Spawns new slices as needed
- Each slice is rotated to its spot and its `Image.fillAmount` is set so the slices form a full circle minus the spacing

### Selection
- **Controller:** the right stick direction is used directly
- **Mouse:** the direction from the ring's center to the cursor is used
- The angle of that direction picks the slice. Inside `_minimumSelectionDistance`, nothing is selected and the text shows `---`
- The selected slice is highlighted, the previous one is unhighlighted

A red debug line in the Scene view shows the selection direction.

---
## RadialButton

| Function                    | Description                                         |
| :-------------------------- | :-------------------------------------------------- |
| `SetUpDial(ability, spacing)` | Stores the ability and offsets the icon          |
| `SetUpDialIcon(count, index)` | Positions and sets the ability icon (hidden if only one slice) |
| `OnSelection` / `OnDeselction` | Switches the slice color                        |
| `Activation(source)`        | Starts the ability on the source's `AbilityInterpreter` |
