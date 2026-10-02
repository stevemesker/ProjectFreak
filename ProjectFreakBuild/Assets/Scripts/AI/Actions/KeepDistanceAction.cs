using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class KeepDistanceAction : AIAction
{
    //For ranged units: stays about _PreferredDistance away from the target, backing off if it gets too close
    //and closing in if it's too far. Use this instead of Chase and Engage in a ranged unit's action list.
    //Score: aggression, whenever there's a target

    [Tooltip("How far from the target it likes to stay, in meters")]
    [Min(0f)] public float _PreferredDistance = 8f;

    [Tooltip("How far off the preferred distance it can be before it moves, in meters. Bigger = moves around less")]
    [Min(0f)] public float _Tolerance = 2f;

    [Tooltip("How far around the spot it's trying to reach it can look for the NavMesh, in meters")]
    [Min(0.1f)] public float _NavMeshSnapDistance = 3f;

    public override float Score(UnitBrain brain)
    {
        //function that wants to hold range whenever there's a target
        if (brain.GetTarget() == null) return 0f;
        return brain.GetPersonality()._Aggression;
    }

    public override void Tick(UnitBrain brain)
    {
        //function that moves to the preferred distance if it's outside the comfortable band, otherwise holds still
        UnitTeam target = brain.GetTarget();
        if (target == null) return;

        float distance = brain.DistanceTo(target.transform.position);
        bool tooClose = distance < _PreferredDistance - _Tolerance;
        bool tooFar = distance > _PreferredDistance + _Tolerance;
        if (tooClose == false && tooFar == false)
        {
            brain.GetMover().StopMoving();
            return;
        }

        //the spot at the preferred distance, on the line from the target through this unit
        Vector3 awayFromTarget = brain.transform.position - target.transform.position;
        awayFromTarget.y = 0f;
        if (awayFromTarget.sqrMagnitude < 0.0001f) awayFromTarget = -brain.transform.forward; //standing right on top of it, back straight off
        Vector3 spot = target.transform.position + awayFromTarget.normalized * _PreferredDistance;

        if (NavMeshTools.TryGetNavMeshPoint(spot, _NavMeshSnapDistance, out Vector3 navSpot)) brain.GetMover().SetDestination(navSpot);
        else brain.GetMover().StopMoving(); //no room to back off (like against a wall), hold where it is
    }
}
