This section describes the complete process for creating a new dungeon from scratch.

The goal is to provide a repeatable workflow that can be followed whenever a new dungeon is added to the game.

A dungeon consists of several interconnected pieces of data and content:

```text
DungeonSO
    ↓
Dungeon Manager
    ↓
Dungeon Floors
    ↓
Dungeon Map
    ↓
Dungeon Floor Scenes
    ↓
POI Requirements
    ↓
POI Prefabs
    ↓
Enemies / Loot / Encounters
```

A dungeon should be built from the top down. Establish the dungeon's overall rules and structure first, then create the physical spaces and POIs that satisfy those rules.

---

# Dungeon Creation Workflow

The general process for creating a dungeon is:

1. Create the Dungeon ScriptableObject
2. Configure the dungeon's basic settings
3. Add the DungeonSO to the Dungeon Manager
4. Create and configure the dungeon's floors
5. Define floor-specific rules and content
6. Create the dungeon map configuration
7. Create the physical floor scene
8. Configure the floor's room/POI spaces
9. Create the required POIs
10. Configure POI types, sizes, and tags
11. Add POI spawn points
12. Add doors and door connection points
13. Configure enemies, loot, and other encounter content
14. Test POI selection
15. Test room connections and progression
16. Playtest the complete dungeon
17. Balance and iterate

The exact order may occasionally change during development, but this should be the default workflow.

---

# 1. Create the DungeonSO

The first step is creating the ScriptableObject that represents the new dungeon.

The DungeonSO contains the high-level configuration for the dungeon and acts as the data definition for the dungeon.

Create a new DungeonSO using the appropriate Unity asset creation menu.

Example:

```text
Assets
└── Game
    └── Dungeons
        └── [Dungeon Name]
            └── Dungeon_[DungeonName].asset
```

Use a consistent naming convention for dungeon assets.

For example:

```text
Dungeon_ForestTemple
Dungeon_AbandonedMine
Dungeon_AncientRuins
```

The DungeonSO should contain the information necessary for the Dungeon Manager to understand what content belongs to the dungeon.

---

# 2. Configure the DungeonSO

Once the DungeonSO has been created, configure its high-level settings.

These settings may include:

* Dungeon name
* Floors
* Floor order
* Enemy tables
* Loot tables
* Trap tables
* Other dungeon-wide configuration
* Dungeon-specific rules

The DungeonSO should contain information that applies to the dungeon as a whole.

Information that only applies to a specific floor should generally remain associated with that floor instead.

### Example

```text
Dungeon
    Name: Ancient Temple

    Floors:
        Floor 1 - Temple Entrance
        Floor 2 - Inner Temple
        Floor 3 - Temple Depths

    Enemy Table:
        Temple Enemies

    Loot Table:
        Temple Loot

    Trap Table:
        Temple Traps
```

---

# 3. Add the DungeonSO to the Dungeon Manager

After creating the DungeonSO, it must be registered with the Dungeon Manager.

The Dungeon Manager maintains the collection of available dungeons and uses this data to determine which dungeon should be loaded.

Add the newly created DungeonSO to the appropriate Dungeon Manager list.

```text
Dungeon Manager
└── Dungeons
    ├── Forest Temple
    ├── Abandoned Mine
    ├── Ancient Ruins
    └── New Dungeon
```

If the DungeonSO is not registered with the Dungeon Manager, the game will not be able to locate it through the normal dungeon-selection process.

### Checklist

* [ ] DungeonSO created
* [ ] DungeonSO configured
* [ ] DungeonSO added to Dungeon Manager
* [ ] Dungeon appears in available dungeon data

---

# 4. Create the Dungeon Floors

A dungeon is made up of one or more floors.

Determine how many floors the dungeon requires and create/configure the appropriate floor data.

For each floor, determine:

* Floor name
* Floor order
* Difficulty
* Environment/theme
* Available room types
* Available POIs
* Enemy pool
* Loot pool
* Special rules
* Boss or exit requirements

For example:

```text
Ancient Temple
│
├── Floor 1
│   └── Temple Entrance
│
├── Floor 2
│   └── Inner Temple
│
└── Floor 3
    └── Temple Depths
```

The floors should increase in difficulty or otherwise provide a sense of progression unless the dungeon intentionally uses a different structure.

