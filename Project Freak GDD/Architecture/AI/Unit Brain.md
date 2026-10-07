## Overview
The AI decision maker for enemies and shades. It's one component, `UnitBrain`, in `Scripts/AI/`. Built in step 7 of the [[AI Movement & Dungeon Loading Plan]].

**Making a new enemy or changing how the shade behaves?** The step-by-step is in [[How To - Create Enemies & Shades]]. This page is the reference for how the brain works.

Every `_ThinkInterval` seconds the brain asks each action in its list "how good an idea are you right now?" Each action answers with a **score**, about 0 to 1, and the best one runs. Personality values are part of every score, so the same actions can play like a coward or a berserker. There's no behavior tree: adding a behavior means adding one more action.

```text
UnitTargeting ──► who to go after
                       ↓
UnitBrain: score every action (× personality × weight) → run the best
                       ↓
IUnitMover (EnemyMovement / NavGuideDriver) → the unit moves
```

The brain never moves the unit itself. Actions give orders through `IUnitMover` (see [[Unit Targeting#IUnitMover]]).

---
## Setup
A unit needs `UnitTeam`, `UnitTargeting`, `UnitBrain` and a mover (`EnemyMovement` or `NavGuideDriver`). `PFB_Shade_Released` and `PFB_Enemy_ChaseTest_Dev` already have all of them.

If the action list is empty, the brain fills in the **default set for its role** when the game starts:
- **Enemy:** Chase, Engage, Flee, Wander
- **Shade:** Follow Leader, Chase, Engage, Flee, Wander, Seek Fight

To tweak a unit's actions, press **Fill Enemy Defaults** or **Fill Shade Defaults** on the prefab. That puts the list in the inspector, where each action's settings can be edited and actions added or removed. The type picker on the list works like ability functions.

---
## How it picks
1. **Paused?** If the mover can't take orders (waiting on the NavMesh, mid-knockback, or the player driving the shade), the brain drops its current action and waits. It picks fresh once it can move again.
2. **Score** every action: `action score × _Weight`.
3. **Switch** to the best one if:
   - nothing is running, or the current action scored 0 or says it's finished, **or**
   - it's past the personality's **Commit Time** and the new action scores higher, **or**
   - it's still committed, but the new action beats the current one by `_InterruptMargin`
4. **Tick** the current action, which updates where it's going.

When no action scores above 0, the unit just stops.

---
## Personality
Each brain builds its **own copy** of its personality when the game starts, from a **preset** plus any **overrides**. Because it's a copy, the values can change during play (like a shade losing control) without touching any asset. The copy shows under the brain's **Runtime Data**. You can tweak it there during play to test, but those changes are lost when play stops.

| Value         | What it does                                                                                     | Default |
| :------------ | :----------------------------------------------------------------------------------------------- | :------ |
| `_Aggression` | How much it wants to fight. Feeds Chase, Engage, Keep Distance and Seek Fight. **Below the Wander score (0.1), it won't bother chasing at all** | 0.6 |
| `_Fear`       | How easily it gets scared. Feeds Flee                                                            | 0.3     |
| `_Roam`       | How far it wanders. Above about 0.6 it turns on Seek Fight                                       | 0.3     |
| `_Loyalty`    | Shades: how strongly it sticks near the player. Feeds Follow Leader                              | 0.7     |
| `_CommitTime` | Seconds it sticks with a choice before changing its mind easily                                  | 1       |

### Where a unit's personality is set
- **Enemies:** on their **EnemySO**, in the **AI** foldout. Every enemy using that EnemySO gets it. The brain's own setup is hidden on enemies, and the inspector says where to look instead.
- **Units without an EnemySO** (shades): on the **UnitBrain** itself (`_PersonalitySetup`). `PFB_Shade_Released` uses `SO_AIPersonality_Shade_Loyal`.
  *Later the shade's personality will come from its shade slot data and training (shade overhaul pass).* [[Notes for the future]]

Both places use the same **Personality Setup**:

| Field | Description |
| :--- | :--- |
| `_Preset` | The shared preset to start from. Empty = the default values (a normal enemy) |
| `_Override...` toggles | Turn one on and a value field appears. That value replaces the preset's for just this enemy type or unit. Everything else still follows the preset |

### Starter presets
In `Scriptable Objects/AI/`:

| Preset | Aggression | Fear | Roam | Loyalty | Plays like |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `SO_AIPersonality_Enemy_Normal` | 0.6 | 0.3 | 0.3 | - | Chases what it sees and fights to the end (used by the test enemy) |
| `SO_AIPersonality_Enemy_Survivor` | 0.6 | 0.8 | 0.3 | - | Fights, but runs once it's below about a quarter health |
| `SO_AIPersonality_Enemy_Coward` | 0.05 | 1 | 0.3 | - | Never chases, runs from anything within 12 m |
| `SO_AIPersonality_Enemy_Berserker` | 1 | 0 | 0.5 | - | Always fights, never runs, roams a bit wider |
| `SO_AIPersonality_Shade_Loyal` | 0.6 | 0.3 | 0.2 | 0.9 | Stays put near you, fights nearby, comes back if dragged off (used by the shade) |
| `SO_AIPersonality_Shade_Berserk` | 1 | 0 | 1 | 0 | Runs off to find something to kill and never comes back on its own |

### Making a preset
Step-by-step, with rules of thumb for picking values: [[How To - Create Enemies & Shades#Make a Personality Preset]].

### Tuning during play
- **Units only read their personality when the game starts.** After changing a preset, an EnemySO's personality or a brain's setup during play, press a **Reload** button (play mode only):
  - **Reload All Units** on the preset asset or on the EnemySO (AI foldout): every active unit rebuilds its personality
  - **Reload Personality** on a UnitBrain: just that unit
- Changes to SO assets made in play mode **stay** after you stop, so editing the preset + Reload All Units is the easy way to tune presets.
- **One unit only:** type into that unit's **Runtime Data → Personality** while watching the **Scores** list. Those values are lost when play stops, and a Reload replaces them.

### Code
- `AIPersonality`: the values. `Copy()` makes a separate copy.
- `AIPersonalitySO`: the preset asset (`_Notes`, `_Values`).
- `PersonalitySetup`: the preset + overrides. `BuildPersonality()` makes the brain's copy.
- `UnitBrain.SetPersonality(personality)` swaps in a different personality during play (keeps its own copy). `ResetPersonality()` goes back to the preset + overrides. Both are ready for the shade overhaul.
- `UnitBrain.ReloadAllPersonalities()` (static) makes every active unit reset its personality. Used by the Reload All Units buttons.

---
## Actions
Each action is its own script in `Scripts/AI/Actions/`. Every action has `_Weight` (multiplies its score, 0 turns it off).

| Action | Score | What it does |
| :--- | :--- | :--- |
| **Chase** | Aggression, while the target is farther than the fight range | Runs at the target |
| **Engage** *(placeholder)* | Aggression, while the target is within fight range + `_RangeSlack` | Holds in fighting range. **Doesn't attack yet.** Abilities replace it in the ability overhaul [[Notes for the future]] |
| **Keep Distance** | Aggression, whenever there's a target | For ranged units: stays about `_PreferredDistance` (8 m) away, ± `_Tolerance`. Use it *instead of* Chase + Engage |
| **Flee** | Fear × the bigger of `_BaseFleeUrge` (0.3) and `_LowHealthCurve` (how hurt it is), while closer than `_SafeDistance` (12 m) | Runs away, trying a few angles if straight back is blocked. Finished once safe |
| **Wander** | Flat `_BaseScore` (0.1) | Waits, walks to a random nearby spot, repeats. Radius goes from `_MinRadius` to `_MaxRadius` with roam. **No leader:** wanders around where it started. **With a leader (shades):** shuffles around where it's standing; loyalty shrinks the radius (`_LoyaltyStayPut`), and if that leaves less than `_ShortestWalk` (2 m) it just stands still. It never picks a spot within `_LeaderPersonalSpace` (2.5 m) of the leader or walks a straight line that passes that close |
| **Follow Leader** | Loyalty × `_PullCurve` (how far from the leader, up to `_LeashDistance` 15 m) | Shades: walks back to a spot `_ArriveDistance` (2 m) beside the player on its own side, not onto the player, and stops within `_FollowDistance` (3 m). The farther it strays, the harder the pull |
| **Seek Fight** | Aggression × `_RoamCurve` (0 until roam 0.6), only with no target | Heads for the nearest hostile unit within `_SearchRange` (60 m), even unseen, or roams far. Makes berserk shades run off when released |

### How they play together
- **Normal enemy:** wanders → sees you → chases → holds in range. With the default fear (0.3), Flee tops out at 0.3, below its aggression, so it fights to the end.
- **Survivor:** with fear above its aggression (like 0.8), Flee's health curve beats Chase once it drops below about a quarter health, and it runs.
- **Coward:** its aggression is below Wander's score, so it never chases. When something gets within 12 m, Flee wins and it runs.
- **Your empty body:** enemies only target it when nothing else is around (see [[Unit Targeting]]). Then aggressive ones close in, and cowards run.
- **Loyal shade:** next to you, Follow Leader scores about 0, so it fights. As a fight drags it away, the pull grows until following you back wins. With no target it stays put near you: at the default loyalty (0.7) its wander distance shrinks below a worthwhile walk, so it stands still. Lower loyalty lets it shuffle around, never through you.
- **Berserk shade:** with no loyalty, Follow Leader never wins. Seek Fight sends it after the nearest enemy, and Chase takes over once it's in sight.

---
## Brain fields

| Field              | Description                                                                 | Default |
| :----------------- | :-------------------------------------------------------------------------- | :------ |
| `_ThinkInterval`   | How often it rethinks when near the camera, s                               | 0.2     |
| `_FarThinkInterval` | How often it rethinks when far from the camera, s                          | 0.6     |
| `_NearDistance` / `_FarDistance` | Distance from what the camera follows, m. Closer than near = normal interval, farther than far = far interval, blended in between | 15 / 35 |
| `_PopcornThinkMultiplier` | Popcorn rank enemies multiply their interval by this                  | 1.5     |
| `_ShowDebugLabel`  | Editor only: write the current action and target above the unit while playing | On |
| `_DebugLabelHeight` | How high the label sits, m                                                 | 2.5     |
| `_InterruptMargin` | How much better a new action has to score while committed                    | 0.15    |
| `_FightRange`      | How close it gets to fight, m. Used by Chase and Engage. *Temp until abilities give real ranges* [[Notes for the future]] | 2 |
| `_Targeting`       | Its `UnitTargeting` (auto-filled)                                           |         |

**Runtime Data** (read only) shows the current action, the current think interval, whether it's paused, and **what every action scored on the last think**. Watching the scores is the fastest way to tune.

### Performance
- The brain thinks on a timer in `Update`, not every frame. Each unit starts at a random point in its first interval, so a room full of enemies doesn't all think on the same frame.
- **Distance:** units far from whatever the camera follows (the player, or the shade while you drive it) think less often, blending from `_ThinkInterval` to `_FarThinkInterval`.
- **Rank:** Popcorn enemies think `_PopcornThinkMultiplier` times slower on top of that.
- **Shades** always think at the normal interval.

### Debug label
While playing, each brain writes **`Action > Target`** above the unit (like `Chase > Player Character_PFB`). It turns gray while paused. It shows in the Scene view, and in the Game view when the Game view's **Gizmos** button is on. Turn it off per unit with `_ShowDebugLabel`. It's editor-only, so the built game doesn't include it.

Health comes from `IUnitHealth` (`EnemyStats` has it). Units without it count as full health, which is the shade for now. [[Notes for the future]]

---
## Making a New Action
1. Make a class that inherits `AIAction`, with `[System.Serializable]`, in `Scripts/AI/Actions/`.
2. Override `Score(brain)` to return about 0 to 1. Multiply in a personality value so it reacts to personality.
3. Override `Tick(brain)` to give orders (`brain.GetMover().SetDestination(...)`). Optional: `Begin`, `End`, `IsFinished`.
4. It now shows up in the brain's action list picker.

Things an action can read from the brain: `GetTarget()`, `GetTargetTier()`, `GetPersonality()`, `GetHealthPercent()`, `GetLeader()`, `GetHomePosition()`, `GetFightRange()`, `GetTeam()`, `DistanceTo(point)` (flat distance, ignoring height).

**Runtime state:** an action can keep its own private variables (like Wander's timer). Each unit gets its own copy of its actions, so they don't share them.

---
## Still to come
- **Ability overhaul:** abilities become scored actions and replace Engage [[Notes for the future]]
- Units don't turn to face their target while standing still in Engage [[Notes for the future]]
