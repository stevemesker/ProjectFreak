using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// One swing type (Slash, Thrust, Spin...), shared by every melee weapon. Each weapon scales the reach with its own _Reach.
/// The shape is an arc around the wielder with a height range, measured from the bottom of the wielder's body.
/// See Melee Weapon System in the GDD.
///
////////////////////////////////////////////////
[CreateAssetMenu(fileName = "SO_Swing_Name", menuName = "Combat/Swing Shape", order = 1)]
public class SwingShapeSO : ScriptableObject
{
    [Title("Sweep")]
    [Tooltip("Angle: the arc sweeps sideways from start to end angle (slashes, spins). Reach: the arc grows outward from the wielder (thrusts). Instant: the whole shape hits at once (slams, chops)")]
    public MeleeType.SweepMode _SweepMode = MeleeType.SweepMode.Angle;

    [Tooltip("Where the swing starts, in degrees from the wielder's forward. Negative = left, positive = right")]
    [Range(-180f, 180f)] public float _StartAngle = -80f;

    [Tooltip("Where the swing ends, in degrees. A slash goes -80 to 80, a backslash 80 to -80, a full spin -180 to 180")]
    [Range(-180f, 180f)] public float _EndAngle = 80f;

    [Tooltip("How the sweep moves over the active time (0 = start, 1 = end). Lets a swing start slow and whip through")]
    public AnimationCurve _SweepCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Title("Size")]
    [Tooltip("How far the swing reaches at 1× weapon reach, in meters from the center of the wielder")]
    [Min(0.1f)] public float _Reach = 2f;

    [Tooltip("Things closer than this to the wielder's center don't get hit, in meters. Usually 0")]
    [Min(0f)] public float _InnerRadius = 0f;

    [Tooltip("How far below the bottom of the wielder's body the swing reaches, in meters")]
    [Min(0f)] public float _HeightBelow = 0.75f;

    [Tooltip("How far above the bottom of the wielder's body the swing reaches, in meters")]
    [Min(0f)] public float _HeightAbove = 2.5f;

    //local variables
    const float GizmoStepDegrees = 5f; //how finely the gizmo arcs are drawn

    #region Shape
    public float GetSweepAmount(float timePercent)
    {
        //function that turns how far through the active time we are (0-1) into how far the sweep has gone (0-1), using the curve
        return Mathf.Clamp01(_SweepCurve.Evaluate(Mathf.Clamp01(timePercent)));
    }

    public float GetMaxDistance(float reachMultiplier)
    {
        //function for the furthest any part of this shape gets from the wielder (used to size the overlap check)
        float reach = _Reach * reachMultiplier;
        float height = Mathf.Max(_HeightAbove, _HeightBelow);
        return Mathf.Sqrt(reach * reach + height * height); //the corner of the shape: reach out and height up
    }

    public bool IsInsideShape(Vector3 basePoint, Vector3 forward, Vector3 point, float reachMultiplier, float sweepAmount)
    {
        //function for checking if a point is inside the part of the swing that has been swept so far
        //basePoint = the wielder's center, at the bottom of its body. forward = the locked swing direction (flat)

        //height check: within the band above/below the wielder's feet
        float height = point.y - basePoint.y;
        if (height > _HeightAbove || height < -_HeightBelow) return false;

        //distance check, on the flat ground plane
        Vector3 flatOffset = point - basePoint;
        flatOffset.y = 0f;
        float distance = flatOffset.magnitude;
        if (distance < _InnerRadius || distance > GetCurrentReach(reachMultiplier, sweepAmount)) return false;

        //angle check. Something right on top of the wielder counts as straight ahead
        float angle = 0f;
        if (distance > 0.001f) angle = Vector3.SignedAngle(forward, flatOffset, Vector3.up); //SignedAngle around up: negative = left, positive = right
        GetCurrentAngles(sweepAmount, out float minAngle, out float maxAngle);
        return angle >= minAngle && angle <= maxAngle;
    }

    public void GetCurrentAngles(float sweepAmount, out float minAngle, out float maxAngle)
    {
        //function for the angle range swept so far. "out" lets one function hand back two numbers
        float fromAngle = _StartAngle;
        float toAngle = _EndAngle;
        if (_SweepMode == MeleeType.SweepMode.Angle) toAngle = Mathf.Lerp(_StartAngle, _EndAngle, sweepAmount);

        //Min/Max so it works in either direction (a backslash goes from right to left)
        minAngle = Mathf.Min(fromAngle, toAngle);
        maxAngle = Mathf.Max(fromAngle, toAngle);
    }

    public float GetCurrentReach(float reachMultiplier, float sweepAmount)
    {
        //function for how far the swing reaches right now. Reach sweeps grow outward, the others are full length straight away
        float fullReach = _Reach * reachMultiplier;
        if (_SweepMode == MeleeType.SweepMode.Reach) return Mathf.Lerp(_InnerRadius, fullReach, sweepAmount);
        return fullReach;
    }
    #endregion

    #region Gizmo
    public void DrawGizmo(Vector3 basePoint, Vector3 forward, float reachMultiplier, float sweepAmount, Color fullColor, Color sweptColor)
    {
        //draws the whole shape faintly, and the part swept so far brightly. Call it from an OnDrawGizmos function
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        forward.Normalize();

        DrawArcShape(basePoint, forward, _StartAngle, _EndAngle, _Reach * reachMultiplier, fullColor);

        GetCurrentAngles(sweepAmount, out float minAngle, out float maxAngle);
        DrawArcShape(basePoint, forward, minAngle, maxAngle, GetCurrentReach(reachMultiplier, sweepAmount), sweptColor);
    }

    void DrawArcShape(Vector3 basePoint, Vector3 forward, float fromAngle, float toAngle, float reach, Color color)
    {
        //draws one arc band: an arc at the top and bottom heights, joined at the ends and back to the inner radius
        Gizmos.color = color;
        Vector3 bottom = basePoint + Vector3.down * _HeightBelow;
        Vector3 top = basePoint + Vector3.up * _HeightAbove;

        float minAngle = Mathf.Min(fromAngle, toAngle);
        float maxAngle = Mathf.Max(fromAngle, toAngle);
        int steps = Mathf.Max(1, Mathf.CeilToInt((maxAngle - minAngle) / GizmoStepDegrees));

        Vector3 lastDirection = Quaternion.AngleAxis(minAngle, Vector3.up) * forward; //turn forward around the up axis by the angle
        for (int i = 1; i <= steps; i++)
        {
            float angle = Mathf.Lerp(minAngle, maxAngle, (float)i / steps);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            Gizmos.DrawLine(bottom + lastDirection * reach, bottom + direction * reach);
            Gizmos.DrawLine(top + lastDirection * reach, top + direction * reach);
            lastDirection = direction;
        }

        //the two edges of the arc
        DrawArcEdge(bottom, top, Quaternion.AngleAxis(minAngle, Vector3.up) * forward, reach);
        DrawArcEdge(bottom, top, Quaternion.AngleAxis(maxAngle, Vector3.up) * forward, reach);
    }

    void DrawArcEdge(Vector3 bottom, Vector3 top, Vector3 direction, float reach)
    {
        //draws one end of the arc: a box outline from the inner radius out to the reach, bottom to top
        Vector3 innerBottom = bottom + direction * _InnerRadius;
        Vector3 innerTop = top + direction * _InnerRadius;
        Vector3 outerBottom = bottom + direction * reach;
        Vector3 outerTop = top + direction * reach;
        Gizmos.DrawLine(innerBottom, outerBottom);
        Gizmos.DrawLine(innerTop, outerTop);
        Gizmos.DrawLine(outerBottom, outerTop);
        Gizmos.DrawLine(innerBottom, innerTop);
    }
    #endregion
}
