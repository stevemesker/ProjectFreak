# Overview

The Timeline Runner is a lightweight, data-driven sequencing system used to execute a series of events in order with configurable timing. It is intended for gameplay sequences that require multiple actions to occur one after another without requiring dedicated scripts for every scenario.

### Example Use Cases

- Treasure chests
- Doors
- NPC interactions
- Cutscenes
- Boss introductions
- Tutorials
- Environmental events
- Scripted gameplay moments

The system is intentionally generic so any UnityEvent can participate in a timeline.

---

# Architecture

The system consists of two classes:

- **TimelineRunner** *(MonoBehaviour)*
- **TimelineEventTrack** *(Serializable Data Class)*

`TimelineRunner` owns the execution logic.

`TimelineEventTrack` simply stores the data for a single timeline event.

```
TimelineRunner
│
├── List<TimelineEventTrack>
│
├── Event 1
├── Event 2
├── Event 3
└── ...
```

---

# TimelineEventTrack

Each track represents one step in the timeline.

## Variables

### Start Delay

Time (seconds) to wait before invoking the UnityEvent.

---

### Wait For Continue

If enabled, the timeline pauses after invoking the event until `ContinueTimeline()` is called.

If disabled, the timeline simply waits for **End Delay** before moving to the next track.

---

### End Delay

Time (seconds) to wait after the event before continuing.

Ignored when **Wait For Continue** is enabled.

---

### Unity Event

The event(s) executed for this timeline step.

A single track can execute multiple UnityEvents simultaneously.

---

# TimelineRunner

The TimelineRunner executes every TimelineEventTrack sequentially.

## Execution Flow

```
For Each Track

↓

Wait Start Delay

↓

Invoke UnityEvent

↓

Wait For Continue?

YES
    Wait until ContinueTimeline()

NO
    Wait End Delay

↓

Next Track
```

Once every track has finished, the timeline invokes the completion event.

---

# Public Functions

## PlayTimeline()

Starts the timeline from the beginning.

If another timeline is already running, it is stopped before restarting.

---

## StopTimeline()

Stops the active timeline immediately.

Resets all runtime state.

---

## ContinueTimeline()

Signals the currently executing track to continue.

Only functions while:

- The timeline is currently running.
- The active track is waiting for a continue signal.

This is typically called by:

- Animation Events
- Dialogue systems
- AI
- Gameplay scripts
- Any other external system that determines when an event has finished.

---

# Runtime Variables

## IsRunning

Read-only property indicating whether the timeline is currently executing.

Useful for gameplay checks such as:

- Preventing duplicate activations
- Determining if a cutscene is active
- Blocking player interaction

---

# Completion Event

## On Timeline Complete

UnityEvent fired once every TimelineEventTrack has finished executing.

Useful for:

- Re-enabling player controls
- Destroying temporary objects
- Starting another timeline
- Unlocking cameras
- Triggering follow-up gameplay

---

# Typical Example

## Treasure Chest

### Track 1

```
Start Delay
0

Wait For Continue
✔

Event
Play Chest Animation
```

Animation Event calls:

```
TimelineRunner.ContinueTimeline();
```

---

### Track 2

```
Start Delay
0

Wait For Continue
✘

End Delay
0.25

Event
Spawn Loot
```

---

### Track 3

```
Start Delay
0

Wait For Continue
✘

End Delay
0

Event
Play Sound
```

---

# Design Philosophy

The Timeline Runner is intentionally lightweight.

It is designed around sequencing gameplay events rather than replacing Unity Timeline.

Advantages include:

- Completely data-driven
- Minimal scripting required
- Highly reusable
- Inspector-friendly
- Supports both timed events and externally controlled events
- Easily extended in the future without changing existing timelines

The current implementation intentionally avoids more advanced features (branching, parallel tracks, looping, conditional logic, timeout handling, etc.) until real gameplay requirements justify them.