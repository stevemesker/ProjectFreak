using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

public class UnitDash : MonoBehaviour
{
    [Header("Dash Data")]
    [Tooltip("Number of simultanious dashes this unit can use before having to wait for more to refresh")]
    public int DashNumberMax = 1;
    
    [Tooltip("Current number of dashes available")]
    [SerializeField] int DashCurrent = 1;

    public float dashDistance = 2;
    public float cooldownTime = .5f;

    [Header("Dash Settings")]
    [SerializeField] Rigidbody _RB;
    [SerializeField]private bool canDash = true;
    [SerializeField, Tooltip("The rate of speed the character moves as the dash runs")] 
    AnimationCurve dashCurve;
    public float dashDuration = .5f;
    public float floorDetectionDistance = 2;
    [Tooltip("How high above the floor the direction ray should actually cast. Used for avoiding inconsistant terrain heights that shouldn't effect the dash")]
    public float floorDetectionAdjustment = 0.05f;

    //Passthrough Data----------------------------------------------------------------
    [FoldoutGroup("---Pass Through Data---")]
    public float passThroughDetectionRadius = .5f;

    [FoldoutGroup("---Pass Through Data---")]
    [SerializeField]public DamagePackage Damage;

    [FoldoutGroup("---Pass Through Data---")]
    [SerializeField] private HashSet<GameObject> hitList; //objects that actually need to take damage
    //--------------------------------------------------------------------------------
    [Header("Event Actions")]
    public UnityEvent startDashEvent;
    public UnityEvent endDashEvent;
    //-------------------------------------------------------------------------------

    private Coroutine refreshTimer = null;
    private Vector3 dashOriginPoint;

    //local variables
    Coroutine _dashRoutine; //the dash that's currently running, null when not dashing
    DamagePackage _activeDamage; //the package the current dash delivers. Null for a normal dash that doesn't deal damage
    UnitTeam _team; //this unit's side, so damaging dashes skip allies (unless the package has friendly fire). Null = hits everyone

    private void Awake()
    {
        _team = GetComponent<UnitTeam>();
    }

    private void OnDisable()
    {
        //if the unit gets turned off mid-dash, end the dash properly so its movement isn't left paused when it comes back
        if (_dashRoutine == null) return;
        StopCoroutine(_dashRoutine);
        _dashRoutine = null;
        endDashEvent?.Invoke();
    }
    
    //private ColliderHit 
    

    #region Input
    public void DashCharacter(Vector3 direction)
    {
        //Dash activation. A normal dash, no damage
        StartDash(direction, null);
    }

    public void DashPassthrough(Vector3 direction, DamagePackage dmg)
    {
        //alternate dash activation that also damages everything it passes through
        StartDash(direction, dmg);
    }

    void StartDash(Vector3 direction, DamagePackage dmg)
    {
        //function that starts a dash. dmg is the package delivered to everything passed through, or null for no damage
        if (DashCurrent <= 0 || canDash == false) return;
        _activeDamage = dmg;
        Damage = dmg; //shown in the inspector for testing
        DashCurrent -= 1;
        if (refreshTimer == null) refreshTimer = StartCoroutine(DashRefresh());

        Vector3 dashDirection = DashFloorDirectionCalculation(direction);
        float finalDashDistance = dashDistance;

        RaycastHit[] hits = Physics.RaycastAll(dashOriginPoint, dashDirection, dashDistance);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        //hitList.Clear();
        hitList = new HashSet<GameObject>();
        int stopIndex = FindDashStopperIndex(hits);

        if (stopIndex != -1)
        {
            finalDashDistance = hits[stopIndex].distance;
        }

        //chaining a dash while one is still running: stop the old one and start the new one from here
        //the start event only fires for the first dash in a chain, so start/end events always come in pairs
        if (_dashRoutine != null) StopCoroutine(_dashRoutine);
        else startDashEvent?.Invoke();

        _dashRoutine = StartCoroutine(DashRoutine(_RB.position, _RB.position+(dashDirection*finalDashDistance)));

    }

