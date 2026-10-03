using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Goes on a held ranged weapon prefab. Reads the WeaponRangedItem it was set up with and fires it:
/// warm up → charge → cycle (burst of patterns) → wait out the cycle → repeat while an automatic trigger is held.
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

    //hit scan constants
    const float InfiniteFireDistance = 500f; //"0 = infinite" still needs a length for the cast, in meters
    const float AimTieAngle = 5f; //targets within this many degrees of the straightest one count as a tie, and the closer one wins
    const int MaxRayHits = 32; //how many things one ray can pass through before it stops looking

    //local variables
    CoreStats _stats; //the wielder's stats. A reference, so it always has their current values when a package is built
    UnitTeam _wielderTeam; //the wielder's side, so hit scan skips allies. Null = nobody counts as an ally
    RaycastHit[] _rayHitBuffer = new RaycastHit[MaxRayHits]; //reused every shot. The NonAlloc casts fill it instead of making a new array (less garbage)
    List<RaycastHit> _sortedHits = new List<RaycastHit>(); //this shot's hits, nearest first
    HashSet<GameObject> _damagedThisRay = new HashSet<GameObject>(); //so a target with several colliders only takes damage once per ray
    List<Collider> _colliderBuffer = new List<Collider>(); //reused when looking for a unit's body collider
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
        _wielderTeam = wielder.GetComponent<UnitTeam>();

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
                FirePattern(cycle, package, launch);
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

    #region Spawning the Pattern
    void FirePattern(FireCycleEntry cycle, DamagePackage package, LaunchPackage launch)
    {
        //spawns one volley: every projectile (or hit scan ray) in the pattern at the same time
        bool isHitScan = _WeaponData._IsHitScan;
        if (isHitScan == false && cycle._Projectile == null) { Debug.LogError($"Error! No projectile assigned on {_WeaponData.name}, nothing to fire", this); return; }

        ShakeCamera();

        //the pattern is laid out level, along the wielder's facing
        Vector3 forward = GetFlatForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward); //Cross gives the direction at a right angle to both, which here is the wielder's right
        Quaternion forwardRotation = Quaternion.LookRotation(forward);

        int count = cycle.GetSafeProjectileCount();
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPoint = _Muzzle.position + right * GetOriginOffset(cycle, i, count);
            Quaternion rotation = Quaternion.AngleAxis(GetSpreadAngle(cycle, i, count), Vector3.up) * forwardRotation; //turn the forward rotation around the up axis by the spread angle

            if (isHitScan)
            {
                FireHitScanRay(spawnPoint, rotation * Vector3.forward, package); //rotation * forward turns the rotation into the direction it points
                continue;
            }

            GameObject projectileObject = Instantiate(cycle._Projectile, spawnPoint, rotation);
            if (projectileObject.TryGetComponent(out ProjectileObject projectile) == false)
            {
                Debug.LogError($"Error! Projectile {cycle._Projectile.name} on {_WeaponData.name} has no ProjectileObject script", this);
                Destroy(projectileObject);
                return;
            }
            projectile.LaunchProjectile(package, launch, cycle._ProjectileSpeed, cycle._ProjectileLifeTime);
        }
    }

    float GetOriginOffset(FireCycleEntry cycle, int index, int count)
    {
        //function for how far left/right of the muzzle projectile [index] starts, in meters
        float width = cycle._FiringOrigin;
        if (width <= 0f) return 0f;
        if (cycle._RandomOrigin) return Random.Range(-width / 2f, width / 2f);
        if (count <= 1) return 0f;

        //evenly spaced from the left edge to the right edge
        return -width / 2f + width * index / (count - 1);
    }

    float GetSpreadAngle(FireCycleEntry cycle, int index, int count)
    {
        //function for which way projectile [index] points, in degrees away from forward (negative = left)
        float spread = cycle._Spread;
        float angle = 0f;

        if (count > 1)
        {
            float step;
            if (Mathf.Abs(spread) >= 360f) step = spread / count; //full circle: split it count ways so the first and last don't land on top of each other
            else step = spread / (count - 1); //an arc: the ends land exactly on its edges

            angle = -spread / 2f + step * index;
        }

        if (cycle._SpreadRandomness > 0f) angle += Random.Range(-cycle._SpreadRandomness, cycle._SpreadRandomness);
        return angle;
    }

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

    #region Hit Scan
    void FireHitScanRay(Vector3 origin, Vector3 flatDirection, DamagePackage package)
    {
        //fires one instant ray: tilts toward the enemy closest to straight ahead (if any), then damages what's along the line
        float fireDistance = _WeaponData._FireDistance;
        if (fireDistance <= 0f) fireDistance = InfiniteFireDistance;

        //1-2. find the target to aim at. No target = the shot stays level
        Vector3 shotDirection = flatDirection;
        Collider aimTarget = FindHitScanTarget(origin, flatDirection, fireDistance);
        if (aimTarget != null) shotDirection = (aimTarget.bounds.center - origin).normalized;

        //3. push a small sphere along the line and collect everything it touches
        int hitCount = Physics.SphereCastNonAlloc(origin, _WeaponData._RayRadius, shotDirection, _rayHitBuffer, fireDistance, _HitScanLayers, QueryTriggerInteraction.Ignore);
        SortHitsByDistance(hitCount);

        _damagedThisRay.Clear();
        int unitsHit = 0;
        Vector3 endPoint = origin + shotDirection * fireDistance; //where the tracer stops if nothing stops the shot

        for (int i = 0; i < _sortedHits.Count; i++)
        {
            RaycastHit hit = _sortedHits[i];
            Collider hitCollider = hit.collider;
            if (CombatTools.IsPartOf(hitCollider, _Wielder)) continue;

            UnitTeam unit = hitCollider.GetComponentInParent<UnitTeam>();
            if (unit != null && CombatTools.IsAlly(_wielderTeam, unit)) continue; //shots pass through allies

            IDamagable damagable = hitCollider.GetComponentInParent<IDamagable>(); //GetComponentInParent checks this object, then its parents
            Vector3 hitPoint = GetHitPoint(hit, origin);

            //level geometry (not a unit, can't be damaged): the shot stops here
            if (unit == null && damagable == null)
            {
                SpawnImpact(hitPoint, hit.normal);
                endPoint = hitPoint;
                break;
            }

            if (damagable == null) continue; //a unit that can't take damage, the shot passes it

            //"as Component" turns the interface back into the script it came from, so we can get its GameObject
            GameObject damagedObject = (damagable as Component).gameObject;
            if (_damagedThisRay.Add(damagedObject) == false) continue; //Add returns false if it was already damaged by this ray (several colliders)

            damagable.TakeDamage(package);
            SpawnImpact(hitPoint, hit.normal);

            //only units count toward pierce. Destructibles (damagable, but not a unit) don't
            if (unit != null)
            {
                unitsHit++;
                if (unitsHit >= _WeaponData._HitPierce)
                {
                    endPoint = hitPoint;
                    break;
                }
            }
        }

        //4. the tracer line from the muzzle to where the shot ended
        SpawnTracer(origin, endPoint);
    }

    Collider FindHitScanTarget(Vector3 origin, Vector3 flatDirection, float fireDistance)
    {
        //function that finds the enemy this ray should tilt toward: on the aim line, within the up/down angle, in sight,
        //and closest to straight ahead (near ties go to the closer one). Uses the UnitRegistry instead of searching the scene
        Collider bestTarget = null;
        float bestAngle = 0f;
        float bestDistance = 0f;

        List<UnitTeam> units = UnitRegistry.GetUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam unit = units[i];
            if (unit == null || unit.gameObject == _Wielder || CombatTools.IsAlly(_wielderTeam, unit)) continue;

            Collider body = CombatTools.GetBodyCollider(unit, _colliderBuffer);
            if (body == null) continue;

            Vector3 targetCenter = body.bounds.center;
            Vector3 toTarget = targetCenter - origin;
            Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);

            //how far along the aim line it is (Dot = how much of one direction points along the other)
            float forwardDistance = Vector3.Dot(flatToTarget, flatDirection);
            if (forwardDistance <= 0f || forwardDistance > fireDistance) continue; //behind us or out of range

            //how far to the side of the aim line it is. Big enemies count by their body, not just their center
            float sideDistance = (flatToTarget - flatDirection * forwardDistance).magnitude;
            float bodyRadius = Mathf.Max(body.bounds.extents.x, body.bounds.extents.z);
            if (sideDistance > _WeaponData._AimWidth / 2f + bodyRadius) continue;

            //how far up or down we'd have to tilt to hit its center. Atan2 turns height and distance into an angle
            float tiltAngle = Mathf.Abs(Mathf.Atan2(toTarget.y, forwardDistance) * Mathf.Rad2Deg);
            if (tiltAngle > _WeaponData._VerticalAimAngle) continue;

            if (HasLineOfSight(origin, targetCenter, unit.transform) == false) continue;

            //keep the straightest target. Within AimTieAngle of each other, the closer one wins
            bool isBetter = false;
            if (bestTarget == null) isBetter = true;
            else if (tiltAngle < bestAngle - AimTieAngle) isBetter = true;
            else if (Mathf.Abs(tiltAngle - bestAngle) <= AimTieAngle && forwardDistance < bestDistance) isBetter = true;

            if (isBetter)
            {
                bestTarget = body;
                bestAngle = tiltAngle;
                bestDistance = forwardDistance;
            }
        }

        return bestTarget;
    }

    bool HasLineOfSight(Vector3 origin, Vector3 targetPoint, Transform targetRoot)
    {
        //function for checking nothing solid (level geometry) is between the muzzle and the target
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return true;

        int hitCount = Physics.RaycastNonAlloc(origin, toTarget / distance, _rayHitBuffer, distance, _HitScanLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _rayHitBuffer[i].collider;
            if (CombatTools.IsPartOf(hitCollider, _Wielder)) continue;
            if (hitCollider.transform.IsChildOf(targetRoot)) continue; //the target itself
            if (CombatTools.IsLevelGeometry(hitCollider)) return false;
        }
        return true;
    }

    void SortHitsByDistance(int hitCount)
    {
        //function that copies the cast's hits into _sortedHits, nearest first (casts don't return them in order)
        _sortedHits.Clear();
        for (int i = 0; i < hitCount; i++) _sortedHits.Add(_rayHitBuffer[i]);
        _sortedHits.Sort((a, b) => a.distance.CompareTo(b.distance)); //the (a, b) => part is a small inline function that says how to compare two hits
    }

    Vector3 GetHitPoint(RaycastHit hit, Vector3 origin)
    {
        //function for where a hit happened. Things the ray started inside report distance 0 and no point, so use the muzzle
        if (hit.distance <= 0f) return origin;
        return hit.point;
    }

    void SpawnImpact(Vector3 point, Vector3 surfaceNormal)
    {
        //function that spawns the impact effect at a hit, facing out of the surface
        if (_WeaponData._ImpactEffect == null) return;

        Quaternion rotation = Quaternion.identity;
        if (surfaceNormal.sqrMagnitude > 0.0001f) rotation = Quaternion.LookRotation(surfaceNormal);

        GameObject impact = Instantiate(_WeaponData._ImpactEffect, point, rotation);
        Destroy(impact, _WeaponData._ImpactLifeTime); //cleans it up even if the effect doesn't remove itself
    }

    void SpawnTracer(Vector3 start, Vector3 end)
    {
        //function that draws the shot's line
        if (_DrawDebugRays) Debug.DrawLine(start, end, Color.red, 0.5f); //Scene view only

        if (_WeaponData._TracerEffect == null) return;
        GameObject tracerObject = Instantiate(_WeaponData._TracerEffect, start, Quaternion.identity);
        if (tracerObject.TryGetComponent(out HitScanTracerObject tracer) == false)
        {
            Debug.LogError($"Error! Tracer effect {_WeaponData._TracerEffect.name} on {_WeaponData.name} has no HitScanTracerObject script, it can't draw its line", this);
            Destroy(tracerObject);
            return;
        }
        tracer.SetLine(start, end);
    }
    #endregion

    #region Effects
    void ShakeCamera()
    {
        //function for the weapon's activation shake (the shared helper only shakes when the camera follows the wielder)
        CombatTools.ShakeCameraForWielder(_Wielder, _WeaponData._ActivationShake);
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
