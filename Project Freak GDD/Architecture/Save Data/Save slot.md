The save slot is a scriptable object that holds the save data while the game is actively running and is the go between for the actual save file and the game's visual access. An example would be the load selection card that displays data such as chapters, player level, and current scene.

---
**Game Data**
General data related to the game


| Type       | Description                                                                              |
| :--------- | :--------------------------------------------------------------------------------------- |
| Empty save | Boolean that knows if this slot is either an unused save slot or if the slot was deleted |

---
**Player Data**
Save slots need to track the player's various data and save it to the save file when necessary. Most of this is accessed when the game manager has to spawn in a new player pawn, typically at the launch of the game. The following is tracked by the save slot:

| Type          | Description                                                        |
| :------------ | :----------------------------------------------------------------- |
| Tamer Level   | current tamer level of the player                                  |
| Game Chapter  | What major chapter the player is on (used for opening scene stuff) |
| Quest Chapter | What sub-chapter the player is on per quest                        |


---
**Shade Data**


| Type                   | Description                                         |
| :--------------------- | :-------------------------------------------------- |
| Shade Slot Selection   | What the player's current active shade slot is      |
| Shade Stats            |                                                     |
| Rune field data        | position and activated state of all runes and nodes |
| Shade active abilities |                                                     |

---

**Scene Data**


| Type                   | Description                                                                                              |
| :--------------------- | :------------------------------------------------------------------------------------------------------- |
| Last Door Used ID      |                                                                                                          |
| Current Scene Readable | Last player facing name for the scene they're currently on. This is for readability on save slots in ui. |
| Current Scene          | Actual scene last entered by the player, used for loading the scene                                      |

---
**Entity States**
Some entities need to retain their current state when saving/loading such as chests or npc's that change state/position due to cutscene changes.