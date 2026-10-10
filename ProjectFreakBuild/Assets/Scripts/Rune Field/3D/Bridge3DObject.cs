using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

//one bridge on the 3D rune field. It's a view: it sits on one end, points at the other and stretches its Z scale to the distance
//(the mesh starts at its origin and runs 1 m along +Z, so the Z scale is the length in meters and the tiling material follows it)
//it also runs the tear timer while a rune pulls it past its reach (used from step 3), and asks the field to remove it when it snaps
public class Bridge3DObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far above the field the bridge sits, in meters. Raise it if it disappears into the field's surface")]
    [SerializeField] float _HeightOffset = 0f;

    [Tooltip("The material's color name: _BaseColor for HDRP Lit, _UnlitColor for HDRP Unlit")]
    [SerializeField] string _ColorProperty = "_BaseColor";

    [Tooltip("Tint while the bridge is being torn")]
    [SerializeField] Color _TearColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Tearing")]
    [Tooltip("How much pull has to build up before the bridge snaps")]
    [SerializeField, Min(0.01f)] float _PullTearRequiredTime = 10f;

    [Tooltip("Cap on how hard a single pull can count")]
    [SerializeField, Min(0f)] float _MaxPullStrength = 200f;

    [Tooltip("How often the pull builds up, in seconds")]
    [SerializeField, Min(0.01f)] float _PullTickTime = 0.1f;

    [Tooltip("Bigger = pulling further counts for less. Pull strength is (field units past reach / this)")]
    [SerializeField, Min(0.01f)] float _PullDampening = 150f;

    [Header("Runtime Data")]
    [Tooltip("Rune ID at each end (-1 = the core) (read only)")]
    [SerializeField, ReadOnly] Vector2Int _Ends;

    [Tooltip("Is the tear timer running (read only)")]
    [SerializeField, ReadOnly] bool _Tearing;

    [Tooltip("How much pull has built up (read only)")]
    [SerializeField, ReadOnly] float _PullCurrentTime;

    //local variables
    Transform _endA;
    Transform _endB;
    Vector3 _fieldUp = Vector3.up;
    Vector3 _baseScale = Vector3.one; //the prefab's own scale, so only Z gets changed
    float _pullStrength;
    Coroutine _pullTimer;
    RuneFieldTint _tint;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _tint = new RuneFieldTint(RuneFieldTint.FindRenderers(transform, null), _ColorProperty);
    }

    private void OnDisable()
    {
        //stops the tear timer so it doesn't keep running on a bridge that's going away
        if (_pullTimer != null) StopCoroutine(_pullTimer);
        _pullTimer = null;
        _Tearing = false;
    }

    #region Initialize
    public void Setup(int idA, int idB, Transform endA, Transform endB, Vector3 fieldUp)
    {
        //function the field calls right after spawning this bridge
        _Ends = new Vector2Int(idA, idB);
        _endA = endA;
        _endB = endB;
        _fieldUp = fieldUp;
        UpdatePosition();
    }

    public bool Touches(int id)
    {
        //is this rune (or the core) one of the bridge's ends
        return _Ends.x == id || _Ends.y == id;
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
    #endregion

    #region Tearing
    public void StartTearing(float pullDistance)
    {
        //called every drag frame while the bridge is pulled past its reach (in field units). Pulling harder makes it tear faster
        _pullStrength = Mathf.Min(pullDistance / _PullDampening, _MaxPullStrength);
        if (_Tearing) return; //timer already running, it just picks up the new strength

        _Tearing = true;
        if (_tint != null) _tint.SetColor(_TearColor);
        _pullTimer = StartCoroutine(TearTimer());
    }

    public void StopTearing()
    {
        //called when the pull stops (the rune moved back in reach or was let go)
        _pullStrength = 0f;
        _PullCurrentTime = 0f;
        _Tearing = false;
        if (_tint != null) _tint.Clear();
        if (_pullTimer != null) StopCoroutine(_pullTimer);
        _pullTimer = null;
    }

    IEnumerator TearTimer()
    {
        //builds up pull every tick until the bridge snaps
        //WaitForSecondsRealtime keeps counting while the game is paused under the menu (WaitForSeconds would freeze)
        while (_PullCurrentTime < _PullTearRequiredTime)
        {
            yield return new WaitForSecondsRealtime(_PullTickTime);
            _PullCurrentTime += _PullTickTime * _pullStrength;
        }

        _pullTimer = null;
        _Tearing = false;
        if (RuneField3DObject._Field3D != null) RuneField3DObject._Field3D.TearBridge(_Ends.x, _Ends.y); //the field removes the bridge, then redraws (which destroys this object)
    }
    #endregion
}
