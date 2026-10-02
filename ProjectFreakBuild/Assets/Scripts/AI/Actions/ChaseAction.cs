using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChaseAction : AIAction
{
    //Runs at the target until it's within fighting range, then Engage takes over.
    //Score: aggression, but only while the target is out of fighting range

    public override float Score(UnitBrain brain)
    {
        //function that wants to chase when there's a target that's too far away to fight
        UnitTeam target = brain.GetTarget();
        if (target == null) return 0f;
        if (brain.DistanceTo(target.transform.position) <= brain.GetFightRange()) return 0f; //close enough, Engage takes over
        return brain.GetPersonality()._Aggression;
    }

    public override void Tick(UnitBrain brain)
    {
        //function that keeps heading for wherever the target is now
        UnitTeam target = brain.GetTarget();
        if (target == null) return;
        brain.GetMover().SetDestination(target.transform.position);
    }
}
