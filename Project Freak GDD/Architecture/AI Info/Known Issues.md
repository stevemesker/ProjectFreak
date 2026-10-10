Bugs and cleanup tasks Claude has noticed while reading the code. Nothing here has been changed yet. Check items off or delete them as they get fixed. Unchecked items count as [[Notes for the future]].

Back to [[AA - AI Info]]

---
## Bugs

**Damage** *(damage overhaul, Oct 2026)*
- [x] `PlayerDamegable.DamageCalculation` negates the defense stat and then subtracts it, so defense is **added** to damage instead of reducing it. *Fixed Oct 2026: both health scripts use the new shared `CombatTools.ResolveHit`*
- [x] `PlayerDamegable` doesn't implement `IDamagable`, so projectiles (including [[Traps]]) can't hit the player. *Checked Oct 2026: not actually a bug. The player prefab's `EnemyDamagable` is the `IDamagable`, and its `onDamage` event forwards to `PlayerDamegable`. Left as is so the player gets the same hit checks as every unit*
- [x] `CoreStats.GetAttackResistanceModifier` checks resistances but not immunities. *Fixed Oct 2026*
- [x] `TrapProjectileSpawner._CritMultiplier` is never used (the package always gets 1). *Fixed Oct 2026, traps also have a crit chance and a faux attack stat now*
- [x] `UnitDash.DashPassthrough` replaced the package's entries with an empty list, so pass-through dashes did no damage. *Fixed Oct 2026*

**Abilities**
- [ ] `AbilityInterpreter.ExecuteAbility` always waits using the **first** step's `Timing` instead of the current step's (see [[Ability System]])
- [ ] `ReleaseShade.FindSummonSpot` (was `SummonShade`): `smallestDistance` is never reset between directions, so one close wall in any direction makes every later direction fail too. It also has hardcoded search values (5 m, 2 m, etc.), uses `Vector3.zero` to mean "no spot found", and leaves debug `LogWarning`s running ("Found Myself...", "is summoning a shade", "Correct numbers on a flat plane"). *Spotted Oct 2026 during the shade forms work, left as is*

**Dungeons**
- [ ] `DungeonSO.SearchDictionaries` / `GetPOI` throw an error if no POI has the requested size or tag, instead of returning null (see [[POI System]])
- [ ] `DungeonManager.MoveToFloor` picks a random floor scene but doesn't save it on the node, so revisiting a floor can load a different layout
- [ ] `DungeonDoorSpawnerObject._CurrentDoors` is a static list, so door order depends on enable order. Worth checking this stays reliable across scene loads

