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
    

    #region Input
    public void DashCharacter(Vector3 direction)
    {
        if (DashCurrent <= 0 || canDash == false) return;
        DashCurrent -= 1;
        if (refreshTimer == null) refreshTimer = StartCoroutine(DashRefresh());
        startDashEvent?.Invoke();
        PassthroughDash(direction);
    }

    void PassthroughDash(Vector3 direction)
    {
        print("Running pass through dash");
        RaycastHit hit;
        Vector3 adjustedDirection = direction.normalized;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, floorDetectionDistance)) adjustedDirection = Vector3.ProjectOnPlane(direction, hit.normal).normalized;

        StartCoroutine(DashRoutine(adjustedDirection));
    }
    #endregion

    #region Tools
    IEnumerator DashRoutine(Vector3 dashDirection)
    {
        float elapsed = 0f;

        Vector3 startPosition = _RB.position;
        Vector3 endPosition = startPosition + dashDirection * dashDistance;

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
}
