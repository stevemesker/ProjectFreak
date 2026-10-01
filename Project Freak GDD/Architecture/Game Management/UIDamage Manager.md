## Overview
`ScreenDamageUIManager` is the entry point for showing damage numbers on screen. It holds a reference to the `UIDamageCanvas`, which does the actual work (see [[Damage Popup System]]).

Reached with `ScreenDamageUIManager._UIdamage`. In `Awake` it sets the singleton, or destroys itself if one already exists.

---
## Data

| Variable        | Description                            |
| :-------------- | :------------------------------------- |
| `_damageCanvas` | The `UIDamageCanvas` in the HUD        |

---
## Usage

```csharp
if (ScreenDamageUIManager._UIdamage != null)
    ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(worldPosition, amount, isCrit);
```

Negative amounts display as healing.

The **DamageTester** dev script has buttons to test damage, healing, and crit popups (see [[Utility Scripts]]).
