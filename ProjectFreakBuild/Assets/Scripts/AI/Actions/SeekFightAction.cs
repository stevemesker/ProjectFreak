using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SeekFightAction : AIAction
{
    //Goes looking for trouble when there's no target: heads for the nearest hostile unit even if it can't see it yet,
    //or roams far if there isn't one around. Once something is in sight, targeting picks it up and Chase takes over.
    //This is what makes a berserk shade run off the moment it's summoned.
    //Score: aggression × the roam curve, only while there's no target. With normal roam (0.3) it scores 0

    [Tooltip("How far away it can sense hostile units to go after, in meters. They don't need to be in sight")]
    [Min(0f)] public float _SearchRange = 60f;

    [Tooltip("How much roam turns this on. Left = roam 0, right = roam 1. Up = more eager to go looking")]
    public AnimationCurve _RoamCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.6f, 0f), new Keyframe(1f, 1f));

    [Tooltip("How far it roams to look when no hostile units are around, in meters")]
    [Min(0f)] public float _RoamRadius = 25f;

    public override float Score(UnitBrain brain)
    {
        //function that wants to go looking when there's no target and it's a roamer
        if (brain.GetTarget() != null) return 0f; //already found something, Chase handles it
        AIPersonality personality = brain.GetPersonality();
        return personality._Aggression * _RoamCurve.Evaluate(personality._Roam);
    }

    public override void Tick(UnitBrain brain)
    {
        //function that heads for the nearest hostile unit, or roams if there isn't one
        UnitTeam prey = FindNearestHostile(brain);
        if (prey != null)
        {
            brain.GetMover().SetDestination(prey.transform.position);
            return;
        }

        //nobody around: keep roaming to random far spots
        if (brain.GetMover().HasArrived() == false) return;
        if (NavMeshTools.TryGetRandomPoint(brain.transform.position, _RoamRadius, out Vector3 spot)) brain.GetMover().SetDestination(spot);
    }

    UnitTeam FindNearestHostile(UnitBrain brain)
    {
        //function that finds the closest hostile unit within the search range, seen or not
        UnitTeam myTeam = brain.GetTeam();
        if (myTeam == null) return null;

        UnitTeam nearest = null;
        float nearestDistance = _SearchRange;
        List<UnitTeam> units = UnitRegistry.GetUnits();
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam unit = units[i];
            if (myTeam.IsHostileTo(unit) == false) continue;

            float distance = brain.DistanceTo(unit.transform.position);
            if (distance > nearestDistance) continue;
            nearest = unit;
            nearestDistance = distance;
        }
        return nearest;
    }
}
