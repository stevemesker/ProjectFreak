using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//one bridge on the 3D rune field. It's a view: it sits on one end, points at the other and stretches its Z scale to the distance
//(the mesh starts at its origin and runs 1 m along +Z, so the Z scale is the length in meters and the tiling material follows it)
//it also runs the tear timer while a rune pulls it past its reach (used from step 3), and asks the field to remove it when it snaps
//the tear only pauses when the pull stops, so it keeps what it built up for the rest of the drag. It goes back to 0 when the drag ends (ResetTearing)
//for the bridge shader (SH_NodeBridge on MAT_Bridge), it hands each bridge its own end colors, tear progress and strain through a MaterialPropertyBlock,
//so every bridge shares one material: no material per color combo, and no copy while tearing
//end A is always the end closer to the core (the field sorts that out before calling Setup), so the shader's Source Color side and the tear start face the core
public class Bridge3DObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far above the field the bridge sits, in meters. Raise it if it disappears into the field's surface")]
    [SerializeField] float _HeightOffset = 0f;

    [Header("Tearing")]
    [Tooltip("How much pull has to build up before the bridge snaps")]
    [SerializeField, Min(0.01f)] float _PullTearRequiredTime = 10f;

    [Tooltip("Cap on how hard a single pull can count")]
    [SerializeField, Min(0f)] float _MaxPullStrength = 200f;

    [Tooltip("How often the pull builds up, in seconds")]
    [SerializeField, Min(0.01f)] float _PullTickTime = 0.1f;

    [Tooltip("Bigger = pulling further counts for less. Pull strength is (field units past reach / this)")]
    [SerializeField, Min(0.01f)] float _PullDampening = 150f;

    [FoldoutGroup("Shader Properties")]
    [Tooltip("SH_NodeBridge's color at end A, the end closer to the core (where the bridge starts, UV x = 0). Gets the color of what's at that end")]
    [SerializeField] string _SourceColorProperty = "_SourceColor";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("SH_NodeBridge's color at end B, the end further from the core (where the bridge points, UV x = 1). Gets the color of what's at that end")]
    [SerializeField] string _ConnectionColorProperty = "_ConnectionColor";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("SH_NodeBridge's tear progress, 0 to 1 (how close the tear timer is to snapping it). The tear color moves from end A to end B as it fills. The tear color itself is set on MAT_Bridge")]
    [SerializeField] string _TearProgressProperty = "_TearProgress";

    [FoldoutGroup("Shader Properties")]
    [Tooltip("The bridge shader's strain, 0 to 1 (how far into the field's Tear Slack it's pulled, before the tear timer starts). For the tear dead zone look. SH_NodeBridge doesn't have this yet, so it's ignored until it does")]
    [SerializeField] string _StrainProperty = "_Strain";

    [Header("Runtime Data")]
    [Tooltip("Rune ID at each end (-1 = the core). X is end A (closer to the core), Y is end B (read only)")]
    [SerializeField, ReadOnly] Vector2Int _Ends;

    [Tooltip("Is the tear timer running (read only)")]
    [SerializeField, ReadOnly] bool _Tearing;

    [Tooltip("How much pull has built up (read only)")]
    [SerializeField, ReadOnly] float _PullCurrentTime;

    [Tooltip("The color of end A, the end closer to the core: the rune's color, or the field's core color (read only)")]
    [SerializeField, ReadOnly] Color _ColorA = Color.white;

    [Tooltip("The color of end B, the end further from the core (read only)")]
    [SerializeField, ReadOnly] Color _ColorB = Color.white;

    [Tooltip("How far into the field's Tear Slack the bridge is pulled, 0 to 1 (read only)")]
    [SerializeField, ReadOnly] float _Strain;

    //local variables
    Transform _endA;
    Transform _endB;
    Vector3 _fieldUp = Vector3.up;
    Vector3 _baseScale = Vector3.one; //the prefab's own scale, so only Z gets changed
    float _pullStrength;
    Coroutine _pullTimer;
    RuneFieldTint _tint;
    int _sourceColorID; //shader property IDs, worked out once from the names above
    int _connectionColorID;
    int _tearProgressID;
    int _strainID;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _tint = new RuneFieldTint(RuneFieldTint.FindRenderers(transform, null), null); //no single tint color here, the bridge only uses StartValues / Put / ApplyValues
        _sourceColorID = Shader.PropertyToID(_SourceColorProperty);
        _connectionColorID = Shader.PropertyToID(_ConnectionColorProperty);
        _tearProgressID = Shader.PropertyToID(_TearProgressProperty);
        _strainID = Shader.PropertyToID(_StrainProperty);
    }

    private void OnDisable()
    {
        //stops the tear timer so it doesn't keep running on a bridge that's going away
        if (_pullTimer != null) StopCoroutine(_pullTimer);
        _pullTimer = null;
        _Tearing = false;
    }

    #region Initialize
    public void Setup(int idA, int idB, Transform endA, Transform endB, Vector3 fieldUp, Color colorA, Color colorB)
    {
        //function the field calls right after spawning this bridge. A is the end closer to the core, B the one further out
        //colorA / colorB are the colors of the two things it joins
        _Ends = new Vector2Int(idA, idB);
        _endA = endA;
        _endB = endB;
        _fieldUp = fieldUp;
        _ColorA = colorA;
        _ColorB = colorB;
        UpdatePosition();
        ApplyLook();
    }

    public bool Touches(int id)
    {
        //is this rune (or the core) one of the bridge's ends
        return _Ends.x == id || _Ends.y == id;
    }

    public Vector2Int GetEnds()
    {
        //the rune ID at each end (-1 = the core). X is end A, the end closer to the core
        return _Ends;
    }

    public int GetOtherEnd(int id)
    {
        //returns the ID at the opposite end from the one given
        return _Ends.x == id ? _Ends.y : _Ends.x;
    }
    #endregion

    #region Drawing
    public void UpdatePosition()
    {
        //sits on end A, points at end B and stretches to the distance between them
        if (_endA == null || _endB == null) return;

        Vector3 start = _endA.position;
        Vector3 direction = _endB.position - start;
        float length = direction.magnitude;

        transform.position = start + _fieldUp * _HeightOffset;
        if (length > 0.0001f) transform.rotation = Quaternion.LookRotation(direction, _fieldUp); //LookRotation turns +Z to face the direction
        transform.localScale = new Vector3(_baseScale.x, _baseScale.y, length);
    }

    public void ShowStrain(float strain)
    {
        //how far into the field's Tear Slack the bridge is pulled (0 to 1). The field calls this every drag frame for bridges on the dragged rune
        strain = Mathf.Clamp01(strain);
        if (Mathf.Approximately(strain, _Strain)) return; //nothing changed, skip updating the look
        _Strain = strain;
        ApplyLook();
    }

    public float GetTearAmount()
    {
        //how close the tear timer is to snapping the bridge, 0 to 1
        return _PullTearRequiredTime > 0f ? Mathf.Clamp01(_PullCurrentTime / _PullTearRequiredTime) : 0f;
    }

    void ApplyLook()
    {
        //puts every shader value on the bridge at once: both end colors, tear progress and strain
        //values the material's shader doesn't have are ignored (like strain for now)
        if (_tint == null) return;
        _tint.StartValues();
        _tint.PutColor(_sourceColorID, _ColorA);
        _tint.PutColor(_connectionColorID, _ColorB);
        _tint.PutFloat(_tearProgressID, GetTearAmount());
        _tint.PutFloat(_strainID, _Strain);
        _tint.ApplyValues();
    }
    #endregion

    #region Tearing
    public void StartTearing(float pullDistance)
    {
        //called every drag frame while the bridge is pulled past its reach (in field units). Pulling harder makes it tear faster
        _pullStrength = Mathf.Min(pullDistance / _PullDampening, _MaxPullStrength);
        if (_Tearing) return; //timer already running, it just picks up the new strength

        _Tearing = true;
        ApplyLook();
        _pullTimer = StartCoroutine(TearTimer());
    }

    public void PauseTearing()
    {
        //called when the bridge stops being pulled during a drag (the rune moved back in reach, or the player is pulling another way so another bridge holds it)
        //the tear built up so far stays, so the player can pull another bridge and come back to finish this one. Strain goes back to 0
        bool changed = _Tearing || _Strain > 0f;
        _pullStrength = 0f;
        _Tearing = false;
        _Strain = 0f;
        if (_pullTimer != null) StopCoroutine(_pullTimer);
        _pullTimer = null;
        if (changed) ApplyLook(); //only when something was showing, since this is called every drag frame for bridges that aren't pulled
    }

    public void ResetTearing()
    {
        //called when the drag ends (dropped, put back or cancelled): the bridge goes back to no tear at all
        bool hadTear = _PullCurrentTime > 0f;
        _PullCurrentTime = 0f;
        PauseTearing();
        if (hadTear) ApplyLook();
    }

    public float GetPullBuiltUp()
    {
        //how much pull has built up so far. The field reads it before rebuilding its bridges (a bridge snapping mid-drag rebuilds them all)
        return _PullCurrentTime;
    }

    public void SetPullBuiltUp(float pull)
    {
        //hands a rebuilt bridge the pull its old object had built up, so another bridge snapping doesn't reset this one's tear
        _PullCurrentTime = Mathf.Clamp(pull, 0f, _PullTearRequiredTime);
        ApplyLook();
    }

    IEnumerator TearTimer()
    {
        //builds up pull every tick until the bridge snaps. The tear amount goes to the shader on every tick
        //WaitForSecondsRealtime keeps counting while the game is paused under the menu (WaitForSeconds would freeze)
        while (_PullCurrentTime < _PullTearRequiredTime)
        {
            yield return new WaitForSecondsRealtime(_PullTickTime);
            _PullCurrentTime += _PullTickTime * _pullStrength;
            ApplyLook();
        }

        _pullTimer = null;
        _Tearing = false;
        if (RuneField3DObject._Field3D != null) RuneField3DObject._Field3D.TearBridge(_Ends.x, _Ends.y); //the field removes the bridge, then redraws (which destroys this object)
    }
    #endregion
}
