using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Goes on a held melee weapon prefab. Hands the MeleeWeaponItem it was set up with to a MeleeComboRunner, which plays the combo:
/// wind up → active (the swing shape sweeps and hits) → recovery → next hit while attacking is held or buffered.
/// The runner is shared with abilities, so a sword and a Lurker's swipes hit the same way.
/// See Melee Weapon System in the GDD.
///
////////////////////////////////////////////////
public class WeaponAttackMelee : MonoBehaviour, ITriggerable
{
    [Header("Settings")]
    [Tooltip("Layers swings (and their line of sight checks) can hit. Trigger colliders are always ignored")]
    [SerializeField] LayerMask _HitLayers = ~0; //~0 means every layer

    [Tooltip("Draws the swing shape in the Scene view while it's active (faint = whole shape, bright = swept so far). For tuning")]
    [SerializeField] bool _DrawSwingGizmos = true;

    [Header("Runtime Data")]
    [Tooltip("Who is holding the weapon. Set by SetUpWeapon (read only)")]
    [SerializeField, ReadOnly] GameObject _Wielder;

    [Tooltip("The weapon data this was set up with (read only)")]
    [SerializeField, ReadOnly] MeleeWeaponItem _WeaponData;

    [ShowInInspector, ReadOnly] //shows a value that isn't saved, read from the runner
    [PropertyTooltip("Which hit of the combo comes next, counting from 0 (read only)")]
    int ComboStep { get { return _runner != null ? _runner.GetComboStep() : 0; } }

    [ShowInInspector, ReadOnly]
    [PropertyTooltip("True while the attack button is held down (read only)")]
    bool IsTriggerHeld { get { return _runner != null && _runner.IsTriggerHeld(); } }

    [ShowInInspector, ReadOnly]
    [PropertyTooltip("True if attack was pressed mid swing, so the next hit starts right after this one (read only)")]
    bool IsAttackBuffered { get { return _runner != null && _runner.IsAttackBuffered(); } }

    [ShowInInspector, ReadOnly]
    [PropertyTooltip("Where the current hit is: wind up, active, recovery or none (read only)")]
    CombatType.Phase Phase { get { return _runner != null ? _runner.GetPhase() : CombatType.Phase.None; } }

    //local variables
    static readonly Color GizmoFullColor = new Color(1f, 1f, 1f, 0.25f); //static readonly: made once and never changed, like a constant for types that can't be const
    static readonly Color GizmoSweptColor = new Color(1f, 0.3f, 0.2f, 1f);
    MeleeComboRunner _runner; //does the actual swinging (see MeleeComboRunner)

    private void OnDisable()
    {
        //put away or destroyed mid swing: stop, and give the wielder back their facing and speed
        if (_runner != null) _runner.Stop();
    }

    private void OnDestroy()
    {
        //stop listening to the wielder's dashes
        if (_runner != null) _runner.TearDown();
    }

    #region Initialize
    public void SetUpWeapon(ItemSO item, GameObject wielder, CoreStats stats)
    {
        //function the wielder calls right after spawning the weapon, so it knows its data, its stats and who is holding it
        _WeaponData = item as MeleeWeaponItem; //"as" gives null instead of an error if the item is a different kind of weapon
        if (_WeaponData == null) { Debug.LogError($"Error! {gameObject.name} was set up with {(item != null ? item.name : "nothing")}, which isn't a MeleeWeaponItem", this); return; }

        if (_runner == null) _runner = new MeleeComboRunner(this); //"this" is the weapon, so the runner can use it to run coroutines
        if (_runner.SetUp(_WeaponData, wielder, stats, _HitLayers) == false) return; //the runner logs what's missing

        _Wielder = wielder;
        _WeaponData.LogSetupWarnings(this);
    }

    public bool IsRange()
    {
        return false; //melee uses the primary attack stat (STR for physical)
    }
    #endregion

    #region Weapon Activation Trigger
    public void TriggerAttack()
    {
        //attack pressed. The runner starts the combo, or buffers the press if a swing is already going
        if (_WeaponData == null || _runner == null) return;
        _runner.Press();
    }

    public void ReleaseAttack()
    {
        //attack let go. Holding keeps the combo going, so letting go just means it stops after this hit (unless a press was buffered)
        if (_runner != null) _runner.Release();
    }

    public bool IsBusy()
    {
        //true from the start of a swing until its recovery ends, so weapons can't be switched mid swing (or by dashing out of one)
        return _runner != null && _runner.IsBusy();
    }
    #endregion

    #region Debugging
    private void OnDrawGizmos()
    {
        //draws the active swing in the Scene view (turn on Gizmos in the Game view to see it there too)
        if (_DrawSwingGizmos == false || _runner == null) return;
        _runner.DrawGizmos(GizmoFullColor, GizmoSweptColor);
    }
    #endregion

    #region Test Tools
    [Button("Test Press Attack")]
    void TestPressAttack()
    {
        //editor button for pressing attack without input (play mode only)
        if (Application.isPlaying == false) return;
        TriggerAttack();
    }

    [Button("Test Release Attack")]
    void TestReleaseAttack()
    {
        //editor button for letting go of attack (play mode only)
        if (Application.isPlaying == false) return;
        ReleaseAttack();
    }
    #endregion
}
