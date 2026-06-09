[[Note To Self]] add image here

---
**Description**
[[Element Rune]]s are the only nodes that can be moved by the player. They can be dragged over from the player's [[inventory]] and interact with [[Ability Node]]s and the [[Core Node]].

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

---
**Stat effects**
These nodes (when powered) give various types of buffs, debuffs, and other effects

| Type             | Description                                        |
| :--------------- | :------------------------------------------------- |
| stat effect      | changes [[Character Stats]] / [[Shade Stats]]      |
| element effect   | adds an [[Elemental Affinity]] to attack/defense   |
| connect boost    | adds more of a stat to nodes connected to this one |
| energy reduction | lowers the required energy a node needs to power   |