---

# 5. Configure Floor-Specific Content

Each floor can have its own collection of content.

This is where the dungeon begins to become more specific.

For example:

### Floor 1

```text
Enemy Pool:
    Slime
    Goblin
    Bat

POI Types:
    Combat
    Treasure
    Healing

Difficulty:
    Low
```

### Floor 3

```text
Enemy Pool:
    Knight
    Mage
    Elite Guardian

POI Types:
    Combat
    Elite
    Treasure
    Healing
    Boss

Difficulty:
    High
```

This allows multiple floors to use the same overall dungeon framework while providing different gameplay experiences.

---

# 6. Define the Dungeon Map

The dungeon map determines the logical progression of the floor.

Before building the physical room, determine what the player should encounter.

Define:

* Entrance
* Room nodes
* Room types
* Connections
* Branches
* Difficulty progression
* Special nodes
* Boss node
* Exit node

Example:

```text
                    [Treasure]
                       |
[Entrance] → [Combat] → [Combat] → [Boss]
                       |
                    [Healing]
```

The map should be considered the **gameplay plan** for the floor.

The physical level should then be built to satisfy this plan.

---

# 7. Create the Dungeon Floor Scene

Once the logical structure is established, create the physical scene that represents the floor.

The floor scene contains the actual environment the player will explore.

This includes:

* Terrain
* Walls
* Floors
* Decorations
* Lighting
* Navigation
* Collision
* Room spaces
* POI placement areas
* Doors
* Other environmental elements

The floor scene should provide enough physical space for the POIs that the dungeon system is expected to place within it.

---

# 8. Define POI Spaces

Each location where a POI can be placed needs to define its requirements.

A POI space should communicate things such as:

* Required size
* Desired type
* Required tags
* Available room type
* Other placement restrictions

For example:

```text
POI Space

Type:
    Combat

Size:
    Large

Required Tags:
    Indoor
    EnemyHeavy
```

The POI system can then search for a POI that satisfies these requirements.

This allows the level designer to control what kinds of content can appear in each location without explicitly selecting the exact POI.

---

# 9. Create the POI Prefab

The next step is creating the physical POI that can be spawned into the dungeon.

A POI prefab should represent a complete, reusable gameplay space.

Depending on the POI, it may contain:

```text
POI
│
├── Environment
├── Spawn Points
│   ├── Enemy
│   ├── Treasure
│   └── Other
│
├── Gameplay Objects
└── Other POI-specific content
```

The POI prefab should be self-contained enough that it can be dropped into any compatible dungeon space.

---

# 10. Configure POI Type

Every POI should have a primary type.

The type describes the primary gameplay purpose of the POI.

Examples:

```text
Combat
Treasure
Healing
Boss
Event
Special
```

The type should answer:

> "What kind of room is this?"

It should not attempt to describe every characteristic of the POI.

Additional characteristics should be represented using tags.

---

# 11. Configure POI Size

Assign the appropriate size category to the POI.

Current size conventions:

| Size   | Footprint |
| ------ | --------: |
| Tiny   |     1 × 1 |
| Small  |     2 × 2 |
| Medium |     4 × 4 |
| Large  |     8 × 8 |
| Huge   |   12 × 12 |

These values represent logical cell dimensions rather than necessarily being permanent physical measurements.

The size should correspond to the amount of space the POI actually requires.

### Example

A small treasure room:

```text
Size:
    Small

Footprint:
    2 × 2
```

A large boss arena:

```text
Size:
    Large

Footprint:
    8 × 8
```

The POI's size must match the space it is intended to occupy.

---

# 12. Configure POI Tags

Add tags that describe the characteristics of the POI.

Tags should be used when a POI needs to satisfy requirements beyond its basic type and size.

Example:

```text
Type:
    Combat

Size:
    Medium

Tags:
    Indoor
    EnemyHeavy
    Ambush
```

Another POI might be:

```text
Type:
    Combat

Size:
    Medium

Tags:
    Outdoor
    Open
    EnemyLight
```

Both are Combat POIs, but their tags allow the dungeon system to distinguish between them.

### Tag Guidelines

Tags should describe **meaningful gameplay or design characteristics**.

Good tags:

