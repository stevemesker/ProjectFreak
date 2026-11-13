Getting the Chapter 1 fiends fighting, from [[Popcorn]] to [[Elite]]: enemy attacks, one ability system for every unit, status effects, telegraphs and the shared movers. Talked through one topic at a time with Steve on Nov 8–9, 2026. Every item has its card(s) on the boards, listed in **Cards** at the bottom.

Back to [[AA - AI Info]] · [[Fiend Enemies]] · [[Enemies]] · [[Unit Brain]] · [[Ability System]]

---
## Scope
The Chapter 1 dungeon ([[Sanctoria Slums]]) only has [[Fiend]]s, so this batch is the nine fiends from Popcorn to Elite, playable with greybox art:

| Rank | Fiends | New tech they need |
| :--- | :--- | :--- |
| Popcorn | [[Scarabs]], [[Burst Scarabs]] | Basic melee, self-destruct, poison patch, big swarms |
| Basic | [[Lurker]], [[Locust]] | Lunge (dash), projectile, lobbed grenade, slow patch, flying |
| Lieutenant | [[Living Hive]], [[Chaos Knight]] (melee and ranged) | Spawning with a cap, combos, leap slam, slow-turning homing arrow, 3-shot burst |
| Elite | [[Stalkers]], [[Juggernaut]] | Cloak, backstab, low-health self buff, long charge that breaks barriers, fire trails |

**Not in this batch:** the [[Cyclopse]] mini boss and the [[Watcher Fiend]] boss, loot and XP (the next combat part), destructible walls and buildings in POIs (after the first demo), attack limits (after playtests), fiend models and animations (greybox until then).

---
## Decisions

### The brain stays a scorer
- No behavior trees. The October brain scores every action and runs the best one, with personality in every score ([[Unit Brain]]). The design notes in [[Enemies]] were rewritten to match (Nov 9, 2026)
- **Temperaments are personality presets:** Aggressive (close to Berserker), Cunning (Normal), Timid (Survivor, and prefers ranged abilities once hurt), Passive (Coward, plus an "alert others" action when a passive enemy is made)
- **Idle, Wander and Combat already exist.** Alert (easier to aggro after noticing something) and Searching (go to the last place it saw the target, look around, give up) are two new actions, low priority since fiends are bestial
- Each fiend sets its preset on its EnemySO, like the test enemy does now

### One ability system for every unit
- Hazen, shades and enemies all use `AbilitySO`. It already runs from player input (the [[Radial Menu]]), so it gets extended, not replaced. NPC abilities and shade abilities are the same assets, so the design work is shared
- **Abilities don't care who's driving.** Starting one hands it a **cast context**: caster, aim direction, aim point and target (if any). The player fills it from the same aim the weapons use, the AI from its target. Steps and functions never check which one did it
- **AI Use section** on the `AbilitySO`: range (min / max), when it's a good idea (close distance, finish someone, low health), weight, needs line of sight. The brain turns each ability into a scored choice. The player ignores it
- **Cooldowns are per unit**, using `_AbilityCooldown` (on the asset, unused until now). Same for player and AI
- The interpreter's step timing bug ([[Known Issues]]) gets fixed here

### Weapons and abilities share one attack runner
- Weapons **stay items** (rarity, element, ranged firing types). Turning them into abilities would mean rebuilding finished code and losing the item side
- The combo timing, swing sweep, hit checks, damage, knockback and stagger move out of `WeaponAttackMelee` into a shared **attack runner**, along with the "next press continues the combo, a pause resets it" input logic. Projectile firing comes out of `WeaponAttackRanged` the same way
- Hazen's weapon hands the runner its combo data. Abilities use it through two new functions: **Melee Combo** (runs `ComboStepEntry`s and `SwingShapeSO`s) and **Fire Projectile**. A shade's Hit, a Lurker's swipes and Hazen's sword all hit the same way
- **Basic attacks are abilities.** For the AI, one use plays the whole combo. When the player drives a shade, each press does the next hit, like weapons

