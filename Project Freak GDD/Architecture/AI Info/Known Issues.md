Bugs and cleanup tasks Claude has noticed while reading the code. Nothing here has been changed yet. Check items off or delete them as they get fixed.

Back to [[AA - AI Info]]

---
## Bugs

**Damage** *(save for the damage overhaul)*
- [ ] `PlayerDamegable.DamageCalculation` negates the defense stat and then subtracts it, so defense is **added** to damage instead of reducing it
- [ ] `PlayerDamegable` doesn't implement `IDamagable`, so projectiles (including [[Traps]]) can't hit the player
- [ ] `CoreStats.GetAttackResistanceModifier` checks resistances but not immunities
- [ ] `TrapProjectileSpawner._CritMultiplier` is never used (the package always gets 1)

**Abilities**
- [ ] `AbilityInterpreter.ExecuteAbility` always waits using the **first** step's `Timing` instead of the current step's (see [[Ability System]])

**Dungeons**
- [ ] `DungeonSO.SearchDictionaries` / `GetPOI` throw an error if no POI has the requested size or tag, instead of returning null (see [[POI System]])
- [ ] `DungeonManager.MoveToFloor` picks a random floor scene but doesn't save it on the node, so revisiting a floor can load a different layout
- [ ] `DungeonDoorSpawnerObject._CurrentDoors` is a static list, so door order depends on enable order. Worth checking this stays reliable across scene loads

**Rune Field** (see [[Rune Field System]])
- [ ] `CoreNode.ClearConnection` loops with `i = Count; i > 0; i++`, which goes out of range if the core has any connections
- [ ] `RuneFieldManager.SaveRuneFieldPackage` creates one `NodePackage` outside the loop and adds it for every node, so every saved node entry ends up with the last node's values
- [ ] Dropping an element from the inventory list onto the field spawns a rune but doesn't give it the element's data or reduce the inventory count
- [ ] `ElementItemSO`'s `connectionsAllowed`, `connectionDistance`, and `powerNeeded` aren't applied to the rune
- [ ] `ElementItemSO.triggerElementEffects` only tags the first stat boost package with the rune object
- [ ] `ShadeSlotManager` unlocks slots by player level instead of the shade slot count (`_SHA` / `tamerSlotLevel`)

**Save**
- [ ] `SaveManager.setCurrentActiveSaveSlot` has its range check backwards (`Count - 1 > index` should be `index > Count - 1`)

**Player**
- [ ] `Player.UseCurrentWeapon` / `releaseCurrentWeapon` check `handPointer` for `ITriggerable` instead of the held weapon, and the check is inverted

---
## Cleanup

**Legacy scripts** *(from a previous iteration, low priority)*
- [ ] Remove `PlayerMovement.cs` (replaced by `CharacterMovement`)
- [ ] Remove `FreakCharacter`, `FreakData`, `FreakInput`, `UnitBaseClass`, `Pawn`, `cameraScript`

**Empty template scripts** *(build out or delete)*
- [ ] `DungeonEnemyTableSO`, `DungeonLootTableSO` (also should inherit `ScriptableObject`, not `MonoBehaviour`)
- [ ] `GameManagerEventSO`
- [ ] `EnvironmentDamageable`
- [ ] `PlayerEquipment`, `ShadeSummoner`
- [ ] `DraggableItem`, `ElementObject`, `ShadeSlotDataObject` (early Rune Field leftovers)

**Consistency**
- [ ] Swap `StatNameType.Stat` and `ElementType.Element` over to the `DamageType` enums (see [[Notes for the future]])
- [ ] Rename lowercase methods to PascalCase (see [[Code Style Rules]])
- [ ] Give `ShadeManager` a singleton like the other managers
- [ ] Make manager singleton setup consistent (destroy duplicates in `Awake`)
- [ ] Add null checks to `SceneManagerWrapper` and `CameraManagerWrapper`
- [ ] Remove leftover debug prints (`print("boop")` in `PlayerMenuInputs`, scene load logs in `SceneManagerObject`, etc.)
