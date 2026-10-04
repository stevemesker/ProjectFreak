## Overview
`ScreenDamageUIManager` is the entry point for showing damage numbers on screen. It holds a reference to the `UIDamageCanvas`, which does the actual work (see [[Damage Popup System]]).

Reached with `ScreenDamageUIManager._UIdamage`. In `Awake` it sets the singleton, or destroys itself if one already exists.

**Where it lives:** the `DamageCanvasUI` object (screen space canvas + `UIDamageCanvas` + `ScreenDamageUIManager`, with the pooled `Damage` label as a child) is a child of the **Game Manager** prefab, so popups work in every scene, including dungeon floors. *Before Oct 2026 it only existed in `New_Player_Movement_Scene`, so enemies elsewhere never showed popups.* That scene still has its old copy, which removes itself on start since the Game Manager's copy already exists. It can be deleted from the scene.

---
## Data

| Variable        | Description                            |
| :-------------- | :------------------------------------- |
| `_damageCanvas` | The `UIDamageCanvas` on the same object |

---
## Usage

```csharp
if (ScreenDamageUIManager._UIdamage != null)
    ScreenDamageUIManager._UIdamage._damageCanvas.DisplayDamage(worldPosition, amount, isCrit);
```

Negative amounts display as healing.

The **DamageTester** dev script has buttons to test damage, healing, and crit popups (see [[Utility Scripts]]).
