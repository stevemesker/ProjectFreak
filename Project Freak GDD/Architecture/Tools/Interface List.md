All interfaces currently in the project, grouped by the file they live in.

---
**Combat** (`CombatInterface.cs`)

| Interface     | Description                                                                                           |
| :------------ | :---------------------------------------------------------------------------------------------------- |
| `IDamagable`  | Anything that can be affected by damaging projectiles, weapons, spells, and abilities. `TakeDamage(DamagePackage)`. See [[Damage Receivers & Projectiles]] |
| `ITriggerable` | Any equipment that can be activated via the trigger function in combat. More details in [[Weapon usage]] |
| `IUnitData`   | Gives access to a unit's `DangerLevel` settings. Used for camera shake dampening                       |
| `IKnockbackable` | Anything that can be pushed back by a hit. `TakeKnockback(DamagePackage)`, called by `EnemyDamagable`. See [[Enemy Movement]] |

*Spelling note:* the code uses `IDamagable` (one "e").

---
**Interaction** (`InteractionInterface.cs`)

| Interface       | Description                                                                                                   |
| :-------------- | :------------------------------------------------------------------------------------------------------------ |
| `IInteractable` | Anything that has a basic interaction such as activating a lever, opening a door, or talking to an npc. Typically uses [[Interaction Object]] |
| `IPickup`       | Items on the ground that can be picked up (`ItemDrop`). Gives the item, the amount, and lets the picker take some |
| `IInventory`    | Anything that can hold items (`PlayerData`). `AddItem` returns how many didn't fit                            |

---
**Units** (`SummonedUnitInterface.cs`)

| Interface     | Description                                                                      |
| :------------ | :------------------------------------------------------------------------------- |
| `ISummonUnit` | Anything that can be summoned by another unit (like a [[Shade (Runtime)|Shade]]). Takes its summoner and stats |

---
**Rune Field / Map Nodes** (`EvolutionInterfaces.cs`)

| Interface        | Description                                                                                  |
| :--------------- | :------------------------------------------------------------------------------------------- |
| `IBridgeable`    | Anything that can be linked with a bridge line. Used by [[Rune Field]] nodes and [[Dungeon Map Node]]s |
| `IConnectable`   | [[Rune Field]] nodes that pass power along a chain                                           |
| `ICoreNode`      | The [[Core Node]] power source                                                               |
| `iEvolutionNode` | [[Ability Node]]s that an element rune can plug into                                         |
