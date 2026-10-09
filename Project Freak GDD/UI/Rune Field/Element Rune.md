Element Rune Mockup
![[Element node.png|275]]

---
**Description**
[[Element Rune]]s are the only nodes that can be moved by the player. They can be dragged over from the player's [[inventory]] and interact with [[Ability Node]]s, other [[Element Rune]]s via [[Bridge]]s , and the [[Core Node]].

---
**Rarity and elemental effects**
Not all [[Element Rune]]s are made the same. Though technically arbitrary, the general pattern of rarity is as follows:

| Type          | Description                                                                              |
| :------------ | :--------------------------------------------------------------------------------------- |
| [[Common]]    | underwhelming stats, almost always has a negative stat enhancement                       |
| [[Rare]]      | underwhelming stats, almost never has a negative stat enhancement                        |
| [[Epic]]      | Average stats with no negative stat enhancements or large stats with some negative stats |
| [[Legendary]] | Huge stats and little to no negative stat enhancements                                   |
| [[God]]       | For developer purposes only                                                              |

---
**Material Type**
[[Element Rune]]s are composed of various types of materials that technically only effect the [[Shade]]s visuals and are arbitrary but, like rarity, usually have a pattern that follows:

| Type              | Description                                                                                        |
| :---------------- | :------------------------------------------------------------------------------------------------- |
| Stone             | The most basic and common material, no real bonus enhancements                                     |
| Elemental Liquid  | Gives bonus buffs of a specific [[Elemental Affinity]] attack type                                 |
| Elemental Crystal | Gives bonus buffs of a specific [[Elemental Affinity]] attack or resistance type                   |
| Void Essence      | Adds the [[Void Element]] and occasionally [[Ice Element]] property to attack or resistance type   |
| Lumen Stone       | Adds the [[Light Element]] and occasionally [[Fire Element]] property to attack or resistance type |

When the [[Shade Slot]] is saved, the [[Shade Manager]] will take all of the stat data and find the average material type and [[Elemental Affinity]] if the particular shader supports it.

Mixing elements is how secondary elements are found: a shade built with a lot of fire and earth runes builds out as a Lava shade, water and air as Ice, and so on (see [[Elemental Affinity]]).

---
**Stat effects**
These nodes (when powered) give various types of buffs, debuffs, and other effects

| Type             | Description                                        |
| :--------------- | :------------------------------------------------- |
| stat effect      | changes [[Character Stats]] / [[Shade Stats]]      |
| element effect   | adds an [[Elemental Affinity]] to attack/defense   |
| connect boost    | adds more of a stat to nodes connected to this one |
| energy reduction | lowers the required energy a node needs to power   |
| crit effect      | adds crit chance or crit multiplier (see [[Damage]]) |

---
**Stat Values Over the Game**
Runes are where most of a shade's stats come from, on top of the base stats its [[Evolution]] sets. Rough starting values:

| Stage | Typical rune                          | Runes a core can power (guess) |
| :---- | :------------------------------------ | :----------------------------- |
| Early | +2 good stat / −2 bad stat            | ~6–12                          |
| Mid   | +3 good / −1 bad                      | ~20                            |
| Late  | +10 good / −2 bad, or +5 good only    | ~30, maybe 50 at endgame       |

Early runes net out to zero on purpose: early power comes from evolution and loot, and runes push the shade into a specialization. The runes available get better as the player reaches better enemies, and synthesizing runes may become a small minigame [[undecided]].

No stat can be pushed below 1 by negative runes. If a player wants a stat that low, let them.

**Attack runes and obedience:** attack-heavy runes tend to raise [[WILD]] and lower [[DIS]] (see [[Shade Stats]]), so raw power comes with a shade that's harder to control.

**Runes and Hazen:** a rune can give stats to the shade, to [[Hazen]], or both, sometimes trading one for the other (for example −1 STR to the shade but +2 STR to Hazen). This is where Hazen's stats come from, and it feeds the story of why someone would split off their shadow and give it an artificial soul. Whether Hazen gets stats from only the active shade's runes or every slot is [[undecided]]. See [[Main Character Stats]].

---
**Energy Requirements**
All [[Element Rune]]s have a required energy they must consume from the [[Core Node]] in order to apply stat effects and activate [[Ability Node]]s. When on the [[Rune Field]], energy requirements are listed at the bottom of the node and are displayed as the current energy / required energy (example 2/2 or 0/4). Nodes cannot take up fractions of energy, they are either on or off.