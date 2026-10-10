## Overview
A reusable popup window: a title, a message and up to 3 buttons (like **Save / Discard / Cancel**). Any script can show it and say what each button does. Added Oct 9, 2026 for the [[Rune Field System]] save flow, and meant for the 3D rune field and other menus too.

Script: `Scripts/UI/ConfirmPopup.cs`.

Back to [[Architecture Atlas]]

---
## Setup
1. Under a canvas, add an empty object and put `ConfirmPopup` on it
2. Press **Build Default Layout**. It builds a plain window under it and fills in the references (Ctrl+Z undoes it)
3. Restyle it however you like (colors, fonts, sprites, sizes). The script only needs the references to stay assigned

The object with `ConfirmPopup` stays active; only its `_Window` child is shown and hidden, so the script keeps running.

**Built layout:**
```text
ConfirmPopup (stretched over the canvas)
└── Window              (shown/hidden)
    ├── Backing         (dark, full screen, blocks clicks behind the popup)
    └── Panel
        ├── Title
        ├── Message
        └── Buttons     (Horizontal Layout Group)
            ├── Button 0 (Label)
            ├── Button 1 (Label)
            └── Button 2 (Label)
```

---
## Variables

| Variable | Description |
| :--- | :--- |
| `_Window` | The whole popup window, shown and hidden. Must be a child of the `ConfirmPopup` object |
| `_TitleText` / `_MessageText` | The title and message texts |
| `_Buttons` | Each button and its label text, left to right. Up to 3 are used |
| `_DefaultPanelSize` | Panel size used by **Build Default Layout** (620 × 320) |

---
## Functions

| Function | Description |
| :--- | :--- |
| `Show(title, message, labelA, actionA, labelB, actionB, labelC, actionC)` | Opens the popup. Pass a label and an action for each button you want; leave the rest out and they're hidden. An action can be `null` for a button that only closes the popup (like Cancel). Moves itself to the top of its canvas so it draws over everything |
| `Close()` | Hides it without running any action (what Escape does in the rune field) |
| `IsOpen()` | True while it's showing |

Clicking a button closes the popup **first**, then runs its action, so an action can open another popup.

Odin buttons: **Build Default Layout**, **Test Show** (play mode).
