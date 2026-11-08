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
Enemies can be defined based on a spectrum from Agressive to Passive that defines how likely they are to fight and even how they fight

***Aggressive*** - These enemies will attack anything they see that is not part of their faction. They also tend to wander more looking for a fight and prefer up close combat. They will never run from fights and always use their most powerful abilities as soon as they can. [[Popcorn]] enemies tend to fall under this category.

***Cunning*** - These enemies will use a mix of ranges depending on their situation. They won't shy away from up close fights and tend to use their strongest abilities first if they can reach their targets. They never run from fights. The majority of enemies, especially melee focused enemies, fall under this category.

***Timid*** - These enemies can go from up close attacks to ranged abilities when they lose a lot of health. When given the option between ranged and melee attacks they will more likely chose ranged.

***Passive*** - These enemies will never try to fight and opt for running away and alerting other enemies of threats. They tend to use abilities that turn them invisible or speed them up. Only highly specialized enemies fall under this category.

**Behavior**
Enemies operate on behavior trees defined by their particular temperament. 

Idle - A sit and wait stage. Typically used as a transition state for enemies.

Wander - enemies are walking around their location.

Alert - Enemies know the player is around so they're easier to agro

Searching - Enemies have lost their last hostile target and are looking in the last known location for a short time before going back to alert

Combat - Enemies can see a hostile target and are actively fighting

*This behavior section is a WIP and I'm just throwing in some ideas*

**Combat**

***Attacking***
Most, if not all, enemies have a basic attack action they can take. It will typically be either a melee attack or a ranged attack (see [[Basic Attack]] for more info) and has a preset attack speed.

***Abilities***
Abilities are specialized skills that can do more than just hit something. Some can cast an AOE attack the binds the player to one spot or a tether ability that heals a target but locks the caster in place. 

*-Ideally the abilities will use the same system as the [[Shade Ability]] system.*