using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class PlayerDamegable : MonoBehaviour
{
    [FoldoutGroup("Damage")]
    [SerializeField] Vector3 _damageTextOffset;

    [FoldoutGroup("Damage")]
    [SerializeField]
    private UnityEvent onDeath;

    #region Damage
    public void TakeDamage(DamagePackage dmg)
    {
        print(dmg._Source.name + " hit " + gameObject.name);
        int damageTakenTotal = 0;
        for (int i = 0; i < dmg._Entries.Count; i++)
        {
            if (ScreenDamageUIManager._UIdamage != null) ScreenDamageUIManager._UIdamage._damageCanvas.displayDamage(transform.position + _damageTextOffset, (int)(DamageCalculation(dmg._Entries[i]) * dmg._CritMultiplier), false);
            damageTakenTotal += (int)(DamageCalculation(dmg._Entries[i]) * dmg._CritMultiplier);
        }
        Player.player.pData.pStats._Health -= damageTakenTotal;


        if (Player.player.pData.pStats._Health <= 0) onDeath?.Invoke();
        if (Player.player.pData.pStats._Health > Player.player.pData.pStats._HP) Player.player.pData.pStats._Health = Player.player.pData.pStats._HP;

        //camera shake

        if (CameraManager._CamManager._currentFollowTarget == gameObject)
        {
            DangerLevel temp = GetComponent<IUnitData>().GetDangerLevelSettings();
            float dangerMultiplier = temp._DangerLevels[temp.getCurrentDangerIndex(temp.getCurrentDangerType(Player.player.pData.pStats._Health, Player.player.pData.pStats._HP))]._ShakeDampenMultiplier;
            print($"current shake dampen is {dangerMultiplier} with a final strength of {dmg._DamageImpactStrength * dangerMultiplier}");
            CameraManager._CamManager.CombatCameraShake(dmg._DamageImpactStrength*dangerMultiplier, null);
        }
    }

    public int DamageCalculation(DamageEntry entry)
    {
        float defenseStat = -Player.player.pData.pStats.TypeToStatFinder(Player.player.pData.pStats.GetDefensiveStatType(entry._statType));
        float dmg = ((entry._Damage) - defenseStat) / Player.player.pData.pStats.GetAttackResistanceModifier(entry._atkType, entry._elementType);
        return (int)dmg;
    }

    #endregion
    public void Death()
    {
        //temp stuff for now
        print("Ope I died");
        Player.player.pData.pStats._Health = Player.player.pData.pStats._HP;
    }
}
