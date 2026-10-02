using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FleeAction : AIAction
{
    //Runs away from the target until it's at a safe distance.
    //Score: fear × (how much it wants to run), where the urge is the bigger of _BaseFleeUrge and the low health curve.
    //So brave units only run when badly hurt, and cowards (high fear, low aggression) run even at full health

    [Tooltip("Once it's this far from the target, it stops running, in meters")]
    [Min(0f)] public float _SafeDistance = 12f;

    [Tooltip("How far ahead it picks each spot to run to, in meters")]
    [Min(1f)] public float _FleeStep = 8f;

    [Tooltip("How much it wants to run even when unhurt, 0 to 1. Gets multiplied by fear")]
    [Range(0f, 1f)] public float _BaseFleeUrge = 0.3f;

    [Tooltip("How much it wants to run based on how hurt it is. Left = unhurt, right = almost dead. Up = wants to run more")]
    public AnimationCurve _LowHealthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0f), new Keyframe(0.8f, 1f), new Keyframe(1f, 1f));

    [Tooltip("How far around each escape spot it can look for the NavMesh, in meters")]
    [Min(0.1f)] public float _NavMeshSnapDistance = 3f;

    //local variables
    static readonly float[] EscapeAngles = { 0f, 45f, -45f, 90f, -90f }; //directions to try, in degrees off "straight away from the target"

    public override float Score(UnitBrain brain)
    {
        //function that wants to run when there's a target within the safe distance, more when hurt
        UnitTeam target = brain.GetTarget();
        if (target == null) return 0f;
        if (brain.DistanceTo(target.transform.position) >= _SafeDistance) return 0f; //already safe

        float missingHealth = 1f - brain.GetHealthPercent();
        float urge = Mathf.Max(_BaseFleeUrge, _LowHealthCurve.Evaluate(missingHealth));
        return brain.GetPersonality()._Fear * urge;
    }

    public override void Tick(UnitBrain brain)
    {
        //function that picks a spot away from the target to run to, trying a few angles if straight away is blocked
        UnitTeam target = brain.GetTarget();
        if (target == null) return;

        Vector3 away = brain.transform.position - target.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -brain.transform.forward; //standing right on top of it, run straight back
        away.Normalize();

        for (int i = 0; i < EscapeAngles.Length; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, EscapeAngles[i], 0f) * away; //turns the direction around the up axis
            Vector3 spot = brain.transform.position + direction * _FleeStep;
            if (NavMeshTools.TryGetNavMeshPoint(spot, _NavMeshSnapDistance, out Vector3 navSpot))
            {
                brain.GetMover().SetDestination(navSpot);
                return;
            }
        }

        brain.GetMover().StopMoving(); //cornered, nowhere to run
    }

    public override bool IsFinished(UnitBrain brain)
    {
        //done once it's at a safe distance (or the target is gone)
        UnitTeam target = brain.GetTarget();
        if (target == null) return true;
        return brain.DistanceTo(target.transform.position) >= _SafeDistance;
    }
}
