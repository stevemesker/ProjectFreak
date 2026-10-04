using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStats : MonoBehaviour, IUnitHealth
{
    [Header("Data")]
    [Tooltip("This enemy's stats. Health goes down here when it's hit")]
    public EnemyCoreStats eStats;

    [Header("Events")]
    [Tooltip("Runs when health reaches 0")]
    [SerializeField]
    private UnityEvent onDeath;

    //local variables
    IStaggerable _staggerable; //asked on every hit, staggered enemies always take a crit. Null if this enemy can't be staggered

    private void Awake()
    {
        //interfaces can't be dragged into the inspector, so this is found once here
        _staggerable = GetComponent<IStaggerable>();
    }

    #region Damage
    public void TakeDamage(DamagePackage dmg)
    {
        //function hooked to EnemyDamagable's onDamage event. The math lives in CombatTools.ResolveHit, shared with the player
        if (dmg == null || eStats == null) return;

        bool isStaggered = _staggerable != null && _staggerable.IsStaggered();
        HitResult result = CombatTools.ResolveHit(dmg, eStats, isStaggered);
        CombatTools.ShowHitPopups(transform.position, result);

        eStats._Health -= result._TotalDamage;
        if (eStats._Health <= 0) onDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        //function for healing. Skips the damage formula entirely. todo: a real healing system comes later (see Damage Receivers in the GDD)
        if (amount <= 0 || eStats == null) return;
        eStats._Health = Mathf.Min(eStats._Health + amount, eStats._HP); //never above max health
        CombatTools.ShowHealPopup(transform.position, amount);
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
