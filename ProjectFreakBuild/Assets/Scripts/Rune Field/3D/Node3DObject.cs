using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

//an ability node on the 3D rune field. RuneField decides if the node is on, this shows it and fires the events
//its unlock and lockout lists and its effects are where the rules get the node from when the field builds its node list
//the model has two moving parts: the cover closes while no runes can go in or come out, the shield rises while the node is on
public class Node3DObject : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("Nodes that must be on before this one can turn on")]
    [SerializeField] List<Node3DObject> _Unlocks = new List<Node3DObject>();

    [Tooltip("Nodes this one locks out once a rune is plugged into it. Lockouts are always two-way (a missing reverse link gets filled in and logged)")]
    [SerializeField] List<Node3DObject> _Lockouts = new List<Node3DObject>();

    [Tooltip("What this node does to the shade while it's on (stat change, grant ability, evolve). A node with an Evolve effect is an evolution gate")]
    [InfoBox("$_effectProblems", InfoMessageType.Warning, "HasEffectProblems")]
    [SerializeReference] //lets this one list hold different effect types. The inspector asks which type to add
    List<RuneEffect> _Effects = new List<RuneEffect>();

    [Header("Settings")]
    [FoldoutGroup("Cover & Shield")]
    [Tooltip("How the cover turns to open, in degrees, on top of its model rotation (the model is built closed). Flip the sign if it opens the wrong way")]
    [SerializeField] Vector3 _CoverOpenRotation = new Vector3(180f, 0f, 0f);

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("How long the cover takes to open or close, in seconds")]
    [SerializeField, Min(0f)] float _CoverTime = 0.4f;

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("The shape of the cover's move over its time (0 = where it started, 1 = where it's going). Going past 1 gives an overshoot")]
    [SerializeField] AnimationCurve _CoverCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("Where the shield sits while down, as an offset from its model position in its parent's space, in meters (the model is built with the shield up)")]
    [SerializeField] Vector3 _ShieldDownOffset = new Vector3(0f, -0.1f, 0f);

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("How long the shield takes to go up or down, in seconds")]
    [SerializeField, Min(0f)] float _ShieldTime = 0.25f;

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("The shape of the shield's move over its time (0 = where it started, 1 = where it's going). Going past 1 gives an overshoot")]
    [SerializeField] AnimationCurve _ShieldCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("The child the cover is found by when Cover is left empty")]
    [SerializeField] string _CoverName = "Mesh_RuneField_Node_Cover";

    [FoldoutGroup("Cover & Shield")]
    [Tooltip("The child the shield is found by when Shield is left empty")]
    [SerializeField] string _ShieldName = "Mesh_RuneField_Node_Shield";

    [Header("References")]
    [Tooltip("Shown while the node is on. Left empty, a child named OnEffect is used if there is one")]
    [SerializeField] GameObject _OnEffect;

    [Tooltip("The part that closes over the node while no runes can go in or come out (locked out, or a locked-in rune). Left empty, the child named in Cover Name is used")]
    [SerializeField] Transform _Cover;

    [Tooltip("The part that rises while the node is on (a powered rune plugged in). Left empty, the child named in Shield Name is used")]
    [SerializeField] Transform _Shield;

    [Header("Events")]
    [Tooltip("Runs when the node turns on while the player is editing the field (not when a saved field loads). The shield starts rising at the same time")]
    [SerializeField] UnityEvent _ActivationEvent;

    [Tooltip("Runs when the node turns off while the player is editing the field (not when a saved field loads). The shield starts lowering at the same time")]
    [SerializeField] UnityEvent _DeactivationEvent;

    [Tooltip("Runs when the cover starts closing while the player is editing the field (not when a saved field loads)")]
    [SerializeField] UnityEvent _CoverClosedEvent;

    [Tooltip("Runs when the cover starts opening while the player is editing the field (not when a saved field loads)")]
    [SerializeField] UnityEvent _CoverOpenedEvent;

    [Header("Runtime Data")]
    [Tooltip("Is this node on right now (read only, set by the field)")]
    [SerializeField, ReadOnly] bool _Active;

    [Tooltip("Is the cover closed, or closing (read only, set by the field)")]
    [SerializeField, ReadOnly] bool _CoverClosed = true;

    [Tooltip("Is the shield up, or rising (read only, set by the field)")]
    [SerializeField, ReadOnly] bool _ShieldUp = true;

    //local variables
    const string OnEffectName = "OnEffect"; //the child used for On Effect when nothing is assigned
    string _effectProblems; //filled by OnValidate, shown as a warning box in the inspector
    Quaternion _coverClosedRotation; //the model's own poses (cover closed, shield up), read once in Awake
    float _coverOpenAmount; //how far open the cover is: 0 closed, 1 fully open (Cover Open Rotation)
    Vector3 _shieldUpPosition;
    Vector3 _shieldDownPosition;
    Coroutine _coverMove;
    Coroutine _shieldMove;

    private void OnValidate()
    {
        //checks the effect list for empty entries, missing assets or more than one Evolve, so broken nodes show up in the inspector
        _effectProblems = RuneEffect.GetListProblems(_Effects, gameObject.name);
    }

    private void Awake()
    {
        if (_OnEffect == null) _OnEffect = RuneFieldTint.FindChild(transform, OnEffectName);
        if (_OnEffect != null) _OnEffect.SetActive(false);
        SetupCoverAndShield();
    }

    private void OnDisable()
    {
        //stops any move part way and puts both parts where they were heading, so a node switched off mid-move isn't left half open
        StopMove(ref _coverMove);
        StopMove(ref _shieldMove);
        PlaceCover();
        PlaceShield();
    }

    #region Rules
    public List<Node3DObject> GetUnlockNodes() { return _Unlocks; }
    public List<Node3DObject> GetLockoutNodes() { return _Lockouts; }
    public List<RuneEffect> GetEffects() { return _Effects; }

    public bool IsGate()
    {
        //a node is an evolution gate if it has an Evolve effect
        return RuneEffect.HasEvolve(_Effects);
    }

    bool HasEffectProblems()
    {
        //used by the InfoBox above to decide if the warning shows
        return string.IsNullOrEmpty(_effectProblems) == false;
    }
    #endregion

    #region Display
    public void ShowState(bool active, bool fireEvents)
    {
        //function the field calls after every change to show if this node is on (being locked shows through the cover, see ShowCoverAndShield)
        //events only fire when the state actually changes, and only if fireEvents is true (it's false when a saved field loads)
        bool wasActive = _Active;
        _Active = active;

        if (_OnEffect != null) _OnEffect.SetActive(active);

        if (fireEvents == false || active == wasActive) return;
        if (active) _ActivationEvent?.Invoke();
        else _DeactivationEvent?.Invoke();
    }

    public void ShowCoverAndShield(bool coverClosed, bool shieldUp, bool animate)
    {
        //function the field calls after every change. The two parts are separate:
        //the cover closes while no runes can go in or come out, the shield rises while the node is on
        //animate is false when a saved field loads, so the parts snap into place instead of playing
        //it's called on every refresh (even mid-drag), so a part only starts moving when its target actually changes
        bool coverChanged = coverClosed != _CoverClosed;
        bool shieldChanged = shieldUp != _ShieldUp;
        _CoverClosed = coverClosed;
        _ShieldUp = shieldUp;

        if (animate == false)
        {
            StopMove(ref _coverMove);
            StopMove(ref _shieldMove);
            PlaceCover();
            PlaceShield();
            return;
        }

        if (coverChanged)
        {
            MoveCover();
            if (coverClosed) _CoverClosedEvent?.Invoke();
            else _CoverOpenedEvent?.Invoke();
        }
        if (shieldChanged) MoveShield(); //the shield's events are the Activation / Deactivation events above, since it follows the node being on
    }
    #endregion

    #region Cover & Shield
    void SetupCoverAndShield()
    {
        //finds the two moving parts (if not assigned) and reads their model poses. The model is built locked: cover closed, shield up
        if (_Cover == null)
        {
            GameObject found = RuneFieldTint.FindChild(transform, _CoverName);
            if (found != null) _Cover = found.transform;
        }
        if (_Shield == null)
        {
            GameObject found = RuneFieldTint.FindChild(transform, _ShieldName);
            if (found != null) _Shield = found.transform;
        }

        if (_Cover == null) Debug.LogWarning($"Warning! No cover found on {gameObject.name} (looked for a child named {_CoverName}), it won't open or close...", this);
        else _coverClosedRotation = _Cover.localRotation;

        if (_Shield == null) Debug.LogWarning($"Warning! No shield found on {gameObject.name} (looked for a child named {_ShieldName}), it won't go up or down...", this);
        else
        {
            _shieldUpPosition = _Shield.localPosition;
            _shieldDownPosition = _shieldUpPosition + _ShieldDownOffset;
        }

        //shows whatever the field asked for already (normally the model's own pose, but the field may have refreshed before this ran)
        PlaceCover();
        PlaceShield();
    }

    void MoveCover()
    {
        //starts the cover moving toward its target from wherever it is now, so a change mid-move doesn't snap
        StopMove(ref _coverMove);
        if (_Cover == null) return;
        if (isActiveAndEnabled == false || _CoverTime <= 0f) { PlaceCover(); return; } //coroutines can't run on a switched off object
        _coverMove = StartCoroutine(CoverMove());
    }

    void MoveShield()
    {
        //starts the shield moving toward its target from wherever it is now
        StopMove(ref _shieldMove);
        if (_Shield == null) return;
        if (isActiveAndEnabled == false || _ShieldTime <= 0f) { PlaceShield(); return; }
        _shieldMove = StartCoroutine(ShieldMove());
    }

    IEnumerator CoverMove()
    {
        //turns the cover from where it is to its target over Cover Time, shaped by Cover Curve
        //unscaledDeltaTime keeps it moving while the game is paused under the rune field (deltaTime would be 0)
        //it moves how far open the cover is (a number) rather than blending two rotations, since a 180 degree blend could swing either way
        float start = _coverOpenAmount;
        float target = _CoverClosed ? 0f : 1f;
        float timer = 0f;

        while (timer < _CoverTime)
        {
            timer += Time.unscaledDeltaTime;
            float amount = _CoverCurve.Evaluate(Mathf.Clamp01(timer / _CoverTime));
            _coverOpenAmount = Mathf.LerpUnclamped(start, target, amount); //Unclamped lets a curve that goes past 1 overshoot
            TurnCover();
            yield return null;
        }

        PlaceCover();
        _coverMove = null;
    }

    IEnumerator ShieldMove()
    {
        //slides the shield from where it is to its target over Shield Time, shaped by Shield Curve
        Vector3 start = _Shield.localPosition;
        Vector3 target = _ShieldUp ? _shieldUpPosition : _shieldDownPosition;
        float timer = 0f;

        while (timer < _ShieldTime)
        {
            timer += Time.unscaledDeltaTime;
            float amount = _ShieldCurve.Evaluate(Mathf.Clamp01(timer / _ShieldTime));
            _Shield.localPosition = Vector3.LerpUnclamped(start, target, amount);
            yield return null;
        }

        PlaceShield();
        _shieldMove = null;
    }

    void PlaceCover()
    {
        //puts the cover straight at its target, no animation
        _coverOpenAmount = _CoverClosed ? 0f : 1f;
        TurnCover();
    }

    void TurnCover()
    {
        //turns the cover to match how far open it is: the model's rotation plus that share of Cover Open Rotation
        if (_Cover == null) return;
        _Cover.localRotation = _coverClosedRotation * Quaternion.Euler(_CoverOpenRotation * _coverOpenAmount); //multiplying quaternions adds the turn on top of the model's rotation
    }

    void PlaceShield()
    {
        //puts the shield straight at its target, no animation
        if (_Shield == null) return;
        _Shield.localPosition = _ShieldUp ? _shieldUpPosition : _shieldDownPosition;
    }

    void StopMove(ref Coroutine move)
    {
        //stops a part's move if one is running. "ref" lets this set the caller's field to null, so one helper works for both parts
        if (move != null) StopCoroutine(move);
        move = null;
    }
    #endregion

    #region Test Tools
    [FoldoutGroup("Cover & Shield"), Button("Test Cover (Play Mode)")]
    void TestCover()
    {
        //play mode button: flips the cover open / closed with its animation, for tuning the time and curve. The next field refresh puts it back
        if (Application.isPlaying == false) { Debug.LogWarning("Warning! Test Cover only works in play mode, nothing moved...", this); return; }
        ShowCoverAndShield(_CoverClosed == false, _ShieldUp, true);
    }

    [FoldoutGroup("Cover & Shield"), Button("Test Shield (Play Mode)")]
    void TestShield()
    {
        //play mode button: flips the shield up / down with its animation. The next field refresh puts it back
        if (Application.isPlaying == false) { Debug.LogWarning("Warning! Test Shield only works in play mode, nothing moved...", this); return; }
        ShowCoverAndShield(_CoverClosed, _ShieldUp == false, true);
    }
    #endregion

    #region Zone Tools
    [FoldoutGroup("Zone Tools"), Button("Snap To Next Ring Out"), GUIColor(0.4f, 1f, 0.4f)]
    void SnapToNextRingOut()
    {
        //editor button: moves the node out onto the closest ring past it, keeping its angle around the core. A node already on a ring goes up to the next one
        RuneField3DObject field = FindField();
        if (field == null) return;
        field.SnapNodeToRing(transform, field.GetNextRingOut(transform));
    }

    [FoldoutGroup("Zone Tools"), Button("Snap To Previous Ring In"), GUIColor(0.4f, 0.8f, 1f)]
    void SnapToPreviousRingIn()
    {
        //editor button: moves the node in onto the closest ring inside it, keeping its angle around the core. A node already on a ring goes down to the one before it
        RuneField3DObject field = FindField();
        if (field == null) return;

        int ring = field.GetPreviousRingIn(transform);
        if (ring < 1) { Debug.LogWarning($"Warning! {gameObject.name} is already on or inside ring 1, there's no ring further in. It wasn't moved...", this); return; }
        field.SnapNodeToRing(transform, ring);
    }

    [FoldoutGroup("Zone Tools"), Button("Snap To Ring")]
    void SnapToRing(int ring = 1)
    {
        //editor button: moves the node onto a chosen ring, keeping its angle around the core. Ring 1 is zone 1's outer edge
        RuneField3DObject field = FindField();
        if (field == null) return;
        field.SnapNodeToRing(transform, ring);
    }

    RuneField3DObject FindField()
    {
        //finds the field this node belongs to: a parent first, otherwise any 3D field in the open scenes (editor tool, so searching is fine here)
        RuneField3DObject field = GetComponentInParent<RuneField3DObject>();
        if (field == null) field = FindObjectOfType<RuneField3DObject>();
        if (field == null) Debug.LogError($"Error! No RuneField3DObject found for {gameObject.name}, can't snap it to a ring", this);
        return field;
    }
    #endregion
}
