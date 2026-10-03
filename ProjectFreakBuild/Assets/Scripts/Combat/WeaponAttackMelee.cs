using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Goes on a held melee weapon prefab. Reads the MeleeWeaponItem it was set up with and runs its combo:
/// wind up → active (the swing shape sweeps and hits) → recovery → next hit while attacking is held or buffered.
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

    [Tooltip("Which hit of the combo comes next, counting from 0 (read only)")]
    [SerializeField, ReadOnly] int _ComboStep;

    [Tooltip("True while the attack button is held down (read only)")]
    [SerializeField, ReadOnly] bool _IsTriggerHeld;

    [Tooltip("True if attack was pressed mid swing, so the next hit starts right after this one (read only)")]
    [SerializeField, ReadOnly] bool _IsAttackBuffered;

    //local variables
    const int MaxOverlaps = 64; //how many colliders one swing check can look at
    const int MaxLineOfSightHits = 16;
    static readonly Color GizmoFullColor = new Color(1f, 1f, 1f, 0.25f); //static readonly: made once and never changed, like a constant for types that can't be const
    static readonly Color GizmoSweptColor = new Color(1f, 0.3f, 0.2f, 1f);

    CoreStats _stats; //the wielder's stats. A reference, so it always has their current values when a hit lands
    UnitTeam _wielderTeam; //so swings skip allies. Null = nobody counts as an ally
    CharacterMovement _wielderMovement; //for locking facing and slowing during swings. Null for units that move another way
    UnitDash _wielderDash; //listened to so a dash can cancel a swing
    Collider _wielderBody; //the wielder's solid collider. Swing heights are measured from its bottom
    Coroutine _comboRoutine; //runs the swings, null while idle
    float _busyUntil; //Time.time when the current swing (including recovery) ends. Busy until then, even if a dash cancels it
    float _lastSwingEndTime = -999f; //for resetting the combo after a pause
    Vector3 _lockedForward = Vector3.forward; //the swing's direction, locked when the swing starts
    SwingShapeSO _currentSwing; //the shape currently sweeping (for the gizmo)
    float _currentSweepAmount; //how far the current sweep has gone, 0-1
    bool _isSwingActive; //true during the active part of a swing
    bool _isTurnLocked; //true while this weapon has locked the wielder's facing
    bool _isSlowingWielder; //true while this weapon has slowed the wielder
    HashSet<GameObject> _hitThisSwing = new HashSet<GameObject>(); //everything already hit by this swing, so each target is hit once per swing
    Collider[] _overlapBuffer = new Collider[MaxOverlaps]; //reused every check. The NonAlloc queries fill it instead of making a new array (less garbage)
    RaycastHit[] _lineOfSightBuffer = new RaycastHit[MaxLineOfSightHits];
    List<Collider> _colliderBuffer = new List<Collider>();

    private void OnDisable()
    {
        //put away or destroyed mid swing: stop, and give the wielder back their facing and speed
        StopCombo();
        _IsTriggerHeld = false;
    }

    private void OnDestroy()
    {
        //stop listening to the wielder's dashes
        if (_wielderDash != null) _wielderDash.startDashEvent.RemoveListener(OnWielderDash);
    }

    #region Initialize
    public void SetUpWeapon(ItemSO item, GameObject wielder, CoreStats stats)
    {
        //function the wielder calls right after spawning the weapon, so it knows its data, its stats and who is holding it
        _WeaponData = item as MeleeWeaponItem; //"as" gives null instead of an error if the item is a different kind of weapon
        if (_WeaponData == null) { Debug.LogError($"Error! {gameObject.name} was set up with {(item != null ? item.name : "nothing")}, which isn't a MeleeWeaponItem", this); return; }
        if (wielder == null) { Debug.LogError($"Error! {gameObject.name} was set up without a wielder", this); return; }
        if (stats == null) Debug.LogWarning($"Warning! {gameObject.name} was set up without stats, hits will only use the weapon's base damage...", this);

        _Wielder = wielder;
        _stats = stats;
        _wielderTeam = wielder.GetComponent<UnitTeam>();
        _wielderMovement = wielder.GetComponent<CharacterMovement>();
        _wielderBody = CombatTools.GetBodyCollider(wielder.transform, _colliderBuffer);
        if (_wielderBody == null) Debug.LogWarning($"Warning! {wielder.name} has no solid collider, swing heights will be measured from its pivot instead...", this);

        //listen for dashes so they can cancel a swing. AddListener hooks up a UnityEvent from code (it won't show in the inspector)
        if (_wielderDash != null) _wielderDash.startDashEvent.RemoveListener(OnWielderDash); //in case this weapon gets set up twice
        _wielderDash = wielder.GetComponent<UnitDash>();
        if (_wielderDash != null) _wielderDash.startDashEvent.AddListener(OnWielderDash);

        _ComboStep = 0;
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
        //attack pressed. Starts the combo, or buffers the press if a swing is already going
        if (_WeaponData == null) return;
        _IsTriggerHeld = true;

        if (_comboRoutine != null)
        {
            _IsAttackBuffered = true; //the next hit starts right after this one's recovery
            return;
        }
        _comboRoutine = StartCoroutine(ComboRoutine());
    }

    public void ReleaseAttack()
    {
        //attack let go. Holding keeps the combo going, so letting go just means it stops after this hit (unless a press was buffered)
        _IsTriggerHeld = false;
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
            _IsAttackBuffered = false;

            List<ComboStepEntry> combo = _WeaponData._Combo;
            if (combo == null || combo.Count == 0) break; //nothing to swing (warned about in LogSetupWarnings)

            //a pause longer than the reset time, or a finished combo, starts over at the first hit
            if (Time.time - _lastSwingEndTime > _WeaponData._ComboResetTime) _ComboStep = 0;
            if (_ComboStep >= combo.Count) _ComboStep = 0;

            ComboStepEntry step = combo[_ComboStep];
            if (step == null || step._Swing == null)
            {
                Debug.LogError($"Error! Hit {_ComboStep} on {_WeaponData.name} has no swing shape, skipping it", this);
                AdvanceComboStep();
                break;
            }

            //the step's timings are relative, so scale them to match the weapon's attack speed
            float timeScale = _WeaponData.GetTimeScale();
            float windupTime = step._Windup * timeScale;
            float activeTime = step._ActiveTime * timeScale;
            float recoveryTime = step._Recovery * timeScale;
            _busyUntil = Time.time + windupTime + activeTime + recoveryTime;

            StartSwing(step);

            //WIND UP
            if (windupTime > 0f) yield return new WaitForSeconds(windupTime);

            //ACTIVE: the shape sweeps and hits
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
            if (recoveryTime > 0f) yield return new WaitForSeconds(recoveryTime);

            ClearSlow();
            _lastSwingEndTime = Time.time;
            AdvanceComboStep();

            //keep going while held, or if a press came in during this hit
            if (_IsAttackBuffered == false && _IsTriggerHeld == false) break;
        }

        _comboRoutine = null;
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

        CombatTools.ShakeCameraForWielder(_Wielder, _WeaponData._ActivationShake);
    }

    void AdvanceComboStep()
    {
        //function that moves to the next hit. After the finisher it goes back to the first
        _ComboStep++;
        if (_WeaponData == null || _ComboStep >= _WeaponData._Combo.Count) _ComboStep = 0;
    }

    void StopCombo()
    {
        //function that stops the combo right away and cleans up. The busy timer is left alone on purpose
        if (_comboRoutine != null) StopCoroutine(_comboRoutine);
        _comboRoutine = null;
        _isSwingActive = false;
        _IsAttackBuffered = false;
        UnlockFacing();
        ClearSlow();
    }

    void OnWielderDash()
    {
        //dashing cancels the swing and resets the combo. The weapon stays busy until the swing would have ended
        if (_comboRoutine == null) return;
        StopCombo();
        _ComboStep = 0;
        _lastSwingEndTime = Time.time;

        //still holding attack: start again once the cancelled swing's time runs out
        if (_IsTriggerHeld) _comboRoutine = StartCoroutine(ComboRoutine());
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
        //function that lets the wielder turn again, if this weapon locked it
        if (_isTurnLocked && _wielderMovement != null) _wielderMovement.UnlockTurning();
        _isTurnLocked = false;
    }

    void ClearSlow()
    {
        //function that puts the wielder back to normal speed, if this weapon slowed it
        if (_isSlowingWielder && _wielderMovement != null) _wielderMovement.ClearMoveSpeedMultiplier();
        _isSlowingWielder = false;
    }

    Vector3 GetAimDirection()
    {
        //function for which way the swing should go: where the wielder is aiming (which can be ahead of where its body faces), flat
        Vector3 aim = Vector3.zero;
        if (_wielderMovement != null) aim = _wielderMovement.GetLookDirection();
        if (aim.sqrMagnitude < 0.0001f) aim = _Wielder.transform.forward;

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
        float reachMultiplier = _WeaponData._Reach;
        Vector3 basePoint = GetSwingBasePoint();
        Vector3 sightOrigin = GetLineOfSightOrigin();

        //everything close enough to possibly be in the shape
        int overlapCount = Physics.OverlapSphereNonAlloc(basePoint, swing.GetMaxDistance(reachMultiplier), _overlapBuffer, _HitLayers, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider hitCollider = _overlapBuffer[i];
            if (CombatTools.IsPartOf(hitCollider, _Wielder)) continue;
            if (CombatTools.IsAlly(_wielderTeam, hitCollider.GetComponentInParent<UnitTeam>())) continue;

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
            DamagePackage package = CombatTools.BuildWeaponDamagePackage(_Wielder, _stats, _WeaponData, IsRange(), step._DamageMultiplier, step._KnockbackMultiplier);
            package._StaggerPower = step._StaggerPower; //the target decides if it actually staggers (see EnemyStagger)
            damagable.TakeDamage(package);
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

        int hitCount = Physics.RaycastNonAlloc(fromPoint, toTarget / distance, _lineOfSightBuffer, distance, _HitLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _lineOfSightBuffer[i].collider;
            if (CombatTools.IsPartOf(hitCollider, _Wielder)) continue;
            if (CombatTools.IsLevelGeometry(hitCollider)) return false;
        }
        return true;
    }

    Vector3 GetSwingBasePoint()
    {
        //function for where swing shapes are measured from: the wielder's center, at the bottom of its body (its feet)
        if (_wielderBody == null) return _Wielder.transform.position;
        Bounds bodyBounds = _wielderBody.bounds;
        return new Vector3(bodyBounds.center.x, bodyBounds.min.y, bodyBounds.center.z);
    }

    Vector3 GetLineOfSightOrigin()
    {
        //function for where wall checks start: the middle of the wielder's body
        if (_wielderBody == null) return _Wielder.transform.position;
        return _wielderBody.bounds.center;
    }
    #endregion

    #region Debugging
    private void OnDrawGizmos()
    {
        //draws the active swing in the Scene view (turn on Gizmos in the Game view to see it there too)
        if (_DrawSwingGizmos == false || _isSwingActive == false) return;
        if (_currentSwing == null || _WeaponData == null || _Wielder == null) return;
        _currentSwing.DrawGizmo(GetSwingBasePoint(), _lockedForward, _WeaponData._Reach, _currentSweepAmount, GizmoFullColor, GizmoSweptColor);
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
