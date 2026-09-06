Dungeons are a web of interconnected floors represented as a map of nodes. The player will have access to this map during the entirety of the dungeon, allowing them to plan out their ideal route at any time while on one of the [[Dungeon Floor]]s.

Once their path is chosen, player's will have to navigate the [[Dungeon Floor]] to find the exit door that matches the symbol on their map they're searching for. They'll continue to do this every floor until they reach the [[Boss Room]] at the end of the dungeon which usually marks the end of a chapter.

*-There may need to be some sort of timing mechanic similar to FTL so that the player's can't just go back to all rooms to grind levels and loot*

*-See* [[Dungeon Architecture]] *for more info*

---
**Typical Flow**
Player has selected their [[Shade]]/s they are taking into the dungeon and enters via the [[Dungeon Door]]. They are brought to the entrance of the dungeon and are immediately presented with several doors. the player checks their [[Dungeon Map Manager]] and sees the path to the right is the most dangerous but has rare loot right away and is connected to a rest spot. They decide to risk it and take the door with the same markings as the map.

The player proceeds to enter the floor and is immediately greeted with a monster spawning arena [[POI]] and has to summon their shade to fight while they take a position in the back corner and lob bombs. Eventually they succeed in clearing out the [[POI]] and that triggers a chest to spawn with loot. They continue through the room, stopping to blow up a wall on the side of a building to ambush some smaller enemies and take some of their runes. All of this combat has taken more out of their shade than they would have hoped. When they saw the first door is not the one they're looking for but does, on closer inspection of the map, lead to an easier line. They decide to take it for now instead of trying to clear out the next room in hopes of finding the door on their original path.

The next few rooms contain some healing [[POI]]s as well as a hidden treasure vault so they're feeling more confident. They decide to get back on the harder track and fight a [[Mini Boss]] floor. They do succeed in killing the boss and are rewarded with a rare rifle and they free an npc that joins [[The Black Lantern]] as an upgraded materials vendor.

The player's [[Shade]] is severely damaged by the end of the dungeon. It is hardly a surprise when their shade quickly falls to the [[Dungeon Boss]] and the player is sent back to their [[Hub Scene]]  at [[The Black Lantern]]. Their [[Shade]] is down a life but still has plenty remaining and the new npc merchant means huge upgrades. The next time they face the boss, they will succeed.

# Dungeon System

## Overview

Dungeons are the primary gameplay spaces where the player explores, fights enemies, discovers rewards, and progresses toward a final objective.

Rather than generating an entire dungeon from scratch, the dungeon system uses a **hybrid approach** that combines hand-authored level structure with procedurally selected Points of Interest ([[POI]]s).

The overall dungeon structure is intentionally controlled by the designer. The game determines which rooms the player will encounter, how those rooms are connected, and which POIs are placed within them. This allows the dungeon to retain a deliberate sense of pacing and progression while still providing variation between runs.

The dungeon system is built around several layers:

1. **Dungeon** — Defines the overall dungeon and its floors
2. **Floor** — Represents an individual level within the dungeon
3. **Dungeon Map** — Defines the progression and connections between rooms
4. **Point of Interest (POI)** — The gameplay content occupying a room or section of a room
5. **Doors** — Connect rooms together and control progression between them

The dungeon system therefore separates **where the player goes** from **what the player finds there**.

---

## Dungeon Structure

A dungeon is composed of one or more floors.

Each floor represents a self-contained gameplay space and contains a collection of rooms connected through doors.

A simplified hierarchy is:

```text
Dungeon
│
├── Floor
│   ├── Entrance
│   ├── Room
│   ├── Room
│   ├── Room
│   └── Boss / Exit
│
├── Floor
│   ├── Entrance
│   ├── Room
│   ├── Room
│   └── Boss / Exit
│
└── ...
```

The exact number and arrangement of rooms can vary depending on the dungeon.

The dungeon itself is responsible for maintaining the overall progression, while individual floors handle the physical spaces the player explores.

---

## Dungeon as the Source of Truth

The **Dungeon Manager** acts as the central authority for dungeon runtime information.