**Rune Field** (see [[Rune Field System]])
*Oct 2026: the rune field was rebuilt on the new `RuneField` rules (see [[Rune Field System]]), so the old-script bugs below are gone with the old code.*
- [x] `CoreNode.ClearConnection` loops with `i = Count; i > 0; i++`, which goes out of range if the core has any connections. *Gone, `CoreNode` is a view now*
- [x] `RuneFieldManager.SaveRuneFieldPackage` creates one `NodePackage` outside the loop and adds it for every node, so every saved node entry ends up with the last node's values. *Fixed Oct 2026 (rune field bug pass)*
- [x] Dropping an element from the inventory list onto the field doesn't reduce the inventory count (it gets the element's data now). Needs to give runes back if the field isn't saved. *Fixed Oct 9, 2026 (overhaul step 4): runes count as pending in the draft, can't be placed past what the inventory has, and leave the inventory on save*
- [x] `ElementItemSO`'s `connectionsAllowed`, `connectionDistance`, and `powerNeeded` aren't applied to the rune. *Fixed Oct 2026, the new rules use them*
- [x] `ElementItemSO.TriggerElementEffects` only tags the first stat boost package with the rune object. *Fixed Oct 2026: it doesn't write the rune into the SO at all now (the SO is shared by every rune of that type, so they overwrote each other). `statBoostPackage._ElementConnect` was removed*
- [ ] `ShadeSlotManager` unlocks slots by player level instead of the shade slot count (`_SHA` / `tamerSlotLevel`)
- [x] Loaded runes never got their `CoreNode` set, so they couldn't give power back or turn their stats off when cut off, and dropping a new rune on the core after a load could throw a null error. *Fixed Oct 2026: `CoreNode.LoadReconnect` calls `ConnectNode` on each rune it reconnects*
- [x] `NodeBridge.SeverConnection` ran both "can I still reach the core" searches without clearing the checked flags in between, so in a loop of runes the second side could wrongly think it lost the core and shut down. *Fixed Oct 2026*
- [x] `ElementItem.BuildConnections` never enforced `connectionsMax`, so a rune dropped near several runes bridged to all of them. *Fixed Oct 2026*
- [x] Ability nodes only checked themselves when a rune was dropped on them: losing power didn't turn them off, powering a chain later didn't turn them on, turning off a node didn't shut down nodes it unlocked, and unplugging cleared lockouts even when it never activated or another node was also locking them. *Fixed Oct 2026: `RuneFieldManager.RefreshAllNodes` runs after every power change and plug/unplug, and lockouts are a count (`_LockoutCount`)*
- [x] Loading ignored the saved ability nodes, so runes reappeared on nodes without being plugged in. *Fixed Oct 2026: `EvolutionNode.LoadNodeState` restores them without re-firing the events*
- [x] The core's current power was loaded from the save, so leveling up didn't add power until a reset. *Fixed Oct 2026: it's rebuilt as max power minus what the runes use*
- [x] *Gone Oct 2026: the rules don't use colliders anymore and `UpdateScaler` does nothing.* `RuneFieldManager.UpdateScaler` only resizes colliders when zoomed out (scale < 1), so zooming back in leaves them small. It also sizes them from the rect width (143 on the rune prefab) while the prefab's collider radius is 50, so the first zoom-out tick makes them bigger, not smaller. 3D colliders already scale with their parent, so this function may not be needed at all. Needs a test in the Evolution UI scene before changing
- [x] Runes and layouts are written straight onto the `ShadeSO` assets at runtime (`_AlteredStats`, `_RuneFieldPackage`), so they stay changed in the editor after play mode. *Fixed Oct 2026: the [[Shade Manager]] keeps runtime copies of each slot's field and stats. Oct 9, 2026: saved fields go onto the slot asset again, this time **on purpose** (see [[Rune Field Overhaul Plan]], the slot is the source of truth). Use the slot's **Reset Slot** button to start over*
- [x] Picking a different shade slot threw away unsaved runes but kept their stat boosts. *Fixed Oct 2026: stats are worked out from the saved field only, so unsaved changes never touch them. Throwing away unsaved changes is on purpose (save button design)*
- [x] If the runes on a loaded field use more power than the core has, they kept their power and stats. *Fixed Oct 2026: power is handed out from scratch every time, so a smaller core just powers fewer runes*
- [ ] The [[Save Manager]] doesn't save shade slots to disk yet. *Since Oct 9, 2026 each slot's field stays on its slot asset in the editor, but a built game would lose it* (see [[Rune Field Overhaul Plan]], disk save comes later)
- [ ] No UI save button for the rune field yet (only the Odin **Save Current Field** button on `RuneFieldManager`). Hook a button to `RuneFieldManager.SaveRuneSlot`, and later a "save before leaving?" popup. *Oct 9, 2026: the code side is done (`_SaveButton`, `_Popup`, `RequestLeave`), the buttons and popup still need adding in the scene (see [[Rune Field System]], Unity setup still to do)*
- [x] Runes can't be taken off the field (`RuneField.RemoveRune` exists, but nothing in the UI calls it). *Fixed Oct 9, 2026: dropping a rune on the inventory list removes it*
- [ ] Nothing uses a slot's stats in combat yet: the released shade has no stats/health component (see [[Shade (Runtime)]]). *Since Oct 9, 2026 both shade forms hold their slot's runtime entry (live stats, current health), ready for one*

