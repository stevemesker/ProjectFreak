using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AIAction
{
    //Parent class for every AI action (Chase, Flee, Wander...). Works like AbilityFunction: the brain holds a list of these,
    //and you pick which ones a unit has in the inspector.
    //Every think, the brain asks each action for a Score (about 0 to 1, how good an idea it is right now) and runs the best one.

    [Tooltip("Multiplies this action's score. 0 turns it off, 2 makes it twice as likely to win")]
    [Min(0f)] public float _Weight = 1f;

    public virtual string GetActionName()
    {
        //function that gives the name shown in the brain's score readout. Defaults to the class name
        return GetType().Name; //GetType().Name is the name of the class this object really is, like "ChaseAction"
    }

    public virtual float Score(UnitBrain brain)
    {
        //how good an idea this action is right now. 0 = not possible or pointless
        return 0f;
    }

    public virtual void Begin(UnitBrain brain)
    {
        //called once when this action wins and starts
    }

    public virtual void Tick(UnitBrain brain)
    {
        //called every think while this action is running. Where it updates its destination
    }

    public virtual void End(UnitBrain brain)
    {
        //called once when another action takes over, or the brain pauses
    }

    public virtual bool IsFinished(UnitBrain brain)
    {
        //true when the action is done and the brain can switch without needing a better score
        return false;
    }
}
