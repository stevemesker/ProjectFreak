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
    [SerializeField]ElementManagerSO managerScriptableObject;

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
    [SerializeField] List<statBoostPackage> shadeAlterPackages;

    [Header("Rune Fields")]
    [Tooltip("Core settings and ability nodes shared by every slot's rune field. Can be left empty for now: the rune field UI builds one from its scene nodes and hands it over when it opens")]
    [SerializeField] RuneFieldLayoutSO _RuneFieldLayout;

    [Tooltip("Each slot's saved rune field (read only). Copied from the slot assets' Starting Rune Field when the game starts, so playing never changes the assets")]
    [SerializeField, ReadOnly] List<RuneFieldData> _SlotRuneFields = new List<RuneFieldData>();

    [Tooltip("Each slot's stats with its saved runes applied (read only). Worked out from the base stats every time a field is saved")]
    [SerializeField, ReadOnly] List<ShadeStats> _SlotStats = new List<ShadeStats>();

    [Header("Local Variables")]
    [SerializeField] float _switchTimeIn = .5f;
    [SerializeField] float _switchTimeOut = .3f;
    Coroutine _transferTimer;
    [SerializeField] bool isBusy;

    private void Awake()
    {
        //sets up the singleton so other scripts can reach this manager with ShadeManager._ShadeManager
        if (_ShadeManager == null) _ShadeManager = this;

        InitializeRuneFields();
    }

    private void OnEnable()
    {
        managerScriptableObject.manager = this;
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

    #region stat change
    public void ReceiveStatBoostPackage(List<statBoostPackage> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            ChangeStat(input[i], 1);
        }
    }

    public void RemoveStatBoostPackage(List<statBoostPackage> input)
    {
        for (int i = 0; i < input.Count; i++)
        {
            ChangeStat(input[i], -1);
        }
    }

    public void ChangeStat(statBoostPackage input, int multiplier)
    {
        switch (input._statToChange)
        {
            case (DamageType.StatType.Health):
                _ShadeSlots[currentShadeSelected]._AlteredStats._HP += input._ChangeAmount * multiplier;
                _ShadeSlots[currentShadeSelected]._AlteredStats._Health += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Strength):
                _ShadeSlots[currentShadeSelected]._AlteredStats._STR += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Defense):
                _ShadeSlots[currentShadeSelected]._AlteredStats._DEF += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Agility):
                _ShadeSlots[currentShadeSelected]._AlteredStats._AGI += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Intellect):
                _ShadeSlots[currentShadeSelected]._AlteredStats._INT += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Spirit):
                _ShadeSlots[currentShadeSelected]._AlteredStats._SPR += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Discipline):
                _ShadeSlots[currentShadeSelected]._AlteredStats._DIS += input._ChangeAmount * multiplier;
                break;
            case (DamageType.StatType.Wild):
                _ShadeSlots[currentShadeSelected]._AlteredStats._WILD += input._ChangeAmount * multiplier;
                break;
            default:
                Debug.LogWarning("Warning! Stat upgrade package is trying to access a stat that is unaccounted for: " + input._statToChange);
                break;
        }
    }

    #endregion

    #region Rune Fields
    void InitializeRuneFields()
    {
        //copies every slot's starting rune field and works out its stats, so nothing at runtime ever changes the slot assets
        _SlotRuneFields.Clear();
        _SlotStats.Clear();

        for (int i = 0; i < _ShadeSlots.Count; i++)
        {
            ShadeSO slot = _ShadeSlots[i];
            RuneFieldData startingField = (slot != null && slot._StartingRuneField != null) ? slot._StartingRuneField.Clone() : new RuneFieldData();
            _SlotRuneFields.Add(startingField);
            _SlotStats.Add(null);
            RecalculateSlotStats(i);
        }
    }

    public bool IsValidSlot(int slotIndex)
    {
        //checks a slot index points at a real slot
        return slotIndex >= 0 && slotIndex < _ShadeSlots.Count && _ShadeSlots[slotIndex] != null && slotIndex < _SlotRuneFields.Count;
    }

    public RuneFieldData GetSavedRuneField(int slotIndex)
    {
        //returns a copy of a slot's saved rune field for the rune field UI to edit. Editing the copy doesn't change the saved one
        if (IsValidSlot(slotIndex) == false) { Debug.LogError($"Error! Shade slot {slotIndex} doesn't exist on {gameObject.name}, can't load its rune field", this); return new RuneFieldData(); }
        return _SlotRuneFields[slotIndex].Clone();
    }

    public void SaveRuneField(int slotIndex, RuneFieldData field)
    {
        //function the rune field UI calls when the player saves. Stores a copy and works the slot's stats out again
        if (IsValidSlot(slotIndex) == false) { Debug.LogError($"Error! Shade slot {slotIndex} doesn't exist on {gameObject.name}, can't save its rune field", this); return; }
        if (field == null) return;

        _SlotRuneFields[slotIndex] = field.Clone();
        RecalculateSlotStats(slotIndex);
    }

    public int GetCorePower(int slotIndex)
    {
        //how much power a slot's core has. todo: + the core fragment's power once fragments exist (see Rune Field Overhaul Plan)
        if (IsValidSlot(slotIndex) == false) return 0;
        return _ShadeSlots[slotIndex]._shadeStats._LVL;
    }

    public RuneFieldLayoutSO GetRuneFieldLayout()
    {
        return _RuneFieldLayout;
    }

    public void SetRuneFieldLayout(RuneFieldLayoutSO layout)
    {
        //function the rune field UI uses to hand over the layout it built from its scene, when none is assigned here
        //every slot's stats get worked out again now that the ability nodes are known
        _RuneFieldLayout = layout;
        for (int i = 0; i < _SlotRuneFields.Count; i++)
        {
            RecalculateSlotStats(i);
        }
    }

    public ShadeStats GetSlotStats(int slotIndex)
    {
        //returns a slot's stats with its saved runes applied
        if (IsValidSlot(slotIndex) == false || slotIndex >= _SlotStats.Count) return null;
        return _SlotStats[slotIndex];
    }

    public ShadeStats GetCurrentShadeStats()
    {
        return GetSlotStats(currentShadeSelected);
    }
    #endregion

    #region Stat Calculation
    void RecalculateSlotStats(int slotIndex)
    {
        //works a slot's stats out from scratch: its base stats plus every powered rune in its saved field
        //nothing is added or taken away bit by bit, so the stats can't drift out of sync with the runes
        if (IsValidSlot(slotIndex) == false) return;

        ShadeStats baseStats = _ShadeSlots[slotIndex]._shadeStats;
        ShadeStats result = CopyStats(baseStats);

        //a throwaway RuneField over a copy of the saved data, only used for its power and stat math
        RuneField field = new RuneField(_SlotRuneFields[slotIndex].Clone(), _RuneFieldLayout, GetCorePower(slotIndex));
        Dictionary<DamageType.StatType, int> totals = field.GetStatTotals();

        //todo: evolution rank multiplier and Hazen's share of the runes (see Element Rune and Evolution in the GDD)
        foreach (KeyValuePair<DamageType.StatType, int> entry in totals)
        {
            AddToStat(result, entry.Key, entry.Value);
        }

        _SlotStats[slotIndex] = result;
    }

    ShadeStats CopyStats(ShadeStats source)
    {
        //makes a full copy of a set of stats, so changing the copy never touches the slot asset
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
}