**Save**
- [ ] `SaveManager.SetCurrentActiveSaveSlot` has its range check backwards (`Count - 1 > index` should be `index > Count - 1`)

**Player**
- [x] `UnitDash.startDashEvent` is never invoked (only `endDashEvent` is). *Fixed Oct 2026: start event now fires, dashes are tracked in `_dashRoutine` so chained dashes don't overlap, and `OnDisable` ends a running dash*
- [ ] `UnitDash` keeps one shared `hitList`, so chaining a second dash before the first finishes throws away the first dash's hits (they never take damage)
- [x] `UnitDash` pass-through damage doesn't check teams, so a damaging dash would hit allies too. *Fixed Oct 2026: dashes use `CombatTools.CanHitTeam` like weapons*
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
- [ ] Old rune field stat path, unused since the Oct 2026 rebuild. *All removed Oct 9, 2026 (overhaul steps 1 and 2).* Still to delete in Unity: `Scripts/Common/RuneFieldPackage.cs` and `Scripts/Rune Field/RuneFieldLayoutSO.cs` (both emptied)
- [ ] `IConnectable`, `ICoreNode`, `iEvolutionNode` in `EvolutionInterfaces.cs` are unused. Keep `IBridgeable`: the dungeon map uses it (and `NodeBridge.BuildConnection`)
- [ ] `ElementDataObject` / `IngredientDataObject` `NodePrefabToSpawn` isn't used anymore

**Consistency**
- [x] Swap `StatNameType.Stat` and `ElementType.Element` over to the `DamageType` enums (see [[Notes for the future]]). *Done Oct 2026. `StatNameType.cs` and `ElementType.cs` can now be deleted in Unity*
- [x] Rename lowercase methods to PascalCase (see [[Code Style Rules]]). *Done Oct 2026, including the UnityEvent hookups in scenes, prefabs and element assets. `loadShadeSlectionIndex` also had its typo fixed (`LoadShadeSelectionIndex`)*
- [x] Give `ShadeManager` a singleton like the other managers. *Done Oct 2026 (`ShadeManager._ShadeManager`)*
- [ ] Make manager singleton setup consistent (destroy duplicates in `Awake`). *Skipped for now: all managers live on the one Game Manager object*
- [x] Add null checks to `SceneManagerWrapper` and `CameraManagerWrapper`. *Done Oct 2026*
- [x] Remove leftover debug prints (`print("boop")` in `PlayerMenuInputs`, scene load logs in `SceneManagerObject`, etc.). *Done Oct 2026. Kept `AbilityInterpreter.AbilIntLog` and the Odin test buttons. The legacy scripts above were left alone since they're getting deleted*
- [ ] A few classes still have lowercase names: `cameraScript` (legacy). *`statBoostPackage` is gone since Oct 9, 2026 (replaced by `StatChangeEffect`)*
- [x] **Remove WIS** *(damage overhaul)*. *Done Oct 2026: `_WIS` and `StatType.Wisdom` are gone, "Intelect" is now "Intellect", and every `DamageType` enum has explicit numbers so removing a value never shifts assets again. One trap in `New_Player_Movement_Scene` used Wisdom and was moved to Intellect*
- [x] **Swap `DamageType.ElementType` to the final element list** *(damage overhaul)*. *Done Oct 2026: Normal (0), Fire, Water, Air, Earth, Ice, Lava, Lightning, Plant, Void, Light. Weapon, rune element and trap assets were shifted to keep their element. The two dev enemy prefabs had `None` in their element resistance list, which was cleared (it would have become a Normal resistance)*
- [ ] There's a bug where the radial dial toggle becomes always on instead of always off until button is pressed. Something about long holding or switching windows with it up breaks the radial dial
