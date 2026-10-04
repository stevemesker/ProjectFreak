## Overview
`ScreenDamageUIManager` is the entry point for showing damage numbers on screen. It holds a reference to the `UIDamageCanvas`, which does the actual work (see [[Damage Popup System]]).

Reached with `ScreenDamageUIManager._UIdamage`. In `Awake` it claims the singleton if it's free, and `OnDestroy` clears it. It doesn't destroy duplicates itself, since it lives on the Game Manager and the Game Manager's own duplicate check already does that.

**Where it lives:** the `DamageCanvasUI` object (screen space canvas + `UIDamageCanvas` + `ScreenDamageUIManager`, with the pooled `Damage` label as a child) is a child of the **Game Manager** prefab, so popups work in every scene, including dungeon floors. *Before Oct 2026 it only existed in `New_Player_Movement_Scene`, so enemies elsewhere never showed popups.* That scene's old copy has been removed. *This is temporary: once the planned UI root is built, the damage canvas moves there and registers itself with this manager (see [[HUD Manager#Planned: UI Root]]).* [[Notes for the future]]

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
