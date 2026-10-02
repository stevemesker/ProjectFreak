using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FollowLeaderAction : AIAction
{
    //Shades: heads back to the player. The farther away it gets, the harder the pull, so a loyal shade fights near you
    //and comes back when a fight drags it too far, while a disloyal one strays.
    //It walks to a spot beside the leader on its own side, not onto the leader, so it doesn't shove the player around.
    //Score: loyalty × the pull curve (based on how far it is from the leader)

    [Tooltip("Once it's this close to the leader, it stops walking, in meters")]
    [Min(0f)] public float _FollowDistance = 3f;

    [Tooltip("How close to the leader it aims for when it walks back, in meters. Keep it a bit under Follow Distance so it stops before reaching the leader")]
    [Min(0f)] public float _ArriveDistance = 2f;

    [Tooltip("At this distance from the leader the pull is at its strongest, in meters")]
    [Min(0.1f)] public float _LeashDistance = 15f;

    [Tooltip("How hard it's pulled back. Left = next to the leader, right = at the leash distance or beyond. Up = pulled harder")]
    public AnimationCurve _PullCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 0f), new Keyframe(1f, 1f));

    [Tooltip("How far around the spot beside the leader it can look for the NavMesh, in meters")]
    [Min(0.1f)] public float _NavMeshSnapDistance = 3f;

    public override float Score(UnitBrain brain)
    {
        //function that wants to follow more the farther it is from its leader
        Transform leader = brain.GetLeader();
        if (leader == null) return 0f;

        float distance = brain.DistanceTo(leader.position);
        float pull = _PullCurve.Evaluate(distance / _LeashDistance); //past the end of the curve it keeps the last value
        return brain.GetPersonality()._Loyalty * pull;
    }

    public override void Tick(UnitBrain brain)
    {
        //function that walks to a spot beside the leader, and stops once close enough
        Transform leader = brain.GetLeader();
        if (leader == null) return;

        if (brain.DistanceTo(leader.position) <= _FollowDistance)
        {
            brain.GetMover().StopMoving();
            return;
        }

        brain.GetMover().SetDestination(GetSpotBesideLeader(brain, leader.position));
    }

    public override bool IsFinished(UnitBrain brain)
    {
        //done once it's next to the leader
        Transform leader = brain.GetLeader();
        if (leader == null) return true;
        return brain.DistanceTo(leader.position) <= _FollowDistance;
    }

    Vector3 GetSpotBesideLeader(UnitBrain brain, Vector3 leaderPosition)
    {
        //function that finds the spot _ArriveDistance from the leader, on the side this unit is coming from
        Vector3 towardMe = brain.transform.position - leaderPosition;
        towardMe.y = 0f;
        if (towardMe.sqrMagnitude < 0.0001f) return leaderPosition; //right on top of them, nothing to work out

        Vector3 spot = leaderPosition + towardMe.normalized * _ArriveDistance;
        if (NavMeshTools.TryGetNavMeshPoint(spot, _NavMeshSnapDistance, out Vector3 navSpot)) return navSpot;
        return leaderPosition; //no NavMesh beside them (like a ledge), head for the leader instead
    }
}
