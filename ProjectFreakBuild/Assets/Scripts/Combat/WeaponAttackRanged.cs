using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Goes on a held ranged weapon prefab. Reads the WeaponRangedItem it was set up with and fires it:
/// warm up → charge → cycle (burst of patterns) → wait out the cycle → repeat while an automatic trigger is held.
/// Each volley (projectiles or hit scan) is fired by a RangedPatternFirer, which abilities share.
/// See Ranged Weapon System in the GDD.
///
////////////////////////////////////////////////
public class WeaponAttackRanged : MonoBehaviour, ITriggerable
{
    [Header("Settings")]
    [Tooltip("Layers hit scan rays (and their line of sight checks) can hit. Trigger colliders are always ignored")]
    [SerializeField] LayerMask _HitScanLayers = ~0; //~0 means every layer

    [Tooltip("Draws each hit scan ray in the Scene view for half a second (red = shot line). For testing")]
    [SerializeField] bool _DrawDebugRays;

    [Header("References")]
    [Tooltip("Where projectiles start from. Uses this object if left empty")]
    [SerializeField] Transform _Muzzle;

    [Header("Runtime Data")]
    [Tooltip("Who is holding the weapon. Set by SetUpWeapon (read only)")]
    [SerializeField, ReadOnly] GameObject _Wielder;

    [Tooltip("The weapon data this was set up with (read only)")]
    [SerializeField, ReadOnly] WeaponRangedItem _WeaponData;

    [Tooltip("True while the trigger is held down (read only)")]
    [SerializeField, ReadOnly] bool _IsTriggerHeld;

    [Tooltip("True once warm up is done. Lost when the trigger is let go (read only)")]
    [SerializeField, ReadOnly] bool _IsWarm;

    [Tooltip("How charged the current shot is, 0 to 1 (read only)")]
    [SerializeField, ReadOnly] float _ChargePercent;

    [Tooltip("Cycles fired since the last finisher (read only)")]
    [SerializeField, ReadOnly] int _FinisherCount;

    //local variables
    CoreStats _stats; //the wielder's stats. A reference, so it always has their current values when a package is built
    RangedPatternFirer _firer = new RangedPatternFirer(); //fires each volley: projectiles or hit scan (see RangedPatternFirer)
    CharacterMovement _wielderMovement; //for slowing the wielder while charging
    UnitDash _wielderDash; //listened to so a dash can cancel a charge
    Coroutine _fireRoutine; //the warm up → charge → cycle loop, null while idle
    float _busyUntil; //Time.time when the current cycle ends. Busy (can't switch weapons) until then
    float _chargeStartTime; //Time.time the current charge started (moved forward when a dash restarts it)
    bool _isCharging;
    bool _isSlowingWielder; //true while this weapon has slowed the wielder, so only it clears the slowdown
    bool _releaseRequested; //set by ReleaseAttack, so the charge loop can't miss a quick press and release

    private void OnDisable()
    {
        //put away or destroyed mid attack: stop everything so nothing keeps firing and the wielder isn't left slowed
        StopFiring();
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
        _WeaponData = item as WeaponRangedItem; //"as" gives null instead of an error if the item is a different kind of weapon
        if (_WeaponData == null) { Debug.LogError($"Error! {gameObject.name} was set up with {(item != null ? item.name : "nothing")}, which isn't a WeaponRangedItem", this); return; }
        if (wielder == null) { Debug.LogError($"Error! {gameObject.name} was set up without a wielder", this); return; }
        if (stats == null) Debug.LogWarning($"Warning! {gameObject.name} was set up without stats, attacks will only use the weapon's base damage...", this);

        _Wielder = wielder;
        _stats = stats;
        if (_Muzzle == null) _Muzzle = transform;

        _wielderMovement = wielder.GetComponent<CharacterMovement>();
        _firer.SetUp(wielder, _HitScanLayers, _DrawDebugRays, this);

        //listen for dashes so they can cancel a charge. AddListener hooks up a UnityEvent from code (it won't show in the inspector)
        if (_wielderDash != null) _wielderDash.startDashEvent.RemoveListener(OnWielderDash); //in case this weapon gets set up twice
        _wielderDash = wielder.GetComponent<UnitDash>();
        if (_wielderDash != null) _wielderDash.startDashEvent.AddListener(OnWielderDash);

        _FinisherCount = 0;
        _WeaponData.LogSetupWarnings(this);
    }

