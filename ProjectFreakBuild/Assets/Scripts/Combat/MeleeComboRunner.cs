using System.Collections;
using System.Collections.Generic;
using UnityEngine;

////////////////////////////////////////////////
///
/// Plays a melee combo: wind up → active (the swing shape sweeps and hits) → recovery → next hit while attack is held or buffered.
/// A plain C# class, not a component: whoever owns it (WeaponAttackMelee now, the Melee Combo ability function later) makes one,
/// hands it a combo source, and passes presses in. It needs its owner (the host) to run coroutines, since only components can.
/// See Melee Weapon System > Attack Runner in the GDD.
///
////////////////////////////////////////////////
public class MeleeComboRunner
{
    //fires when a hit moves into a new phase (wind up, active, recovery, none), with which hit of the combo it is. For animations later
    public event System.Action<CombatType.Phase, int> PhaseChanged;

    //local variables
    const int MaxOverlaps = 64; //how many colliders one swing check can look at
    const int MaxLineOfSightHits = 16;

    MonoBehaviour _host; //the component that owns this runner. Runs the coroutines and is used as the context for error messages
    IMeleeComboSource _source; //where the combo, reach, timing and damage come from
    LayerMask _hitLayers; //layers swings (and their line of sight checks) can hit
    GameObject _wielder;
    CoreStats _stats; //the wielder's stats. A reference, so it always has their current values when a hit lands
    UnitTeam _wielderTeam; //so swings skip allies. Null = nobody counts as an ally
    CharacterMovement _wielderMovement; //for locking facing and slowing during swings. Null for units that move another way (enemies)
    UnitDash _wielderDash; //listened to so a dash can cancel a swing
    Collider _wielderBody; //the wielder's solid collider. Swing heights are measured from its bottom
    Coroutine _comboRoutine; //runs the swings, null while idle
    int _comboStep; //which hit of the combo comes next, counting from 0
    bool _isTriggerHeld;
    bool _isAttackBuffered; //attack was pressed mid swing, so the next hit starts right after this one
    float _busyUntil; //Time.time when the current swing (including recovery) ends. Busy until then, even if a dash cancels it
    float _lastSwingEndTime = -999f; //for resetting the combo after a pause
    Vector3 _lockedForward = Vector3.forward; //the swing's direction, locked when the swing starts
    SwingShapeSO _currentSwing; //the shape currently sweeping (for the gizmo)
    float _currentSweepAmount; //how far the current sweep has gone, 0-1
    bool _isSwingActive; //true during the active part of a swing
    bool _isTurnLocked; //true while this runner has locked the wielder's facing
    bool _isSlowingWielder; //true while this runner has slowed the wielder
    CombatType.Phase _phase = CombatType.Phase.None;
    HashSet<GameObject> _hitThisSwing = new HashSet<GameObject>(); //everything already hit by this swing, so each target is hit once per swing
    Collider[] _overlapBuffer = new Collider[MaxOverlaps]; //reused every check. The NonAlloc queries fill it instead of making a new array (less garbage)
    RaycastHit[] _lineOfSightBuffer = new RaycastHit[MaxLineOfSightHits];
    List<Collider> _colliderBuffer = new List<Collider>();

    public MeleeComboRunner(MonoBehaviour host)
    {
        //a constructor: runs once when the owner writes "new MeleeComboRunner(this)", and remembers who owns it
        _host = host;
    }

    #region Initialize
    public bool SetUp(IMeleeComboSource source, GameObject wielder, CoreStats stats, LayerMask hitLayers)
    {
        //function the owner calls once it knows the combo and who is swinging. Returns false if it can't work
        if (source == null) { Debug.LogError($"Error! {_host.name}'s melee runner was set up without a combo source", _host); return false; }
        if (wielder == null) { Debug.LogError($"Error! {_host.name}'s melee runner was set up without a wielder", _host); return false; }
        if (stats == null) Debug.LogWarning($"Warning! {_host.name} was set up without stats, hits will only use the base damage...", _host);

        _source = source;
        _wielder = wielder;
        _stats = stats;
        _hitLayers = hitLayers;
        _wielderTeam = wielder.GetComponent<UnitTeam>();
        _wielderMovement = wielder.GetComponent<CharacterMovement>();
        _wielderBody = CombatTools.GetBodyCollider(wielder.transform, _colliderBuffer);
        if (_wielderBody == null) Debug.LogWarning($"Warning! {wielder.name} has no solid collider, swing heights will be measured from its pivot instead...", _host);

        //listen for dashes so they can cancel a swing. AddListener hooks up a UnityEvent from code (it won't show in the inspector)
        if (_wielderDash != null) _wielderDash.startDashEvent.RemoveListener(OnWielderDash); //in case it gets set up twice
        _wielderDash = wielder.GetComponent<UnitDash>();
        if (_wielderDash != null) _wielderDash.startDashEvent.AddListener(OnWielderDash);

        _comboStep = 0;
        return true;
    }

