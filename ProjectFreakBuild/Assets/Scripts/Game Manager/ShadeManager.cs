using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

public class ShadeManager : MonoBehaviour
{
    public static ShadeManager _ShadeManager;

    [Header("Pointer")]
    [Tooltip("Prefab spawned when the shade is tethered (PFB_Shade_Tethered). Needs a TetheredShade on its root")]
    [SerializeField] GameObject _TetheredPrefab;

    [Tooltip("Prefab spawned when the shade is released (PFB_Shade_Released). Needs a ReleasedShade on its root")]
    [FormerlySerializedAs("_ShadePrefab")] //this field used to be _ShadePrefab, this keeps the prefab already assigned
    [SerializeField] GameObject _ReleasedPrefab;

    [Header("Runtime Data")]
    [Tooltip("The tethered shade currently out. Only one form is ever out, so this and Released Shade are never both filled (read only)")]
    [SerializeField, ReadOnly] TetheredShade _TetheredShade;

    [Tooltip("The released shade currently out (read only)")]
    [SerializeField, ReadOnly] ReleasedShade _ReleasedShade;

    [Tooltip("True while the player is driving the released shade (read only)")]
    [SerializeField, ReadOnly] bool _IsControllingShade;


    [Header("All possible shade slots")]
    public List<ShadeSO> _ShadeSlots;

    [Header("How many shade slots the player has access to")]
    public int tamerSlotLevel;

    [SerializeField] int currentShadeSelected;

    [Tooltip("The form every shade starts in. A slot's Reset Slot button puts it back to this (in play mode), and dying will too later")]
    [SerializeField] ShadeEvolutionSO _BoundEvolution;

    [Header("Rune Fields")]
    [Tooltip("The rune field numbers every slot shares (SO_RuneField_Settings): core reach, snap radius, zone width, rune size. If empty, the defaults are used and a warning is logged")]
    [SerializeField] RuneFieldSettingsSO _RuneFieldSettings;

    [Tooltip("Each slot's runtime entry (read only): hard stats, live stats (live Health = current health) and abilities. Summoned shades point at these, so health carries over when a shade is put away")]
    [ShowInInspector, ReadOnly] //ShowInInspector shows it without Unity saving it. It's runtime only, and saving it could make Unity swap in copies the shades don't point at
    List<ShadeRuntimeEntry> _RuntimeEntries = new List<ShadeRuntimeEntry>();

    public event System.Action<int> OnShadeEvolved; //fires with the slot index after a shade evolves (from a save or a level-up). The rune field listens to reload

    [Header("Local Variables")]
    [SerializeField] float _switchTimeIn = .5f;
    [SerializeField] float _switchTimeOut = .3f;
    Coroutine _transferTimer;
    [SerializeField] bool isBusy;

    private void Awake()
    {
        //sets up the singleton so other scripts can reach this manager with ShadeManager._ShadeManager
        if (_ShadeManager == null) _ShadeManager = this;

        if (_RuneFieldSettings == null)
        {
            Debug.LogWarning($"Warning! No Rune Field Settings assigned on {gameObject.name}, using the default settings...", this);
            _RuneFieldSettings = ScriptableObject.CreateInstance<RuneFieldSettingsSO>(); //a temporary asset with the default values, not saved anywhere
        }

        InitializeRuntimeEntries();
    }

    #region Summoning
    public void TetherShade()
    {
        //function the Tether Shade ability calls. Tethered out: put it away. Released out: swap it for a tethered one. Nothing out: tether one
        if (isBusy) return;

        if (_TetheredShade != null)
        {
            ReturnShade();
            return;
        }

        if (CanSummon() == false) return;
        ReturnReleased();
        SpawnTethered();
    }

    public void ReleaseShade(Vector3 offset)
    {
        //function the Release Shade ability calls with a spot next to the player. Released out: put it away. Tethered out: swap it for a released one
        if (isBusy) return;

        if (_ReleasedShade != null)
        {
            ReturnShade();
            return;
        }

        if (CanSummon() == false) return;
        ReturnTethered();
        SpawnReleased(offset);
    }

