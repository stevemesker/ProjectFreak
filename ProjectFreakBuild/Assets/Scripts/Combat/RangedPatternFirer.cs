using System.Collections;
using System.Collections.Generic;
using UnityEngine;

////////////////////////////////////////////////
///
/// Fires one volley of a ranged pattern: every projectile (or hit scan ray) in a FireCycleEntry at once, with its spread
/// and origin offsets. Hit scan rays tilt toward the enemy closest to straight ahead, pierce, and draw a tracer.
/// A plain C# class, not a component: WeaponAttackRanged owns one now (and keeps the trigger, warm up and charge flow),
/// and the Fire Projectile ability function will own one later. See Ranged Weapon System > Pattern Firer in the GDD.
///
////////////////////////////////////////////////
public class RangedPatternFirer
{
    //local variables
    const float InfiniteFireDistance = 500f; //"0 = infinite" still needs a length for the cast, in meters
    const float AimTieAngle = 5f; //targets within this many degrees of the straightest one count as a tie, and the closer one wins
    const int MaxRayHits = 32; //how many things one ray can pass through before it stops looking

    Object _logContext; //what error messages point at when clicked (the owner)
    GameObject _wielder;
    UnitTeam _wielderTeam; //the wielder's side, so hit scan skips allies. Null = nobody counts as an ally
    LayerMask _hitScanLayers; //layers hit scan rays (and their line of sight checks) can hit
    bool _drawDebugRays; //draws each hit scan ray in the Scene view for half a second
    RaycastHit[] _rayHitBuffer = new RaycastHit[MaxRayHits]; //reused every shot. The NonAlloc casts fill it instead of making a new array (less garbage)
    List<RaycastHit> _sortedHits = new List<RaycastHit>(); //this shot's hits, nearest first
    HashSet<GameObject> _damagedThisRay = new HashSet<GameObject>(); //so a target with several colliders only takes damage once per ray
    List<Collider> _colliderBuffer = new List<Collider>(); //reused when looking for a unit's body collider

    #region Initialize
    public void SetUp(GameObject wielder, LayerMask hitScanLayers, bool drawDebugRays, Object logContext)
    {
        //function the owner calls once it knows who is shooting
        _wielder = wielder;
        _wielderTeam = wielder != null ? wielder.GetComponent<UnitTeam>() : null;
        _hitScanLayers = hitScanLayers;
        _drawDebugRays = drawDebugRays;
        _logContext = logContext;
    }
    #endregion

    #region Spawning the Pattern
    public void FirePattern(IRangedFireSource source, FireCycleEntry cycle, DamagePackage package, LaunchPackage launch, Vector3 muzzlePosition, Vector3 flatForward)
    {
        //spawns one volley: every projectile (or hit scan ray) in the pattern at the same time, laid out level along flatForward
        if (source == null || cycle == null) { Debug.LogError("Error! A ranged pattern was fired without a source or cycle", _logContext); return; }

        bool isHitScan = source.IsHitScan();
        if (isHitScan == false && cycle._Projectile == null) { Debug.LogError($"Error! No projectile assigned on {source.GetSourceName()}, nothing to fire", _logContext); return; }

        CombatTools.ShakeCameraForWielder(_wielder, source.GetActivationShake()); //only shakes when the camera follows the wielder

        Vector3 forward = flatForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward; //no flat direction given, fire along world forward
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward); //Cross gives the direction at a right angle to both, which here is the wielder's right
        Quaternion forwardRotation = Quaternion.LookRotation(forward);

        int count = cycle.GetSafeProjectileCount();
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPoint = muzzlePosition + right * GetOriginOffset(cycle, i, count);
            Quaternion rotation = Quaternion.AngleAxis(GetSpreadAngle(cycle, i, count), Vector3.up) * forwardRotation; //turn the forward rotation around the up axis by the spread angle

            if (isHitScan)
            {
                FireHitScanRay(source, spawnPoint, rotation * Vector3.forward, package); //rotation * forward turns the rotation into the direction it points
                continue;
            }