Rather than individual rooms independently deciding what should happen next, they communicate with the Dungeon Manager when they need information about the current dungeon.

This allows the dungeon to maintain a consistent understanding of:

* Current dungeon
* Current floor
* Floor progression
* Room/node information
* Available POIs
* POI requirements
* Door connections
* Dungeon progression
* Room transitions
* Other dungeon-wide gameplay data

This separation is important because individual rooms should not need to understand the entire dungeon.

A room should primarily concern itself with:

> "What am I?"

While the dungeon system concerns itself with:

> "Where does the player go next, and what should be there?"

---

# Dungeon Map

The dungeon map represents the **logical structure of the dungeon** rather than its physical geometry.

Each room is represented by a node.

Nodes contain information about:

* Room identity
* Room type
* Room connections
* Floor information
* Difficulty/path information
* Other metadata needed for dungeon progression

Connections between nodes represent doors or other transitions between rooms.

The map can therefore be thought of as a graph:

```text
             [Room]
                |
[Room] ---- [Room] ---- [Room]
                |
             [Room]
```

The player does not necessarily experience this graph directly as a literal physical map. Instead, it determines the possible progression through the dungeon.

This separation allows the logical dungeon structure to exist independently from the physical scenes used to represent it.

---

## Dungeon Map Philosophy

The dungeon map is designed to provide the player with **meaningful choices without requiring completely random level generation**.

Different paths can represent different levels of risk and reward.

For example:

```text
                 [Treasure]
                    /
[Entrance] ---- [Combat] ---- [Elite]
                    \
                 [Healing]
```

The player may therefore make decisions based on the information available to them.

A path may be:

* Safer but less rewarding
* More dangerous but more rewarding
* Focused on combat
* Focused on recovery
* Focused on exploration
* Required for progression

The goal is to make dungeon navigation itself part of the gameplay.

---

# Room Types

Rooms are categorized according to their role within the dungeon.

Common room types may include:

* **Entrance**
* **Combat**
* **Treasure**
* **Healing**
* **Event**
* **Elite**
* **Boss**
* **Special**
* **Exit**

These categories describe the **purpose of a room**, rather than necessarily describing its physical appearance.

A combat room could contain a small arena, a ruined courtyard, a series of corridors, or another appropriate POI.

This allows the same logical room type to support many different visual and gameplay implementations.

---

# Points of Interest

A **Point of Interest ([[POI]])** is the physical gameplay content placed into a dungeon room.

POIs are the primary building blocks used to populate dungeon spaces.

A POI may represent:

* Combat encounters
* Treasure rooms
* Healing areas
* Boss arenas
* Environmental challenges
* Shops or merchants
* Puzzle areas
* Special encounters
* Large compound structures
* Other unique gameplay spaces

A POI is more than simply a visual prefab. It represents a gameplay space with defined requirements and content.

A POI can contain things such as:

* Environment geometry
* Enemy spawn points
* Treasure spawn points
* Player entry points
* Interactive objects
* Gameplay-specific spawn locations
* Tags describing its intended use
* other [[POI]]s

---

# POI Selection

POIs are selected dynamically based on the requirements of the room being populated.

The dungeon system does not simply choose a random POI.

Instead, a room provides requirements such as:

* Desired POI type
* Required tags
* Required size
* Floor-specific restrictions
* Other dungeon-specific constraints

The POI system then searches the available POI pool for an appropriate candidate.

This allows the same room type to produce different physical spaces between runs.

For example:

```text
Room Requirement:
    Type = Combat
    Size = Medium
    Tags = Indoor, EnemyHeavy

Possible POIs:

    Crypt_A
    Barracks_A
    Prison_A
    RuinedHall_A
```

The player still experiences a combat room, but the physical space can vary.

---

# POI Tags

POIs use tags to describe characteristics beyond their primary type.

Tags allow the dungeon system to ask questions such as:

> "Give me a POI that can support this particular gameplay requirement."

Rather than creating a large number of highly specific POI types, tags allow POIs to satisfy multiple requirements.

For example, a POI could have:

