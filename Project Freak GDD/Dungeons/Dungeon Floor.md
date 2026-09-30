[[Dungeon Floor]]s are where the play spends the majority of their time in [[Project Freak]]. They contain enemies, traps, and loot the player can collect in order to defeat the boss at the end of the dungeon.

---
**Dungeon Room**
[[Dungeon Floor]]s are premade scene layouts that belong to a dungeon. Each node on the dungeon map is one floor. When the player enters a node for the first time, the [[Dungeon Manager]] picks a random layout from that dungeon's floor list. This lets me make well crafted and interesting rooms that still have some variance to keep the player entertained.

The entrance and boss nodes always use their own dedicated scenes.

*Current behavior:* layouts are picked at random, not by how many connections the node has. If a floor has more door spots than the node has connections, the extra spots get a "null door" (a blocked door). Layouts should have enough door spots for the most connected node (up to 3 by default).

---
**Point Of Interests ([[POI]])**
small modular prefabs that add variability to dungeons. POI spawners placed in the floor pick a POI that fits the node's type and the space available. See [[POI System]].

---
**[[Dungeon Door]]**
Floors are connected via [[Dungeon Door]]s in a web. Doors are color coded to show what rooms they lead to, matching the node colors on the map.
