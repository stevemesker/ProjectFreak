The Save Manager is in charge of saving and loading game data. It lives on the Game Manager object and is reached with `SaveManager._save` (see [[AA - Managers]]).

> **Status:** the save slot structure exists, but nothing is written to or read from an actual save file yet. All save/load functions are empty stubs.

---
**Save Slot**
[[Save slot]]s hold the entirety of a game instance within it. They're a scriptable object (`SaveSlotSO`) that will eventually write the data into an actual save file.

`_saveSlotList` holds every save slot.

**Active Save Slot Index**
`_ActiveSaveSlotIndex` is what save slot has been selected at startup. This will get saved out to another file so the game knows which slot to use when "continue" has been selected at the start. The [[Scene Manager]] reads the active slot's chapter to pick the opening scene.

---
## Functions

| Function                           | Description                                                                          |
| :--------------------------------- | :----------------------------------------------------------------------------------- |
| `GetSaveSlotData()`                | Returns the active `SaveSlotSO`                                                       |
| `GetCurrentActiveSaveSlot()`       | Returns the active slot index                                                         |
| `SetCurrentActiveSaveSlot(index)`  | Sets the active slot index                                                            |
| `ClearSave(index)`                 | Resets a slot's data and saves it                                                     |
| `SaveDataAll(index)`               | Calls each of the category saves below                                                |
| `SaveDataGame` / `SaveDataPlayer` / `SaveDataShades` / `SaveDataScene` | One save per data category. *Empty for now* [[Notes for the future]] |
| `LoadSaveDataToSlot()`             | Will load a save file into a slot. *Empty for now* [[Notes for the future]] |

---
## Notes
- `SetCurrentActiveSaveSlot` has its range check backwards, so a valid index can trigger the "outside of available save slots" error. See [[Known Issues]] [[Notes for the future]]
