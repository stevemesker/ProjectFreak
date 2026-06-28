Damage can be delivered universally via the IDamageable interface (see [[Interface List]]). Damage usually involves lowering [[Health]] but in the case of [[Destructible Objects]] or traps that don't have proper stat points they still use the same interface but will instead break the object outright

___
**Damage Types**

Standard Damage - standard damage is typically done through combat between units via weapons, spells, or abilities. Typically enhanced by the associated [[Character Stats]] and reduced by the defense of that same stat

True Damage - true damage is an unblockable and unreducable damage type typically reserved for special events and development testing.

Explosive Damage - the most chaotic kind of damage. It will hurt anything it reaches regardless of who created the damage and is the only thing that can harm certain kinds of [[Destructible Objects]]

---
**Standard Damage**

Standard damage is typically applied during combat and is the most modified type. The main damage algorithm is:

**((Base damage + Character Attack Stat) x Effectiveness - Receiver Defense Stat) / Resistance x Critical**


| Type          | Description                                                                                                                                                                                                                       |
| :------------ | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Base Damage   | the base damage defined by either the weapon or the ability being used                                                                                                                                                            |
| Attack Stat   | The stat that is dependent on the type of attack being made. Physical melee attacks will use STR, Physical ranged is AGI, and Magical attacks typically use INT                                                                   |
| Defense Stat  | The stat that is dependent on the type of attack being made. Physical defense will use DEF and Magical defense will use SPR                                                                                                       |
| Effectiveness | When an attack type is a weakness of the target this number acts as a damage multiplier. Usually this happens with elemental effectiveness but some things can be weak to physical ranged or explosive damage                     |
| Critical      | [[Critical hit]]s are semi-random multiplier bonuses that can cause huge damage to enemies                                                                                                                                        |
| Resistance    | Resistance is the opposite of effectiveness. Typically resistances are given to a character that is of that type, like a water creature being resistant to water or a heavy tank being resistant to most types of physical damage |

---
