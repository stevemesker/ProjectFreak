## Overview

`ChestContent` is responsible for managing the behavior of an interactive chest after it has been activated. It acts as the coordinator between the chest's opening sequence, Timeline system, and loot spawning system.

The script **does not** directly handle player interaction. Instead, it is triggered externally by the interaction system and then manages the opening process internally.

---

## Responsibilities

- Track whether the chest has already been opened.
- Play the chest opening sequence.
- Coordinate with the Timeline system.
- Wait for the opening animation to complete.
- Spawn all contained loot.
- Calculate landing positions for loot.
- Launch each item using the [[ArcMover]] system.

---

## System Dependencies

### Interaction System

The chest is opened externally by the interaction system.

See:

- [[Interaction Object]]

The interaction system determines **when** the chest should be activated.

---

### Timeline System

Once activated, the chest hands control over to the Timeline system.

See:

- [[Timeline Runner System]]

The timeline determines the sequence of events during the opening animation.

Example:

```text
Player Interacts
        │
        ▼
InteractObject
        │
        ▼
ChestContent.OpenChest()
        │
        ▼
TimelineRunner
        │
        ▼
Play Animation
Spawn Loot
Play SFX
Continue Timeline
```

The chest simply starts the timeline and waits for it to complete.

---

### ArcMover

Loot movement is delegated entirely to the `ArcMover` component.

See:

- [[ArcMover]]

The chest determines **where** loot should land.

`ArcMover` determines **how** it travels there.

---

# Opening Flow

## 1. Player Interaction

The player interacts with the chest.

`OpenChest()` is called.

---

## 2. Already Open Check

The chest first verifies it has not already been opened.

```text
Already Open?
│
├── Yes → Exit
└── No → Continue
```

This prevents duplicate loot or repeated animations.

---

## 3. Start Timeline

The chest marks itself as opened and begins the opening timeline.

```text
_isOpened = true

TimelineRunner.PlayTimeline()
```

From this point forward, the Timeline system controls the sequence.

---

## Timeline Synchronization

The chest animation uses Unity's `PlayableDirector`.

When the animation finishes, the chest automatically notifies the Timeline system.

```text
PlayableDirector
        │
Animation Finished
        │
        ▼
OnTimelineFinished()
        │
        ▼
TimelineRunner.ContinueTimeline()
```

This allows the Timeline to pause while waiting for animations without requiring hardcoded delays.

---

# Loot Spawning

Loot is not spawned immediately.

Instead, the Timeline calls `SpawnLootDrop()` at the desired point in the opening sequence.

This allows designers to synchronize:

- Animation
- Audio
- Visual effects
- Loot spawning

without changing code.

---

## Spawn Flow

```text
SpawnLootDrop()
        │
        ▼
Start Coroutine
        │
        ▼
Spawn Item
        │
        ▼
Initialize Item
        │
        ▼
Calculate Landing Position
        │
        ▼
Launch ArcMover
        │
        ▼
Wait
        │
        ▼
Repeat
```

Items are intentionally spawned one at a time instead of simultaneously.

This creates a more satisfying visual effect while allowing each item to use its own independent arc.

---

# Loot Distribution

The chest spreads loot across a configurable arc.

Two values control the spread.

| Variable | Purpose |
|----------|----------|
| `_LootArc` | Total angle available for loot distribution. |
| `_LootDistance` | Distance from the chest each item attempts to land. |

Example:

```text
      90°

      X
   X  X  X
      ▲
    Chest
```

---

## Distribution Order

Items are intentionally spawned from the center outward.

Order:

```text
1
```

```text
2

 X
  \
  Chest
  /
 X
```

```text
3

   X
   │
X Chest
```

```text
5

X  X  X  X  X
 \ |  | | /
    Chest
```

The first item always lands directly in front of the chest.

Additional items alternate left and right, gradually filling the available arc.

This creates a visually balanced loot spread while keeping the first item in the player's immediate view.

---

# Loot Initialization

Each loot object is responsible only for representing the item.

The chest performs the following initialization:

1. Instantiate the loot prefab.
2. Assign the contained `ItemSO`.
3. Calculate its landing position.
4. Launch the item using `ArcMover`.

The chest does **not** animate the movement itself.

---

# Landing Position Calculation

The chest calculates the desired landing position using:

- Chest forward direction.
- Configured loot spread angle.
- Configured drop distance.
- Item index within the loot list.

This creates deterministic and evenly distributed loot placement.

Future versions will extend this calculation to validate landing positions using raycasts before launching the item.

---

# Inspector Variables

## Chest State

| Variable | Purpose |
|----------|----------|
| `_Content` | List of all items contained within the chest. |
| `_isOpened` | Prevents the chest from opening multiple times. |

---

## References

| Variable | Purpose |
|----------|----------|
| `_TLine` | Timeline controlling the opening sequence. |
| `director` | Unity PlayableDirector used for chest animations. |
| `_itemHolderPrefab` | Prefab instantiated for each loot drop. |

---

## Loot Settings

| Variable | Purpose |
|----------|----------|
| `_LootArc` | Total spread angle of the loot fan. |
| `_LootDistance` | Distance each item attempts to travel. |
| `_maxLootHeight` | Reserved for future landing validation (maximum vertical drop). |
| `_LootDisperseWaitTime` | Delay between spawning consecutive items. |
| `_LootArcHeight` | Height of each loot arc. |
| `_LootArcDuration` | Time required for each arc movement. |

---

# Design Philosophy

`ChestContent` acts as a coordinator rather than a monolithic system.

It intentionally delegates specialized tasks:

- **Interaction** → [[Interaction Object]]
- **Event sequencing** → [[Timeline Runner System]]
- **Movement animation** → [[ArcMover]]
- **Item behavior** → `ItemDrop`

Because of this separation, each system has a single responsibility and can evolve independently.

For example:

- Enemies can reuse the loot spawning logic without using chest animations.
- Other objects can reuse `ArcMover` without any knowledge of loot.
- Different interaction types can trigger the same chest behavior without modifying the chest itself.

This modular architecture keeps the chest focused on one responsibility: **coordinating the opening experience and spawning its contents.**