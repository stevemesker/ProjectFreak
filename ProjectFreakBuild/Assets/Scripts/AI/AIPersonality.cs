using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AIPersonality
{
    //The values that make the same set of actions play differently. Presets (AIPersonalitySO) hold a set of these,
    //and each brain builds its own copy from a preset + overrides when the game starts (see PersonalitySetup).
    //Because the brain has its own copy, it can change during play (like a shade getting out of control) without touching any asset.

    [Tooltip("How much it wants to fight, 0 to 1. Raises Chase, Engage, Keep Distance and Seek Fight. If it's below the Wander score it won't bother chasing at all")]
    [Range(0f, 1f)] public float _Aggression = 0.6f;

    [Tooltip("How easily it gets scared, 0 to 1. Raises Flee, especially when hurt. Cowards (high fear, low aggression) run from fights even at full health")]
    [Range(0f, 1f)] public float _Fear = 0.3f;

    [Tooltip("How much it drifts and goes looking for trouble, 0 to 1. Makes Wander cover more ground, and above about 0.6 turns on Seek Fight. A berserk shade has this maxed")]
    [Range(0f, 1f)] public float _Roam = 0.3f;

    [Tooltip("Shades only. How strongly it sticks near the player, 0 to 1. Raises Follow Leader")]
    [Range(0f, 1f)] public float _Loyalty = 0.7f;

    [Tooltip("How long it sticks with a choice before it's allowed to change its mind easily, in seconds")]
    [Min(0f)] public float _CommitTime = 1f;

    public AIPersonality Copy()
    {
        //function that makes a separate copy, so changing the copy never changes the original (like a preset asset)
        return (AIPersonality)MemberwiseClone(); //MemberwiseClone copies every field into a brand new object
    }
}