    public void ReturnShade()
    {
        //function the Return Shade ability calls. Puts away whichever form is out
        if (isBusy) return;
        ReturnTethered();
        ReturnReleased();
    }

    public void ReturnReleased()
    {
        //function that puts away only the released shade. Also called by DungeonManager.EnterDungeon, since released shades don't come into dungeons
        if (_ReleasedShade == null) return;
        if (IsControlTiedToShade()) GiveControlBackToPlayer(); //don't leave the player stuck driving a shade that's gone

        ReleasedShade leaving = _ReleasedShade;
        _ReleasedShade = null;
        leaving.Dismiss();
    }

    void ReturnTethered()
    {
        //function that puts away only the tethered shade
        if (_TetheredShade == null) return;

        TetheredShade leaving = _TetheredShade;
        _TetheredShade = null;
        leaving.Dismiss();
    }

    public bool CanSummon()
    {
        //the one place that decides if the current slot's shade can come out in any form
        //todo: health and lives (Fail State) plug in here, so a shade with nothing left can't be summoned
        if (Player.player == null) { Debug.LogError("Error! No player found, there's nobody to summon a shade for", this); return false; }
        if (currentShadeSelected < 0 || currentShadeSelected >= _ShadeSlots.Count) { Debug.LogError($"Error! Shade slot {currentShadeSelected} doesn't exist on {gameObject.name}", this); return false; }
        if (_ShadeSlots[currentShadeSelected] == null) { Debug.LogError($"Error! Shade slot {currentShadeSelected} is empty on {gameObject.name}", this); return false; }
        return true;
    }

    public ShadeFormType.Form GetCurrentForm()
    {
        //function for checking which form (if any) the shade is out in
        if (_TetheredShade != null) return ShadeFormType.Form.Tethered;
        if (_ReleasedShade != null) return ShadeFormType.Form.Released;
        return ShadeFormType.Form.None;
    }

    void SpawnTethered()
    {
        //spawns the tethered prefab next to the player and hands it the slot data
        if (_TetheredPrefab == null) { Debug.LogError($"Error! Tethered prefab not assigned on {gameObject.name}", this); return; }

        GameObject shadeObject = Instantiate(_TetheredPrefab, Player.player.transform.position, Player.player.transform.rotation);
        if (shadeObject.TryGetComponent(out TetheredShade tethered) == false)
        {
            Debug.LogError($"Error! {_TetheredPrefab.name} has no TetheredShade on its root", this);
            Destroy(shadeObject);
            return;
        }

        _TetheredShade = tethered;
        tethered.Setup(GetCurrentShade(), Player.player.gameObject);
        tethered.Appear();
    }

    void SpawnReleased(Vector3 offset)
    {
        //spawns the released prefab at the spot the ability found, hands it the slot data, and lets it act on its own
        if (_ReleasedPrefab == null) { Debug.LogError($"Error! Released prefab not assigned on {gameObject.name}", this); return; }

        GameObject shadeObject = Instantiate(_ReleasedPrefab, Player.player.transform.position + offset, Quaternion.identity);
        if (shadeObject.TryGetComponent(out ReleasedShade released) == false)
        {
            Debug.LogError($"Error! {_ReleasedPrefab.name} has no ReleasedShade on its root", this);
            Destroy(shadeObject);
            return;
        }

        _ReleasedShade = released;
        released.Setup(GetCurrentShade(), Player.player.gameObject);
        released.Appear();
        ControlShade(false); //the player keeps control, the shade runs on its AI
    }

    public void OnReleasedShadeGone(ReleasedShade shade)
    {
        //called by the released shade when it's destroyed. Covers scene changes, which remove it without going through ReturnReleased
        if (_ReleasedShade != null && _ReleasedShade != shade) return; //a different shade, ignore it
        _ReleasedShade = null;
        if (IsControlTiedToShade()) GiveControlBackToPlayer();
    }
    #endregion

