using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WanderAction : AIAction
{
    //Idles around: waits a moment, walks to a random spot nearby, waits again. Enemies wander around where they started.
    //Units with a leader (shades) shuffle around where they're standing instead, and loyalty shrinks how far they go,
    //so a loyal shade mostly stays put. They never pick a spot near the leader or walk a path that cuts through them.
    //Score: a flat low score, so it only wins when nothing else is worth doing.
    //Any action that scores below this (like Chase on a unit with very low aggression) never happens

    [Tooltip("The flat score. Keep it low so real actions beat it")]
    [Range(0f, 1f)] public float _BaseScore = 0.1f;

    [Tooltip("How far it wanders with roam at 0, in meters")]
    [Min(0f)] public float _MinRadius = 3f;

    [Tooltip("How far it wanders with roam at 1, in meters")]
    [Min(0f)] public float _MaxRadius = 12f;

    [Tooltip("Shortest wait between walks, in seconds")]
    [Min(0f)] public float _MinPause = 1f;

    [Tooltip("Longest wait between walks, in seconds")]
    [Min(0f)] public float _MaxPause = 3f;

    [Header("Units With a Leader")]
    [Tooltip("How much loyalty shrinks the wander distance. 1 = a fully loyal unit doesn't wander at all, 0 = loyalty doesn't matter")]
    [Range(0f, 1f)] public float _LoyaltyStayPut = 1f;

    [Tooltip("Walks shorter than this aren't worth it, so it just stands still, in meters. This is what makes loyal shades stay put")]
    [Min(0f)] public float _ShortestWalk = 2f;

    [Tooltip("How much room it gives the leader, in meters. It won't pick a spot this close to them or walk a path that passes this close")]
    [Min(0f)] public float _LeaderPersonalSpace = 2.5f;

    //local variables
    const int SpotTries = 5; //how many random spots to try before giving up for this walk
    bool _isWalking;
    float _nextWalkTime;

    public override float Score(UnitBrain brain)
    {
        //function that always gives the same low score
        return _BaseScore;
    }

    public override void Begin(UnitBrain brain)
    {
        //stops whatever it was doing and waits a moment before the first walk
        brain.GetMover().StopMoving();
        _isWalking = false;
        _nextWalkTime = Time.time + Random.Range(_MinPause, _MaxPause);
    }

    public override void Tick(UnitBrain brain)
    {
        //function that waits, then picks a new spot once it's arrived and rested
        IUnitMover mover = brain.GetMover();

        if (_isWalking)
        {
            if (mover.HasArrived() == false) return;
            _isWalking = false;
            _nextWalkTime = Time.time + Random.Range(_MinPause, _MaxPause);
            return;
        }

        if (Time.time < _nextWalkTime) return;
        _nextWalkTime = Time.time + Random.Range(_MinPause, _MaxPause); //wait again before the next try, whether or not this one walks

        if (TryPickSpot(brain, out Vector3 spot))
        {
            mover.SetDestination(spot);
            _isWalking = true;
        }
    }

    bool TryPickSpot(UnitBrain brain, out Vector3 spot)
    {
        //function that picks a random spot to walk to, following the leader rules at the top. False means stand still this time
        spot = brain.transform.position;
        float radius = Mathf.Lerp(_MinRadius, _MaxRadius, brain.GetPersonality()._Roam); //Lerp blends between the two by roam (0 = min, 1 = max)

        Transform leader = brain.GetLeader();
        if (leader == null) return NavMeshTools.TryGetRandomPoint(brain.GetHomePosition(), radius, out spot); //no leader: wander around home

        //with a leader: wander around where it's standing, less the more loyal it is (Follow Leader keeps it near the leader)
        radius *= 1f - (brain.GetPersonality()._Loyalty * _LoyaltyStayPut);
        if (radius < _ShortestWalk) return false; //not worth moving, just stand

        for (int i = 0; i < SpotTries; i++)
        {
            if (NavMeshTools.TryGetRandomPoint(brain.transform.position, radius, out Vector3 candidate) == false) continue;
            if (brain.DistanceTo(candidate) < _ShortestWalk) continue; //too short a walk
            if (PathPassesLeader(brain.transform.position, candidate, leader.position)) continue; //would bump into the leader
            spot = candidate;
            return true;
        }
        return false;
    }

    bool PathPassesLeader(Vector3 start, Vector3 end, Vector3 leaderPosition)
    {
        //function that checks if walking straight from start to end would pass within the leader's personal space
        //(the real path can bend around walls, but straight is close enough for short wander walks)
        start.y = 0f;
        end.y = 0f;
        leaderPosition.y = 0f;

        //find the point on the walk closest to the leader, then measure how far that is from them
        Vector3 walk = end - start;
        float walkLengthSquared = walk.sqrMagnitude;
        float along = 0f; //0 = at the start of the walk, 1 = at the end
        if (walkLengthSquared > 0.0001f) along = Mathf.Clamp01(Vector3.Dot(leaderPosition - start, walk) / walkLengthSquared); //Dot measures how far along the walk the leader sits
        Vector3 closestPoint = start + walk * along;

        return Vector3.Distance(closestPoint, leaderPosition) < _LeaderPersonalSpace;
    }
}
