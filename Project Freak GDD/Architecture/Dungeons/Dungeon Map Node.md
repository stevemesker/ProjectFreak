[[Dungeon Map Node]]s track the status and connections of other nodes.

Node ID -
All floor nodes have a unique ID that is directly linked to their position in the node list for the dungeon manager. This is used for quickly finding connection nodes as well as ensuring loot chest states are saved as the ID will be included in its own unique ID

Node Type - 
This is the type of [[Dungeon Floor]] the node represents that controls loot and encounter types

Node Connection - 
What rooms this node shares a connection to. This data is pulled when setting doors and what dungeon levels they connect to

Floor Name -
holds the data for which floor scene name is going to be loaded when player enters the floor that is represented by this node. This ensures that if the player backtracks they will always enter the same dungeon floor