    #region ShadeControl
    [Button("Control Shade")]
    public void ShadeControlAbility()
    {
        //toggles control: driving the shade? go back to the player. Otherwise take over the released shade
        //todo: while controlling, the radial menu should show the shade's abilities instead of the player's (system for later)
        if (isBusy) return;

        GameObject controlTarget;
        if (_IsControllingShade)
        {
            if (Player.player == null) { Debug.LogError("Error! No player found to switch control back to", this); return; }
            controlTarget = Player.player.gameObject;
        }
        else
        {
            if (_ReleasedShade == null) return; //only the released shade can be controlled
            controlTarget = _ReleasedShade.gameObject;
        }

        isBusy = true;
        _transferTimer = StartCoroutine(ShadeControlSwitch(_switchTimeIn, _switchTimeOut, controlTarget));
    }

    public void ControlShade(bool control)
    {
        _IsControllingShade = control;
        if (control)
        {
            Player.player.DisablePlayerControl();
            if (_ReleasedShade != null) _ReleasedShade.EnablePlayerControl();
        }
        else
        {
            Player.player.EnablePlayerControl();
            if (_ReleasedShade != null) _ReleasedShade.EnableShadeControl();
        }
        
    }

    bool IsControlTiedToShade()
    {
        //true while the player is driving the shade, or a control switch is fading over to it
        return _IsControllingShade || _transferTimer != null;
    }

    void GiveControlBackToPlayer()
    {
        //function that puts the player straight back in their own body (no fade), for when the controlled shade goes away
        if (_transferTimer != null) StopCoroutine(_transferTimer);
        _transferTimer = null;
        isBusy = false;

        if (Player.player == null) return; //game is shutting down
        ControlShade(false);
        if (CameraManager._CamManager != null) CameraManager._CamManager.SetCamTargetToPlayer();
        if (HUDManager._HUD != null) HUDManager._HUD.FadeIn(_switchTimeOut); //in case it was mid fade-out
    }

    IEnumerator ShadeControlSwitch(float easeOutTime, float easeInTime, GameObject controlTarget)
    {
        HUDManager._HUD.FadeOut(easeOutTime);
        yield return new WaitForSeconds(easeOutTime + .5f);

        if (controlTarget == Player.player.gameObject)
        {
            CameraManager._CamManager.SetCamTargetToPlayer(); //back to the normal player camera setup
            ControlShade(false); //player is back in their own body
        }
        else
        {
            CameraManager._CamManager.SetCamTargetToTarget(controlTarget);
            ControlShade(true); //player is controlling the shade
        }

        HUDManager._HUD.FadeIn(easeInTime);
        yield return new WaitForSeconds(easeInTime + .1f);
        isBusy = false;
        _transferTimer = null;
    }

    #endregion

    #region get shade info
    public ShadeSO GetCurrentShade()
    {
        return _ShadeSlots[currentShadeSelected];
    }

    public ShadeSO GetShadeOfIndex(int index)
    {
        return _ShadeSlots[index];
    }
    #endregion

    #region Rune Fields
    public bool IsValidSlot(int slotIndex)
    {
        //checks a slot index points at a real slot
        return slotIndex >= 0 && slotIndex < _ShadeSlots.Count && _ShadeSlots[slotIndex] != null;
    }

    public RuneFieldData GetSavedRuneField(int slotIndex)
    {
        //returns a copy of a slot's saved rune field for the rune field UI to edit. Editing the copy doesn't change the saved one
        if (IsValidSlot(slotIndex) == false) { Debug.LogError($"Error! Shade slot {slotIndex} doesn't exist on {gameObject.name}, can't load its rune field", this); return new RuneFieldData(); }
        return _ShadeSlots[slotIndex].GetRuneFieldCopy();
    }