```text
Indoor
Outdoor
EnemyHeavy
Elite
Open
Tight
Ambush
Puzzle
MultiEncounter
```

Avoid creating tags for information that is already represented elsewhere.

For example, avoid:

```text
Combat
```

as a tag if `Combat` is already the POI's primary type.

---

# 13. Add POI Spawn Points

POIs should contain spawn points for gameplay content.

Common spawn point categories include:

* Player spawn
* Enemy spawn
* Treasure spawn
* Item spawn
* NPC spawn
* Special encounter spawn

Spawn points should be positioned intentionally within the POI.

For example:

```text
Combat Arena
│
├── Player Spawn
│
├── Enemy Spawn
│   ├── Spawn 01
│   ├── Spawn 02
│   ├── Spawn 03
│   └── Spawn 04
│
└── Reward Spawn
```

The POI defines **where** content can appear.

The dungeon/gameplay systems determine **what** content appears there.

This keeps the physical layout independent from the specific enemy or reward selected.

---

# 14. Configure Doors

Each dungeon floor needs physical door locations that correspond to possible connections between rooms.

A floor should provide the required door positions for the rooms it contains.

Doors should be configured so that they can receive the destination node information from the dungeon system.

Conceptually:

```text
Room A
  |
Door
  |
  ↓
Room B
```

The door should not need to know the entire dungeon structure.

It only needs to know:

> "Which node am I currently connected to?"

The Dungeon Manager handles the larger progression logic.

---

# 15. Configure Enemy Content

Once the POI itself is functional, configure the enemies that can appear within it.

Enemy selection should generally be controlled by the dungeon/floor's enemy tables rather than hardcoded into individual POIs.

For example:

```text
Floor 1 Enemy Pool
    Goblin
    Bat
    Slime
```

A Combat POI may contain six enemy spawn points, but the actual enemies occupying those points can be selected dynamically.

This allows the same POI to support multiple encounters.

---

# 16. Configure Loot and Rewards

Loot should be handled similarly.

POIs provide locations where rewards can appear, while dungeon/floor loot tables determine what rewards are available.

For example:

```text
Treasure POI
    ↓
Treasure Spawn Point
    ↓
Dungeon Loot Table
    ↓
Selected Reward
```

This prevents the physical POI from becoming tightly coupled to a particular reward.

A treasure chest in the same physical POI could therefore contain different rewards depending on the dungeon, floor, difficulty, or other gameplay rules.

---

# 17. Add the POI to the Appropriate Pool

Once the POI has been completely configured, make sure it is available to the dungeon system.

The POI must be included in the appropriate pool/list so that the Dungeon Manager can find it when a room requests one.

Verify that:

* The POI is registered
* Its type is correct
* Its size is correct
* Its tags are correct
* The appropriate floor can access it
* Its prefab reference is valid

If the POI is not available to the appropriate pool, the dungeon may be unable to satisfy a room's requirements.

---

# 18. Verify POI Compatibility

Before testing the entire dungeon, verify that every POI space has at least one compatible POI.

For every room requirement, ask:

```text
Room Requirement
    ↓
Required Type
    +
Required Size
    +
Required Tags
    ↓
Does a compatible POI exist?
```

For example:

```text
Required:
    Type = Combat
    Size = Large
    Tag = EnemyHeavy

Available:
    Combat_A
        Medium
        EnemyHeavy

    Combat_B
        Large
        EnemyHeavy

    Combat_C
        Large
        Outdoor
```

`Combat_B` is the only guaranteed match.

If no POI satisfies the requirements, the dungeon may fail to populate that room.

---

# 19. Test the Dungeon

Once all components have been created, test the dungeon from the beginning rather than testing individual pieces in isolation.

Test the complete flow:

```text
Dungeon Selected
       ↓
Dungeon Loaded
       ↓
Floor Loaded
       ↓
Map Generated
       ↓
Room Populated
       ↓
POI Selected
       ↓
POI Spawned
       ↓
Doors Connected
       ↓
Player Enters Room
       ↓
Player Completes Room
       ↓
Next Room
```

Verify that each stage behaves correctly.

---

# 20. Dungeon Testing Checklist

### Dungeon Data

* [ ] DungeonSO exists
* [ ] DungeonSO is registered with Dungeon Manager
* [ ] Dungeon has the correct floors
* [ ] Dungeon-wide data is configured

