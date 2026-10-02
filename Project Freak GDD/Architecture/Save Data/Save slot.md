The save slot is a scriptable object (`SaveSlotSO`) that holds the save data while the game is actively running and is the go between for the actual save file and the game's visual access. An example would be the load selection card that displays data such as chapters, player level, and current scene.

Save slots are managed by the [[Save Manager]]. The **In Code** column shows which fields exist in `SaveSlotSO` right now and which are still planned. `ResetData()` sets every existing field back to its empty default.

---
**Game Data**
General data related to the game


| Type       | Variable   | In Code | Description                                                                              |
| :--------- | :--------- | :------ | :--------------------------------------------------------------------------------------- |
| Empty save | `_IsEmpty` | Yes     | Boolean that knows if this slot is either an unused save slot or if the slot was deleted |

---
**Player Data**
Save slots need to track the player's various data and save it to the save file when necessary. Most of this is accessed when the game manager has to spawn in a new player pawn, typically at the launch of the game. The following is tracked by the save slot:

| Type          | Variable            | In Code | Description                                                        |
| :------------ | :------------------ | :------ | :----------------------------------------------------------------- |
| Tamer Level   | `_PlayerTamerLevel` | Yes     | current tamer level of the player                                  |
| Game Chapter  | `_SaveChapter`      | Yes     | What major chapter the player is on (used for opening scene stuff) |
| Quest Chapter |                     | Planned | What sub-chapter the player is on per quest [[Notes for the future]] |


---
**Shade Data**


| Type                   | Variable              | In Code | Description                                         |
| :--------------------- | :-------------------- | :------ | :-------------------------------------------------- |
| Shade Slot Selection   | `_ShadeSlotSelection` | Yes     | What the player's current active shade slot is      |
| Shade Stats            |                       | Planned | [[Notes for the future]] |
| Rune field data        |                       | Planned | position and activated state of all runes and nodes [[Notes for the future]] |
| Shade active abilities |                       | Planned | [[Notes for the future]] |

---

**Scene Data**


| Type                   | Variable                | In Code | Description                                                                                              |
| :--------------------- | :---------------------- | :------ | :------------------------------------------------------------------------------------------------------- |
| Last Door Used ID      |                         | Planned | [[Notes for the future]] |
| Current Scene Readable | `_CurrentSceneReadable` | Yes     | Last player facing name for the scene they're currently on. This is for readability on save slots in ui. |
| Current Scene          | `_CurrentScene`         | Yes     | Actual scene last entered by the player, used for loading the scene                                      |

---
**Entity States**
*Planned.* Some entities need to retain their current state when saving/loading such as chests or npc's that change state/position due to cutscene changes. [[Notes for the future]]