    public bool SaveRuneField(int slotIndex, RuneField field)
    {
        //function the rune field UI calls when the player saves. Returns false if nothing was saved
        //1. the inventory changes by the difference between the old and new saved field (added runes come out, removed runes go back)
        //2. the field, everything it does (compiled effects) and the nodes it plugged into (snapshots) are written onto the slot
        //3. if a gate in the open zone has power, the shade evolves
        //4. the slot's runtime entry is rebuilt
        if (IsValidSlot(slotIndex) == false) { Debug.LogError($"Error! Shade slot {slotIndex} doesn't exist on {gameObject.name}, can't save its rune field", this); return false; }
        if (field == null) return false;
        ShadeSO slot = _ShadeSlots[slotIndex];
        if (field.GetRank() != slot.GetRank())
        {
            //the shade evolved while this draft was open, so the draft still thinks the old zone is open. Saving it could move frozen runes
            Debug.LogWarning($"Warning! {slot.name} evolved while its rune field was open, this draft is out of date. Nothing was saved...", this);
            return false;
        }

        Dictionary<ElementItemSO, int> oldCounts = slot._RuneField != null ? slot._RuneField.CountRunes() : new Dictionary<ElementItemSO, int>();
        Dictionary<ElementItemSO, int> newCounts = field.GetData().CountRunes();
        if (CanAffordRuneChanges(oldCounts, newCounts) == false) return false;
        ApplyRuneChanges(oldCounts, newCounts);

        field.SetMaxPower(GetCorePower(slotIndex)); //in case the shade leveled while the field was open, so a stale draft can't save the wrong power
        slot.SaveRuneField(field.GetData(), field.CompileEffects(), field.GetPluggedNodeSnapshots());
        bool evolved = TryEvolve(slotIndex);
        RebuildSlot(slotIndex);
        if (evolved) FinishEvolving(slotIndex);
        return true;
    }

    bool CanAffordRuneChanges(Dictionary<ElementItemSO, int> oldCounts, Dictionary<ElementItemSO, int> newCounts)
    {
        //checks the inventory has enough of every rune the new field adds. Checked before anything changes, so a failed save changes nothing
        //the rune field UI already refuses runes the player doesn't have, so this only fails on bad data
        if (InventoryManager._PlayerInventory == null) return true; //no inventory (testing a scene on its own), ApplyRuneChanges warns about it

        foreach (KeyValuePair<ElementItemSO, int> entry in newCounts)
        {
            int added = entry.Value - GetCount(oldCounts, entry.Key);
            if (added <= 0) continue;

            if (InventoryManager._PlayerInventory.GetElementCount(entry.Key) < added)
            {
                Debug.LogError($"Error! Can't save the rune field: it adds {added} {entry.Key.ItemName} but the inventory only has {InventoryManager._PlayerInventory.GetElementCount(entry.Key)}. Nothing was saved", this);
                return false;
            }
        }
        return true;
    }

    void ApplyRuneChanges(Dictionary<ElementItemSO, int> oldCounts, Dictionary<ElementItemSO, int> newCounts)
    {
        //takes added runes out of the inventory and puts removed runes back in
        if (InventoryManager._PlayerInventory == null) { Debug.LogWarning($"Warning! No Inventory Manager found, saving the rune field without changing the inventory...", this); return; }

        //runes on the new field: take out whatever was added
        foreach (KeyValuePair<ElementItemSO, int> entry in newCounts)
        {
            int added = entry.Value - GetCount(oldCounts, entry.Key);
            if (added > 0) InventoryManager._PlayerInventory.RemoveElement(entry.Key, added);
        }

        //runes on the old field: give back whatever was removed
        foreach (KeyValuePair<ElementItemSO, int> entry in oldCounts)
        {
            int removed = entry.Value - GetCount(newCounts, entry.Key);
            if (removed > 0) InventoryManager._PlayerInventory.AddElement(entry.Key, removed); //AddElement warns if this goes past the stack cap
        }
    }

    int GetCount(Dictionary<ElementItemSO, int> counts, ElementItemSO element)
    {
        //how many of a rune a count has, 0 if it's not in there
        if (counts.TryGetValue(element, out int count)) return count;
        return 0;
    }

    public int GetSavedRuneCount(int slotIndex, ElementItemSO element)
    {
        //how many of a rune a slot's saved field has. The rune field UI uses this to work out how many a draft is using on top
        if (IsValidSlot(slotIndex) == false || _ShadeSlots[slotIndex]._RuneField == null) return 0;
        return _ShadeSlots[slotIndex]._RuneField.CountRunes(element);
    }

