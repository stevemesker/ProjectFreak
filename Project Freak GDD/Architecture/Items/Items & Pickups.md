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

Item types: `WeaponItem` / `WeaponRangedItem` (see [[Weapon usage]]), `IngredientItem`, `ElementItemSO`, `ShardItemSO`.

**`ShardItemSO`** *(Oct 11, 2026)*: a core fragment ("shard", see Core Fragments in [[Rune Field Overhaul Plan]]). `_Tier` (`ShardType.Tier`: Minor, Basic, Major, Perfect) and `_Power` (core power it adds). One asset per tier, made from **Create → ScriptableObjects → Items → Shard**, so shards stack like any item. Slotted into a shade with `ShadeManager.SlotShard` (see [[Shade Manager]]). Dropping shards when a shade dies or is broken down is part of the [[Fail State]] work [[Notes for the future]]. Shard assets are Steve's to make (Minor 1, Basic 3, Major 7, Perfect 15 to start)

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