            GameObject projectileObject = Object.Instantiate(cycle._Projectile, spawnPoint, rotation);
            if (projectileObject.TryGetComponent(out ProjectileObject projectile) == false)
            {
                Debug.LogError($"Error! Projectile {cycle._Projectile.name} on {source.GetSourceName()} has no ProjectileObject script", _logContext);
                Object.Destroy(projectileObject);
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
    #endregion

    #region Hit Scan
    void FireHitScanRay(IRangedFireSource source, Vector3 origin, Vector3 flatDirection, DamagePackage package)
    {
        //fires one instant ray: tilts toward the enemy closest to straight ahead (if any), then damages what's along the line
        float fireDistance = source.GetFireDistance();
        if (fireDistance <= 0f) fireDistance = InfiniteFireDistance;

        //1-2. find the target to aim at. No target = the shot stays level
        Vector3 shotDirection = flatDirection;
        Collider aimTarget = FindHitScanTarget(source, origin, flatDirection, fireDistance);
        if (aimTarget != null) shotDirection = (aimTarget.bounds.center - origin).normalized;

        //3. push a small sphere along the line and collect everything it touches
        int hitCount = Physics.SphereCastNonAlloc(origin, source.GetRayRadius(), shotDirection, _rayHitBuffer, fireDistance, _hitScanLayers, QueryTriggerInteraction.Ignore);
        SortHitsByDistance(hitCount);

        _damagedThisRay.Clear();
        int unitsHit = 0;
        Vector3 endPoint = origin + shotDirection * fireDistance; //where the tracer stops if nothing stops the shot

        for (int i = 0; i < _sortedHits.Count; i++)
        {
            RaycastHit hit = _sortedHits[i];
            Collider hitCollider = hit.collider;
            if (CombatTools.IsPartOf(hitCollider, _wielder)) continue;

            UnitTeam unit = hitCollider.GetComponentInParent<UnitTeam>();
            if (unit != null && CombatTools.CanHitTeam(_wielderTeam, unit, package._HitsAllies) == false) continue; //shots pass through allies, unless the source has friendly fire

            IDamagable damagable = hitCollider.GetComponentInParent<IDamagable>(); //GetComponentInParent checks this object, then its parents
            Vector3 hitPoint = GetHitPoint(hit, origin);

            //level geometry (not a unit, can't be damaged): the shot stops here
            if (unit == null && damagable == null)
            {
                SpawnImpact(source, hitPoint, hit.normal);
                endPoint = hitPoint;
                break;
            }

            if (damagable == null) continue; //a unit that can't take damage, the shot passes it

            //"as Component" turns the interface back into the script it came from, so we can get its GameObject
            GameObject damagedObject = (damagable as Component).gameObject;
            if (_damagedThisRay.Add(damagedObject) == false) continue; //Add returns false if it was already damaged by this ray (several colliders)

            damagable.TakeDamage(package);
            SpawnImpact(source, hitPoint, hit.normal);

            //only units count toward pierce. Destructibles (damagable, but not a unit) don't
            if (unit != null)
            {
                unitsHit++;
                if (unitsHit >= source.GetHitPierce())
                {
                    endPoint = hitPoint;
                    break;
                }
            }
        }

        //4. the tracer line from the muzzle to where the shot ended
        SpawnTracer(source, origin, endPoint);
    }

    Collider FindHitScanTarget(IRangedFireSource source, Vector3 origin, Vector3 flatDirection, float fireDistance)
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
            if (unit == null || unit.gameObject == _wielder || CombatTools.IsAlly(_wielderTeam, unit)) continue;

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
            if (sideDistance > source.GetAimWidth() / 2f + bodyRadius) continue;

            //how far up or down we'd have to tilt to hit its center. Atan2 turns height and distance into an angle
            float tiltAngle = Mathf.Abs(Mathf.Atan2(toTarget.y, forwardDistance) * Mathf.Rad2Deg);
            if (tiltAngle > source.GetVerticalAimAngle()) continue;

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

        int hitCount = Physics.RaycastNonAlloc(origin, toTarget / distance, _rayHitBuffer, distance, _hitScanLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _rayHitBuffer[i].collider;
            if (CombatTools.IsPartOf(hitCollider, _wielder)) continue;
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

    void SpawnImpact(IRangedFireSource source, Vector3 point, Vector3 surfaceNormal)
    {
        //function that spawns the impact effect at a hit, facing out of the surface
        GameObject impactEffect = source.GetImpactEffect();
        if (impactEffect == null) return;

        Quaternion rotation = Quaternion.identity;
        if (surfaceNormal.sqrMagnitude > 0.0001f) rotation = Quaternion.LookRotation(surfaceNormal);

        GameObject impact = Object.Instantiate(impactEffect, point, rotation);
        Object.Destroy(impact, source.GetImpactLifeTime()); //cleans it up even if the effect doesn't remove itself
    }

    void SpawnTracer(IRangedFireSource source, Vector3 start, Vector3 end)
    {
        //function that draws the shot's line
        if (_drawDebugRays) Debug.DrawLine(start, end, Color.red, 0.5f); //Scene view only

        GameObject tracerEffect = source.GetTracerEffect();
        if (tracerEffect == null) return;
        GameObject tracerObject = Object.Instantiate(tracerEffect, start, Quaternion.identity);
        if (tracerObject.TryGetComponent(out HitScanTracerObject tracer) == false)
        {
            Debug.LogError($"Error! Tracer effect {tracerEffect.name} on {source.GetSourceName()} has no HitScanTracerObject script, it can't draw its line", _logContext);
            Object.Destroy(tracerObject);
            return;
        }
        tracer.SetLine(start, end);
    }
    #endregion
}