    void RerunSavedField(int slotIndex)
    {
        //runs the rules again on a slot's saved field with its current core power, using its node snapshots (no field scene needed)
        //runes that were waiting for power light up, nodes they reach switch on, and the slot's saved field and compiled list are replaced
        //frozen runes get their power first (see RuneField.CalculatePower)
        ShadeSO slot = _ShadeSlots[slotIndex];
        RuneField field = BuildSavedField(slot);
        slot.SaveRuneField(field.GetData(), field.CompileEffects(), field.GetPluggedNodeSnapshots());
    }

    RuneField BuildSavedField(ShadeSO slot)
    {
        //makes the rules for a slot's saved field (a copy) from its node snapshots, with its current core power and rank. No field scene needed
        return new RuneField(slot.GetRuneFieldCopy(), _RuneFieldSettings, slot._PluggedNodes, slot.GetCorePower(), slot.GetRank());
    }

    public int GetRank(int slotIndex)
    {
        //how many times a slot's shade has evolved (Bound = 0)
        if (IsValidSlot(slotIndex) == false) return 0;
        return _ShadeSlots[slotIndex].GetRank();
    }

    public int GetCorePower(int slotIndex)
    {
        //how much power a slot's core has: its level plus its fragment
        if (IsValidSlot(slotIndex) == false) return 0;
        return _ShadeSlots[slotIndex].GetCorePower();
    }

    public RuneFieldSettingsSO GetRuneFieldSettings()
    {
        return _RuneFieldSettings;
    }

    public ShadeEvolutionSO GetBoundEvolution()
    {
        //the form every shade starts in (null if it isn't set)
        return _BoundEvolution;
    }
    #endregion

    #region Evolving
    bool TryEvolve(int slotIndex)
    {
        //function that evolves a slot's shade if a gate in its open zone has power (its saved field, so this works anywhere, even mid-dungeon)
        //the gate is recorded (rank goes up) and the slot swaps to the gate's form, then the field runs again so the zone it left counts as frozen
        //returns true if it evolved. The caller rebuilds the runtime entry and then calls FinishEvolving
        ShadeSO slot = _ShadeSlots[slotIndex];
        RuneField field = BuildSavedField(slot);

        int gate = field.GetEvolvingGate();
        if (gate == -1) return false;

        ShadeEvolutionSO evolution = field.GetNode(gate).GetEvolution();
        if (evolution == null) { Debug.LogError($"Error! {slot.name}'s gate {field.GetNode(gate)._Name} has power but no evolution set on its Evolve effect, it can't evolve", this); return false; }

        slot.Evolve(gate, evolution);
        RerunSavedField(slotIndex);
        return true;
    }

    void FinishEvolving(int slotIndex)
    {
        //after an evolve and the rebuild: swaps the art on the shade if it's out right now, then tells listeners (the rune field reloads)
        if (slotIndex == currentShadeSelected)
        {
            if (_TetheredShade != null) _TetheredShade.RefreshForm();
            if (_ReleasedShade != null) _ReleasedShade.RefreshForm();
        }
        OnShadeEvolved?.Invoke(slotIndex);
    }
    #endregion

    #region Runtime Entries
    void InitializeRuntimeEntries()
    {
        //builds every slot's runtime entry when the game starts. Shades start the session at full health
        _RuntimeEntries.Clear();
        for (int i = 0; i < _ShadeSlots.Count; i++)
        {
            _RuntimeEntries.Add(null);
            RebuildSlot(i);
        }
    }

