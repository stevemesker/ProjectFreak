using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "SO_Enemy_Name", menuName = "Enemy/Enemy", order = 0)]
public class EnemySO : ScriptableObject
{
    //The template for one kind of enemy. Enemies read from this and copy what they need, they never change it.
    //Sections start folded closed so you only open the ones you want to change from the defaults.

    [Header("Data")]
    [Tooltip("Readable name of this enemy")]
    public string _EnemyName;

    [Tooltip("How important this enemy is. Popcorn through Lieutenant are placed by spawners, MiniBoss and Boss are placed in their arenas")]
    public EnemyType.Rank _Rank = EnemyType.Rank.Basic;

    [Tooltip("How big this enemy is. Decides how far knockback pushes it, using the shared Size Class Rules on the Game Manager")]
    public EnemyType.SizeClass _SizeClass = EnemyType.SizeClass.Medium;

    //todo: stats and loot sections get added here as those systems are built

    [FoldoutGroup("AI", false)]
    [Tooltip("This enemy's personality: a preset plus any overrides. Its UnitBrain reads this when the game starts")]
    public PersonalitySetup _Personality = new PersonalitySetup();

    [FoldoutGroup("AI", false)]
    [Button("Reload All Units")]
    void ReloadAllUnits()
    {
        //editor button (play mode only): after changing the personality during play, every unit picks up its personality again
        if (Application.isPlaying == false) return;
        UnitBrain.ReloadAllPersonalities();
    }

    [FoldoutGroup("Movement", false)]
    [Tooltip("Top running speed, in meters per second. The player runs at about 8")]
    [Min(0f)] public float _MoveSpeed = 6f;

    [FoldoutGroup("Movement", false)]
    [Tooltip("How fast it gets up to speed, in meters per second per second. Higher = snappier starts and stops")]
    [Min(0f)] public float _Acceleration = 40f;

    [FoldoutGroup("Movement", false)]
    [Tooltip("How fast it turns, in degrees per second")]
    [Min(0f)] public float _TurnSpeed = 720f;

    [FoldoutGroup("Movement", false)]
    [Tooltip("How close it gets to its destination before stopping, in meters. Keeps it from walking into the player")]
    [Min(0f)] public float _StoppingDistance = 1.5f;

    [FoldoutGroup("Knockback", false)]
    [Tooltip("Ignore knockback completely, no matter the size class. For things like turrets")]
    public bool _KnockbackImmune = false;

    [FoldoutGroup("Knockback", false)]
    [Tooltip("How long a knockback slide lasts, in seconds")]
    [Min(0.01f)] public float _KnockbackDuration = 0.25f;

    [FoldoutGroup("Knockback", false)]
    [Tooltip("Shape of the knockback slide over its duration (0 = start, 1 = end). The default starts fast and slows down")]
    public AnimationCurve _KnockbackCurve = new AnimationCurve(new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
}
