## Overview
`InventoryManager` stores the player's crafting inventory: **ingredients** and **element runes**. It listens for item pickups through an event channel and adds them to the right list.

Reached with `InventoryManager._PlayerInventory`.

> Weapons are **not** stored here. Equipped weapons live in the player's `Inventory` in `PlayerData` (see [[Stats & Inventory Data]]). The two inventory systems haven't been merged yet.

---
## Data

| Variable            | Description                                                                                     |
| :------------------ | :---------------------------------------------------------------------------------------------- |
| `pickupChannel`     | `PickupEventChannelSO` the manager listens to                                                   |
| `playerIngredients` | `Dictionary<IngredientItem, int>` of ingredients and counts                                     |
| `playerElements`    | `Dictionary<ElementItemSO, int>` of element runes and counts (used by the [[Rune Field]])       |
| `ItemStackSizeMax`  | Max count for any single item                                                                   |
| `OnInventoryChanged` | C# event fired when the inventory changes, so UI can refresh                                   |

`Ingredients` and `Elements` are read-only versions of the dictionaries for other scripts to read.

---
## Pickup Flow

```text
Something raises PickupEventChannelSO.Raise(item, amount, source)
    ↓
HandlePickup()
    ├── IngredientItem → addIngredient()
    ├── ElementItemSO  → addElement()
    └── WeaponItem     → (not handled here)
    ↓
OnInventoryChanged
```

*Note:* `ItemDrop.pickupItem()` (which raises this event) is marked deprecated. Player pickups now go through `PlayerPickup` and `IInventory` instead (see [[Items & Pickups]]), so this path is mostly used for testing right now.

---
## Functions

| Function                                | Description                                                             |
| :-------------------------------------- | :---------------------------------------------------------------------- |
| `addIngredient(item, amount)`           | Adds to the stack, capped at `ItemStackSizeMax`                         |
| `removeIngredient(item, amount)`        | Removes from the stack, deleting it at 0                                |
| `checkIngredient(item)`                 | Returns how many the player has                                         |
| `addElement(item, amount)`              | Adds element runes, capped at `ItemStackSizeMax`                        |

Most functions have Odin buttons for testing.