    public void RebuildSlot(int slotIndex)
    {
        //works a slot's runtime entry out again from its slot asset. Call this whenever something deliberate changes the slot
        //(saving its field, leveling, evolving, and later slotting a shard). Also on the Shade Manager Wrapper for events
        //the same entry object is kept and filled in again, so summoned shades pointing at it stay pointed at it
        if (IsValidSlot(slotIndex) == false || slotIndex >= _RuntimeEntries.Count) return;
        ShadeSO slot = _ShadeSlots[slotIndex];

        ShadeRuntimeEntry entry = _RuntimeEntries[slotIndex];
        bool firstBuild = entry == null;
        if (firstBuild)
        {
            entry = new ShadeRuntimeEntry();
            _RuntimeEntries[slotIndex] = entry;
        }

        int oldMaxHealth = entry.GetMaxHealth();
        int oldHealth = entry.GetCurrentHealth();

        entry._SlotName = slot.name;
        entry._HardStats = BuildHardStats(slot);
        entry._Abilities = BuildAbilityList(slot);

        //max HP rule: when max HP changes, current HP moves by the same amount (and can't go above the new max or below 0)
        int newMaxHealth = entry._HardStats._HP;
        int newHealth;
        if (firstBuild) newHealth = newMaxHealth; //first build of the session: full health
        else if (oldHealth <= 0) newHealth = 0; //an already fallen shade stays down, it doesn't get revived by a stat change
        else newHealth = oldHealth + (newMaxHealth - oldMaxHealth);
        newHealth = Mathf.Clamp(newHealth, 0, Mathf.Max(0, newMaxHealth));

        if (firstBuild == false && oldHealth > 0 && newHealth <= 0)
        {
            //todo: the shade falls here (Fail State work). Even a shade that's put away can fall this way
            Debug.LogWarning($"Warning! {slot.name}'s max HP dropped low enough to take its health to 0. Falling isn't built yet, it just stays at 0...", this);
        }

        RebuildLiveStats(entry, newHealth);
    }

    void RebuildLiveStats(ShadeRuntimeEntry entry, int currentHealth)
    {
        //live stats = hard stats with the current health. todo: timed effects (buffs, debuffs, potions) and equipment get added here later
        entry._LiveStats = CopyStats(entry._HardStats);
        entry._LiveStats._Health = currentHealth;
    }

    public ShadeRuntimeEntry GetRuntimeEntry(int slotIndex)
    {
        //returns a slot's runtime entry, or null if the slot doesn't exist
        if (IsValidSlot(slotIndex) == false || slotIndex >= _RuntimeEntries.Count) return null;
        return _RuntimeEntries[slotIndex];
    }

    public ShadeRuntimeEntry GetRuntimeEntry(ShadeSO slot)
    {
        //same as above, found by the slot asset (what the summoned shades are handed)
        if (slot == null) return null;
        return GetRuntimeEntry(_ShadeSlots.IndexOf(slot));
    }

    public ShadeStats GetSlotStats(int slotIndex)
    {
        //returns a slot's live stats (its Health is its current health)
        ShadeRuntimeEntry entry = GetRuntimeEntry(slotIndex);
        if (entry == null) return null;
        return entry._LiveStats;
    }

    public ShadeStats GetCurrentShadeStats()
    {
        return GetSlotStats(currentShadeSelected);
    }
    #endregion

    #region Health
    public void Heal(int slotIndex, int amount)
    {
        //heals a slot's shade, never above its max HP. A fallen shade (0 health) isn't healed back up, that's for the Fail State work
        ShadeRuntimeEntry entry = GetRuntimeEntry(slotIndex);
        if (entry == null || amount <= 0) return;
        if (entry.GetCurrentHealth() <= 0) return;

        entry._LiveStats._Health = Mathf.Min(entry.GetCurrentHealth() + amount, entry.GetMaxHealth());
    }

    public void RefillHealth(int slotIndex)
    {
        //puts a slot's shade back to full health (entering the hub, the rest floor refill)
        //todo: nothing calls this on entering the hub yet, there's no "entered the hub" moment in the code
        ShadeRuntimeEntry entry = GetRuntimeEntry(slotIndex);
        if (entry == null) return;
        entry._LiveStats._Health = entry.GetMaxHealth();
    }

    public void RefillAllHealth()
    {
        //puts every slot's shade back to full health
        for (int i = 0; i < _RuntimeEntries.Count; i++)
        {
            RefillHealth(i);
        }
    }
    #endregion