```text
Type:
    Combat

Tags:
    EnemyHeavy
    Indoor
    LargeEncounter
```

Another POI could be:

```text
Type:
    Combat

Tags:
    EnemyHeavy
    Outdoor
    Ambush
```

Both are combat POIs, but they can be selected for different circumstances.

Tags therefore act as a flexible filtering system for POI selection.

---

# POI Size

POIs are assigned a standardized size category.

The current size progression is:

| Size   | Approximate Footprint |
| ------ | --------------------: |
| Tiny   |           1 × 1 cells |
| Small  |           2 × 2 cells |
| Medium |           3 × 3 cells |
| Large  |           4 × 4 cells |
| Huge   |           6 × 6 cells |

The actual physical dimensions of a cell can be adjusted independently of the logical size system, but in general is 4x4 m.

The purpose of the size system is to allow a dungeon space to request a POI based on the amount of physical space available.

For example:

```text
Room Space:
    4 × 4 cells

Valid POI:
    Large

Invalid POI:
    Huge
```

Size therefore acts as another constraint during POI selection.

---

# Compound POIs

Larger POIs can contain multiple smaller POIs or gameplay spaces.

This allows a large structure to be treated as a single POI while still containing several distinct gameplay elements.

For example:

```text
Large POI
│
├── Entrance
├── Combat Area
├── Treasure Room
└── Exit
```

This provides a way to create more complex environments without requiring every individual space to become its own dungeon-map node.

A large POI can therefore function as a **micro-dungeon** inside the larger dungeon structure.

This distinction is useful because not every physical room needs to represent a meaningful choice on the dungeon map.

---

# [[Dungeon Floor]]s

A dungeon is divided into floors to create larger-scale progression.

Each floor can have its own:

* Environment
* Difficulty
* POI pool
* Enemy pool
* Loot pool
* Room distribution
* Special rules
* Boss encounters
* Visual theme

This allows progression to occur at multiple levels.

---

# Door System

Doors are the physical representation of connections between dungeon rooms.

A door is associated with a destination node and provides the transition between the current room and the next room.

Conceptually:

```text
Current Room
     |
   Door
     |
     ↓
Next Room
```

Doors therefore bridge the gap between the **logical dungeon map** and the **physical dungeon environment**.

The map determines:

> "This room connects to that room."

The door represents:

> "This physical doorway takes the player there."

---

# Door Requirements

Dungeon floors are designed around the expectation that rooms can have multiple possible connections.

A floor therefore needs to account for:

* Doors leading to previously known rooms
* Doors leading to unexplored rooms
* Optional connections
* Dead ends
* Required progression connections

The door system is responsible for presenting the appropriate physical connections once the dungeon structure has been determined.

This keeps room construction independent from the higher-level dungeon graph.

---

# Dungeon Generation Flow

At a high level, creating a dungeon follows this sequence:

```text
Dungeon Selected
       ↓
Dungeon Data Loaded
       ↓
Floor Selected
       ↓
Dungeon Map Generated
       ↓
Room Nodes Established
       ↓
Room Types Determined
       ↓
POI Requirements Determined
       ↓
POIs Selected
       ↓
Physical Floor Loaded
       ↓
POIs Spawned
       ↓
Doors Connected
       ↓
Player Enters Dungeon
```

The important distinction is that **logical generation occurs before physical population**.

The system first determines what the dungeon should contain, then constructs the physical environment that represents that information.

---

# Separation of Responsibilities

The dungeon system intentionally separates responsibilities between its major components.

| System             | Responsibility                          |
| ------------------ | --------------------------------------- |
| Dungeon            | Defines the overall dungeon             |
| Dungeon Floor      | Defines a floor and its rules           |
| Dungeon Manager    | Controls runtime dungeon state          |
| Dungeon Map        | Defines logical progression             |
| Dungeon Map Node   | Represents a room and its connections   |
| Room / Floor Scene | Provides the physical environment       |
| POI                | Provides physical gameplay content      |
| POI Tags           | Describe POI characteristics            |
| POI Size           | Defines the physical scale of a POI     |
| Door               | Connects physical rooms                 |
| Spawn Points       | Define where gameplay content is placed |