    IEnumerator DashRoutine(Vector3 startPosition, Vector3 endPosition)
    {
        //Dashing movement code

        float elapsed = 0f;
        float nextCastDistance = passThroughDetectionRadius;

        Vector3 lastCastPosition = startPosition;
        float dashDistance = Vector3.Distance(startPosition, endPosition);

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(elapsed / dashDuration);
            float curveT = dashCurve.Evaluate(t);

            Vector3 targetPosition = Vector3.Lerp(startPosition, endPosition, curveT);

            _RB.MovePosition(targetPosition);

            //damage detections
            if (_activeDamage != null)
            {
                float distanceTraveled = curveT * dashDistance;

                if (distanceTraveled >= nextCastDistance)
                {
                    SphereCastForHits(lastCastPosition, targetPosition);

                    lastCastPosition = targetPosition;
                    nextCastDistance += passThroughDetectionRadius;
                }
            }
            //-----

            yield return new WaitForFixedUpdate();
        }

        _RB.MovePosition(endPosition);

        if (_activeDamage != null)
        {
            ApplyDashDamage();
        }

        _dashRoutine = null; //clear before the event so anything listening sees the dash as finished
        endDashEvent?.Invoke();
    }
    #endregion

    #region Tools

    void ApplyDashDamage()
    {
        //function that delivers the dash's package to everything it passed through
        foreach(GameObject hits in hitList)
        {
            if (hits == null) continue; //destroyed during the dash
            IDamagable damagable = hits.GetComponent<IDamagable>();
            if (damagable != null) damagable.TakeDamage(_activeDamage);
        }
    }

    bool CanDamage(GameObject target)
    {
        //function for checking if the dash should hurt something it passed: never itself, and allies only with friendly fire
        if (target == null || target == gameObject) return false;
        if (target.transform.IsChildOf(transform)) return false; //one of this unit's own colliders
        bool hitsAllies = _activeDamage != null && _activeDamage._HitsAllies;
        return CombatTools.CanHitTeam(_team, target.GetComponentInParent<UnitTeam>(), hitsAllies);
    }

    void SphereCastForHits(Vector3 start, Vector3 end)
    {
        //raycast sphere for hit detection for dash damage

        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits = Physics.SphereCastAll(start, passThroughDetectionRadius, direction, distance);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponent<IDamagable>() != null && CanDamage(hit.transform.gameObject))
            {
                hitList.Add(hit.transform.gameObject);
            }
        }
    }

    Vector3 DashFloorDirectionCalculation(Vector3 direction)
    {
        //function that returns the final direction all dashes should move
        //Also saves out the floor position for further calculations

        RaycastHit hit;
        Vector3 adjustedDirection = direction.normalized;

        if (direction == Vector3.zero) direction = transform.forward;

        dashOriginPoint = _RB.position;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, floorDetectionDistance))
        {
            adjustedDirection = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
            dashOriginPoint = hit.point;
            dashOriginPoint.y += floorDetectionAdjustment;
        }

        return adjustedDirection;
    }

    int FindDashStopperIndex(RaycastHit[] hits)
    {
        //function that goes through the list of hit objects and forms a list of which ones can take damage and returns an index if anything obstructs the dash
        //may need to add functionality to skip over small barriers so the unit can pass through them without ending the list early
        if (hits.Length <= 0) return -1;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.gameObject.GetComponent<IDamagable>() != null)
            {
                //damageable things never stop the dash, but only enemies (or allies with friendly fire) get added to take damage
                if (_activeDamage != null && CanDamage(hits[i].transform.gameObject)) hitList.Add(hits[i].transform.gameObject);
            }
            else
            {
                //maybe add the functionality for passing through obstacles here? Like it doesn't get added to the hit list but does not stop the loop either
                return i;
            }
        }
        return -1;
    }

    IEnumerator DashRefresh()
    {
        yield return new WaitForSeconds(cooldownTime);
        DashCurrent += 1;
        if (DashCurrent < DashNumberMax) StartCoroutine(DashRefresh());
        else refreshTimer = null;
    }

    #endregion
}