    public void TearDown()
    {
        //function the owner calls when it's destroyed, so the wielder's dash stops calling into this runner
        if (_wielderDash != null) _wielderDash.startDashEvent.RemoveListener(OnWielderDash);
        _wielderDash = null;
    }
    #endregion

    #region Input
    public void Press()
    {
        //attack pressed. Starts the combo, or buffers the press if a swing is already going
        if (_source == null) return;
        _isTriggerHeld = true;

        if (_comboRoutine != null)
        {
            _isAttackBuffered = true; //the next hit starts right after this one's recovery
            return;
        }
        _comboRoutine = _host.StartCoroutine(ComboRoutine());
    }

    public void Release()
    {
        //attack let go. Holding keeps the combo going, so letting go just means it stops after this hit (unless a press was buffered)
        _isTriggerHeld = false;
    }

    public void Stop()
    {
        //function the owner calls when it's put away or turned off mid swing: stops, and gives the wielder back their facing and speed
        StopCombo();
        _isTriggerHeld = false;
    }

    public bool IsBusy()
    {
        //true from the start of a swing until its recovery ends, so weapons can't be switched mid swing (or by dashing out of one)
        return Time.time < _busyUntil;
    }
    #endregion

    #region Combo Flow
    IEnumerator ComboRoutine()
    {
        //runs one hit after another while attack is held or buffered
        while (true)
        {
            //a swing cancelled by a dash still runs out its time, so dashing can't skip attack speed
            while (IsBusy()) yield return null;
            _isAttackBuffered = false;

            List<ComboStepEntry> combo = _source.GetCombo();
            if (combo == null || combo.Count == 0) break; //nothing to swing (the weapon warns about this when it's set up)

            //a pause longer than the reset time, or a finished combo, starts over at the first hit
            if (Time.time - _lastSwingEndTime > _source.GetComboResetTime()) _comboStep = 0;
            if (_comboStep >= combo.Count) _comboStep = 0;

            ComboStepEntry step = combo[_comboStep];
            if (step == null || step._Swing == null)
            {
                Debug.LogError($"Error! Hit {_comboStep} on {_source.GetSourceName()} has no swing shape, skipping it", _host);
                AdvanceComboStep();
                break;
            }

            //the step's timings are relative, so scale them to match the attack speed
            float timeScale = _source.GetTimeScale();
            float windupTime = step._Windup * timeScale;
            float activeTime = step._ActiveTime * timeScale;
            float recoveryTime = step._Recovery * timeScale;
            _busyUntil = Time.time + windupTime + activeTime + recoveryTime;

            StartSwing(step);

            //WIND UP
            SetPhase(CombatType.Phase.WindUp);
            if (windupTime > 0f) yield return new WaitForSeconds(windupTime);

            //ACTIVE: the shape sweeps and hits
            SetPhase(CombatType.Phase.Active);
            _currentSwing = step._Swing;
            _isSwingActive = true;
            if (step._Swing._SweepMode == MeleeType.SweepMode.Instant)
            {
                //the whole shape hits at once at the start of the active time
                _currentSweepAmount = 1f;
                CheckHits(step);
                yield return new WaitForSeconds(activeTime);
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < activeTime)
                {
                    _currentSweepAmount = step._Swing.GetSweepAmount(elapsed / activeTime);
                    CheckHits(step);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                //one last check at the very end, so a frame hitch can't skip the end of the sweep
                _currentSweepAmount = 1f;
                CheckHits(step);
            }
            _isSwingActive = false;
            UnlockFacing(); //free to turn during recovery

            //RECOVERY
            SetPhase(CombatType.Phase.Recovery);
            if (recoveryTime > 0f) yield return new WaitForSeconds(recoveryTime);

            ClearSlow();
            _lastSwingEndTime = Time.time;
            AdvanceComboStep();

            //keep going while held, or if a press came in during this hit
            if (_isAttackBuffered == false && _isTriggerHeld == false) break;
        }

        _comboRoutine = null;
        SetPhase(CombatType.Phase.None);
    }

