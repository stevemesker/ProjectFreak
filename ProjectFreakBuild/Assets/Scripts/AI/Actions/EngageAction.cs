using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EngageAction : AIAction
{
    //temp placeholder until abilities are hooked into the brain (ability overhaul): gets into fighting range and holds there.
    //It doesn't attack yet. Later, the unit's abilities become their own scored actions and this goes away.
    //Score: aggression, while the target is within fighting range

    [Tooltip("Extra meters past the fight range that still count as in range. Stops it flicking between Chase and Engage right at the edge")]
    [Min(0f)] public float _RangeSlack = 0.5f;

    public override float Score(UnitBrain brain)
    {
        //function that wants to engage when there's a target within fighting range
        UnitTeam target = brain.GetTarget();
        if (target == null) return 0f;
        if (brain.DistanceTo(target.transform.position) > brain.GetFightRange() + _RangeSlack) return 0f; //too far, Chase handles this
        return brain.GetPersonality()._Aggression;
    }

    public override void Begin(UnitBrain brain)
    {
        //stops where it is, it's already in range
        brain.GetMover().StopMoving();
    }

    public override void Tick(UnitBrain brain)
    {
        //function that creeps back into range if the target steps away a little, otherwise holds still
        UnitTeam target = brain.GetTarget();
        if (target == null) return;

        if (brain.DistanceTo(target.transform.position) > brain.GetFightRange()) brain.GetMover().SetDestination(target.transform.position);
        else brain.GetMover().StopMoving();
    }
}
