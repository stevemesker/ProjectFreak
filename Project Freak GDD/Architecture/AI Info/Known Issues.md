Bugs and cleanup tasks Claude has noticed while reading the code. Nothing here has been changed yet. Check items off or delete them as they get fixed. Unchecked items count as [[Notes for the future]].

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
- [ ] `ElementItemSO.TriggerElementEffects` only tags the first stat boost package with the rune object
- [ ] `ShadeSlotManager` unlocks slots by player level instead of the shade slot count (`_SHA` / `tamerSlotLevel`)

**Save**
- [ ] `SaveManager.SetCurrentActiveSaveSlot` has its range check backwards (`Count - 1 > index` should be `index > Count - 1`)

**Player**
- [x] `UnitDash.startDashEvent` is never invoked (only `endDashEvent` is). *Fixed Oct 2026: start event now fires, dashes are tracked in `_dashRoutine` so chained dashes don't overlap, and `OnDisable` ends a running dash*
- [ ] `UnitDash` keeps one shared `hitList`, so chaining a second dash before the first finishes throws away the first dash's hits (they never take damage)
- [ ] `UnitDash` pass-through damage doesn't check teams, so a damaging dash would hit allies too. Projectiles and hit scan skip allies through `UnitTeam.IsHostileTo` (Oct 2026); dashes should do the same
- [x] `Player.UseCurrentWeapon` / `ReleaseCurrentWeapon` check `handPointer` for `ITriggerable` instead of the held weapon, and the check is inverted. *Fixed Oct 2026: `Player` keeps a reference to the weapon it spawned, and `PlayerCombatInteract` goes through these functions instead of its own copy of the check*
- [ ] `PlayerCombatInteract.EndSelection` calls `StopCoroutine(cycleTimer)` without checking for null, which logs an error if the scroll is released without a cycle running

---
## Cleanup

**Legacy scripts** *(from a previous iteration, low priority)*
- [ ] Remove `PlayerMovement.cs` (replaced by `CharacterMovement`)
- [ ] Remove `FreakCharacter`, `FreakData`, `FreakInput`, `UnitBaseClass`, `Pawn`, `cameraScript`. *Oct 2026: all but `cameraScript` were emptied during the ranged weapon overhaul (they read old `WeaponItem` fields). Delete the empty files in Unity, plus `PFB_Character_Freak_Dev`, which will show a missing script*

**Empty template scripts** *(build out or delete)*
- [x] `DungeonEnemyTableSO` *(built Oct 2026, now a `ScriptableObject`. See [[Enemy Spawners]])*
- [ ] `DungeonLootTableSO` (also should inherit `ScriptableObject`, not `MonoBehaviour`)
- [ ] `GameManagerEventSO`
- [ ] `EnvironmentDamageable`
- [ ] `PlayerEquipment`, `ShadeSummoner`
- [ ] `DraggableItem`, `ElementObject`, `ShadeSlotDataObject` (early Rune Field leftovers)

**Consistency**
- [x] Swap `StatNameType.Stat` and `ElementType.Element` over to the `DamageType` enums (see [[Notes for the future]]). *Done Oct 2026. `StatNameType.cs` and `ElementType.cs` can now be deleted in Unity*
- [x] Rename lowercase methods to PascalCase (see [[Code Style Rules]]). *Done Oct 2026, including the UnityEvent hookups in scenes, prefabs and element assets. `loadShadeSlectionIndex` also had its typo fixed (`LoadShadeSelectionIndex`)*
- [x] Give `ShadeManager` a singleton like the other managers. *Done Oct 2026 (`ShadeManager._ShadeManager`)*
- [ ] Make manager singleton setup consistent (destroy duplicates in `Awake`). *Skipped for now: all managers live on the one Game Manager object*
- [x] Add null checks to `SceneManagerWrapper` and `CameraManagerWrapper`. *Done Oct 2026*
- [x] Remove leftover debug prints (`print("boop")` in `PlayerMenuInputs`, scene load logs in `SceneManagerObject`, etc.). *Done Oct 2026. Kept `AbilityInterpreter.AbilIntLog` and the Odin test buttons. The legacy scripts above were left alone since they're getting deleted*
- [ ] A few classes still have lowercase names: `statBoostPackage` (in `ElementManagerSO.cs`) and `cameraScript` (legacy). Renaming `statBoostPackage` is safe for saved data since it's a plain serializable class
- [ ] **Remove WIS** *(damage overhaul, Oct 2026 decision, see [[Character Stats]])*: delete `_WIS` from `CoreStats` and the WIS value from `DamageType.StatType`, and update `TypeToStatFinder`, `ShadeManager.ChangeStat` and anything else that switches on stats. `StatType` is saved as an int on assets (rune stat boosts, weapons), so removing a value shifts everything after it. Either shift those assets like the last enum cleanup, or give the enum explicit numbers first so this never happens again. Also rename the "Intelect" spelling in the stat pointers while there
- [ ] **Swap `DamageType.ElementType` to the final element list** *(damage overhaul, Oct 2026 decision, see [[Elemental Affinity]])*: Normal, Fire, Water, Air, Earth, Ice, Lava, Lightning, Plant, Void, Light. Cut `None` (Normal is the default and means non-elemental), rename Electric → Lightning and Dark → Void, add Air, Lava and Plant, and remove Poison (future status effect) and Healing (healing gets its own path later). Do this in the same pass as Remove WIS. Nothing much is built on elements yet, so shifting assets isn't a concern, but explicit enum numbers are still worth adding
- [ ] There's a bug where the radial dial toggle becomes always on instead of always off until button is pressed. Something about long holding or switching windows with it up breaks the radial dial