This separation allows individual systems to change without requiring the entire dungeon architecture to be rewritten.

---

# Design Goals

The dungeon system is designed around several core goals.

## Controlled Variety

Dungeons should feel different between runs without becoming completely unpredictable.

The designer controls the overall structure and available content while the game controls specific selections.

---

## Meaningful Choices

Dungeon navigation should give the player reasons to choose one path over another.

Different paths should communicate different:

* Risks
* Rewards
* Difficulty levels
* Recovery opportunities
* Gameplay opportunities

A choice should ideally have a meaningful consequence.

---

## Strong Authorial Control

Procedural systems should serve level design rather than replace it.

The system should make it possible to define rules such as:

* Which room types can appear
* How frequently they appear
* Which POIs are appropriate
* Which POIs are allowed on a particular floor
* How difficult a path should be
* Where special encounters can occur

This prevents procedural generation from producing technically valid but poorly designed encounters.

Procedural generation is most useful when it is constrained by meaningful design rules rather than relying purely on randomness. Research into procedural dungeon generation similarly emphasizes designer control, gameplay constraints, and navigability as important parts of successful generation.

---

## Reusable Content

A POI should be usable in multiple situations whenever possible.

A single POI should not need to exist exclusively for one specific dungeon.

Instead, its type, size, and tags allow the dungeon system to determine where it is appropriate.

This increases the value of each piece of authored content.

---

## Separation of Logic and Presentation

The dungeon map describes the **logical experience**.

The POI describes the **physical experience**.

These should remain separate.

For example:

```text
Logical:
    "The player encounters a high-difficulty combat room."

Physical:
    "The player enters an abandoned barracks containing
     several enemy spawn locations."
```

Both represent the same gameplay requirement while allowing the physical implementation to vary.

---

# Procedural Philosophy

The dungeon system is not intended to create infinite random layouts simply for the sake of randomness.

Its purpose is to create **controlled variation**.

The ideal result is:

> **Familiar structure + unpredictable content**

The player should understand the general rules of the dungeon while still being unable to perfectly predict what they will encounter.

This approach also allows authored content to remain meaningful. Hand-designed POIs can provide intentional gameplay experiences while the dungeon system determines when and where those experiences occur.

This is particularly valuable because completely random generation can easily produce layouts that are technically functional but lack intentional pacing, readable progression, or interesting decisions. Hybrid approaches are commonly used to preserve authored structure while gaining the replayability benefits of procedural variation.

---

# Difficulty and Path Progression

The dungeon map can communicate relative difficulty through its path structure.

For example:

```text
                 Easy
                  ↓
Entrance ─────── Normal ─────── Boss
                  ↓
                Hard
```

Or:

```text
                    Treasure
                       ↑
Entrance → Easy → Normal → Elite → Boss
                       ↓
                    Healing
```

The exact implementation can change, but the important design principle is that **position on the dungeon map can communicate expected difficulty and reward**.

This allows players to make informed decisions rather than selecting paths randomly.

---

# Future Expansion

The dungeon system should remain flexible enough to support additional content without requiring major architectural changes.

Potential future additions include:

* New room types
* New POI types
* Additional POI tags
* New POI sizes
* Special dungeon rules
* Environmental hazards
* Puzzle rooms
* Secret rooms
* Elite encounters
* Shops
* NPC encounters
* Alternate exits
* Locked paths
* Keys and gates
* Branching objectives
* Floor-specific modifiers
* Dungeon-wide modifiers
* Rare or unique POIs
* Boss-specific arenas
* Special event rooms

The goal is for these systems to be additions to the existing framework rather than exceptions that require completely separate dungeon logic.

---

# Design Principle

The dungeon system can ultimately be summarized as:

> **The dungeon decides where the player goes.**
>
> **The room decides what kind of experience the player should have.**
>
> **The POI decides what that experience physically looks like.**
>
> **The dungeon manager keeps everything coordinated.**

This separation allows the dungeon to remain highly controllable while still providing enough variation to keep exploration fresh.
