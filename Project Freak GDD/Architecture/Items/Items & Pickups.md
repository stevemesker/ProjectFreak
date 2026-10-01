## Overview
How items exist in the world and get picked up.

```text
ItemSO (data)
    ↓ spawned as
ItemDrop (in the world, IPickup)
    ↓ player walks close + presses Pickup
PlayerPickup
    ↓
PlayerData (IInventory).AddItem()
```

---
## ItemSO
The abstract base for all item data.

| Variable           | Description                                                           |
| :----------------- | :-------------------------------------------------------------------- |
| `ItemID`           | ID unique within its item type (weapon and ingredient IDs can overlap) |
| `ItemName`         | Name                                                                  |
| `ItemDescription`  | Description                                                           |
| `dropArt`          | Art spawned when the item is dropped in the world                     |
| `ItemRarity`       | Rarity                                                                |
| `itemStackSizeMax` | Max stack (default 99)                                                |

Item types: `WeaponItem` / `WeaponRangedItem` (see [[Weapon usage]]), `IngredientItem`, `ElementItemSO`.

---
## ItemDrop
An item lying in the world. Needs a collider and the **Item** tag.

| Variable         | Description                                     |
| :--------------- | :---------------------------------------------- |
| `ItemLootDrop`   | The `ItemSO`                                    |
| `ItemLootAmount` | How many                                        |
| `_ArtParent`     | Child object the item's art spawns under        |

- `FillDrop(item)` spawns the art. Weapons get offset and turn on `ItemFloatAndSpin`
- `MoveArc(location, height, speed)` launches it with [[ArcMover]] (used by [[Chest Content]])
- Implements `IPickup`: `GetObject`, `GetAmount`, `TakeObjectAmount` (destroys the drop when empty)
- `PickupItem()` and `RemoveItemInventory()` are the older event channel path, marked deprecated

---
## PlayerPickup
On the player, with a trigger collider.
- Items tagged **Item** that are weapons get added to an in-range list when the player gets close
- Pressing **Pickup** grabs the closest one, calls `AddItem` on the player's `IInventory`, and takes however many fit
- *Non-weapon items are detected but not picked up yet*

---
## PickupEventChannelSO
A ScriptableObject event: `Raise(item, amount, source)` fires `OnPickup`. The [[Inventory Manager]] listens to it.
