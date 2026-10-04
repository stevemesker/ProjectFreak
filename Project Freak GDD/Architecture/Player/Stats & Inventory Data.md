## Overview
All unit stats are serializable classes in `CharacterStats.cs`. They build on each other so shared stats only exist once. For what each stat means in design terms see [[Character Stats]].

```text
CoreStats           (every unit that can fight)
 ├── PartyStats     (units that level up: XP, danger level)
 │    ├── PlayerStats  (Hazen: SOUL, SHA, shade list)
 │    └── ShadeStats   (shades: DIS, WILD)
 └── EnemyCoreStats (enemies: drops)
```

---
## CoreStats

**Stats:** `_HP`, `_Health`, `_STR`, `_DEF`, `_AGI`, `_INT`, `_SPR`, `_WIS`, `_LVL`, `_Name`

*`_WIS` is being removed. SPR is now the mental defense stat and INT the mental attack stat (see [[Character Stats]]). The cleanup is tracked in [[Known Issues]].* [[Notes for the future]]

**Combat stat pointers** - which stat each kind of attack uses:

| Variable                | Default   |
| :---------------------- | :-------- |
| `PhysicalPrimaryStat`   | Strength  |
| `PhysicalSecondaryStat` | Agility   |
| `MagicalPrimaryStat`    | Intelect  |
| `MagicalSecondaryStat`  | Intelect  |
| `PhysicalDefMod`        | Defense   |
| `MagicalDefMod`         | Spirit    |

**Resistances / Immunities:** lists of `DamageType.AttackType` and `DamageType.ElementType`.

| Function                              | Description                                                          |
| :------------------------------------ | :------------------------------------------------------------------- |
| `TypeToStatFinder(StatType)`          | Returns the value of a stat                                          |
| `GetDefensiveStatType(StatType)`      | Which defense stat blocks an attack that used this stat              |
| `GetAttackStatType(isRanged, AttackType)` | Which stat an attack uses (primary for melee, secondary for ranged) |
| `GetAttackResistanceModifier(atk, element)` | 0.5 if resistant, otherwise 1. *Immunities aren't checked yet* [[Notes for the future]] |

---
## PartyStats
Adds `_XP`, `_Danger` (`DangerLevel`, see [[Damage Receivers & Projectiles]]), and `_CurrentDangerType`.

## PlayerStats
Adds `_SOUL`, `_SHA` (number of shades allowed), `_CurrentShade`, and `_Shades`. See [[Main Character Stats]].

## ShadeStats
Adds `_DIS` and `_WILD`. See [[Shade Stats]].

## EnemyCoreStats
Adds `_DropGold` and `DropItems`.

---
## Inventory
The player's carried equipment, stored on `PlayerData.pInventory`.

| Variable             | Description                                         |
| :------------------- | :-------------------------------------------------- |
| `_EquipmentSize`     | How many weapons can be equipped                    |
| `_InventorySize`     | How many item types the backpack can hold           |
| `_EquippedWeapons`   | Equipped weapon slots (empty slots are `null`)      |
| `_BackpackInventory` | `Dictionary<ItemSO, int>`                           |

Functions: `CheckInventoryFits`, `CheckEquippedWeaponFits`, `AddBackpackInventory`, `AddEquipmentInventory`.

*Ingredients and element runes are stored separately in the [[Inventory Manager]].*

---
## PlayerData
The component on the player that holds all of this.

| Variable             | Description                                      |
| :------------------- | :----------------------------------------------- |
| `pStats`             | `PlayerStats`                                    |
| `pInventory`         | `Inventory`                                      |
| `_TamerAbilities`    | Abilities shown in the [[Radial Menu]]           |
| `_StandardAbilities` | Other abilities                                  |

It implements:
- **`IUnitData`** - returns `pStats._Danger`
- **`IInventory`** - `AddItem` equips weapons into an empty slot and refreshes the held weapon. Other item types and `GetItemAmount`/`RemoveItem` aren't built yet [[Notes for the future]]

`EnemyStats` is the enemy-side stats component.
