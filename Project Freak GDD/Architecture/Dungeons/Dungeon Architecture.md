[[Dungeon]]s are semi procedural linked environments where the majority of combat is held. They're controlled by a tiered data structure, starting with the [[Dungeon Manager]] to the [[Dungeon Floor]]

---
## Dungeon Manager
The dungeon manager's responsibility is maintaining all dungeon data and initializing all dungeons. A dungeon only exists if its data is contained within the manager so that it can properly initialize and operate the basic functionality of the dungeon and work with the [[Save Manager]] to store current data of the dungeon.

### Dungeon Data
_ DungeonChapterData - The master list of DungeonChapterData. This holds the base initialization data for starting a dungeon as well as references to Dungeon SOs that contain the various tables for loot, enemies, and [[POI]].

### Current Dungeon Tracking
_ CurrentDungeon - When null it means the save manager does not need to worry about saving/loading dungeon data as the player has finished their latest dungeon and exists in a hub world of some kind.

### Realtime Systems
the manager handles most of the real time systems such as map generation and pulling for data lists as floors are being made

---
## Dungeon SO
This is a data structure of the individual dungeons as a whole. It mostly holds things like gameplay balance of each dungeon, floor selection, etc.

---
## Dungeon Floor
This is a scene file that contains different layouts for each floor of the dungeon


