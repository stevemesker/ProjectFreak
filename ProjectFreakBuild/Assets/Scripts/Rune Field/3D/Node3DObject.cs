using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

//an ability node on the 3D rune field (the 3D version of EvolutionNode). RuneField decides if the node is on, this shows it and fires the events
//its unlock and lockout lists and its effects are where the rules get the node from when the field builds its node list
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
    [Tooltip("The material's color name: _BaseColor for HDRP Lit, _UnlitColor for HDRP Unlit")]
    [SerializeField] string _ColorProperty = "_BaseColor";

    [Tooltip("Tint while the node can't take a rune (outside the open zone, a closed gate, or locked out)")]
    [SerializeField] Color _LockedColor = new Color(0.45f, 0.2f, 0.2f, 1f);

    [Header("References")]
    [Tooltip("Shown while the node is on. Left empty, a child named OnEffect is used if there is one")]
    [SerializeField] GameObject _OnEffect;

    [Header("Events")]
    [Tooltip("Runs when the node turns on while the player is editing the field (not when a saved field loads)")]
    [SerializeField] UnityEvent _ActivationEvent;

    [Tooltip("Runs when the node turns off while the player is editing the field (not when a saved field loads)")]
    [SerializeField] UnityEvent _DeactivationEvent;

    [Header("Runtime Data")]
    [Tooltip("Is this node on right now (read only, set by the field)")]
    [SerializeField, ReadOnly] bool _Active;

    [Tooltip("Can this node not take a rune right now (read only, set by the field)")]
    [SerializeField, ReadOnly] bool _Locked;

    //local variables
    const string OnEffectName = "OnEffect"; //the child used for On Effect when nothing is assigned
    string _effectProblems; //filled by OnValidate, shown as a warning box in the inspector
    RuneFieldTint _tint;

    private void OnValidate()
    {
        //checks the effect list for empty entries, missing assets or more than one Evolve, so broken nodes show up in the inspector
        _effectProblems = RuneEffect.GetListProblems(_Effects, gameObject.name);
    }

    private void Awake()
    {
        if (_OnEffect == null) _OnEffect = FindChild(transform, OnEffectName);
        Transform skip = _OnEffect != null ? _OnEffect.transform : null; //the OnEffect mesh keeps its own look
        _tint = new RuneFieldTint(RuneFieldTint.FindRenderers(transform, skip), _ColorProperty);
        if (_OnEffect != null) _OnEffect.SetActive(false);
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
    public void ShowState(bool active, bool locked, bool fireEvents)
    {
        //function the field calls after every change to show if this node is on or locked
        //events only fire when the state actually changes, and only if fireEvents is true (it's false when a saved field loads)
        bool wasActive = _Active;
        _Active = active;
        _Locked = locked;

        if (_OnEffect != null) _OnEffect.SetActive(active);
        if (_tint != null)
        {
            if (locked) _tint.SetColor(_LockedColor);
            else _tint.Clear();
        }

        if (fireEvents == false || active == wasActive) return;
        if (active) _ActivationEvent?.Invoke();
        else _DeactivationEvent?.Invoke();
    }

    static GameObject FindChild(Transform parent, string childName)
    {
        //looks through every child (and their children) for one with this name. Null if there isn't one
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child.gameObject;
            GameObject deeper = FindChild(child, childName);
            if (deeper != null) return deeper;
        }
        return null;
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
