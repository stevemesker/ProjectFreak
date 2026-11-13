using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

public class EnemyStats : MonoBehaviour, IUnitHealth
{
    [Header("Data")]
    [InfoBox("This enemy's stats come from its EnemySO (Stats foldout). This shows its own copy while playing", nameof(UsesEnemyData))]
    [DisableIf(nameof(UsesEnemyData))] //Odin: greys the stats out when they come from the EnemySO, so nobody edits the wrong place
    [Tooltip("This enemy's stats. Health goes down here when it's hit. Enemies with an EnemySO copy their stats from it when they spawn; units without one (like the target dummy) use these")]
    public EnemyCoreStats eStats;

    [Header("Events")]
    [Tooltip("Runs when health reaches 0")]
    [SerializeField]
    private UnityEvent onDeath;

    //local variables
    IStaggerable _staggerable; //asked on every hit, staggered enemies always take a crit. Null if this enemy can't be staggered
    IKillable _killable; //what happens at 0 health (EnemyDeath). Null on units that don't die, like the target dummy

    private void Awake()
    {
        //interfaces can't be dragged into the inspector, so this is found once here
        _staggerable = GetComponent<IStaggerable>();
        _killable = GetComponent<IKillable>();

        //safety net so every real enemy can die: anything with an EnemySO gets an EnemyDeath if the prefab doesn't have one
        if (_killable == null && GetEnemyData() != null) _killable = gameObject.AddComponent<EnemyDeath>();

        LoadStats();
    }

    #region Initialize
    void LoadStats()
    {
        //function that gives this enemy its own copy of its EnemySO's stats, at full health
        //no EnemySO (like the target dummy) keeps the stats typed on the prefab
        EnemySO enemyData = GetEnemyData();
        if (enemyData != null && enemyData.HasNoHP())
            Debug.LogWarning($"Warning! {enemyData.name} has no HP in its Stats foldout, so {gameObject.name} is using the stats on its prefab...", this);
        else if (enemyData != null)
            eStats = enemyData._Stats.Copy(); //a copy, so taking damage never changes the asset

        if (eStats == null) { Debug.LogError($"Error! {gameObject.name} has no stats on its EnemySO or its prefab", this); return; }

        eStats._Health = eStats._HP; //every enemy starts at full health
        if (string.IsNullOrEmpty(eStats._Name) && enemyData != null) eStats._Name = enemyData._EnemyName; //fall back to the SO's readable name
    }
    #endregion

    #region Damage
    public void TakeDamage(DamagePackage dmg)
    {
        //function hooked to EnemyDamagable's onDamage event. The math lives in CombatTools.ResolveHit, shared with the player
        if (dmg == null || eStats == null) return;
        if (_killable != null && _killable.IsDead()) return; //already dead, ignore hits that land on the same frame

        bool isStaggered = _staggerable != null && _staggerable.IsStaggered();
        HitResult result = CombatTools.ResolveHit(dmg, eStats, isStaggered);
        CombatTools.ShowHitPopups(transform.position, result);

        eStats._Health -= result._TotalDamage;
        if (eStats._Health > 0) return;
        onDeath?.Invoke();
        if (_killable != null) _killable.Kill();
    }

    public void Heal(int amount)
    {
        //function for healing. Skips the damage formula entirely. todo: a real healing system comes later (see Damage Receivers in the GDD)
        if (amount <= 0 || eStats == null) return;
        eStats._Health = Mathf.Min(eStats._Health + amount, eStats._HP); //never above max health
        CombatTools.ShowHealPopup(transform.position, amount);
    }
    #endregion

    #region Tools
    EnemySO GetEnemyData()
    {
        //function that finds this enemy's EnemySO through its EnemyMovement, the same way EnemyStagger does. Null if there isn't one
        if (TryGetComponent(out EnemyMovement movement) == false) return null;
        return movement.GetEnemyData();
    }

    bool UsesEnemyData()
    {
        //editor check for the info box: true when the stats come from an EnemySO instead of this prefab
        EnemySO enemyData = GetEnemyData();
        return enemyData != null && enemyData.HasNoHP() == false;
    }
    #endregion

    #region Unit Health Interface
    public float GetHealthPercent()
    {
        //function that gives how healthy this enemy is, 0 to 1. Used by the AI brain (like deciding when to flee)
        if (eStats == null || eStats._HP <= 0) return 1f; //no max health set, treat it as healthy instead of dividing by 0
        return Mathf.Clamp01((float)eStats._Health / eStats._HP); //(float) so the division keeps decimals instead of rounding to 0
    }
    #endregion
}
