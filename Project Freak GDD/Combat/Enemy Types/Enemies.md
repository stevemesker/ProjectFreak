Enemies can be defined by three traits: Factions, Types, and Specialties

**Factions**
Factions refer to a theme for the characters, typically controlled by the dungeon the player will be in. Does not refer to the mechanics of the teams which governs if a projectile will hit an enemy.

[[Lumina Guard]] - Peace keeping force of the [[Lumina Council]] who answer to [[General Izen]] and [[Lord Kestrel]]. See [[Lumina Guard Enemies]] for more detail

[[Lumina Archon]] - Magic wielders and those chosen by the light of [[Pallium]], they are the closest thing to divinity next to the [[Quiet King]] who they take direct orders from. They mostly operate within the castle and very rarely enter the lower parts of [[Sanctoria]]. See [[Lumina Archon Enemies]] for more info

[[Fiend]]s - the demonic scourge that has all but eradicated the mortal world. See [[Fiend Enemies]] for more detail

**Types**
[[Popcorn]] - simplest and weakest of enemy units. Meant to be fodder for the player to feel powerful while offering small amounts of poke damage. [[Popcorn]] enemies tend to be melee units with very low stats and can be killed in a single hit. There can be anywhere between 15 to 50 of these units in a given floor depending on its size

[[Basic]] - your run of the mill enemy types. They tend to have only one specialization and make up the bulk of enemy groups the player will encounter. There can be anywhere between 15 to 30 of these units in a given floor depending on its size

[[Lieutenant]] - higher ranking enemy units that command the basic ones. They tend to look similar to basic units with some unique trait. They tend to specialize in two categories or a single one that they really excel at. There can be anywhere between 10 to 20 of these units in a given floor depending on its size

[[Elite]] - highest ranking combat unit that can be easily found in dungeons. They are the epitome of what an average being of a faction can be. They always have two specialties and tend to have unique signature attacks that effect the battlefield in profound ways. There can be anywhere between 3 to 10 of these units in a given floor depending on its size

[[Mini Boss]] - unique enemies that do not have much narrative influence but have very unique skillsets and locations tailored to them. Usually only one of these can be found in a floor

[[Boss]] - the be all end all enemy of a chapter with massive significance in the story, unique mechanics, and two legendary specialties. Only one of these will be in a dungeon

**Specialties**
Specialties refer to what kind of combat an enemy is good at. We follow the rule that an enemy can have no more than two specialties, giving them a weakness the player must use to exploit and defeat an enemy. Two enemies can share a specialty but can very on the skills they have access to and if they're more magic based or physical based.

Strength - Enemies that do large damaging attacks

Defense - Enemies that have lots of health or skills that allow them to minimize or even nagate damage

Speed - enemies that move quickly and have lots of skills that allow them to move and escape the player

---
# Enemy Combat
The following section defines how enemies operate in combat. Bosses will always be the exception to most of this as their AI will tend to be more custom

**Temperament**
Enemies sit on a spectrum from Aggressive to Passive that decides how likely they are to fight and how they fight. In the game, a temperament is a **personality preset** on the enemy's EnemySO (aggression, fear, roam), so the same AI plays differently for each one. See [[Unit Brain#Personality]]. *Rewritten Nov 9, 2026 to match how the AI is built ([[Fiend Combat Plan]])*

***Aggressive*** - Attacks anything it sees that isn't part of its faction. Wanders more looking for a fight, prefers up close combat, never runs, and uses its most powerful abilities as soon as it can. [[Popcorn]] enemies tend to be Aggressive. *Preset: high aggression, no fear, wider roam (like Berserker)*

***Cunning*** - Mixes ranges depending on the situation. Won't shy away from up close fights and uses its strongest abilities first if it can reach its target. Never runs. Most enemies, especially melee ones. *Preset: normal aggression, low fear (like Normal)*

***Timid*** - Fights up close until it loses a lot of health, then switches to ranged abilities. Given the choice, it picks ranged. *Preset: fear that rises with damage taken (like Survivor), and its ranged abilities score higher once it's hurt*

***Passive*** - Never tries to fight. Runs away and alerts other enemies, and uses abilities that turn it invisible or speed it up. Only highly specialized enemies. *Preset: almost no aggression, high fear (like Coward), plus an "alert others" action*

**Behavior**
There are no behavior trees. Every enemy scores each thing it could do and does the best one, and its temperament is part of every score (see [[Unit Brain]]). The states below are actions it can pick:

Idle - A sit and wait stage between other actions *(built)*

Wander - Walking around its area *(built)*

Alert - It knows something is around, so it notices targets from farther away *(planned, [[Alert and Search actions]])*

Searching - It lost its target and checks the last place it saw it for a short time before giving up *(planned, [[Alert and Search actions]])*

Combat - It can see a hostile target and is fighting: chasing, keeping its distance or fleeing, while picking attacks *(movement built, attacks planned in [[Fiend Combat Plan]])*

**Combat**

***Attacking***
Almost every enemy has a basic attack, melee or ranged (see [[Basic Attack]]). Basic attacks are abilities that use the same swing shapes and combos as the player's weapons, so they follow the same rules. Most don't stop the enemy moving. Basic attacks don't show ground markers, since they happen too often.

***Abilities***
Abilities are skills that do more than just hit something, like an AOE that binds the player in place, or a tether that heals a target but locks the caster in place. Enemies, shades and Hazen all use **the same ability system** ([[Shade Ability]]s and enemy abilities are the same kind of asset), and it works from the player's input or the AI. Almost every ability shows a telegraph marker on the ground while it winds up.