    #region Leveling
    public void SetSlotLevel(int slotIndex, int level)
    {
        //changes a slot's level (minimum 1), runs its saved rune field again with the new core power, then rebuilds its runtime entry
        //if the extra power reaches a plugged gate, the shade evolves right here (even mid-fight)
        //works anywhere, including mid-dungeon, since it doesn't need the rune field scene
        if (IsValidSlot(slotIndex) == false) { Debug.LogError($"Error! Shade slot {slotIndex} doesn't exist on {gameObject.name}, can't change its level", this); return; }

        _ShadeSlots[slotIndex].SetLevel(level);
        RerunSavedField(slotIndex);
        bool evolved = TryEvolve(slotIndex);
        RebuildSlot(slotIndex);
        if (evolved) FinishEvolving(slotIndex);
    }

    public void AddLevels(int slotIndex, int amount)
    {
        //adds (or takes away, if negative) levels from a slot
        if (IsValidSlot(slotIndex) == false) { Debug.LogError($"Error! Shade slot {slotIndex} doesn't exist on {gameObject.name}, can't change its level", this); return; }
        SetSlotLevel(slotIndex, _ShadeSlots[slotIndex]._Level + amount);
    }
    #endregion

    #region Stat Calculation
    ShadeStats BuildHardStats(ShadeSO slot)
    {
        //works a slot's hard stats out from scratch: its evolution's base stats plus every stat change in its saved (compiled) effect list
        //nothing is added or taken away bit by bit, so the stats can't drift out of sync with the runes
        if (slot._CurrentEvolution == null) Debug.LogWarning($"Warning! {slot.name} has no evolution, its base stats are all 0...", this);
        ShadeStats result = CopyStats(slot._CurrentEvolution != null ? slot._CurrentEvolution._BaseStats : null);

        //the personal parts come from the slot, not the evolution
        result._LVL = slot._Level;
        result._XP = slot._XP;
        result._LIFE = slot._Lives;

        Dictionary<DamageType.StatType, int> totals = RuneEffect.AddUpStats(slot._CompiledEffects);

        //todo: Hazen's share of the runes (see Element Rune in the GDD)
        foreach (KeyValuePair<DamageType.StatType, int> entry in totals)
        {
            AddToStat(result, entry.Key, entry.Value);
        }

        result._Health = result._HP; //in hard stats Health just matches max. The shade's current health lives in its live stats
        return result;
    }

    List<AbilitySO> BuildAbilityList(ShadeSO slot)
    {
        //every ability the shade has: its evolution's natural abilities and ultimate, plus every Grant Ability effect in its saved compiled list
        //repeats are only listed once. Nothing reads this yet, shades use their abilities later
        List<AbilitySO> abilities = new List<AbilitySO>();

        ShadeEvolutionSO evolution = slot._CurrentEvolution;
        if (evolution != null)
        {
            if (evolution._NaturalAbilities != null)
            {
                for (int i = 0; i < evolution._NaturalAbilities.Count; i++)
                {
                    AddAbility(abilities, evolution._NaturalAbilities[i]);
                }
            }
            AddAbility(abilities, evolution._UltimateAbility);
        }

        if (slot._CompiledEffects != null)
        {
            for (int i = 0; i < slot._CompiledEffects.Count; i++)
            {
                GrantAbilityEffect grant = slot._CompiledEffects[i] as GrantAbilityEffect;
                if (grant != null) AddAbility(abilities, grant._Ability);
            }
        }
        return abilities;
    }

    void AddAbility(List<AbilitySO> abilities, AbilitySO ability)
    {
        //adds an ability to a list if it's set and not already in there
        if (ability == null || abilities.Contains(ability)) return;
        abilities.Add(ability);
    }

