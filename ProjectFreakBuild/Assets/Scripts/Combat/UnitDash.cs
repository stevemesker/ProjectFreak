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
    [Header("Dash Type")]
    [Tooltip("If true, unit will dash through anything labeled as damageable")]
    public bool isPassThrough;
    

    [FoldoutGroup("---Pass Through Data---")]
    [ShowIf(nameof(isPassThrough))]
    public float _DashDistance;

    [FoldoutGroup("---Pass Through Data---")]
    [ShowIf(nameof(isPassThrough))]
    [SerializeField]public List<DamageEntry> Damage;
    //--------------------------------------------------------------------------------
    [FoldoutGroup("---Impulse Data---")]
    [HideIf(nameof(isPassThrough))]
    public float _DashImpulseStrength;
    //--------------------------------------------------------------------------------
    [Header("Event Actions")]
    public UnityEvent startDashEvent;
    public UnityEvent endDashEvent;
    //-------------------------------------------------------------------------------

    private Coroutine refreshTimer = null;
    private Vector3 dashOriginPoint;
    [SerializeField]private List<GameObject> hitList; //objects that actually need to take damage
    //private ColliderHit 
    

    #region Input
    public void DashCharacter(Vector3 direction)
    {
        if (DashCurrent <= 0 || canDash == false) return;
        DashCurrent -= 1;
        if (refreshTimer == null) refreshTimer = StartCoroutine(DashRefresh());
        //startDashEvent?.Invoke();

        Vector3 dashDirection = DashFloorDirectionCalculation(direction);
        float finalDashDistance = dashDistance;
        //Vector3 endPosition = _RB.position + (dashDirection * dashDistance);

        RaycastHit[] hits = Physics.RaycastAll(dashOriginPoint, dashDirection, dashDistance);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        hitList = new List<GameObject>();
        int stopIndex = FindDashStopperIndex(hits);

        hitListCheck(hits);

        if (stopIndex != -1)
        {
            finalDashDistance = hits[stopIndex].distance;
        }

        StartCoroutine(DashRoutine(_RB.position, _RB.position+(dashDirection*finalDashDistance)));

        /*
        if (isPassThrough) PassthroughDash(dashDirection, hits);
        else
        {
            
        }*/
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
                hitList.Add(hits[i].transform.gameObject);
            }
            else
            {
                //maybe add the functionality for passing through obstacles here? Like it doesn't get added to the hit list but does not stop the loop either
                return i;
            }
        }
        return -1;


        /*
        print("Running pass through dash");
        if (DashCurrent <= 0 || canDash == false) return;
        DashCurrent -= 1;
        if (refreshTimer == null) refreshTimer = StartCoroutine(DashRefresh());
        startDashEvent?.Invoke();

        
        RaycastHit hit;
        Vector3 adjustedDirection = direction.normalized;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, floorDetectionDistance)) adjustedDirection = Vector3.ProjectOnPlane(direction, hit.normal).normalized;
        */
        //StartCoroutine(DashRoutine(adjustedDirection));
    }

    #endregion

    #region Tools

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
    IEnumerator DashRoutine(Vector3 startPosition, Vector3 endPosition)
    {
        float elapsed = 0f;

        //Vector3 startPosition = _RB.position;
        //Vector3 endPosition = startPosition + dashDirection * dashDistance;

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(elapsed / dashDuration);

            float curveT = dashCurve.Evaluate(t);

            Vector3 targetPosition =
                Vector3.Lerp(startPosition, endPosition, curveT);

            _RB.MovePosition(targetPosition);

            yield return new WaitForFixedUpdate();
        }

        _RB.MovePosition(endPosition);

        endDashEvent?.Invoke();
    }

    IEnumerator DashRefresh()
    {
        yield return new WaitForSeconds(cooldownTime);
        DashCurrent += 1;
        if (DashCurrent < DashNumberMax) StartCoroutine(DashRefresh());
        else refreshTimer = null;
    }

    #endregion

    #region debug
    void hitListCheck(RaycastHit[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            print("Hit " + i + " | " + hits[i].transform.name);
        }
    }
    #endregion
}