### Floor Data

* [ ] Floors are in the correct order
* [ ] Floor-specific data is configured
* [ ] Enemy pools are configured
* [ ] Loot pools are configured
* [ ] Required room types are available

### Dungeon Map

* [ ] Entrance exists
* [ ] Room nodes generate correctly
* [ ] Connections are valid
* [ ] Boss/exit progression is valid
* [ ] No unintended dead ends exist

### Floor Scene

* [ ] Physical level exists
* [ ] Room spaces are correctly positioned
* [ ] POI requirements are configured
* [ ] Door locations are configured
* [ ] Collision works
* [ ] Navigation works
* [ ] Lighting/environment are complete

### POIs

* [ ] Every required POI exists
* [ ] POI prefab is configured
* [ ] Type is correct
* [ ] Size is correct
* [ ] Tags are correct
* [ ] Spawn points are correctly positioned
* [ ] POI is registered in the appropriate pool
* [ ] POI fits within its intended space

### Gameplay

* [ ] Enemies spawn correctly
* [ ] Rewards spawn correctly
* [ ] Doors connect correctly
* [ ] Room completion works
* [ ] Room transitions work
* [ ] Player progression works from entrance to exit

---

# Recommended Asset Organization

A consistent folder structure will make adding future dungeons significantly easier.

A dungeon could be organized approximately like this:

```text
Assets
└── Game
    └── Dungeons
        └── [Dungeon Name]
            │
            ├── Data
            │   ├── Dungeon_[Name].asset
            │   ├── Floor_[Name].asset
            │   └── Other Dungeon Data
            │
            ├── Scenes
            │   ├── Floor_01_[Name].unity
            │   ├── Floor_02_[Name].unity
            │   └── Floor_03_[Name].unity
            │
            ├── POIs
            │   ├── Combat
            │   ├── Treasure
            │   ├── Healing
            │   ├── Elite
            │   └── Boss
            │
            ├── Prefabs
            │   ├── Doors
            │   ├── Props
            │   └── Gameplay
            │
            └── Art
                ├── Environment
                ├── Materials
                └── Textures
```

The exact folder structure can evolve, but consistency is more important than the specific structure.

---

# Creating a New Dungeon — Quick Reference

For future use, the entire process can be reduced to the following checklist:

```text
DUNGEON
[ ] Create DungeonSO
[ ] Configure DungeonSO
[ ] Add DungeonSO to Dungeon Manager

FLOORS
[ ] Create floor data
[ ] Configure floor progression
[ ] Configure enemy pool
[ ] Configure loot pool
[ ] Configure floor-specific rules

MAP
[ ] Define entrance
[ ] Define room nodes
[ ] Define room types
[ ] Define connections
[ ] Define boss/exit

SCENE
[ ] Create floor scene
[ ] Build environment
[ ] Create room spaces
[ ] Configure POI requirements
[ ] Add door locations

POIs
[ ] Create POI prefab
[ ] Configure POI type
[ ] Configure POI size
[ ] Configure POI tags
[ ] Add spawn points
[ ] Add gameplay objects
[ ] Add POI to appropriate pool

CONTENT
[ ] Configure enemies
[ ] Configure loot
[ ] Configure special encounters

TESTING
[ ] Verify every room has a compatible POI
[ ] Test POI spawning
[ ] Test doors
[ ] Test room transitions
[ ] Test dungeon progression
[ ] Playtest complete dungeon
[ ] Balance difficulty and rewards
```

---

# Important Design Rule

When creating dungeon content, remember the distinction between **data, structure, and physical content**.

```text
DungeonSO
    ↓
"What dungeon am I creating?"

Dungeon Map
    ↓
"Where can the player go?"

Room Requirements
    ↓
"What kind of experience belongs here?"

POI
    ↓
"What physical space provides that experience?"

Spawn Points
    ↓
"Where does the gameplay happen?"

Enemy / Loot Systems
    ↓
"What actual content appears?"
```

Keeping these responsibilities separate is what allows the dungeon system to remain flexible.

A new dungeon should therefore be created primarily by **assembling existing systems and creating new content that conforms to their rules**, rather than by adding dungeon-specific logic to the underlying systems.
