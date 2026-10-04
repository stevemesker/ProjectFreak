using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class PlayerDamegable : MonoBehaviour
{
    //Handles damage to the player. Hits reach it through the EnemyDamagable on the player prefab (its onDamage event calls TakeDamage),
    //so it gets the same aggro/knockback/stagger checks as every other unit. The math is shared with enemies in CombatTools.ResolveHit

    [FoldoutGroup("Damage")]
    [Tooltip("Where damage numbers pop up, relative to the player's position")]
    [SerializeField] Vector3 _damageTextOffset;

    [FoldoutGroup("Damage")]
    [Tooltip("Runs when health reaches 0")]
    [SerializeField]
    private UnityEvent onDeath;

    //local variables
    IStaggerable _staggerable; //the player can't be staggered yet, but if it ever gets an IStaggerable, crits on stagger work automatically
    IUnitData _unitData; //for the danger level camera shake dampening

    private void Awake()
    {
        //interfaces can't be dragged into the inspector, so these are found once here
        _staggerable = GetComponent<IStaggerable>();
        _unitData = GetComponent<IUnitData>();
    }

    #region Damage
    public void TakeDamage(DamagePackage dmg)
    {
        //function hooked to EnemyDamagable's onDamage event on the player prefab
        if (dmg == null) return;
        PlayerStats stats = GetPlayerStats();
        if (stats == null) return;

        bool isStaggered = _staggerable != null && _staggerable.IsStaggered();
        HitResult result = CombatTools.ResolveHit(dmg, stats, isStaggered);
        CombatTools.ShowHitPopups(transform.position + _damageTextOffset, result);

        stats._Health -= result._TotalDamage;
        if (stats._Health <= 0) onDeath?.Invoke();

        ShakeCameraFromHit(dmg, stats);
    }

    public void Heal(int amount)
    {
        //function for healing. Skips the damage formula entirely. todo: a real healing system comes later (see Damage Receivers in the GDD)
        PlayerStats stats = GetPlayerStats();
        if (amount <= 0 || stats == null) return;
        stats._Health = Mathf.Min(stats._Health + amount, stats._HP); //never above max health
        CombatTools.ShowHealPopup(transform.position + _damageTextOffset, amount);
    }

    void ShakeCameraFromHit(DamagePackage dmg, PlayerStats stats)
    {
        //function that shakes the camera when the player is hit, softened by how hurt they are (danger level)
        if (CameraManager._CamManager == null || CameraManager._CamManager._currentFollowTarget != gameObject) return;

        float dangerMultiplier = 1f;
        DangerLevel danger = null;
        if (_unitData != null) danger = _unitData.GetDangerLevelSettings();
        if (danger != null && danger._DangerLevels != null)
        {
            int dangerIndex = danger.GetCurrentDangerIndex(danger.GetCurrentDangerType(stats._Health, stats._HP));
            if (dangerIndex >= 0 && dangerIndex < danger._DangerLevels.Count) dangerMultiplier = danger._DangerLevels[dangerIndex]._ShakeDampenMultiplier;
        }

        CameraManager._CamManager.CombatCameraShake(dmg._DamageImpactStrength * dangerMultiplier, null);
    }

    PlayerStats GetPlayerStats()
    {
        //function that finds the player's stats, with an error if something in the chain is missing
        if (Player.player == null || Player.player.pData == null || Player.player.pData.pStats == null)
        {
            Debug.LogError($"Error! Player stats not found for PlayerDamegable on {gameObject.name}", this);
            return null;
        }
        return Player.player.pData.pStats;
    }
    #endregion

    public void Death()
    {
        //temp stuff for now
        PlayerStats stats = GetPlayerStats();
        if (stats == null) return;
        stats._Health = stats._HP;
    }
}