    ShadeStats CopyStats(ShadeStats source)
    {
        //makes a full copy of a set of stats, so changing the copy never touches the original
        //JsonUtility turns the stats into text and back into a brand new object. It works because ShadeStats is plain data
        if (source == null) return new ShadeStats();
        return JsonUtility.FromJson<ShadeStats>(JsonUtility.ToJson(source));
    }
    void AddToStat(ShadeStats target, DamageType.StatType stat, int amount)
    {
        //adds a rune bonus to one stat. Negative runes can't push a stat below 1 (unless it already started below 1)
        switch (stat)
        {
            case DamageType.StatType.Health:
                target._HP = ApplyStatFloor(target._HP, amount);
                target._Health = ApplyStatFloor(target._Health, amount);
                break;
            case DamageType.StatType.Strength:
                target._STR = ApplyStatFloor(target._STR, amount);
                break;
            case DamageType.StatType.Defense:
                target._DEF = ApplyStatFloor(target._DEF, amount);
                break;
            case DamageType.StatType.Agility:
                target._AGI = ApplyStatFloor(target._AGI, amount);
                break;
            case DamageType.StatType.Intellect:
                target._INT = ApplyStatFloor(target._INT, amount);
                break;
            case DamageType.StatType.Spirit:
                target._SPR = ApplyStatFloor(target._SPR, amount);
                break;
            case DamageType.StatType.Discipline:
                target._DIS = ApplyStatFloor(target._DIS, amount);
                break;
            case DamageType.StatType.Wild:
                target._WILD = ApplyStatFloor(target._WILD, amount);
                break;
            default:
                Debug.LogWarning($"Warning! A rune is trying to change a stat that isn't handled: {stat}. Skipping it...", this);
                break;
        }
    }

    int ApplyStatFloor(int baseValue, int amount)
    {
        //adds a bonus to a stat, but a negative bonus never takes it below 1 (see Element Rune in the GDD)
        int result = baseValue + amount;
        if (amount < 0 && result < 1) result = Mathf.Min(baseValue, 1); //if the base was already below 1, it just stays where it was
        return result;
    }
    #endregion

    #region Shade Selection Querry
    public void SetShadeSelection(int Selection)
    {
        currentShadeSelected = Selection;
    }
    public int GetShadeSelectionIndex()
    {
        return currentShadeSelected;
    }
    #endregion

    #region Test Tools
    [FoldoutGroup("Test Tools"), Button("Level Up Current Slot")]
    void TestLevelUp()
    {
        //editor button (play mode only): +1 level on the selected slot, re-running its saved rune field
        if (Application.isPlaying == false) { Debug.LogWarning("Warning! Level Up only works in play mode, the runtime entries don't exist yet..."); return; }
        AddLevels(currentShadeSelected, 1);
    }

    [FoldoutGroup("Test Tools"), Button("Hurt Current Slot")]
    void TestHurt(int amount = 5)
    {
        //editor button (play mode only): takes health off the selected slot's shade, for testing healing and the max HP rule
        if (Application.isPlaying == false) return;
        ShadeRuntimeEntry entry = GetRuntimeEntry(currentShadeSelected);
        if (entry == null) return;
        entry._LiveStats._Health = Mathf.Max(0, entry.GetCurrentHealth() - amount);
    }

    [FoldoutGroup("Test Tools"), Button("Refill All Health")]
    void TestRefillAll()
    {
        //editor button (play mode only)
        if (Application.isPlaying == false) return;
        RefillAllHealth();
    }
    #endregion
}

//one shade slot's runtime side: what the shade is like right now. Built from its slot asset by ShadeManager.RebuildSlot
//summoned shades point at their slot's entry instead of copying it, so putting a shade away or switching loses nothing
[System.Serializable]
public class ShadeRuntimeEntry
{
    [Tooltip("The slot asset this entry was built from")]
    public string _SlotName;

    [Tooltip("Base stats + everything the saved rune field does + the slot's level, XP and lives. Only changes at deliberate moments (saving the field, leveling, evolving)")]
    public ShadeStats _HardStats;

    [Tooltip("Hard stats + timed effects and equipment (later). Its Health is the shade's current health. This is what combat should read")]
    public ShadeStats _LiveStats;

    [Tooltip("Every ability the shade has: natural, ultimate and granted by the rune field. Not used yet")]
    public List<AbilitySO> _Abilities = new List<AbilitySO>();

    public int GetCurrentHealth()
    {
        if (_LiveStats == null) return 0;
        return _LiveStats._Health;
    }

    public int GetMaxHealth()
    {
        if (_HardStats == null) return 0;
        return _HardStats._HP;
    }
}