### Moving during abilities
- Using an ability doesn't mean standing still. **Each step** (with a default on the ability) has:
  - **Move multiplier:** 1 = full speed (most basic attacks), lower = slowed, 0 = rooted (a bow charging, the hive hatching)
  - **Turning:** free, or locked to the aim (like melee swings lock facing)
  - **Blocks other abilities:** its own checkbox, separate from movement
- **The brain runs two layers:** a movement action (Chase, Keep Distance, Flee...) and an attack choice (which ready ability scores best, if any). A rooted step pauses the movement action until it ends; mover abilities take movement over while they run. Engage (the placeholder) goes away
- Enemies turn to face their target while attacking or holding still

### Movers
- **Dash mover:** a straight line to a point, stopping early at walls (and on a hit, if the ability says). Optional turn rate while dashing. Enemies slide along the NavMesh like knockback does, so they can't be pushed through walls. Dashes are blocked by everything, leap-over objects included. **Plows Through** keeps going through destructibles it breaks. Used by Lunge, Backstab, Immovable Chaos
- **Leap mover:** airborne for a set time, not blocked along the way, with a **landing circle** on the ground
  - Player: the circle starts ahead and movement input steers it during the air time, within a max distance from takeoff (like V Rising's mace jump). AI: aims it at its target
  - **Steerable** checkbox: Chaos Dive, as written, always lands a set distance ahead
  - The circle doubles as the warning when an enemy leaps at you
  - Only lands on valid ground (NavMesh for enemies)
- **What leaps can cross:** blocked by everything solid **unless** the object has a `LeapOverObject` marker (crates, railings, rubble). Forgetting a marker only makes a leap stop short; the other way round, a forgotten wall would let units leap out of the map. If finished dungeons show the opposite default is better, it's one setting in the leap check. The arc is checked live, and the landing circle stops at the last clear spot so it shows where you'll really land

### Status effects
- **`StatusEffectSO`** is data: a list of **parts** picked in the inspector, like ability functions and rune effects. Starter parts: Damage Over Time (a normal damage package, so elements and resistances work), Stat Modifier (move speed, attack speed, defense...), Stun (the stun enemies already have), Invisible, Visual (shader or VFX on the unit). Plus a reapply rule: refresh, stack, or keep the strongest
- Which effects exist (poison, on fire, frozen, chilled and unique ones) is Steve's design card. Reactions between them (fire thaws frozen) can come later on top
- **An effect on a unit is a tiny record**, not an object: which effect, who applied it, stacks, end time, next tick time
- **Performance:** timers store when they end instead of counting down every frame; damage ticks on its own interval (about twice a second); `UnitStatus` joins one shared update list only while it has effects, so clean units cost nothing; stat changes are worked out only when an effect starts or ends and everything reads the cached result; visuals only change on add or remove
- Abilities apply effects with an **Apply Status** function, so shades can slow enemies the same way a Locust slows you
- **Invisible** is a status (some shades will have it too). Its look depends on the viewer's side: an ally look for your own invisible shade, the Predator-style warp for a hostile Stalker. Targeting checks it: units can't pick an invisible hostile as a new target

### Ground patches and AoE
- **`GroundZoneObject`:** spawned by an ability, with a radius and lifetime, applying its status to anyone standing in it. The poison patch, sap patch and fire trail are one object with different settings
- **AoE hits hostile factions only by default** (Player, Fiend, Lumina), through `UnitTeam.IsHostileTo`, like weapons. A **Hits Everyone** checkbox turns that off for chaotic spells and traps
- **Flying units** (a **Flying** checkbox on the EnemySO) ignore ground patches, like certain floor traps

### Wind-ups and telegraphs
- **Every step has a phase:** Wind Up, Active, Recovery or None, plus an optional animation name. The attack runner reports the same phases per combo hit, so Hazen's weapons get the hooks too
- **`UnitAnimationBridge`** (every unit) sets Animator parameters by name when a phase starts: a trigger, plus a 0–1 wind-up progress value for blend trees. Safe on units with no Animator
- **`GreyboxTelegraph`** (temporary) listens to the same events: tints toward a charge color during the wind-up, flashes when active, dulls during recovery. Deleted from prefabs when the real animations come in
- **Ground telegraphs, Wildstar style:** almost every ability shows its area during the wind-up; basic attacks don't (too frequent). A **Telegraph** part on a step: shape (circle, cone, lane, ring) sized from the ability's own numbers, with a fill that tracks the charge, locked where aimed or following the caster. Pooled. The marker designs and look are Steve's card

### Death (bare bones)
- Needed now so anything can be tested: at 0 health the brain, abilities, statuses and colliders turn off, the unit leaves the target lists, the death phase plays through the bridge (the greybox shrinks it), then it despawns. Burst Scarab's Detonate goes through the same path
- Hatched scarabs are marked as **spawned** so the loot part can skip them. **The Living Hive itself is a big loot source** to make up for it
- Loot tables, drops and XP are the **next combat part**, not this batch ([[Defeated enemies drop items]])

### Swarms
- Already in: far and Popcorn enemies think less often, and thinking is staggered
- Hatched scarabs come from a **pool**
- **No attack limits yet.** Get the swarms playable and see if they're needed (attack tokens: only a few melee attackers per target, the rest circle)

### Flying
- Flyers stay on the NavMesh with the body raised to a hover height: they path around walls and can't cross pits
- **Melee can hit them.** Chapter 1 flyers are meant to be easy kills; later flyers get movement abilities to dodge. Only flyers reaching high areas is for later [[Notes for the future]]

### Breaking barriers
- Immovable Chaos is the first "specific ability" from [[Destructible Objects]]: its hit carries a **Crush** tag on the [[Damage Package]], and Medium and Heavy destructibles count it as a breaking hit
- Destructibles aren't built yet (`EnvironmentDamageable` is an empty template), so the tiers, materials and the tag come in with this, for **barriers and doors**
- Destructible walls and buildings in POIs (Streets of Rogue style mayhem vs a subtle route) come after the first demo [[Notes for the future]]

### Enemy stats
- The EnemySO gets a **Stats** section (HP, STR, DEF, AGI, INT, SPR, resistances, immunities). `EnemyStats` reads it on spawn, and health stops living on the prefab
- **No special case for Popcorn.** Steve balances them so a player at the dungeon's level out-damages their health, so they still take real hits from an under-leveled shade in a late dungeon (no farming easy kills)
- Real numbers come from the enemy stat curve in [[Damage Balance]] (still undecided), Steve's card. Placeholder stats to start
- **Elite rank** goes into `EnemyType.Rank` between Lieutenant and MiniBoss. *Built Nov 9, 2026 with fixed numbers instead (Elite saved as 5, like `DamageType`), so no saved asset shifted; nothing compares ranks by order*

---
## Build order
1. **Foundation:** enemy stats and the Elite rank, bare-bones death, the shared attack runner, the ability overhaul (cast context, cooldowns, step movement, phases, AI Use), the brain's attack layer and facing, Melee Combo and Fire Projectile, the animation bridge and greybox telegraph
2. **Popcorn:** Scarab, then status effects and ground zones, then Burst Scarab
3. **Shared pieces:** dash mover, telegraph markers, leap mover, player input for unit abilities
4. **Basic:** Lurker, Locust
5. **Lieutenant:** Chaos Knight (melee), Chaos Knight (ranged), Living Hive
6. **Elite:** destructible barriers, Stalker, Juggernaut
7. **Extras:** Alert and Search actions

Each fiend is set up and tested with greybox art when its card is done.

---
## Later [[Notes for the future]]
- Loot, drops and XP (next combat part). The Living Hive should be a big loot source
- Attack tokens, if playtests show swarms need them
- Only flyers reaching high areas
- Destructible walls and buildings in POIs (after the first demo, with a community video)
- Status effect reactions (fire thaws frozen, etc.)
- Cyclopse mini boss and the Watcher Fiend boss

---
## Cards
Every card that links this note (through `plan`, `needs` or anywhere in its text), grouped by board.
```base
filters:
  and:
    - file.inFolder("Tasks")
    - file.hasLink(this.file)
views:
  - type: table
    name: Cards
    groupBy:
      property: file.folder
      direction: ASC
    order:
      - file.name
      - status
      - owner
      - needs
    sort:
      - property: priority
        direction: ASC
```
