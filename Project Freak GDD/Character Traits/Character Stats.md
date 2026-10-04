these are the basic stats that all movable units will have in a [[Dive]]. [[Destructible Objects]] will not have these stats and will have a different method to receive damage.

**Physical Stats**

[[HP]] - Maximum health points of a character
[[Health]] - current health points of a character. Bringing to 0 or less kills the unit
[[STR]] - physical strength stat
[[DEF]] - physical defense stat
[[AGI]] - physical agility stat

---
**Mental Stats**

[[INT]] - mental power stat. Drives magical attacks (melee and ranged)
[[SPR]] - mental fortitude stat. Defends against magical attacks

*Decided Oct 2026 (damage overhaul):* WIS was dropped and SPR went from "mental agility" to mental fortitude. Nothing in combat needed a mental agility stat, since AGI already covers speed. The code still has `_WIS` until the enum cleanup in [[Known Issues]]. [[Notes for the future]]

**Stat balance rule:** the physical side has three stats (STR / AGI / DEF) and the mental side has two (INT / SPR), so a magic build gets one attack stat for both melee and ranged. That's intended: Hazen is a shadowmancer and magic can be a bit stronger thematically. Balance it through how much physical stat growth units get, not by adding stats. **Physical builds must always stay viable and never feel punished.** AGI also drives dash cooldown and pass-through dash damage (see [[Dash]]).

---
**Other Stats**

[[Name]]
[[LVL]] - never raises stats directly. For shades it decides how many runes the core can power; for Hazen it's his [[Tamer Level]]. See [[Damage Balance]]

**Stat floor:** no stat can go below 1, however many negative runes are stacked.

---
**Combat Stats**  
The default stats for handling damage types (physical melee vs ranged magic etc.)

[[Physical Primary]] - the main physical ability for an attack by a standard unit. Tends to be used by melee weapons but not always
[[Physical Secondary]] - secondary ability for an attack by a standard unit. Tends to be used by ranged weapons but not always
[[Magical Primary]] - the main magical ability for an attack by a standard unit. Most magical attacking weapons use this
[[Magical Secondary]] - secondary magical ability for an attack by a standard unit. This is usually used for stat debuff/altering magical abilities and weapons

---
**Modifier Pointers** 
The default stats for handling resistances to physical or magical effects

[[Physical Defense Modifier]] - used to defend against physical attacks
[[Magical Defense Modifier]] - used to defend against magical attacks

---
**Resistances** 
Taking half damage from certain attack types or elemental types

[[Attack Type]] Resistance - Takes half damage from a specific Attack Type
[[Elemental Affinity]] Type Resistance - Takes half damage from a specific Element Type

---
**Immunity** 
Complete immunity to certain attack types and elemental types

Attack Type Immunity - Takes no damage from a specific Attack Type
Element Type Immunity - Takes no damage from a specific Element Type