using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ReleasedShade : MonoBehaviour, ISummonUnit, IShadeForm
{
    //the released shade currently on the field
    public static ReleasedShade _Released;

    [Header("Data")]
    public ShadeSO _shadeSlotData;
    public ShadeEvolutionSO _shadeEvoData;

    [Tooltip("The slot's runtime entry on the Shade Manager: live stats (Health = current health) and abilities (read only). Shared with the manager, not a copy")]
    [ShowInInspector, ReadOnly] ShadeRuntimeEntry _RuntimeEntry; //ShowInInspector shows it without Unity saving it, so it stays the manager's object

    [FoldoutGroup("Pointers")]
    [Header("Script pointers")]
    public CharacterMovement _movement;
    [Tooltip("Reads the player's controls and steers _movement while the player controls this shade. Grabbed from this object if left empty")]
    public PlayerInputDriver _InputDriver;
    [Tooltip("The AI driver that steers the shade along the NavMesh when it acts on its own. Grabbed from this object if left empty")]
    public NavGuideDriver _AIDriver;
    [Tooltip("Empty child the evolution's Released Art is spawned into. Leave empty to keep whatever art is already on the prefab (nothing gets spawned)")]
    public Transform _ArtHolder;
    public GameObject PlayerRef;

    //local variables
    GameObject _artInstance; //the art spawned from the evolution
    ShadeArtRig _artRig;

    private void Awake()
    {
        if (_InputDriver == null) _InputDriver = GetComponent<PlayerInputDriver>();
        if (_AIDriver == null) _AIDriver = GetComponent<NavGuideDriver>();
    }

    private void OnEnable()
    {
        if (_Released == null) _Released = this;
    }

    private void OnDisable()
    {
        //clears the static so a returned shade doesn't stay "the shade on the field"
        if (_Released == this) _Released = null;
    }

    private void OnDestroy()
    {
        //lets the manager know this shade is gone, even when a scene change removes it instead of the manager
        if (ShadeManager._ShadeManager != null) ShadeManager._ShadeManager.OnReleasedShadeGone(this);
    }

    #region Shade Form Interface
    public ShadeRuntimeEntry GetRuntimeEntry()
    {
        return _RuntimeEntry;
    }

    public void Setup(ShadeSO slot, GameObject summoner)
    {
        //function the Shade Manager calls right after spawning this shade
        _shadeSlotData = slot;
        _shadeEvoData = slot != null ? slot._CurrentEvolution : null; //"? :" picks the left value if the check is true, the right one if not
        _RuntimeEntry = ShadeManager._ShadeManager != null ? ShadeManager._ShadeManager.GetRuntimeEntry(slot) : null; //nothing reads it yet, the shade's health component will
        AssignSummoner(summoner);
        SpawnArt();
    }

    public void Appear()
    {
        //instant for now. todo: magic circle / pop-out effect once shade art exists
    }

    public void Dismiss()
    {
        //destroys it for now. todo: put-away effect
        Destroy(gameObject);
    }
    #endregion

    #region Art
    public void RefreshForm()
    {
        //function the Shade Manager calls when this shade evolves while it's out (even mid-fight): swaps in the new form's art on the spot
        //todo: evolve effect/VFX once shade art exists
        if (_shadeSlotData == null) return;
        _shadeEvoData = _shadeSlotData._CurrentEvolution;
        SpawnArt();
    }

    void SpawnArt()
    {
        //function that spawns the evolution's released art into the art holder
        if (_ArtHolder == null) return; //prefab not set up for spawned art yet, keep the art already on it
        if (_shadeEvoData == null || _shadeEvoData._ReleasedArt == null) { Debug.LogWarning($"Warning! No Released Art on the evolution for {gameObject.name}, spawning no art...", this); return; }

        if (_artInstance != null) Destroy(_artInstance);
        _artInstance = Instantiate(_shadeEvoData._ReleasedArt, _ArtHolder.position, _ArtHolder.rotation, _ArtHolder);

        _artRig = _artInstance.GetComponent<ShadeArtRig>();
        if (_artRig != null) _artRig.ApplyForm(ShadeFormType.Form.Released);
    }

    public Transform GetCastPoint()
    {
        //function for where abilities come from. Falls back to the shade itself
        if (_artRig == null) return transform;
        return _artRig.GetCastPoint();
    }
    #endregion

    #region Control Shade
    public void EnablePlayerControl()
    {
        //the player takes over this shade: AI driver off first, then the input driver on
        if (_AIDriver != null) _AIDriver.enabled = false;
        if (_InputDriver == null) { Debug.LogError($"Error! No PlayerInputDriver on {gameObject.name}, the player can't control it", this); return; }
        _InputDriver.enabled = true;
    }

    public void EnableShadeControl()
    {
        //the shade acts on its own: input driver off first, then the AI driver on
        if (_InputDriver != null) _InputDriver.enabled = false;
        if (_AIDriver == null) { Debug.LogWarning($"Warning! No NavGuideDriver on {gameObject.name}, it will just stand still...", this); return; }
        _AIDriver.enabled = true;
    }
    #endregion

    #region Isummon
    public void AssignSummoner(GameObject Summoner)
    {
        PlayerRef = Summoner;
    }

    public void UpdateStats(CoreStats summonerStats)
    {

    }
    #endregion
}