    void StartSwing(ComboStepEntry step)
    {
        //function that gets a swing going: locks the facing, slows the wielder, clears the hit list and shakes the camera
        _hitThisSwing.Clear();
        _currentSweepAmount = 0f;
        LockFacing();

        if (step._MoveMultiplier < 1f && _wielderMovement != null)
        {
            _wielderMovement.SetMoveSpeedMultiplier(step._MoveMultiplier);
            _isSlowingWielder = true;
        }

        CombatTools.ShakeCameraForWielder(_wielder, _source.GetActivationShake());
    }

    void AdvanceComboStep()
    {
        //function that moves to the next hit. After the finisher it goes back to the first
        _comboStep++;
        List<ComboStepEntry> combo = _source != null ? _source.GetCombo() : null;
        if (combo == null || _comboStep >= combo.Count) _comboStep = 0;
    }

    void StopCombo()
    {
        //function that stops the combo right away and cleans up. The busy timer is left alone on purpose
        if (_comboRoutine != null) _host.StopCoroutine(_comboRoutine);
        _comboRoutine = null;
        _isSwingActive = false;
        _isAttackBuffered = false;
        UnlockFacing();
        ClearSlow();
        SetPhase(CombatType.Phase.None);
    }

    void OnWielderDash()
    {
        //dashing cancels the swing and resets the combo. The weapon stays busy until the swing would have ended
        if (_comboRoutine == null) return;
        StopCombo();
        _comboStep = 0;
        _lastSwingEndTime = Time.time;

        //still holding attack: start again once the cancelled swing's time runs out
        if (_isTriggerHeld) _comboRoutine = _host.StartCoroutine(ComboRoutine());
    }

    void SetPhase(CombatType.Phase phase)
    {
        //function that records the new phase and tells anyone listening. "?." skips it if nobody is listening
        if (_phase == phase) return;
        _phase = phase;
        PhaseChanged?.Invoke(phase, _comboStep);
    }
    #endregion

    #region Facing and Speed
    void LockFacing()
    {
        //function that locks the swing's direction to where the wielder is aiming, and holds the wielder's facing there
        _lockedForward = GetAimDirection();
        if (_wielderMovement == null) return;
        _wielderMovement.LockTurning(_lockedForward);
        _isTurnLocked = true;
    }

    void UnlockFacing()
    {
        //function that lets the wielder turn again, if this runner locked it
        if (_isTurnLocked && _wielderMovement != null) _wielderMovement.UnlockTurning();
        _isTurnLocked = false;
    }

    void ClearSlow()
    {
        //function that puts the wielder back to normal speed, if this runner slowed it
        if (_isSlowingWielder && _wielderMovement != null) _wielderMovement.ClearMoveSpeedMultiplier();
        _isSlowingWielder = false;
    }

    Vector3 GetAimDirection()
    {
        //function for which way the swing should go: where the wielder is aiming (which can be ahead of where its body faces), flat
        Vector3 aim = Vector3.zero;
        if (_wielderMovement != null) aim = _wielderMovement.GetLookDirection();
        if (aim.sqrMagnitude < 0.0001f) aim = _wielder.transform.forward;

        aim.y = 0f;
        if (aim.sqrMagnitude < 0.0001f) return Vector3.forward; //facing straight up or down, no flat direction to use
        return aim.normalized;
    }
    #endregion

