using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

////////////////////////////////////////////////
///
/// Lets an enemy get staggered: interrupted and stunned for a moment. Reads its settings from the EnemySO (Stagger foldout).
/// Regular enemies roll a chance on each hit (hit's stagger power × size class multiplier).
/// Mini bosses and bosses ignore the chance and stagger once each time their health drops past a threshold.
/// See Melee Weapon System > Stagger in the GDD.
///
////////////////////////////////////////////////
[RequireComponent(typeof(EnemyMovement))] //Unity adds EnemyMovement too if it's missing when this is added
public class EnemyStagger : MonoBehaviour, IStaggerable
{
    [Header("References")]
    [Tooltip("Gets stunned while staggered, and holds the EnemySO with the stagger settings. Grabbed from this object if left empty")]
    [SerializeField] EnemyMovement _Movement;

    [Tooltip("Optional. Its ability gets interrupted on a stagger. Grabbed from this object if left empty")]
    [SerializeField] AbilityInterpreter _Abilities;

    [Header("Events")]
    [Tooltip("Runs every time this enemy gets staggered. Hook up hit reaction effects, sounds or animations here")]
    public UnityEvent _OnStaggered;

    [Header("Runtime Data")]
    [Tooltip("True while staggered (read only)")]
    [SerializeField, ReadOnly] bool _IsStaggered;

    [Tooltip("True while regular hits can't stagger it, during and right after a stagger (read only)")]
    [SerializeField, ReadOnly] bool _IsStaggerImmune;

    [Tooltip("Boss only: how many health thresholds have already been used (read only)")]
    [SerializeField, ReadOnly] int _ThresholdsUsed;

    //local variables
    EnemySO _enemyData;
    IUnitHealth _health; //interfaces can't show in the inspector, so this is found with GetComponent
    List<float> _bossThresholds = new List<float>(); //a sorted copy of the EnemySO's thresholds, highest first. The SO itself is never changed
    float _staggerEndTime;
    float _immuneUntil;

    private void Awake()
    {
        if (_Movement == null) _Movement = GetComponent<EnemyMovement>();
        if (_Abilities == null) _Abilities = GetComponent<AbilityInterpreter>();
        _health = GetComponent<IUnitHealth>();

        if (_Movement == null) { Debug.LogError($"Error! No EnemyMovement found for EnemyStagger on {gameObject.name}", this); enabled = false; return; }
        _enemyData = _Movement.GetEnemyData();
        if (_enemyData == null) { Debug.LogError($"Error! No EnemySO on {gameObject.name}'s EnemyMovement, it can't be staggered", this); enabled = false; return; }

        SetUpBossThresholds();
    }

    private void Update()
    {
        //keeps the read only inspector values up to date
        _IsStaggered = Time.time < _staggerEndTime;
        _IsStaggerImmune = Time.time < _immuneUntil;
    }

    #region Initialize
    void SetUpBossThresholds()
    {
        //function that copies the boss thresholds and sorts them highest first, so they're used in order as health drops
        _bossThresholds.Clear();
        _ThresholdsUsed = 0;
        if (_enemyData.IsBossRank() == false) return;

        if (_enemyData._BossStaggerThresholds != null) _bossThresholds.AddRange(_enemyData._BossStaggerThresholds);
        _bossThresholds.Sort((a, b) => b.CompareTo(a)); //b before a sorts from highest to lowest

        if (_health == null) Debug.LogWarning($"Warning! {gameObject.name} is a boss rank but has no health script (like EnemyStats), so its stagger thresholds never trigger...", this);
    }
    #endregion

    #region Stagger Interface
    public void TakeStagger(DamagePackage dmgPackage)
    {
        //called by EnemyDamagable on every hit. Decides if this hit staggers
        if (enabled == false || dmgPackage == null) return;

        if (_enemyData.IsBossRank())
        {
            CheckBossThresholds();
            return;
        }

        if (_enemyData._StaggerImmune) return;
        if (dmgPackage._StaggerPower <= 0f) return; //this hit can't stagger
        if (Time.time < _immuneUntil) return; //already staggered, or just recovered from one

        float chance = dmgPackage._StaggerPower * GetSizeMultiplier();
        if (Random.value >= chance) return; //Random.value is a random number from 0 to 1, so a 0.4 chance passes 40% of the time

        Stagger();
    }

    public bool IsStaggered()
    {
        //function the health script asks when a hit lands. Staggered enemies always take a crit
        return Time.time < _staggerEndTime;
    }
    #endregion

    #region Stagger
    void CheckBossThresholds()
    {
        //function that staggers a boss once when its health drops past its next threshold
        //a big hit that passes several thresholds at once only staggers once, but uses them all up
        if (_health == null || _ThresholdsUsed >= _bossThresholds.Count) return;

        float healthPercent = _health.GetHealthPercent() * 100f;
        bool passedThreshold = false;
        while (_ThresholdsUsed < _bossThresholds.Count && healthPercent <= _bossThresholds[_ThresholdsUsed])
        {
            passedThreshold = true;
            _ThresholdsUsed++;
        }

        if (passedThreshold) Stagger(); //boss staggers ignore immunity, they're designed moments in the fight
    }

    void Stagger()
    {
        //function that staggers the enemy: interrupts its ability, stuns its movement (the AI brain pauses while it can't move)
        float duration = _enemyData._StaggerDuration;
        _staggerEndTime = Time.time + duration;
        _immuneUntil = _staggerEndTime + _enemyData._StaggerImmunityTime;

        if (_Abilities != null) _Abilities.InterruptAbility();
        _Movement.Stun(duration);
        _OnStaggered?.Invoke(); //?. only calls it if the event exists
    }

    float GetSizeMultiplier()
    {
        //function that gets how much stagger this enemy's size class takes, from the shared rules on the Game Manager
        if (GameManager._GameManager == null || GameManager._GameManager.GetSizeClassRules() == null) return 1f; //no rules to read, take the full chance
        return GameManager._GameManager.GetSizeClassRules().GetStaggerMultiplier(_enemyData._SizeClass);
    }
    #endregion

    #region Test Tools
    [Button("Test Stagger")]
    void TestStagger()
    {
        //editor button (play mode only) that staggers this enemy right away, skipping the chance and immunity
        if (Application.isPlaying == false || enabled == false) return;
        Stagger();
    }
    #endregion
}