    public bool IsRange()
    {
        return true;
    }
    #endregion

    #region Weapon Activation Trigger
    public void TriggerAttack()
    {
        //trigger pressed. Starts the firing loop if it isn't already running
        if (_WeaponData == null) return;
        _IsTriggerHeld = true;

        //already running: an automatic weapon just keeps going while held, and a non automatic press mid cycle does nothing
        if (_fireRoutine != null) return;
        _fireRoutine = StartCoroutine(FireRoutine());
    }

    public void ReleaseAttack()
    {
        //trigger let go. The firing loop reads these flags to fire or cancel a charge and to stop repeating
        if (_WeaponData == null) return;
        _IsTriggerHeld = false;
        _releaseRequested = true;
        _IsWarm = false; //letting go loses the warm up
        if (_WeaponData._IsAutomatic) _FinisherCount = 0; //automatic finishers come from holding fire, so the count starts over
    }

    public bool IsBusy()
    {
        //true from the moment a cycle fires until its cycle time is over. Warm up and charging don't count, nothing has fired yet
        return Time.time < _busyUntil;
    }
    #endregion

    #region Firing Flow
    IEnumerator FireRoutine()
    {
        //runs warm up → charge → cycle, and repeats while an automatic trigger is held
        while (true)
        {
            //WARM UP: once per trigger press, kept through dashes
            if (_WeaponData._WarmUpTime > 0f && _IsWarm == false)
            {
                float warmUpEnd = Time.time + _WeaponData._WarmUpTime;
                while (Time.time < warmUpEnd)
                {
                    if (_IsTriggerHeld == false) { _fireRoutine = null; yield break; } //let go before it spun up
                    yield return null;
                }
                _IsWarm = true;
            }

            //CHARGE
            float chargeTimeUsed = 0f; //how long this shot charged, counts toward the cycle time
            float chargeMultiplier = 1f;
            float launchChargePercent = 1f; //weapons that don't charge tell projectiles they're at full power

            if (_WeaponData._UsesCharge)
            {
                StartCharging();
                bool shouldFire = false;

                while (true)
                {
                    _ChargePercent = Mathf.Clamp01((Time.time - _chargeStartTime) / _WeaponData._ChargeTime);

                    if (_WeaponData._IsAutomatic)
                    {
                        if (_ChargePercent >= 1f) { shouldFire = true; break; } //automatic charged weapons fire themselves at full charge
                        if (_IsTriggerHeld == false) break; //let go early, cancel
                    }
                    else if (_releaseRequested)
                    {
                        //non automatic weapons fire on release, unless they need a full charge and aren't there yet
                        shouldFire = _WeaponData._RequiresFullCharge == false || _ChargePercent >= 1f;
                        break;
                    }
                    yield return null;
                }

                chargeTimeUsed = Time.time - _chargeStartTime;
                StopCharging();

                if (shouldFire == false)
                {
                    _ChargePercent = 0f;
                    _fireRoutine = null;
                    yield break;
                }

                //firing early scales damage: charge % × max multiplier, so a ×2 weapon at half charge does normal damage
                chargeMultiplier = _ChargePercent * _WeaponData._MaxChargeMultiplier;
                launchChargePercent = _ChargePercent;
            }

            //PICK THE CYCLE (normal or finisher)
            bool isFinisher = AdvanceFinisherCount();
            FireCycleEntry cycle = _WeaponData._Cycle;
            if (isFinisher) cycle = _WeaponData._FinisherCycle;
            if (cycle == null) { Debug.LogError($"Error! {_WeaponData.name} has an empty cycle, can't fire", this); _fireRoutine = null; yield break; }

            //busy from now until the cycle is over. Cycle time is the longer of the attack time and charge + burst
            //charging already happened, so that part is taken off what's left to wait
            float burstTime = cycle.GetBurstTime();
            _busyUntil = Time.time + Mathf.Max(_WeaponData.GetAttackTime() - chargeTimeUsed, burstTime);

            //the package is built once when the cycle fires, so buffs gained after firing don't change shots in the air
            DamagePackage package = CombatTools.BuildWeaponDamagePackage(_Wielder, _stats, _WeaponData, IsRange(), cycle._DamageMultiplier * chargeMultiplier, 1f);
            LaunchPackage launch = new LaunchPackage();
            launch._Source = _Wielder;
            launch._ChargePercent = launchChargePercent;
            launch._IsFinisher = isFinisher;

            //FIRE: the burst fires the whole pattern _BurstCount times
            for (int burstIndex = 0; burstIndex < cycle._BurstCount; burstIndex++)
            {
                if (burstIndex > 0) yield return new WaitForSeconds(1f / Mathf.Max(cycle._BurstSpeed, 0.01f));
                _firer.FirePattern(_WeaponData, cycle, package, launch, _Muzzle.position, GetFlatForward());
            }
            _ChargePercent = 0f;

            //WAIT out the rest of the cycle
            while (Time.time < _busyUntil) yield return null;

            //automatic weapons keep going while held, everything else stops after one cycle
            if (_WeaponData._IsAutomatic == false || _IsTriggerHeld == false) break;
        }

        _fireRoutine = null;
    }

