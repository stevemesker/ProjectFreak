## Overview
A step-by-step guide for making a **new enemy** and for setting up how a **shade** behaves. It's written for someone who has never opened this project before, so every step says where to click and what you should see when it's done.

**This page is a work in progress.** Enemies can move, notice the player, chase, hold position, flee and wander, but they **can't attack yet**, and shades all share one prefab. Sections that will change later are marked *(not built yet)*. [[Notes for the future]]

Back to [[Architecture Atlas]]

**Contents**
- [[#Before You Start]]
- [[#Part 1 - Make a New Enemy]]
- [[#Part 2 - Set Up How a Shade Behaves]]
- [[#Make a Personality Preset]] (used by both parts)
- [[#Troubleshooting]]
- [[#Not Built Yet]]

---
## Before You Start

### Words you'll see
| Word | What it means here |
| :--- | :--- |
| **Inspector** | The Unity panel that shows the settings of whatever you've selected |
| **Project window** | The Unity panel that shows every file in the project (`Assets/...`) |
| **Prefab** | A saved object you can place in any scene, like an enemy. Its name starts with `PFB_` |
| **ScriptableObject (SO)** | A data file that holds settings, like an enemy's speed. Its name starts with `SO_`. Many objects can share one SO, so changing it changes all of them |
| **Template** | How this project uses SOs: the SO holds the starting values and objects copy them when the game starts. Playing never changes the SO by itself |
| **Foldout** | A section of the Inspector that opens and closes with a little arrow. Most enemy sections start closed |
| **Play mode** | When you press the ▶ button at the top of Unity. Changes made to objects in a scene during play mode are **undone** when you stop. Changes to SO files during play mode are **kept** |
| **NavMesh** | The invisible "walkable floor" map AI uses to find paths. Dungeon floors build it by themselves when the level loads |
| **Brain** | The `UnitBrain` component, which decides what an AI unit does. See [[Unit Brain]] |
| **Personality** | Numbers (aggression, fear, roam, loyalty) that change how a brain makes choices |

### Where things live
| What | Folder |
| :--- | :--- |
| Enemy data files | `Assets/Scriptable Objects/Enemies/` |
| Personality presets | `Assets/Scriptable Objects/AI/` |
| Enemy prefabs | `Assets/Prefab/Enemies/` *(make this folder the first time, see Step 3)* |
| The test enemy to copy from | `Assets/Prefab/Dev/PFB_Enemy_ChaseTest_Dev` |
| The shade prefab | `Assets/Prefab/Shades/PFB_Shade` |
| A test level with a NavMesh | `Assets/Scriptable Objects/Dungeon/Chapter Dev/Scenes/SCN_DungeonTestRoom_0` |

### How to test
Open any scene and press **Play**. The game's core systems load automatically, so you don't need to start from a menu (see [[Runtime Bootstrapper & Runtime Asset]]). Use the dungeon test room above, because AI needs a NavMesh to walk on.

---
## Part 1 - Make a New Enemy

An enemy is made of two things:
- **An enemy SO**, which says *what kind of enemy* it is: name, rank, size, speed, knockback and personality. You'll spend most of your time here.
- **An enemy prefab**, which is the thing that goes in the level: its model, its collider and the scripts that make it work. It points at its SO.

```text
PFB_Enemy_Goblin (the thing in the level)
 └─ EnemyMovement → Enemy Data → SO_Enemy_Goblin (the settings)
                                   └─ AI → Personality → Preset → SO_AIPersonality_Enemy_Normal
```

### Step 1 - Plan the enemy
Before touching Unity, write down answers to these. You'll need them in the next steps.
- **Name**, like "Goblin".
- **Rank:** Popcorn (weak crowd enemy), Basic, Lieutenant (tougher, leads groups), MiniBoss or Boss. MiniBosses and Bosses get placed by hand in their own arenas.
- **Size class:** Small, Medium, Large or Huge. Bigger enemies get knocked back less (Huge isn't knocked back at all). See [[Enemy Movement#Size Class Rules]].
- **Fights up close or from range?** This decides which actions it gets in Step 6.
- **Personality:** does it fight to the end, run when hurt, avoid fights, or go berserk? Check the presets in [[Unit Brain#Starter presets]]. If none fit, you'll make one in [[#Make a Personality Preset]].

✅ **When this step is done:** you have a name, rank, size class, close-up or ranged, and either a starter preset name or a description of a new personality.

### Step 2 - Make the enemy SO
1. In the Project window, open `Assets/Scriptable Objects/Enemies/`.
2. Right-click an empty spot → **Create → Enemy → Enemy**.
3. Name the new file `SO_Enemy_<Name>`, like `SO_Enemy_Goblin`.
4. Click it. In the Inspector, fill in the always-visible fields:
   - **Enemy Name:** the readable name ("Goblin")
   - **Rank** and **Size Class:** from Step 1
5. Open the **Movement** foldout and set how it moves. Hover over any field for an explanation.

   | Field | Default | Tip |
   | :--- | :--- | :--- |
   | Move Speed | 6 | Meters per second. The player runs at about 8 |
   | Acceleration | 40 | Higher = snappier starts and stops |
   | Turn Speed | 720 | Degrees per second |
   | Stopping Distance | 1.5 | How close it walks up to its destination |

6. Open the **Knockback** foldout. The defaults are fine for most enemies. Tick **Knockback Immune** for things that should never budge, like turrets.
   Then the **Stagger** foldout: the defaults are fine too. Tick **Stagger Immune** for things that should never flinch. Mini bosses and bosses show a **Boss Stagger Thresholds** list instead (health percentages to stagger at, 66 and 33 to start).
7. Open the **AI** foldout → **Personality** → drag a personality preset from `Assets/Scriptable Objects/AI/` into **Preset**. If you need a new one, do [[#Make a Personality Preset]] first, then come back.

✅ **When this step is done:** `SO_Enemy_<Name>` exists in `Scriptable Objects/Enemies/`, its name, rank and size class are set, and its AI foldout has a preset in it.

### Step 3 - Make the enemy prefab
The quickest way is to copy the test enemy, which already has every script set up and connected.

1. If there's no `Assets/Prefab/Enemies/` folder yet, make one: right-click `Assets/Prefab/` → **Create → Folder** → name it `Enemies`.
2. In `Assets/Prefab/Dev/`, click `PFB_Enemy_ChaseTest_Dev` and press **Ctrl+D** to duplicate it.
3. Drag the copy into `Assets/Prefab/Enemies/` and rename it `PFB_Enemy_<Name>`, like `PFB_Enemy_Goblin`.
4. Double-click the new prefab to open it.
5. Remove the **Damage Tester** component (click its ⋮ menu → **Remove Component**). It's a testing tool and real enemies don't need it.

✅ **When this step is done:** `PFB_Enemy_<Name>` is in `Assets/Prefab/Enemies/` and opens on its own. Its Inspector lists these components: Enemy Damagable, Enemy Stats, Capsule Collider, Nav Mesh Agent (turned **off**), Enemy Movement, Unit Team, Unit Targeting, Unit Brain and Enemy Stagger. Enemy Stagger's **On Staggered** event is where a hit reaction (flash, sound, animation) gets hooked up.

### Step 4 - Give it its look and body
With the prefab still open:
1. In the Hierarchy, delete the child object `Mesh_TargetDummy_Dev` (the placeholder model).
2. Drag your enemy's model in as a child of the prefab's top object. Line it up so its feet sit at the top object's position.
3. Select the top object. On the **Capsule Collider**, adjust **Center**, **Radius** and **Height** until the green capsule wraps the model's body. This is what weapons and the player bump into.
4. On the **Nav Mesh Agent**, set **Radius** and **Height** to roughly match the capsule. The agent uses these to path around other units and fit through doorways.
   - **Leave the Nav Mesh Agent turned off** (unticked). The enemy turns it on by itself once the level's NavMesh is ready. If it's on, a yellow warning box appears on Enemy Movement.
5. Check the top object's **Layer** (top of the Inspector) is **Units**. If Unity asks whether to change the children too, choose **Yes, change children**.
6. Make sure the model itself has **no colliders** of its own. Only the top object's capsule should be solid.

✅ **When this step is done:** the prefab shows your model, the green capsule fits around it, the Nav Mesh Agent is off with a matching size, the layer is Units, and the only collider is the capsule on the top object.

### Step 5 - Connect it to its SO and set its health
Still on the top object:
1. **Enemy Movement → Enemy Data:** drag in your `SO_Enemy_<Name>` from Step 2.
2. **Unit Brain:** at the top of its Data section it should now say *"This enemy's personality comes from its EnemySO (AI foldout)"*. That's how you know the connection worked.
3. **Unit Team:** Team should be **Enemy** and Role should be **Enemy** (already set on the copy).
4. **Enemy Stats → E Stats:** set **HP** (max health) and **Health** (starting health) to the same number. *Health lives on the prefab for now and will move into the SO in a later stats pass.* [[Notes for the future]]
5. **Enemy Damagable:** its **On Damage** list should have one entry calling `EnemyStats.TakeDamage`. That's what makes hits hurt. Don't remove it.

✅ **When this step is done:** Enemy Data shows your SO, the Unit Brain shows the "comes from its EnemySO" message, Unit Team says Enemy/Enemy, HP and Health are set, and On Damage still calls `EnemyStats.TakeDamage`.

### Step 6 - Choose its actions
Actions are the things the brain can choose between (chase, flee, wander...). See [[Unit Brain#Actions]] for what each one does.

- **Fights up close:** do nothing. When the **Actions** list on Unit Brain is empty, the enemy gets the standard set when the game starts: Chase, Engage, Flee, Wander.
- **Fights from range:**
  1. On Unit Brain, press **Fill Enemy Defaults**. The four standard actions appear in the Actions list.
  2. Remove **Chase Action** and **Engage Action** (the X on each list entry).
  3. Press **+** on the list and pick **Keep Distance Action**.
  4. Open it and set **Preferred Distance** (how far away it likes to stay, in meters) and **Tolerance** (how far off that distance it can drift before moving).
- **Any enemy:** you can open any action to change its settings, or set its **Weight** to make it more (above 1) or less (below 1) likely to win. Weight 0 turns it off.

✅ **When this step is done:** either the Actions list is empty (close-up fighter), or it holds exactly the actions you want with their settings filled in.

### Step 7 - Test it
1. Open `SCN_DungeonTestRoom_0`.
2. Drag `PFB_Enemy_<Name>` into the scene, onto the floor and away from where the player starts.
3. Press **Play**. Turn on the **Gizmos** button at the top of the Game view so you can see the label above the enemy.
4. Check each of these:

   | Do this | You should see |
   | :--- | :--- |
   | Wait a few seconds | The label above it says **WanderAction** and it strolls around near its start point, pausing between walks |
   | Walk into its view (within 15 m, nothing in between) | Label changes to **ChaseAction >** followed by the player's name, and it comes at you. A coward shows **FleeAction** and runs |
   | Stand still next to it | Label says **EngageAction** and it stops about 2 m away. It won't attack, that's expected for now |
   | Hit it with a weapon | A damage number pops up, and it turns on you even if it hadn't noticed you yet. *Weapons don't push enemies back yet. Knockback works, but no weapon uses it so far* |
   | Run far away | It gives up past about 25 m and goes back to wandering |

5. Select the enemy while playing and look at **Unit Brain → Runtime Data**. **Scores** shows what every action scored on its last think, so you can see *why* it picked what it did.
6. Stop Play. Remove the enemy from the test scene if you don't want it saved there (or don't save the scene).

✅ **When this step is done:** everything in the table behaved as described (or the way its personality should) and there were no red errors in the Console. If something didn't work, see [[#Troubleshooting]].

### Step 8 - Tune it
1. Press **Play** again with your enemy in the scene.
2. Change values on its **SO** (Movement foldout, or AI foldout overrides) or on its **personality preset**.
3. Press **Reload All Units** (on the preset, or in the SO's AI foldout) so enemies pick up personality changes. Movement changes need a fresh Play to take effect.
4. Repeat until it feels right. Changes to SO files during Play are **kept** when you stop.

✅ **When this step is done:** the enemy feels the way you planned in Step 1, and the final values are saved in its SO and preset.

### Step 9 - Put it in the game
- **Popcorn, Basic and Lieutenant enemies:** add the prefab to the dungeon's enemy table (the `DungeonEnemyTableSO` in its `DungeonSO`'s **Enemy Table** slot) and give it a weight. Enemy spawners on the floors place it automatically. Its rank comes from its SO, so there's nothing else to set. See [[Enemy Spawners]].
- **Testing on a single floor:** set a **Test Enemy Table** on the floor's enemy spawners (Testing foldout), or drag the prefab in by hand.
- **MiniBosses and Bosses:** always placed by hand in their arena scenes.

✅ **When this step is done:** the enemy is placed where it belongs, and its prefab and SO are saved in their folders.

---
## Part 2 - Set Up How a Shade Behaves

**Right now every shade uses the same prefab**, `PFB_Shade`, which the [[Shade Manager]] spawns when you summon. What you *can* change today is how the shade **behaves**: its personality and its actions. *Making a brand-new kind of shade (its own look, abilities and commands) comes with the shade overhaul pass. See [[#Not Built Yet]].* [[Notes for the future]]

How a shade acts on its own:
- It **follows you** and stays close (Follow Leader). The more loyal it is, the harder it's pulled back when it strays.
- It **fights** whatever it notices nearby (Chase, Engage).
- When there's nothing to do, it **stays put** near you, or shuffles around if it's not very loyal (Wander).
- A **berserk** shade runs off to find something to fight (Seek Fight).
- While **you're controlling** the shade, its brain pauses. The label goes gray and says Paused.

### Step 1 - Pick or make its personality
1. Look at the shade presets in `Assets/Scriptable Objects/AI/`:
   - `SO_AIPersonality_Shade_Loyal`: stays near you and fights nearby (the current default)
   - `SO_AIPersonality_Shade_Berserk`: runs off to fight and never comes back by itself
2. If neither fits, make one with [[#Make a Personality Preset]]. For shades, **Loyalty** matters most.

✅ **When this step is done:** you know which preset the shade should use, and it exists in `Scriptable Objects/AI/`.

### Step 2 - Set it on the shade
1. Double-click `Assets/Prefab/Shades/PFB_Shade` to open it.
2. On **Unit Brain → Data → Personality Setup**, drag your preset into **Preset**.
3. To change just one value for the shade without making a new preset, tick that value's **Override** box and set the number.

✅ **When this step is done:** the shade's Personality Setup shows your preset (plus any overrides you meant to set), and the prefab is saved.

### Step 3 - Adjust its actions (optional)
1. On **Unit Brain**, if the **Actions** list is empty, press **Fill Shade Defaults** to show the six shade actions.
2. The settings people change most:

   | Action | Setting | What it does |
   | :--- | :--- | :--- |
   | Follow Leader | Follow Distance (3 m) | Stops walking once it's this close to you |
   | Follow Leader | Leash Distance (15 m) | At this distance the pull back to you is strongest |
   | Wander | Leader Personal Space (2.5 m) | Never walks closer to you than this while idle |
   | Wander | Shortest Walk (2 m) | Idle walks shorter than this don't happen, it just stands |
   | Seek Fight | Search Range (60 m) | How far a berserk shade senses enemies to run toward |

✅ **When this step is done:** the Actions list holds the shade's actions with the settings you want (or is empty, to use the defaults).

### Step 4 - Test it
1. Open `SCN_DungeonTestRoom_0` with an enemy in it, press **Play**, and **summon** the shade.
2. Check each of these:

   | Do this | A loyal shade should | A berserk shade should |
   | :--- | :--- | :--- |
   | Stand still with no enemies around | Stand near you (**WanderAction**), never walking through you | Run off looking (**SeekFightAction**) |
   | Walk away | Follow and stop about 3 m from you (**FollowLeaderAction**) | Ignore you |
   | Get near an enemy | Go after it (**ChaseAction > enemy name**) | Go after it |
   | Take control of the shade | Label turns gray, **Paused** | Same |

✅ **When this step is done:** the shade behaved as in the table for its personality, with no red errors in the Console.

---
## Make a Personality Preset
A personality preset is a small file of four numbers plus a commit time. Many enemies (or shades) can share one, and changing it changes all of them. For what each number does, see [[Unit Brain#Personality]].

1. In the Project window, open `Assets/Scriptable Objects/AI/`.
2. Right-click → **Create → AI → Personality Preset**.
3. Name it `SO_AIPersonality_<Enemy or Shade>_<Name>`, like `SO_AIPersonality_Enemy_Ambusher`.
4. In **Notes**, write one or two sentences on how it should play, so anyone can tell what it's for.
5. Set the values. Rules of thumb:
   - **Aggression below 0.1:** it never chases anything.
   - **Fear higher than aggression:** it runs away once it's badly hurt.
   - **Fear × 0.3 higher than aggression:** it runs from fights even at full health (a coward).
   - **Roam above 0.6:** with nothing to fight, it goes looking for trouble.
   - **Loyalty:** only matters for units that follow someone (shades). High = sticks close, 0 = never comes back on its own.
   - **Commit Time:** higher = stubborn, slower to change its mind. Lower = reacts faster but can look twitchy.
6. Use it: drag it into an **EnemySO's AI foldout → Preset** (enemies) or **Unit Brain → Personality Setup → Preset** (shades).

✅ **When this step is done:** the preset is in `Scriptable Objects/AI/` with a clear name and notes, and at least one enemy SO or shade uses it.

**Tuning tip:** during Play, edit the preset's numbers and press its **Reload All Units** button. Every unit using it updates right away, and your changes are kept when you stop.

---
## Troubleshooting

| Problem | Likely cause | Fix |
| :--- | :--- | :--- |
| Red error: *"No EnemySO assigned to EnemyMovement"* | Step 5.1 was skipped | Drag the enemy's SO into Enemy Movement → Enemy Data |
| Yellow warning: *"No NavMesh found within 3m"*, enemy never moves | The scene has no NavMesh, or the enemy is placed too high or off the floor | Use a dungeon floor scene like the test room, and place the enemy on the floor |
| Enemy stands still, label says **None** | No action scored above 0, or its personality isn't set | Check Runtime Data → Scores. Check the SO's AI foldout has a preset |
| Never chases you | Aggression is below 0.1 (that's what cowards do), or it can't see you | Check its preset. Make sure nothing solid is between you, and you're within 15 m |
| Runs away at full health | Fear × 0.3 is higher than its aggression | Lower fear or raise aggression |
| Label doesn't show | Gizmos is off in the Game view, or Show Debug Label is unticked on Unit Brain | Turn on the Game view's **Gizmos** button. Tick **Show Debug Label** |
| Personality changes during Play don't do anything | Units only read their personality when the game starts | Press **Reload All Units** on the preset or SO |
| Enemy slides into walls or through other enemies | Its Nav Mesh Agent radius is too small for its body | Match the agent's Radius to the capsule (Step 4.4) |
| Shade walks through the player | Personal Space is too small | Raise Wander → Leader Personal Space |
| Unity warns the Nav Mesh Agent can't find a NavMesh | The agent is turned on in the prefab | Untick the Nav Mesh Agent on the prefab (Step 4.4) |

---
## Not Built Yet
These will change the steps above when they're built. [[Notes for the future]]
- **Attacks.** Enemies and shades can't attack yet. The **ability overhaul** will let you give an enemy abilities that its brain chooses between, and Engage will be replaced. Expect a new step: "Give it its abilities".
- **Death and loot.** Enemy Stats has an **On Death** event, but nothing happens on death yet.
- **Stats on the SO.** Health and other stats live on the prefab's Enemy Stats for now. A stats pass will move them into the enemy SO, so Step 5.4 will move into Step 2.
- **Leash / room volumes.** Enemies will stay in their own area instead of chasing the player across the whole floor.
- **New kinds of shades** (shade overhaul pass). Each shade will get its own look, abilities and commands, and training will change its personality during play (Digimon World style). Expect a full "Make a New Shade" walkthrough to replace Part 2.
- **Shade health.** The shade has no health script yet, so its brain always treats it as unhurt (it never flees from low health).
- **Facing.** Enemies don't turn to face their target while standing still in Engage.

---
## Related
- [[Unit Brain]]: how the brain picks actions, every action's settings, personality reference
- [[Unit Targeting]]: who units go after and why
- [[Enemy Movement]]: the enemy SO, ranks, size classes, movement and knockback
- [[Shade (Runtime)]] and [[Shade Manager]]: how the shade exists in code and gets summoned
- [[AI Movement & Dungeon Loading Plan]]: the history of how all this was built, and what's still to do