    #region Hit Check
    void CheckHits(ComboStepEntry step)
    {
        //function that hits everything inside the part of the swing swept so far. Runs every frame of the active time
        SwingShapeSO swing = step._Swing;
        float reachMultiplier = _source.GetReach();
        Vector3 basePoint = GetSwingBasePoint();
        Vector3 sightOrigin = GetLineOfSightOrigin();

        //everything close enough to possibly be in the shape
        int overlapCount = Physics.OverlapSphereNonAlloc(basePoint, swing.GetMaxDistance(reachMultiplier), _overlapBuffer, _hitLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider hitCollider = _overlapBuffer[i];
            if (CombatTools.IsPartOf(hitCollider, _wielder)) continue;
            if (CombatTools.CanHitTeam(_wielderTeam, hitCollider.GetComponentInParent<UnitTeam>(), _source.HitsAllies()) == false) continue; //skips allies unless the source has friendly fire

            IDamagable damagable = hitCollider.GetComponentInParent<IDamagable>(); //GetComponentInParent checks this object, then its parents
            if (damagable == null) continue;

            GameObject target = (damagable as Component).gameObject; //"as Component" turns the interface back into the script, so we can get its GameObject
            if (_hitThisSwing.Contains(target)) continue; //already hit by this swing (several colliders, or hit on an earlier frame)

            //big enemies are hit by their body: use the point on their collider closest to the wielder, not their center
            Vector3 hitPoint = GetClosestPoint(hitCollider, sightOrigin);
            if (swing.IsInsideShape(basePoint, _lockedForward, hitPoint, reachMultiplier, _currentSweepAmount) == false) continue;
            if (HasLineOfSight(sightOrigin, hitPoint) == false) continue; //swings don't go through walls

            _hitThisSwing.Add(target);
            //the package is built at the moment of the hit, with the wielder's stats right now
            DamagePackage package = _source.BuildHitPackage(_wielder, _stats, step);
            if (package != null) damagable.TakeDamage(package);
        }
    }

    Vector3 GetClosestPoint(Collider hitCollider, Vector3 fromPoint)
    {
        //function for the point on a collider closest to fromPoint. ClosestPoint only works on box, sphere, capsule and convex mesh colliders,
        //so other mesh colliders use their bounding box instead
        MeshCollider meshCollider = hitCollider as MeshCollider;
        if (meshCollider != null && meshCollider.convex == false) return hitCollider.bounds.ClosestPoint(fromPoint);
        return hitCollider.ClosestPoint(fromPoint);
    }

    bool HasLineOfSight(Vector3 fromPoint, Vector3 toPoint)
    {
        //function for checking no level geometry (walls, floors) is between the wielder and the hit point
        Vector3 toTarget = toPoint - fromPoint;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return true;

        int hitCount = Physics.RaycastNonAlloc(fromPoint, toTarget / distance, _lineOfSightBuffer, distance, _hitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _lineOfSightBuffer[i].collider;
            if (CombatTools.IsPartOf(hitCollider, _wielder)) continue;
            if (CombatTools.IsLevelGeometry(hitCollider)) return false;
        }
        return true;
    }

    Vector3 GetSwingBasePoint()
    {
        //function for where swing shapes are measured from: the wielder's center, at the bottom of its body (its feet)
        if (_wielderBody == null) return _wielder.transform.position;
        Bounds bodyBounds = _wielderBody.bounds;
        return new Vector3(bodyBounds.center.x, bodyBounds.min.y, bodyBounds.center.z);
    }

    Vector3 GetLineOfSightOrigin()
    {
        //function for where wall checks start: the middle of the wielder's body
        if (_wielderBody == null) return _wielder.transform.position;
        return _wielderBody.bounds.center;
    }
    #endregion

    #region Readouts
    public int GetComboStep() { return _comboStep; } //which hit comes next, counting from 0
    public bool IsTriggerHeld() { return _isTriggerHeld; }
    public bool IsAttackBuffered() { return _isAttackBuffered; }
    public CombatType.Phase GetPhase() { return _phase; }
    #endregion

    #region Debugging
    public void DrawGizmos(Color fullColor, Color sweptColor)
    {
        //function the owner calls from its OnDrawGizmos to draw the active swing (faint = whole shape, bright = swept so far)
        if (_isSwingActive == false || _currentSwing == null || _source == null || _wielder == null) return;
        _currentSwing.DrawGizmo(GetSwingBasePoint(), _lockedForward, _source.GetReach(), _currentSweepAmount, fullColor, sweptColor);
    }
    #endregion
}