    bool AdvanceFinisherCount()
    {
        //function that counts this cycle and says whether it's the finisher. The count starts over after each finisher
        if (_WeaponData.HasFinisher() == false) return false;

        _FinisherCount++;
        if (_FinisherCount < _WeaponData._FinisherEvery) return false;

        _FinisherCount = 0;
        return true;
    }

    void StopFiring()
    {
        //function that stops the firing loop and resets everything except the busy timer
        if (_fireRoutine != null) StopCoroutine(_fireRoutine);
        _fireRoutine = null;
        StopCharging();
        _IsTriggerHeld = false;
        _IsWarm = false;
        _ChargePercent = 0f;
    }
    #endregion

    #region Charge
    void StartCharging()
    {
        //function that begins a charge and slows the wielder if the weapon says so
        _isCharging = true;
        _releaseRequested = false;
        _chargeStartTime = Time.time;
        _ChargePercent = 0f;

        if (_WeaponData._SlowWhileCharging && _wielderMovement != null)
        {
            _wielderMovement.SetMoveSpeedMultiplier(_WeaponData._ChargeMoveMultiplier);
            _isSlowingWielder = true;
        }
    }

    void StopCharging()
    {
        //function that ends a charge (fired or cancelled) and puts the wielder back to normal speed
        _isCharging = false;
        if (_isSlowingWielder && _wielderMovement != null) _wielderMovement.ClearMoveSpeedMultiplier();
        _isSlowingWielder = false;
    }

    void OnWielderDash()
    {
        //dashing cancels a charge: nothing fires and the charge starts over. Warm up is kept on purpose (rule of cool)
        if (_isCharging == false) return;
        _chargeStartTime = Time.time;
        _ChargePercent = 0f;
    }
    #endregion

    #region Aim
    Vector3 GetFlatForward()
    {
        //function for the wielder's facing with the up/down part removed, so shots fly level
        Transform facing = transform;
        if (_Wielder != null) facing = _Wielder.transform;

        Vector3 forward = facing.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return Vector3.forward; //facing straight up or down, no flat direction to use
        return forward.normalized;
    }
    #endregion

    #region Test Tools
    [Button("Test Press Trigger")]
    void TestPressTrigger()
    {
        //editor button for pressing the trigger without input (play mode only)
        if (Application.isPlaying == false) return;
        TriggerAttack();
    }

    [Button("Test Release Trigger")]
    void TestReleaseTrigger()
    {
        //editor button for letting go of the trigger (play mode only)
        if (Application.isPlaying == false) return;
        ReleaseAttack();
    }
    #endregion
}
