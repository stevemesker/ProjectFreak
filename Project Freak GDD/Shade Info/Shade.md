A shade is a creature made by separating one's soul into their shadow. Over time the shadow becomes more powerful and can take on different qualities using the tool known as a [[Rune Field]].

---
**Evolution**
[[Shade]]s can evolve using an [[Ability Node]] with an [[Evolution]] property. Each evolution is designed to look similar to the rank below it so we don't run into the Digimon problem of dog turns to sexy lady which turns into gun. Every evolution should carry design elements of the previous and have an obvious escalation of strength.

---
**Abilities**
[[Shade]]s have a mixture of abilities they can use in and out of combat to enhance gameplay

the list of ability types are as follows

| Type                 | Description                                                                                                                                                                                  |
| :------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [[Natural Ability]]  | An ability that is inherent to all [[Shade]]s of a certain [[Evolution Type]] such as flight, swimming, etc                                                                                  |
| [[Ultimate Ability]] | An ability specific to that shade [[Evolution]]. Some of the younger evolution ranks of the [[Shade]] have the same abilities but Legendary ranked [[Evolution]]s have their own unique ones |
| [[Shade Ability]]    | An ability that can be unlocked via the [[Rune Field]]. Any [[Shade]] can access the ability provided it has not been locked out for any reason                                              |

---
**Levels and Power**
A shade's level decides how much power its [[Core Node]] has, which decides how many [[Element Rune]]s it can power. Its level never raises stats directly. The stats come from its [[Evolution]] and the runes the player places, so progression is how you build your [[Rune Field]]. See [[Damage Balance]].

---
**Death and Burning Down** *(idea, to work out later)*
The game is a roguelite at heart: going back through dungeons, trying new training methods and raising new shades from easy-to-make beasts is a big part of the loop (the Digimon World influence).

When a shade dies it starts over from Bound. Losing a strong shade in a final dungeon could mean a huge grind, so a dead shade should pass something on to the next one. A starting idea:
- **Shade dies:** about **25%** of its levels carry over to the new shade
- **Player burns it down on purpose** (breaks it down to its core before it dies): about **50%** carries over, and it gives Hazen a lot of XP toward his [[Tamer Level]]

The numbers are [[undecided]]. The goal is that losing a shade hurts, but never means starting the whole game over.

---
**Summoning Styles**
When shades are summoned they take one of several forms that have different capabilities. Each form has a matching ability: [[Tether Shade]], [[Release Shade]], [[Control Shade]] and [[Fuse Shade]]. "Summon" is the umbrella word for bringing the shade out in any form.

[[Tethered]] - This is the starting form shades take at the beginning of the game. The shade hovers a certain distance from the player (based on the specific shade creature as they can all have different shapes and widths). They will cast abilities based on their aggressive levels. Shades in this form do not take damage for now *(need to test in playtests)*. The player always summons it themselves, and if it's tethered in the hub it stays tethered when entering a dungeon. 

Released - This shade is released from its tether and is an actual unit in the level. It can take damage, die, and make autonomous decisions like navigation and using abilities. It can be commanded to be aggressive, defensive, or passive or use specific abilities through the radial menu. Entering a dungeon returns it, and it doesn't follow the player to the next floor, so if it went on a rampage the player can choose not to release it again.

Controlled - Shades can be controlled by [[Hazen]] directly when his tamer level is high enough. When the control shade ability is activated, the camera switches to the perspective of the released shade and [[Hazen]] stays stationary playing a looped animation. He will have access to the shade's abilities and stats for a limited time ([[Notes for the future]] that time should be based on the shade's wildness and discipline but the exact algorithm needs to be decided). This ability does require the shade to be alive and released on the field. If the shade is killed while controlled, the camera goes back to [[Hazen]]. If [[Hazen]] is killed while the shade is controlled, play the sequence of de-summoning and run through the [[Fail State]] as normal.

Fused - The perfect form of a [[Shadow Tamer]] and their [[Shade]]. They become combined, fusing their stats together and allowing the summoner to access all of the shade's abilities and traits for the entirety of the floor. Can only be used when the [[Shade]] is well disciplined.

Only one shade is ever out. [[Return Shade]] brings it back whatever form it's in, so the player can hide it in a fight without working out where it is. See [[Shade Forms Plan]].

---
Data:
[[Character Stats]]
[[Party Stats]]
[[Shade Stats]]